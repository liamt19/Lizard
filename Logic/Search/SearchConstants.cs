namespace LTChess.Logic.Search
{
    public static class SearchConstants
    {
        public const int AlphaStart = -ScoreInfinite;
        public const int BetaStart = ScoreInfinite;

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


        public const bool StopLosingOnTimeFromVerizon = true;
        public const int VerizonDisconnectionBuffer = 10 * 1000;



        /// <summary>
        /// Every 4095 nodes, check to see if we are near or at the maximum search time.
        /// </summary>
        public const int SearchCheckInCount = 4095;


        /// <summary>
        /// The default depth to search to.
        /// </summary>
        public const int DefaultSearchDepth = 7;

        /// <summary>
        /// Default amount of time in milliseconds that a search will run for before it is cancelled.
        /// </summary>
        public const int DefaultSearchTime = 5 * 1000;


        /// <summary>
        /// For HalfKA, the NNUE evaluation has a 2 parts: PSQT and Positional. It is usually beneficial to give Positional slightly more weight.
        /// <para></para>
        /// The PSQT component comes from the FeatureTransformer and almost exclusively uses the values of pieces, 
        /// plus a small amount for the square that piece is on.
        /// <para></para>
        /// The Positional component comes from the network layers (AffineTransform, ClippedReLU, ...) and gives a score based on
        /// the relative positions of each piece on the board.
        /// </summary>
        public const bool FavorPositionalEval = true;



        /// <summary>
        /// Calls to search will display this amount of principal variation lines. 
        /// <para></para>
        /// Ordinarily engines only search for the 1 "best" move, but with MultiPV values 
        /// above 1 this will also display the 2nd best move, the 3rd best, etc.
        /// </summary>
        public static int MultiPV = 1;


        /// <summary>
        /// This number of threads will be used during searches.
        /// <para></para>
        /// For values above 1, the engine will create extra threads to increase the amount of nodes that can be looked at.
        /// Do note that a decent amount of the nodes that are looked at by secondary threads won't influence anything,
        /// but higher numbers of threads tends to correlate with better playing strength.
        /// </summary>
        public static int Threads = 2;



        /// <summary>
        /// Aspiration windows will clamp the expected evaluation (alpha/beta values) at the next depth to the evaluation of the current depth.
        /// The thought behind this is that the evaluation at the next depth generally doesn't change all that much,
        /// so we could save time by guessing what the evaluation should be and only looking at the nodes that will get us there.
        /// <para></para>
        /// The major issue with aspiration windows is that they require positional evaluation to be good/consistent,
        /// because an incorrect, large swing in evaluation will waste time.
        /// </summary>
        public const bool UseAspirationWindows = true;

        /// <summary>
        /// If the evaluation at the next depth is within this margin from the previous evaluation,
        /// we use the next depth's evaluation as the starting point for our Alpha/Beta values.
        /// <para></para>
        /// Smaller margins will eliminate more nodes from the search (saves time), but if the margin is too small
        /// we will be forced to redo the search which can waste more time than it saves at high depths.
        /// </summary>
        public const int AspirationWindowMargin = 40;

        /// <summary>
        /// The margins for the aspiration windows will increase by this much per depth.
        /// This represents our uncertainty about which way the position is heading.
        /// </summary>
        public const int AspirationMarginPerDepth = 20;



        /// <summary>
        /// Nodes need to be at this depth of higher to be considered for pruning.
        /// This also influences the reduced depth that the following nodes are searched
        /// at, which is calculated by adding this flat amount to a node's depth divided by this amount.
        /// i.e. R = <see cref="NullMovePruningMinDepth"/> + (depth / <see cref="NullMovePruningMinDepth"/>)
        /// </summary>
        public const int NullMovePruningMinDepth = 3;


        /// <summary>
        /// This margin is added to the pruning check, per depth.
        /// </summary>
        public const int FutilityPruningMarginPerDepth = 120;

        /// <summary>
        /// If moves exceed this margin, they are treated as "good" in multiple places.
        /// </summary>
        public const int ExchangeBase = 200;


        /// <summary>
        /// The depth must be less than or equal to this for reverse futility pruning to be considered.
        /// </summary>
        public const int ReverseFutilityPruningMaxDepth = 8;
        public const int ReverseFutilityPruningPerDepth = 70;
        public const int ReverseFutilityPruningImproving = 75;


        public const int RazoringMaxDepth = 6;
        public const int RazoringMargin = 275;


        /// <summary>
        /// Whether to use the included (small) Polyglot opening book file.
        /// Most UCI's will handle this automatically, but using a book can add some variety to the 
        /// first couple moves that engines make.
        /// </summary>
        public const bool UsePolyglot = false;

        /// <summary>
        /// How many moves to try to play from the Polyglot file. 
        /// A ply of X means that from the starting position it will try probing for the first X moves that it makes.
        /// </summary>
        public const int PolyglotMaxPly = 6;

        /// <summary>
        /// Whether to simulate the time it would ordinarily take to search when using an opening book.
        /// Probing a Polyglot file only takes 5-20 ms, so to make things more fair for engines that don't use books,
        /// this will pick the move it wants to make in ~10 ms and waste the remaining few seconds before responding with that move.
        /// </summary>
        public const bool PolyglotSimulateTime = false;

    }
}
