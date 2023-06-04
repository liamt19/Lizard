using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class SearchConstants
    {
        public const int MaxSearchTime = int.MaxValue - 1;

        public static int DefaultSearchDepth = 8;

        /// <summary>
        /// Default amount of time in milliseconds that a search will run for before it is cancelled.
        /// </summary>
        public static int DefaultSearchTime = 5 * 1000;

        /// <summary>
        /// If we have fewer than this amount of milliseconds on our clock, we are in "low time".
        /// Our search times will be reduced if we would enter low time.
        /// </summary>
        public const int SearchLowTimeThreshold = 3 * 1000;

        /// <summary>
        /// If we are at or past this depth, start checking if our time is approaching the low time threshold.
        /// Searches in depths 1-4 currently finish within a few milliseconds, and beginning at depth 5 they can begin to take 2-3 seconds.
        /// </summary>
        public const int SearchLowTimeMinDepth = 2;

        public static bool UseAspirationWindows = false;

        /// <summary>
        /// If the evaluation at the next depth is within this margin from the previous evaluation,
        /// we use the next depth's evaluation as the starting point for our Alpha/Beta values.
        /// <para></para>
        /// Smaller margins will eliminate more nodes from the search (saves time), but if the margin is too small
        /// we will be forced to redo the search which can waste more time than it saves at high depths.
        /// </summary>
        public static int AspirationWindowMargin = 35;

        /// <summary>
        /// The margins for the aspiration windows will increase by this much per depth.
        /// This represents our uncertainty about which way the position is heading.
        /// </summary>
        public static int MarginIncreasePerDepth = 10;

        public static bool UseDeltaPruning = false;

        public const int DeltaPruningMargin = 210;

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

        public const int AlphaStart = -100000;
        public const int BetaStart = 100000;
    }
}
