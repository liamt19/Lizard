using Lizard.Logic.NN;
using Lizard.Logic.Search.History;
using Lizard.Logic.Search.Ordering;
using System.Runtime.InteropServices;

namespace Lizard.Logic.Threads
{

    /// <summary>
    /// Represents a thread that performs searches. 
    /// 
    /// <para></para>
    /// Much of the actual thread logic in this class is based on Stockfish's Thread class
    /// (namely PrepareToSearch, WaitForThreadFinished, MainThreadSearch, and IdleLoop), the source of which is here:
    /// <br></br>
    /// https://github.com/official-stockfish/Stockfish/blob/master/src/thread.cpp
    /// <para></para>
    /// 
    /// The main differences are in using dumbed-down, explicit versions of condition_variable::wait()
    /// and having to deal with spurious wakeups because of that.
    /// 
    /// </summary>
    public unsafe class SearchThread : IDisposable
    {
        public const int CheckupMax = 512;

        private bool _Disposed = false;

        /// <summary>
        /// The number of nodes that this thread has encountered.
        /// </summary>
        public ulong Nodes;

        public ulong HardNodeLimit;

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

        public int NMPPly;

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

        public readonly bool IsMain = false;

        public readonly Position RootPosition;

        public HistoryTable History;
        public BucketCache[] CachedBuckets;
        public ulong[][] NodeTable;
        public Move* CurrentMoves;
        public PieceToHistory** Continuations;
        public SearchThreadPool AssocPool;
        public TranspositionTable TT;

        private Thread _SysThread;
        private readonly object _Mutex;
        private readonly ConditionVariable _SearchCond;
        private Barrier _InitBarrier = new Barrier(2);


        public List<RootMove> RootMoves = new List<RootMove>(20);
        public Move CurrentMove => RootMoves[PVIndex].Move;

        private PieceToHistory* NullContHist => &(History.Continuations[0][0][0][0]);
        public void ClearCurrentMoves() => new Span<Move>(CurrentMoves, MaxPly).Fill(Move.Null);
        // If it's stupid and it works, it ain't stupid.
        public void ClearContinuations() => new Span<IntPtr>(Continuations, MaxPly).Fill((nint)NullContHist);

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


            NodeTable = new ulong[SquareNB][];
            for (int sq = 0; sq < SquareNB; sq++)
            {
                NodeTable[sq] = new ulong[SquareNB];
            }

            CachedBuckets = new BucketCache[Bucketed768.INPUT_BUCKETS * 2];
            for (int i = 0; i < Bucketed768.INPUT_BUCKETS * 2; i++)
            {
                CachedBuckets[i] = new BucketCache();
            }

            //  Each thread its own position object, which lasts the entire lifetime of the thread.
            RootPosition = new Position(InitialFEN, true, this);

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

            History = new HistoryTable();

            _SysThread.Name = $"{(IsMain ? "(MAIN)" : "Search")}Thread {ThreadIdx}, ID {Environment.CurrentManagedThreadId}";

            CurrentMoves = AlignedAllocZeroed<Move>(MaxPly);
            Continuations = (PieceToHistory**)AlignedAllocZeroed((nuint)(sizeof(PieceToHistory*) * MaxPly));
            ClearContinuations();

            IdleLoop();
        }



        /// <summary>
        /// Called by the MainThread after it is woken up by a call to <see cref="SearchThreadPool.StartSearch"/>.
        /// The MainThread will wake up the other threads and notify them to begin searching, and then start searching itself. 
        /// Once the MainThread finishes searching, it will wait until all other threads have finished as well, and will then 
        /// send the search results as output to the UCI.
        /// </summary>
        public void MainThreadSearch()
        {
            TT.TTUpdate();  //  Age the TT

            AssocPool.StartThreads();  //  Start other threads (if any)
            this.Search();              //  Make this thread begin searching

            while (!AssocPool.StopThreads && AssocPool.SharedInfo.IsInfinite) { }

            //  When the main thread is done, prevent the other threads from searching any deeper
            AssocPool.StopThreads = true;

            //  Wait for the other threads to return
            AssocPool.WaitForSearchFinished();

            //  Search is finished, now give the UCI output.
            AssocPool.SharedInfo.OnSearchFinish?.Invoke(ref AssocPool.SharedInfo);
            AssocPool.SharedInfo.TimeManager.ResetTimer();

            AssocPool.SharedInfo.SearchActive = false;

            //  If the main program thread called BlockCallerUntilFinished,
            //  then the Blocker's ParticipantCount will be 2 instead of 1.
            if (AssocPool.Blocker.ParticipantCount == 2)
            {
                //  Signal that we are here, but only wait for 1 ms if the main thread isn't already waiting
                AssocPool.Blocker.SignalAndWait(1);
            }
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
                (ss + i)->Ply = (short)i;
                (ss + i)->PV = AlignedAllocZeroed<Move>(MaxPly);
            }

