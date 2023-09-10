using System.Text;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.Simple768;

namespace LTChess.Logic.Search
{
    public struct SearchInformation
    {
        public Action<SearchInformation>? OnDepthFinish;
        public Action<SearchInformation>? OnSearchFinish;

        public Position Position;

        /// <summary>
        /// The depth to stop the search at.
        /// </summary>
        public int MaxDepth = DefaultSearchDepth;

        /// <summary>
        /// The ply of the deepest Pv search so far, which should be at least equal to MaxDepth but almost always higher.
        /// </summary>
        public int SelectiveDepth = 0;

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
        /// Set to true if this SearchInformation instance is being used in a threaded search.
        /// </summary>
        public bool IsMultiThreaded = false;

        /// <summary>
        /// A private reference to a ThreadedEvaluation instance, which is used by the thread to evaluate the positions
        /// that it encounters during the search.
        /// </summary>
        private ThreadedEvaluation tdEval;

        public TimeManager TimeManager;

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
                SearchStatistics.EvalCalls++;
                return (short) HalfKA_HM.GetEvaluation(position);
            }

            return (short) this.tdEval.Evaluate(position, position.ToMove, Trace);
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

            tdEval = new ThreadedEvaluation();
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
            TimeManager.HasMoveTime = true;
        }

        /// <summary>
        /// Replaces the BestMove and BestScore fields when a search is interrupted.
        /// </summary>
        /// <param name="move">The best Move from the previous depth</param>
        /// <param name="score">The evaluation from the previous depth</param>
        [MethodImpl(Inline)]
        public void SetLastMove(Move move, int score)
        {
            if (!move.IsNull())
            {
                Log("SetLastMove(" + move + ", " + score + ") is replacing previous " + BestMove + ", " + BestScore);

                this.BestMove = move;
                this.BestScore = score;
            }
            else
            {
                //  This shouldn't happen.
                Log("ERROR SetLastMove(" + move + ", " + score + ") " + "[old " + BestMove + ", " + BestScore + "] was illegal in FEN " + Position.GetFEN());
            }
        }

        /// <summary>
        /// Prints out the "info depth (number) ..." string
        /// </summary>
        [MethodImpl(Inline)]
        public void PrintSearchInfo(SearchInformation info)
        {
            info.LastSearchInfo = FormatSearchInformation(ref info);
            if (IsMultiThreaded)
            {
                Log(Thread.CurrentThread.ManagedThreadId + " ->\t" + LastSearchInfo);
            }
            else
            {
                Log(info.LastSearchInfo);
            }

#if DEBUG
            SearchStatistics.TakeSnapshot(info.NodeCount, (ulong)info.TimeManager.GetSearchTime());
#endif
        }

        /// <summary>
        /// Creates a deep copy of an existing <see cref="SearchInformation"/>
        /// </summary>
        public static SearchInformation Clone(SearchInformation other)
        {
            SearchInformation copy = (SearchInformation)other.MemberwiseClone();
            copy.Position = new Position(other.Position.GetFEN());

            copy.PV = new Move[other.PV.Length];
            for (int i = 0; i < other.PV.Length; i++)
            {
                copy.PV[i] = other.PV[i];
            }

            copy.OnDepthFinish = copy.PrintSearchInfo;


            return copy;
        }

        /// <summary>
        /// Returns a string with the PV line from this search, 
        /// which begins with the best move, followed by a series of moves that we think will be played in response.
        /// <br></br>
        /// If <paramref name="EngineFormat"/> is true, then the string will look like "e2e4 e7e5 g1g3 b8c6" which is what
        /// chess UCI and other engines programs expect a PV to look like.
        /// </summary>
        /// <param name="EngineFormat">If false, provides the line in human readable form (i.e. Nxf7+ instead of e5f7)</param>
        public string GetPVString(bool EngineFormat = false)
        {
            StringBuilder pv = new StringBuilder();

            //  Start fresh, since a PV at depth 3 could write to PV[0-2] and the time we call GetPV
            //  it could fail at PV[1] and leave the wrong move in PV[2].
            Array.Clear(this.PV);
            int pvLen = SimpleSearch.GetPV(this.PV);

            Position temp = new Position(this.Position.GetFEN(), false);

            int maxPvDepth = Math.Min(this.MaxDepth, pvLen);
            for (int i = 0; i < maxPvDepth; i++)
            {
                if (this.PV[i].IsNull())
                {
                    if (!(temp.CheckInfo.InCheck || temp.CheckInfo.InDoubleCheck))
                    {
                        Log("WARN GetPVString's PV[" + i + "] was null!");
                    }
                    else if (!EngineFormat)
                    {
                        //  This should only happen for checkmates, which means that the last move in the human-readable
                        //  PV string is only considered as causing check (+) rather than checkmate (#).
                        //  If this isn't being sent to a UCI, change the last move's "+" to a "#".
                        pv.Remove(pv.Length - 2, 2);
                        pv.Append("# ");
                    }
                    break;
                }

                if (EngineFormat)
                {
                    if (temp.bb.IsPseudoLegal(this.PV[i]) && temp.IsLegal(this.PV[i]))
                    {
                        pv.Append(this.PV[i] + " ");
                        temp.MakeMove(this.PV[i], false);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (temp.bb.IsPseudoLegal(this.PV[i]))
                    {
                        pv.Append(this.PV[i].ToString(temp) + " ");
                        temp.MakeMove(this.PV[i], false);
                    }
                    else
                    {
                        pv.Append(this.PV[i].ToString() + "? ");
                    }
                }
            }

            if (pv.Length > 1)
            {
                pv.Remove(pv.Length - 1, 1);
            }
            return pv.ToString();
        }

        public override string ToString()
        {
            return "MaxDepth: " + MaxDepth + ", " + "MaxNodes: " + MaxNodes + ", " + "MaxSearchTime: " + MaxSearchTime + ", "
                + "BestMove: " + BestMove.ToString() + ", " + "BestScore: " + BestScore + ", " + "SearchTime: " + (TimeManager == null ? "0 (NULL!)" : TimeManager.GetSearchTime()) + ", "
                + "NodeCount: " + NodeCount + ", " + "StopSearching: " + StopSearching;
        }
    }
}
