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

        public Barrier Blocker;

        static SearchThreadPool()
        {
            //  Initialize the global threadpool here
            SearchPool = new SearchThreadPool(SearchConstants.Threads);
        }

        public SearchThreadPool(int threadCount)
        {
            Blocker = new Barrier(1);
            Resize(threadCount);
        }

        public void Resize(int newThreadCount)
        {
            if (Threads != null)
            {
                MainThread.WaitForThreadFinished();

                for (int i = 0; i < ThreadCount; i++)
                {
                    if (Threads[i] != null)
                    {
                        Threads[i].Dispose();
                    }
                }
            }

            this.ThreadCount = newThreadCount;
            Threads = new SearchThread[ThreadCount];
            

            for (int i = 0; i < ThreadCount; i++)
            {
                Threads[i] = new SearchThread(i);
            }

            MainThread.WaitForThreadFinished();
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

            ScoredMove* moves = stackalloc ScoredMove[MoveListSize];
            int size = rootPosition.GenLegal(moves);

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
                    td.RootMoves.Add(new RootMove(moves[j].Move));
                }

                td.RootPosition.LoadFromFEN(rootFEN);

                if (EnableAssertions)
                {
                    Assert(td.RootPosition.Owner == td, 
                        "The RootPosition for the thread " + td.ToString() + " had an owner of " + td.RootPosition.Owner.ToString() + "! " + 
                        "All search threads must be the owner of their RootPosition objects. " +
                        "This can only happen when a SearchThread's RootPosition object is overwritten with a different position, " +
                        "and if the RootPosition field is readonly (which it should be) this means there is UB.");
                }
                
                foreach (var move in setup.SetupMoves)
                {
                    td.RootPosition.MakeMove(move);
                }

                if (EnableAssertions)
                {
                    if ((td.RootPosition.State->Hash) != (rootPosition.State->Hash))
                    {
                        StringBuilder threadHashes = new StringBuilder();
                        var temp = td.RootPosition.StartingState;
                        while (temp != td.RootPosition.State)
                        {
                            threadHashes.Append(temp->Hash + ", ");
                            temp++;
                        }

                        if (threadHashes.Length > 3)
                            threadHashes.Remove(threadHashes.Length - 2, 2);


                        StringBuilder searchHashes = new StringBuilder();
                        temp = td.RootPosition.StartingState;
                        while (temp != td.RootPosition.State)
                        {
                            searchHashes.Append(temp->Hash + ", ");
                            temp++;
                        }

                        if (searchHashes.Length > 3)
                            searchHashes.Remove(searchHashes.Length - 2, 2);

                        Assert((td.RootPosition.State->Hash) == (rootPosition.State->Hash),
                            "The RootPosition for the thread " + td.ToString() + " had a hash of " + (td.RootPosition.State->Hash) + 
                            ", but it should have been " + (rootPosition.State->Hash) + ". " +
                            "The previous hashes of the thread RootPosition are as follows: [" + threadHashes.ToString() + "]. " +
                            "The previous hashes of the should have looked like this: [" + searchHashes.ToString() + "].");
                    }
                }
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


        public void BlockCallerUntilFinished()
        {
            if (Blocker.ParticipantCount == 1)
            {
                Blocker.AddParticipant();
                Blocker.SignalAndWait();
                Blocker.RemoveParticipant();
            }
            else
            {
                Debug.WriteLine("WARN BlockCallerUntilFinished was called, but the barrier had " + Blocker.ParticipantCount + " participants (should have been 1)");
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
