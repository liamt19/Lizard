using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lizard.Logic.NN
{
    public unsafe struct BucketCache
    {
        /// <summary>
        /// 2 Boards, 1 for each perspective
        /// </summary>
        public Bitboard[] Boards = new Bitboard[ColorNB];
        public Accumulator Accumulator = new Accumulator();

        public BucketCache() { }
    }
}
