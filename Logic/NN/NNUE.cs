﻿
using System.Runtime.Intrinsics.X86;

using Lizard.Properties;

namespace Lizard.Logic.NN
{
    public static unsafe class NNUE
    {
        public const NetworkArchitecture NetArch = NetworkArchitecture.Simple768;
        public static readonly bool UseAvx = Avx2.IsSupported;

        public static void RefreshAccumulator(Position pos)
        {
            if (NetArch == NetworkArchitecture.Simple768)
            {
                Simple768.RefreshAccumulator(pos);
            }
            else if (NetArch == NetworkArchitecture.Bucketed768)
            {
                Bucketed768.RefreshAccumulator(pos);
            }
        }

        public static short GetEvaluation(Position pos)
        {
            if (NetArch == NetworkArchitecture.Simple768)
            {
                if (UseAvx)
                {
                    return (short)Simple768.GetEvaluationUnrolled(pos);
                }
                else
                {
                    return (short)Simple768.GetEvaluation(pos);
                }
            }
            else if (NetArch == NetworkArchitecture.Bucketed768)
            {
                if (UseAvx)
                {
                    return (short)Bucketed768.GetEvaluationUnrolled(pos);
                }
                else
                {
                    return (short)Bucketed768.GetEvaluation(pos);
                }
            }

        }

        public static void MakeMove(Position pos, Move m)
        {
            if (NetArch == NetworkArchitecture.Simple768)
            {
                Simple768.MakeMove(pos, m);
            }
            else if (NetArch == NetworkArchitecture.Bucketed768)
            {
                Bucketed768.MakeMove(pos, m);
            }
        }



        /// <summary>
        /// Calls the Initialize method for the selected architecture.
        /// <br></br>
        /// Call <see cref="RefreshAccumulator"/> for any existing positions, since their accumulators will no longer be computed!
        /// </summary>
        public static void LoadNewNetwork(string networkToLoad)
        {
            if (NetArch == NetworkArchitecture.Simple768)
            {
                Simple768.Initialize(networkToLoad, exitIfFail: false);
            }
            else if (NetArch == NetworkArchitecture.Bucketed768)
            {
                Bucketed768.Initialize(networkToLoad, exitIfFail: false);
            }
        }


        /// <summary>
        /// Attempts to open the file <paramref name="networkToLoad"/> if it exists, 
        /// and otherwise loads the embedded network.
        /// </summary>
        public static Stream TryOpenFile(string networkToLoad, bool exitIfFail = true)
        {
            Stream netFile;

            if (File.Exists(networkToLoad))
            {
                netFile = File.OpenRead(networkToLoad);
                Log("Using NNUE with 768 network " + networkToLoad);
            }
            else
            {
                var NetworkName = NetArch == NetworkArchitecture.Simple768 ? Simple768.NetworkName :
                                                                             Bucketed768.NetworkName;

                //  Just load the default network
                networkToLoad = NetworkName;
                Log("Using NNUE with 768 network " + NetworkName);

                string resourceName = networkToLoad.Replace(".nnue", string.Empty).Replace(".bin", string.Empty);

                object? o = Resources.ResourceManager.GetObject(resourceName);
                if (o == null)
                {
                    Console.WriteLine("The 768 NNRunOption was set to true, but there isn't a valid 768 network to load!");
                    Console.WriteLine("This program looks for a 768 network named " + "'nn.nnue' or '" + NetworkName + "' within the current directory.");
                    Console.WriteLine("If neither can be found, then '" + NetworkName + "' needs to be a compiled as a resource as a fallback!");
                    Console.ReadLine();

                    if (exitIfFail)
                    {
                        Environment.Exit(-1);
                    }
                }

                byte[] data = (byte[])o;
                netFile = new MemoryStream(data);
            }

            return netFile;
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

            Log("\nNNUE evaluation: " + baseEval + "\n");

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
            Position pos = new Position("7k/8/8/8/8/8/8/K7 w - - 0 1", true, owner: SearchPool.MainThread);
            int baseEval = GetEvaluation(pos);

            Log("\nNNUE evaluation: " + baseEval + "\n");

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
