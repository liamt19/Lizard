using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using static LTChess.Data.RunOptions;
using LTChess.Data;
using static LTChess.Data.Squares;
using LTChess.Search;
using LTChess.Core;

namespace LTChess.Util
{
    public static class Utilities
    {
        public const int NumPieces = 6;

        public const int IndexTop = 7;
        public const int IndexBot = 0;
        public const int IndexLeft = 0;
        public const int IndexRight = 7;
        public const string InitialFEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public const string EngineBuildVersion = "6.1";
        public const string EngineTagLine = "Now rated 1700 on Lichess (so long as my internet stops making it lose games on time)!";

        public const int MaxListCapacity = 512;
        public const int NormalListCapacity = 128;
        public const int LSBEmpty = 64;
        public const int MaxDepth = 64;

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

        public const ulong WhiteKingsideMask = (1UL << F1) | (1UL << G1);
        public const ulong WhiteQueensideMask = (1UL << B1) | (1UL << C1) | (1UL << D1);
        public const ulong BlackKingsideMask = (1UL << F8) | (1UL << G8);
        public const ulong BlackQueensideMask = (1UL << B8) | (1UL << C8) | (1UL << D8);

        /// <summary>
        /// A mask of the ranks that outpost squares can be on for each color
        /// </summary>
        public static readonly ulong[] OutpostSquares = { (Rank4BB | Rank5BB | Rank6BB), (Rank3BB | Rank4BB | Rank5BB) };

        /// <summary>
        /// This is set to true when the program receives the "uci" command, 
        /// and causes calls to the Log method to write to a file rather than stdout.
        /// </summary>
        public static bool UCIMode = false;

        public static long debug_time_off = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        public static void Log(string s)
        {
            if (!UCIMode)
            {
                Console.WriteLine(s);
            }
            else
            {
                LogString("[LOG]: " + s);
            }

            Debug.WriteLine(s);
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
        public static int Up(int color) => (color == Color.White) ? Direction.NORTH : Direction.SOUTH;

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

        [MethodImpl(Inline)]
        public static ulong NextUlong(this Random random)
        {
            Span<byte> arr = new byte[8];
            random.NextBytes(arr);

            return BitConverter.ToUInt64(arr);
        }

        [MethodImpl(Inline)]
        public static char GetFileChar(int i) => (char)(97 + i);

        [MethodImpl(Inline)]
        public static int GetFileInt(char c) => c - 97;

        [MethodImpl(Inline)]
        public static char PieceToFENChar(int n)
        {
            switch (n)
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

        [MethodImpl(Inline)]
        public static int FENToPiece(char c)
        {
            c = char.ToLower(c);
            if (c == 'p')
            {
                return Piece.Pawn;
            }
            if (c == 'n')
            {
                return Piece.Knight;
            }
            if (c == 'b')
            {
                return Piece.Bishop;
            }
            if (c == 'r')
            {
                return Piece.Rook;
            }
            if (c == 'q')
            {
                return Piece.Queen;
            }
            if (c == 'k')
            {
                return Piece.King;
            }

            Log("ERROR Failed parsing FEN character '" + c + "'");
            return Piece.None;
        }

        public static int FENToColor(char c) => char.IsUpper(c) ? Color.White : Color.Black;

        public static int IndexOf(this int[] arr, int value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }


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

        [MethodImpl(Inline)]
        public static void IndexToCoord(in int index, out int x, out int y)
        {
            x = index % 8;
            y = index / 8;
        }

        [MethodImpl(Inline)]
        public static int CoordToIndex(int x, int y)
        {
            return (y * 8) + x;
        }

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

        [MethodImpl(Inline)]
        public static int StringToIndex(string s)
        {
            return CoordToIndex(GetFileInt(s[0]), int.Parse(s[1].ToString()) - 1);
        }

        [MethodImpl(Inline)]
        public static bool InBounds(int x, int y)
        {
            return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
        }

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

        public static string Flip(this string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }


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

        public static string Stringify(this Span<Move> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Length; i++)
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

        public static string Stringify(this Span<Move> list, Position position)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Length; i++)
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
        public static string FormatSearchInformation(SearchInformation info)
        {
            int depth = info.MaxDepth;
            int selDepth = depth;
            double time = Math.Max(1, Math.Round(info.SearchTime));
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
            sb.Append(" pv " + info.GetPVString(true));

            return sb.ToString();
        }

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

        public static string FormatEvalTerm(double normalScore, int sz = 8)
        {
            return CenteredString(string.Format("{0:N2}", InCentipawns(normalScore)), sz);
        }


        //http://graphics.stanford.edu/%7Eseander/bithacks.html
        //https://sharplab.io/
        private static int[] BitScanValues = {
        0,  1,  48,  2, 57, 49, 28,  3,
        61, 58, 50, 42, 38, 29, 17,  4,
        62, 55, 59, 36, 53, 51, 43, 22,
        45, 39, 33, 30, 24, 18, 12,  5,
        63, 47, 56, 27, 60, 41, 37, 16,
        54, 35, 52, 21, 44, 32, 23, 11,
        46, 26, 40, 15, 34, 20, 31, 10,
        25, 14, 19,  9, 13,  8,  7,  6
        };


        /// <summary>
        /// Returns the number of bits set in <paramref name="value"/> using <c>Popcnt.X64.PopCount(<paramref name="value"/>)</c>
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong popcount(ulong value)
        {
#if BMI
            return Popcnt.X64.PopCount(value);
#else
            var count = 0ul;
            while (value > 0)
            {
                value = poplsb(value);
                count++;
            }

            return count;
#endif
        }

        /// <summary>
        /// Returns the number of trailing least significant zero bits in <paramref name="value"/> using <c>Bmi1.X64.TrailingZeroCount</c>. 
        /// So lsb(100_2) returns 2.
        /// </summary>
        [MethodImpl(Inline)]
        public static int lsb(ulong value)
        {
#if BMI
            return (int)Bmi1.X64.TrailingZeroCount(value);
#else
            return BitScanValues[((ulong)((long)value & -(long)value) * 0x03F79D71B4CB0A89) >> 58];
#endif
        }

        /// <summary>
        /// Sets the least significant bit to 0 using <c>Bmi1.X64.ResetLowestSetBit</c>. 
        /// So PopLsb(10110_2) returns 10100_2.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong poplsb(ulong value)
        {
#if BMI
            return Bmi1.X64.ResetLowestSetBit(value);
#else
            return value & (value - 1);
#endif
        }

        [MethodImpl(Inline)]
        public static int msb(ulong value)
        {
#if BMI
            return (int)(63 - Lzcnt.X64.LeadingZeroCount(value));
#else
            return (BitOperations.Log2(x - 1) + 1);
#endif
        }

        [MethodImpl(Inline)]
        public static ulong popmsb(ulong value)
        {
            return value ^ (1UL << msb(value));
        }

        [MethodImpl(Inline)]
        public static ulong pext(ulong b, ulong mask)
        {
#if PEXT
            return Bmi2.X64.ParallelBitExtract(b, mask);
#else
            ulong res = 0;
            for (ulong bb = 1; mask != 0; bb += bb)
            {
                if ((b & mask & (0UL-mask)) != 0)
                {
                    res |= bb;
                }
                mask &= mask - 1;
            }
            return res;
#endif
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
