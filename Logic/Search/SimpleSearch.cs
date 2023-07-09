﻿
#define SHOW_STATS


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Timer = System.Timers.Timer;

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
        public const int TimerTickInterval = 20;

        /// <summary>
        /// Add this amount of milliseconds to the total search time when checking if the
        /// search should stop, in case the move overhead is very low and the UCI expects
        /// the search to stop very quickly after our time expires.
        /// </summary>
        public const int TimerBuffer = 50;

        /// <summary>
        /// The minimum amount of time to search, regardless of the other limitations of the search.
        /// This only applies to the amount of time that we were told to search for (i.e. "movetime 100").
        /// If we receive a "stop" command from the UCI, this does no apply and we stop as soon as possible.
        /// </summary>
        public const int MinSearchTime = 200;

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
        private static int LastBestScore = ThreadedEvaluation.ScoreDraw;

        /// <summary>
        /// Begin a new search with the parameters in <paramref name="info"/>.
        /// This performs iterative deepening, which searches at higher and higher depths as time goes on.
        /// <br></br>
        /// If <paramref name="allowDepthIncrease"/> is true, then the search will continue above the requested maximum depth
        /// so long as there is still search time remaining.
        /// </summary>
        public static void StartSearching(ref SearchInformation info, bool allowDepthIncrease = false)
        {
            TranspositionTable.Clear();
            TotalSearchTime.Restart();

            int depth = 1;
            int maxDepthStart = info.MaxDepth;
            double maxTime = info.MaxSearchTime;
            int playerTimeLeft = info.PlayerTimeLeft;

            info.RootPositionMoveCount = info.Position.Moves.Count;

            bool continueDeepening = true;
            info.NodeCount = 0;

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
                    Log("Depth " + depth + " aspiration bounds are [A: " + alpha + ", eval: " + LastBestScore + ", B: " + beta + "]");
#endif
                }

                aspirationFailed = false;
                ulong prevNodes = info.NodeCount;
                Deepen(ref info, alpha, beta);
                ulong afterNodes = info.NodeCount;

                if (info.StopSearching)
                {
                    Log("Received StopSearching command just after Deepen at depth " + depth);

                    info.SearchWasAborted = true;
                    info.SetLastMove(LastBestMove, LastBestScore);
                    info.OnSearchFinish?.Invoke(info);
                    TotalSearchTime.Reset();
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

#if (DEBUG || SHOW_STATS)
                    SearchStatistics.AspirationWindowFails++;
                    SearchStatistics.AspirationWindowTotalDepthFails += (ulong)depth;
#endif

#if DEBUG
                    Log("Depth " + depth + " failed aspiration bounds, got " + info.BestScore);
#endif
                    continue;
                }

                info.OnDepthFinish?.Invoke(info);

                if (continueDeepening && ThreadedEvaluation.IsScoreMate(info.BestScore, out int mateIn))
                {
                    Log(info.BestMove.ToString(info.Position) + " forces mate in " + mateIn + ", aborting at depth " + depth + " after " + TotalSearchTime.Elapsed.TotalSeconds + " seconds");
                    TotalSearchTime.Reset();
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

            TotalSearchTime.Reset();
            info.OnSearchFinish?.Invoke(info);
        }

        /// <summary>
        /// Performs one pass of deepening, at the depth specified in <paramref name="info"/>.maxDepthStart
        /// </summary>
        public static void Deepen(ref SearchInformation info, int alpha = AlphaStart, int beta = BetaStart)
        {
            TotalSearchTime.Start();
            int score = SimpleSearch.FindBest(ref info, alpha, beta, info.MaxDepth, isPV: true, isRoot: true);
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
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int depth, bool isPV = false, bool isRoot = false)
        {
            //  Check every few thousand nodes if we need to stop the search.
            if ((info.NodeCount & SearchCheckInCount) == 0)
            {
                bool doStop = false;

                if (TotalSearchTime.Elapsed.TotalMilliseconds > (info.MaxSearchTime - TimerTickInterval - TimerBuffer))
                {
                    //  Stop early because we are about to (or did) go over the max time
                    Log("WARN Stopping early at depth " + depth + ", time: " + TotalSearchTime.Elapsed.TotalMilliseconds + " > (maxtime: " + info.MaxSearchTime + " - interval: " + TimerTickInterval + " - TimerBuffer: " + TimerBuffer + "), current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));

                    doStop = true;
                }
                else if ((depth >= SearchLowTimeMinDepth && info.MaxSearchTime > info.PlayerTimeLeft && (info.PlayerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds) < SearchLowTimeThreshold))
                {
                    //  Stop early if:
                    //  We are at/past the depth to begin checking AND
                    //  We were told to search for more time than we have left AND
                    //  We now have less time than the low time threshold

                    Log("WARN Stopping early at depth " + depth + ", maxTime: " + info.MaxSearchTime + " > playerTimeLeft: " + info.PlayerTimeLeft + " and (playerTimeLeft - time): (" + info.PlayerTimeLeft + " - " + TotalSearchTime.Elapsed.TotalMilliseconds + ") = " + (info.PlayerTimeLeft - TotalSearchTime.Elapsed.TotalMilliseconds) + ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));

                    doStop = true;
                }

                if (doStop && (TotalSearchTime.Elapsed.TotalMilliseconds < MinSearchTime) && ((info.PlayerTimeLeft - TimerTickInterval - TimerBuffer) > MinSearchTime))
                {
                    //  If we ordinarily would stop, try enforcing a minimum search time
                    //  to prevent the time spent on moves from oscillating to a large degree.

                    //  As long as we have enough time left that this timer will tick (playerTimeLeft - TimerTickInterval - TimerBuffer)
                    //  Then postpone stopping until TotalSearchTime.Elapsed.TotalMilliseconds > MinSearchTime
                    Log("WARN Stopping at depth " + depth + " was postponed! maxTime: " + info.MaxSearchTime + " > playerTimeLeft: " + info.PlayerTimeLeft + " but we've only searched for " + TotalSearchTime.Elapsed.TotalMilliseconds + " < MinSearchTime: " + MinSearchTime + ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));

                    doStop = false;
                }

                if (doStop)
                {
                    LogString("STOPPING in FindBest! time " + TotalSearchTime.Elapsed.TotalMilliseconds + " > " + info.MaxSearchTime);
                    info.StopSearching = true;
                }
            }


            if (info.StopSearching || info.NodeCount >= info.MaxNodes)
            {
                info.StopSearching = true;
                return 0;
            }

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
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

            int staticEval = 0;
            bool isFutilityPrunable = false;
            bool isNullMovePrunable = false;

            if (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck)
            {
                //  Don't bother getting the static evaluation or checking if we can prune nodes.
                goto MoveLoop;
            }

            ETEntry etEntry = EvaluationTable.Probe(posHash);
            if (etEntry.key == EvaluationTable.InvalidKey || !etEntry.Validate(posHash) || etEntry.score == ETEntry.InvalidScore)
            {
                staticEval = info.GetEvaluation(pos, pos.ToMove);
                EvaluationTable.Save(posHash, (short)staticEval);
            }
            else
            {
                staticEval = etEntry.score;
            }

            isFutilityPrunable = CanFutilityPrune((pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck), isPV, alpha, beta, depth);
            isNullMovePrunable = CanNullMovePrune(bb, (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck), isPV, staticEval, beta, depth);

            if (UseNullMovePruning && isNullMovePrunable)
            {
                int reduction = SearchConstants.NullMovePruningMinDepth + (depth / SearchConstants.NullMovePruningMinDepth);

                info.Position.MakeNullMove();
                int nullMoveEval = -FindBest(ref info, -beta, -beta + 1, depth - reduction, false, false);
                info.Position.UnmakeNullMove();

                if (nullMoveEval >= beta)
                {
                    //  Then our opponent couldn't improve their position sufficiently with a free move,
#if DEBUG || SHOW_STATS
                    SearchStatistics.NullMovePrunedNodes++;
#endif
                    return beta;
                }
            }

        MoveLoop:

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
                    return -ThreadedEvaluation.ScoreDraw;
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

                if (UseFutilityPruning && isFutilityPrunable)
                {
                    if (!list[i].Capture && !isPV)
                    {
                        if (staticEval + GetFutilityMargin(depth) < alpha)
                        {
#if DEBUG || SHOW_STATS
                            SearchStatistics.FutilityPrunedNoncaptures++;
#endif
                            continue;
                        }

                    }
                }

                bool kingCheckExtension = (bb.GetPieceAtIndex(list[i].from) == Piece.King && (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck));

                pos.MakeMove(list[i]);

                int reduction = 0;

                if (info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial())
                {
                    //  Instead of looking further and probably breaking something,
                    //  Just evaluate this move as a draw here and keep looking at the others.
                    score = -ThreadedEvaluation.ScoreDraw;
                }
                else if (isPV)
                {
                    score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1, isPV: true);
                    isPV = false;
                }
                else
                {
                    if (UseLateMoveReduction && CanLateMoveReduce(ref info, list[i], depth, isPV))
                    {

#if DEBUG || SHOW_STATS
                        SearchStatistics.LMRReductions++;
#endif

                        reduction = GetLateMoveReductionAmount(size, i, depth);

                        if (!isPV)
                        {
                            reduction++;
                        }

                        if (kingCheckExtension)
                        {
                            reduction--;
                        }

                        //  Try a search with LMR applied (depth - 1 - reduction).
                        //  If this fails, we will need to repeat the search at full depth.
                        score = -SimpleSearch.FindBest(ref info, -alpha - 1, -alpha, depth - 1 - reduction);
                    }
                    else
                    {
                        score = alpha + 1;
                    }

                    if (score > alpha)
                    {
                        //  If the alpha from the previous search went above the -alpha - 1,
                        //  We will have to redo the search

                        score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1);

                        if (score > alpha && score < beta)
                        {
                            //  The previous search fell between the alpha and beta,
                            //  So we treat this as if it were our PV and search again.
                            score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1, isPV: true);
                        }
                    }
                }

                pos.UnmakeMove();

                if (score >= beta)
                {
                    //TranspositionTable.Save(posHash, (short)score, NodeType.Beta, depth, BestMove);
#if DEBUG || SHOW_STATS
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

        /// <summary>
        /// Assigns each move in the list a score based on things like whether it is a capture, causes check, etc.
        /// This is important for iterative deepening since we generally have a good idea of which moves are good/bad
        /// based on the results from the previous depth, and don't necessarily want to spend time looking at 
        /// a "bad" move's entire search tree again when we already have a couple moves that look promising.
        /// </summary>
        /// <param name="pvOrTTMove">This is set to the TTEntry.BestMove from the previous depth, or possibly Move.Null</param>
        /// <returns>The index of the first move classified as "normal"</returns>
        [MethodImpl(Inline)]
        public static int SortByMoveScores(ref SearchInformation info, in Span<Move> list, int size, Move pvOrTTMove)
        {
            Span<int> scores = stackalloc int[size];
            int theirKing = info.Position.bb.KingIndex(Not(info.Position.ToMove));
            int pt;

            for (int i = 0; i < size; i++)
            {
                if (list[i].Equals(pvOrTTMove))
                {
                    scores[i] = MoveScores.PV_TT_Move;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_PV_TT_Move++;
#endif
                    continue;
                }
                else if (list[i].Capture)
                {
                    if (UseStaticExchangeEval)
                    {
                        scores[i] = SimpleQuiescence.StaticExchange(ref info.Position.bb, list[i].to, info.Position.ToMove);
                    }
                    else
                    {
                        int ourPieceVal = ThreadedEvaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].from]);
                        int theirPieceVal = ThreadedEvaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].to]);
                        if (theirPieceVal - ourPieceVal > 0)
                        {
                            scores[i] = MoveScores.WinningCapture;
#if DEBUG || SHOW_STATS
                            SearchStatistics.Scores_WinningCapture++;
#endif
                        }
                        else if (theirPieceVal - ourPieceVal == 0)
                        {
                            scores[i] = MoveScores.EqualCapture;
#if DEBUG || SHOW_STATS
                            SearchStatistics.Scores_EqualCapture++;
#endif
                        }
                        else
                        {
                            scores[i] = MoveScores.LosingCapture;
#if DEBUG || SHOW_STATS
                            SearchStatistics.Scores_LosingCapture++;
#endif
                        }
                    }

                    continue;
                }
                else if (list[i].CausesDoubleCheck)
                {
                    scores[i] = MoveScores.DoubleCheck;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_DoubleCheck++;
#endif
                    continue;
                }
                else if (list[i].CausesCheck)
                {
                    scores[i] = MoveScores.Check;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Check++;
#endif
                    continue;
                }
                else if (list[i].Castle)
                {
                    scores[i] = MoveScores.Castle;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Castle++;
#endif
                    continue;
                }

                pt = info.Position.bb.GetPieceAtIndex(list[i].from);
                if (pt == Piece.Pawn && info.Position.bb.IsPasser(list[i].from))
                {
                    scores[i] = MoveScores.PassedPawnPush;
                    continue;
                }

                if ((pt == Piece.Queen || pt == Piece.Rook) && (RookRays[theirKing] & SquareBB[list[i].to]) != 0)
                {
                    scores[i] = MoveScores.KingXRay;
                    continue;
                }

                if ((pt == Piece.Queen || pt == Piece.Bishop) && (BishopRays[theirKing] & SquareBB[list[i].to]) != 0)
                {
                    scores[i] = MoveScores.KingXRay;
                    continue;
                }

                scores[i] = MoveScores.Normal;
