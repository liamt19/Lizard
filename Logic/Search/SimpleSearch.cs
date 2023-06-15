using System;
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
    public static class SimpleSearch
    {
        /// <summary>
        /// Check if we have reached/exceeded the maximum search time every x milliseconds
        /// </summary>
        public const int TimerTickInterval = 100;

        /// <summary>
        /// A timer that checks if the search time has reached/exceeded the maximum every <c>TimerTickInterval</c> milliseconds.
        /// </summary>
        private static Timer SearchDurationTimer;

        /// <summary>
        /// Keeps track of the time spent on during the entire search
        /// </summary>
        private static Stopwatch TotalSearchTime = Stopwatch.StartNew();

        /// <summary>
        /// The best move found in the previous call to Deepen.
        /// </summary>
        private static Move LastBestMove = Move.Null;

        /// <summary>
        /// The evaluation of that best move.
        /// </summary>
        private static int LastBestScore = Evaluation.ScoreDraw;

        //  These are inconvenient but work
        delegate void CallSearchFinishDelegate();
        delegate void StopSearchingDelegate();
        delegate void SetLastMoveDelegate(Move move, int score);

        /// <summary>
        /// Begin a new search with the parameters in <paramref name="info"/>.
        /// This performs iterative deepening, which searches at higher and higher depths as time goes on.
        /// <br></br>
        /// If <paramref name="allowDepthIncrease"/> is true, then the search will continue above the requested maximum depth
        /// so long as there is still search time remaining.
        /// </summary>
        public static void StartSearching(ref SearchInformation info, bool allowDepthIncrease = false)
        {
            //  TODO: Clearing the TT for new searches
            //  to prevent illegal moves defeats the purpose.
            TranspositionTable.Clear();
            TotalSearchTime.Restart();

            int depth = 1;
            int maxDepthStart = info.MaxDepth;
            double maxTime = info.MaxSearchTime;
            int playerTimeLeft = info.PlayerTimeLeft;
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
                if (TotalSearchTime.Elapsed.TotalMilliseconds > (maxTime - TimerTickInterval))
                {
                    Log("WARN Stopping early at depth " + depth + ", time: " + TotalSearchTime.Elapsed.TotalMilliseconds + " > (maxtime: " + maxTime + " - interval: " + TimerTickInterval + ")");

                    SearchDurationTimer?.Stop();
                    stopSearchingDelegate();
                    setLastMoveDelegate(LastBestMove, LastBestScore);
                    TotalSearchTime.Reset();
                    callSearchFinishDelegate();
                    SearchDurationTimer?.Dispose();
                }
                else if ((depth >= SearchLowTimeMinDepth && maxTime > playerTimeLeft && (playerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds) < SearchLowTimeThreshold))
                {
                    Log("WARN Stopping early at depth " + depth + ", maxTime: " + maxTime + " > playerTimeLeft: " + playerTimeLeft + " and (playerTimeLeft - time): (" + playerTimeLeft + " - " + TotalSearchTime.Elapsed.TotalMilliseconds + ") = " + (playerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds));
                    
                    SearchDurationTimer?.Stop();
                    stopSearchingDelegate();
                    setLastMoveDelegate(LastBestMove, LastBestScore);
                    TotalSearchTime.Reset();
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
                    alpha = LastBestScore - (AspirationWindowMargin + (depth * MarginIncreasePerDepth));
                    beta = LastBestScore + (AspirationWindowMargin + (depth * MarginIncreasePerDepth));
                    Log("Depth " + depth + " aspiration bounds are [A: " + alpha + ", eval: " + LastBestScore + ", B: " + beta + "]");
                }

                aspirationFailed = false;
                ulong prevNodes = info.NodeCount;
                Deepen(ref info, alpha, beta);
                ulong afterNodes = info.NodeCount;

                if (info.StopSearching)
                {
                    Log("Received StopSearching command just after Deepen at depth " + depth);

                    SearchDurationTimer?.Stop();
                    stopSearchingDelegate();
                    setLastMoveDelegate(LastBestMove, LastBestScore);
                    TotalSearchTime.Reset();
                    callSearchFinishDelegate();
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

                    Log("Depth " + depth + " failed aspiration bounds, got " + info.BestScore);
                    continue;
                }

                info.OnDepthFinish?.Invoke();

                if (continueDeepening && Evaluation.IsScoreMate(info.BestScore, out _))
                {
                    Log("Forced mate found (" + info.BestMove + "), aborting at depth " + depth + " after " + TotalSearchTime.Elapsed.TotalSeconds + " seconds");
                    TotalSearchTime.Reset();
                    SearchDurationTimer?.Stop();
                    info.OnSearchFinish?.Invoke();
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

                    TotalSearchTime.Reset();
                    SearchDurationTimer?.Stop();
                    info.OnSearchFinish?.Invoke();
                    return;
                }

                depth++;
                LastBestMove = info.BestMove;
                LastBestScore = info.BestScore;

                if (allowDepthIncrease && TotalSearchTime.Elapsed.TotalMilliseconds < SearchLowTimeThreshold && depth == maxDepthStart)
                {
                    maxDepthStart++;
                    Log("Extended search depth to " + maxDepthStart);
                }

                continueDeepening = (depth <= maxDepthStart && TotalSearchTime.Elapsed.TotalMilliseconds <= maxTime);
            }

            TotalSearchTime.Reset();
            SearchDurationTimer?.Stop();
            info.OnSearchFinish?.Invoke();
        }

        /// <summary>
        /// Performs one pass of deepening, at the depth specified in <paramref name="info"/>.maxDepthStart
        /// </summary>
        public static void Deepen(ref SearchInformation info, int alpha = AlphaStart, int beta = BetaStart)
        {
            TotalSearchTime.Start();
            int score = SimpleSearch.FindBest(ref info, alpha, beta, info.MaxDepth);
            TotalSearchTime.Stop();
            info.SearchTime = TotalSearchTime.Elapsed.TotalMilliseconds;
            info.BestScore = score;
        }

        /// <summary>
        /// Finds the best move according to the Evaluation function, looking at least <paramref name="depth"/> moves in the future.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="alpha">
        ///     The evaluation of the lower bound move. 
        ///     This will eventually be set equal to the evaluation of the best move we can make.
        /// </param>
        /// <param name="beta">
        ///     The evaluation of the upper bound move.
        ///     This essentially represents the best case scenerio for our opponent based on the moves we have available to us.
        ///     If we ever evaluate a position that scores higher than the beta, 
        ///     we immediately stop searching (return beta) because that would mean our opponent has a good reply
        ///     and we know that we can probably do better by making a different move.
        /// </param>
        /// <param name="depth">
        ///     The depth to search to.
        ///     When this reaches 0 we would ordinarily stop looking, 
        ///     but there is an additional quiescence search which looks at all available captures to make sure we didn't just 
        ///     make a blunder that could have been avoided by looking an additional move in the future.
        /// </param>
        /// <returns>The evaluation of the best move.</returns>
        [MethodImpl(Inline)]
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int depth)
        {
            if (info.StopSearching || info.NodeCount >= info.MaxNodes)
            {
                info.StopSearching = true;
                return 0;
            }

            if (depth <= 0)
            {
                return SimpleQuiescence.FindBest(ref info, alpha, beta, depth);
            }

            info.NodeCount++;

            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            Move BestMove = Move.Null;
            int startingAlpha = alpha;

            bool useTT = false;
            if (useTT)
            {
                if (ttEntry.NodeType != NodeType.Invalid && ttEntry.Key == TTEntry.MakeKey(posHash) && !ttEntry.BestMove.IsNull())
                {
                    //  Make sure it's legal since collisions are common
                    //  If we found a node for this position
                    if (ttEntry.NodeType != NodeType.Alpha && bb.IsPseudoLegal(ttEntry.BestMove) && IsLegal(pos, bb, ttEntry.BestMove, bb.KingIndex(Not(pos.ToMove)), bb.KingIndex(pos.ToMove)))
                    {
                        //  Exclude alpha nodes because they are lower bounds and probably not "the best"
                        info.BestMove = ttEntry.BestMove;
                    }

                    if (ttEntry.Depth >= depth)
                    {
                        //  If we have already searched this node at an equal or higher depth, we can update alpha and beta accordingly
                        if (ttEntry.NodeType == NodeType.Alpha)
                        {
                            if (ttEntry.Eval < beta)
                            {
                                //  This node is known to be worse than beta
                                beta = ttEntry.Eval;
                            }
                        }
                        else if (ttEntry.NodeType == NodeType.Beta)
                        {
                            if (ttEntry.Eval > alpha)
                            {
                                //  This node is known to be better than alpha
                                alpha = ttEntry.Eval;
                            }
                        }
                        else if (ttEntry.NodeType == NodeType.Exact)
                        {
                            if ((beta - alpha >= 1) || Evaluation.IsScoreMate(ttEntry.Eval, out _))
                            {
#if DEBUG
                                SearchStatistics.NegamaxTTExactHits++;
#endif
                                return ttEntry.Eval;
                            }
                        }

                        //  Beta cutoff
                        if (alpha >= beta)
                        {
#if DEBUG
                            SearchStatistics.BetaCutoffs++;
#endif
                            return ttEntry.Eval;
                        }
                    }
                }
            }

            bool useStaticEval = false;
            if (useStaticEval)
            {
                int staticEval = 0;
                ETEntry etEntry = EvaluationTable.Probe(posHash);
                if (etEntry.key == EvaluationTable.InvalidKey || !etEntry.Validate(posHash) || etEntry.score == ETEntry.InvalidScore)
                {
                    staticEval = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
                    EvaluationTable.Save(posHash, (short)staticEval);
                }
                else
                {
                    staticEval = etEntry.score;
                }
            }

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = pos.GenAllLegalMoves(list);

            //  No legal moves, is this checkmate or a draw?
            if (size == 0)
            {
                if (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck)
                {
                    return -Evaluation.ScoreMate - ((info.MaxDepth / 2) + 1);
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            SortByMoveScores(ref info, list, size, ttEntry.BestMove);

            for (int i = 0; i < size; i++)
            {
                int score;

                if (info.StopSearching)
                {
                    return 0;
                }
                
                if (info.Position.WouldCauseDraw(list[i]))
                {
                    //  Instead of looking further and probably breaking something,
                    //  Just evaluate this move as a draw here and keep looking at the others.
                    score = -Evaluation.ScoreDraw;
                }
                else
                {
                    pos.MakeMove(list[i]);
                    score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1);
                    pos.UnmakeMove();
                }

                if (score >= beta)
                {
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

        /// <summary>
        /// Assigns each move in the list a score based on things like whether it is a capture, causes check, etc.
        /// This is important for iterative deepening since we generally have a good idea of which moves are good/bad
        /// based on the results from the previous depth, and don't necessarily want to spend time looking at 
        /// a "bad" move's entire search tree again when we already have a couple moves that look promising.
        /// </summary>
        /// <param name="pvOrTTMove">This is set to the TTEntry.BestMove from the previous depth, or possibly Move.Null</param>
        [MethodImpl(Inline)]
        private static void SortByMoveScores(ref SearchInformation info, Span<Move> list, int size, Move pvOrTTMove)
        {
            Span<int> scores = stackalloc int[size];
            for (int i = 0; i < size; i++)
            {
                if (list[i].Equals(pvOrTTMove))
                {
                    scores[i] = MoveScores.PV_TT_Move;
                }
                else if (list[i].Capture)
                {
                    int ourPieceVal = Evaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].from]);
                    int theirPieceVal = Evaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].to]);
                    if (theirPieceVal - ourPieceVal > 0)
                    {
                        scores[i] = MoveScores.WinningCapture;
                    }
                    else if (theirPieceVal - ourPieceVal == 0)
                    {
                        scores[i] = MoveScores.EqualCapture;
                    }
                    else
                    {
                        scores[i] = MoveScores.LosingCapture;
                    }
                }
                else if (list[i].CausesDoubleCheck)
                {
                    scores[i] = MoveScores.DoubleCheck;
                }
                else if (list[i].CausesCheck)
                {
                    scores[i] = MoveScores.Check;
                }
                else if (list[i].Castle)
                {
                    scores[i] = MoveScores.Castle;
                }
                else
                {
                    scores[i] = MoveScores.Normal;
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
        }

        private static bool CanFutilityPrune(CheckInfo checkInfo, int alpha, int beta)
        {
            Evaluation.IsScoreMate(alpha, out _);
            return false;
        }

        private static bool CanLateMoveReduce(ref SearchInformation info, Move m, int alpha, int beta)
        {
            if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck)
            {
                return false;
            }

            if (m.CausesCheck || m.CausesDoubleCheck)
            {
                return false;
            }

            if (m.Capture || m.Promotion)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the PV line from a search, which is the 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="moves"></param>
        /// <param name="numMoves"></param>
        /// <returns></returns>
        public static int GetPV(SearchInformation info, in Move[] moves, int numMoves)
        {
            int theirKing = info.Position.bb.KingIndex(Not(info.Position.ToMove));
            int ourKing = info.Position.bb.KingIndex(info.Position.ToMove);

            TTEntry stored = TranspositionTable.Probe(info.Position.Hash);
            if (stored.NodeType == NodeType.Exact && stored.Key == TTEntry.MakeKey(info.Position.Hash) && numMoves < MaxDepth)
            {
                if (!info.Position.bb.IsPseudoLegal(stored.BestMove) || !IsLegal(info.Position, info.Position.bb, stored.BestMove, ourKing, theirKing))
                {
                    if (stored.BestMove.IsNull())
                    {
                        Log("GetPV stopping normally at numMoves: " + numMoves + "\t info -> " + info.ToString());
                    }
                    else
                    {
                        Log("WARN GetPV(" + stored.BestMove.ToString() + " isn't (pseudo)legal, stopping at numMoves: " + numMoves + "\t info -> " + info.ToString());
                    }

                    return numMoves;
                }

                //  Prevent any perpetual check loops from being included in the PV.
                //  Before, it would sometimes keep placing the same 4 forced moves
                //  in the PV, which some UCI's mark as (technically) illegal.
                if (info.Position.IsThreefoldRepetition() || info.Position.IsFiftyMoveDraw())
                {
                    //  TODO position.IsInsufficientMaterial() here?
                    return numMoves;
                }

                //  If the above check fails for any reason,
                //  This will also prevent those loops from occuring.
                //  TODO seldepth won't work with this.

                if (numMoves >= info.MaxDepth)
                {
                    return numMoves;
                }

                moves[numMoves] = stored.BestMove;
                info.Position.MakeMove(stored.BestMove);

                numMoves = GetPV(info, moves, numMoves + 1);
                info.Position.UnmakeMove();
            }

            return numMoves;
        }


    }
}
