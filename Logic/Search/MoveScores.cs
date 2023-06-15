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
        public const ushort PV_TT_Move = 100;
        public const ushort WinningCapture = 90;
        public const ushort EqualCapture = 80;
        public const ushort KillerMove = 70;
        public const ushort DoubleCheck = 65;
        public const ushort Check = 60;
        public const ushort Castle = 50;
        public const ushort LosingCapture = 45;
        public const ushort Normal = 40;
    }
}
