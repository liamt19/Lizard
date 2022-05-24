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

namespace LTChess.Util
{
    public static class Utilities
    {
        public const int TOP = 7;
        public const int BOT = 0;
        public const int LEFT = 0;
        public const int RIGHT = 7;
        public const string InitialFEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        //public static Move NULLMOVE = new Move(64, 64);

        public const int MAX_CAPACITY = 512;
        public const int NORMAL_CAPACITY = 128;
        public const int LSB_EMPTY = 64;
        public const int MAX_DEPTH = 64;

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

        public static ulong[] KingFlank = new ulong[]{
            QueenSide ^ FileDBB,
            QueenSide,
            QueenSide,
            CenterFiles,
            CenterFiles,
            KingSide,
            KingSide,
            KingSide ^ FileEBB
        };

        public const ulong WhiteKingsideMask = (1UL << F1) | (1UL << G1);
        public const ulong WhiteQueensideMask = (1UL << B1) | (1UL << C1) | (1UL << D1);
        public const ulong BlackKingsideMask = (1UL << F8) | (1UL << G8);
        public const ulong BlackQueensideMask = (1UL << B8) | (1UL << C8) | (1UL << D8);


        public static void LogFile(string s, string fileName)
        {
            File.AppendAllText(fileName, s);
        }

        public static void Log(string s)
        {
            Console.WriteLine(s);
            Debug.WriteLine(s);
        }
        public static void LogW(string s)
        {
            Console.WriteLine("Warn: " + s);
            Debug.WriteLine("Warn: " + s);
        }

        public static void LogE(string s)
        {
            Console.WriteLine("Error: " + s);
            Debug.WriteLine("Error: " + s);
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

            return "EMPTY";
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

            LogE("Failed parsing FEN character '" + c + "'");
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

        public static string IndexToString(int idx)
        {
            return "" + GetFileChar(GetIndexFile(idx)) + (GetIndexRank(idx) + 1);
        }

        public static string CoordToString(int x, int y)
        {
            return "" + GetFileChar(x) + (y + 1);
        }

        public static int StringToIndex(string s)
        {
            return CoordToIndex(GetFileInt(s[0]), int.Parse(s[1].ToString()) - 1);
        }

        public static string UlongToString(ulong b)
        {
            StringBuilder sb = new StringBuilder();

            while (b != 0)
            {
                int idx = lsb(b);
                sb.Append(IndexToString(idx) + ", ");
                b = poplsb(b);
            }

            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

        [MethodImpl(Inline)]
        public static bool InBounds(int x, int y)
        {
            return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
        }

        /// <summary>
        /// returns <c>Convert.ToString((long)u, 2)</c>
        /// </summary>
        public static string InBinary(ulong u) => Convert.ToString((long)u, 2);

        public static string InBinaryFull(ulong bb)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Convert.ToString((long)bb, 2));
            if (sb.Length != 64)
            {
                sb.Insert(0, "0", 64 - sb.Length);
            }

            return sb.ToString();
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

        public static void SortByCapture(this Span<Move> arr)
        {
            int sortIndex = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].IsNull())
                {
                    return;
                }

