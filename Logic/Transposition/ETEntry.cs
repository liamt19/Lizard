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

        public ushort key;
        public short score = InvalidScore;


        public ETEntry(ulong hash, short score)
        {
            this.key = (ushort)(hash >> KeyShift);
            this.score = score;
        }

        [MethodImpl(Inline)]
        public bool Validate(ulong hash)
        {
            return key == (ushort)(hash >> KeyShift);
        }
    }
}
