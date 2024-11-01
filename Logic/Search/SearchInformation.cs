
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

namespace Lizard.Logic.Search
{
    public struct SearchInformation
    {
        /// <summary>
        /// The normal <see cref="Action"/> delegate doesn't allow passing by reference
        /// </summary>
        public delegate void ActionRef<T>(ref T info);

        /// <summary>
        /// The method to call (which must accept a <see langword="ref"/> <see cref="SearchInformation"/> parameter)
        /// during a search just before the depth increases.
        /// <br></br>
        /// By default, this will print out the "info depth # ..." string.
        /// </summary>
        public ActionRef<SearchInformation>? OnDepthFinish;

        /// <summary>
        /// The method to call (which must accept a <see langword="ref"/> <see cref="SearchInformation"/> parameter)
        /// when a search is finished.
        /// <br></br>
        /// By default, this will print "bestmove (move)".
        /// </summary>
        public ActionRef<SearchInformation>? OnSearchFinish;

        public Position Position;

        public int DepthLimit = Utilities.MaxDepth;
        public ulong NodeLimit = MaxSearchNodes;
        public ulong SoftNodeLimit = MaxSearchNodes;


        /// <summary>
        /// Set to true the first time that OnSearchFinish is invoked.
        /// </summary>
        public bool SearchFinishedCalled = false;

        /// <summary>
        /// Set to true while a search is ongoing, and false otherwise.
        /// </summary>
        public bool SearchActive = false;

        public TimeManager TimeManager;

        public bool HasDepthLimit => (DepthLimit != Utilities.MaxDepth);
        public bool HasNodeLimit => (NodeLimit != MaxSearchNodes);
        public bool HasTimeLimit => (this.TimeManager.MaxSearchTime != SearchConstants.MaxSearchTime);

        public bool IsInfinite => !HasDepthLimit && !HasTimeLimit;

        public SearchInformation(Position p, int depth = Utilities.MaxDepth, int searchTime = SearchConstants.MaxSearchTime)
        {
            this.Position = p;
            this.DepthLimit = depth;

            this.TimeManager = new TimeManager();
            this.TimeManager.MaxSearchTime = searchTime;

            this.OnDepthFinish = Utilities.PrintSearchInfo;
            this.OnSearchFinish = (ref SearchInformation info) => Log($"bestmove {info.Position.Owner.AssocPool.GetBestThread().RootMoves[0].Move.ToString()}");
        }

        public void SetMoveTime(int moveTime)
        {
            TimeManager.MaxSearchTime = moveTime;
            TimeManager.HasMoveTime = true;
        }

        public override string ToString()
        {
            return $"DepthLimit: {DepthLimit}, NodeLimit: {NodeLimit}, MaxSearchTime: {MaxSearchTime}, SearchTime: "
                 + (TimeManager == null ? "0 (NULL!)" : TimeManager.GetSearchTime());
        }
    }
}
