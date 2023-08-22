
//#define SHOW_STATS


using LTChess.Logic.Core;
using LTChess.Logic.Data;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.HalfKP;
using LTChess.Logic.NN.Simple768;
using LTChess.Logic.Search.Ordering;

using static LTChess.Logic.Search.Ordering.MoveOrdering;

namespace LTChess.Logic.Search
{
    public static class SimpleSearch
    {
        /// <summary>
        /// The best move found in the previous call to Deepen.
        /// </summary>
        private static Move LastBestMove = Move.Null;

        /// <summary>
        /// The evaluation of that best move.
        /// </summary>
        private static int LastBestScore = ThreadedEvaluation.ScoreDraw;

        private static SearchStack SearchStack = new SearchStack();

        public static KillerMoveTable KillerMoves = new KillerMoveTable();

        public static HistoryMoveTable HistoryMoves = new HistoryMoveTable();

        private static PrincipalVariationTable PvMoves = new PrincipalVariationTable();

        /// <summary>
        /// Begin a new search with the parameters in <paramref name="info"/>.
        /// This performs iterative deepening, which searches at higher and higher depths as time goes on.
        /// <br></br>
        /// If <paramref name="allowDepthIncrease"/> is true, then the search will continue above the requested maximum depth
        /// so long as there is still search time remaining.
        /// </summary>
        [MethodImpl(Optimize)]
        public static void StartSearching(ref SearchInformation info, bool allowDepthIncrease = false)
        {
            TranspositionTable.Clear();
            SearchStack.Clear();
            PvMoves.Clear();

            if (UseKillerHeuristic)
            {
                KillerMoves.Clear();
            }

            info.TimeManager.RestartTimer();

            int depth = 1;

            int maxDepthStart = info.MaxDepth;
            double maxTime = info.TimeManager.MaxSearchTime;

            info.RootPositionMoveCount = info.Position.Moves.Count;
            info.NodeCount = 0;
            info.SearchActive = true;

            int alpha = AlphaStart;
            int beta = BetaStart;
            bool aspirationFailed = false;

            bool continueDeepening = true;
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

                if (UseHistoryHeuristic)
                {
                    HistoryMoves.Reduce();
                }

                aspirationFailed = false;
                ulong prevNodes = info.NodeCount;

                int score = SimpleSearch.FindBest<RootNode>(ref info, alpha, beta, info.MaxDepth, 0);
                info.BestScore = score;

                ulong afterNodes = info.NodeCount;

                if (info.StopSearching)
                {
                    Log("Received StopSearching command just after Deepen at depth " + depth);
                    info.SearchActive = false;

                    //  If our search was interrupted, info.BestMove probably doesn't contain the actual best move,
                    //  and instead has the best move from whichever call to FindBest set it last.

                    if (PvMoves.Get(0) != LastBestMove)
                    {
                        Log("WARN PvMoves[0] " + PvMoves.Get(0).ToString(info.Position) + " != LastBestMove " + LastBestMove.ToString(info.Position));
                    }

                    info.SetLastMove(LastBestMove, LastBestScore);
                    info.OnSearchFinish?.Invoke(info);
                    info.TimeManager.ResetTimer();
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
                    Log(info.BestMove.ToString(info.Position) + " forces mate in " + mateIn + ", aborting at depth " + depth + " after " + info.TimeManager.GetSearchTime() + "ms");
                    break;
                }

                depth++;
                LastBestMove = info.BestMove;
                LastBestScore = info.BestScore;

                if (allowDepthIncrease && info.TimeManager.GetSearchTime() < SearchLowTimeThreshold && depth == maxDepthStart)
                {
                    maxDepthStart++;
                    //Log("Extended search depth to " + (maxDepthStart - 1));
                }

                continueDeepening = (depth <= maxDepthStart && info.TimeManager.GetSearchTime() <= maxTime);
            }

            info.OnSearchFinish?.Invoke(info);
            info.TimeManager.ResetTimer();

