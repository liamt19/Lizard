using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Timer = System.Timers.Timer;

using static System.Formats.Asn1.AsnWriter;
using static LTChess.Search.SearchConstants;
using LTChess.Data;
using LTChess.Util;

namespace LTChess.Search
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
        private int LastBestScore = Evaluation.ScoreDraw;

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
            SearchInformation rootInfo = new SearchInformation(rootPosition, depth, 3000);

            Move[] list = new Move[NormalListCapacity];
            int size = rootPosition.GenAllLegalMovesTogether(list);
            SimpleSearch.SortByMoveScores(ref rootInfo, list, size, Move.Null);

            SearchInformation[] searchInfos = new SearchInformation[size];
            for (int i = 0; i < size; i++)
            {
                searchInfos[i] = SearchInformation.Clone(rootInfo);
                searchInfos[i].Position = new Position(rootPosition.GetFEN());
                searchInfos[i].Position.MakeMove(list[i]);
                searchInfos[i].IsMultiThreaded = true;

            }

            Log("Made " + searchInfos.Length + " searchInfos with " + threads + " threads for " + size + " legal moves at depth " + depth);

            ulong totalNodes = 0;

            List<(int index, int resultEval, string resultPV)> results = new List<(int, int, string)>(size);

            ParallelOptions opts = new ParallelOptions();
            opts.MaxDegreeOfParallelism = threads;

            ThreadedSearch.TotalSearchTime = Stopwatch.StartNew();

            Parallel.For(0, size, opts, (i) =>
            {
                SearchInformation thisInfo = searchInfos[i];
                ThreadedSearch thisSearchThread = new ThreadedSearch(thisInfo);

                if (ThreadedSearch.TotalSearchTime.Elapsed.TotalMilliseconds >= (rootInfo.MaxSearchTime - ThreadedSearch.TimerBuffer))
                {
                    Log(Thread.CurrentThread.ManagedThreadId + " ->\tran out of time before being searched!");
                }
                else
                {
                    Log(Thread.CurrentThread.ManagedThreadId + " ->\thas " + thisInfo.Position.Moves.Peek().ToString() + ", MaxDepth = " + thisInfo.MaxDepth);
                    thisSearchThread.StartSearching();
                    Log(Thread.CurrentThread.ManagedThreadId + " ->\tLine: " + thisInfo.GetPVString() + " = " + FormatMoveScore(thisInfo.BestScore));
                    totalNodes += thisInfo.NodeCount;
                    results.Add((i, thisInfo.BestScore, thisInfo.GetPVString()));
                }
            });

            ThreadedSearch.TotalSearchTime.Reset();

            Log("\n\n");
            var sorted = results.OrderBy(x => x.resultEval).ToArray();
            for (int i = 0; i < results.Count; i++)
            {
                Log(list[i].ToString(rootPosition) + "\tEval\t" + sorted[i].resultEval + " Line " + sorted[i].resultPV);
            }

            return totalNodes;
        }


        public void StartSearching() => this.StartSearching(this.info);

        public void StartSearching(SearchInformation info)
        {
            //TotalSearchTime.Restart();

            int depth = 1;
            int maxDepthStart = info.MaxDepth;
            double maxTime = info.MaxSearchTime;
            int playerTimeLeft = info.PlayerTimeLeft;

            info.RootPositionMoveCount = info.Position.Moves.Count;

            StopSearchingDelegate stopSearchingDelegate = info.DoStopSearching;
            SetLastMoveDelegate setLastMoveDelegate = info.SetLastMove;
            CallSearchFinishDelegate callSearchFinishDelegate = info.CallSearchFinish;

            bool continueDeepening = true;
            info.NodeCount = 0;

            //  Start checking the search time
            SearchDurationTimer = new Timer(TimerTickInterval);
            SearchDurationTimer.Start();
            SearchDurationTimer.Elapsed += (_, _) =>
            {
                //  This will stop the search early if:
                //  We are about to (or did) go over the max time, OR
                //  We are at/past the depth to begin checking AND
                //  We were told to search for more time than we have left AND
                //  We now have less time than the low time threshold
                if (TotalSearchTime.Elapsed.TotalMilliseconds > (maxTime - TimerTickInterval - TimerBuffer))
                {
                    Log("WARN Stopping early at depth " + depth + ", time: " + TotalSearchTime.Elapsed.TotalMilliseconds + " > (maxtime: " + maxTime + " - interval: " + TimerTickInterval + " - TimerBuffer: " + TimerBuffer + "), current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));

                    SearchDurationTimer?.Stop();
                    stopSearchingDelegate();
                    setLastMoveDelegate(LastBestMove, LastBestScore);
                    //TotalSearchTime.Reset();
                    callSearchFinishDelegate();
                    SearchDurationTimer?.Dispose();
                }
                else if ((depth >= SearchLowTimeMinDepth && maxTime > playerTimeLeft && (playerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds) < SearchLowTimeThreshold))
                {
                    Log("WARN Stopping early at depth " + depth + ", maxTime: " + maxTime + " > playerTimeLeft: " + playerTimeLeft + " and (playerTimeLeft - time): (" + playerTimeLeft + " - " + TotalSearchTime.Elapsed.TotalMilliseconds + ") = " + (playerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds) + ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));

                    SearchDurationTimer?.Stop();
                    stopSearchingDelegate();
                    setLastMoveDelegate(LastBestMove, LastBestScore);
                    //TotalSearchTime.Reset();
                    callSearchFinishDelegate();
                    SearchDurationTimer?.Dispose();
                }
            };

            int alpha = AlphaStart;
            int beta = BetaStart;

            bool aspirationFailed = false;

            while (continueDeepening)
            {
                info.MaxDepth = depth;

                if (UseAspirationWindows && !aspirationFailed && depth > 1)
                {
                    alpha = LastBestScore - (AspirationWindowMargin + (depth * AspirationMarginPerDepth));
                    beta = LastBestScore + (AspirationWindowMargin + (depth * AspirationMarginPerDepth));
#if DEBUG
                    //Log("Depth " + depth + " aspiration bounds are [A: " + alpha + ", eval: " + LastBestScore + ", B: " + beta + "]");
#endif
                }

                aspirationFailed = false;
                ulong prevNodes = info.NodeCount;
                Deepen(ref info, alpha, beta);
                ulong afterNodes = info.NodeCount;

                if (info.StopSearching)
                {
                    Log("Received StopSearching command just after Deepen at depth " + depth);

                    SearchDurationTimer?.Stop();
                    //TotalSearchTime.Reset();
                    SearchDurationTimer?.Dispose();

                    return;
                }

                if (UseAspirationWindows && (info.BestScore <= alpha || info.BestScore >= beta))
                {
                    //  Redo the search with the default bounds, at the same depth.
                    alpha = AlphaStart;
                    beta = BetaStart;

                    //  TODO: not sure if engines are supposed to include nodes that they are searching again in this context.
                    info.NodeCount -= (afterNodes - prevNodes);

                    aspirationFailed = true;

#if DEBUG
                    //Log("Depth " + depth + " failed aspiration bounds, got " + info.BestScore);
#endif
                    continue;
                }

                info.OnDepthFinish?.Invoke(info);

                if (continueDeepening && Evaluation.IsScoreMate(info.BestScore, out int mateIn))
                {
                    Log(info.BestMove.ToString(info.Position) + " forces mate in " + mateIn + ", aborting at depth " + depth + " after " + TotalSearchTime.Elapsed.TotalSeconds + " seconds");
                    //TotalSearchTime.Reset();
                    SearchDurationTimer?.Stop();
                    info.OnSearchFinish?.Invoke(info);
                    return;
                }

                if (info.StopSearching)
                {
                    Log("WARN Received StopSearching command late, aborting at depth " + depth + " after " + TotalSearchTime.Elapsed.TotalSeconds + " seconds");

                    //  We ran out of time before completely searching this depth, and might've missed something critical.
                    //  If we found a different BestMove before being interrupted, just give the previous best one instead.
                    if (!info.BestMove.Equals(LastBestMove))
                    {
                        if (info.Position.bb.IsPseudoLegal(LastBestMove) && info.Position.IsLegal(LastBestMove))
                        {
                            Log("Reverting to previous best move " + LastBestMove + " = " + LastBestScore + "cp instead of " + info.BestMove + " = " + info.BestScore + "cp ");

                            info.BestMove = LastBestMove;
                            info.BestScore = LastBestScore;
                        }
                        else
                        {
                            //  This shouldn't happen.
                            Log("Tried reverting to previous best, but it wasn't legal!");
                            Log("illegal previous best move " + LastBestMove + " = " + LastBestScore + "cp, current best is " + info.BestMove + " = " + info.BestScore + "cp ");
                        }

                    }

                    //TotalSearchTime.Reset();
                    SearchDurationTimer?.Stop();
                    info.OnSearchFinish?.Invoke(info);
                    return;
                }

                depth++;
                LastBestMove = info.BestMove;
                LastBestScore = info.BestScore;

                if (allowDepthIncrease && TotalSearchTime.Elapsed.TotalMilliseconds < SearchLowTimeThreshold && depth == maxDepthStart)
                {
                    maxDepthStart++;
                    Log("Extended search depth to " + (maxDepthStart - 1));
                }

                continueDeepening = (depth <= maxDepthStart && TotalSearchTime.Elapsed.TotalMilliseconds <= maxTime);
            }

            //TotalSearchTime.Reset();
            SearchDurationTimer?.Stop();
            info.OnSearchFinish?.Invoke(info);
        }

        public void Deepen(ref SearchInformation info, int alpha, int beta)
        {
            double beforeTime = TotalSearchTime.Elapsed.TotalMilliseconds;
            //TotalSearchTime.Start();
            int score = FindBest(ref info, alpha, beta, info.MaxDepth);
            //TotalSearchTime.Stop();
            info.SearchTime = beforeTime - TotalSearchTime.Elapsed.TotalMilliseconds;
            info.BestScore = score;
        }

        [MethodImpl(Inline)]
        public int FindBest(ref SearchInformation info, int alpha, int beta, int depth, int extensions = 0)
        {
            if (info.StopSearching || info.NodeCount >= info.MaxNodes)
            {
                info.StopSearching = true;
                return 0;
            }

            if (depth <= 0)
            {
                return FindBestQuiesce(ref info, alpha, beta, depth);
            }

            info.NodeCount++;

            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            Move BestMove = Move.Null;
            int startingAlpha = alpha;
            bool isPV = true;
            bool isPVSearch = (beta - alpha > 1);

            bool isFutilityPrunable = false;
            int staticEval = 0;
            if (UseFutilityPruning)
            {
                isFutilityPrunable = SimpleSearch.CanFutilityPrune((pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck), false, alpha, beta, depth);

                ETEntry etEntry = EvaluationTable.Probe(posHash);
                if (etEntry.key == EvaluationTable.InvalidKey || !etEntry.Validate(posHash) || etEntry.score == ETEntry.InvalidScore)
                {
                    staticEval = tdEval.Evaluate(pos, pos.ToMove);
                    EvaluationTable.Save(posHash, (short)staticEval);
                }
                else
                {
                    staticEval = etEntry.score;
                }
            }


            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = pos.GenAllLegalMovesTogether(list);

            //  No legal moves, is this checkmate or a draw?
            if (size == 0)
            {
                if (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck)
                {
                    return info.MakeMateScore();
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            SimpleSearch.SortByMoveScores(ref info, list, size, ttEntry.BestMove);

            for (int i = 0; i < size; i++)
            {
                int score;

                if (info.StopSearching)
                {
                    return 0;
                }

                if (UseFutilityPruning && isFutilityPrunable)
                {
                    if (!list[i].Capture && !isPV)
                    {
                        if (staticEval + SimpleSearch.GetFutilityMargin(depth) < alpha)
                        {
                            //#if DEBUG
                            SearchStatistics.FutilityPrunedNoncaptures++;
                            //#endif
                            continue;
                        }

                    }
                }

                bool kingCheckExtension = (bb.GetPieceAtIndex(list[i].from) == Piece.King && (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck));

                pos.MakeMove(list[i]);

                int reduction = 0;
                int extension = 0;

                if (UseSearchExtensions)
                {
                    if (bb.GetPieceAtIndex(list[i].from) == Piece.Pawn && bb.IsPasser(list[i].from) && DistanceFromPromotion(list[i].from, pos.ToMove) <= SearchConstants.PassedPawnExtensionDistance)
                    {
                        //  Passed pawns get an extension

                        if (extensions < SearchConstants.MaxExtensions)
                        {
                            //extension = 1;
                            extension = SearchConstants.PassedPawnExtensionDistance -
                                DistanceFromPromotion(list[i].from, pos.ToMove) + 1;

                            //#if DEBUG
                            SearchStatistics.ExtensionsPassedPawns++;
                            //#endif
                        }
                        else
                        {
                            //#if DEBUG
                            SearchStatistics.SearchMaxExtensionsReached++;
                            //#endif
                        }

                    }
                    else if (kingCheckExtension)
                    {
                        extension = 1;

                        //#if DEBUG
                        SearchStatistics.ExtensionsMovesInCheck++;
                        //#endif
                    }

                    //#if DEBUG
                    SearchStatistics.SearchExtensionTotalPlies += (ulong)extension;
                    //#endif
                }

                if (info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial())
                {
                    //  Instead of looking further and probably breaking something,
                    //  Just evaluate this move as a draw here and keep looking at the others.
                    score = -Evaluation.ScoreDraw;
                }
                else if (isPV)
                {
                    score = -FindBest(ref info, -beta, -alpha, depth - 1 + extension, extensions + extension);
                    isPV = false;
                }
                else
                {
                    if (SimpleSearch.CanLateMoveReduce(ref info, list[i], depth, isPV))
                    {
                        reduction = SimpleSearch.GetLateMoveReductionAmount(size, i, depth);
                        //#if DEBUG
                        SearchStatistics.LMRReductions++;
                        //#endif
                    }

                    score = -FindBest(ref info, -alpha - 1, -alpha, depth - 1 - reduction + extension, extensions + extension);
                    if (score > alpha)
                    {
                        //  If the alpha from the previous search went above the -alpha - 1,
                        //  We will have to redo the search


                        if (isPV || isPVSearch)
                        {
                            score = -FindBest(ref info, -beta, -alpha, depth - 1 + extension, extensions + extension);

                            //#if DEBUG
                            SearchStatistics.LMRReductionResearchesPV++;
                            //#endif
                        }
                        else if (reduction != 0)
                        {
                            score = -FindBest(ref info, -beta, -alpha, depth - 1 + extension, extensions + extension);

                            //#if DEBUG
                            SearchStatistics.LMRReductionResearches++;
                            //#endif
                        }

                    }

                }

                pos.UnmakeMove();

                if (score >= beta)
                {
                    //TranspositionTable.Save(posHash, (short)score, NodeType.Beta, depth, BestMove);
#if DEBUG
                    SearchStatistics.BetaCutoffs++;
#endif
                    return beta;
                }

                if (score > alpha)
                {
                    alpha = score;
                    BestMove = list[i];
                }
            }

            //  We want to replace entries that we have searched to a higher depth since the new evaluation will be more accurate.
            //  But this is only done if the TTEntry was null (move hadn't already been looked at)
            //  or we are sure that this move changes the alpha score after searching at a higher depth
            bool setTT = (ttEntry.Depth <= depth && (ttEntry.NodeType == NodeType.Invalid || alpha != startingAlpha));
            if (setTT)
            {
                NodeType nodeType;
                if (alpha <= startingAlpha)
                {
                    nodeType = NodeType.Alpha;
                }
                else if (alpha >= beta)
                {
                    nodeType = NodeType.Beta;
                }
                else
                {
                    nodeType = NodeType.Exact;
                }
                TranspositionTable.Save(posHash, (short)alpha, nodeType, depth, BestMove);

                //  TODO this looks misplaced
                info.BestMove = BestMove;
                info.BestScore = alpha;
            }



            return alpha;
        }

        [MethodImpl(Inline)]
        public int FindBestQuiesce(ref SearchInformation info, int alpha, int beta, int curDepth)
        {
            if (info.StopSearching)
            {
                return 0;
            }

            info.NodeCount++;

            int standingPat;

#if DEBUG
            SearchStatistics.QuiescenceNodes++;
#endif

            ulong posHash = info.Position.Hash;
            ETEntry stored = EvaluationTable.Probe(posHash);
            if (stored.key != EvaluationTable.InvalidKey)
            {
                if (stored.Validate(posHash))
                {
                    //  Use stored evaluation
                    standingPat = stored.score;
                    //#if DEBUG
                    SearchStatistics.ETHits++;
                    //#endif
                }
                else
                {
                    //#if DEBUG
                    SearchStatistics.ETWrongHashKey++;
                    //#endif
                    //  This is the lower bound for the score
                    standingPat = tdEval.Evaluate(info.Position, info.Position.ToMove);
                    EvaluationTable.Save(posHash, (short)standingPat);
                }
            }
            else
            {
                //  This is the lower bound for the score
                standingPat = tdEval.Evaluate(info.Position, info.Position.ToMove);
                EvaluationTable.Save(posHash, (short)standingPat);
            }


            if (standingPat >= beta)
            {
                return beta;
            }

            if (alpha < standingPat)
            {
                alpha = standingPat;
            }

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = info.Position.GenAllLegalMovesTogether(list);


            if (size == 0)
            {
                if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck)
                {
                    return info.MakeMateScore();
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            //SimpleSearch.SortByMoveScores(ref info, list, size, Move.Null);
            int numCaps = SortByCaptureValue(info.Position.bb, list, size);

            for (int i = 0; i < numCaps; i++)
            {
                if (UseDeltaPruning)
                {
                    int theirPieceVal = Evaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].to]);

                    if (standingPat + theirPieceVal + DeltaPruningMargin < alpha)
                    {
                        //Log("Skipping " + list[i].ToString(info.Position) + " because " + "standingPat: " + standingPat + " + theirPieceVal: " + theirPieceVal + " + Margin: " + DeltaPruningMargin + " < alpha: " + alpha);
                        break;
                    }
                }


                info.Position.MakeMove(list[i]);

                if (info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial())
                {
                    info.Position.UnmakeMove();
                    return -Evaluation.ScoreDraw;
                }

                //  Keep making moves until there aren't any captures left.
                var score = -FindBestQuiesce(ref info, -beta, -alpha, curDepth - 1);
                info.Position.UnmakeMove();

                if (score > alpha)
                {
                    alpha = score;
                }

                if (score >= beta)
                {
                    return beta;
                }
            }

            return alpha;
        }

        [MethodImpl(Inline)]
        public static int SortByCaptureValue(in Bitboard bb, in Span<Move> list, int size)
        {
            Span<int> scores = stackalloc int[size];
            int numCaps = 0;
            for (int i = 0; i < size; i++)
            {
                if (list[i].Capture)
                {
                    int theirPieceVal = Evaluation.GetPieceValue(bb.PieceTypes[list[i].to]);
                    scores[i] = theirPieceVal;
                    numCaps++;
                }
            }

            int max;
            for (int i = 0; i < size - 1; i++)
            {
                max = i;
                for (int j = i + 1; j < size; j++)
                {
                    if (scores[j] > scores[max])
                    {
                        max = j;
                    }
                }

                Move tempMove = list[i];
                list[i] = list[max];
                list[max] = tempMove;

                int tempScore = scores[i];
                scores[i] = scores[max];
                scores[max] = tempScore;
            }

            return numCaps;
        }

    }
}