            ClearContinuations();
            ClearCurrentMoves();

            ResetCaches();

            for (int sq = 0; sq < SquareNB; sq++)
            {
                Array.Clear(NodeTable[sq]);
            }

            //  Create a copy of the AssocPool's root SearchInformation instance.
            SearchInformation info = AssocPool.SharedInfo;

            TimeManager tm = info.TimeManager;

            HardNodeLimit = info.NodeLimit;

            //  And set it's Position to this SearchThread's unique copy.
            //  (The Position that AssocPool.SharedInfo has right now has the same FEN, but its "Owner" field might not be correct.)
            info.Position = RootPosition;

            //  MultiPV searches will only consider the lesser between the number of legal moves and the requested MultiPV number.
            int multiPV = Math.Min(SearchOptions.MultiPV, RootMoves.Count);

            Span<int> searchScores = stackalloc int[MaxPly];
            int scoreIdx = 0;

            RootMove lastBestRootMove = new RootMove(Move.Null);
            int stability = 0;

            //  The main thread may only go up to 64
            //  Other threads can go until depth 256
            int maxDepth = IsMain ? MaxDepth : MaxPly;
            while (++RootDepth < maxDepth)
            {
                //  The main thread is not allowed to search past info.DepthLimit
                if (IsMain && RootDepth > info.DepthLimit)
                    break;

                if (AssocPool.StopThreads)
                    break;

                foreach (RootMove rm in RootMoves)
                {
                    rm.PreviousScore = rm.Score;
                }

                int usedDepth = RootDepth;

                for (PVIndex = 0; PVIndex < multiPV; PVIndex++)
                {
                    if (AssocPool.StopThreads)
                        break;

                    int alpha = AlphaStart;
                    int beta = BetaStart;
                    int window = ScoreInfinite;
                    int score = RootMoves[PVIndex].AverageScore;
                    SelDepth = 0;

                    if (usedDepth >= 5)
                    {
                        window = AspWindow;
                        alpha = Math.Max(AlphaStart, score - window);
                        beta = Math.Min(BetaStart, score + window);
                    }

                    while (true)
                    {
                        score = Searches.Negamax<RootNode>(info.Position, ss, alpha, beta, Math.Max(1, usedDepth), false);

                        StableSort(RootMoves, PVIndex);

                        if (AssocPool.StopThreads)
                            break;

                        if (score <= alpha)
                        {
                            beta = (alpha + beta) / 2;
                            alpha = Math.Max(alpha - window, AlphaStart);
                            usedDepth = RootDepth;
                        }
                        else if (score >= beta)
                        {
                            beta = Math.Min(beta + window, BetaStart);
                            usedDepth = Math.Max(usedDepth - 1, RootDepth - 5);
                        }
                        else
                            break;

                        window += window / 2;
                    }

                    StableSort(RootMoves, 0, PVIndex + 1);

                    if (IsMain && (AssocPool.StopThreads || PVIndex == multiPV - 1))
                    {
                        info.OnDepthFinish?.Invoke(ref info);
                    }
                }

                if (!IsMain)
                    continue;

                if (AssocPool.StopThreads)
                {
                    //  If we received a stop command or hit the hard time limit, our RootMoves may not have been filled in properly.
                    //  In that case, we replace the current bestmove with the last depth's bestmove
                    //  so that the move we send is based on an entire depth being searched instead of only a portion of it.
                    RootMoves[0] = lastBestRootMove;

                    for (int i = -10; i < MaxSearchStackPly; i++)
                    {
                        NativeMemory.AlignedFree((ss + i)->PV);
                    }

                    return;
                }


                if (lastBestRootMove.Move == RootMoves[0].Move)
                {
                    stability++;
                }
                else
                {
                    stability = 0;
                }

                lastBestRootMove.Move = RootMoves[0].Move;
                lastBestRootMove.Score = RootMoves[0].Score;
                lastBestRootMove.Depth = RootMoves[0].Depth;

                for (int i = 0; i < MaxPly; i++)
                {
                    lastBestRootMove.PV[i] = RootMoves[0].PV[i];
                    if (lastBestRootMove.PV[i] == Move.Null)
                    {
                        break;
                    }
                }

                searchScores[scoreIdx++] = RootMoves[0].Score;

                if (SoftTimeUp(tm, stability, searchScores[..scoreIdx]))
                {
                    break;
                }

                if (Nodes >= info.SoftNodeLimit)
                {
                    break;
                }

                if (!AssocPool.StopThreads)
                {
                    CompletedDepth = RootDepth;
                }
            }

            if (IsMain && RootDepth >= MaxDepth && info.HasNodeLimit && !AssocPool.StopThreads)
            {
                //  If this was a "go nodes x" command, it is possible for the main thread to hit the
                //  maximum depth before hitting the requested node count (causing an infinite wait).

                //  If this is the case, and we haven't been told to stop searching, then we need to stop now.
                AssocPool.StopThreads = true;
            }

