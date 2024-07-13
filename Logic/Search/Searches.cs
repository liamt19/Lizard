using System.Text;

using Lizard.Logic.NN;
using Lizard.Logic.Search.History;
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
        public static int Negamax<NodeType>(Position pos, SearchStackEntry* ss, int alpha, int beta, int depth, bool cutNode) where NodeType : SearchNodeType
        {
            bool isRoot = typeof(NodeType) == typeof(RootNode);
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return QSearch<NodeType>(pos, ss, alpha, beta, depth);
            }

            if (!isRoot && alpha < ScoreDraw && Cuckoo.HasCycle(pos, ss->Ply))
            {
                alpha = ScoreDraw;
                if (alpha >= beta)
                    return alpha;
            }

            SearchThread thisThread = pos.Owner;
            ref HistoryTable history = ref thisThread.History;
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;

            int us = pos.ToMove;
            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;
            short eval = ss->StaticEval;

            int startingAlpha = alpha;

            bool doSkip = ss->Skip != Move.Null;
            bool improving = false;


            if (thisThread.IsMain && ((++thisThread.CheckupCount) >= SearchThread.CheckupMax))
            {
                thisThread.CheckupCount = 0;
                //  If we are out of time, or have met/exceeded the max number of nodes, stop now.
                if (SearchPool.SharedInfo.TimeManager.CheckUp() ||
                    SearchPool.GetNodeCount() >= SearchPool.SharedInfo.MaxNodes)
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
                    //  Return a draw score or static eval
                    return pos.Checked ? ScoreDraw : NNUE.GetEvaluation(pos);
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

            (ss + 1)->Killer0 = (ss + 1)->Killer1 = Move.Null;

            ss->DoubleExtensions = (ss - 1)->DoubleExtensions;
            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(pos.Hash, out TTEntry* tte);
            if (!doSkip)
            {
                ss->TTPV = isPV || (ss->TTHit && tte->PV);
            }

            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;

            //  If this is a root node, we treat the RootMove at index 0 as the ttMove.
            //  Otherwise, we use the TT entry move if it was a TT hit or a null move otherwise.
            Move ttMove = isRoot ? thisThread.CurrentMove : (ss->TTHit ? tte->BestMove : Move.Null);

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
                eval = ss->StaticEval = tte->StatEval != ScoreNone ? tte->StatEval : NNUE.GetEvaluation(pos);

                //  If the ttScore isn't invalid, use that score instead of the static eval.
                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                //  Get the static evaluation and store it in the empty TT slot.
                eval = ss->StaticEval = NNUE.GetEvaluation(pos);
                
                tte->Update(pos.Hash, ScoreNone, BoundNone, DepthNone, Move.Null, eval, ss->TTPV);
            }

            if (ss->Ply >= 2)
            {
                //  We are improving if the static evaluation at this ply is better than what it was
                //  when it was our turn 2 plies ago, (or 4 plies if we were in check).
                improving = (ss - 2)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 2)->StaticEval :
                            (ss - 4)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 4)->StaticEval : true;
            }



            //  We accept Reverse Futility Pruning for:
            //  non-PV nodes
            //  that aren't a response to a previous singular extension search
            //  at a depth at or below the max (currently 6)
            //  which don't have a TT move,
            //  so long as:
            //  The static evaluation (eval) is below a TT win or a mate score,
            //  the eval would cause a beta cutoff,
            //  and the eval is significantly above beta.
            if (UseRFP
                && !ss->TTPV
                && !doSkip
                && depth <= RFPMaxDepth
                && ttMove.Equals(Move.Null)
                && (eval < ScoreAssuredWin)
                && (eval >= beta)
                && (eval - GetRFPMargin(depth, improving)) >= beta)
            {
                return (eval + beta) / 2;
            }


            //  We accept Null Move Pruning for:
            //  non-PV nodes
            //  at a depth at or above the min (currently 6)
            //  which have a static eval or TT score equal to or above beta
            //  (ditto for ss->StaticEval),
            //  so long as:
            //  The previous node didn't start a singular extension search,
            //  the previous node didn't start a null move search,
            //  and we have non-pawn material (important for Zugzwang).
            if (UseNMP
                && !isPV
                && depth >= NMPMinDepth
                && eval >= beta
                && eval >= ss->StaticEval
                && !doSkip
                && (ss - 1)->CurrentMove != Move.Null
                && pos.MaterialCountNonPawn[pos.ToMove] > 0)
            {
                int reduction = NMPReductionBase + (depth / NMPReductionDivisor) + Math.Min((eval - beta) / NMPEvalDivisor, NMPEvalMin);
                ss->CurrentMove = Move.Null;
                ss->ContinuationHistory = history.Continuations[0][0][0];

                //  Skip our turn, and see if the our opponent is still behind even with a free move.
                pos.MakeNullMove();
                score = -Negamax<NonPVNode>(pos, ss + 1, -beta, -beta + 1, depth - reduction, !cutNode);
                pos.UnmakeNullMove();

                if (score >= beta)
                {
                    //  Null moves are not allowed to return mate or TT win scores, so ensure the score is below that.
                    return score < ScoreTTWin ? score : beta;
                }
            }


            //  Try ProbCut for:
            //  non-PV nodes
            //  that aren't a response to a previous singular extension search
            //  at a depth at or above the min (currently 2!)
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
                int numCaps = pos.GenAll<GenNoisy>(captures);
                AssignProbCutScores(ref bb, captures, numCaps);

                for (int i = 0; i < numCaps; i++)
                {
                    Move m = OrderNextMove(captures, numCaps, i);
                    if (!pos.IsLegal(m) || !SEE_GE(pos, m, seeThreshold))
                    {
                        //  Skip illegal moves, and captures/promotions that don't result in a positive material trade
                        continue;
                    }

                    prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));

                    bool isCap = (bb.GetPieceAtIndex(m.GetTo()) != None && !m.GetCastle());
                    int histIdx = PieceToHistory.GetIndex(us, bb.GetPieceAtIndex(m.GetFrom()), m.GetTo());
                    
                    ss->CurrentMove = m;
                    ss->ContinuationHistory = history.Continuations[ss->InCheck.AsInt()][isCap.AsInt()][histIdx];
                    thisThread.Nodes++;

                    pos.MakeMove(m);

                    score = -QSearch<NonPVNode>(pos, ss + 1, -probBeta, -probBeta + 1, DepthQChecks);

                    if (score >= probBeta)
                    {
                        //  Verify at a low depth
                        score = -Negamax<NonPVNode>(pos, ss + 1, -probBeta, -probBeta + 1, depth - 3, !cutNode);
                    }

                    pos.UnmakeMove(m);

                    if (score >= probBeta)
                    {
                        return score;
                    }
                }
            }


            if (ttMove.Equals(Move.Null)
                && (cutNode || isPV)
                && depth >= ExtraCutNodeReductionMinDepth)
            {
                //  We expected this node to be a bad one, so give it an extra depth reduction
                //  if the depth is at or above a threshold (currently 4).
                depth--;
            }


            MovesLoop:

            int legalMoves = 0;     //  Number of legal moves that have been encountered so far in the loop.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            int quietCount = 0;     //  Number of quiet moves that have been played, to a max of 16.
            int captureCount = 0;   //  Number of capture moves that have been played, to a max of 16.

            bool didSkip = false;

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

                if (isRoot && thisThread.RootMoves.FindIndex(thisThread.PVIndex, r => r.Move == m) == -1)
                {
                    //  For multipv to work properly, we need to skip root moves that are ordered before this one
                    //  since they've already been searched and we don't want them as options again.
                    continue;
                }

                Assert(pos.IsPseudoLegal(m), $"The move {m} = {m.ToString(pos)} was legal for FEN {pos.GetFEN()}, but it isn't pseudo-legal!");

                int moveFrom = m.GetFrom();
                int moveTo = m.GetTo();
                int theirPiece = bb.GetPieceAtIndex(moveTo);
                int ourPiece = bb.GetPieceAtIndex(moveFrom);
                bool isCapture = (theirPiece != None && !m.GetCastle());

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

                    bool givesCheck = ((pos.State->CheckSquares[ourPiece] & SquareBB[moveTo]) != 0);

                    if (skipQuiets && depth <= SkipQuietsMaxDepth && !(givesCheck || isCapture))
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
                    int singleDepth = (depth + SingularExtensionsDepthAugment) / 2;

                    ss->Skip = m;
                    score = Negamax<NonPVNode>(pos, ss, singleBeta - 1, singleBeta, singleDepth, cutNode);
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
                    else if (ttScore >= beta)
                    {
                        extend = -2 + (isPV ? 1 : 0);
                    }
                    else if (cutNode)
                    {
                        extend = -2;
                    }
                    else if (ttScore <= alpha)
                    {
                        extend = -1;
                    }
                }

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));

                int histIdx = PieceToHistory.GetIndex(us, ourPiece, moveTo);

                ss->DoubleExtensions = (ss - 1)->DoubleExtensions + (extend == 2 ? 1 : 0);
                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[ss->InCheck.AsInt()][isCapture.AsInt()][histIdx];
                thisThread.Nodes++;

                pos.MakeMove(m);

                playedMoves++;
                ulong prevNodes = thisThread.Nodes;

                if (isPV)
                {
                    System.Runtime.InteropServices.NativeMemory.Clear((ss + 1)->PV, (nuint)(MaxPly * sizeof(Move)));
                }

                int newDepth = depth + extend;

                if (depth >= 2
                    && legalMoves >= 2
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

                    //  Extend killer moves
                    if (m.Equals(ss->Killer0) || m.Equals(ss->Killer1))
                        R--;

                    var histScore = 2 * history.MainHistory[us, m] + 
                                    2 * (*(ss - 1)->ContinuationHistory)[histIdx] + 
                                        (*(ss - 2)->ContinuationHistory)[histIdx] + 
                                        (*(ss - 4)->ContinuationHistory)[histIdx];

                    R -= (histScore / (4096 * HistoryReductionMultiplier));

                    //  Clamp the reduction so that the new depth is somewhere in [1, depth + extend]
                    //  If we don't reduce at all, then we will just be searching at (depth + extend - 1) as normal.
                    //  With a large number of reductions, this is able to drop directly into QSearch with depth 0.
                    R = Math.Clamp(R, 1, newDepth);
                    int reducedDepth = (newDepth - R);

                    score = -Negamax<NonPVNode>(pos, ss + 1, -alpha - 1, -alpha, reducedDepth, true);

                    //  If we reduced by any amount and got a promising score, then do another search at a slightly deeper depth
                    //  before updating this move's continuation history.
                    if (score > alpha && R > 1)
                    {
                        //  This is mainly SF's idea about a verification search, and updating
                        //  the continuation histories based on the result of this search.
                        newDepth += (score > (bestScore + LMRExtensionThreshold)) ? 1 : 0;
                        newDepth -= (score < (bestScore + newDepth)) ? 1 : 0;

                        if (newDepth - 1 > reducedDepth)
                        {
                            score = -Negamax<NonPVNode>(pos, ss + 1, -alpha - 1, -alpha, newDepth, !cutNode);
                        }

                        int bonus = 0;
                        if (score <= alpha)
                        {
                            //  Apply a penalty to this continuation.
                            bonus = -StatBonus(newDepth - 1);
                        }
                        else if (score >= beta)
                        {
                            //  Apply a bonus to this continuation.
                            bonus = StatBonus(newDepth - 1);
                        }

                        UpdateContinuations(ss, us, ourPiece, m.GetTo(), bonus);
                    }
                }
                else if (!isPV || legalMoves > 1)
                {
                    score = -Negamax<NonPVNode>(pos, ss + 1, -alpha - 1, -alpha, newDepth - 1, !cutNode);
                }

                if (isPV && (playedMoves == 1 || score > alpha))
                {
                    //  Do a new PV search here.
                    //  TODO: Is it fine to use (newDepth - 1) here since it could've been changed in the LMR logic section?
                    (ss + 1)->PV[0] = Move.Null;
                    score = -Negamax<PVNode>(pos, ss + 1, -beta, -alpha, newDepth - 1, false);
                }

                pos.UnmakeMove(m);

                if (isRoot)
                {
                    //  Update the NodeTM table with the number of nodes that were searched in this subtree.
                    thisThread.NodeTable[moveFrom][moveTo] += thisThread.Nodes - prevNodes;
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
                          ((bestScore > startingAlpha) ? TTNodeType.Exact : 
                                                         TTNodeType.Beta);

                Move toSave = (bound == TTNodeType.Beta) ? Move.Null : bestMove;

                tte->Update(pos.Hash, MakeTTScore((short)bestScore, ss->Ply), bound, depth, toSave, ss->StaticEval, ss->TTPV);
            }

            return bestScore;
        }



        /// <summary>
        /// Searches the available checks, captures, and evasions in the position.
        /// <para></para>
        /// This is similar to Negamax, but there is far less pruning going on here, and we are only interested in ensuring that
        /// the score for a particular Negamax node is reasonable if we look at the forcing moves that can be made after that node.
        /// </summary>
        public static int QSearch<NodeType>(Position pos, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            if (alpha < ScoreDraw && Cuckoo.HasCycle(pos, ss->Ply))
            {
                alpha = ScoreDraw;
                if (alpha >= beta)
                    return alpha;
            }

            SearchThread thisThread = pos.Owner;
            ref HistoryTable history = ref thisThread.History;
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;

            int us = pos.ToMove;
            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;
            short futility = -ScoreInfinite;
            short eval = ss->StaticEval;

            int startingAlpha = alpha;

            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(pos.Hash, out TTEntry* tte);
            int ttDepth = ss->InCheck || depth >= DepthQChecks ? DepthQChecks : DepthQNoChecks;
            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;
            Move ttMove = ss->TTHit ? tte->BestMove : Move.Null;
            bool ttPV = ss->TTHit && tte->PV;

            if (isPV)
            {
                ss->PV[0] = Move.Null;
                thisThread.SelDepth = Math.Max(thisThread.SelDepth, ss->Ply + 1);
            }


            if (pos.IsDraw())
            {
                return ScoreDraw;
            }

            if (ss->Ply >= MaxSearchStackPly - 1)
            {
                return ss->InCheck ? ScoreDraw : NNUE.GetEvaluation(pos);
            }

            if (!isPV
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
                    //  If the TT hit didn't have a static eval, get one now.
                    eval = ss->StaticEval = tte->StatEval != ScoreNone ? tte->StatEval : NNUE.GetEvaluation(pos);

                    if (ttScore != ScoreNone && ((tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0))
                    {
                        //  If the TTEntry has a valid score and the bound is correct, use that score in place of the static eval.
                        eval = ttScore;
                    }
                }
                else
                {
                    //  If the previous move made was done in NMP (and nothing has changed since (ss - 1)),
                    //  use the previous static eval but negative. Otherwise get the eval as normal.
                    eval = ss->StaticEval = (ss - 1)->CurrentMove.IsNull() ? (short)(-(ss - 1)->StaticEval) : NNUE.GetEvaluation(pos);
                }

                if (eval >= beta)
                {
                    if (!ss->TTHit)
                        tte->Update(pos.Hash, MakeTTScore(eval, ss->Ply), TTNodeType.Alpha, DepthNone, Move.Null, eval, false);

                    if (Math.Abs(eval) < ScoreTTWin) eval = (short) ((4 * eval + beta) / 5);
                    return eval;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                }

                bestScore = eval;

                futility = (short)(Math.Min(ss->StaticEval, bestScore) + FutilityExchangeBase);
            }

            int prevSquare = (ss - 1)->CurrentMove.IsNull() ? SquareNB : (ss - 1)->CurrentMove.GetTo();
            int legalMoves = 0;
            int movesMade = 0;
            int checkEvasions = 0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegalQS(list, ttDepth);
            AssignQuiescenceScores(pos, ss, history, list, size, ttMove);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);

                if (!pos.IsLegal(m))
                {
                    continue;
                }

                Assert(pos.IsPseudoLegal(m), $"The move {m} = {m.ToString(pos)} was legal for FEN {pos.GetFEN()}, but it isn't pseudo-legal!");

                legalMoves++;

                int moveFrom = m.GetFrom();
                int moveTo = m.GetTo();
                int theirPiece = bb.GetPieceAtIndex(moveTo);
                int ourPiece = bb.GetPieceAtIndex(moveFrom);
                bool isCapture = (theirPiece != None && !m.GetCastle());
                bool givesCheck = ((pos.State->CheckSquares[ourPiece] & SquareBB[moveTo]) != 0);

                movesMade++;

                if (bestScore > ScoreTTLoss)
                {
                    if (!(givesCheck || m.GetPromotion())
                        && (prevSquare != moveTo)
                        && futility > -ScoreWin)
                    {
                        if (legalMoves > 3 && !ss->InCheck)
                        {
                            //  If we've already tried 3 moves and we know that we aren't getting mated,
                            //  only try checks, promotions, and recaptures
                            continue;
                        }

                        short futilityValue = (short)(futility + GetPieceValue(theirPiece));

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

                    if (!ss->InCheck && !SEE_GE(pos, m, -QSSeeThreshold))
                    {
                        //  This move loses a significant amount of material
                        continue;
                    }
                }

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));

                if (ss->InCheck && !isCapture)
                {
                    checkEvasions++;
                }

                int histIdx = PieceToHistory.GetIndex(us, ourPiece, moveTo);

                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[ss->InCheck.AsInt()][isCapture.AsInt()][histIdx];
                thisThread.Nodes++;

                pos.MakeMove(m);
                score = -QSearch<NodeType>(pos, ss + 1, -beta, -alpha, depth - 1);
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
                            if (Math.Abs(bestScore) < ScoreTTWin) bestScore = ((4 * bestScore + beta) / 5);

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
                      ((bestScore > startingAlpha) ? TTNodeType.Exact : 
                                                     TTNodeType.Beta);

            tte->Update(pos.Hash, MakeTTScore((short)bestScore, ss->Ply), bound, ttDepth, bestMove, ss->StaticEval, ss->TTPV);

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
            int moveFrom = bestMove.GetFrom();
            int moveTo = bestMove.GetTo();

            ref Bitboard bb = ref pos.bb;

            int thisPiece = bb.GetPieceAtIndex(moveFrom);
            int thisColor = bb.GetColorAtIndex(moveFrom);
            int capturedPiece = bb.GetPieceAtIndex(moveTo);

            int quietMoveBonus = StatBonus(depth + 1);
            int quietMovePenalty = StatMalus(depth);

            if (capturedPiece != None && !bestMove.GetCastle())
            {
                history.CaptureHistory[thisColor, thisPiece, moveTo, capturedPiece] <<= quietMoveBonus;
            }
            else
            {

                int bestMoveBonus = (bestScore > beta + HistoryCaptureBonusMargin) ? quietMoveBonus : StatBonus(depth);

                if (ss->Killer0 != bestMove && !bestMove.GetEnPassant())
                {
                    ss->Killer1 = ss->Killer0;
                    ss->Killer0 = bestMove;
                }

                history.MainHistory[thisColor, bestMove] <<= bestMoveBonus;
                UpdateContinuations(ss, thisColor, thisPiece, moveTo, bestMoveBonus);

                for (int i = 0; i < quietCount; i++)
                {
                    Move m = quietMoves[i];
                    history.MainHistory[thisColor, m] <<= -quietMovePenalty;
                    UpdateContinuations(ss, thisColor, bb.GetPieceAtIndex(m.GetFrom()), m.GetTo(), -quietMovePenalty);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                Move m = captureMoves[i];
                history.CaptureHistory[thisColor, bb.GetPieceAtIndex(m.GetFrom()), m.GetTo(), bb.GetPieceAtIndex(m.GetTo())] <<= -quietMoveBonus;
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
                    (*(ss - i)->ContinuationHistory)[pc, pt, sq] <<= bonus;
                }
            }
        }

        /// <summary>
        /// Calculates a bonus, given the current <paramref name="depth"/>.
        /// </summary>
        private static int StatBonus(int depth)
        {
            return Math.Min((StatBonusMult * depth) - StatBonusSub, StatBonusMax);
        }

        /// <summary>
        /// Calculates a penalty, given the current <paramref name="depth"/>.
        /// </summary>
        private static int StatMalus(int depth)
        {
            return Math.Min((StatMalusMult * depth) - StatMalusSub, StatMalusMax);
        }

        /// <summary>
        /// Returns a safety margin score given the <paramref name="depth"/> and whether or not our 
        /// static evaluation is <paramref name="improving"/> or not.
        /// </summary>
        private static int GetRFPMargin(int depth, bool improving)
        {
            return (depth - (improving ? 1 : 0)) * RFPMargin;
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
            if (m.GetCastle() || m.GetEnPassant() || m.GetPromotion())
            {
                return threshold <= 0;
            }

            ref Bitboard bb = ref pos.bb;

            int from = m.GetFrom();
            int to = m.GetTo();

            int swap = GetSEEValue(bb.GetPieceAtIndex(to)) - threshold;
            if (swap < 0)
                return false;

            swap = GetSEEValue(bb.GetPieceAtIndex(from)) - swap;
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
