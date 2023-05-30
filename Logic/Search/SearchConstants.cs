using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class SearchConstants
    {
        public static int DefaultSearchDepth = 5;

        /// <summary>
        /// Default amount of time in milliseconds that a search will run for before it is cancelled.
        /// </summary>
        public static int DefaultSearchTime = 6 * 1000;

        public const int MaxSearchTime = int.MaxValue - 1;

        public const int FutilityPruningDepth = 6;

        public const int FutilityPruningScore = 100;

        /// <summary>
        /// Number of plys to reduce.
        /// </summary>
        public static int LMRReductionAmount = 1;

        /// <summary>
        /// Only reduce if the depth is at or above this number.
        /// </summary>
        public static int LMRDepth = 3;

        public static int AlphaStart = -100000;
        public static int BetaStart = 100000;
    }
}
