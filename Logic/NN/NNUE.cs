
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

using Lizard.Properties;

namespace Lizard.Logic.NN
{
    public static unsafe class NNUE
    {
        public const NetworkArchitecture NetArch = NetworkArchitecture.Bucketed768;
        public static readonly bool UseAvx = Avx2.IsSupported && false;
        public static readonly bool UseSSE = Sse3.IsSupported;
        public static readonly bool UseARM = AdvSimd.IsSupported;
        public static bool UseFallback => !(UseAvx || UseSSE || UseARM);

        [MethodImpl(Inline)]
        public static void RefreshAccumulator(Position pos)
        {
            Bucketed768.RefreshAccumulator(pos);
        }

        [MethodImpl(Inline)]
        public static short GetEvaluation(Position pos)
        {
            return Bucketed768.GetEvaluation(pos);
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
                Log($"Loading {networkToLoad} via filepath");
                return File.OpenRead(networkToLoad);
            }

            //  Try to load the default network
            networkToLoad = Bucketed768.NetworkName;

            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                var resName = $"{asm.GetName().Name}.{networkToLoad}";
                Debug.WriteLine($"resources [{string.Join(", ", asm.GetManifestResourceNames())}]");
                foreach (string res in asm.GetManifestResourceNames())
                {
                    //  Specifically exclude the .resx file
                    if (res.ToLower().Contains("properties"))
                        continue;

                    if (res.EndsWith(".bin") || res.EndsWith(".nnue") || res.EndsWith(".zstd"))
                    {
                        Stream stream = asm.GetManifestResourceStream(res);
                        if (stream != null)
                        {
                            Log($"Loading {res} via reflection");
                            return stream;
                        }
                    }
                }
            }
            catch { }

            //  Then look for it as an absolute path
            if (File.Exists(networkToLoad))
            {
                Log($"Loading {networkToLoad} via absolute path");
                return File.OpenRead(networkToLoad);
            }

            //  Lastly try looking for it in the current directory
            var cwdFile = Path.Combine(Environment.CurrentDirectory, networkToLoad);
            if (File.Exists(cwdFile))
            {
                Log($"Loading {networkToLoad} via relative path");
                return File.OpenRead(cwdFile);
            }


            Console.WriteLine($"Couldn't find a network named '{networkToLoad}' or as a compiled resource or as a file within the current directory!");
            Console.ReadLine();

            if (exitIfFail)
                Environment.Exit(-1);

            return null;
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

            RefreshAccumulator(pos);
            Log("Buckets:\n");
            for (int b = 0; b < Bucketed768.OUTPUT_BUCKETS; b++)
            {
                var ev = Bucketed768.GetEvaluation(pos, b);
                Log($"bucket {b}: {ev,6}" + ((baseEval == ev) ? "    <--- Using this bucket" : string.Empty));
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
            RefreshAccumulator(pos);
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
