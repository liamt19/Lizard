using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace LTChess.Logic.Util
{
    public static class Utilities
    {
        public const int NumPieces = 6;

        public const int IndexTop = 7;
        public const int IndexBot = 0;
        public const int IndexLeft = 0;
        public const int IndexRight = 7;
        public const string InitialFEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public const string EngineBuildVersion = "8.3";
        public const string EngineTagLine = "Now with even more pointers!";

        public const int MaxListCapacity = 512;
        public const int ExtendedListCapacity = 256;
        public const int NormalListCapacity = 128;
        public const int LSBEmpty = 64;
        public const int MaxDepth = 64;

        public const int MaxPly = 256;

        /// <summary>
        /// The maximum ply that SimpleSearch's SearchStackEntry* array can be indexed at.
        /// <br></br>
        /// The array actually contains MaxPly == 256 entries, but the first 10 of them are off limits to
        /// prevent accidentally indexing memory before the stack.
        /// </summary>
        public const int MaxSearchStackPly = 256 - 10;

        public const nuint AllocAlignment = 32;

        public const ulong Border = 0xFF818181818181FF;
        public const ulong PawnPromotionSquares = 0xFF000000000000FF;
        public const ulong Corners = 0x8100000000000081;

        public const ulong Empty = 0UL;
        public const ulong AllSquares = ~(0UL);
        public const ulong DarkSquares = 0xAA55AA55AA55AA55UL;
        public const ulong LightSquares = 0x55AA55AA55AA55AAUL;

        public const ulong FileABB = 0x0101010101010101UL;
        public const ulong FileBBB = FileABB << 1;
        public const ulong FileCBB = FileABB << 2;
        public const ulong FileDBB = FileABB << 3;
        public const ulong FileEBB = FileABB << 4;
        public const ulong FileFBB = FileABB << 5;
        public const ulong FileGBB = FileABB << 6;
        public const ulong FileHBB = FileABB << 7;

        public const ulong Rank1BB = 0xFF;
        public const ulong Rank2BB = Rank1BB << (8 * 1);
        public const ulong Rank3BB = Rank1BB << (8 * 2);
        public const ulong Rank4BB = Rank1BB << (8 * 3);
        public const ulong Rank5BB = Rank1BB << (8 * 4);
        public const ulong Rank6BB = Rank1BB << (8 * 5);
        public const ulong Rank7BB = Rank1BB << (8 * 6);
        public const ulong Rank8BB = Rank1BB << (8 * 7);

        public const ulong QueenSide = FileABB | FileBBB | FileCBB | FileDBB;
        public const ulong CenterFiles = FileCBB | FileDBB | FileEBB | FileFBB;
        public const ulong KingSide = FileEBB | FileFBB | FileGBB | FileHBB;
        public const ulong Center = (FileDBB | FileEBB) & (Rank4BB | Rank5BB);

        public const ulong MobilityInner = 0x3C3C3C3C0000;
        public const ulong MobilityOutter = AllSquares ^ MobilityInner;

        public static readonly ulong[] LowRanks = new ulong[]
        {
            (Rank2BB | Rank3BB),
            (Rank7BB | Rank6BB),
        };

        public const ulong WhiteKingsideMask = (1UL << F1) | (1UL << G1);
        public const ulong WhiteQueensideMask = (1UL << B1) | (1UL << C1) | (1UL << D1);
        public const ulong BlackKingsideMask = (1UL << F8) | (1UL << G8);
        public const ulong BlackQueensideMask = (1UL << B8) | (1UL << C8) | (1UL << D8);

        /// <summary>
        /// A mask of the ranks that outpost squares can be on for each color
        /// </summary>
        public static readonly ulong[] OutpostSquares = { (Rank4BB | Rank5BB | Rank6BB), (Rank3BB | Rank4BB | Rank5BB) };


        public static bool JITHasntSeenSearch = false;

        public static bool IsRunningConcurrently = false;

        public static long debug_time_off = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        /// <summary>
        /// Writes the string <paramref name="s"/> to the debugger, and to the log file if in UCI mode or to the console otherwise.
        /// </summary>
        [MethodImpl(Inline)]
        public static void Log(string s)
        {
            if (JITHasntSeenSearch)
            {
                return;
            }

            if (!UCI.Active)
            {
                Console.WriteLine(s);
            }
            else
            {
                LogString("[LOG]: " + s);
            }

            Debug.WriteLine(s);
        }

        public static void Log(int i) => Log(i.ToString());

        /// <summary>
        /// If there are multiple instances of this engine running, we won't write to the ucilog file.
        /// <br></br>
        /// This uses a FileStream to access it and a mutex to make writes atomic, so having multiple
        /// processes all doing that at the same time is needlessly risky.
        /// </summary>
        public static void CheckConcurrency()
        {
            if (Process.GetProcesses().Where(x => x.ProcessName == (Process.GetCurrentProcess().ProcessName)).Count() > 1)
            {
                Log("Running concurrently!");
                IsRunningConcurrently = true;
            }
        }

        [MethodImpl(Inline)]
        public static int Rotate180(int square)
        {
            return square ^ 0x3F;
        }


        public static class Direction
        {
            public const int NORTH =  8;
            public const int EAST  =  1;
            public const int SOUTH = -NORTH;
            public const int WEST  = -EAST;

            public const int NORTH_EAST = NORTH + EAST;
            public const int SOUTH_EAST = SOUTH + EAST;
            public const int SOUTH_WEST = SOUTH + WEST;
            public const int NORTH_WEST = NORTH + WEST;
        }

        /// <summary>
        /// Returns the <c>Direction</c> that the <paramref name="color"/> pawns move in, white pawns up, black pawns down.
        /// </summary>
        [MethodImpl(Inline)]
        public static int ShiftUpDir(int color) => (color == Color.White) ? Direction.NORTH : Direction.SOUTH;

        [MethodImpl(Inline)]
        public static int ShiftDownDir(int color) => (color == Color.Black) ? Direction.NORTH : Direction.SOUTH;

        /// <summary>
        /// Returns a bitboard with bits set 1 "above" the bits in <paramref name="b"/>.
        /// So Forward(Color.White) with a bitboard that has A2 set will return one with A3 set,
        /// and Forward(Color.Black) returns one with A1 set instead.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Forward(int color, ulong b)
        {
            if (color == Color.White)
            {
                return Shift(Direction.NORTH, b);
            }

            return Shift(Direction.SOUTH, b);
        }

        /// <summary>
        /// Returns a bitboard with bits set 1 "below" the bits in <paramref name="b"/>.
        /// So Backward(Color.White) with a bitboard that has A2 set will return one with A1 set,
        /// and Backward(Color.Black) returns one with A3 set instead.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Backward(int color, ulong b)
        {
            if (color == Color.White)
            {
                return Shift(Direction.SOUTH, b);
            }

            return Shift(Direction.NORTH, b);
        }

        [MethodImpl(Inline)]
        public static ulong Shift(int dir, ulong b)
        {
            return dir == Direction.NORTH ? b << 8
                : dir == Direction.SOUTH ? b >> 8
                : dir == Direction.NORTH + Direction.NORTH ? b << 16
                : dir == Direction.SOUTH + Direction.SOUTH ? b >> 16
                : dir == Direction.EAST ? (b & ~FileHBB) << 1
                : dir == Direction.WEST ? (b & ~FileABB) >> 1
                : dir == Direction.NORTH_EAST ? (b & ~FileHBB) << 9
                : dir == Direction.NORTH_WEST ? (b & ~FileABB) << 7
                : dir == Direction.SOUTH_EAST ? (b & ~FileHBB) >> 7
                : dir == Direction.SOUTH_WEST ? (b & ~FileABB) >> 9
                : 0;
        }

        [MethodImpl(Inline)]
        public static ulong PawnShift(int color, ulong b)
        {
            if (color == Color.White)
            {
                return Shift(Direction.NORTH_WEST, b) | Shift(Direction.NORTH_EAST, b);
            }

            return Shift(Direction.SOUTH_WEST, b) | Shift(Direction.SOUTH_EAST, b);
        }

        [MethodImpl(Inline)]
        public static double InCentipawns(double score)
        {
            double div = Math.Round(score / 100, 2);
            return div;
        }

        [MethodImpl(Inline)]
        public static int Not(int color)
        {
            //return (color == Color.White) ? Color.Black : Color.White;
            return color ^ 1;
        }

        [MethodImpl(Inline)]
        public static string ColorToString(int color)
        {
            if (color == Color.White)
            {
                return "White";
            }
            else if (color == Color.Black)
            {
                return "Black";
            }
            else
            {
                return "None";
            }
        }

        [MethodImpl(Inline)]
        public static string PieceToString(int n)
        {
            switch (n)
            {
                case Piece.Pawn:
                    return "Pawn";
                case Piece.Knight:
                    return "Knight";
                case Piece.Bishop:
                    return "Bishop";
                case Piece.Rook:
                    return "Rook";
                case Piece.Queen:
                    return "Queen";
                case Piece.King:
                    return "King";
            }

            return "None";
        }

        /// <summary>
        /// Returns a random ulong using the Random instance <paramref name="random"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong NextUlong(this Random random)
        {
            Span<byte> arr = new byte[8];
            random.NextBytes(arr);

            return BitConverter.ToUInt64(arr);
        }

        /// <summary>
        /// Returns the letter of the file numbered <paramref name="fileNumber"/>, so GetFileChar(0) returns 'a'.
        /// </summary>
        [MethodImpl(Inline)]
        public static char GetFileChar(int fileNumber) => (char)(97 + fileNumber);

        /// <summary>
        /// Returns the number of the file with the letter <paramref name="fileLetter"/>, so GetFileInt('a') returns 0.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetFileInt(char fileLetter) => fileLetter - 97;

        /// <summary>
        /// Returns the first letter of the name of the piece of type <paramref name="pieceType"/>, so PieceToFENChar(0 [Piece.Pawn]) returns 'P'.
        /// </summary>
        [MethodImpl(Inline)]
        public static char PieceToFENChar(int pieceType)
        {
            switch (pieceType)
            {
                case Piece.Pawn:
                    return 'P';
                case Piece.Knight:
                    return 'N';
                case Piece.Bishop:
                    return 'B';
                case Piece.Rook:
                    return 'R';
                case Piece.Queen:
                    return 'Q';
                case Piece.King:
                    return 'K';
            }

            return ' ';
        }

        /// <summary>
        /// Returns the numerical piece type of the piece given its FEN character <paramref name="fenChar"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int FENToPiece(char fenChar)
        {
            fenChar = char.ToLower(fenChar);
            if (fenChar == 'p')
            {
                return Piece.Pawn;
            }
            if (fenChar == 'n')
            {
                return Piece.Knight;
            }
            if (fenChar == 'b')
            {
                return Piece.Bishop;
            }
            if (fenChar == 'r')
            {
                return Piece.Rook;
            }
            if (fenChar == 'q')
            {
                return Piece.Queen;
            }
            if (fenChar == 'k')
            {
                return Piece.King;
            }

            Log("ERROR Failed parsing FEN character '" + fenChar + "'");
            return Piece.None;
        }

        public static int FENToColor(char c) => char.IsUpper(c) ? Color.White : Color.Black;

        /// <summary>
        /// Returns the file (x coordinate) for the index, which is between A=0 and H=7.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetIndexFile(int index) => index & 7;

        /// <summary>
        /// Returns the rank (y coordinate) for the index, which is between 0 and 7.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetIndexRank(int index) => index >> 3;

        /// <summary>
        /// Sets <paramref name="x"/> to the file of <paramref name="index"/>, and <paramref name="y"/> to its rank.
        /// </summary>
        [MethodImpl(Inline)]
        public static void IndexToCoord(in int index, out int x, out int y)
        {
            x = index % 8;
            y = index / 8;
        }

        /// <summary>
        /// Returns the index of the square with the rank <paramref name="x"/> and file <paramref name="y"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int CoordToIndex(int x, int y)
        {
            return (y * 8) + x;
        }

        /// <summary>
        /// Returns the rank and file of the square <paramref name="idx"/>, which looks like "a1" or "e4".
        /// </summary>
        [MethodImpl(Inline)]
        public static string IndexToString(int idx)
        {
            return "" + GetFileChar(GetIndexFile(idx)) + (GetIndexRank(idx) + 1);
        }

        [MethodImpl(Inline)]
        public static string CoordToString(int x, int y)
        {
            return "" + GetFileChar(x) + (y + 1);
        }

        /// <summary>
        /// Returns the index of the square <paramref name="s"/>, which should look like "a1" or "e4".
        /// </summary>
        [MethodImpl(Inline)]
        public static int StringToIndex(string s)
        {
            return CoordToIndex(GetFileInt(s[0]), int.Parse(s[1].ToString()) - 1);
        }

        /// <summary>
        /// Returns true if <paramref name="x"/> and <paramref name="y"/> are both between 0 and 7.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool InBounds(int x, int y)
        {
            return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
        }

        /// <summary>
        /// Returns a string formatted as an 8x8 square with the bits set in <paramref name="bb"/> as 1's and everything else as 0's.
        /// </summary>
        public static string FormatBB(ulong bb)
        {
            StringBuilder temp = new StringBuilder();

            temp.Append(Convert.ToString((long)bb, 2));
            if (temp.Length != 64)
            {
                temp.Insert(0, "0", 64 - temp.Length);
            }

            string s = temp.ToString();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                if (i != 7)
                {
                    sb.AppendLine(s.Substring(8 * i, 8).Flip());
                }
                else
                {
                    sb.Append(s.Substring(8 * i, 8).Flip());
                }
            }

            return sb.ToString() + "\r\n";
        }

        /// <summary>
        /// Returns the string <paramref name="s"/> backwards.
        /// </summary>
        public static string Flip(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Returns a text representation of the board
        /// </summary>
        public static string PrintBoard(Bitboard bb)
        {
            StringBuilder sb = new StringBuilder();

            for (int y = 7; y >= 0; y--)
            {
                sb.Append((y + 1) + " |");
                for (int x = 0; x < 8; x++)
                {
                    int idx = CoordToIndex(x, y);
                    int pt = bb.GetPieceAtIndex(idx);

                    if (bb.IsColorSet(Color.White, idx))
                    {
                        char c = PieceToFENChar(pt);
                        sb.Append(char.ToUpper(c) + " ");
                    }
                    else if (bb.IsColorSet(Color.Black, idx))
                    {
                        char c = PieceToFENChar(pt);
                        sb.Append(char.ToLower(c) + " ");
                    }
                    else
                    {
                        sb.Append(" " + " ");
                    }
                }
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine("|");

            }
            sb.AppendLine("   A B C D E F G H");

            return sb.ToString();
        }

        public static string SpanToString<T>(Span<T> list) where T : struct
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Length; i++)
            {
                sb.Append(list[i].ToString() + ", ");
            }
            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string containing a ToString() version of a list of moves in human readable format.
        /// This would return something similar to "g1f3, e2e4, d2d4".
        /// </summary>
        public static string Stringify(this Span<Move> list, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();

            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Equals(Move.Null))
                {
                    break;
                }
                sb.Append(list[i].ToString() + ", ");
            }
            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a string containing a ToString() version of a list of moves in human readable format.
        /// So instead of seeing "g1f3, e2e4, d2d4" it would show "Nf3, e4, d4".
        /// </summary>
        public static string Stringify(this Span<Move> list, Position position, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();
            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Equals(Move.Null))
                {
                    break;
                }
                string s = list[i].ToString(position);
                sb.Append(s + ", ");
            }

            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }

        //  https://stackoverflow.com/questions/18573004/how-to-center-align-arguments-in-a-format-string
        public static string CenteredString(string s, int width)
        {
            if (s.Length >= width)
            {
                return s;
            }

            int leftPadding = (width - s.Length) / 2;
            int rightPadding = width - s.Length - leftPadding;

            return new string(' ', leftPadding) + s + new string(' ', rightPadding);
        }

        public static bool EqualsIgnoreCase(this string s, string other)
        {
            return s.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string s, string other)
        {
            return s.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string s, string other)
        {
            return s.Contains(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a string representing the current search statistics, which is sent to chess GUI programs.
        /// <br></br>
        /// If <paramref name="TraceEval"/> is true, an evaluation of the position will be printed as well.
        /// </summary>
        public static string FormatSearchInformation(ref SearchInformation info)
        {
            int depth = info.MaxDepth;
            int selDepth = info.SelectiveDepth;
            double time = Math.Max(1, Math.Round(info.TimeManager.GetSearchTime()));
            var score = FormatMoveScore(info.BestScore);
            double nodes = info.NodeCount;
            int nodesPerSec = ((int)(nodes / (time / 1000)));

            StringBuilder sb = new StringBuilder();
            sb.Append("info depth " + depth);
            sb.Append(" seldepth " + selDepth);
            sb.Append(" time " + time);
            sb.Append(" score " + score);
            sb.Append(" nodes " + nodes);
            sb.Append(" nps " + nodesPerSec);
            sb.Append(" hashfull " + TranspositionTable.GetHashFull());
            sb.Append(" pv " + info.GetPVString(true));

            return sb.ToString();
        }

        /// <summary>
        /// Returns an appropriately formatted string representing the Score, which is either "cp #" or "mate #".
        /// </summary>
        [MethodImpl(Inline)]
        public static string FormatMoveScore(int score)
        {
            if (ThreadedEvaluation.IsScoreMate(score, out int mateIn))
            {
                return "mate " + mateIn;
            }
            else
            {
                return "cp " + score;
            }
        }

        /// <summary>
        /// Used in evaluation traces to truncate the evaluation term's Score <paramref name="normalScore"/> to two decimal places,
        /// and to keep that number centered within its column, which has a size of <paramref name="sz"/>.
        /// </summary>
        public static string FormatEvalTerm(double normalScore, int sz = 8)
        {
            return CenteredString(string.Format("{0:N2}", InCentipawns(normalScore)), sz);
        }




        /// <summary>
        /// Returns a ulong with bits set along whichever file <paramref name="idx"/> is in.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetFileBB(int idx)
        {
            return (FileABB << GetIndexFile(idx));
        }

        /// <summary>
        /// Returns a ulong with bits set along whichever rank <paramref name="idx"/> is on.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetRankBB(int idx)
        {
            return (Rank1BB << (8 * GetIndexRank(idx)));
        }

    }


}
