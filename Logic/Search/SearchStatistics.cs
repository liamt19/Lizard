using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace LTChess.Search
{
    public static class SearchStatistics
    {

        public static ulong NegamaxNodes = 0;
        public static ulong LMRReductions = 0;
        public static ulong NegamaxTTExactHits = 0;
        public static ulong BetaCutoffs = 0;

        public static ulong QuiescenceNodes = 0;

        public static ulong TTHits = 0;
        public static ulong TTSaves = 0;
        public static ulong TTReplacements = 0;
        public static ulong TTDepthIncreased = 0;
        public static ulong TTReplacementOther = 0;

        public static ulong TTInvalid = 0;
        public static ulong TTWrongHashKey = 0;
        public static ulong TTNullMoves = 0;
        public static ulong TTIllegal = 0;
        public static ulong TTNotPseudo = 0;
        public static ulong TTIgnoredAlpha = 0;

        public static ulong ETHits = 0;
        public static ulong ETSaves = 0;
        public static ulong ETReplacements = 0;
        public static ulong ETWrongHashKey = 0;

        public static ulong SetTTs = 0;
        public static ulong NotSetTTs = 0;

        public static void PrintStatistics()
        {
            //  https://stackoverflow.com/questions/12474908/how-to-get-all-static-properties-and-its-values-of-a-class-using-reflection
            var fields = typeof(SearchStatistics).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(ulong)).ToList();

            for (int i = 0; i < fields.Count; i++)
            {
                Log(fields[i].Name + ": " + fields[i].GetValue(null));
            }
        }
    }
}
