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
        public ulong MaxNodes = ulong.MaxValue - 1;

        /// <summary>
        /// The best move found. This may be modified at the end of any call to <c>SimpleSearch.FindBest</c>,
        /// but <c>SimpleSearch.LastBestMove</c> is kept correct at all times.
        /// </summary>
        public Move BestMove = Move.Null;

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
        /// The evaluation of the best move.
        /// </summary>
        public int BestScore = 0;

        /// <summary>
        /// The number of nodes/positions evaluated during the search.
        /// </summary>
        public ulong NodeCount = 0;

        /// <summary>
        /// The color of the player to move in the root position.
        /// </summary>
        public int RootPlayerToMove = Color.White;

        /// <summary>
        /// A private reference to a ThreadedEvaluation instance, which is used by the thread to evaluate the positions
        /// that it encounters during the search.
        /// </summary>
        private ThreadedEvaluation _ClassicalEval;

        public TimeManager TimeManager;

        public bool IsInfinite => (MaxDepth == Utilities.MaxDepth && this.TimeManager.MaxSearchTime == SearchConstants.MaxSearchTime);

        /// <summary>
        /// Returns the evaluation of the position relative to <paramref name="pc"/>, which is the side to move.
        /// </summary>
        [MethodImpl(Inline)]
        public short GetEvaluation(in Position position, bool Trace = false)
        {
            if (UseSimple768)
            {
                return (short) NNUEEvaluation.GetEvaluation(position);
            }

            if (UseHalfKA)
            {
                return (short) HalfKA_HM.GetEvaluation(position, FavorPositionalEval);
            }

            return (short) this._ClassicalEval.Evaluate(position, position.ToMove, Trace);
        }

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
            this.OnSearchFinish = PrintHumanReadableLine;

            _ClassicalEval = new ThreadedEvaluation();
        }

        public static SearchInformation Infinite(Position p)
        {
            SearchInformation si = new SearchInformation(p, Utilities.MaxDepth, SearchConstants.MaxSearchTime);
            si.MaxNodes = ulong.MaxValue - 1;
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
        public void PrintSearchInfo(ref SearchInformation info)
        {
            info.LastSearchInfo = FormatSearchInformationMultiPV(ref info);
            Log(info.LastSearchInfo);

#if DEBUG
            SearchStatistics.TakeSnapshot(info.NodeCount, (ulong)info.TimeManager.GetSearchTime());
#endif
        }

        /// <summary>
        /// Prints a human-readable version of the PV after a search has finished.
        /// </summary>
        [MethodImpl(Inline)]
        public void PrintHumanReadableLine(ref SearchInformation info)
        {
            StringBuilder sb = new StringBuilder();

            SearchThread thisThread = info.Position.Owner;
            List<RootMove> rootMoves = thisThread.RootMoves;

            Move[] PV = new Move[MaxPly];
            RootMove rm = rootMoves[0];
            Array.Copy(rm.PV, PV, MaxPly);
            


            Position temp = new Position(info.Position.GetFEN(), false, null);

            sb.Append("Line:");
            int i = 0;

            for (; i < MaxPly; i++)
            {
                if (PV[i] == Move.Null)
                {
                    break;
                }

                sb.Append(" " + PV[i].ToString(temp));
                temp.MakeMove(PV[i]);
            }

            Log(sb.ToString());



            if (ServerGC)
            {
                //  Force a GC now if we are running in the server mode.
                ForceGC();
            }
        }

        public override string ToString()
        {
            return "MaxDepth: " + MaxDepth + ", " + "MaxNodes: " + MaxNodes + ", " + "MaxSearchTime: " + MaxSearchTime + ", "
                + "BestMove: " + BestMove.ToString() + ", " + "BestScore: " + BestScore + ", " + "SearchTime: " + (TimeManager == null ? "0 (NULL!)" : TimeManager.GetSearchTime()) + ", "
                + "NodeCount: " + NodeCount + ", " + "StopSearching: " + StopSearching;
        }
    }
}
