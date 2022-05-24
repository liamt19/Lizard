using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Transposition
{
    public struct ETEntry
    {
        public ushort key;
        public const int KEYSHIFT = 48;
        public short score;


        public ETEntry(ulong hash, short score)
        {
            this.key = (ushort)(hash >> KEYSHIFT);
            this.score = score;
        }

        public bool Validate(ulong hash)
        {
            return key == (ushort)(hash >> KEYSHIFT);
        }
    }
}
