
#define SHOW_STATS

using System;
using System.Runtime.InteropServices;

using LTChess.Logic.Core;
using LTChess.Logic.Data;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.Simple768;
using LTChess.Logic.Search.Ordering;
using LTChess.Logic.Threads;

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
        /// Clears the MainHistory/CaptureHistory tables, as well as the Continuation History. 
        /// This is called when we receive a "ucinewgame" command.
        /// </summary>
        public static void HandleNewGame()
        {
            SearchPool.Clear();
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
        public static int Negamax<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth, bool cutNode) where NodeType : SearchNodeType
        {
            bool isRoot = (typeof(NodeType) == typeof(RootNode));
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return QSearch<NodeType>(ref info, (ss), alpha, beta, depth);
            }

            Position pos = info.Position;
            ref Bitboard bb = ref pos.bb;
            SearchThread thisThread = pos.Owner;
            ref HistoryTable history = ref thisThread.History;

            ulong posHash = pos.State->Hash;

            Move bestMove = Move.Null;

            int ourColor = pos.ToMove;
            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;

            short eval = ss->StaticEval;

            bool doSkip = (ss->Skip != Move.Null);
            bool improving = false;


            if (thisThread.IsMain && ((++thisThread.CheckupCount) >= SearchThread.CheckupMax))
            {
                thisThread.CheckupCount = 0;
                //  If we are out of time, or have met/exceeded the max number of nodes, stop now.
                if (info.TimeManager.CheckUp(info.RootPlayerToMove) || 
                    SearchPool.GetNodeCount() >= info.MaxNodes)
                {
                    SearchPool.StopThreads = true;
                }
            }

            if (isPV)
            {
                thisThread.SelDepth = Math.Max(thisThread.SelDepth, ss->Ply + 1);
            }

            if (!isRoot)
            {
                if (pos.IsDraw())
                {
                    return ScoreDraw;
                }

                if (SearchPool.StopThreads || ss->Ply >= MaxSearchStackPly - 1)
                {
                    if (pos.Checked)
                    {
                        //  Instead of looking further and probably breaking something,
                        //  Just evaluate this move as a draw here and keep looking at the others.
                        return ScoreDraw;
                    }
                    else
                    {
                        //  If we aren't in check, then just return the static eval instead of a draw score for consistency.
                        return info.GetEvaluation(pos);
                    }

                }

                alpha = Math.Max(MakeMateScore(ss->Ply), alpha);
                beta = Math.Min(ScoreMate - (ss->Ply + 1), beta);
                if (alpha >= beta)
                {
                    return alpha;
                }
            }

            

            (ss + 1)->Skip = Move.Null;
            (ss + 1)->Killer0 = (ss + 1)->Killer1 = Move.Null;

            ss->Extensions = (ss - 1)->Extensions;

            ss->StatScore = 0;
            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(posHash, out TTEntry* tte);
            if (!doSkip)
            {
                ss->TTPV = isPV || (ss->TTHit && tte->PV);
            }

            short ttScore = (ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply, pos.State->HalfmoveClock) : ScoreNone);
            CondensedMove ttMove = (isRoot ? thisThread.RootMoves[thisThread.PVIndex].CondMove : (ss->TTHit ? tte->BestMove : CondensedMove.Null));

            if (!isPV 
                && !doSkip 
                && tte->Depth > depth 
                && ttScore != ScoreNone 
                && (ttScore < alpha || cutNode))
            {
                if ((tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
                {
                    return ttScore;
                }
            }

            if (ss->InCheck)
            {
                ss->StaticEval = eval = ScoreNone;
                goto MovesLoop;
            }
            
            if (doSkip)
            {
                eval = ss->StaticEval;
            }
            else if (ss->TTHit)
            {
                ss->StaticEval = eval = tte->StatEval;
                if (ss->StaticEval == ScoreNone)
                {
                    ss->StaticEval = eval = info.GetEvaluation(pos);
                }

                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                ss->StaticEval = eval = info.GetEvaluation(pos);
                tte->Update(posHash, ScoreNone, TTNodeType.Invalid, TTEntry.DepthNone, CondensedMove.Null, eval, ss->TTPV);
            }

            if (ss->Ply >= 2)
            {
                improving = ss->StaticEval > ((ss - 2)->StaticEval != ScoreNone ? (ss - 2)->StaticEval :
                                             ((ss - 4)->StaticEval != ScoreNone ? (ss - 4)->StaticEval : 173));
            }




            if (!ss->TTPV
                && depth <= ReverseFutilityPruningMaxDepth
                && (ttMove.Equals(CondensedMove.Null))
                && (eval < ScoreAssuredWin)
                && (eval >= beta)
                && (eval - GetReverseFutilityMargin(depth, improving)) >= beta)
            {
                return eval;
            }


            if (depth <= RazoringMaxDepth && (eval + (RazoringMargin * (depth + 1)) <= alpha))
            {
                score = QSearch<NodeType>(ref info, ss, alpha, beta, 0);
                if (score < alpha)
                {
                    return score;
                }
            }

            if (!isPV
                && depth >= NullMovePruningMinDepth
                && eval >= beta
                && eval >= ss->StaticEval
                && !doSkip
                && (ss - 1)->CurrentMove != Move.Null
                && pos.MaterialCountNonPawn[pos.ToMove] > 0)
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


            if (ttMove.Equals(CondensedMove.Null)) 
            {
                if (isPV)
                {
                    depth -= 2;

                    if (depth <= 0)
                    {
                        return QSearch<PVNode>(ref info, ss, alpha, beta, 0);
                    }
                }

                if (cutNode && depth >= ExtraCutNodeReductionMinDepth)
                {
                    depth--;
                }
            }


            MovesLoop:

            PieceToHistory*[] contHist = { (ss - 1)->ContinuationHistory, (ss - 2)->ContinuationHistory,
                                            null                        , (ss - 4)->ContinuationHistory,
                                            null                        , (ss - 6)->ContinuationHistory };


            int legalMoves = 0;     //  Number of moves that have been encountered so far in the loop.
            int lmpMoves = 0;       //  Number of non-captures that have been encountered so far.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            int quietCount = 0;     //  Number of quiet moves that have been played, to a max of 64.
            int captureCount = 0;   //  Number of capture moves that have been played, to a max of 32.

            bool didSkip = false;

            Move* PV = stackalloc Move[MaxPly];
            Move* captureMoves = stackalloc Move[32];
            Move* quietMoves = stackalloc Move[64];

            bool skipQuiets = false;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(ref bb, ss, history, contHist, list, size, ttMove);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);


                if (m == ss->Skip)
                {
                    didSkip = true;
                    continue;
                }

                if (!pos.IsLegal(m))
                {
                    continue;
                }

                if (EnableAssertions)
                {
                    Assert(pos.IsPseudoLegal(m),
                        "The move " + m + " = " + m.ToString(pos) + " was legal for FEN " + pos.GetFEN() + ", " +
                        "but it isn't pseudo-legal!");
                }


                bool isCapture = m.Capture;
                int toSquare = m.To;
                int thisPieceType = bb.GetPieceAtIndex(m.From);

                legalMoves++;
                int extend = 0;

                if (!isCapture)
                {
                    lmpMoves++;

                    if (bestScore > -ScoreMateMax 
                        && !isRoot 
                        && playedMoves > 0 
                        && lmpMoves >= lmpCutoff)
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

                if (UseSingularExtensions 
                    && !isRoot
                    && !doSkip
                    && ss->Ply < thisThread.RootDepth * 2
                    && depth >= (5 + (isPV && tte->PV ? 1 : 0))
                    && m.Equals(ttMove) 
                    && Math.Abs(ttScore) < ScoreWin 
                    && ((tte->Bound & BoundLower) != 0) 
                    && tte->Depth >= depth - 3)
                {
                    int singleBeta = ttScore - (8 * depth) / 9;
                    int singleDepth = (depth - 1) / 2;

                    ss->Skip = m;
                    score = Negamax<NonPVNode>(ref info, ss, singleBeta - 1, singleBeta, singleDepth, cutNode);
                    ss->Skip = Move.Null;

                    if (score < singleBeta)
                    {
                        extend = 1;

                        if (!isPV
                            && score < singleBeta - 20
                            && ss->Extensions <= 8)
                        {
                            extend = 2;
                            if (depth < 12)
                            {
                                depth++;
                            }
                        }
                    }
                    else if (singleBeta >= beta)
                    {
                        return singleBeta;
                    }
                    else if (ttScore >= beta || ttScore <= alpha)
                    {
                        extend = (isPV ? -1 : -extend);
                    }
                }

                int histIdx = PieceToHistory.GetIndex(ourColor, thisPieceType, toSquare);

                ss->Extensions = (ss - 1)->Extensions + (extend == 2 ? 1 : 0);

                prefetch(Unsafe.AsPointer(ref TranspositionTable.GetCluster(pos.HashAfter(m))));
                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];
                pos.MakeMove(m);

                thisThread.Nodes++;
                playedMoves++;

                if (isPV)
                {
                    (ss + 1)->PV = null;
                }

                int newDepth = depth - 1 + extend;
                bool doFullSearch = false;

                if (depth >= 2 
                    && legalMoves >= 3 
                    && !(isPV && isCapture))
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
                    if (m.Equals(ttMove))
                        R--;


                    ss->StatScore = 2 * history.MainHistory[HistoryTable.HistoryIndex(ourColor, m)] +
                                        (*contHist[0])[histIdx] +
                                        (*contHist[1])[histIdx] +
                                        (*contHist[3])[histIdx];

                    R -= (ss->StatScore / 10000);

                    //  Clamp the reduction so that the new depth is somewhere in [1, depth]
                    int reducedDepth = Math.Clamp(newDepth - R, 1, newDepth + 1);

                    score = -Negamax<NonPVNode>(ref info, (ss + 1), -alpha - 1, -alpha, reducedDepth, true);
                    if (EnableAssertions)
                    {
                        Assert(score is > -ScoreInfinite and < ScoreInfinite, 
                            "The returned score = " + score + " from a LMR search was OOB! (should be " + (-ScoreInfinite) + " < score < " + ScoreInfinite);
                    }

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
                    (ss + 1)->PV = PV;
                    (ss + 1)->PV[0] = Move.Null;
                    score = -Negamax<PVNode>(ref info, (ss + 1), -beta, -alpha, newDepth, false);
                }

                pos.UnmakeMove(m);

                if (EnableAssertions)
                {
                    Assert(score is > -ScoreInfinite and < ScoreInfinite,
                        "The returned score = " + score + " from a recursive call to Negamax was OOB! (should be " + (-ScoreInfinite) + " < score < " + ScoreInfinite);
                }

                if (SearchPool.StopThreads)
                {
                    return ScoreDraw;
                }

                if (isRoot)
                {
                    int rmIndex = -1;
                    for (int j = 0; j < thisThread.RootMoves.Count; j++)
                    {
                        if (thisThread.RootMoves[j].Move == m)
                        {
                            rmIndex = j;
                            break;
                        }
                    }

                    if (EnableAssertions)
                    {
                        Assert(rmIndex != -1, 
                            "Move " + m + " wasn't in this thread's (" + thisThread.ToString() + ") RootMoves!" +
                            "This call to Negamax used a NodeType of RootNode, so any moves encountered should have been placed in the following list: " +
                            "[" + string.Join(", ", thisThread.RootMoves.Select(rootM => rootM.Move)) + "]");
                    }

                    RootMove rm = thisThread.RootMoves[rmIndex];

                    rm.AverageScore = (rm.AverageScore == -ScoreInfinite) ? score : ((rm.AverageScore + (score * 2)) / 3);

                    if (playedMoves == 1 || score > alpha)
                    {
                        rm.Score = score;
                        rm.Depth = thisThread.SelDepth;

                        rm.PVLength = 1;
                        Array.Fill(rm.PV, Move.Null, 1, MaxPly - rm.PVLength);
                        for (Move* childMove = (ss + 1)->PV; *childMove != Move.Null; ++childMove)
                        {
                            rm.PV[rm.PVLength++] = *childMove;
                        }
                    }
                    else
                    {
                        rm.Score = -ScoreInfinite;
                    }


                }

                if (score > bestScore)
                {
                    bestScore = score;

                    if (score > alpha)
                    {
                        //  This is the best move so far
                        bestMove = m;

                        if (isPV && !isRoot)
                        {
                            UpdatePV(ss->PV, m, (ss + 1)->PV);
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
                if (didSkip)
                {
                    bestScore = alpha;
                }
            }
            else if (bestMove != Move.Null)
            {
                UpdateStats(pos, ss, bestMove, bestScore, beta, depth, quietMoves, quietCount, captureMoves, captureCount);
            }

            if (bestScore <= alpha)
            {
                ss->TTPV = ss->TTPV || ((ss - 1)->TTPV && depth > 3);
            }

            TTNodeType nodeTypeToSave = (bestScore >= beta)           ? TTNodeType.Alpha :
                                        ((isPV && !bestMove.IsNull()) ? TTNodeType.Exact : TTNodeType.Beta);

            if (!doSkip && !(isRoot && thisThread.PVIndex > 0))
            {
                tte->Update(posHash, MakeTTScore((short)bestScore, ss->Ply), nodeTypeToSave, depth, bestMove, ss->StaticEval, ss->TTPV);
            }

            info.BestMove = bestMove;
            info.BestScore = bestScore;

            return bestScore;
        }


        public static int QSearch<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));
            if (EnableAssertions)
            {
                Assert(typeof(NodeType) != typeof(RootNode),
                "QSearch(..., depth = " + depth + ") got a NodeType of RootNode, but RootNodes should never enter a QSearch!" +
                (depth < 1 ? " If the depth is 0, this might have been caused by razoring pruning. " +
                             "Otherwise, Negamax was called with probably called a negative depth." : string.Empty));
            }

            Position pos = info.Position;
            SearchThread thisThread = pos.Owner;
            ref HistoryTable history = ref thisThread.History;
            ulong posHash = pos.State->Hash;
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

            Move* PV = stackalloc Move[MaxPly];
            if (isPV)
            {
                (ss + 1)->PV = PV;
                ss->PV[0] = Move.Null;
            }


            if (pos.IsDraw())
            {
                return ScoreDraw;
            }

            if (ss->Ply >= MaxSearchStackPly - 1)
            {
                return ss->InCheck ? ScoreDraw : info.GetEvaluation(pos);
            }

            if (!isPV 
                && tte->Depth >= ttDepth 
                && ttScore != ScoreNone)
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


            int prevSquare = ((ss - 1)->CurrentMove.IsNull() ? SquareNB : (ss - 1)->CurrentMove.To);
            int legalMoves = 0;
            int movesMade = 0;
            int quietCheckEvasions = 0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(ref pos.bb, ss, history, contHist, list, size, ttMove, false);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);

                if (!pos.IsLegal(m))
                {
                    continue;
                }

                if (EnableAssertions)
                {
                    Assert(pos.IsPseudoLegal(m),
                        "The move " + m + " = " + m.ToString(pos) + " was legal for FEN " + pos.GetFEN() + ", " +
                        "but it isn't pseudo-legal!");
                }

                legalMoves++;

                bool isCapture = m.Capture;
                bool isPromotion = m.Promotion;
                bool givesCheck = m.Checks;

                //  Captures and moves made while in check are always OK.
                //  Moves that give check are only OK if the depth is above the threshold.

                if (!(isCapture || ss->InCheck || (givesCheck && ttDepth > DepthQNoChecks)))
                {
                    continue;
                }

                movesMade++;

                if (bestScore > ScoreTTLoss)
                {
                    if (!(givesCheck || isPromotion) 
                        && (prevSquare != m.To) 
                        && futilityBase > -ScoreWin)
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
                ss->ContinuationHistory = history.Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];
                thisThread.Nodes++;

                pos.MakeMove(m);
                score = -QSearch<NodeType>(ref info, (ss + 1), -beta, -alpha, depth - 1);
                pos.UnmakeMove(m);

                if (score > bestScore)
                {
                    bestScore = (short)score;

                    if (score > alpha)
                    {
                        bestMove = m;

                        if (isPV)
                        {
                            UpdatePV(ss->PV, m, (ss + 1)->PV);
                        }

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

            if (EnableAssertions)
            {
                Assert(bestScore is > -ScoreInfinite and < ScoreInfinite, 
                    "A call to QSearch is returning a bestScore of " + bestScore + ", which is OOB! " + 
                    "QSearch's return values should always be in the range (" + -ScoreInfinite + " < " + bestScore + " < " + ScoreInfinite + "). " +
                    "bestValue is only initialized to " + -ScoreInfinite + " when ss->InCheck is true, which it currently " + (ss->InCheck ? "is" : "isn't") + ".");
            }

            return bestScore;
        }


        /// <summary>
        /// Appends the <paramref name="move"/> to the <paramref name="pv"/>, and then appends each non-null move 
        /// in <paramref name="childPV"/> to <paramref name="pv"/>.
        /// </summary>
        private static void UpdatePV(Move* pv, Move move, Move* childPV)
        {
            for (*pv++ = move; childPV != null && *childPV != Move.Null;)
            {
                *pv++ = *childPV++;
            }
            *pv = Move.Null;
        }

        /// <summary>
        /// Updates the killer moves for <paramref name="ss"/>, and applies bonuses or penalties to the 
        /// <paramref name="captureCount"/> captures stored in <paramref name="captureMoves"/>, 
        /// and <paramref name="quietCount"/> quiet moves stored in <paramref name="quietMoves"/>.
        /// </summary>
        private static void UpdateStats(Position pos, SearchStackEntry* ss, Move bestMove, int bestScore, int beta, int depth,
                                Move* quietMoves, int quietCount, Move* captureMoves, int captureCount)
        {
            ref HistoryTable history = ref pos.Owner.History;
            int moveFrom = bestMove.From;
            int moveTo = bestMove.To;

            int thisPiece = pos.bb.GetPieceAtIndex(moveFrom);
            int thisColor = pos.bb.GetColorAtIndex(moveFrom);
            int capturedPiece = pos.bb.GetPieceAtIndex(moveTo);

            int quietMoveBonus = StatBonus(depth + 1);

            if (bestMove.Capture)
            {
                int idx = HistoryTable.CapIndex(thisColor, thisPiece, moveTo, capturedPiece);
                history.ApplyBonus(history.CaptureHistory, idx, quietMoveBonus, HistoryTable.CaptureClamp);
            }
            else
            {

                int captureBonus = (bestScore > beta + 150) ? quietMoveBonus : StatBonus(depth);

                if (ss->Killer0 != bestMove)
                {
                    ss->Killer1 = ss->Killer0;
                    ss->Killer0 = bestMove;
                }

                history.ApplyBonus(history.MainHistory, HistoryTable.HistoryIndex(thisColor, bestMove), captureBonus, HistoryTable.MainHistoryClamp);

                for (int i = 0; i < quietCount; i++)
                {
                    Move m = quietMoves[i];
                    history.ApplyBonus(history.MainHistory, HistoryTable.HistoryIndex(thisColor, m), -captureBonus, HistoryTable.MainHistoryClamp);
                    UpdateContinuations(ss, thisColor, pos.bb.GetPieceAtIndex(m.From), m.To, -captureBonus);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                int idx = HistoryTable.CapIndex(thisColor, pos.bb.GetPieceAtIndex(captureMoves[i].From), captureMoves[i].To, pos.bb.GetPieceAtIndex(captureMoves[i].To));
                history.ApplyBonus(history.CaptureHistory, idx, -quietMoveBonus, HistoryTable.CaptureClamp);
            }
        }

        /// <summary>
        /// Applies the <paramref name="bonus"/> to the continuation history for the previous 1, 2, 4, and 6 plies, 
        /// given the piece of type <paramref name="pt"/> and color <paramref name="pc"/> moving to the square <paramref name="sq"/>
        /// </summary>
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

        /// <summary>
        /// Calculates a bonus, given the current <paramref name="depth"/>.
        /// </summary>
        [MethodImpl(Inline)]
        private static int StatBonus(int depth)
        {
            return Math.Min(250 * depth - 100, 1700);
        }

        /// <summary>
        /// Returns a safety margin score given the <paramref name="depth"/> and whether or not our static evaluation is <paramref name="improving"/> or not.
        /// </summary>
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
        /// Returns a string with the CurrentMove for each state between the first one and the current one.
        /// </summary>
        public static string Debug_GetMovesPlayed(SearchStackEntry* ss)
        {
            StringBuilder sb = new StringBuilder();

            while (ss->Ply >= 0)
            {
                sb.Insert(0, ss->CurrentMove.ToString() + ", ");

                ss--;
            }

            if (sb.Length >= 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }
    }
}
