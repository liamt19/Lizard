using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.Search.Ordering;

using static System.Formats.Asn1.AsnWriter;

namespace LTChess.Logic.Threads
{
    public unsafe class SearchThread
    {
        public const int CheckupMax = 512;

        /// <summary>
        /// The number of nodes that this thread has encountered.
        /// </summary>
        public ulong Nodes;

        /// <summary>
        /// The index of this thread within the SearchPool, which starts at 0 for the MainSearchThread, 
        /// 1 for the first non-main thread, etc.
        /// </summary>
        public int ThreadIdx;

        /// <summary>
        /// The index of the root move that this thread is currently on, if MultiPV is enabled.
        /// Otherwise, this will only ever be 0, which is the index that the best move will be in this thread's RootMoves.
        /// </summary>
        public int PVIndex;

        /// <summary>
        /// The current depth that this thread is calling Negamax with.
        /// </summary>
        public int RootDepth;

        /// <summary>
        /// The maximum depth that this thread has reached in Negamax, 
        /// which will generally be a bit above RootDepth due to search extensions.
        /// </summary>
        public int SelDepth;

        /// <summary>
        /// The highest depth that this thread has completed.
        /// </summary>
        public int CompletedDepth;

        /// <summary>
        /// When this number reaches <see cref="CheckupMax"/>, the main thread will check to see if the search 
        /// needs to stop because of max time or max node constraints.
        /// <br></br>
        /// Only used by the main thread.
        /// </summary>
        public int CheckupCount = 0;

        /// <summary>
        /// Set to true while this thread should be searching, and false when it should be blocked in IdleLoop.
        /// <para></para>
        /// Even if this is true, this thread could still be idle if it is currently blocked somewhere. 
        /// The only guarantee about this bool is that the thread will (hopefully) obey the condition in the near future.
        /// </summary>
        public bool Searching;

        /// <summary>
        /// If true, then this thread will terminate itself in the IdleLoop once it is no longer blocked.
        /// </summary>
        public bool Quit;

        /// <summary>
        /// Set to true if this thread is the first one created, with a <see cref="ThreadIdx"/> of 0.
        /// </summary>
        public readonly bool IsMain = false;

        /// <summary>
        /// A unique copy of the root position for the search.
        /// </summary>
        public Position RootPosition;

        /// <summary>
        /// A unique list of unique RootMoves (scored moves from the starting position) that this thread can search.
        /// <para></para>
        /// Threads must get their own List, and the RootMoves within that list must be a personal copy as well.
        /// </summary>
        public List<RootMove> RootMoves = new List<RootMove>(20);


        /// <summary>
        /// A stack of Accumulators that this thread will use when it gets NNUE evaluation.
        /// </summary>
        public AccumulatorPSQT[] Accumulators;
        public int AccumulatorIndex;
        public ref AccumulatorPSQT CurrentAccumulator => ref Accumulators[AccumulatorIndex];

        /// <summary>
        /// The unique history heuristic table for this thread.
        /// </summary>
        public HistoryTable History;


        /// <summary>
        /// The system Thread that this SearchThread is running on.
        /// </summary>
        private Thread _SysThread;

        /// <summary>
        /// The mutex that <see cref="_SearchCond"/> signals when <see cref="Searching"/>'s value changes.
        /// </summary>
        private readonly object _Mutex;

        /// <summary>
        /// A condition associated with <see cref="Searching"/>. The main thread may call <see cref="WaitForThreadFinished"/> and 
        /// block until this thread finishes its search and signals that accordingly.
        /// </summary>
        private readonly ConditionVariable _SearchCond;

        /// <summary>
        /// Prevents the main program thread from using this thread before it is initialized.
        /// </summary>
        private Barrier _InitBarrier = new Barrier(2);

        public string FriendlyName => _SysThread.Name;

        public SearchThread(int idx)
        {
            ThreadIdx = idx;
            if (ThreadIdx == 0)
            {
                IsMain = true;
            }

            _Mutex = "Mut" + ThreadIdx;
            _SearchCond = new ConditionVariable();
            Searching = true;

            _SysThread = new Thread(ThreadInit);

            //  Start the new thread, which will enter into ThreadInit --> IdleLoop
            _SysThread.Start();

            //  Wait here until the new thread signals that it is ready.
            _InitBarrier.SignalAndWait();

            WaitForThreadFinished();

            //  This isn't necessary but doesn't hurt either.
            _InitBarrier.RemoveParticipant();
        }


