
#define MP_NM
#undef MP_NM

#define MP_QS
#undef MP_QS

using System;
using System.Runtime.InteropServices;
using System.Text;

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
        public const int DepthQChecks = 0;

        /// <summary>
        /// If the depth is at or below this, then QSearch will ignore non-capture, non-evasion moves that GIVE check.
        /// </summary>
        public const int DepthQNoChecks = -1;



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

                //  https://www.chessprogramming.org/Mate_Distance_Pruning
                //  Adjust alpha and beta depending the distance to mate:
                //  If we have a mate in 3, there is not point in searching nodes that can at best lead to a mate in 4, etc.
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

            //  If this is a root node, we treat the RootMove at index 0 as the ttMove.
            //  Otherwise, we use the TT entry move if it was a TT hit or a null move otherwise.
            CondensedMove ttMove = (isRoot ? thisThread.RootMoves[thisThread.PVIndex].CondMove : (ss->TTHit ? tte->BestMove : CondensedMove.Null));

            //  For TT hits, we can accept and return the TT score if:
            //  We aren't in a PV node,
            //  we aren't in a singular extension search,
            //  the TT hit's depth is above the current depth,
            //  the ttScore isn't invalid,
            //  the ttScore is below alpha or we thought this node would fail high,
            //  and the tt entry's bound fits the criteria.
            if (!isPV 
                && !doSkip 
                && tte->Depth > depth 
                && ttScore != ScoreNone
                && (ttScore < alpha || cutNode)
                && (tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
            {
                return ttScore;
            }

            //  If we are in check, don't bother getting a static evaluation or pruning.
            if (ss->InCheck)
            {
                ss->StaticEval = eval = ScoreNone;
                goto MovesLoop;
            }
            
            if (doSkip)
            {
                //  Use the static evaluation from the previous call to Negamax,
                //  which has the same position as this one.
                eval = ss->StaticEval;
            }
            else if (ss->TTHit)
            {
                //  Use the static evaluation from the TT
                ss->StaticEval = eval = tte->StatEval;
                if (ss->StaticEval == ScoreNone)
                {
                    //  But get the actual evaluation if the TT had an invalid score.
                    ss->StaticEval = eval = info.GetEvaluation(pos);
                }

                //  If the ttScore isn't invalid, use that score instead of the static eval.
                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                //  Get the static evaluation and store it in the empty TT slot.
                ss->StaticEval = eval = info.GetEvaluation(pos);
                tte->Update(posHash, ScoreNone, TTNodeType.Invalid, TTEntry.DepthNone, CondensedMove.Null, eval, ss->TTPV);
            }

            if (ss->Ply >= 2)
            {
                //  We are improving if the static evaluation at this ply is better than what it was
                //  when it was our turn 2 plies ago, (or 4 plies if we were in check).
                improving = ss->StaticEval > ((ss - 2)->StaticEval != ScoreNone ? (ss - 2)->StaticEval :
                                             ((ss - 4)->StaticEval != ScoreNone ? (ss - 4)->StaticEval : 173));
            }



            //  We accept Reverse Futility Pruning for:
            //  non-PV nodes
            //  at a depth at or below the max (currently 8)
            //  which don't have a TT move,
            //  so long as:
            //  The static evaluation (eval) is below a TT win or a mate score,
            //  the eval would cause a beta cutoff,
            //  and the eval is significantly above beta.
            if (UseReverseFutilityPruning
                && !ss->TTPV
                && depth <= ReverseFutilityPruningMaxDepth
                && (ttMove.Equals(CondensedMove.Null))
                && (eval < ScoreAssuredWin)
                && (eval >= beta)
                && (eval - GetReverseFutilityMargin(depth, improving)) >= beta)
            {
                return eval;
            }


            //  Try razoring if the depth is at or below the max (currently 6),
            //  and the static evaluation (eval) is extremely bad (far lower than alpha).
            if (UseRazoring
                && depth <= RazoringMaxDepth 
                && (eval + (RazoringMargin * (depth + 1)) <= alpha))
            {
                //  Try only forcing moves and see if any of them raise alpha.
                score = QSearch<NodeType>(ref info, ss, alpha, beta, 0);
                if (score < alpha)
                {
                    //  If none did, then this node is certainly bad.
                    return score;
                }
            }


            //  We accept Null Move Pruning for:
            //  non-PV nodes
            //  at a depth at or above the min (currently 3)
            //  which have a static eval or TT score equal to or above beta
            //  (ditto for ss->StaticEval),
            //  so long as:
            //  The previous node didn't start a singular extension search,
            //  the previous node didn't start a null move search,
            //  and we have non-pawn material (important for Zugzwang).
            if (UseNullMovePruning
                && !isPV
                && depth >= NullMovePruningMinDepth
                && eval >= beta
                && eval >= ss->StaticEval
                && !doSkip
                && (ss - 1)->CurrentMove != Move.Null
                && pos.MaterialCountNonPawn[pos.ToMove] > 0)
            {
                int reduction = NullMovePruningMinDepth + (depth / NullMovePruningMinDepth);
                ss->CurrentMove = Move.Null;
                ss->ContinuationHistory = history.Continuations[0][0][0, 0, 0];

                //  Skip our turn, and see if the our opponent is still behind even with a free move.
                info.Position.MakeNullMove();
                score = -Negamax<NonPVNode>(ref info, (ss + 1), -beta, -beta + 1, depth - reduction, !cutNode);
                info.Position.UnmakeNullMove();

                if (score >= beta)
                {
                    //  Null moves are not allowed to return mate or TT win scores, so ensure the score is below that.
                    return (score < ScoreTTWin ? score : beta);
                }
            }


            if (ttMove.Equals(CondensedMove.Null)) 
            {
                if (isPV)
                {
                    //  PV searches are more heavily scrutinized, so if we didn't get a TT move in this node
                    //  it may not be worth looking as deeply in it.
                    depth -= 2;

                    if (depth <= 0)
                    {
                        //  If we just reduced the depth below 1, go to QSearch instead.
                        return QSearch<PVNode>(ref info, ss, alpha, beta, 0);
                    }
                }

                //  We expected this node to be a bad one, so give it an extra depth reduction
                //  if the depth is at or above a threshold (currently 6).
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
#if MP_NM
            //  Killer0's 4 byte move is followed by a 4 byte buffer, and Killer1's move and buffer immediately follows that.
            //  This is less of a headache to deal with constantly converting ScoredMove's to Move's and vice versa,
            //  but we also need to be much more careful about accessing this pointer (since the Move*[1] is the junk data in Killer0's buffer)
            Move* killers = &ss->Killer0;

            MovePicker mp = new MovePicker(pos, history.MainHistory, history.CaptureHistory, contHist, killers, list, depth, ttMove, SquareNB);

            Move m;
            while ((m = mp.NextMove(skipQuiets)) != Move.Null)
            {
#else
            int size = pos.GenPseudoLegal(list);
            AssignScores(ref bb, ss, history, contHist, list, size, ttMove);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);
#endif

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

                //  If this isn't a root node,
                //  we have a non-mate score for at least one move,
                //  and we have non-pawn material:
                //  We can start skipping quiet moves if we have already seen enough of them at this depth.
                if (!isRoot 
                    && bestScore > ScoreMatedMax 
                    && pos.MaterialCountNonPawn[pos.ToMove] > 0)
                {
                    if (skipQuiets == false)
                    {
                        skipQuiets = (legalMoves >= LMPTable[improving ? 1 : 0][depth]);
                    }

                    if (m.CausesCheck || isCapture || (!isCapture && skipQuiets))
                    {
                        if (!SEE_GE(pos, m, -ExchangeBase * depth))
                        {
                            continue;
                        }
                    }
                }

                //  Try Singular Extensions for:
                //  non-root nodes
                //  which aren't a response to a previous singular extension search,
                //  haven't already been extended significantly,
                //  and have a depth at or above 5 (or 6 for PV searches + PV TT entry hits),
                //  so long as:
                //  The current move is the TT hit's move,
                //  the TT hit's score isn't a definitive win/loss,
                //  the TT hit is an alpha node,
                //  and the TT depth is close to or above the current depth.
                if (UseSingularExtensions 
                    && !isRoot
                    && !doSkip
                    && ss->Ply < thisThread.RootDepth * 2
                    && depth >= (SingularExtensionsMinDepth + (isPV && tte->PV ? 1 : 0))
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

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
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
                    //  Check if we should stop before modifying the root moves or TT.
                    return ScoreDraw;
                }

                if (isRoot)
                {
                    //  Find the corresponding RootMove for the current move.
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

                    //  If AverageScore hasn't been set yet, give it the current score.
                    //  Otherwise, adjust the average up or down slightly.
                    rm.AverageScore = (rm.AverageScore == -ScoreInfinite) ? score : ((rm.AverageScore + (score * 2)) / 3);

                    if (playedMoves == 1 || score > alpha)
                    {
                        //  Update the information for the first move,
                        //  and for any other move that has a higher score than the highest score so far.

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
                        //  Assign an "unset" score, which is treated differently.
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
                            //  Add the current move to the PV, and append the PV from the child node if one exists.
                            UpdatePV(ss->PV, m, (ss + 1)->PV);
                        }

                        if (score >= beta)
                        {
                            //  This is a beta cutoff: Don't bother searching other moves because the current one is already too good.
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
                //  We have no legal moves, so we were either checkmated or stalemated.
                bestScore = (ss->InCheck ? MakeMateScore(ss->Ply) : ScoreDraw);

                if (didSkip)
                {
                    //  Special case:
                    //  If we skipped the only legal move we had, return alpha instead of an erroneous mate/draw score.
                    bestScore = alpha;
                }
            }
            else if (bestMove != Move.Null)
            {
                //  We found at least one move that was better than alpha, so update the history tables.
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
                //  Don't update the TT if:
                //  This is a singular extensions search (since the a/b bounds were modified and we skipped a move),
                //  This is one of the root nodes in a MultiPV search.
                tte->Update(posHash, MakeTTScore((short)bestScore, ss->Ply), nodeTypeToSave, depth, bestMove, ss->StaticEval, ss->TTPV);
            }

            return bestScore;
        }



        /// <summary>
        /// Searches the available checks, captures, and evasions in the position.
        /// <para></para>
        /// This is similar to Negamax, but there is far less pruning going on here, and we are only interested in ensuring that
        /// the score for a particular Negamax node is reasonable if we look at the forcing moves that can be made after that node.
        /// </summary>
        public static int QSearch<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {
            bool isPV = (typeof(NodeType) != typeof(NonPVNode));
            if (EnableAssertions)
            {
                Assert(typeof(NodeType) != typeof(RootNode),
                "QSearch(..., depth = " + depth + ") got a NodeType of RootNode, but RootNodes should never enter a QSearch!" +
                (depth < 1 ? " If the depth is 0, this might have been caused by razoring pruning. " +
                             "Otherwise, Negamax was called with probably called with a negative depth." : string.Empty));
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
                && ttScore != ScoreNone
                && (tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
            {
                return ttScore;
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

#if MP_QS
            //  Killer0's 4 byte move is followed by a 4 byte buffer, and Killer1's move and buffer immediately follows that.
            //  This is less of a headache to deal with constantly converting ScoredMove's to Move's and vice versa,
            //  but we also need to be much more careful about accessing this pointer (since the Move*[1] is the junk data in Killer0's buffer)
            Move* killers = &ss->Killer0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            MovePicker mp = new MovePicker(pos, history.MainHistory, history.CaptureHistory, contHist, killers, list, depth, ttMove, prevSquare);
            Move m;
            while ((m = mp.NextMove()) != Move.Null) 
            {

#else

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(ref pos.bb, ss, history, contHist, list, size, ttMove, false);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);

#endif

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

#if !MP_QS
                //  Captures and moves made while in check are always OK.
                //  Moves that give check are only OK if the depth is above the threshold.

                if (!(isCapture || ss->InCheck || (givesCheck && ttDepth > DepthQNoChecks)))
                {
                    continue;
                }
#endif

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

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
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

            if (ss->InCheck && legalMoves == 0)
            {
                return MakeMateScore(ss->Ply);
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
        /// Returns a safety margin score given the <paramref name="depth"/> and whether or not our 
        /// static evaluation is <paramref name="improving"/> or not.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetReverseFutilityMargin(int depth, bool improving)
        {
            return (depth - (improving ? 1 : 0)) * ReverseFutilityPruningPerDepth;
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
