
#define SHOW_STATS


using System.Runtime.InteropServices;

using LTChess.Logic.Core;
using LTChess.Logic.Data;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.Simple768;
using LTChess.Logic.Search.Ordering;

using static LTChess.Logic.Search.Ordering.MoveOrdering;
using static LTChess.Logic.Transposition.TTEntry;

namespace LTChess.Logic.Search
{
    public static unsafe class Search
    {
        private static readonly int[] SEE_VALUE = new int[] { 126, 781, 825, 1276, 2538, 0, 0 };
        private const int BadSEEScore = -90;

        /// <summary>
        /// If the depth is at or above this, then QSearch will allow non-capture, non-evasion moves that GIVE check.
        /// </summary>
        private const int DepthQChecks = 0;

        /// <summary>
        /// If the depth is at or below this, then QSearch will ignore non-capture, non-evasion moves that GIVE check.
        /// </summary>
        private const int DepthQNoChecks = -1;

        /// <summary>
        /// The best move found in the previous call to Deepen.
        /// If a search is interrupted, this will replace <c>info.BestMove</c>.
        /// </summary>
        private static Move LastBestMove = Move.Null;

        /// <summary>
        /// The evaluation of <see cref="LastBestMove"/>, which will also replace <c>info.BestScore</c> for interrupted searches.
        /// </summary>
        private static int LastBestScore = ThreadedEvaluation.ScoreDraw;


        /// <summary>
        /// Index with [inCheck] [Capture]
        /// <para></para>
        /// Continuations[0][0] is the PieceToHistory[][] for a non-capture while we aren't in check,
        /// and that PieceToHistory[0, 1, 2] is the correct PieceToHistory for a white (0) knight (1) moving to C1 (2).
        /// This is then used by <see cref="MoveOrdering"/>.AssignScores
        /// </summary>
        private static ContinuationHistory[][] Continuations;

        private static HistoryTable History;

        private static PrincipalVariationTable PvMoves = new PrincipalVariationTable();

        private static SearchStackEntry* _SearchStackBlock;


        /// <summary>
        /// We start indexing the SearchStackEntry block at index 10 to prevent indexing outside of bounds.
        /// This really only needs to be 6, so we don't have to check where we are in the Stack when we get
        /// the StaticEval or ContinuationHistory of previous states.
        /// <br></br>
        /// I just set it to 10 to make it a nice round number.
        /// </summary>
        private static readonly SearchStackEntry* _SentinelStart;

        static Search()
        {
            _SearchStackBlock = (SearchStackEntry*) AlignedAllocZeroed((nuint)(sizeof(SearchStackEntry) * MaxPly), AllocAlignment);

            for (int i = 0; i < MaxPly; i++)
            {
                *(_SearchStackBlock + i) = new SearchStackEntry();
            }
            
            _SentinelStart = _SearchStackBlock + 10;


            Continuations = new ContinuationHistory[2][]
            {
                    new ContinuationHistory[2] {new(), new()},
                    new ContinuationHistory[2] {new(), new()},
            };

            History = new HistoryTable();
        }


        /// <summary>
        /// Clears the MainHistory/CaptureHistory tables, as well as the Continuation History. 
        /// This is called when we receive a "ucinewgame" command.
        /// </summary>
        public static void HandleNewGame()
        {
            NativeMemory.Clear(History.MainHistory,    sizeof(short) * HistoryTable.MainHistoryElements);
            NativeMemory.Clear(History.CaptureHistory, sizeof(short) * HistoryTable.CaptureHistoryElements);

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Clear();
                Continuations[i][1].Clear();
            }
        }

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
            //  Increase the age of the Transposition table
            TranspositionTable.TTUpdate();

            //  Reset each of the SearchStack items to the default
            SearchStackEntry* ss = _SentinelStart;
            for (int i = -10; i < MaxSearchStackPly; i++)
            {
                (ss + i)->Clear();
                (ss + i)->Ply = i;
                (ss + i)->ContinuationHistory = Continuations[0][0][0, 0, 0];
            }

