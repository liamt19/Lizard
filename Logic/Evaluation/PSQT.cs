using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LTChess.Search
{
    public static class PSQT
    {
        //  The distance that these pawns are from promoting
        //  D1 means the pawn can promote on it's next move
        private const int D1 = 170;
        private const int D2 = 100;
        private const int D3 = 60;
        private const int D4 = 30;
        private const int D5 = 5;
        private const int D6 = 2;

        private const int C1 = 30;
        private const int C2 = 20;
        private const int C3 = 10;
        private const int C4 = 0;

        //  Center file bonus
        private const int CF = 30;

        //  Score per controlled square
        private const int CC1 = 20;
        private const int CC2 = 10;
        private const int CC3 = 4;
        private const int CC4 = 0;

        private const int EGK1 = -30;
        private const int EGK2 = -20;
        private const int EGK3 = -10;
        private const int EGK4 = 0;

        public static int[] WhitePawns;

        public static int[] BlackPawns;

        public static int[][] PawnsByColor;

        //  From white's perspective.
        //  Small bonus for fianchetto and for center pawns
        public static int[] _Pawns = new int[]
        {
             0,  0,   0,   0,   0,   0,  0,  0,
            50, 50,  50,  50,  50,  50, 50, 50,
            10, 10,  20,  30,  30,  20, 10, 10,
             5,  5,  10,  25,  25,  10,  5,  5,
             0,  0,   0,  20,  20,   0,  0,  0,
             5, -5, -10,   0,   0, -10, -5,  5,
             5, 10,  10, -20, -20,  10, 10,  5,
             0,  0,   0,   0,   0,   0,  0,  0
        };

        public static int[] PawnCenter = new[]
        {
            C4, C4, C4, C4 + CF, C4 + CF, C4, C4, C4,
            C4, C3, C3, C3 + CF, C3 + CF, C3, C3, C4,
            C4, C3, C2, C2 + CF, C2 + CF, C2, C3, C4,
            C4, C3, C2, C1 + CF, C1 + CF, C2, C3, C4,
            C4, C3, C2, C1 + CF, C1 + CF, C2, C3, C4,
            C4, C3, C2, C2 + CF, C2 + CF, C2, C3, C4,
            C4, C3, C3, C3 + CF, C3 + CF, C3, C3, C4,
            C4, C4, C4, C4 + CF, C4 + CF, C4, C4, C4,
        };

        public static int[] CenterControl = new[]
        {
            CC4, CC4, CC4, CC4, CC4, CC4, CC4, CC4,
            CC4, CC3, CC3, CC3, CC3, CC3, CC3, CC4,
            CC4, CC3, CC2, CC2, CC2, CC2, CC3, CC4,
            CC4, CC3, CC2, CC1, CC1, CC2, CC3, CC4,
            CC4, CC3, CC2, CC1, CC1, CC2, CC3, CC4,
            CC4, CC3, CC2, CC2, CC2, CC2, CC3, CC4,
            CC4, CC3, CC3, CC3, CC3, CC3, CC3, CC4,
            CC4, CC4, CC4, CC4, CC4, CC4, CC4, CC4,
        };

        public static int[] Center = new[]
        {
            C4, C4, C4, C4, C4, C4, C4, C4,
            C4, C3, C3, C3, C3, C3, C3, C4,
            C4, C3, C2, C2, C2, C2, C3, C4,
            C4, C3, C2, C1, C1, C2, C3, C4,
            C4, C3, C2, C1, C1, C2, C3, C4,
            C4, C3, C2, C2, C2, C2, C3, C4,
            C4, C3, C3, C3, C3, C3, C3, C4,
            C4, C4, C4, C4, C4, C4, C4, C4,
        };

        public static int[] EGWeakKingPosition = new[]
        {
            EGK1, EGK1, EGK1, EGK1, EGK1, EGK1, EGK1, EGK1,
            EGK1, EGK2, EGK2, EGK2, EGK2, EGK2, EGK2, EGK1,
            EGK1, EGK2, EGK3, EGK3, EGK3, EGK3, EGK2, EGK1,
            EGK1, EGK2, EGK3, EGK4, EGK4, EGK3, EGK2, EGK1,
            EGK1, EGK2, EGK3, EGK4, EGK4, EGK3, EGK2, EGK1,
            EGK1, EGK2, EGK3, EGK3, EGK3, EGK3, EGK2, EGK1,
            EGK1, EGK2, EGK2, EGK2, EGK2, EGK2, EGK2, EGK1,
            EGK1, EGK1, EGK1, EGK1, EGK1, EGK1, EGK1, EGK1,
        };


        public static int[][][] FishPSQT;

        private static int[][][] FishPSQTKnight =
        {
            new int[][] {
                new int[] { -175, -92, -74, -73, -73, -74, -92, -175, },
                new int[] {  -77, -41, -27, -15, -15, -27, -41,  -77, },
                new int[] {  -61, -17,  6,  12,  12,  6, -17,  -61, },
                new int[] { -35,   8,  40,  49,  49,  40,   8,  -35, },
                new int[] {  -34,  13,  44,  51,  51,  44,  13,  -34, },
                new int[] {   -9,  22,  58,  53,  53,  58,  22,   -9, },
                new int[] { -67, -27,   4,  37,  37,   4, -27,  -67, },
                new int[] { -201, -83, -56, -26, -26, -56, -83, -201, }
            },
            new int[][] {
                new int[] { -96, -65, -49, -21,  -21,  -49, -65,  -96 },
                new int[] { -67, -54, -18,   8,    8,  -18, -54,  -67 },
                new int[] { -40, -27,  -8,  29,   29,   -8, -27,  -40 },
                new int[] { -35,  -2,  13,  28,   28,   13,  -2,  -35 },
                new int[] { -45, -16,   9,  39,   39,    9, -16,  -45 },
                new int[] { -51, -44, -16,  17,   17,  -16, -44,  -51 },
                new int[] { -69, -50, -51,  12,   12,  -51, -50,  -69 },
                new int[] {-100, -88, -56, -17,  -17,  -56, -88, -100 }
            },
        };

        private static int[][][] FishPSQTBishop =
        {
            new int[][] {
                new int[] { -37, -4 ,  -6, -16, -16,  -6, -4 , -37 },
                new int[] { -11,   6,  13,   3,   3,  13,   6, -11 },
                new int[] { -5 ,  15,  -4,  12,  12,  -4,  15, -5  },
                new int[] { -4 ,   8,  18,  27,  27,  18,   8, -4  },
                new int[] { -8 ,  20,  15,  22,  22,  15,  20, -8  },
                new int[] { -11,   4,   1,   8,   8,   1,   4, -11 },
                new int[] { -12, -10,   4,   0,   0,   4, -10, -12 },
                new int[] { -34,   1, -10, -16, -16, -10,   1, -34 }
            },
            new int[][] {
                new int[] { -40, -21, -26,  -8,  -8, -26, -21, -40 },
                new int[] { -26,  -9, -12,   1,   1, -12,  -9, -26 },
                new int[] { -11,  -1,  -1,   7,   7,  -1,  -1, -11 },
                new int[] { -14,  -4,   0,  12,  12,   0,  -4, -14 },
                new int[] { -12,  -1, -10,  11,  11, -10,  -1, -12 },
                new int[] { -21,   4,   3,   4,   4,   3,   4, -21 },
                new int[] { -22, -14,  -1,   1,   1,  -1, -14, -22 },
                new int[] { -32, -29, -26, -17, -17, -26, -29, -32 }
            },
        };

        private static int[][][] FishPSQTRook =
        {
            new int[][] {
                new int[] { -31, -20, -14, -5, -5, -14, -20, -31 },
                new int[] { -21, -13,  -8,  6,  6,  -8, -13, -21 },
                new int[] { -25, -11,  -1,  3,  3,  -1, -11, -25 },
                new int[] { -13,  -5,  -4, -6, -6,  -4,  -5, -13 },
                new int[] { -27, -15,  -4,  3,  3,  -4, -15, -27 },
                new int[] { -22,  -2,   6, 12, 12,   6,  -2, -22 },
                new int[] {  -2,  12,  16, 18, 18,  16,  12,  -2 },
                new int[] { -17, -19,  -1,  9,  9,  -1, -19, -17 }
            },
            new int[][] {
                new int[] {  -9, -13, -10,  -9,  -9, -10, -13,  -9 },
                new int[] { -12,  -9,  -1,  -2,  -2,  -1,  -9, -12 },
                new int[] {   6,  -8,  -2,  -6,  -6,  -2,  -8,   6 },
                new int[] {  -6,   1,  -9,   7,   7,  -9,   1,  -6 },
                new int[] {  -5,   8,   7,  -6,  -6,   7,   8,  -5 },
                new int[] {   6,   1,  -7,  10,  10,  -7,   1,   6 },
                new int[] {   4,   5,  20,  -5,  -5,  20,   5,   4 },
                new int[] {  18,   0,  19,  13,  13,  19,   0,  18 }
            },
        };

        private static int[][][] FishPSQTQueen =
        {
            new int[][] {
                new int[] {  3, -5, -5,  4,  4, -5, -5,  3 },
                new int[] { -3,  5,  8, 12, 12,  8,  5, -3 },
                new int[] { -3,  6, 13,  7,  7, 13,  6, -3 },
                new int[] {  4,  5,  9,  8,  8,  9,  5,  4 },
                new int[] {  0, 14, 12,  5,  5, 12, 14,  0 },
                new int[] { -4, 10,  6,  8,  8,  6, 10, -4 },
                new int[] { -5,  6, 10,  8,  8, 10,  6, -5 },
                new int[] { -2, -2,  1, -2, -2,  1, -2, -2 }
            },
            new int[][] {
                new int[] { -69, -57, -47, -26, -26, -47, -57, -69 },
                new int[] { -54, -31, -22,  -4,  -4, -22, -31, -54 },
                new int[] { -39, -18,  -9,   3,   3,  -9, -18, -39 },
                new int[] { -23,  -3,  13,  24,  24,  13,  -3, -23 },
                new int[] { -29,  -6,   9,  21,  21,   9,  -6, -29 },
                new int[] { -38, -18, -11,   1,   1, -11, -18, -38 },
                new int[] { -50, -27, -24,  -8,  -8, -24, -27, -50 },
                new int[] { -74, -52, -43, -34, -34, -43, -52, -74 }
            },
        };

        private static int[][][] FishPSQTKing =
        {
            new int[][] {
                new int[] { 271, 327, 271, 198, 198, 271, 327, 271 },
                new int[] { 278, 303, 234, 179, 179, 234, 303, 278 },
                new int[] { 195, 258, 169, 120, 120, 169, 258, 195 },
                new int[] { 164, 190, 138,  98,  98, 138, 190, 164 },
                new int[] { 154, 179, 105,  70,  70, 105, 179, 154 },
                new int[] { 123, 145,  81,  31,  31,  81, 145, 123 },
                new int[] {  88, 120,  65,  33,  33,  65, 120,  88 },
                new int[] {  59,  89,  45,  -1,  -1,  45,  89, 59 }
            },
            new int[][] {
                new int[] {   1,  45,  85,  76,  76,  85,  45,   1 },
                new int[] {  53, 100, 133, 135, 135, 133, 100,  53 },
                new int[] {  88, 130, 169, 175, 175, 169, 130,  88 },
                new int[] { 103, 156, 172, 172, 172, 172, 156, 103 },
                new int[] {  96, 166, 199, 199, 199, 199, 166,  96 },
                new int[] {  92, 172, 184, 191, 191, 184, 172,  92 },
                new int[] {  47, 121, 116, 131, 131, 116, 121,  47 },
                new int[] {  11,  59,  73,  78,  78,  73,  59,  11 }
            },

        };


        private static bool Initialized = false;
        static PSQT()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            WhitePawns = _Pawns.Reverse().ToArray();
            BlackPawns = _Pawns;
            Initialized = true;

            PawnsByColor = new int[2][];

            PawnsByColor[Color.White] = WhitePawns;
            PawnsByColor[Color.Black] = _Pawns;

            MakeFishPSQT();
        }

        private static void MakeFishPSQT()
        {
            FishPSQT = new int[2][][];
            FishPSQT[Color.White] = new int[NumPieces][];
            FishPSQT[Color.Black] = new int[NumPieces][];

            for (int pt = Piece.Knight; pt <= Piece.King ; pt++)
            {
                FishPSQT[Color.White][pt] = new int[64];
                FishPSQT[Color.Black][pt] = new int[64];

                int[][][] arr = (pt == Piece.Knight ? FishPSQTKnight :
                                (pt == Piece.Bishop ? FishPSQTBishop :
                                (pt == Piece.Rook   ? FishPSQTRook   :
                                (pt == Piece.Queen  ? FishPSQTQueen  :
                                                      FishPSQTKing ))));


                for (int i = 0; i < 64; i++)
                {
                    FishPSQT[Color.White][pt][i] = arr[EvaluationConstants.GamePhaseNormal][GetIndexRank(i)][GetIndexFile(i)];
                    FishPSQT[Color.Black][pt][i] = arr[EvaluationConstants.GamePhaseNormal][IndexTop - GetIndexRank(i)][GetIndexFile(i)];
                }
            }
        }


    }
}
