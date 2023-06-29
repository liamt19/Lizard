using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class MoveScores
    {
        //  https://www.chessprogramming.org/Move_Ordering
        public const int PV_TT_Move = 1000;
        public const int KillerMove = 900;
        public const int WinningCapture = 800;
        public const int EqualCapture = 750;
        public const int DoubleCheck = 700;
        public const int Check = 650;
        public const int KingXRay = 625;
        public const int PassedPawnPush = 600;
        public const int Castle = 500;
        public const int NormalFirst = Normal + 1;
        public const int LosingCapture = Normal + 5;
        public const int Normal = 50;
    }
}