            if (UseSimple768)
            {
                NNUEEvaluation.ResetNN();
            }

            if (UseHalfKP)
            {
                HalfKP.ResetNN();
            }

            if (UseHalfKA)
            {
                HalfKA_HM.ResetNN();
            }
        }


        /// <summary>
        /// Finds the best move according to the Evaluation function, looking at least <paramref name="depth"/> moves in the future.
        /// </summary>
        /// <typeparam name="NodeType">One of <see cref="RootNode"/>, <see cref="PVNode"/>, or <see cref="NonPVNode"/></typeparam>
        /// <param name="info">Reference to the current search's SearchInformation</param>
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
        public static int FindBest<NodeType>(ref SearchInformation info, int alpha, int beta, int depth, int ply) where NodeType : SearchNodeType
        {
            bool isRoot = (typeof(NodeType) == typeof(RootNode));
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));

            //  Check every few thousand nodes if we need to stop the search.
            if ((info.NodeCount & SearchCheckInCount) == 0)
            {
#if DEBUG || SHOW_STATS
                SearchStatistics.Checkups++;
#endif
                if (info.TimeManager.CheckUp(info.RootPlayerToMove))
                {
                    info.StopSearching = true;
                }
            }

            if (info.NodeCount >= info.MaxNodes || info.StopSearching)
            {
                info.StopSearching = true;
                return 0;
            }

#if DEBUG || SHOW_STATS
            SearchStatistics.NMCalls++;
#endif

            if (isPV)
            {
                PvMoves.InitializeLength(ply);
                if (ply > info.SelectiveDepth)
                {
                    info.SelectiveDepth = ply;
                }
            }

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return SimpleQuiescence.QSearch<NodeType>(ref info, alpha, beta, depth, ply);
            }

#if DEBUG || SHOW_STATS
            SearchStatistics.NMCalls_NOTQ++;
#endif

            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            Move BestMove = Move.Null;
            int startingAlpha = alpha;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            if (ttEntry.NodeType != TTNodeType.Invalid && ttEntry.ValidateKey(posHash))
            {
                if (!isPV && ttEntry.Depth >= depth)
                {
                    //  We have already seen this position before at a higher depth,
                    //  so we can take the information from that depth and use it here.
                    if (ttEntry.NodeType == TTNodeType.Exact)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.TTExactHits++;
#endif
                        return ttEntry.Eval;
                    }
                    else if (ttEntry.NodeType == TTNodeType.Beta)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.TTBetaHits++;
#endif
                        alpha = Math.Max(alpha, ttEntry.Eval);
                    }
                    else if (ttEntry.NodeType == TTNodeType.Beta)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.TTAlphaHits++;
