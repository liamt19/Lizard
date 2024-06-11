using Lizard.Logic.Threads;

namespace Lizard.Logic.Search
{
    public struct SearchInformation
    {
        public Action? OnDepthFinish;
        public Action? OnSearchFinish;

        public bool HasDepthLimit => DepthLimit != MaxDepth;
        public int DepthLimit = MaxDepth;
        
        public bool HasNodeLimit => NodeLimit != MaxSearchNodes;
        public ulong NodeLimit = MaxSearchNodes;


        public Position Position;

        public bool IsInfinite => (!HasDepthLimit && !TimeManager.HasHardTimeLimit);

        public SearchInformation(Position p, int depth = MaxDepth, int searchTime = MaxSearchTime)
        {
            this.Position = p;
            this.DepthLimit = depth;

            TimeManager.HardTimeLimit = searchTime;

            this.OnDepthFinish = SearchThreadPool.OnDepthDone;
            this.OnSearchFinish = SearchThreadPool.OnSearchDone;
        }


        public override string ToString()
        {
            return $"MaxDepth: {DepthLimit}, MaxNodes: {NodeLimit}, MaxSearchTime: {MaxSearchTime}, SearchTime: {TimeManager.GetSearchTime()}";
        }
    }
}