        /// <summary>
        /// Initializes this thread's Accumulators and history heuristic data.
        /// </summary>
        public void ThreadInit()
        {
            Quit = false;

            Accumulators = new AccumulatorPSQT[MaxPly];
            for (int i = 0; i < MaxPly; i++)
            {
                Accumulators[i] = new AccumulatorPSQT();
            }
            AccumulatorIndex = 0;

            History = new HistoryTable();

            _SysThread.Name = "SearchThread " + ThreadIdx + ", ID " + Environment.CurrentManagedThreadId;
            if (IsMain)
            {
                _SysThread.Name = "(MAIN)Thread " + ThreadIdx + ", ID " + Environment.CurrentManagedThreadId;
            }

            IdleLoop();
        }



        /// <summary>
        /// Sets this thread's <see cref="Searching"/> variable to true, which will cause the thread in the IdleLoop to
        /// call the search function once it wakes up.
        /// </summary>
        public void PrepareToSearch()
        {
            Monitor.Enter(_Mutex);
            Searching = true;
            Monitor.Exit(_Mutex);

            _SearchCond.Pulse();
        }


        /// <summary>
        /// Blocks the calling thread until this SearchThread has exited its search call 
        /// and has returned to the beginning of its IdleLoop.
        /// </summary>
        public void WaitForThreadFinished()
        {
            if (_Mutex == null)
            {
                //  Asserting that _Mutex has been initialized properly
                throw new Exception("Thread " + Thread.CurrentThread.Name + " tried accessing the Mutex of " + this.ToString() + ", but Mutex was null!");
            }

            Monitor.Enter(_Mutex);

            while (Searching)
            {
                _SearchCond.Wait(_Mutex);

                //  Spurious wakeups might be possible here?, but I haven't seen any occur so this doesn't need any special handling.
            }

            Monitor.Exit(_Mutex);
        }


        /// <summary>
        /// The main loop that threads will be in while they are not currently searching.
        /// Threads enter here after they have been initialized and do not leave until their thread is terminated.
        /// </summary>
        public void IdleLoop()
        {
            //  Let the main thread know that this thread is initialized and ready to go.
            _InitBarrier.SignalAndWait();

            while (true)
            {
                Monitor.Enter(_Mutex);
                Searching = false;
                _SearchCond.Pulse();

                while (!Searching)
                {
                    //  Wait here until we are notified of a change in Searching's state.
                    _SearchCond.Wait(_Mutex);
                    if (!Searching)
                    {
                        //  This was a spurious wakeup since Searching's state has not changed.

                        //  Another thread was waiting on this signal but the OS gave it to this thread instead.
                        //  We can pulse the condition again, yield, and hope that the OS gives it to the thread that actually needs it
                        _SearchCond.Pulse();
                        Thread.Yield();
                    }
                    
                }

                if (Quit)
                    return;

                Monitor.Exit(_Mutex);

                if (IsMain)
                {
                    MainThreadSearch();
                }
                else
                {
                    Search();
                }
            }
        }



        /// <summary>
        /// Called by the MainThread after it is woken up by a call to <see cref="SearchThreadPool.StartSearch"/>.
        /// The MainThread will wake up the other threads and notify them to begin searching, and then start searching itself. 
        /// Once the MainThread finishes searching, it will wait until all other threads have finished as well, and will then 
        /// send the search results as output to the UCI.
        /// </summary>
        public void MainThreadSearch()
        {
            TranspositionTable.TTUpdate();  //  Age the TT

            SearchPool.StartThreads();  //  Start other threads (if any)
            this.Search();              //  Make this thread begin searching

            while (!SearchPool.StopThreads && (SearchPool.SharedInfo.IsInfinite)) { }

            //  When the main thread is done, prevent the other threads from searching any deeper
            SearchPool.StopThreads = true;

            //  Wait for the other threads to return
            SearchPool.WaitForSearchFinished();

            //  Search is finished, now give the UCI output.
            SearchPool.SharedInfo.OnSearchFinish?.Invoke(ref SearchPool.SharedInfo);
            SearchPool.SharedInfo.TimeManager.ResetTimer();

            SearchPool.SharedInfo.SearchActive = false;
        }

