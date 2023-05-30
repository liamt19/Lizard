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
        private const int D1 = 80;
        private const int D2 = 40;
        private const int D3 = 20;
        private const int D4 = 10;
        private const int D5 = 5;
        private const int D6 = 2;

        private const int C1 = 100;
        private const int C2 = 60;
        private const int C3 = 30;
        private const int C4 = 0;

        private const int EGK1 = -100;
        private const int EGK2 = -50;
        private const int EGK3 = -10;
        private const int EGK4 = 0;

        public static int[] WhitePawns;

        public static int[] BlackPawns;

        //  From white's perspective.
        //  Small bonus for fianchetto and for center pawns
        public static int[] Pawns = new int[]
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
