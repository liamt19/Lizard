using System.Diagnostics;

namespace LTChess.Transposition
{
    public static class TranspositionTable
    {
        private static TTEntry[] Table;
        private static ulong Size;

        private static bool Initialized = false;

        static TranspositionTable()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 1 mb is enough for 65,536 entries. 
        /// </summary>
        public static unsafe void Initialize(int mb = 16)
        {
            var entrySize = sizeof(TTEntry);

            Size = ((ulong)mb * 0x100000UL) / (ulong)entrySize;
            Table = new TTEntry[Size];
        }

        [MethodImpl(Inline)]
        public static void Clear() => Array.Clear(Table);

        [MethodImpl(Inline)]
        public static void Save(ulong key, short eval, TTNodeType nodeType, int depth, Move m)
        {
            Table[key % Size] = new TTEntry(key, eval, nodeType, depth, m);
        }

        [MethodImpl(Inline)]
        public static TTEntry Probe(ulong hash)
        {
            return Table[hash % Size];
        }

        [MethodImpl(Inline)]
        public static int GetHashFull()
        {
            int entries = 0;
            Debug.Assert(Size > 1000, "Hash is undersized!");

            for (int i = 0; i < 1000; i++)
            {
                if (Table[i].NodeType != TTNodeType.Invalid)
                {
                    entries++;
                }
            }

            return entries;
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

                if (tt.NodeType == TTNodeType.Beta)
                {
                    Beta++;
                }
                else if (tt.NodeType == TTNodeType.Alpha)
                {
                    Alpha++;
                }
                else if (tt.NodeType == TTNodeType.Exact)
                {
                    Exact++;
                }
                else
                {
                    Invalid++;
                }

                if (tt.BestMove.IsNull() && tt.NodeType != TTNodeType.Invalid)
                {
                    NullMoves++;
                }
            }

            int entries = Beta + Alpha + Exact;
            double percent = (double)entries / Size;
            Log("TT:\t " + entries + " / " + Size + " = " + (percent * 100) + "%");
            Log("Alpha:\t " + Alpha);
            Log("Beta:\t " + Beta);
            Log("Exact:\t " + Exact);
            Log("Invalid: " + Invalid);
            Log("Null:\t " + NullMoves);
        }
    }
}