#if DEBUG || SHOW_STATS
                SearchStatistics.Scores_Normal++;
#endif
            }

            int iFirstNormal = 0;

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

            for (int i = 0; i < size; i++)
            {
                if (scores[i] == MoveScores.Normal)
                {
                    return i;
                }
            }

            return size;
        }


        [MethodImpl(Inline)]
        public static bool CanFutilityPrune(bool isInCheck, bool isPV, int alpha, int beta, int depth)
        {
            if (!isInCheck && !isPV && depth <= SearchConstants.FutilityPruningMaxDepth)
            {
                if (!ThreadedEvaluation.IsScoreMate(alpha, out _) && !ThreadedEvaluation.IsScoreMate(beta, out _))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static int GetFutilityMargin(int depth)
        {
            return SearchConstants.FutilityPruningMarginPerDepth * (depth);
        }

        [MethodImpl(Inline)]
        public static bool CanNullMovePrune(in Bitboard bb, bool isInCheck, bool isPVNode, int staticEval, int beta, int depth)
        {
            if (!isInCheck && !isPVNode && depth >= SearchConstants.NullMovePruningMinDepth && staticEval >= beta)
            {
                //  Shouldn't be used in endgames.
                int weakerSideMaterial = Math.Min(bb.MaterialCount(Color.White), bb.MaterialCount(Color.Black));
                return (weakerSideMaterial > EvaluationConstants.EndgameMaterial);
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static bool CanLateMoveReduce(ref SearchInformation info, Move m, int depth, bool isPV)
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

            if (isPV || depth < SearchConstants.LMRDepth)
            {
                return false;
            }

            if (info.Position.bb.IsPasser(m.from))
            {
                return false;
            }

            return true;
        }


        [MethodImpl(Inline)]
        public static int GetLateMoveReductionAmount(int listLen, int listIndex, int depth)
        {
            // Always reduce by 1, and reduce by 1 again if this move is ordered late in the list.
            bool isLateInList = (listIndex * 2 > listLen);

            bool isVeryLateInList = (listIndex * 4 > listLen * 3);

            if (isVeryLateInList)
            {
                return SearchConstants.LMRReductionAmount + (depth / 2);
            }
            else if (isLateInList)
            {
                // We also reduce by slightly more if the move is very close to the end of the list.
                return SearchConstants.LMRReductionAmount + (depth / 2);
                //return SearchConstants.LMRReductionAmount + (depth / 4);
            }

            return SearchConstants.LMRReductionAmount;
        }


        /// <summary>
        /// Returns the PV line from a search, which is the series of moves that the engine thinks will be played.
        /// </summary>
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
            else if (stored.NodeType != NodeType.Invalid)
            {
                //Log("GetPV stopping normally, TTEntry was -> " + stored.ToString());
            }

            return numMoves;
        }

    }
}
