using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Magic
{
    /// <summary>
    /// Contains information for one square of the magic bitboard for rooks or bishops
    /// </summary>
    public struct MagicSquare
    {
        public ulong mask;
        public ulong number;
        public ulong[] attacks;
        public int shift;
    }
}
