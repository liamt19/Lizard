namespace Lizard.Logic.Search
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
    }
}
