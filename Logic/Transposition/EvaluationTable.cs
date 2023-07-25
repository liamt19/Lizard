namespace LTChess.Transposition
{
    public class EvaluationTable
    {
        public const int DefaultEvaluationTableSizeMB = 32;

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
            if (Table[hash % Size].Key != 0 && Table[hash % Size].Score != 0)
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

        [MethodImpl(Inline)]
        public static int ProbeOrEval(ref SearchInformation info)
        {
            int staticEval;

            ETEntry etEntry = EvaluationTable.Probe(info.Position.Hash);
            if (etEntry.Key == EvaluationTable.InvalidKey || !etEntry.Validate(info.Position.Hash) || etEntry.Score == ETEntry.InvalidScore)
            {
                staticEval = info.GetEvaluation(info.Position, info.Position.ToMove);
                EvaluationTable.Save(info.Position.Hash, (short)staticEval);
            }
            else
            {
                staticEval = etEntry.Score;
            }

            return staticEval;
        }

        public static void PrintStatus()
        {
            int entries = 0;
            int keylessScores = 0;

            for (int i = 0; i < Table.Length; i++)
            {
                var item = Table[i];
                if (item.Key != 0)
                {
                    entries++;
                }
                else if (item.Score != 0)
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