            //  Clear out the last search's PV line.
            PvMoves.Clear();

            LastBestMove = Move.Null;
            LastBestScore = ThreadedEvaluation.ScoreDraw;

            info.TimeManager.RestartTimer();

            int depth = 1;

            int maxDepthStart = info.MaxDepth;
            double maxTime = info.TimeManager.MaxSearchTime;

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
                    //Log("Depth " + depth + " aspiration bounds are [A: " + alpha + ", eval: " + LastBestScore + ", B: " + beta + "]");
#endif
                }

                aspirationFailed = false;
                ulong prevNodes = info.NodeCount;

                int score = Negamax<RootNode>(ref info, ss, alpha, beta, info.MaxDepth, false);
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
                    info.OnSearchFinish?.Invoke(ref info);
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


#if DEBUG
                    SearchStatistics.AspirationWindowFails++;
                    SearchStatistics.AspirationWindowTotalDepthFails += (ulong)depth;
                    //Log("Depth " + depth + " failed aspiration bounds, got " + info.BestScore);
                    if (SearchStatistics.AspirationWindowFails > 1000)
                    {
                        Log("Depth " + depth + " failed aspiration bounds " + SearchStatistics.AspirationWindowFails + " times, quitting");
                        return;
                    }
#endif
                    continue;
                }

                info.OnDepthFinish?.Invoke(ref info);


                if (false && continueDeepening && ThreadedEvaluation.IsScoreMate(info.BestScore, out int mateIn))
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

                continueDeepening = (depth <= maxDepthStart && depth < MaxDepth && info.TimeManager.GetSearchTime() <= maxTime);
            }

            info.OnSearchFinish?.Invoke(ref info);
            info.TimeManager.ResetTimer();

            if (UseSimple768)
            {
                NNUEEvaluation.ResetNN();
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
        ///     This essentially represents the best case scenario for our opponent based on the moves we have available to us.
        /// </param>
        /// <param name="depth">
        ///     The depth to search to, which is then extended by QSearch.
        /// </param>
        /// <returns>The evaluation of the best move.</returns>
        [MethodImpl(Optimize)]
        public static int Negamax<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth, bool cutNode) where NodeType : SearchNodeType
        {
            bool isRoot = (typeof(NodeType) == typeof(RootNode));
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));

            //  Check every few thousand nodes if we need to stop the search.
            if ((info.NodeCount & SearchCheckInCount) == 0)
            {
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

            if (isPV)
            {
                PvMoves.InitializeLength(ss->Ply);
                if (ss->Ply > info.SelectiveDepth)
                {
                    info.SelectiveDepth = ss->Ply;
                }
            }

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return QSearch<NodeType>(ref info, (ss), alpha, beta, depth);
            }


            Position pos = info.Position;
            ref Bitboard bb = ref pos.bb;
            ulong posHash = pos.Hash;
            Move bestMove = Move.Null;
            int ourColor = pos.ToMove;

            if (!isRoot)
            {
                if (pos.IsDraw())
                {
                    //  Instead of looking further and probably breaking something,
                    //  Just evaluate this move as a draw here and keep looking at the others.
                    return ScoreDraw;
                }

                alpha = Math.Max(MakeMateScore(ss->Ply), alpha);
                beta = Math.Min(ScoreMate - (ss->Ply + 1), beta);
                if (alpha >= beta)
                {
                    return alpha;
                }
            }

            ss->StatScore = 0;
            (ss + 1)->Killer0 = (ss + 1)->Killer1 = Move.Null;
            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(posHash, out TTEntry* tte);
            short ttScore = (ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply, pos.State->HalfmoveClock) : ScoreNone);
            CondensedMove ttMove = (ss->TTHit ? tte->BestMove : Move.Null);
            ss->TTPV = isPV || (ss->TTHit && tte->PV);

            short eval;
            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;

            bool improving = false;


            if (!isPV && tte->Depth >= depth && ttScore != ScoreNone && (ttScore < alpha || cutNode))
            {
                if ((tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
                {
                    return ttScore;
                }
            }



            if (ss->InCheck)
            {
                ss->StaticEval = eval = ScoreNone;
            }
            else
            {
                if (ss->TTHit)
                {
                    ss->StaticEval = eval = tte->StatEval;
                    if (ss->StaticEval == ScoreNone)
                    {
                        ss->StaticEval = eval = info.GetEvaluation(pos);
                    }

                    if (ttScore != ScoreNone && (tte->Bound & (ttScore > beta ? BoundLower : BoundUpper)) != 0)
                    {
                        eval = ttScore;
                    }
                }
                else
                {
                    ss->StaticEval = eval = info.GetEvaluation(pos);
                    tte->Update(posHash, ScoreNone, TTNodeType.Invalid, TTEntry.DepthNone, Move.Null, eval, ss->TTPV);
                }

                if (ss->Ply >= 2)
                {
                    int improvement = ss->StaticEval - ((ss - 2)->StaticEval != ScoreNone ? (ss - 2)->StaticEval :
                                                       ((ss - 4)->StaticEval != ScoreNone ? (ss - 4)->StaticEval : 173));
                    improving = (ss->Ply >= 2 && improvement > 0);
                }
            }


            if (!isPV && !ss->InCheck)
            {
                if (depth <= ReverseFutilityPruningMaxDepth && (ttMove == Move.Null) &&
                    (eval < ScoreAssuredWin) && (eval >= beta) &&
                    (eval - GetReverseFutilityMargin(depth, improving)) >= beta)
                {
                    return eval;
                }


                if (depth <= RazoringMaxDepth && (eval + (RazoringMargin * depth) <= alpha))
                {
                    score = QSearch<NodeType>(ref info, ss, alpha, beta, 0);
                    if (score <= alpha)
                    {
                        return score;
                    }
                }

                if (depth >= NullMovePruningMinDepth && eval >= beta &&
                    (ss - 1)->CurrentMove != Move.Null && pos.MaterialCountNonPawn[pos.ToMove] > 0)
                {
                    int reduction = SearchConstants.NullMovePruningMinDepth + (depth / SearchConstants.NullMovePruningMinDepth);
                    ss->CurrentMove = Move.Null;

                    info.Position.MakeNullMove();
                    score = -Negamax<NonPVNode>(ref info, (ss + 1), -beta, -beta + 1, depth - reduction, !cutNode);
                    info.Position.UnmakeNullMove();

                    if (score >= beta)
                    {
                        //  Null moves are not allowed to return mate scores, so ensure the score is below that.
                        return Math.Min(score, ScoreMateMax - 1);
                    }
                }
            }

            PieceToHistory*[] contHist = { (ss - 1)->ContinuationHistory, (ss - 2)->ContinuationHistory,
                                            null                        , (ss - 4)->ContinuationHistory,
                                            null                        , (ss - 6)->ContinuationHistory };

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = pos.GenAllPseudoLegalMovesTogether(list);

            Span<int> scores = stackalloc int[size];
            //AssignNormalMoveScores(pos, History, list, scores, ss, size, tte->BestMove);
            AssignScores(ref bb, ss, History, contHist, list, scores, size, tte->BestMove);

            int legalMoves = 0;     //  Number of moves that have been encountered so far in the loop.
            int lmpMoves = 0;       //  Number of non-captures that have been encountered so far.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            int quietCount = 0;     //  Number of quiet moves that have been played, to a max of 64.
            int captureCount = 0;   //  Number of capture moves that have been played, to a max of 32.

            Span<Move> captureMoves = stackalloc Move[32];
            Span<Move> quietMoves = stackalloc Move[64];

            int lmpCutoff = LMPTable[improving ? 1 : 0][depth];

            for (int i = 0; i < size; i++)
            {
                OrderNextMove(list, scores, size, i);

                Move m = list[i];

                if (!pos.IsLegal(m))
                {
                    continue;
                }

                legalMoves++;
                bool isCapture = m.Capture;
                int toSquare = m.To;
                int thisPieceType = bb.GetPieceAtIndex(m.From);

                if (!isCapture)
                {
                    lmpMoves++;

                    if (bestScore > -ScoreMateMax && !isRoot && playedMoves > 0 && lmpMoves >= lmpCutoff)
                    {
                        int lmrDepth = depth - LogarithmicReductionTable[depth][legalMoves];
                        lmrDepth = Math.Max(1, lmrDepth);

                        int seeVal = -40 * (lmrDepth * lmrDepth);
                        if (eval + (SearchConstants.FutilityPruningMarginPerDepth * depth) < alpha && !SEE_GE(pos, m, seeVal))
                        {
                            continue;
                        }
                    }
                }

                int histIdx = PieceToHistory.GetIndex(ourColor, thisPieceType, toSquare);
                prefetch(Unsafe.AsPointer(ref TranspositionTable.GetCluster(pos.HashAfter(m))));
                ss->CurrentMove = m;
                ss->ContinuationHistory = Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];
                pos.MakeMove(m);

                info.NodeCount++;
                playedMoves++;

                int newDepth = depth - 1;
                bool doFullSearch = false;

                if (depth >= 2 && legalMoves >= 3 && !(isPV && isCapture))
                {

                    int R = LogarithmicReductionTable[depth][legalMoves];

                    //  Reduce if our static eval is declining
                    if (!improving)
                        R++;

                    //  Reduce if we think that this move is going to be a bad one
                    if (cutNode)
                        R++;

                    //  Extend for PV searches
                    if (isPV)
                        R--;

                    //  Extend moves that give check
                    if (m.Checks)
                        R--;

                    //  Extend if this move was also the one in the TTEntry
                    if (m == ttMove)
                        R--;


                    ss->StatScore = 2 * History.MainHistory[(ourColor * HistoryTable.MainHistoryPCStride) + m.MoveMask] +
                                        (*contHist[0])[histIdx] +
                                        (*contHist[1])[histIdx] +
                                        (*contHist[3])[histIdx];

                    R -= (ss->StatScore / 10000);

                    //  Clamp the reduction so that the new depth is somewhere in [1, depth]
                    int reducedDepth = Math.Clamp(newDepth - R, 1, newDepth + 1);

                    score = -Negamax<NonPVNode>(ref info, (ss + 1), -alpha - 1, -alpha, reducedDepth, true);
                    Debug.Assert(score > -ScoreInfinite && score < ScoreInfinite);

                    if (score > alpha && newDepth > reducedDepth)
                    {
                        if (score > (bestScore + ExchangeBase))
                        {
                            newDepth++;
                        }
                        else if (score < (bestScore + newDepth))
                        {
                            newDepth--;
                        }

                        if (newDepth < reducedDepth)
                        {
                            score = -Negamax<NonPVNode>(ref info, (ss + 1), -alpha - 1, -alpha, newDepth, !cutNode);
                        }

                        int bonus = 0;
                        if (score <= alpha)
                        {
                            bonus = -StatBonus(newDepth);
                        }
                        else if (score >= beta)
                        {
                            bonus = StatBonus(newDepth);
                        }

                        UpdateContinuations(ss, ourColor, thisPieceType, m.To, bonus);
                    }
                }
                else if (!isPV || legalMoves > 1)
                {
                    score = -Negamax<NonPVNode>(ref info, (ss + 1), -alpha - 1, -alpha, newDepth, !cutNode);
                }

                if (isPV && (playedMoves == 1 || score > alpha))
                {
                    score = -Negamax<PVNode>(ref info, (ss + 1), -beta, -alpha, newDepth, false);
                }

                pos.UnmakeMove(m);

                Debug.Assert(score > -ScoreInfinite && score < ScoreInfinite);

                if (score > bestScore)
                {
                    bestScore = score;

                    if (score > alpha)
                    {
                        //  This is the best move so far
                        bestMove = m;

                        if (isPV)
                        {
                            Search.PvMoves.Insert(ss->Ply, m);
                        }

                        if (score >= beta)
                        {
                            break;
                        }

                        alpha = score;
                    }
                }

                if (m != bestMove)
                {
                    if (isCapture && captureCount < 32)
                    {
                        captureMoves[captureCount++] = m;
                    }
                    else if (!isCapture && quietCount < 64)
                    {
                        quietMoves[quietCount++] = m;
                    }
                }
            }

            if (legalMoves == 0)
            {
                bestScore = (ss->InCheck ? MakeMateScore(ss->Ply) : ScoreDraw);
            }
            else if (bestMove != Move.Null)
            {
                UpdateStats(pos, ss, bestMove, bestScore, beta, depth, quietMoves, quietCount, captureMoves, captureCount);
            }

            if (bestScore <= alpha)
            {
                ss->TTPV = ss->TTPV || ((ss - 1)->TTPV && depth > 3);
            }

            TTNodeType nodeTypeToSave;
            if (bestScore >= beta)
            {
                nodeTypeToSave = TTNodeType.Alpha;
            }
            else if (isPV && !bestMove.IsNull())
            {
                nodeTypeToSave = TTNodeType.Exact;
            }
            else
            {
                nodeTypeToSave = TTNodeType.Beta;
            }

            tte->Update(posHash, MakeTTScore((short)bestScore, ss->Ply), nodeTypeToSave, depth, bestMove, ss->StaticEval, ss->TTPV);
            info.BestMove = bestMove;
            info.BestScore = bestScore;

            return bestScore;
        }


        [MethodImpl(Inline)]
        public static int QSearch<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));

            //  Check every few thousand nodes if we need to stop the search.
            if ((info.NodeCount & SearchCheckInCount) == 0)
            {
                if (info.TimeManager.CheckUp(info.RootPlayerToMove))
                {
                    info.StopSearching = true;
                }
            }

            if (info.StopSearching)
            {
                return 0;
            }

            Position pos = info.Position;
            ulong posHash = pos.Hash;
            Move bestMove = Move.Null;
            int ourColor = pos.ToMove;

            int score;

            short bestScore;
            short futilityBase;
            short futilityValue;

            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(posHash, out TTEntry* tte);
            int ttDepth = (ss->InCheck || depth >= DepthQChecks ? DepthQChecks : DepthQNoChecks);
            short ttScore = (ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply, pos.State->HalfmoveClock) : ScoreNone);
            CondensedMove ttMove = (ss->TTHit ? tte->BestMove : CondensedMove.Null);
            bool ttPV = (ss->TTHit && tte->PV);

            if (info.Position.IsDraw())
            {
                return ScoreDraw;
            }

            if (ss->Ply >= MaxSearchStackPly - 1)
            {
                return ss->InCheck ? 0 : info.GetEvaluation(info.Position);
            }

            if (!isPV && tte->Depth >= ttDepth && ttScore != ScoreNone)
            {
                if ((tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
                {
                    return ttScore;
                }
            }

            if (ss->InCheck)
            {
                bestScore = futilityBase = -ScoreInfinite;
            }
            else
            {
                if (ss->TTHit)
                {
                    if ((ss->StaticEval = bestScore = tte->StatEval) == ScoreNone)
                    {
                        ss->StaticEval = bestScore = info.GetEvaluation(pos);
                    }

                    if (ttScore != ScoreNone && ((tte->Bound & (ttScore > bestScore ? BoundLower : BoundUpper)) != 0))
                    {
                        bestScore = ttScore;
                    }
                }
                else
                {
                    if ((ss - 1)->CurrentMove.IsNull())
                    {
                        ss->StaticEval = bestScore = (short)(-(ss - 1)->StaticEval);
                    }
                    else
                    {
                        ss->StaticEval = bestScore = info.GetEvaluation(pos);
                    }
                }

                if (bestScore >= beta)
                {
                    if (!ss->TTHit)
                    {
                        tte->Update(posHash, MakeTTScore(bestScore, ss->Ply), TTNodeType.Alpha, TTEntry.DepthNone, Move.Null, ss->StaticEval, false);
                    }

                    return bestScore;
                }

                if (bestScore > alpha)
                {
                    alpha = bestScore;
                }

                futilityBase = (short)(Math.Min(ss->StaticEval, bestScore) + ExchangeBase);
            }

            PieceToHistory*[] contHist = { (ss - 1)->ContinuationHistory, (ss - 2)->ContinuationHistory,
                                            null                        , (ss - 4)->ContinuationHistory,
                                            null                        , (ss - 6)->ContinuationHistory };

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = pos.GenAllPseudoLegalMovesTogether(list);

            Span<int> scores = stackalloc int[size];
            //AssignQuiescenceMoveScores(pos, Search.History, list, scores, size);
            AssignScores(ref pos.bb, ss, History, contHist, list, scores, size, tte->BestMove);

            int prevSquare = ((ss - 1)->CurrentMove.IsNull() ? SquareNB : (ss - 1)->CurrentMove.To);
            int legalMoves = 0;
            int captures = 0;
            int quietCheckEvasions = 0;

            for (int i = 0; i < size; i++)
            {
                OrderNextMove(list, scores, size, i);
                Move m = list[i];

                if (!pos.IsLegal(m))
                {
                    continue;
                }

                legalMoves++;

                bool isCapture = m.Capture;
                bool isPromotion = m.Promotion;
                bool givesCheck = m.Checks;

                //  Captures and moves made while in check are always OK.
                //  Moves that give check are only OK if the depth is above the threshold.
                
                //  if (!(isCapture || ss->InCheck || (givesCheck && depth > DepthQNoChecks)))
                if (!(isCapture || ss->InCheck || (givesCheck && ttDepth > DepthQNoChecks)))
                {
                    continue;
                }

                captures++;
                info.NodeCount++;

                if (bestScore > ScoreTTLoss)
                {
                    if (!(givesCheck || isPromotion) && (prevSquare != m.To) && futilityBase > -ScoreWin)
                    {
                        if (legalMoves > 3 && !ss->InCheck)
                        {
                            continue;
                        }

                        futilityValue = (short)(futilityBase + GetPieceValue(pos.bb.GetPieceAtIndex(m.To)));

                        if (futilityValue <= alpha)
                        {
                            bestScore = Math.Max(bestScore, futilityValue);
                            continue;
                        }

                        if (futilityBase <= alpha && !SEE_GE(pos, m, 1))
                        {
                            bestScore = Math.Max(bestScore, futilityBase);
                            continue;
                        }
                    }

                    if (quietCheckEvasions > 2)
                    {
                        break;
                    }

                    if (!ss->InCheck && !SEE_GE(pos, m, BadSEEScore))
                    {
                        continue;
                    }
                }

                if (ss->InCheck && !isCapture)
                {
                    quietCheckEvasions++;
                }

                int histIdx = PieceToHistory.GetIndex(ourColor, pos.bb.GetPieceAtIndex(m.From), m.To);

                prefetch(Unsafe.AsPointer(ref TranspositionTable.GetCluster(pos.HashAfter(m))));
                ss->CurrentMove = m;
                ss->ContinuationHistory = Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];

                pos.MakeMove(m);
                score = -QSearch<NodeType>(ref info, (ss + 1), -beta, -alpha, depth - 1);
                pos.UnmakeMove(m);

                if (score > bestScore)
                {
                    bestScore = (short)score;

                    if (score > alpha)
                    {
                        bestMove = m;

                        if (score < beta)
                        {
                            alpha = score;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (legalMoves == 0)
            {
                return (ss->InCheck ? MakeMateScore(ss->Ply) : ScoreDraw);
            }

            TTNodeType nodeType = (bestScore >= beta) ? TTNodeType.Alpha : TTNodeType.Beta;
            tte->Update(posHash, MakeTTScore(bestScore, ss->Ply), nodeType, ttDepth, bestMove, ss->StaticEval, ttPV);

            Debug.Assert(bestScore > -ScoreInfinite && bestScore < ScoreInfinite);
            return bestScore;
        }





        [MethodImpl(Optimize)]
        private static void UpdateStats(Position pos, SearchStackEntry* ss, Move bestMove, int bestScore, int beta, int depth,
                                Span<Move> quietMoves, int quietCount, Span<Move> captureMoves, int captureCount)
        {

            int moveFrom = bestMove.From;
            int moveTo = bestMove.To;

            int thisPiece = pos.bb.GetPieceAtIndex(moveFrom);
            int thisColor = pos.bb.GetColorAtIndex(moveFrom);
            int capturedPiece = pos.bb.GetPieceAtIndex(moveTo);

            int quietMoveBonus = StatBonus(depth + 1);

            if (bestMove.Capture)
            {
                int idx = HistoryTable.CapIndex(thisPiece, thisColor, moveTo, capturedPiece);
                History.ApplyBonus(History.CaptureHistory, idx, quietMoveBonus, HistoryTable.CaptureClamp);
            }
            else
            {

                int captureBonus = (bestScore > beta + 150) ? quietMoveBonus : StatBonus(depth);

                if (ss->Killer0 != bestMove)
                {
                    ss->Killer1 = ss->Killer0;
                    ss->Killer0 = bestMove;
                }

                History.ApplyBonus(History.MainHistory, ((thisColor * HistoryTable.MainHistoryPCStride) + bestMove.MoveMask), captureBonus, HistoryTable.MainHistoryClamp);

                for (int i = 0; i < quietCount; i++)
                {
                    Move m = quietMoves[i];
                    History.ApplyBonus(History.MainHistory, ((thisColor * HistoryTable.MainHistoryPCStride) + m.MoveMask), -captureBonus, HistoryTable.MainHistoryClamp);
                    UpdateContinuations(ss, thisColor, pos.bb.GetPieceAtIndex(m.From), m.To, -captureBonus);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                int idx = HistoryTable.CapIndex(pos.bb.GetPieceAtIndex(captureMoves[i].From), thisColor, captureMoves[i].To, pos.bb.GetPieceAtIndex(captureMoves[i].To));
                History.ApplyBonus(History.CaptureHistory, idx, -quietMoveBonus, HistoryTable.CaptureClamp);
            }
        }

        [MethodImpl(Inline)]
        private static void UpdateContinuations(SearchStackEntry* ss, int pc, int pt, int sq, int bonus)
        {
            foreach (int i in new int[] {1, 2, 4, 6})
            {
                if (ss->InCheck && i > 2)
                {
                    break;
                }

                if ((ss - i)->CurrentMove != Move.Null)
                {
                    (*(ss - i)->ContinuationHistory)[pc, pt, sq] += (short) (bonus - 
                    (*(ss - i)->ContinuationHistory)[pc, pt, sq] * Math.Abs(bonus) / PieceToHistory.Clamp);
                }
            }
        }

        [MethodImpl(Inline)]
        private static int StatBonus(int depth)
        {
            return Math.Min(250 * depth - 100, 1700);
        }


        [MethodImpl(Inline)]
        public static int GetReverseFutilityMargin(int depth, bool improving)
        {
            return (depth * SearchConstants.ReverseFutilityPruningPerDepth) - ((improving ? 1 : 0) * SearchConstants.ReverseFutilityPruningImproving);
        }


        /// <summary>
        /// Returns true if the move <paramref name="m"/> results in a static exchange outcome above the <paramref name="threshold"/>.
        /// This happens when <paramref name="m"/> starts a series of captures on its "To" square, and at the end of that sequence 
        /// we captured more points of their material than they did ours.
        /// <para></para>
        /// This is mainly from Stockfish's algorithm:
        /// <br></br>
        /// https://github.com/official-stockfish/Stockfish/blob/3f7fb5ac1d58e1c90db063053e9f913b9df79994/src/position.cpp#L1044
        /// </summary>
        [MethodImpl(Inline)]
        public static bool SEE_GE(Position pos, in Move m, int threshold = 1)
        {
            if (m.Castle || m.EnPassant || m.Promotion)
            {
                return (threshold <= 0);
            }

            ref Bitboard bb = ref pos.bb;

            int from = m.From;
            int to = m.To;

            int swap = SEE_VALUE[bb.PieceTypes[to]] - threshold;
            if (swap < 0)
                return false;

            swap = SEE_VALUE[bb.PieceTypes[from]] - swap;
            if (swap <= 0)
                return true;

            //  ulong occ = bb.Occupancy ^ SquareBB[from] ^ SquareBB[to];
            ulong occ = (bb.Occupancy ^ SquareBB[from]) | SquareBB[to];

            ulong attackers = bb.AttackersTo(to, occ);
            ulong stmAttackers;
            ulong temp;

            int stm = pos.ToMove;
            int res = 1;
            while (true)
            {
                stm = Not(stm);
                attackers &= occ;

                stmAttackers = (attackers & bb.Colors[stm]);
                if (stmAttackers == 0)
                {
                    break;
                }

                if ((pos.State->Pinners[Not(stm)] & occ) != 0)
                {
                    stmAttackers &= ~pos.State->BlockingPieces[stm];
                    if (stmAttackers == 0)
                    {
                        break;
                    }
                }

                res ^= 1;

                if ((temp = stmAttackers & bb.Pieces[Pawn]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Pawn) - swap) < res)
                        break;

                    attackers |= (GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]));
                }
                else if ((temp = stmAttackers & bb.Pieces[Knight]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Knight) - swap) < res)
                        break;
                }
                else if ((temp = stmAttackers & bb.Pieces[Bishop]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Bishop) - swap) < res)
                        break;

                    attackers |= (GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]));
                }
                else if ((temp = stmAttackers & bb.Pieces[Rook]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Rook) - swap) < res)
                        break;

                    attackers |= (GetRookMoves(occ, to) & (bb.Pieces[Rook] | bb.Pieces[Queen]));
                }
                else if ((temp = stmAttackers & bb.Pieces[Queen]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Queen) - swap) < res)
                        break;

                    attackers |= ((GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen])) | (GetRookMoves(occ, to) & (bb.Pieces[Rook] | bb.Pieces[Queen])));
                }
                else
                {
                    if ((attackers & ~bb.Pieces[stm]) != 0)
                    {
                        return (res ^ 1) != 0;
                    }
                    else
                    {
                        return res != 0;
                    }
                }
            }

            return res != 0;
        }



        /// <summary>
        /// Returns the PV line from a search, which is the series of moves that the engine thinks will be played.
        /// </summary>
        public static int GetPV(in Move[] moves)
        {
            int max = PvMoves.Count();

            if (max == 0)
            {
                Log("WARN PvMoves.Count was 0, trying to get line[0] anyways");
                int i = 0;
                while (i < PrincipalVariationTable.TableSize)
                {
                    moves[i] = PvMoves.Get(i);
                    if (moves[i].IsNull())
                    {
                        break;
                    }
                    i++;
                }
                max = i;
            }

            for (int i = 0; i < PvMoves.Count(); i++)
            {
                moves[i] = PvMoves.Get(i);
            }

            return max;
        }

    }
}
