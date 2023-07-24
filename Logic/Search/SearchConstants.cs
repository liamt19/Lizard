using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class SearchConstants
    {
        public const int AlphaStart = -100000;
        public const int BetaStart = 100000;

        /// <summary>
        /// The halfmove clock needs to be at least 8 before a draw by threefold repetition can occur.
        /// </summary>
        public const int LowestRepetitionCount = 8;

        /// <summary>
        /// The maximum amount of time to search, in milliseconds.
        /// </summary>
        public const int MaxSearchTime = int.MaxValue - 1;

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
        /// Every 4095 nodes, check to see if we are near or at the maximum search time.
        /// </summary>
        public const int SearchCheckInCount = 4095;


        /// <summary>
        /// The default depth to search to.
        /// </summary>
        public static int DefaultSearchDepth = 7;

        /// <summary>
        /// Default amount of time in milliseconds that a search will run for before it is cancelled.
        /// </summary>
        public static int DefaultSearchTime = 5 * 1000;




        /// <summary>
        /// Aspiration windows will clamp the expected evaluation (alpha/beta values) at the next depth to the evaluation of the current depth.
        /// The thought behind this is that the evaluation at the next depth generally doesn't change all that much,
        /// so we could save time by guessing what the evaluation should be and only looking at the nodes that will get us there.
        /// <para></para>
        /// The major issue with aspiration windows is that they require positional evaluation to be good/consistent,
        /// because an incorrect, large swing in evaluation will waste time.
        /// </summary>
        public static bool UseAspirationWindows = true;

        /// <summary>
        /// If the evaluation at the next depth is within this margin from the previous evaluation,
        /// we use the next depth's evaluation as the starting point for our Alpha/Beta values.
        /// <para></para>
        /// Smaller margins will eliminate more nodes from the search (saves time), but if the margin is too small
        /// we will be forced to redo the search which can waste more time than it saves at high depths.
        /// </summary>
        public static int AspirationWindowMargin = 40;

        /// <summary>
        /// The margins for the aspiration windows will increase by this much per depth.
        /// This represents our uncertainty about which way the position is heading.
        /// </summary>
        public static int AspirationMarginPerDepth = 30;




        /// <summary>
        /// Null Move Pruning gives our opponent an extra move to try to improve their evaluation.
        /// If they can't improve it enough, then we stop looking at that node
        /// since we are reasonably sure that they are losing.
        /// </summary>
        public static bool UseNullMovePruning = true;

        /// <summary>
        /// Nodes need to be at this depth of higher to be considered for pruning.
        /// This also influences the reduced depth that the following nodes are searched
        /// at, which is calculated by adding this flat amount to a node's depth divided by this amount.
        /// i.e. R = <see cref="NullMovePruningMinDepth"/> + (depth / <see cref="NullMovePruningMinDepth"/>)
        /// </summary>
        public static int NullMovePruningMinDepth = 3;




        /// <summary>
        /// Delta pruning will ignore captures which wouldn't help the losing side improve their position during quiescence searches.
        /// For example, if we are down a queen, then testing a pawn capture is less important than testing the capture of a bishop/rook.
        /// </summary>
        public static bool UseDeltaPruning = true;

        /// <summary>
        /// This value is added to the value of the captured piece when we are considering if a capture
        /// is "worth it" or not, and represents possible positional compensation in exchange for material.
        /// This should generally be set equal to the value of a knight minus the value of a pawn,
        /// although setting it higher doesn't hurt performance too much.
        /// </summary>
        public static int DeltaPruningMargin = EvaluationConstants.ValueKnight - EvaluationConstants.ValuePawn;




        /// <summary>
        /// Static Exchange Evaluation checks whether a series of captures on a square gains or loses material.
        /// This is meant to help speed up quiescence search since we can determine if a series of 8 captures
        /// in a row wins us material without having to make/unmake 8 moves.
        /// <para></para>
        /// Set to false right now as it is currently a bit slower than just going into a quiescence search.
        /// </summary>
        public static bool UseStaticExchangeEval = false;

        public static bool UseQuiescenceSEE = true;




        /// <summary>
        /// Futility pruning will cause moves at depth 1 that don't appear to raise alpha enough
        /// from going into a potentially lengthy quiescence search. 
        /// </summary>
        public static bool UseFutilityPruning = true;

        /// <summary>
        /// The depth must be less than or equal to this for futility pruning to be considered.
        /// </summary>
        public static int FutilityPruningMaxDepth = 6;

        /// <summary>
        /// This margin is added to the pruning check, per depth.
        /// </summary>
        public static int FutilityPruningMarginPerDepth = 120;




        public static bool UseReverseFutilityPruning = true;

        /// <summary>
        /// The depth must be less than or equal to this for reverse futility pruning to be considered.
        /// </summary>
        public static int ReverseFutilityPruningMaxDepth = 8;

        public static int ReverseFutilityPruningBaseMargin = 140;



        public static bool UseRazoring = true;

        public static int RazoringMargin = 160;



        public static bool UseHistoryHeuristic = false;


        public static bool UseKillerHeuristic = false;


        /// <summary>
        /// Late Move Pruning will only look at a certain amount of quiet moves (which are non-captures)
        /// based on the current search depth before stopping the search in the branch.
        /// <br></br>
        /// If the search has found at least one decent move before the cutoff, it will ignore the rest
        /// of the moves at that depth, with the reasoning being that our move ordering should've put 
        /// the best quiet moves before that cutoff (and most if not all non-quiet moves before those), 
        /// so it is likely that the remaining quiet moves aren't good enough to be searched.
        /// </summary>
        public static bool UseLateMovePruning = true;

        /// <summary>
        /// The depth must be at or below this to be considered for move count based pruning.
        /// </summary>
        public static int LMPDepth = 3;



        /// <summary>
        /// Late Move Reduction will decrease the depth that "bad" moves are searched at.
        /// This is based on move ordering, which puts tries to sort moves based on how 
        /// likely they are to be "good" or important (like captures, checks, and castling moves).
        /// </summary>
        public static bool UseLateMoveReduction = true;

        /// <summary>
        /// Number of plys to reduce.
        /// </summary>
        public static int LMRReductionAmount = 1;

        /// <summary>
        /// The depth must be at or above this amount to be reduced.
        /// </summary>
        public static int LMRDepth = 3;




        /// <summary>
        /// Whether to use various search extensions, which will increase the search depth by 1 or more.
        /// This is usually applied when a move causes check since it might be important to spend more time
        /// looking at that line.
        /// </summary>
        public static bool UseSearchExtensions = true;

        /// <summary>
        /// The maximum number of depth increases allowed during a search. 
        /// </summary>
        public static int MaxExtensions = 4;

        /// <summary>
        /// A pawn must be at or closer than this distance to cause an extension.
        /// </summary>
        public static int PassedPawnExtensionDistance = 3;

    }
}
