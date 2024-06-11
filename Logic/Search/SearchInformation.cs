namespace Lizard.Logic.Search
{
    public struct SearchInformation
    {
        /// <summary>
        /// The normal <see cref="Action"/> delegate doesn't allow passing by reference
        /// </summary>
        public delegate void ActionRef<T>(ref T info);

        public ActionRef<SearchInformation>? OnDepthFinish;
        public ActionRef<SearchInformation>? OnSearchFinish;



        public int DepthLimit = MaxDepth;
        public ulong NodeLimit = MaxSearchNodes;
        public Position Position;

        public bool IsInfinite => DepthLimit == Utilities.MaxDepth && TimeManager.HardTimeLimit == SearchConstants.MaxSearchTime;

        public SearchInformation(Position p, int depth = MaxDepth, int searchTime = 5000)
        {
            this.Position = p;
            this.DepthLimit = depth;

            TimeManager.HardTimeLimit = searchTime;

            this.OnDepthFinish = PrintSearchInfo;
            this.OnSearchFinish = PrintBestMove;
        }

        public void SetMoveTime(int moveTime)
        {
            TimeManager.MoveTime = moveTime;
        }

        /// <summary>
        /// Prints out the "info depth (number) ..." string
        /// </summary>
        private void PrintSearchInfo(ref SearchInformation info)
        {
            Log(FormatSearchInformationMultiPV(ref info));
        }

        /// <summary>
        /// Prints the best move from a search.
        /// </summary>
        private void PrintBestMove(ref SearchInformation info)
        {
            Move bestThreadMove = SearchPool.GetBestThread().RootMoves[0].Move;
            Log("bestmove " + bestThreadMove.ToString());
        }

        public override string ToString()
        {
            return "MaxDepth: " + DepthLimit + ", " + "MaxNodes: " + NodeLimit + ", " + "MaxSearchTime: " + MaxSearchTime + ", "
                 + "SearchTime: " + TimeManager.GetSearchTime();
        }
    }
}
