using System.Text;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.Simple768;
using LTChess.Logic.Threads;

namespace LTChess.Logic.Search
{
    public struct SearchInformation
    {
        /// <summary>
        /// The normal <see cref="Action"/> delegate doesn't allow passing by reference
        /// </summary>
        public delegate void ActionRef<T>(ref T info);

        public ActionRef<SearchInformation>? OnDepthFinish;
        public ActionRef<SearchInformation>? OnSearchFinish;

        public Position Position;

        /// <summary>
        /// The depth to stop the search at.
        /// </summary>
        public int MaxDepth = DefaultSearchDepth;

        /// <summary>
        /// The number of nodes the search should stop at.
        /// </summary>
        public ulong MaxNodes = MaxSearchNodes;

        /// <summary>
        /// If true, then the search will stop
        /// </summary>
        public bool StopSearching = false;

        /// <summary>
        /// Set to true the first time that OnSearchFinish is invoked.
        /// </summary>
        public bool SearchFinishedCalled = false;

        /// <summary>
        /// Set to true while a search is ongoing, and false otherwise.
        /// </summary>
        public bool SearchActive = false;


        /// <summary>
        /// Set to the last "info depth ..." string that was sent.
        /// </summary>
        public string LastSearchInfo = string.Empty;

        /// <summary>
        /// A list of moves which the search thinks will be played next.
        /// PV[0] is the best move that we found, PV[1] is the best response that we think they have, etc.
        /// </summary>
        public Move[] PV;

        /// <summary>
        /// The number of nodes/positions evaluated during the search.
        /// </summary>
        public ulong NodeCount = 0;

        /// <summary>
        /// The color of the player to move in the root position.
        /// </summary>
        public int RootPlayerToMove = Color.White;

        public TimeManager TimeManager;

        public bool IsInfinite => (MaxDepth == Utilities.MaxDepth && this.TimeManager.MaxSearchTime == SearchConstants.MaxSearchTime);

        public SearchInformation(Position p) : this(p, SearchConstants.DefaultSearchDepth, SearchConstants.DefaultSearchTime)
        {
        }

        public SearchInformation(Position p, int depth) : this(p, depth, SearchConstants.DefaultSearchTime)
        {
        }

        public SearchInformation(Position p, int depth, int searchTime)
        {
            this.Position = p;
            this.MaxDepth = depth;

            this.TimeManager = new TimeManager();
            this.TimeManager.MaxSearchTime = searchTime;

            PV = new Move[Utilities.MaxDepth];

            this.OnDepthFinish = PrintSearchInfo;
            this.OnSearchFinish = PrintBestMove;
        }

        public static SearchInformation Infinite(Position p)
        {
            SearchInformation si = new SearchInformation(p, Utilities.MaxDepth, SearchConstants.MaxSearchTime);
            si.MaxNodes = MaxSearchNodes;
            return si;
        }

        [MethodImpl(Inline)]
        public void SetMoveTime(int moveTime)
        {
            TimeManager.MoveTime = moveTime;
            TimeManager.MaxSearchTime = moveTime;
            TimeManager.HasMoveTime = true;
        }

        /// <summary>
        /// Prints out the "info depth (number) ..." string
        /// </summary>
        [MethodImpl(Inline)]
        private void PrintSearchInfo(ref SearchInformation info)
        {
            info.LastSearchInfo = FormatSearchInformationMultiPV(ref info);
            Log(info.LastSearchInfo);

#if DEBUG
            SearchStatistics.TakeSnapshot(info.NodeCount, (ulong)info.TimeManager.GetSearchTime());
#endif
        }

        /// <summary>
        /// Prints the best move from a search.
        /// </summary>
        [MethodImpl(Inline)]
        private void PrintBestMove(ref SearchInformation info)
        {
            Move bestThreadMove = SearchPool.GetBestThread().RootMoves[0].Move;
            Log("bestmove " + bestThreadMove.ToString());

            if (ServerGC)
            {
                //  Force a GC now if we are running in the server mode.
                ForceGC();
            }
        }

        public override string ToString()
        {
            return "MaxDepth: " + MaxDepth + ", " + "MaxNodes: " + MaxNodes + ", " + "MaxSearchTime: " + MaxSearchTime + ", "
                 + "SearchTime: " + (TimeManager == null ? "0 (NULL!)" : TimeManager.GetSearchTime()) + ", "
                + "NodeCount: " + NodeCount + ", " + "StopSearching: " + StopSearching;
        }
    }
}
