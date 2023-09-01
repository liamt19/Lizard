namespace LTChess.Logic.Transposition
{
    public static class EvaluationTable
    {
        public const ulong InvalidKey = 0UL;

        private static ETEntry[] Table;
        private static ulong Size;

        private static bool Initialized = false;

        static EvaluationTable()
        {
            if (!Initialized)
            {
                //  This is no longer used
                //Initialize();
            }
        }

        /// <summary>
        /// 1mb is enough for 262,144 entries
        /// </summary>
        public static unsafe void Initialize(int mb = 24)
        {
            //  1024 * 1024 = 1048576 == 0x100000UL
            Size = ((ulong)mb * 0x100000UL) / (ulong)sizeof(ETEntry);
            Table = new ETEntry[Size];
        }

        [MethodImpl(Inline)]
        public static void Clear() => Array.Clear(Table);

        [MethodImpl(Inline)]
        public static void Save(ulong hash, short score)
        {
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
            if (etEntry.Key == EvaluationTable.InvalidKey || !etEntry.ValidateKey(info.Position.Hash) || etEntry.Score == ETEntry.InvalidScore)
            {
                staticEval = info.GetEvaluation(info.Position);
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
