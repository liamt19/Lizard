using System.Text;

using Lizard.Logic.Search.Ordering;
using Lizard.Logic.Threads;

using static Lizard.Logic.Search.Ordering.MoveOrdering;
using static Lizard.Logic.Transposition.TTEntry;

namespace Lizard.Logic.Search
{
    public static unsafe class Searches
    {
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
            bool isRoot = typeof(NodeType) == typeof(RootNode);
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return QSearch<NodeType>(ref info, ss, alpha, beta, depth);
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

            int startingAlpha = alpha;

            short eval = ss->StaticEval;

            bool doSkip = ss->Skip != Move.Null;
            bool improving = false;


            if (thisThread.IsMain && ((++thisThread.CheckupCount) >= SearchThread.CheckupMax))
            {
                thisThread.CheckupCount = 0;
                //  If we are out of time, or have met/exceeded the max number of nodes, stop now.
                if (info.TimeManager.CheckUp() ||
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
                        return Evaluation.GetEvaluation(pos);
                    }

                }

                //  https://www.chessprogramming.org/Mate_Distance_Pruning
                //  Adjust alpha and beta depending the distance to mate:
                //  If we have a mate in 3, there is no point in searching nodes that can at best lead to a mate in 4, etc.
                alpha = Math.Max(MakeMateScore(ss->Ply), alpha);
                beta = Math.Min(ScoreMate - (ss->Ply + 1), beta);
                if (alpha >= beta)
                {
                    return alpha;
                }
            }

            (ss + 1)->Skip = Move.Null;

            //  TODO: SPRT this with (ss + 2) instead
            (ss + 1)->Killer0 = (ss + 1)->Killer1 = Move.Null;

            ss->DoubleExtensions = (ss - 1)->DoubleExtensions;

            ss->StatScore = 0;
            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(posHash, out TTEntry* tte);
            if (!doSkip)
            {
                ss->TTPV = isPV || (ss->TTHit && tte->PV);
            }

            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;

            //  If this is a root node, we treat the RootMove at index 0 as the ttMove.
            //  Otherwise, we use the TT entry move if it was a TT hit or a null move otherwise.
            CondensedMove ttMove = isRoot ? thisThread.CurrentMove : (ss->TTHit ? tte->BestMove : CondensedMove.Null);

            //  For TT hits, we can accept and return the TT score if:
            //  We aren't in a PV node,
            //  we aren't in a singular extension search,
            //  the TT hit's depth is above the current depth,
            //  the ttScore isn't invalid,
            //  the ttScore is below alpha (or it is just above alpha and we expected this node to fail high),
            //  and the tt entry's bound fits the criteria.
            if (!isPV
                && !doSkip
                && tte->Depth >= depth
                && ttScore != ScoreNone
                && (ttScore < alpha || cutNode)
                && (tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
            {
                return ttScore;
            }

            if (ss->InCheck)
            {
                //  If we are in check, don't bother getting a static evaluation or pruning.
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
                //  Use the static evaluation from the TT if it had one, or get a new one.
                //  We don't overwrite that TT's StatEval score yet though.
                eval = ss->StaticEval = tte->StatEval != ScoreNone ? tte->StatEval : Evaluation.GetEvaluation(pos);

                //  If the ttScore isn't invalid, use that score instead of the static eval.
                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                //  Get the static evaluation and store it in the empty TT slot.
                eval = ss->StaticEval = Evaluation.GetEvaluation(pos);
            }

            if (ss->Ply >= 2)
            {
                //  We are improving if the static evaluation at this ply is better than what it was
                //  when it was our turn 2 plies ago, (or 4 plies if we were in check).
                improving = (ss - 2)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 2)->StaticEval :
                            ((ss - 4)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 4)->StaticEval : true);
            }



