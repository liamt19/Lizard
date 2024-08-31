
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Lizard.Properties;

namespace Lizard.Logic.NN
{
    public static unsafe class NNUE
    {
        public const NetworkArchitecture NetArch = NetworkArchitecture.Bucketed768;
        public static readonly bool UseAvx = Avx2.IsSupported;

        [MethodImpl(Inline)]
        public static void RefreshAccumulator(Position pos)
        {
            Bucketed768.RefreshAccumulator(pos);
        }

        [MethodImpl(Inline)]
        public static short GetEvaluation(Position pos)
        {
            int v = UseAvx ? Bucketed768.GetEvaluationUnrolled512(pos) :
                             Bucketed768.GetEvaluation(pos);

            v = int.Clamp(v, -MaxNormalScore, MaxNormalScore);

            return (short)v;
        }

        [MethodImpl(Inline)]
        public static void MakeMove(Position pos, Move m)
        {
            Bucketed768.MakeMove(pos, m);
        }

        [MethodImpl(Inline)]
        public static void MakeNullMove(Position pos)
        {
            Bucketed768.MakeNullMove(pos);
        }



        /// <summary>
        /// Calls the Initialize method for the selected architecture.
        /// <br></br>
        /// Call <see cref="RefreshAccumulator"/> for any existing positions, since their accumulators will no longer be computed!
        /// </summary>
        public static void LoadNewNetwork(string networkToLoad)
        {
            Bucketed768.Initialize(networkToLoad, exitIfFail: false);
        }


        /// <summary>
        /// Attempts to open the file <paramref name="networkToLoad"/> if it exists, 
        /// and otherwise loads the embedded network.
        /// </summary>
        public static Stream TryOpenFile(string networkToLoad, bool exitIfFail = true)
        {
            if (File.Exists(networkToLoad))
            {
                Log($"Using NNUE with 768 network {networkToLoad}");
                return File.OpenRead(networkToLoad);
            }

            //  Try to load the default network
            networkToLoad = Bucketed768.NetworkName;
            string resourceName = networkToLoad.Replace(".nnue", string.Empty).Replace(".bin", string.Empty);
            Log($"Using NNUE with 768 network {networkToLoad}");

            //  First look for it as an embedded resource
            object o = Resources.ResourceManager.GetObject(resourceName);
            if (o != null)
                return new MemoryStream((byte[])o);


            //  Then look for it as an absolute path
            if (File.Exists(networkToLoad))
                return File.OpenRead(networkToLoad);


            //  Lastly try looking for it in the current directory
            var cwdFile = Path.Combine(Environment.CurrentDirectory, networkToLoad);
            if (File.Exists(cwdFile))
                return File.OpenRead(cwdFile);


            Console.WriteLine($"Couldn't find a network named '{networkToLoad}' as a compiled resource or as a file within the current directory!");
            Console.ReadLine();
            
            if (exitIfFail)
                Environment.Exit(-1);

            return null;
        }


