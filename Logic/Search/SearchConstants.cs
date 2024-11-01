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


        public const int DefaultMovesToGo = 20;
    }
}