            //  We accept Reverse Futility Pruning for:
            //  non-PV nodes
            //  that aren't a response to a previous singular extension search
            //  at a depth at or below the max (currently 8)
            //  which don't have a TT move,
            //  so long as:
            //  The static evaluation (eval) is below a TT win or a mate score,
            //  the eval would cause a beta cutoff,
            //  and the eval is significantly above beta.
            if (UseReverseFutilityPruning
                && !ss->TTPV
                && !doSkip
                && depth <= ReverseFutilityPruningMaxDepth
                && ttMove.Equals(CondensedMove.Null)
                && (eval < ScoreAssuredWin)
                && (eval >= beta)
                && (eval - GetReverseFutilityMargin(depth, improving)) >= beta)
            {
                return eval;
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
                && depth >= NMPMinDepth
                && eval >= beta
                && eval >= ss->StaticEval
                && !doSkip
                && (ss - 1)->CurrentMove != Move.Null
                && pos.MaterialCountNonPawn[pos.ToMove] > 0)
            {
                int reduction = NMPReductionBase + (depth / NMPReductionDivisor);
                ss->CurrentMove = Move.Null;
                ss->ContinuationHistory = history.Continuations[0][0][0];

                //  Skip our turn, and see if the our opponent is still behind even with a free move.
                info.Position.MakeNullMove();
                score = -Negamax<NonPVNode>(ref info, ss + 1, -beta, -beta + 1, depth - reduction, !cutNode);
                info.Position.UnmakeNullMove();

                if (score >= beta)
                {
                    //  Null moves are not allowed to return mate or TT win scores, so ensure the score is below that.
                    return score < ScoreTTWin ? score : beta;
                }
            }


            //  Try ProbCut for:
            //  non-PV nodes
            //  that aren't a response to a previous singular extension search
            //  at a depth at or above the min (currently 5)
            //  while our beta isn't near a mate score
            //  so long as:
            //  We didn't have a TT hit,
            //  or the TT hit's depth is well below the current depth,
            //  or the TT hit's score is above beta + ProbCutBeta(Improving).
            int probBeta = beta + (improving ? ProbCutBetaImproving : ProbCutBeta);
            const int seeThreshold = 1;
            if (UseProbCut
                && !isPV
                && !doSkip
                && depth >= ProbCutMinDepth
                && Math.Abs(beta) < ScoreTTWin
                && (!ss->TTHit || tte->Depth < depth - 3 || tte->Score >= probBeta))
            {
                //  nnnnnnnn/PPPPPPPP/1N1N4/1rbrB3/1QbR1q2/1nRn4/2B5/3K3k w - - 0 1
                //  This position has 88 different captures (the most I could come up with), so 128 as a limit is fair.
                ScoredMove* captures = stackalloc ScoredMove[NormalListCapacity];
                int numCaps = pos.GenAll<GenLoud>(captures);
                AssignProbCutScores(ref bb, captures, numCaps);


                /*
                Score of ProbCut vs Baseline: 904 - 723 - 1676 	3303
                Elo difference: 19.1 +/- 8.31 [10.8, 27.4]
                SPRT: llr 3.55 [0, 5] (-2.94, 2.94) - H1 Accepted
                */

                for (int i = 0; i < numCaps; i++)
                {
                    Move m = OrderNextMove(captures, numCaps, i);
                    if (!pos.IsLegal(m) || !SEE_GE(pos, m, seeThreshold))
                    {
                        //  Skip illegal moves, and captures/promotions that don't result in a positive material trade
                        continue;
                    }

                    int histIdx = PieceToHistory.GetIndex(ourColor, bb.GetPieceAtIndex(m.From), m.To);
                    prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
                    ss->CurrentMove = m;
                    ss->ContinuationHistory = history.Continuations[ss->InCheck ? 1 : 0][m.Capture ? 1 : 0][histIdx];
                    thisThread.Nodes++;

                    pos.MakeMove(m);

                    score = -QSearch<NonPVNode>(ref info, ss + 1, -probBeta, -probBeta + 1, DepthQChecks);

                    if (score >= probBeta)
                    {
                        //  Verify at a low depth
                        score = -Negamax<NonPVNode>(ref info, ss + 1, -probBeta, -probBeta + 1, depth - 4, !cutNode);
                    }

                    pos.UnmakeMove(m);

                    if (score >= probBeta)
                    {
                        return score;
                    }
                }
            }


            if (ttMove.Equals(CondensedMove.Null)
                && cutNode
                && depth >= ExtraCutNodeReductionMinDepth)
            {
                //  We expected this node to be a bad one, so give it an extra depth reduction
                //  if the depth is at or above a threshold (currently 6).
                depth--;
            }


            MovesLoop:

            int legalMoves = 0;     //  Number of legal moves that have been encountered so far in the loop.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            int quietCount = 0;     //  Number of quiet moves that have been played, to a max of 16.
            int captureCount = 0;   //  Number of capture moves that have been played, to a max of 16.