            for (int i = -10; i < MaxSearchStackPly; i++)
            {
                NativeMemory.AlignedFree((ss + i)->PV);
            }
        }


        public int GetCorrection(in Position pos) => History.GetCorrection(pos);
        public void UpdateCorrections(in Position pos, int diff, int depth) => History.UpdateCorrections(pos, diff, depth);

        public int GetPlyHistory(short ply, Move m)
        {
            if (ply >= PlyHistoryTable.MaxPlies) return 0;

            var hist = History.GetPlyHistory(ply, m);
            return ((2 * PlyHistoryTable.MaxPlies + 1) * hist) / (2 * ply + 1);
        }

        public short GetMainHistory(int stm, Move m) => History.GetMainHistory(stm, m);
        public short GetNoisyHistory(int movingPiece, int dstSq, int capPieceType) => History.GetNoisyHistory(movingPiece, dstSq, capPieceType);
        public short GetContinuationEntry(short ply, int i, int piece, int dstSq) => HistoryTable.GetContinuationEntry(Continuations, ply, i, piece, dstSq);

        public void UpdateMainHistory(int stm, Move m, int bonus) => History.UpdateMainHistory(stm, m, bonus);
        public void UpdateContinuations(short ply, int piece, int dstSq, int bonus, bool inCheck) => HistoryTable.UpdateContinuations(Continuations, CurrentMoves, ply, piece, dstSq, bonus, inCheck);
        public void UpdateQuietScore(short ply, int piece, Move move, int bonus, bool inCheck) => History.UpdateQuietScore(Continuations, CurrentMoves, ply, piece, move, bonus, inCheck);
        public void UpdateNoisyHistory(int movingPiece, int dstSq, int capturedPiece, int bonus) => History.UpdateNoisyHistory(movingPiece, dstSq, capturedPiece, bonus);


        private static ReadOnlySpan<double> StabilityCoefficients => [2.2, 1.6, 1.4, 1.1, 1, 0.95, 0.9];
        private static readonly int StabilityMax = StabilityCoefficients.Length - 1;

        private bool SoftTimeUp(TimeManager tm, int stability, Span<int> searchScores)
        {
            if (!tm.HasSoftTime)
                return false;

            //  Base values taken from Clarity
            double multFactor = 1.0;
            if (RootDepth > 7)
            {
                double nodeTM = ((1.5 - NodeTable[RootMoves[0].Move.From][RootMoves[0].Move.To] / (double)Nodes) * 1.75);
                double bmStability = StabilityCoefficients[Math.Min(stability, StabilityMax)];

                double scoreStability = searchScores[searchScores.Length - 1 - 3] 
                                      - searchScores[searchScores.Length - 1 - 0];

                scoreStability = Math.Max(0.85, Math.Min(1.15, 0.034 * scoreStability));

                multFactor = nodeTM * bmStability * scoreStability;
            }

            if (tm.GetSearchTime() >= tm.SoftTimeLimit * multFactor)
            {
                return true;
            }

            return false;
        }


        public void ResetCaches()
        {
            for (int bIdx = 0; bIdx < CachedBuckets.Length; bIdx++)
            {
                ref BucketCache bc = ref CachedBuckets[bIdx];
                bc.Accumulator.ResetWithBiases(Bucketed768.Net.FTBiases, sizeof(short) * Bucketed768.L1_SIZE);
                bc.Boards[White].Reset();
                bc.Boards[Black].Reset();
            }
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

                if (Searching)
                {
                    ///  Spurious wakeups are possible here if <see cref="SearchThreadPool.StartSearch"/> is called
                    ///  again before this thread has returned to IdleLoop.
                    _SearchCond.Pulse();
                    Thread.Yield();
                }
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
        /// Frees up the memory that was allocated to this SearchThread.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                Assert(Searching == false, "The thread {ToString()} had its Dispose({disposing}) method called Searching was {Searching}!");

                //  Set quit to True, and pulse the condition to allow the thread in IdleLoop to exit.
                Quit = true;

                PrepareToSearch();
            }


            //  And free up the memory we allocated for this thread.
            History.Dispose();

            //  Destroy the underlying system thread
            _SysThread.Join();

            _Disposed = true;
        }

        /// <summary>
        /// Calls the class destructor, which will free up the memory that was allocated to this SearchThread.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);

            //  We handled the finalization ourselves, so tell the GC not to worry about it.
            GC.SuppressFinalize(this);
        }

        ~SearchThread()
        {
            Dispose(false);
        }


        public override string ToString() => $"[{(_SysThread != null ? _SysThread.Name : "NULL?")} (caller ID {Environment.CurrentManagedThreadId})]";
    }
}