                if (arr[i].Capture)
                {
                    Move temp = arr[sortIndex];
                    arr[sortIndex] = arr[i];
                    arr[i] = temp;

                    sortIndex++;
                }
            }
        }

        public static void SortByCheck(this Span<Move> arr)
        {
            int sortIndex = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].IsNull())
                {
                    return;
                }

                if (arr[i].CausesCheck || arr[i].CausesDoubleCheck)
                {
                    Move temp = arr[sortIndex];
                    arr[sortIndex] = arr[i];
                    arr[i] = temp;

                    sortIndex++;
                }
            }
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
        public static string centeredString(string s, int width)
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

        public static string FormatSearchInformation(SearchInformation info)
        {
            int depth = info.MaxDepth;
            int selDepth = depth;
            double time = Math.Round(info.SearchTime);
            var score = FormatMoveScore(info.BestScore);
            double nodes = info.NodeCount;
            int nodesPerSec = ((int)(nodes / (time / 1000)));
            string pv = "";
            NegaMax.GetPV(info, info.PV, 0);
            for (int i = 0; i < MAX_DEPTH; i++)
            {
                if (info.PV[i].IsNull())
                {
                    break;
                }

                pv += info.PV[i].ToString() + " ";
            }

            StringBuilder sb = new StringBuilder();

            sb.Append("info depth " + depth);
            sb.Append(" seldepth " + selDepth);
            sb.Append(" time " + time);
            sb.Append(" score " + score);
            sb.Append(" nodes " + nodes);
            sb.Append(" nps " + nodesPerSec);
            sb.Append(" pv " + pv);

            return sb.ToString();
        }

        public static string FormatMoveScore(int score)
        {
            if (Evaluation.IsScoreMate(score, out int mateIn))
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
            return centeredString(string.Format("{0:N2}", InCentipawns(normalScore)), sz);
        }

        //  https://stackoverflow.com/questions/8946808/can-console-clear-be-used-to-only-clear-a-line-instead-of-whole-console
        public static void ClearCurrentConsoleLine()
        {
            //Console.SetCursorPosition(0, Console.CursorTop - 1);
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        //  https://stackoverflow.com/questions/15529672/generating-an-indented-string-for-a-single-line-of-text
        public static string Indent(this string value, int tabs)
        {
            var strArray = value.Split('\n');
            var sb = new StringBuilder();
            foreach (var s in strArray)
                sb.Append(new string(' ', (tabs * 4))).Append(s);
            return sb.ToString();
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
        /// Returns the number of bits set in <paramref name="value"/> using <c>Popcnt.X64.PopCount</c>
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

        /// <summary>
        /// I would like to use it like this to avoid having to do some thing like "int idx = lsb(value);    value = poplsb(value);"
        /// But this is slower, I think because of how C# doesn't really like pointers. 
        /// And this was faster than using "ref ulong value" by about 10 ms in a benchmark, but still about 2-3 ms slower than ^^^^ that.
        /// </summary>
        [MethodImpl(Inline)]
        public static unsafe int _poplsbFull(ulong* value)
        {
            int idx = (int) Bmi1.X64.TrailingZeroCount(*value);
            //*value &= *value - 1;
            *value = Bmi1.X64.ResetLowestSetBit(*value);
            return idx;
        }

        [MethodImpl(Inline)]
        public static unsafe int _poplsbRef(ref ulong value)
        {
            int idx = (int)Bmi1.X64.TrailingZeroCount(value);
            value = Bmi1.X64.ResetLowestSetBit(value);
            return idx;
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
#if BMI
            return value ^ (1UL << msb(value));
#endif
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
        /// Returns a mask containing every square the piece on <paramref name="square"/> can move to on the board <paramref name="occupied"/>.
        /// Excludes all edges of the board unless the piece is on that edge. So a rook on A1 has every bit along the A file and 1st rank set,
        /// except for A8 and H1.
        /// </summary>
        public static ulong GetBlockerMask(Fish.PieceType pt, int square, ulong occupied)
        {
            ulong mask = Fish.sliding_attack(pt, (Fish.Square)square, occupied);

            int rank = (square >> 3);
            int file = (square & 7);
            if (rank == 7)
            {
                mask &= ~Rank1BB;
            }
            else if (rank == 0)
            {
                mask &= ~Rank8BB;
            }
            else
            {
                mask &= (~Rank1BB & ~Rank8BB);
            }

            if (file == 0)
            {
                mask &= ~FileHBB;
            }
            else if (file == 7)
            {
                mask &= ~FileABB;
            }
            else
            {
                mask &= (~FileHBB & ~FileABB);
            }

            return mask;
        }

        /// <summary>
        /// Returns a ulong with bits set along whichever file <paramref name="idx"/> is in.
        /// </summary>
        public static ulong GetFileBB(int idx)
        {
            return (FileABB << GetIndexFile(idx));
        }

        /// <summary>
        /// Returns a ulong with bits set along whichever rank <paramref name="idx"/> is on.
        /// </summary>
        public static ulong GetRankBB(int idx)
        {
            return (Rank1BB << (8 * GetIndexRank(idx)));
        }

    }


}
