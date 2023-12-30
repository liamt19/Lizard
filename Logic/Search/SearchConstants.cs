namespace LTChess.Logic.Search
{
    public static class SearchConstants
    {
        public const int AlphaStart = -ScoreMate;
        public const int BetaStart = ScoreMate;


        /// <summary>
        /// The maximum amount of time to search, in milliseconds.
        /// </summary>
        public const int MaxSearchTime = int.MaxValue - 1;

        public const ulong MaxSearchNodes = ulong.MaxValue - 1;

        /// <summary>
        /// If we have fewer than this amount of milliseconds on our clock, we are in "low time".
        /// Our search times will be reduced if we would enter low time.
        /// </summary>
        public const int SearchLowTimeThreshold = 3 * 1000;


        public const bool StopLosingOnTimeFromVerizon = false;
        public const int VerizonDisconnectionBuffer = 10 * 1000;


        /// <summary>
        /// The default depth to search to.
        /// </summary>
        public const int DefaultSearchDepth = MaxDepth;

        /// <summary>
        /// Default amount of time in milliseconds that a search will run for before it is cancelled.
        /// </summary>
        public const int DefaultSearchTime = 5 * 1000;


        public const int DefaultMovesToGo = 20;


        /// <summary>
        /// Whether or not to adjust UCI search times if there is only one legal move in the position.
        /// <br></br>
        /// Using this will save time since we the best move is the only one we can make and we don't need to spend time to confirm that.
        /// </summary>
        public const bool OneLegalMoveMode = false;

        /// <summary>
        /// The max time in milliseconds that will be searched if <see cref="OneLegalMoveMode"/> is <see langword="true"/>.
        /// </summary>
        public const int OneLegalMoveTime = 100;



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
