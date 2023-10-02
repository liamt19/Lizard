using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LTChess.Logic.Search;

using static LTChess.Logic.Threads.SearchThread;

namespace LTChess.Logic.Threads
{
    public unsafe class SearchThreadPool
    {
        public static SearchThreadPool SearchPool;

        public int ThreadCount = SearchConstants.Threads;

        public SearchInformation SharedInfo;
        public SearchThread[] Threads;
        public SearchThread MainThread => Threads[0];

        /// <summary>
        /// Set to true when the main thread when we are nearing the maximum search time / maximum node count,
        /// or when the UCI receives a "stop" command
        /// </summary>
        public volatile bool StopThreads;

        static SearchThreadPool()
        {
            //  Initialize the global threadpool here
            SearchPool = new SearchThreadPool(SearchConstants.Threads);
        }

        public SearchThreadPool(int threadCount)
        {
            Resize(threadCount);
        }

        public void Resize(int newThreadCount)
        {
            if (Threads != null)
            {
                MainThread.WaitForThreadFinished();

                int joins = 0;
                for (int i = 0; i < ThreadCount; i++)
                {
                    if (Threads[i] != null)
                    {
                        Threads[i].Dispose();
                        joins++;
                    }
                }
                Debug.WriteLine("Joined " + joins + " existing threads");
            }

            this.ThreadCount = newThreadCount;
            Threads = new SearchThread[ThreadCount];
            

            for (int i = 0; i < ThreadCount; i++)
            {
                Threads[i] = new SearchThread(i);
            }

            MainThread.WaitForThreadFinished();
            Debug.WriteLine("Spawned " + ThreadCount + " new threads");
        }


        public void StartSearch(Position rootPosition, ref SearchInformation rootInfo)
        {
            StartSearch(rootPosition, ref rootInfo, new ThreadSetup(rootPosition.GetFEN(), new List<Move>()));
        }

        public void StartSearch(Position rootPosition, ref SearchInformation rootInfo, ThreadSetup setup)
        {
            MainThread.WaitForThreadFinished();

            StopThreads = false;
            SharedInfo = rootInfo;          //  Initialize the shared SearchInformation
            SharedInfo.SearchActive = true; //  And mark this search as having started

            Span<Move> moves = stackalloc Move[NormalListCapacity];
            int size = rootPosition.GenAllLegalMovesTogether(moves);

            var rootFEN = setup.StartFEN;
            if (rootFEN == InitialFEN && setup.SetupMoves.Count == 0)
            {
                rootFEN = rootPosition.GetFEN();
            }

            for (int i = 0; i < ThreadCount; i++)
            {
                var td = Threads[i];

                td.Nodes = 0;

                td.CompletedDepth = 0;
                td.RootDepth = 0;
                td.SelDepth = 0;

                //  Each thread gets its own copy of each of the root position's "RootMoves" since the thread will be sorting these
                //  and doing that simultaneously would cause data races
                td.RootMoves = new List<RootMove>(size);
                for (int j = 0; j < size; j++)
                {
                    td.RootMoves.Add(new RootMove(moves[j]));
                }

                td.RootPosition = new Position(rootFEN, true, td);
                foreach (var move in setup.SetupMoves)
                {
                    td.RootPosition.MakeMove(move);
                }

                Debug.Assert((td.RootPosition.State->Hash) == (rootPosition.State->Hash));

#if DEBUG
                for (int j = 0; j < rootPosition.GamePly; j++)
                {
                    Debug.Assert(((td.RootPosition.StartingState + j)->Hash) == (rootPosition.StartingState + j)->Hash);
                }
#endif
            }

            SharedInfo.TimeManager.StartTimer();
            MainThread.PrepareToSearch();
        }




        public SearchThread GetBestThread()
        {
            for (int i = 0; i < ThreadCount; i++)
            {
                Debug.WriteLine("Thread[" + i + "] = " + Threads[i].Searching + " " + Threads[i].RootMoves[0]);
            }


            SearchThread bestThread = MainThread;
            for (int i = 1; i < ThreadCount; i++)
            {
                int thisScore = Threads[i].RootMoves[0].Score - bestThread.RootMoves[0].Score;

                //  If a thread's score is higher than the previous best score,
                //  and that thread's depth is equal to or higher than the previous, then make that the new best.
                if (thisScore > 0 && (Threads[i].CompletedDepth >= bestThread.CompletedDepth))
                {
                    bestThread = Threads[i];
                    Debug.WriteLine("GetBestThread() New best move is " + bestThread.RootMoves[0]);
                }
            }

            return bestThread;
        }


        /// <summary>
        /// Unblocks each thread waiting in IdleLoop by setting their <see cref="SearchThread.Searching"/> variable to true
        /// and signalling the condition variable.
        /// </summary>
        public void StartThreads()
        {
            //  Skip Threads[0] because it will do this to itself after this method returns.
            for (int i = 1; i < ThreadCount; i++)
            {
                Threads[i].PrepareToSearch();
            }
        }


        /// <summary>
        /// Blocks the main thread of the pool until each of the other threads have finished searching and 
        /// are blocked in IdleLoop.
        /// </summary>
        public void WaitForSearchFinished()
        {
            //  Skip Threads[0] (the MainThread) since this method is only ever called when the MainThread is done.
            for (int i = 1; i < ThreadCount; i++)
            {
                Threads[i].WaitForThreadFinished();
            }
        }


        /// <summary>
        /// Resets each SearchThread's Accumulator and history heuristics to their defaults.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < ThreadCount; i++)
            {
                Threads[i].Clear();
            }

            MainThread.CheckupCount = 0;
        }


        /// <summary>
        /// Returns the total amount of nodes searched by all SearchThreads in the pool.
        /// </summary>
        /// <returns></returns>
        public ulong GetNodeCount()
        {
            ulong total = 0;
            for (int i = 0; i < ThreadCount; i++)
            {
                total += Threads[i].Nodes;
            }
            return total;
        }


    }
}
