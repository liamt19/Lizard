using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Transposition
{
    public struct ETEntry
    {
        public const short InvalidScore = short.MaxValue - 3;
        public const int KeyShift = 48;

        public ushort Key;
        public short Score = InvalidScore;


        public ETEntry(ulong hash, short score)
        {
            this.Key = (ushort)(hash >> KeyShift);
            this.Score = score;
        }

        [MethodImpl(Inline)]
        public bool Validate(ulong hash)
        {
            return Key == (ushort)(hash >> KeyShift);
        }
    }
}