            bool didSkip = false;

            Move* PV = stackalloc Move[MaxPly];
            Move* captureMoves = stackalloc Move[16];
            Move* quietMoves = stackalloc Move[16];

            bool skipQuiets = false;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(pos, ss, history, list, size, ttMove);

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
                        skipQuiets = legalMoves >= LMPTable[improving ? 1 : 0][depth];
                    }

                    bool givesCheck = ((pos.State->CheckSquares[thisPieceType] & SquareBB[toSquare]) != 0);

                    if (skipQuiets && depth <= 8 && !(givesCheck || isCapture))
                    {
                        continue;
                    }

                    if (givesCheck || isCapture || skipQuiets)
                    {
                        //  Once we've found at least 1 move that doesn't lead to mate,
                        //  we can start ignoring checks/captures/quiets that lose us significant amounts of material.
                        if (!SEE_GE(pos, m, -LMRExchangeBase * depth))
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
                    int singleBeta = ttScore - (SingularExtensionsNumerator * depth / 10);
                    int singleDepth = (depth - 1) / 2;

                    ss->Skip = m;
                    score = Negamax<NonPVNode>(ref info, ss, singleBeta - 1, singleBeta, singleDepth, cutNode);
                    ss->Skip = Move.Null;

                    if (score < singleBeta)
                    {
                        //  This move seems to be good, so extend it.
                        extend = 1;

                        if (!isPV
                            && score < singleBeta - SingularExtensionsBeta
                            && ss->DoubleExtensions <= 8)
                        {
                            //  If this isn't a PV, and this move is was a good deal better than any other one,
                            //  then extend by 2 so long as we've double extended less than 8 times.
                            extend = 2;
                        }
                    }
                    else if (singleBeta >= beta)
                    {
                        return singleBeta;
                    }
                    else if (ttScore >= beta || ttScore <= alpha)
                    {
                        //  This move is basically the opposite of singular, so reduce it instead.
                        extend = isPV ? -1 : -extend;
                    }
                }

                int histIdx = PieceToHistory.GetIndex(ourColor, thisPieceType, toSquare);

                ss->DoubleExtensions = (ss - 1)->DoubleExtensions + (extend == 2 ? 1 : 0);

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];
                pos.MakeMove(m);

                thisThread.Nodes++;
                playedMoves++;
                ulong prevNodes = thisThread.Nodes;

                if (isPV)
                {
                    (ss + 1)->PV = null;
                }

                int newDepth = depth - 1 + extend;

                if (depth >= 2
                    && legalMoves >= 2
                    && !isCapture)
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

                    //  Extend killer moves
                    if (m.Equals(ss->Killer0) || m.Equals(ss->Killer1))
                        R--;

                    ss->StatScore = 2 * history.MainHistory[HistoryTable.HistoryIndex(ourColor, m)] +
                                        (*(ss - 1)->ContinuationHistory)[histIdx] +
                                        (*(ss - 2)->ContinuationHistory)[histIdx] +
                                        (*(ss - 4)->ContinuationHistory)[histIdx];

                    R -= (ss->StatScore / (4096 * HistoryReductionMultiplier));

                    //  Clamp the reduction so that the new depth is somewhere in [1, depth]
                    int reducedDepth = Math.Clamp(newDepth - R, 1, newDepth + 1);

                    score = -Negamax<NonPVNode>(ref info, ss + 1, -alpha - 1, -alpha, reducedDepth, true);
                    if (EnableAssertions)
                    {
                        Assert(score is > -ScoreInfinite and < ScoreInfinite,
                            "The returned score = " + score + " from a LMR search was OOB! (should be " + (-ScoreInfinite) + " < score < " + ScoreInfinite);
                    }

                    if (score > alpha && newDepth > reducedDepth)
                    {
                        if (score > (bestScore + LMRExtensionThreshold))
                        {
                            newDepth++;
                        }
                        else if (score < (bestScore + newDepth))
                        {
                            newDepth--;
                        }

                        if (newDepth < reducedDepth)
                        {
                            score = -Negamax<NonPVNode>(ref info, ss + 1, -alpha - 1, -alpha, newDepth, !cutNode);
                        }

                        int bonus = 0;
                        if (score <= alpha)
                        {
                            //  Apply a penalty to this continuation.
                            bonus = -StatBonus(newDepth);
                        }
                        else if (score >= beta)
                        {
                            //  Apply a bonus to this continuation.
                            bonus = StatBonus(newDepth);
                        }

                        UpdateContinuations(ss, ourColor, thisPieceType, m.To, bonus);
                    }
                }
                else if (!isPV || legalMoves > 1)
                {
                    score = -Negamax<NonPVNode>(ref info, ss + 1, -alpha - 1, -alpha, newDepth, !cutNode);
                }

                if (isPV && (playedMoves == 1 || score > alpha))
                {
                    //  Do a new PV search here.

                    (ss + 1)->PV = PV;
                    (ss + 1)->PV[0] = Move.Null;
                    score = -Negamax<PVNode>(ref info, ss + 1, -beta, -alpha, newDepth, false);
                }

                pos.UnmakeMove(m);

                if (isRoot)
                {
                    //  Update the NodeTM table with the number of nodes that were searched in this subtree.
                    thisThread.NodeTable[m.From][m.To] += thisThread.Nodes - prevNodes;
                }

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
                    int rmIndex = 0;
                    for (int j = 0; j < thisThread.RootMoves.Count; j++)
                    {
                        if (thisThread.RootMoves[j].Move == m)
                        {
                            rmIndex = j;
                            break;
                        }
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
                            UpdateStats(pos, ss, bestMove, bestScore, beta, depth, quietMoves, quietCount, captureMoves, captureCount);

                            //  This is a beta cutoff: Don't bother searching other moves because the current one is already too good.
                            break;
                        }

                        alpha = score;
                    }
                }

                if (m != bestMove)
                {
                    //  Add the move to the capture/quiet list.
                    if (isCapture && captureCount < 16)
                    {
                        captureMoves[captureCount++] = m;
                    }
                    else if (!isCapture && quietCount < 16)
                    {
                        quietMoves[quietCount++] = m;
                    }
                }
            }

            if (legalMoves == 0)
            {
                //  We have no legal moves, so we were either checkmated or stalemated.
                bestScore = ss->InCheck ? MakeMateScore(ss->Ply) : ScoreDraw;

                if (didSkip)
                {
                    //  Special case:
                    //  If we skipped the only legal move we had, return alpha instead of an erroneous mate/draw score.
                    bestScore = alpha;
                }
            }

            if (bestScore <= alpha)
            {
                ss->TTPV = ss->TTPV || ((ss - 1)->TTPV && depth > 3);
            }

            if (!doSkip && !(isRoot && thisThread.PVIndex > 0))
            {
                //  Don't update the TT if:
                //  This is a singular extensions search (since the a/b bounds were modified and we skipped a move),
                //  This is one of the root nodes in a MultiPV search.

                TTNodeType bound = (bestScore >= beta) ? TTNodeType.Alpha :
                          ((bestScore > startingAlpha) ? TTNodeType.Exact : TTNodeType.Beta);

                tte->Update(posHash, MakeTTScore((short)bestScore, ss->Ply), bound, depth,
                    (bound == TTNodeType.Beta) ? Move.Null : bestMove, ss->StaticEval, ss->TTPV);
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
            bool isPV = typeof(NodeType) != typeof(NonPVNode);
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
            Move bestMove = Move.Null;

            int score = -ScoreMate - MaxPly;
            short bestScore = -ScoreInfinite;
            short futility = -ScoreInfinite;

            short eval = ss->StaticEval;

            int startingAlpha = alpha;

            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(pos.State->Hash, out TTEntry* tte);
            int ttDepth = ss->InCheck || depth >= DepthQChecks ? DepthQChecks : DepthQNoChecks;
            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;
            CondensedMove ttMove = ss->TTHit ? tte->BestMove : CondensedMove.Null;
            bool ttPV = ss->TTHit && tte->PV;

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
                return ss->InCheck ? ScoreDraw : Evaluation.GetEvaluation(pos);
            }

            if (isPV)
            {
                thisThread.SelDepth = Math.Max(thisThread.SelDepth, ss->Ply + 1);
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
                eval = ss->StaticEval = -ScoreInfinite;
            }
            else
            {
                if (ss->TTHit)
                {
                    eval = ss->StaticEval = tte->StatEval;
                    if (eval == ScoreNone)
                    {
                        //  If the TT hit didn't have a static eval, get one now.
                        eval = ss->StaticEval = Evaluation.GetEvaluation(pos);
                    }

                    if (ttScore != ScoreNone && ((tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0))
                    {
                        //  If the TTEntry has a valid score and the bound is correct, use that score in place of the static eval.
                        eval = ttScore;
                    }
                }
                else
                {
                    if ((ss - 1)->CurrentMove.IsNull())
                    {
                        //  The previous move made was done in NMP (and nothing has changed since (ss - 1)),
                        //  so for simplicity we can use the previous static eval but negative.
                        eval = ss->StaticEval = (short)(-(ss - 1)->StaticEval);
                    }
                    else
                    {
                        eval = ss->StaticEval = Evaluation.GetEvaluation(pos);
                    }
                }

                if (eval >= beta)
                {
                    return eval;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                }

                bestScore = eval;

                futility = (short)(Math.Min(ss->StaticEval, bestScore) + FutilityExchangeBase);
            }

            int prevSquare = (ss - 1)->CurrentMove.IsNull() ? SquareNB : (ss - 1)->CurrentMove.To;
            int legalMoves = 0;
            int movesMade = 0;
            int checkEvasions = 0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignQuiescenceScores(pos, ss, history, list, size, ttMove);

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
                bool givesCheck = ((pos.State->CheckSquares[pos.bb.GetPieceAtIndex(m.From)] & SquareBB[m.To]) != 0);

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
                        && futility > -ScoreWin)
                    {
                        if (legalMoves > 3 && !ss->InCheck)
                        {
                            //  If we've already tried 3 moves and we know that we aren't getting mated,
                            //  only try checks, promotions, and recaptures
                            continue;
                        }

                        short futilityValue = (short)(futility + GetPieceValue(pos.bb.GetPieceAtIndex(m.To)));

                        if (futilityValue <= alpha)
                        {
                            //  Our eval is low, and this move doesn't win us enough material to raise it above alpha.
                            bestScore = Math.Max(bestScore, futilityValue);
                            continue;
                        }

                        if (futility <= alpha && !SEE_GE(pos, m, 1))
                        {
                            //  Our eval is low, and this move doesn't win us material
                            bestScore = Math.Max(bestScore, futility);
                            continue;
                        }
                    }

                    if (checkEvasions >= 2)
                    {
                        //  If we are in check, only consider 2 non-capturing moves.
                        break;
                    }

                    if (!ss->InCheck && !SEE_GE(pos, m, -90))
                    {
                        //  This move loses a significant amount of material
                        continue;
                    }
                }

                if (ss->InCheck && !isCapture)
                {
                    checkEvasions++;
                }

                int histIdx = PieceToHistory.GetIndex(pos.ToMove, pos.bb.GetPieceAtIndex(m.From), m.To);

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[ss->InCheck ? 1 : 0][isCapture ? 1 : 0][histIdx];
                thisThread.Nodes++;

                pos.MakeMove(m);
                score = -QSearch<NodeType>(ref info, ss + 1, -beta, -alpha, depth - 1);
                pos.UnmakeMove(m);

                if (score > bestScore)
                {
                    bestScore = (short)score;

                    if (score > alpha)
                    {
                        bestMove = m;
                        alpha = score;

                        if (isPV)
                        {
                            UpdatePV(ss->PV, m, (ss + 1)->PV);
                        }

                        if (score >= beta)
                        {
                            //  Beta cut
                            break;
                        }
                    }
                }
            }

            if (ss->InCheck && legalMoves == 0)
            {
                return MakeMateScore(ss->Ply);
            }

            TTNodeType bound = (bestScore >= beta) ? TTNodeType.Alpha :
                      ((bestScore > startingAlpha) ? TTNodeType.Exact : TTNodeType.Beta);

            tte->Update(pos.State->Hash, MakeTTScore(bestScore, ss->Ply), bound, depth, bestMove, ss->StaticEval, ss->TTPV);

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

            ref Bitboard bb = ref pos.bb;

            int thisPiece = bb.GetPieceAtIndex(moveFrom);
            int thisColor = bb.GetColorAtIndex(moveFrom);
            int capturedPiece = bb.GetPieceAtIndex(moveTo);

            int quietMoveBonus = StatBonus(depth + 1);
            int quietMovePenalty = StatMalus(depth);

            if (bestMove.Capture)
            {
                int idx = HistoryTable.CapIndex(thisColor, thisPiece, moveTo, capturedPiece);
                history.ApplyBonus(history.CaptureHistory, idx, quietMoveBonus, HistoryTable.CaptureClamp);
            }
            else
            {

                int bestMoveBonus = (bestScore > beta + HistoryCaptureBonusMargin) ? quietMoveBonus : StatBonus(depth);

                if (ss->Killer0 != bestMove)
                {
                    ss->Killer1 = ss->Killer0;
                    ss->Killer0 = bestMove;
                }
                history.ApplyBonus(history.MainHistory, HistoryTable.HistoryIndex(thisColor, bestMove), bestMoveBonus, HistoryTable.MainHistoryClamp);
                UpdateContinuations(ss, thisColor, thisPiece, moveTo, bestMoveBonus);

                for (int i = 0; i < quietCount; i++)
                {
                    Move m = quietMoves[i];
                    history.ApplyBonus(history.MainHistory, HistoryTable.HistoryIndex(thisColor, m), -quietMovePenalty, HistoryTable.MainHistoryClamp);
                    UpdateContinuations(ss, thisColor, bb.GetPieceAtIndex(m.From), m.To, -quietMovePenalty);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                int idx = HistoryTable.CapIndex(thisColor, bb.GetPieceAtIndex(captureMoves[i].From), captureMoves[i].To, bb.GetPieceAtIndex(captureMoves[i].To));
                history.ApplyBonus(history.CaptureHistory, idx, -quietMoveBonus, HistoryTable.CaptureClamp);
            }
        }


        private static readonly int[] ContinuationOffsets = [1, 2, 4, 6];
        /// <summary>
        /// Applies the <paramref name="bonus"/> to the continuation history for the previous 1, 2, 4, and 6 plies, 
        /// given the piece of type <paramref name="pt"/> and color <paramref name="pc"/> moving to the square <paramref name="sq"/>
        /// </summary>
        private static void UpdateContinuations(SearchStackEntry* ss, int pc, int pt, int sq, int bonus)
        {
            foreach (int i in ContinuationOffsets)
            {
                if (ss->InCheck && i > 2)
                {
                    break;
                }

                if ((ss - i)->CurrentMove != Move.Null)
                {
                    (*(ss - i)->ContinuationHistory)[pc, pt, sq] += (short)(bonus -
                    ((*(ss - i)->ContinuationHistory)[pc, pt, sq] * Math.Abs(bonus) / PieceToHistory.Clamp));
                }
            }
        }

        /// <summary>
        /// Calculates a bonus, given the current <paramref name="depth"/>.
        /// </summary>
        [MethodImpl(Inline)]
        private static int StatBonus(int depth)
        {
            return Math.Min((StatBonusMult * depth) - StatBonusSub, StatBonusMax);
        }

        /// <summary>
        /// Calculates a penalty, given the current <paramref name="depth"/>.
        /// </summary>
        [MethodImpl(Inline)]
        private static int StatMalus(int depth)
        {
            return Math.Min((StatMalusMult * depth) - StatMalusSub, StatMalusMax);
        }

        /// <summary>
        /// Returns a safety margin score given the <paramref name="depth"/> and whether or not our 
        /// static evaluation is <paramref name="improving"/> or not.
        /// </summary>
        [MethodImpl(Inline)]
        private static int GetReverseFutilityMargin(int depth, bool improving)
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
                return threshold <= 0;
            }

            ref Bitboard bb = ref pos.bb;

            int from = m.From;
            int to = m.To;

            int swap = EvaluationConstants.SEEValues[bb.PieceTypes[to]] - threshold;
            if (swap < 0)
                return false;

            swap = EvaluationConstants.SEEValues[bb.PieceTypes[from]] - swap;
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

                stmAttackers = attackers & bb.Colors[stm];
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

                    attackers |= GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]);
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

                    attackers |= GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]);
                }
                else if ((temp = stmAttackers & bb.Pieces[Rook]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Rook) - swap) < res)
                        break;

                    attackers |= GetRookMoves(occ, to) & (bb.Pieces[Rook] | bb.Pieces[Queen]);
                }
                else if ((temp = stmAttackers & bb.Pieces[Queen]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = GetPieceValue(Queen) - swap) < res)
                        break;

                    attackers |= (GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen])) | (GetRookMoves(occ, to) & (bb.Pieces[Rook] | bb.Pieces[Queen]));
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
        private static string Debug_GetMovesPlayed(SearchStackEntry* ss)
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
