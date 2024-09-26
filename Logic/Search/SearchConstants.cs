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
