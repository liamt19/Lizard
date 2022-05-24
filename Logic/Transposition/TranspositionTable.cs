using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LTChess.Transposition
{
    public class TranspositionTable
    {
        private static TTEntry[] Table;
        private static ulong Size;

        /// <summary>
        /// 16 mb is enough for 381300 entries
        /// </summary>
        public static unsafe void Initialize(int mb = 16)
        {
            var entrySize = sizeof(TTEntry);

            Size = ((ulong)mb * 0x100000UL) / (ulong)entrySize;
            Table = new TTEntry[Size];
        }

        [MethodImpl(Inline)]
        public static void Save(ulong key, short eval, NodeType nodeType, int depth, Move m)
        {
#if DEBUG
            SearchStatistics.TTSaves++;
            if (!Table[key % Size].BestMove.IsNull() || Table[key % Size].Key != 0)
            {
                SearchStatistics.TTReplacements++;
            }
            else if (Table[key % Size].Key == TTEntry.MakeKey(key))
            {
                if (Table[key % Size].Depth > depth)
                {
                    SearchStatistics.TTDepthIncreased++;
                }
                else
                {
                    SearchStatistics.TTReplacementOther++;
                }
            }
#endif
            Table[key % Size] = new TTEntry(key, eval, nodeType, depth, m);
        }

        [MethodImpl(Inline)]
        public static TTEntry Probe(ulong hash)
        {
            return Table[hash % Size];
        }

        public static void PrintStatus()
        {
            int Beta = 0;
            int Alpha = 0;
            int Exact = 0;
            int Invalid = 0;

            int NullMoves = 0;

            for (int i = 0; i < Table.Length; i++)
            {
                var tt = Table[i];
                if (!tt.BestMove.IsNull())
                {
                    if (tt.NodeType == NodeType.Beta)
                    {
                        Beta++;
                    }
                    else if (tt.NodeType == NodeType.Alpha)
                    {
                        Alpha++;
                    }
                    else if (tt.NodeType == NodeType.Exact)
                    {
                        Exact++;
                    }
                    else
                    {
                        Invalid++;
                    }
                }
                else if (tt.Key != 0)
                {
                    NullMoves++;
                }
            }

            int entries = Invalid + Beta + Alpha + Exact + NullMoves;
            double percent = (double)entries / Size;
            Log("TT:\t" + entries + " / " + Size + " = " + (percent * 100) + "%");
            Log("Alpha:\t" + Alpha);
            Log("Beta:\t" + Beta);
            Log("Exact:\t" + Exact);
            Log("Null:\t" + NullMoves);
        }
    }
}
