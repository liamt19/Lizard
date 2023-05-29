using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

namespace LTChess.Transposition
{
    public class EvaluationTable
    {
        public const int DefaultEvaluationTableSizeMB = 16;

        public const ulong InvalidKey = 0UL;

        private static ETEntry[] Table;
        private static ulong Size;

        /// <summary>
        /// 8mb is enough for 2097152 entries
        /// </summary>
        public static unsafe void Initialize(int mb = DefaultEvaluationTableSizeMB)
        {
            //  1024 * 1024 = 1048576 == 0x100000UL
            Size = ((ulong)mb * 0x100000UL) / (ulong)sizeof(ETEntry);
            Table = new ETEntry[Size];
        }

        [MethodImpl(Inline)]
        public static void Save(ulong hash, short score)
        {
#if DEBUG
            SearchStatistics.ETSaves++;
            if (Table[hash % Size].key != 0 && Table[hash % Size].score != 0)
            {
                SearchStatistics.ETReplacements++;
            }
#endif
            Table[hash % Size] = new ETEntry(hash, score);
        }

        [MethodImpl(Inline)]
        public static ETEntry Probe(ulong hash)
        {
            return Table[hash % Size];
        }

        public static void PrintStatus()
        {
            int entries = 0;
            int keylessScores = 0;

            for (int i = 0; i < Table.Length; i++)
            {
                var item = Table[i];
                if (item.key != 0)
                {
                    entries++;
                }
                else if (item.score != 0)
                {
                    keylessScores++;
                }
            }
            double percent = (double)entries / Size;

            Log("ET:\t" + entries + " / " + Size + " = " + (percent * 100) + "%");
            if (keylessScores != 0)
            {
                Log(keylessScores + " keyless scores??");
            }
        }

    }


}
