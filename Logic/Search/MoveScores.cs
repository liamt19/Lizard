using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class MoveScores
    {
        //  Holy hell!
        public const ushort EnPassant = 1660;

        public const ushort PVMove = 10;
        public const ushort TTHit = 9;
        public const ushort WinningCapture = 8;
        public const ushort EqualCapture = 7;
        public const ushort KillerMove = 6;
        public const ushort Check = 5;
        public const ushort Castle = 4;
        public const ushort Normal = 3;
        public const ushort LosingCapture = 2;
    }
}
