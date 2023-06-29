using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace LTChess.Search
{
    public static class SearchStatistics
    {
        public static ulong FutilityPrunedCaptures = 0;
        public static ulong FutilityPrunedNoncaptures = 0;
        public static ulong LMRReductions = 0;
        public static ulong LMRReductionResearches = 0;
        public static ulong LMRReductionResearchesPV = 0;

        public static ulong AspirationWindowFails = 0;
        public static ulong AspirationWindowTotalDepthFails = 0;

        public static ulong NullMovePrunedNodes = 0;

        public static ulong NegamaxTTExactHits = 0;
        public static ulong NegamaxTTExactHitsIgnored = 0;
        public static ulong BetaCutoffs = 0;

        public static ulong QuiescenceNodes = 0;

        public static ulong TTHits = 0;
        public static ulong TTSaves = 0;
        public static ulong TTReplacements = 0;
        public static ulong TTReplacementsE_to_B = 0;
        public static ulong TTReplacementsB_to_E = 0;
        public static ulong TTDepthIncreased = 0;
        public static ulong TTReplacementOther = 0;

        public static ulong SearchMaxExtensionsReached = 0;
        public static ulong SearchExtensions = 0;
        public static ulong SearchExtensionTotalPlies = 0;
        public static ulong ExtensionsMovesInCheck = 0;
        public static ulong ExtensionsPassedPawns = 0;

        public static ulong Scores_SEE_calls = 0;
        public static ulong Scores_PV_TT_Move = 0;
        public static ulong Scores_KillerMove = 0;
        public static ulong Scores_WinningCapture = 0;
        public static ulong Scores_Castle = 0;
        public static ulong Scores_DoubleCheck = 0;
        public static ulong Scores_Check = 0;
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
    }
}
