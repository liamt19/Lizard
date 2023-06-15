using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LTChess.Data;
using static LTChess.Data.PrecomputedData;
using static LTChess.Data.RunOptions;
using static LTChess.Data.Squares;
using LTChess.Transposition;
using System.Runtime.CompilerServices;

namespace LTChess.Util
{
    public unsafe class HashPerft
    {
        const int HashMinDepth = 2;

        private Position p;
        private HashPerftNode[] Table;
        private ulong Size = 20;

        private int originalDepth;
        private int HashMaxDepth;

        public ulong TableHits = 0;
        public ulong TableMisses = 0;
        public ulong TableSaves = 0;

        public HashPerft(Position p, int mb, int depth)
        {
            this.p = p;
            Size = ((ulong)mb * 0x100000UL) / (ulong)sizeof(HashPerftNode);
            Table = new HashPerftNode[Size];
            Log("Table size is " + Size);

            originalDepth = depth;

            // For transpositions, this should be at least 2 less than the original depth
            HashMaxDepth = originalDepth - 2;
        }

        public List<PerftNode> PerftDivide(int depth)
        {
            List<PerftNode> list = new List<PerftNode>();
            if (depth <= 0)
            {
                return list;
            }

            Span<Move> mlist = stackalloc Move[NormalListCapacity];
            int size = p.GenAllLegalMoves(mlist);
            for (int i = 0; i < size; i++)
            {
                PerftNode pn = new PerftNode();
                pn.root = mlist[i].ToString();
                p.MakeMove(mlist[i]);
                pn.number = Perft(depth - 1);
                p.UnmakeMove();
                list.Add(pn);

                Console.Title = "Progress: " + (i + 1) + " / " + size + " branches";
            }

            return list;
        }

        [MethodImpl(Inline)]
        public ulong Perft(int depth)
        {
            ulong n = 0;
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = p.GenAllLegalMoves(list);

            if (depth == 1)
            {
                return (ulong)size;
            }
            if (depth == 0)
            {
                return 1;
            }

            for (int i = 0; i < size; i++)
            {
                p.MakeMove(list[i]);

                if (depth <= HashMaxDepth && depth >= HashMinDepth)
                {
                    ulong hash = p.Hash;
                    HashPerftNode probe = Table[hash % Size];
                    if (probe.hash == hash && probe.depth == depth)
                    {
                        n += probe.number;
                        TableHits++;
                    }
                    else
                    {
                        ulong num = Perft(depth - 1);
                        if (probe.hash == 0UL)
                        {
                            Table[hash % Size] = new HashPerftNode(hash, num, depth);
                            TableSaves++;
                        }
                        n += num;

                        TableMisses++;
                    }
                }
                else
                {
                    // Don't bother probing or saving.
                    n += Perft(depth - 1);
                }
                
                p.UnmakeMove();
            }

            return n;
        }

        struct HashPerftNode
        {
            public ulong hash;
            public ulong number;
            public int depth;

            public HashPerftNode(ulong hash, ulong number, int depth)
            {
                this.hash = hash;
                this.number = number;
                this.depth = depth;
            }
        }
    }

    
}