        /// <summary>
        /// Main deepening loop for threads. This is essentially the same as the old "StartSearching" method that was used.
        /// </summary>
        public void Search()
        {
            SearchStackEntry* _SearchStackBlock = stackalloc SearchStackEntry[MaxPly];
            SearchStackEntry* ss = _SearchStackBlock + 10;
            for (int i = -10; i < MaxSearchStackPly; i++)
            {
                (ss + i)->Clear();
                (ss + i)->Ply = i;
                (ss + i)->ContinuationHistory = History.Continuations[0][0][0, 0, 0];
            }

            Move* PV = stackalloc Move[MaxPly];
            ss->PV = PV;

            //  Create a copy of the SearchPool's root SearchInformation instance.
            SearchInformation info = SearchPool.SharedInfo;

            //  And set it's Position to this SearchThread's unique copy.
            //  (The Position that SearchPool.SharedInfo has right now has the same FEN, but its "Owner" field might not be correct.)
            info.Position = RootPosition;

            //  MultiPV searches will only consider the lesser between the number of legal moves and the requested MultiPV number.
            int multiPV = Math.Min(SearchConstants.MultiPV, RootMoves.Count);



            //  The main thread may only go up to 64
            //  Other threads can go until depth 256
            int maxDepth = IsMain ? MaxDepth : MaxPly;
            while (++RootDepth < maxDepth)
            {
                //  The main thread is not allowed to search past info.MaxDepth
                if (IsMain && RootDepth > info.MaxDepth)
                    break;

                if (SearchPool.StopThreads)
                    break;

#if DEBUG
                if (RootDepth > MaxDepth)
                {
                    Log("WARN thread " + this.FriendlyName + " is still in while loop with RootDepth = " + RootDepth);
                }
#endif

                foreach (RootMove rm in RootMoves)
                {
                    rm.PreviousScore = rm.Score;
                }

                for (PVIndex = 0; PVIndex < multiPV; PVIndex++)
                {
                    if (SearchPool.StopThreads)
                        break;

                    int alpha = AlphaStart;
                    int beta = BetaStart;
                    int window = ScoreInfinite;
                    int score = RootMoves[PVIndex].PreviousScore;
                    SelDepth = 0;

                    if (RootDepth >= 5)
                    {
                        alpha = Math.Max(AlphaStart, score - window);
                        beta = Math.Min(BetaStart, score + window);
                        window = 10;
                    }

                    while (true)
                    {
                        score = Logic.Search.Search.Negamax<RootNode>(ref info, ss, alpha, beta, Math.Max(1, RootDepth), false);

                        StableSort(ref RootMoves, PVIndex);

                        if (SearchPool.StopThreads)
                            break;

                        if (score <= alpha)
                        {
                            beta = (alpha + beta) / 2;
                            alpha = Math.Max(alpha - window, AlphaStart);

                        }
                        else if (score >= beta)
                        {
                            beta = Math.Min(beta + window, BetaStart);

                            if (Math.Abs(score) < ScoreTTWin)
                            {
                                RootDepth--;
                            }
                        }
                        else
                            break;

                        window += window / 4;
                    }

                    StableSort(ref RootMoves, 0);

                    if (IsMain && (SearchPool.StopThreads || PVIndex == multiPV - 1 || info.TimeManager.GetSearchTime() > 3000))
                    {
                        info.OnDepthFinish?.Invoke(ref info);
                    }
                }

                if (!SearchPool.StopThreads)
                {
                    CompletedDepth = RootDepth;
                }
            }
        }

        /// <summary>
        /// To be called when the SearchThreadPool can be resized...
        /// </summary>
        public void Dispose()
        {
            Debug.Assert(Searching == false);

            //  Set quit to True, and pulse the condition to allow the thread in IdleLoop to exit.
            Quit = true;
            PrepareToSearch();

            //  Destroy the underlying system thread
            _SysThread.Join();

            //  And free up the memory we allocated for this thread.
            History.Dispose();
        }

        /// <summary>
        /// Resets this SearchThread's Accumulator and history heuristics to their defaults.
        /// </summary>
        public void Clear()
        {
            AccumulatorIndex = 0;

            NativeMemory.Clear(History.MainHistory, sizeof(short) * HistoryTable.MainHistoryElements);
            NativeMemory.Clear(History.CaptureHistory, sizeof(short) * HistoryTable.CaptureHistoryElements);

            for (int i = 0; i < 2; i++)
            {
                History.Continuations[i][0].Clear();
                History.Continuations[i][1].Clear();
            }
        }


        public override string ToString()
        {
            return "[" + (_SysThread != null ? _SysThread.Name : "NULL?") + " (caller ID " + Environment.CurrentManagedThreadId + ")]";
        }
    }
}