        [MethodImpl(Inline)]
        public static int SumVectorNoHadd(Vector256<int> vect)
        {
            Vector128<int> lo = vect.GetLower();
            Vector128<int> hi = Avx.ExtractVector128(vect, 1);
            Vector128<int> sum128 = Sse2.Add(lo, hi);

            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, 0b_10_11_00_01));
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, 0b_01_00_11_10));

            //  Something along the lines of Add(sum128, UnpackHigh(sum128, sum128))
            //  would also work here but it is occasionally off by +- 1.
            //  The JIT also seems to replace the unpack with a shuffle anyways depending on the instruction order,
            //  and who am I to not trust the JIT? :)

            return Sse2.ConvertToInt32(sum128);
        }

        [MethodImpl(Inline)]
        public static int SumVectorNoHadd(Vector512<int> vect)
        {
            //  _mm512_reduce_add_epi32 is a sequence instruction and isn't callable
            return SumVectorNoHadd(vect.GetLower()) + SumVectorNoHadd(vect.GetUpper());
        }

        /// <summary>
        /// Transposes the weights stored in <paramref name="block"/>
        /// </summary>
        public static void TransposeLayerWeights(short* block, int columnLength, int rowLength)
        {
            short* temp = stackalloc short[columnLength * rowLength];
            Unsafe.CopyBlock(temp, block, (uint)(sizeof(short) * columnLength * rowLength));

            for (int bucket = 0; bucket < rowLength; bucket++)
            {
                short* thisBucket = block + (bucket * columnLength);

                for (int i = 0; i < columnLength; i++)
                {
                    thisBucket[i] = temp[(rowLength * i) + bucket];
                }
            }
        }

        public static void NetStats(string layerName, void* layer, int n)
        {
            long avg = 0;
            int max = int.MinValue;
            int min = int.MaxValue;

            short* ptr = (short*)layer;
            for (int i = 0; i < n; i++)
            {
                max = Math.Max(max, ptr[i]);
                min = Math.Min(min, ptr[i]);
                avg += ptr[i];
            }

            Log($"{layerName}\tmin: {min}, max: {max}, avg: {(double)avg / n}");
        }



        public static void Trace(Position pos)
        {
            char[][] board = new char[3 * 8 + 1][];
            for (int i = 0; i < 3 * 8 + 1; i++)
            {
                board[i] = new char[8 * 8 + 2];
                Array.Fill(board[i], ' ');
            }

            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                board[row][8 * 8 + 1] = '\0';
            }

            int baseEval = GetEvaluation(pos);

            Log($"\nNNUE evaluation: {baseEval}\n");

            ref Accumulator Accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;
            for (int f = Files.A; f <= Files.H; f++)
            {
                for (int r = 0; r <= 7; r++)
                {
                    int idx = CoordToIndex(f, r);
                    int pt = bb.GetPieceAtIndex(idx);
                    int pc = bb.GetColorAtIndex(idx);
                    int fishPc = pt + 1 + pc * 8;
                    int v = ScoreMate;

                    if (pt != None && bb.GetPieceAtIndex(idx) != King)
                    {
                        bb.RemovePiece(idx, pc, pt);

                        RefreshAccumulator(pos);
                        int eval = GetEvaluation(pos);
                        v = baseEval - eval;

                        bb.AddPiece(idx, pc, pt);
                    }

                    writeSquare(board, f, r, fishPc, v);
                }
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }
        }

        public static void TracePieceValues(int pieceType, int pieceColor)
        {
            char[][] board = new char[3 * 8 + 1][];
            for (int i = 0; i < 3 * 8 + 1; i++)
            {
                board[i] = new char[8 * 8 + 2];
                Array.Fill(board[i], ' ');
            }

            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                board[row][8 * 8 + 1] = '\0';
            }

            //  White king on A1, black king on H8
            Position pos = new Position("7k/8/8/8/8/8/8/K7 w - - 0 1", true, owner: GlobalSearchPool.MainThread);
            int baseEval = GetEvaluation(pos);

            Log($"\nNNUE evaluation: {baseEval}\n");

            ref Bitboard bb = ref pos.bb;

            for (int i = 0; i < SquareNB; i++)
            {
                if (bb.GetPieceAtIndex(i) != None)
                {

                    int fp = bb.GetPieceAtIndex(i) + 1 + bb.GetColorAtIndex(i) * 8;
                    writeSquare(board, GetIndexFile(i), GetIndexRank(i), fp, ScoreMate);
                    continue;
                }

                bb.AddPiece(i, pieceColor, pieceType);
                RefreshAccumulator(pos);
                int eval = GetEvaluation(pos);
                bb.RemovePiece(i, pieceColor, pieceType);

                writeSquare(board, GetIndexFile(i), GetIndexRank(i), pieceType + 1 + pieceColor * 8, eval);
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }

            Log("\n");
        }

        private static void writeSquare(char[][] board, int file, int rank, int pc, int value)
        {
            const string PieceToChar = " PNBRQK  pnbrqk";

            int x = file * 8;
            int y = (7 - rank) * 3;

            for (int i = 1; i < 8; i++)
            {
                board[y][x + i] = board[y + 3][x + i] = '-';
            }

            for (int i = 1; i < 3; i++)
            {
                board[y + i][x] = board[y + i][x + 8] = '|';
            }

            board[y][x] = board[y][x + 8] = board[y + 3][x + 8] = board[y + 3][x] = '+';

            if (pc != 0 && !(pc == 15 && value == ScoreMate))
            {
                board[y + 1][x + 4] = PieceToChar[pc];
            }

            if (value != ScoreMate)
            {
                fixed (char* ptr = &board[y + 2][x + 2])
                {
                    format_cp_ptr(value, ptr);
                }
            }

        }


        private static void format_cp_ptr(int v, char* buffer)
        {
            buffer[0] = v < 0 ? '-' : v > 0 ? '+' : ' ';

            //  This reduces the displayed value of each piece so that it is more in line with
            //  conventional piece values, i.e. pawn = ~100, bishop/knight = ~300, rook = ~500
            const int Normalization = 200;
            int cp = Math.Abs(100 * v / Normalization);

            if (cp >= 10000)
            {
                buffer[1] = (char)('0' + cp / 10000); cp %= 10000;
                buffer[2] = (char)('0' + cp / 1000); cp %= 1000;
                buffer[3] = (char)('0' + cp / 100); cp %= 100;
                buffer[4] = ' ';
            }
            else if (cp >= 1000)
            {
                buffer[1] = (char)('0' + cp / 1000); cp %= 1000;
                buffer[2] = (char)('0' + cp / 100); cp %= 100;
                buffer[3] = '.';
                buffer[4] = (char)('0' + cp / 10);
            }
            else
            {
                buffer[1] = (char)('0' + cp / 100); cp %= 100;
                buffer[2] = '.';
                buffer[3] = (char)('0' + cp / 10); cp %= 10;
                buffer[4] = (char)('0' + cp / 1);
            }
        }
    }
}
