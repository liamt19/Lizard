using System.Reflection;


namespace LTChess.Logic.Search
{
    /// <summary>
    /// Contains different statistics that are updated during a search, like the number of nodes that were discarded
    /// due to razoring or the number of LMR reductions that were applied.
    /// </summary>
    public static class SearchStatistics
    {

        public static ulong RazoredNodes = 0;

        public static ulong LateMovePrunings = 0;
        public static ulong LateMovePrunedMoves = 0;

        public static ulong ReverseFutilityPrunedNodes = 0;

        public static ulong KillerMovesAdded = 0;


        public static ulong FutilityPrunedNoncaptures = 0;
        public static ulong FutilityPrunedMoves = 0;
        public static ulong LMRReductions = 0;
        public static ulong LMRReductionTotal = 0;

        public static ulong AspirationWindowFails = 0;
        public static ulong AspirationWindowTotalDepthFails = 0;

        public static ulong NullMovePrunedNodes = 0;

        public static ulong BetaCutoffs = 0;

        public static ulong QCalls = 0;
        public static ulong QCompletes = 0;
        public static ulong QuiescenceNodes = 0;
        public static ulong QuiescenceSEECuts = 0;
        public static ulong QuiescenceSEETotalCuts = 0;
        public static ulong QuiescenceFutilityPrunes = 0;
        public static ulong QuiescenceFutilityPrunesTotal = 0;
        public static ulong QCheckedBreaks = 0;
        public static ulong QSwaps_1 = 0;
        public static ulong QSwaps_0 = 0;
        public static ulong QBetaCuts = 0;

        public static ulong NMCalls = 0;
        public static ulong NMCalls_NOTQ = 0;
        public static ulong NMCompletes = 0;
        public static ulong NMNodes = 0;
        public static ulong NM_Roots = 0;
        public static ulong NM_PVs = 0;
        public static ulong NM_NonPVs = 0;

        public static ulong TTExactHits = 0;
        public static ulong TTBetaHits = 0;
        public static ulong TTAlphaHits = 0;


        public static ulong TTHits_NM = 0;
        public static ulong TTHitNoScore_NM = 0;
        public static ulong TTHitGoodScore_NM = 0;
        public static ulong TTMisses_NM = 0;
        public static ulong TT_InCheck_NM = 0;
        public static ulong TTScoreFit_NM = 0;

        public static ulong TTHits_QS = 0;
        public static ulong TTHitNoScore_QS = 0;
        public static ulong TTHitGoodScore_QS = 0;
        public static ulong TTMisses_QS = 0;
        public static ulong TT_InCheck_QS = 0;
        public static ulong TTScoreFit_QS = 0;
        

        public static ulong EvalCalls = 0;

        public static ulong ReductionsNotImproving = 0;
        public static ulong ExtensionsPV = 0;
        public static ulong ExtensionsTTMove = 0;
        public static ulong ExtensionsKingChecked = 0;
        public static ulong ExtensionsCausesCheck = 0;
        public static ulong ExtensionsPassedPawns = 0;
        public static ulong ReductionsUnder1 = 0;
        public static ulong ReductionsUnderLMR1 = 0;

        public static ulong Scores_SEE_calls = 0;
        public static ulong Scores_HistoryHeuristic = 0;
        public static ulong Scores_PV_TT_Move = 0;
        public static ulong Scores_Promotion = 0;
        public static ulong Scores_MvvLva = 0;
        public static ulong Scores_Killer_1 = 0;
        public static ulong Scores_Killer_2 = 0;

        public static ulong Checkups = 0;

        public static void PrintStatistics()
        {
            //  https://stackoverflow.com/questions/12474908/how-to-get-all-static-properties-and-its-values-of-a-class-using-reflection
            List<FieldInfo>? fields = typeof(SearchStatistics).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ulong)).ToList();

            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].FieldType == typeof(ulong))
                {
                    if ((ulong)fields[i].GetValue(null) != 0)
                    {
                        Log(fields[i].Name + ": " + fields[i].GetValue(null));
                    }
                }
                else
                {
                    Log(fields[i].Name + ": " + fields[i].GetValue(null));
                }
            }
        }

        /// <summary>
        /// Resets every "public static" field to it's default value (0).
        /// </summary>
        public static void Zero()
        {
            foreach (var field in _snapshot_fields)
            {
                field.SetValue(null, default(ulong));
            }

            foreach (var key in _snapshots.Keys)
            {
                _snapshots[key].Clear();
            }

            _shots = 0;
        }


        static SearchStatistics()
        {
            _snapshot_fields = typeof(SearchStatistics).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ulong)).ToList();

            _snapshots.Add("Nodes", new List<ulong>());
            _snapshots.Add("Time", new List<ulong>());

            foreach (var field in _snapshot_fields)
            {
                _snapshots.Add(field.Name, new List<ulong>());
            }
        }

        private static int _shots = 0;
        private static List<FieldInfo>? _snapshot_fields;
        private static Dictionary<string, List<ulong>> _snapshots = new Dictionary<string, List<ulong>>();
        
        public static void TakeSnapshot(ulong nodeCount = 0, ulong time = 0)
        {
            foreach (var field in _snapshot_fields)
            {
                _snapshots[field.Name].Add((ulong)field.GetValue(null));
            }

            _snapshots["Nodes"].Add(nodeCount);
            _snapshots["Time"].Add(time);

            _shots++;
        }

        public static void PrintSnapshots()
        {
            if (_shots == 0)
            {
                return;
            }

            Console.Write("Depth: ");
            for (int i = 0; i < _shots; i++)
            {
                Console.Write((i + 1) + (i < _shots - 1 ? ", " : string.Empty));
            }
            Console.WriteLine();


            Console.Write("Nodes: ");
            for (int i = 0; i < _shots; i++)
            {
                Console.Write(_snapshots["Nodes"][i] + (i < _shots - 1 ? ", " : string.Empty));
            }
            Console.WriteLine();

            Console.Write("Time: ");
            for (int i = 0; i < _shots; i++)
            {
                Console.Write(_snapshots["Time"][i] + (i < _shots - 1 ? ", " : string.Empty));
            }
            Console.WriteLine();


            foreach (var field in _snapshot_fields)
            {
                bool skip = true;
                for (int i = 0; i < _snapshots[field.Name].Count; i++)
                {
                    if (_snapshots[field.Name][i] != 0)
                    {
                        skip = false;
                    }
                }

                if (skip)
                {
                    continue;
                }

                Console.Write(field.Name + ": ");
                for (int i = 0; i < _snapshots[field.Name].Count; i++)
                {
                    Console.Write(_snapshots[field.Name][i] + (i < _snapshots[field.Name].Count - 1 ? ", " : string.Empty));
                }
                Console.WriteLine();
            }
        }
    }
}
