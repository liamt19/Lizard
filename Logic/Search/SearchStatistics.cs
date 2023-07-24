using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace LTChess.Search
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

        public static ulong FutilityPrunedCaptures = 0;
        public static ulong FutilityPrunedNoncaptures = 0;
        public static ulong FutilityPrunedMoves = 0;
        public static ulong LMRReductions = 0;
        public static ulong LMRReductionTotal = 0;
        public static ulong LMRReductionResearches = 0;
        public static ulong LMRReductionResearchesPV = 0;

        public static ulong AspirationWindowFails = 0;
        public static ulong AspirationWindowTotalDepthFails = 0;

        public static ulong NullMovePrunedNodes = 0;

        public static ulong NegamaxTTExactHits = 0;
        public static ulong NegamaxTTExactHitsIgnored = 0;
        public static ulong BetaCutoffs = 0;

        public static ulong QuiescenceNodesTTHits = 0;
        public static ulong QuiescenceNodes = 0;

        public static ulong QuiescenceSEECuts = 0;
        public static ulong QuiescenceSEETotalCuts = 0;


        public static ulong NMCalls = 0;
        public static ulong NMCalls_NOTQ = 0;
        public static ulong NMCompletes = 0;
        public static ulong QCalls = 0;
        public static ulong QCompletes = 0;

        public static ulong TTExactHits = 0;
        public static ulong TTBetaHits = 0;
        public static ulong TTAlphaHits = 0;
        public static ulong TTReplacements = 0;
        public static ulong TTReplacementsE_to_B = 0;
        public static ulong TTReplacementsB_to_E = 0;
        public static ulong TTDepthIncreased = 0;
        public static ulong TTReplacementOther = 0;

        public static ulong ReductionsNonPV = 0;
        public static ulong ReductionsNotImproving = 0;
        public static ulong ReductionsKingChecked = 0;
        public static ulong ReductionsPassedPawns = 0;
        public static ulong ReductionsUnder1 = 0;
        public static ulong ReductionsUnderLMR1 = 0;

        public static ulong Scores_SEE_calls = 0;
        public static ulong Scores_HistoryHeuristic = 0;
        public static ulong Scores_PV_TT_Move = 0;
        public static ulong Scores_Killer_1 = 0;
        public static ulong Scores_Killer_2 = 0;
        public static ulong Scores_WinningCapture = 0;
        public static ulong Scores_Check = 0;
        public static ulong Scores_DoubleCheck = 0;
        public static ulong Scores_Castle = 0;
        public static ulong Scores_PassedPawnPush = 0;
        public static ulong Scores_Normal = 0;
        public static ulong Scores_EqualCapture = 0;
        public static ulong Scores_LosingCapture = 0;

        public static ulong ETHits = 0;
        public static ulong ETSaves = 0;
        public static ulong ETReplacements = 0;
        public static ulong ETWrongHashKey = 0;

        public static ulong SetTTs = 0;
        public static ulong NotSetTTs = 0;

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
        private static Dictionary<string, List<ulong>> _snapshots= new Dictionary<string, List<ulong>>();
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