#endif
                        beta = Math.Min(beta, ttEntry.Eval);
                    }
                }
            }

            bool improving = false;

            int staticEval = ETEntry.InvalidScore;

            TTNodeType nodeTypeToSave = TTNodeType.Invalid;

            bool isFutilityPrunable = false;
            bool isLateMovePrunable = false;
            bool isRazoringPrunable = false;
            bool isNullMovePrunable = false;

            bool isInCheck = (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck);

            if (isInCheck)
            {
                SearchStack[ply].StaticEval = ETEntry.InvalidScore;

                //  Don't bother getting the static evaluation or checking if we can prune nodes.
                goto MoveLoop;
            }

            staticEval = EvaluationTable.ProbeOrEval(ref info);
            SearchStack[ply].StaticEval = staticEval;

            if (UseReverseFutilityPruning && CanReverseFutilityPrune(isInCheck, isPV, beta, depth))
            {
                var lastScore = SearchStack[ply - 2].StaticEval;
                if (lastScore == ETEntry.InvalidScore)
                {
                    lastScore = SearchStack[ply - 4].StaticEval;
                }
                if (lastScore == ETEntry.InvalidScore)
                {
                    //  https://github.com/official-stockfish/Stockfish/blob/af110e02ec96cdb46cf84c68252a1da15a902395/src/search.cpp#L754
                    lastScore = 173;
                }

                var improvement = staticEval - lastScore;
                improving = (ply >= 2 && improvement > 0);

                if (staticEval - GetReverseFutilityMargin(depth, improving) >= beta && staticEval >= beta)
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.ReverseFutilityPrunedNodes++;
#endif
                    return beta;
                }
            }

            isFutilityPrunable = CanFutilityPrune(isInCheck, isPV, alpha, beta, depth);
            isLateMovePrunable = CanLateMovePrune(isInCheck, isPV, isRoot, depth);
            isRazoringPrunable = CanRazoringPrune(isInCheck, isPV, staticEval, alpha, depth);
            isNullMovePrunable = CanNullMovePrune(pos, isInCheck, isPV, staticEval, beta, depth);

            if (UseRazoring && isRazoringPrunable)
            {
#if DEBUG || SHOW_STATS
                SearchStatistics.RazoredNodes++;
#endif
                return SimpleQuiescence.QSearch<NodeType>(ref info, alpha, beta, depth, ply);
            }

            if (UseNullMovePruning && isNullMovePrunable)
            {
                int reduction = SearchConstants.NullMovePruningMinDepth + (depth / SearchConstants.NullMovePruningMinDepth);

                info.Position.MakeNullMove();
                int nullMoveEval = -FindBest<NonPVNode>(ref info, -beta, -beta + 1, depth - reduction, ply + 1);
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

            Span<int> scores = stackalloc int[size];
            AssignNormalMoveScores(ref info, list, scores, size, ply, ttEntry.BestMove);

            int quietMoves = 0;
            int lmpCutoff = SearchConstants.LMPDepth + (depth * depth);
            //int bestScore = AlphaStart;

            for (int i = 0; i < size; i++)
            {
                OrderNextMove(list, scores, size, i);
                if (!list[i].Capture)
                {
                    quietMoves++;
                }

                if (UseFutilityPruning && isFutilityPrunable && !list[i].Capture)
                {
                    if (staticEval + (SearchConstants.FutilityPruningMarginPerDepth * depth) < alpha)
                    {
                        if (i == 0)
                        {
                            //  In the off chance that the move ordered first would fail futility pruning,
                            //  we should at least check the next node to see if it will too.
                            continue;
                        }
                        else
                        {
#if DEBUG || SHOW_STATS
                            SearchStatistics.FutilityPrunedNoncaptures++;
                            SearchStatistics.FutilityPrunedMoves += (ulong)(size - i);
#endif
                            break;
                        }
                    }
                }

                //  Only prune if our alpha has changed, meaning we have found at least 1 good move.
                //if (UseLateMovePruning && isLateMovePrunable && (bestScore > AlphaStart) && (quietMoves > lmpCutoff))
                if (UseLateMovePruning && isLateMovePrunable && (alpha != AlphaStart) && (quietMoves > lmpCutoff))
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.LateMovePrunings++;
                    SearchStatistics.LateMovePrunedMoves += (ulong)(size - i);
#endif
                    break;
                }

                bool kingCheckExtension = (bb.GetPieceAtIndex(list[i].From) == Piece.King && (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck));

                pos.MakeMove(list[i]);
                info.NodeCount++;

                int score;
                if ((info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial()))
                {
                    //  Instead of looking further and probably breaking something,
                    //  Just evaluate this move as a draw here and keep looking at the others.
                    score = -ThreadedEvaluation.ScoreDraw;
                }
                else if (i == 0)
                {
                    //  This is the move ordered first, so we treat this as our PV and search
                    //  without any reductions.
                    score = -SimpleSearch.FindBest<PVNode>(ref info, -beta, -alpha, depth - 1, ply + 1);
                }
                else
                {

                    int nextDepth = depth - 1;

                    if (UseSearchExtensions)
                    {
                        if (!isPV)
                        {
#if DEBUG || SHOW_STATS
                            SearchStatistics.ReductionsNonPV++;
#endif
                            nextDepth -= 1;
                        }

                        if (kingCheckExtension)
                        {
#if DEBUG || SHOW_STATS
                            SearchStatistics.ReductionsKingChecked++;
#endif
                            //  Their last move put us in check, and we need to respond precisely to that, so search an additional ply.
                            nextDepth += 1;
                        }

                        if (!improving)
                        {
#if DEBUG || SHOW_STATS
                            SearchStatistics.ReductionsNotImproving++;
#endif
                            //  Our position doesn't seem to be getting better after our last move,
                            //  so our last move might've just been a bad one and we shouldn't search this node as closely.
                            nextDepth -= 1;
                        }

#if DEBUG || SHOW_STATS
                        if (nextDepth < 1)
                        {
                            SearchStatistics.ReductionsUnder1++;
                        }
#endif

                        nextDepth = Math.Max(nextDepth, 1);
                    }

                    if (UseLateMoveReduction && CanLateMoveReduce(ref info, list[i], depth))
                    {

#if DEBUG || SHOW_STATS
                        SearchStatistics.LMRReductions++;
#endif
                        int reduction;
                        if (UseLogReductionTable)
                        {
                            reduction = LogarithmicReductionTable[depth][i];
                        }
                        else
                        {
                            reduction = GetLateMoveReductionAmount(size, i, depth);
                        }

#if DEBUG || SHOW_STATS
                        SearchStatistics.LMRReductionTotal += (ulong)reduction;
#endif
                        nextDepth -= reduction;

#if DEBUG || SHOW_STATS
                        if (nextDepth < 1)
                        {
                            SearchStatistics.ReductionsUnderLMR1++;
                        }
#endif

                        nextDepth = Math.Max(nextDepth, 1);

                        //  Try a search with LMR applied (depth - 1 - reduction).
                        //  If this fails, we will need to repeat the search at full depth.
                        score = -SimpleSearch.FindBest<NonPVNode>(ref info, -alpha - 1, -alpha, nextDepth, ply + 1);
                    }
                    else
                    {
                        score = alpha + 1;
                    }

                    if (score > alpha)
                    {
                        //  If the alpha from the previous search went above the -alpha - 1,
                        //  We will have to redo the search

                        score = -SimpleSearch.FindBest<NonPVNode>(ref info, -alpha - 1, -alpha, depth - 1, ply + 1);

                        if (score > alpha && score < beta)
                        {
                            //  The previous search fell between the alpha and beta,
                            //  So we treat this as if it were our PV and search again.
                            score = -SimpleSearch.FindBest<PVNode>(ref info, -beta, -alpha, depth - 1, ply + 1);
                        }
                    }
                }

                pos.UnmakeMove();

                if (score > alpha)
                {
                    //  This is the best move so far
                    alpha = score;
                    BestMove = list[i];

                    nodeTypeToSave = TTNodeType.Exact;

                    if (isPV)
                    {
                        PvMoves.Insert(ply, list[i]);
                        int nextPly = ply + 1;
                        while (PvMoves.PlyInitialized(ply, nextPly))
                        {
                            PvMoves.Copy(ply, nextPly);
                            nextPly++;
                        }

                        PvMoves.UpdateLength(ply);
                    }
                }

                if (score >= beta)
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.BetaCutoffs++;
#endif

                    if (!list[i].Capture)
                    {
                        if (UseKillerHeuristic && KillerMoves[ply, 0] != list[i])
                        {
                            KillerMoves.Replace(ply, list[i]);
                        }

                        if (UseHistoryHeuristic)
                        {
                            int history = (depth * depth);
                            HistoryMoves[pos.ToMove, bb.GetPieceAtIndex(list[i].From), list[i].To] += history;
                        }
                    }

                    if (isPV)
                    {
                        PvMoves.Insert(ply, list[i]);
                        int nextPly = ply + 1;
                        while (PvMoves.PlyInitialized(ply, nextPly))
                        {
                            PvMoves.Copy(ply, nextPly);
                            nextPly++;
                        }

                        PvMoves.UpdateLength(ply);
                    }

                    nodeTypeToSave = TTNodeType.Beta;
                    TranspositionTable.Save(posHash, (short)beta, nodeTypeToSave, depth, BestMove);

                    return beta;
                }

            }

            //  We want to replace entries that we have searched to a higher depth since the new evaluation will be more accurate.
            //  But this is only done if the TTEntry was null (move hadn't already been looked at)
            //  or we are sure that this move changes the alpha Score after searching at a higher depth
            if (nodeTypeToSave == TTNodeType.Exact || ttEntry.Depth <= depth || ttEntry.NodeType == TTNodeType.Invalid)
            {
                TranspositionTable.Save(posHash, (short)alpha, nodeTypeToSave, depth, BestMove);
            }
            info.BestMove = BestMove;
            info.BestScore = alpha;

