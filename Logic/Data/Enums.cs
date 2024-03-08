namespace Lizard.Logic.Data
{
    public static class Diagonal
    {
        /// <summary>
        /// Diagonally up and to the right, from white's perspective. 
        /// The longest diagonal in this direction contains A1, B2, ... G7, H8.
        /// </summary>
        public const int Diagonal_A1H8 = 0;

        /// <summary>
        /// Diagonally down and to the right, from white's perspective. 
        /// The longest diagonal in this direction contains A8, B7, ... G2, H1.
        /// </summary>
        public const int Diagonal_A8H1 = 1;
    }

    public static class Color
    {
        public const int White = 0;
        public const int Black = 1;

        public const int ColorNB = 2;
    }

    public static class Piece
    {
        public const int Pawn = 0;
        public const int Knight = 1;
        public const int Bishop = 2;
        public const int Rook = 3;
        public const int Queen = 4;
        public const int King = 5;

        public const int None = 6;
        public const int PieceNB = 6;
    }

    [Flags]
    public enum CastlingStatus
    {
        None = 0,
        WK = 1,
        WQ = 2,
        BK = 4,
        BQ = 8,

        White = WK | WQ,
        Black = BK | BQ,

        Kingside = WK | BK,
        Queenside = WQ | BQ,

        All = WK | WQ | BK | BQ,
    }

    public enum TTNodeType
    {
        Invalid,
        /// <summary>
        /// Upper Bound
        /// </summary>
        Beta,
        /// <summary>
        /// Lower bound
        /// </summary>
        Alpha,
        Exact = Beta | Alpha
    };

    public static class Bound
    {
        public const int BoundNone = (int)TTNodeType.Invalid;
        public const int BoundUpper = (int)TTNodeType.Beta;
        public const int BoundLower = (int)TTNodeType.Alpha;
        public const int BoundExact = (int)TTNodeType.Exact;
    }

    public interface SearchNodeType { }
    public struct PVNode : SearchNodeType { }
    public struct NonPVNode : SearchNodeType { }
    public struct RootNode : SearchNodeType { }


    public static class Files
    {
        public const int A = 0;
        public const int B = 1;
        public const int C = 2;
        public const int D = 3;
        public const int E = 4;
        public const int F = 5;
        public const int G = 6;
        public const int H = 7;
    }

    public static class Squares
    {
        public const int A1 = 0;
        public const int B1 = 1;
        public const int C1 = 2;
        public const int D1 = 3;
        public const int E1 = 4;
        public const int F1 = 5;
        public const int G1 = 6;
        public const int H1 = 7;
        public const int A2 = 8;
        public const int B2 = 9;
        public const int C2 = 10;
        public const int D2 = 11;
        public const int E2 = 12;
        public const int F2 = 13;
        public const int G2 = 14;
        public const int H2 = 15;
        public const int A3 = 16;
        public const int B3 = 17;
        public const int C3 = 18;
        public const int D3 = 19;
        public const int E3 = 20;
        public const int F3 = 21;
        public const int G3 = 22;
        public const int H3 = 23;
        public const int A4 = 24;
        public const int B4 = 25;
        public const int C4 = 26;
        public const int D4 = 27;
        public const int E4 = 28;
        public const int F4 = 29;
        public const int G4 = 30;
        public const int H4 = 31;
        public const int A5 = 32;
        public const int B5 = 33;
        public const int C5 = 34;
        public const int D5 = 35;
        public const int E5 = 36;
        public const int F5 = 37;
        public const int G5 = 38;
        public const int H5 = 39;
        public const int A6 = 40;
        public const int B6 = 41;
        public const int C6 = 42;
        public const int D6 = 43;
        public const int E6 = 44;
        public const int F6 = 45;
        public const int G6 = 46;
        public const int H6 = 47;
        public const int A7 = 48;
        public const int B7 = 49;
        public const int C7 = 50;
        public const int D7 = 51;
        public const int E7 = 52;
        public const int F7 = 53;
        public const int G7 = 54;
        public const int H7 = 55;
        public const int A8 = 56;
        public const int B8 = 57;
        public const int C8 = 58;
        public const int D8 = 59;
        public const int E8 = 60;
        public const int F8 = 61;
        public const int G8 = 62;
        public const int H8 = 63;

        public const int SquareNB = 64;
        public const int EPNone = 0;
    }
}