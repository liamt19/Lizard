using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public class SearchInformation
    {
        public Action? OnDepthFinish;
        public Action? OnSearchFinish;

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

        public void DoStopSearching()
        {
            StopSearching = true;
        }

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

        public void CallSearchFinish()
        {
            this.OnSearchFinish?.Invoke();
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

        public SearchInformation(Position p, int depth)
        {
            this.Position = p;
            this.MaxDepth = depth;

            PV = new Move[MAX_DEPTH];

            this.OnDepthFinish = () => Log(FormatSearchInformation(this));
        }

        public SearchInformation(Position p, int depth, int searchTime)
        {
            this.Position = p;
            this.MaxDepth = depth;
            this.MaxSearchTime = searchTime;

            PV = new Move[MAX_DEPTH];

            this.OnDepthFinish = () => Log(FormatSearchInformation(this));
        }

        public static SearchInformation Clone(SearchInformation other)
        {
            Position copyPos = new Position(other.Position.GetFEN());
            SearchInformation copy = (SearchInformation) other.MemberwiseClone();

            copy.Position = copyPos;

            copy.PV = new Move[other.PV.Length];
            for (int i = 0; i < other.PV.Length; i++)
            {
                copy.PV[i] = other.PV[i];
            }
            
            return copy;
        }

        public string GetPVString(bool EngineFormat = false)
        {
            StringBuilder pv = new StringBuilder();
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
