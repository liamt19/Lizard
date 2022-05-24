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
        public const int D1 = 80;
        public const int D2 = 40;
        public const int D3 = 20;
        public const int D4 = 10;
        public const int D5 = 5;
        public const int D6 = 2;

        public const int C1 = 100;
        public const int C2 = 60;
        public const int C3 = 30;
        public const int C4 = 0;

        public static int[] WhitePawns;

        public static int[] BlackPawns;

        //  From white's perspective.
        //  Small bonus for fianchetto and for center pawns
        private static int[] Pawns = new int[]
        {
            0,  0,  0,  0,  0,  0,  0,  0,
            D1, D1, D1, D1, D1, D1, D1, D1,
            D2, D2, D2, D2, D2, D2, D2, D2,
            D3, D3, D3, D3, D3, D3, D3, D3,
            D4, D4, D4, D3, D3, D4, D4, D4,
            D4, D3, D5, D4, D4, D5, D3, D4,
            D6, D6, D6, D6, D6, D6, D6, D6,
            0,  0,  0,  0,  0,  0,  0,  0,
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
            WhitePawns = Pawns.Reverse().ToArray();
            BlackPawns = Pawns;
            Initialized = true;
        }


    }
}
