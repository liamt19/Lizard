using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
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
        /// The number of nodes the search should stop at.
        /// </summary>
        public ulong MaxNodes = ulong.MaxValue - 1;

        /// <summary>
        /// The time in milliseconds that the search should stop at.
        /// </summary>
        public long MaxSearchTime = DefaultSearchTime;

        /// <summary>
        /// The best move found.
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

        public bool SearchWasAborted = false;
        public string LastSearchInfo = string.Empty;

        /// <summary>
        /// Replaces the BestMove and BestScore fields when a search is interrupted.
        /// </summary>
        /// <param name="move">The best Move from the previous depth</param>
        /// <param name="score">The evaluation from the previous depth</param>
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
        /// A list of moves which the search thinks will be played next.
        /// PV[0] is the best move that we found, PV[1] is the best response that we think they have, etc.
        /// </summary>
        public Move[] PV;

        /// <summary>
        /// The evaluation of the best move.
        /// </summary>
        public int BestScore = 0;

        /// <summary>
        /// The time currently spent during the search.
        /// </summary>
        public double SearchTime = 0;

        /// <summary>
        /// The number of nodes/positions evaluated during the search.
        /// </summary>
        public ulong NodeCount = 0;

        /// <summary>
        /// Set to the value of wtime/btime if one was provided during a UCI "go" command.
        /// If the search time gets too close to this, the search will stop prematurely so
        /// we don't lose on time.
        /// </summary>
        public int PlayerTimeLeft = SearchConstants.MaxSearchTime;

        /// <summary>
        /// The number of moves made so far in the root position.
        /// This is used to calculate mate scores since that is a bit more complicated than just looking at the depth.
        /// </summary>
        public int RootPositionMoveCount = 0;

        /// <summary>
        /// Set to true if this SearchInformation instance is being used in a threaded search.
        /// </summary>
        public bool IsMultiThreaded = false;

        /// <summary>
        /// A private reference to a ThreadedEvaluation instance, which is used by the thread to evaluate the positions
        /// that it encounters during the search.
        /// </summary>
        private ThreadedEvaluation tdEval;

        /// <summary>
        /// Returns the evaluation of the position relative to <paramref name="pc"/>, which is the side to move.
        /// </summary>
        public int GetEvaluation(in Position position, int pc, bool Trace = false) => this.tdEval.Evaluate(position, pc, Trace);



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
            this.MaxSearchTime = searchTime;
            this.RootPositionMoveCount = this.Position.Moves.Count;

            PV = new Move[Utilities.MaxDepth];

            this.OnDepthFinish = PrintSearchInfo;

            tdEval = new ThreadedEvaluation();
        }

        /// <summary>
        /// Prints out the "info depth (number) ..." string
        /// </summary>
        public void PrintSearchInfo(SearchInformation info)
        {
            info.LastSearchInfo = FormatSearchInformation(info);
            if (IsMultiThreaded)
            {
                Log(Thread.CurrentThread.ManagedThreadId + " ->\t" + LastSearchInfo);
            }
            else
            {
                Log(info.LastSearchInfo);
            }
        }

        /// <summary>
        /// Creates a deep copy of an existing <c>SearchInformation</c>
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
        /// Returns the number of plys from the root position, which is the "3" in "+M3" if white has mate in 3.
        /// </summary>
        public int MakeMateScore()
        {
            int movesMade = (this.Position.Moves.Count - this.RootPositionMoveCount);
            int mateScore = -ThreadedEvaluation.ScoreMate - ((movesMade / 2) + 1);
            return mateScore;
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
            SimpleSearch.GetPV(this, this.PV, 0);

            Position temp = new Position(this.Position.GetFEN());
            for (int i = 0; i < this.MaxDepth; i++)
            {
                if (this.PV[i].IsNull())
                {
                    break;
                }

                if (EngineFormat)
                {
                    if (temp.IsLegal(this.PV[i]))
                    {
                        pv.Append(this.PV[i] + " ");
                        temp.MakeMove(this.PV[i]);
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
                        temp.MakeMove(this.PV[i]);
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
                + "BestMove: " + BestMove.ToString() + ", " + "BestScore: " + BestScore + ", " + "SearchTime: " + SearchTime + ", " 
                + "NodeCount: " + NodeCount + ", " + "StopSearching: " + StopSearching;
        }
    }
}
