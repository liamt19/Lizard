using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class SearchConstants
    {
        /// <summary>
        /// The maximum amount of time to search, in milliseconds.
        /// </summary>
        public const int MaxSearchTime = int.MaxValue - 1;

        /// <summary>
        /// The default depth to search to.
        /// </summary>
        public static int DefaultSearchDepth = 7;

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

        /// <summary>
        /// Aspiration windows will clamp the expected evaluation (alpha/beta values) at the next depth to the evaluation of the current depth.
        /// The thought behind this is that the evaluation at the next depth generally doesn't change all that much,
        /// so we could save time by guessing what the evaluation should be and only looking at the nodes that will get us there.
        /// <para></para>
        /// The major issue with aspiration windows is that they require positional evaluation to be good/consistent,
        /// because an incorrect, large swing in evaluation will waste time.
        /// </summary>
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

        /// <summary>
        /// Delta pruning will ignore captures which wouldn't help the losing side improve their position during quiescence searches.
        /// For example, if we are down a queen, then testing a pawn capture is less important than testing the capture of a bishop/rook.
        /// </summary>
        public static bool UseDeltaPruning = true;

        /// <summary>
        /// This value is added to the value of the captured piece when we are considering if a capture
        /// is "worth it" or not, and represents possible positional compensation in exchange for material.
        /// </summary>
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
