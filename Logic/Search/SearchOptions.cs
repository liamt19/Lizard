namespace Lizard.Logic.Search
{
    public static class SearchOptions
    {
        /// <summary>
        /// This number of threads will be used during searches.
        /// <para></para>
        /// For values above 1, the engine will create extra threads to increase the amount of nodes that can be looked at.
        /// Do note that a decent amount of the nodes that are looked at by secondary threads won't influence anything,
        /// but higher numbers of threads tends to correlate with better playing strength.
        /// </summary>
        public static int Threads = 1;


        /// <summary>
        /// Calls to search will display this amount of principal variation lines. 
        /// <para></para>
        /// Ordinarily engines only search for the 1 "best" move, but with MultiPV values 
        /// above 1 this will also display the 2nd best move, the 3rd best, etc.
        /// </summary>
        public static int MultiPV = 1;



        /// <summary>
        /// The size in megabytes of the transposition table.
        /// </summary>
        public static int Hash = 32;



        public static bool UCI_Chess960 = false;



        /// <summary>
        /// Whether or not to extend searches if there is only one good move in a position.
        /// <para></para>
        /// This does reduce search speed (in terms of nodes/second), but the accuracy of the additional nodes 
        /// that are searched make this well worth the speed hit.
        /// </summary>
        public const bool UseSingularExtensions = true;

        /// <summary>
        /// The depth must be greater than or equal to this for singular extensions to be considered.
        /// </summary>
        public static int SingularExtensionsMinDepth = 5;

        /// <summary>
        /// This number is multiplied by the depth to determine the singular beta value.
        /// </summary>
        public static int SingularExtensionsNumerator = 10;

        /// <summary>
        /// If the score from a singular search is below the singular beta value by this amount,
        /// the depth will be extended by 2 instead of by 1.
        /// </summary>
        public static int SingularExtensionsBeta = 25;

        /// <summary>
        /// This amount will be added to the current depth when determining the depth for a singular extension search.
        /// </summary>
        public static int SingularExtensionsDepthAugment = 0;



        /// <summary>
        /// Whether or not to prune sequences of moves that don't improve the position evaluation enough 
        /// even when if we give our opponent a free move.
        /// </summary>
        public const bool UseNMP = true;

        /// <summary>
        /// Nodes need to be at this depth of higher to be considered for pruning.
        /// This also influences the reduced depth that the following nodes are searched
        /// at, which is calculated by adding this flat amount to a node's depth divided by this amount.
        /// i.e. R = <see cref="NMPMinDepth"/> + (depth / <see cref="NMPMinDepth"/>)
        /// </summary>
        public static int NMPMinDepth = 6;

        /// <summary>
        /// The base reduction is always set to this amount.
        /// </summary>
        public static int NMPReductionBase = 4;

        /// <summary>
        /// The reduction is increased by the current depth divided by this amount.
        /// </summary>
        public static int NMPReductionDivisor = 4;

        public static int NMPEvalDivisor = 197;
        public static int NMPEvalMin = 2;



        /// <summary>
        /// Whether or not to ignore moves that don't improve the position evaluation enough.
        /// <br></br>
        /// This is very aggressive in reducing the node count of searches, and generally looks at 2-4x fewer nodes than without RFP.
        /// <para></para>
        /// However, this can cause it to miss some moves (particularly "waiting" moves) until the depth is high enough.
        /// <br></br>For example: 
        /// <see href="https://lichess.org/analysis/fromPosition/n1N3br/2p1Bpkr/1pP2R1b/pP3Pp1/P5P1/1P1p4/p2P4/K7_w_-_-_0_1">this position</see> 
        /// is a mate in 2, but RFP will miss it until depth 8 (regardless of ReverseFutilityPruningMaxDepth).
        /// <br></br>
        /// This is counteracted by the fact that RFP searches are still significantly faster than otherwise, 
        /// so speed-wise this isn't a huge issue.
        /// </summary>
        public const bool UseRFP = true;

        /// <summary>
        /// The depth must be less than or equal to this for reverse futility pruning to be considered.
        /// </summary>
        public static int RFPMaxDepth = 6;

        /// <summary>
        /// This amount is added to reverse futility pruning's margin per depth.
        /// </summary>
        public static int RFPMargin = 47;



        /// <summary>
        /// Whether or not to exclude nodes that give our opponent a seemingly good capture.
        /// <br></br>
        /// ProbCut will test all available captures with a reduced depth and a modified beta,
        /// and if a cutoff occurs we can assume that it would cause a cutoff at the full depth and normal beta value.
        /// </summary>
        public const bool UseProbCut = true;

        /// <summary>
        /// This margin is added to the current beta to determine the modified window if the side to move is NOT improving.
        /// </summary>
        public static int ProbCutBeta = 245;

        /// <summary>
        /// This margin is added to the current beta to determine the modified window if the side to move is improving.
        /// </summary>
        public static int ProbCutBetaImproving = 105;

        /// <summary>
        /// The depth must be greater than or equal to this for ProbCut to be considered.
        /// </summary>
        public static int ProbCutMinDepth = 2;



        /// <summary>
        /// If an LMR search returns a score that is above the current best score by this amount, 
        /// the following verification search will be extended by 1.
        /// </summary>
        public static int LMRExtensionThreshold = 123;

        /// <summary>
        /// In Negamax, non-evasion moves that lose (this amount * depth) will be skipped.
        /// <para></para>
        /// This generally prunes moves that hang a piece.
        /// </summary>
        public static int LMRExchangeBase = 212;



        /// <summary>
        /// For LMR, the reduction of a move is modified by it's history divided by (1024 * this value).
        /// </summary>
        public static int HistoryReductionMultiplier = 12;



        /// <summary>
        /// The margin to add to the current best score in QSearch.
        /// <para></para>
        /// This is used to determine the minimum score a move must have to NOT be considered futile,
        /// as low scoring moves are generally a waste of time to search.
        /// </summary>
        public static int FutilityExchangeBase = 186;



        /// <summary>
        /// Cut nodes without transposition table entries will have a reduction applied to them if the search depth is at or above this.
        /// </summary>
        public static int ExtraCutNodeReductionMinDepth = 4;


        public static int SkipQuietsMaxDepth = 9;
        public static int QSSeeThreshold = 78;


        /// <summary>
        /// If the evaluation at the next depth is within this margin from the previous evaluation,
        /// we use the next depth's evaluation as the starting point for our Alpha/Beta values.
        /// <para></para>
        /// Smaller margins will eliminate more nodes from the search (saves time), but if the margin is too small
        /// we will be forced to redo the search which can waste more time than it saves at high depths.
        /// </summary>
        public static int AspirationWindowMargin = 12;



        /// <summary>
        /// The best move will get a slightly larger bonus if it's score is this much above beta.
        /// </summary>
        public static int HistoryCaptureBonusMargin = 166;



        /// <summary>
        /// Quiet moves that give check will be given this additional bonus.
        /// </summary>
        public static int OrderingGivesCheckBonus = 9611;

        /// <summary>
        /// The multiplier for the value of a piece being captured to add to a capturing move's score.
        /// <para></para>
        /// This establishes a good baseline for a move's value, and is then modified by history.
        /// </summary>
        public static int OrderingVictimValueMultiplier = 14;



        /// <summary>
        /// The value multiplied by the depth
        /// </summary>
        public static int StatBonusMult = 178;

        /// <summary>
        /// The value to subtract from (StatBonusMult * depth)
        /// </summary>
        public static int StatBonusSub = 81;

        /// <summary>
        /// The maximum value that a bonus can be.
        /// </summary>
        public static int StatBonusMax = 1592;



        /// <summary>
        /// The value to multiply by the depth
        /// </summary>
        public static int StatMalusMult = 574;

        /// <summary>
        /// The value to subtract from (StatMalusMult * depth)
        /// </summary>
        public static int StatMalusSub = 109;

        /// <summary>
        /// The maximum value that a malus can be.
        /// </summary>
        public static int StatMalusMax = 1569;



        public static int SEEValue_Pawn = 103;
        public static int SEEValue_Knight = 863;
        public static int SEEValue_Bishop = 1009;
        public static int SEEValue_Rook = 1396;
        public static int SEEValue_Queen = 2222;

        public static int ValuePawn = 170;
        public static int ValueKnight = 797;
        public static int ValueBishop = 975;
        public static int ValueRook = 1604;
        public static int ValueQueen = 3149;
    }
}
