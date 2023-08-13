using System.Diagnostics;

using Timer = System.Timers.Timer;

namespace LTChess.Logic.Search
{
    public class ThreadedSearch
    {
        /// <summary>
        /// Check if we have reached/exceeded the maximum search time every x milliseconds
        /// </summary>
        public const int TimerTickInterval = 50;

        public const int TimerBuffer = 250;

        /// <summary>
        /// A timer that checks if the search time has reached/exceeded the maximum every <c>TimerTickInterval</c> milliseconds.
        /// </summary>
        private Timer SearchDurationTimer;

        /// <summary>
        /// Keeps track of the time spent on during the entire search
        /// </summary>
        public static Stopwatch TotalSearchTime;

        private SearchInformation info;
        private ThreadedEvaluation tdEval;

        private Move LastBestMove = Move.Null;
        private int LastBestScore = ThreadedEvaluation.ScoreDraw;

        //  These are inconvenient but work
        delegate void CallSearchFinishDelegate();
        delegate void StopSearchingDelegate();
        delegate void SetLastMoveDelegate(Move move, int score);

        private bool allowDepthIncrease;

        public bool StopSearching;

        public ThreadedSearch(SearchInformation info, bool allowDepthIncrease = false)
        {
            this.info = info;
            this.allowDepthIncrease = allowDepthIncrease;

            //TotalSearchTime = new Stopwatch();
            tdEval = new ThreadedEvaluation();
        }

        public static ulong StartNew(in Position rootPosition, int depth, int threads)
        {
            return 1;
        }


        public void StartSearching() => this.StartSearching(this.info);

        public void StartSearching(SearchInformation info)
        {

        }

        public void Deepen(ref SearchInformation info, int alpha, int beta)
        {

        }

        [MethodImpl(Inline)]
        public int FindBest(ref SearchInformation info, int alpha, int beta, int depth, int extensions = 0)
        {
            return 0;
        }

        [MethodImpl(Inline)]
        public int FindBestQuiesce(ref SearchInformation info, int alpha, int beta, int curDepth)
        {
            return 0;
        }

    }
}
