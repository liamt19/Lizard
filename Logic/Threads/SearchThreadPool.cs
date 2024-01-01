using System.Text;

namespace LTChess.Logic.Threads
{
    public unsafe class SearchThreadPool
    {
        /// <summary>
        /// Global ThreadPool.
        /// </summary>
        public static SearchThreadPool SearchPool;

        public int ThreadCount = SearchOptions.Threads;

        public SearchInformation SharedInfo;
        public SearchThread[] Threads;
        public SearchThread MainThread => Threads[0];

        /// <summary>
        /// Set to true by the main thread when we are nearing the maximum search time / maximum node count,
        /// or when the UCI receives a "stop" command
        /// </summary>
        public volatile bool StopThreads;

        public Barrier Blocker;

        static SearchThreadPool()
        {
            //  Initialize the global threadpool here
            SearchPool = new SearchThreadPool(SearchOptions.Threads);
        }

        public SearchThreadPool(int threadCount)
        {
            Blocker = new Barrier(1);
            Resize(threadCount);
        }

        /// <summary>
        /// Joins any existing threads and spawns <paramref name="newThreadCount"/> new ones.
        /// </summary>
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

        /// <summary>
        /// Prepares each thread in the SearchThreadPool for a new search, and wakes the MainSearchThread up.
        /// <br></br>
        /// This is called by the main program thread (or UCI) after the search parameters have been set in <paramref name="rootInfo"/>.
        /// <para></para>
        /// <paramref name="rootPosition"/> should be set to the position to create the <see cref="SearchThread.RootMoves"/> from, 
        /// which should be the same as <paramref name="rootInfo"/>'s Position.
        /// </summary>
        public void StartSearch(Position rootPosition, ref SearchInformation rootInfo)
        {
            StartSearch(rootPosition, ref rootInfo, new ThreadSetup(rootPosition.GetFEN(), new List<Move>()));
        }


        /// <summary>
        /// <inheritdoc cref="StartSearch(Position, ref SearchInformation)"/>
        /// <para></para>
        /// Thread positions are first set to the FEN specified by <paramref name="setup"/>, 
        /// and each move in <see cref="ThreadSetup.SetupMoves"/> (if any) is made for each position.
        /// </summary>
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
                    if (td.RootPosition.State->Hash != rootPosition.State->Hash)
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

                        Assert(td.RootPosition.State->Hash == rootPosition.State->Hash,
                            "The RootPosition for the thread " + td.ToString() + " had a hash of " + td.RootPosition.State->Hash +
                            ", but it should have been " + rootPosition.State->Hash + ". " +
                            "The previous hashes of the thread RootPosition are as follows: [" + threadHashes.ToString() + "]. " +
                            "The previous hashes of the should have looked like this: [" + searchHashes.ToString() + "].");
                    }
                }
            }

            if (size == 1 && UCIClient.Active && OneLegalMoveMode)
            {
                //  If we only have one legal move while running in UCI mode, set the search time to be at most 100 ms.
                //  There is no point in searching this node longer, since the TT will be cleared anyways.
                SharedInfo.TimeManager.MaxSearchTime = Math.Min(SharedInfo.TimeManager.MaxSearchTime, OneLegalMoveTime);
            }

            SharedInfo.TimeManager.StartTimer();
            MainThread.PrepareToSearch();
        }




        public SearchThread GetBestThread()
        {
            SearchThread bestThread = MainThread;
            for (int i = 1; i < ThreadCount; i++)
            {
                int thisScore = Threads[i].RootMoves[0].Score - bestThread.RootMoves[0].Score;

                //  If a thread's score is higher than the previous best score,
                //  and that thread's depth is equal to or higher than the previous, then make that the new best.
                if (thisScore > 0 && (Threads[i].CompletedDepth >= bestThread.CompletedDepth))
                {
                    bestThread = Threads[i];
                }
            }

            return bestThread;
        }


        /// <summary>
        /// Unblocks each thread waiting in IdleLoop by setting their <see cref="SearchThread.Searching"/> variable to true
        /// and signaling the condition variable.
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
        /// Blocks the calling thread until the MainSearchThread has finished searching.
        /// <br></br>
        /// This should only be used after calling <see cref="StartSearch"/>, and only blocks if a search is currently active.
        /// </summary>
        public void BlockCallerUntilFinished()
        {
            if (EnableAssertions)
            {
                Assert(Blocker.ParticipantCount == 1,
                    "BlockCallerUntilFinished was called, but the barrier had " + Blocker.ParticipantCount + " participants (should have been 1)! " +
                    "This can happen if any thread other than the main thread calls this method.");
            }

            if (SharedInfo.SearchActive == false)
            {
                //  Don't block if we aren't searching.
                return;
            }

            if (Blocker.ParticipantCount != 1)
            {
                //  This should never happen, but just in case we can signal once to try and unblock Blocker.
                Blocker.SignalAndWait(1);
                return;
            }

            //  The MainSearchThread is always a participant, and the calling thread is a temporary participant.
            //  The MainSearchThread will only signal if there are 2 participants, so add the calling thread.
            Blocker.AddParticipant();

            //  The MainSearchThread will signal Blocker once it has finished, so wait here until it does so.
            Blocker.SignalAndWait();

            //  We are done waiting, so remove the calling thread as a participant (now Blocker.ParticipantCount == 1)
            Blocker.RemoveParticipant();

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