#if DEBUG || SHOW_STATS
            SearchStatistics.NMCompletes++;
#endif

            return alpha;
        }

        
        [MethodImpl(Inline)]
        public static bool CanLateMovePrune(bool isInCheck, bool isPV, bool isRoot, int depth)
        {
            if (!isInCheck && !isPV && !isRoot && depth <= SearchConstants.LMPDepth)
            {
                return true;
            }

            return false;
        }


        [MethodImpl(Inline)]
        public static bool CanRazoringPrune(bool isInCheck, bool isPV, int staticEval, int alpha, int depth)
        {
            if (!isInCheck && !isPV && depth == 1)
            {
                if (staticEval + SearchConstants.RazoringMargin < alpha)
                {
                    return true;
                }
            }

            return false;
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
        public static bool CanReverseFutilityPrune(bool isInCheck, bool isPV, int beta, int depth)
        {
            if (!isInCheck && !isPV && depth <= SearchConstants.ReverseFutilityPruningMaxDepth && !ThreadedEvaluation.IsScoreMate(beta, out _))
            {
                return true;
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static int GetReverseFutilityMargin(int depth, bool improving)
        {
            return (depth * SearchConstants.ReverseFutilityPruningPerDepth) - ((improving ? 1 : 0) * SearchConstants.ReverseFutilityPruningImproving);
        }


        [MethodImpl(Inline)]
        public static bool CanNullMovePrune(Position p, bool isInCheck, bool isPV, int staticEval, int beta, int depth)
        {
            if (!isInCheck && !isPV && depth >= SearchConstants.NullMovePruningMinDepth && staticEval >= beta)
            {
                //  Shouldn't be used in endgames.
                int weakerSideMaterial = Math.Min(p.MaterialCount[Color.White], p.MaterialCount[Color.Black]);
                return (weakerSideMaterial > EvaluationConstants.EndgameMaterial);
            }

            return false;
        }




        [MethodImpl(Inline)]
        public static bool CanLateMoveReduce(ref SearchInformation info, Move m, int depth)
        {
            if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck || m.CausesCheck || m.CausesDoubleCheck)
            {
                return false;
            }

            if (m.Capture || m.Promotion || depth < SearchConstants.LMRDepth)
            {
                return false;
            }

            if (info.Position.bb.IsPasser(m.From))
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
                // Reduce by slightly more if the move is very close to the end of the list.
                return SearchConstants.LMRReductionAmount + (depth / 2);
            }
            else if (isLateInList)
            {
                return SearchConstants.LMRReductionAmount + (depth / 4);
            }

            return SearchConstants.LMRReductionAmount;
        }


        /// <summary>
        /// Returns the PV line from a search, which is the series of moves that the engine thinks will be played.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetPV(in Move[] moves)
        {
            for (int i = 0; i < PvMoves.Count(); i++)
            {
                moves[i] = PvMoves.Get(i);
            }

            return PvMoves.Count();
        }

    }
}
