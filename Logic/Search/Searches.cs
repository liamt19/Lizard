using System.Runtime.CompilerServices;
using System.Text;
using Lizard.Logic.NN;
using Lizard.Logic.Search.History;
using Lizard.Logic.Tablebase;
using Lizard.Logic.Threads;
using static Lizard.Logic.Search.Ordering.MoveOrdering;
using static Lizard.Logic.Transposition.TTEntry;

namespace Lizard.Logic.Search
{
    public static unsafe class Searches
    {

        /// <summary>
        /// Finds the best move according to the Evaluation function, looking at least <paramref name="depth"/> moves in the future.
        /// </summary>
        /// <typeparam name="NodeType">One of <see cref="RootNode"/>, <see cref="PVNode"/>, or <see cref="NonPVNode"/></typeparam>
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
        [SkipLocalsInit]
        public static int Negamax<NodeType>(Position pos, SearchStackEntry* ss, int alpha, int beta, int depth, bool cutNode) where NodeType : SearchNodeType
        {
            bool isRoot = typeof(NodeType) == typeof(RootNode);
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            //  At depth 0, we go into a Quiescence search, which verifies that the evaluation at this depth is reasonable
            //  by checking all of the available captures after the last move (in depth 1).
            if (depth <= 0)
            {
                return QSearch<NodeType>(pos, ss, alpha, beta);
            }

            SearchThread thisThread = pos.Owner;
            TranspositionTable TT = thisThread.TT;

            if (!isRoot && alpha < ScoreDraw && Cuckoo.HasCycle(pos, ss->Ply))
            {
                alpha = MakeDrawScore(thisThread.Nodes);
                if (alpha >= beta)
                    return alpha;
            }

            ref HistoryTable history = ref thisThread.History;
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;

            int us = pos.ToMove;
            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;

            int startingAlpha = alpha;

            short rawEval = ScoreNone;
            short eval = ss->StaticEval;

            bool doSkip = ss->Skip != Move.Null;
            bool improving = false;

#if !DATAGEN
            if (thisThread.IsMain)
            {
                if ((++thisThread.CheckupCount) >= SearchThread.CheckupMax)
                {
                    thisThread.CheckupCount = 0;
                    //  If we are out of time, stop now.
                    if (thisThread.AssocPool.SharedInfo.TimeManager.CheckUp())
                    {
                        thisThread.AssocPool.StopThreads = true;
                    }
                }

                if ((SearchOptions.Threads == 1 && thisThread.Nodes >= thisThread.AssocPool.SharedInfo.NodeLimit) ||
                    (thisThread.CheckupCount == 0 && thisThread.AssocPool.GetNodeCount() >= thisThread.AssocPool.SharedInfo.NodeLimit))
                {
                    thisThread.AssocPool.StopThreads = true;
                }
            }
#else
            if (isPV && (thisThread.Nodes >= thisThread.HardNodeLimit && thisThread.RootDepth > 2))
            {
                thisThread.AssocPool.StopThreads = true;
            }
#endif

            if (isPV)
            {
                thisThread.SelDepth = Math.Max(thisThread.SelDepth, ss->Ply + 1);
            }

            if (!isRoot)
            {
                if (pos.IsDraw(ss->Ply))
                {
                    return MakeDrawScore(thisThread.Nodes);
                }

                if (thisThread.AssocPool.StopThreads || ss->Ply >= MaxSearchStackPly - 1)
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

            (ss + 1)->KillerMove = Move.Null;

            ss->DoubleExtensions = (ss - 1)->DoubleExtensions;
            ss->InCheck = pos.Checked;
            ss->TTHit = TT.Probe(pos.Hash, out TTEntry* tte);
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


            var posPieces = popcount(bb.Occupancy);
            int syzygyMin = -ScoreMate, syzygyMax = ScoreMate;
            if (!isRoot
                && !doSkip
                && posPieces <= TBProbe.TB_LARGEST
                && pos.State->HalfmoveClock == 0
                && pos.State->CastleStatus == CastlingStatus.None)
            {
                var res = Fathom.ProbeWDL(pos);
                if (res != FathomResult.Failed)
                {
                    thisThread.TBHits++;

                    TTNodeType ttFlag;

                    if (res == FathomResult.Win)
                    {
                        score = ScoreTBWin - ss->Ply;
                        ttFlag = TTNodeType.Alpha;
                    }
                    else if (res == FathomResult.Loss)
                    {
                        score = ScoreTBLoss + ss->Ply;
                        ttFlag = TTNodeType.Beta;
                    }
                    else
                    {
                        score = ScoreDraw;
                        ttFlag = TTNodeType.Exact;
                    }

                    if (ttFlag == TTNodeType.Exact
                        || ttFlag == TTNodeType.Beta && score <= alpha
                        || ttFlag == TTNodeType.Alpha && score >= beta)
                    {
                        tte->Update(pos.Hash, MakeTTScore((short)score, ss->Ply), ttFlag, depth, Move.Null, ScoreNone, TT.Age, ss->TTPV);
                        return score;
                    }

                    if (isPV)
                    {
                        if (ttFlag == TTNodeType.Beta)
                            syzygyMax = score;
                        else
                        {
                            alpha = Math.Max(alpha, score);
                            syzygyMin = score;
                        }
                    }
                }
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
                rawEval = eval = ss->StaticEval;
            }
            else if (ss->TTHit)
            {
                //  Use the static evaluation from the TT if it had one, or get a new one.
                //  We don't overwrite that TT's StatEval score yet though.
                rawEval = tte->StatEval != ScoreNone ? tte->StatEval : NNUE.GetEvaluation(pos);

                eval = ss->StaticEval = AdjustEval(thisThread, us, rawEval);

                //  If the ttScore isn't invalid, use that score instead of the static eval.
                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                //  Get the static evaluation and store it in the empty TT slot.
                rawEval = NNUE.GetEvaluation(pos);

                eval = ss->StaticEval = AdjustEval(thisThread, us, rawEval);

                tte->Update(pos.Hash, ScoreNone, BoundNone, DepthNone, Move.Null, rawEval, TT.Age, ss->TTPV);
            }

            if (ss->Ply >= 2)
            {
                //  We are improving if the static evaluation at this ply is better than what it was
                //  when it was our turn 2 plies ago, (or 4 plies if we were in check).
                improving = (ss - 2)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 2)->StaticEval :
                            (ss - 4)->StaticEval != ScoreNone ? ss->StaticEval > (ss - 4)->StaticEval : true;
            }


            if (!(ss - 1)->InCheck
                && (ss - 1)->CurrentMove != Move.Null
                && pos.CapturedPiece == None)
            {
                var val = -QuietOrderMult * ((ss - 1)->StaticEval + ss->StaticEval);
                var bonus = Math.Clamp(val, -QuietOrderMin, QuietOrderMax);
                history.MainHistory[Not(us), (ss - 1)->CurrentMove] <<= bonus;
            }


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


            if (UseRazoring
                && !isPV
                && !doSkip
                && depth <= RazoringMaxDepth
                && alpha < 2000
                && ss->StaticEval + RazoringMult * depth <= alpha)
            {
                score = QSearch<NodeType>(pos, ss, alpha, alpha + 1);
                if (score <= alpha)
                    return score;
            }


            if (UseNMP
                && !isPV
                && !doSkip
                && depth >= NMPMinDepth
                && ss->Ply >= thisThread.NMPPly
                && eval >= beta
                && eval >= ss->StaticEval
                && (ss - 1)->CurrentMove != Move.Null
                && pos.HasNonPawnMaterial(pos.ToMove))
            {
                int reduction = NMPBaseRed + (depth / NMPDepthDiv) + Math.Min((eval - beta) / NMPEvalDiv, NMPEvalMin);
                ss->CurrentMove = Move.Null;
                ss->ContinuationHistory = history.Continuations[0][0][0];

                //  Skip our turn, and see if the our opponent is still behind even with a free move.
                pos.MakeNullMove();
                prefetch(TT.GetCluster(pos.State->Hash));
                score = -Negamax<NonPVNode>(pos, ss + 1, -beta, -beta + 1, depth - reduction, !cutNode);
                pos.UnmakeNullMove();

                if (score >= beta)
                {
                    if (thisThread.NMPPly > 0 || depth <= 15)
                    {
                        return score > ScoreWin ? beta : score;
                    }

                    thisThread.NMPPly = (3 * (depth - reduction) / 4) + ss->Ply;
                    int verification = Negamax<NonPVNode>(pos, ss, beta - 1, beta, depth - reduction, false);
                    thisThread.NMPPly = 0;

                    if (verification >= beta)
                    {
                        return score;
                    }
                }
            }


            if (ttMove.Equals(Move.Null))
            {
                if (cutNode && depth >= IIRMinDepth + 2)
                {
                    depth--;
                }

                if (isPV && depth >= IIRMinDepth)
                {
                    depth--;
                }
            }


            int probBeta = beta + (improving ? ProbcutBetaImp : ProbcutBeta);
            if (UseProbCut
                && !isPV
                && !doSkip
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
                    if (!pos.IsLegal(m) || !SEE_GE(pos, m, Math.Max(1, probBeta - ss->StaticEval)))
                    {
                        //  Skip illegal moves, and captures/promotions that don't result in a positive material trade
                        continue;
                    }

                    prefetch(TT.GetCluster(pos.HashAfter(m)));

                    bool isCap = (bb.GetPieceAtIndex(m.To) != None && !m.IsCastle);
                    int histIdx = PieceToHistory.GetIndex(us, bb.GetPieceAtIndex(m.From), m.To);

                    ss->CurrentMove = m;
                    ss->ContinuationHistory = history.Continuations[ss->InCheck.AsInt()][isCap.AsInt()][histIdx];
                    thisThread.Nodes++;

                    pos.MakeMove(m);

                    score = -QSearch<NonPVNode>(pos, ss + 1, -probBeta, -probBeta + 1);

                    if (score >= probBeta)
                    {
                        //  Verify at a low depth
                        score = -Negamax<NonPVNode>(pos, ss + 1, -probBeta, -probBeta + 1, depth - 3, !cutNode);
                    }

                    pos.UnmakeMove(m);

                    if (score >= probBeta)
                    {
                        tte->Update(pos.Hash, MakeTTScore((short)score, ss->Ply), TTNodeType.Alpha, depth - 2, m, rawEval, TT.Age, ss->TTPV);
                        return score;
                    }
                }
            }


            MovesLoop:

            //  "Small probcut idea" from SF: 
            //  https://github.com/official-stockfish/Stockfish/blob/7ccde25baf03e77926644b282fed68ba0b5ddf95/src/search.cpp#L878
            probBeta = beta + 435;
            if (ss->InCheck
                && !isPV
                && (ttMove != Move.Null && bb.GetPieceAtIndex(ttMove.To) != None)
                && ((tte->Bound & BoundLower) != 0)
                && tte->Depth >= depth - 6
                && ttScore >= probBeta
                && Math.Abs(ttScore) < ScoreTTWin
                && Math.Abs(beta) < ScoreTTWin)
            {
                return probBeta;
            }


            int legalMoves = 0;     //  Number of legal moves that have been encountered so far in the loop.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            int quietCount = 0;     //  Number of quiet moves that have been played, to a max of 16.
            int captureCount = 0;   //  Number of capture moves that have been played, to a max of 16.

            bool didSkip = false;
            int lmpMoves = LMPTable[improving ? 1 : 0][depth];

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

                if (isRoot)
                {
                    //  For multipv to work properly, we need to skip root moves that are ordered before this one
                    //  since they've already been searched and we don't want them as options again.

                    bool cont = true;
                    for (int rmIdx = thisThread.PVIndex; rmIdx < thisThread.RootMoves.Count; rmIdx++)
                    {
                        if (thisThread.RootMoves[rmIdx].Move == m)
                        {
                            cont = false;
                            break;
                        }
                    }

                    if (cont)
                        continue;
                }

                Assert(pos.IsPseudoLegal(m), $"The move {m} = {m.ToString(pos)} was legal for FEN {pos.GetFEN()}, but it isn't pseudo-legal!");

                int moveFrom = m.From;
                int moveTo = m.To;
                int theirPiece = bb.GetPieceAtIndex(moveTo);
                int ourPiece = bb.GetPieceAtIndex(moveFrom);
                bool isCapture = (theirPiece != None && !m.IsCastle);

                legalMoves++;
                int extend = 0;
                int R = LogarithmicReductionTable[depth][legalMoves];
                int moveHist = (isCapture ? history.CaptureHistory[us, ourPiece, moveTo, theirPiece] : history.MainHistory[us, m]);

                if (ShallowPruning
                    && !isRoot
                    && bestScore > ScoreMatedMax
                    && pos.HasNonPawnMaterial(pos.ToMove))
                {
                    if (skipQuiets == false)
                        skipQuiets = legalMoves >= lmpMoves;

                    bool givesCheck = pos.GivesCheck(ourPiece, moveTo);
                    bool isQuiet = !(givesCheck || isCapture);

                    if (isQuiet && skipQuiets && depth <= ShallowMaxDepth)
                        continue;

                    int lmrRed = (R * 1024) + NMFutileBase;

                    lmrRed += (!isPV).AsInt() * NMFutilePVCoeff;
                    lmrRed += (!improving).AsInt() * NMFutileImpCoeff;
                    lmrRed -= (moveHist / (isCapture ? LMRCaptureDiv : LMRQuietDiv)) * NMFutileHistCoeff;

                    lmrRed /= 1024;
                    int lmrDepth = Math.Max(0, depth - lmrRed);

                    int futilityMargin = NMFutMarginB + (lmrDepth * NMFutMarginM) + (moveHist / NMFutMarginDiv);
                    if (isQuiet
                        && !ss->InCheck
                        && lmrDepth <= 8
                        && ss->StaticEval + futilityMargin < alpha)
                    {
                        skipQuiets = true;
                        continue;
                    }

                    var seeMargin = -ShallowSEEMargin * depth;
                    if ((!isQuiet || skipQuiets) && !SEE_GE(pos, m, seeMargin))
                    {
                        continue;
                    }
                }

                if (UseSingularExtensions
                    && !isRoot
                    && !doSkip
                    && m.Equals(ttMove)
                    && ss->Ply < thisThread.RootDepth * 2
                    && Math.Abs(ttScore) < ScoreWin)
                {
                    var SEDepth = SEMinDepth + ((isPV && tte->PV) ? 1 : 0);

                    if (depth >= SEDepth
                        && ((tte->Bound & BoundLower) != 0)
                        && tte->Depth >= depth - 3)
                    {
                        int singleBeta = ttScore - (SENumerator * depth / 10);
                        int singleDepth = (depth + SEDepthAdj) / 2;

                        ss->Skip = m;
                        score = Negamax<NonPVNode>(pos, ss, singleBeta - 1, singleBeta, singleDepth, cutNode);
                        ss->Skip = Move.Null;

                        if (score < singleBeta)
                        {
                            bool doubleExt = !isPV && ss->DoubleExtensions <= 8 && (score < singleBeta - SEDoubleMargin);
                            bool tripleExt = doubleExt && (score < singleBeta - SETripleMargin - (isCapture.AsInt() * SETripleCapSub));

                            //  This move seems to be good, so extend it.
                            extend = 1 + doubleExt.AsInt() + tripleExt.AsInt();
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
                    else if (depth < SEDepth
                        && !ss->InCheck
                        && eval < alpha - 25
                        && (tte->Bound & BoundLower) != 0)
                    {
                        extend = 1;
                    }
                }


                prefetch(TT.GetCluster(pos.HashAfter(m)));

                int histIdx = PieceToHistory.GetIndex(us, ourPiece, moveTo);

                ss->DoubleExtensions = (short)((ss - 1)->DoubleExtensions + (extend >= 2).AsInt());
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

                    //  Reduce if our static eval is declining
                    R += (!improving).AsInt();

                    //  Reduce if we think that this move is going to be a bad one
                    R += cutNode.AsInt() * 2;

                    R -= ss->TTPV.AsInt();

                    //  Extend for PV searches
                    R -= isPV.AsInt();

                    //  Extend killer moves
                    R -= (m == ss->KillerMove).AsInt();

                    var histScore = 2 * moveHist +
                                    2 * (*(ss - 1)->ContinuationHistory)[histIdx] +
                                        (*(ss - 2)->ContinuationHistory)[histIdx] +
                                        (*(ss - 4)->ContinuationHistory)[histIdx];

                    R -= (histScore / (isCapture ? LMRCaptureDiv : LMRQuietDiv));

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
                        newDepth += (score > (bestScore + DeeperMargin + 4 * newDepth)) ? 1 : 0;
                        newDepth -= (score < (bestScore + newDepth)) ? 1 : 0;

                        if (newDepth - 1 > reducedDepth)
                        {
                            score = -Negamax<NonPVNode>(pos, ss + 1, -alpha - 1, -alpha, newDepth - 1, !cutNode);
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

                        UpdateContinuations(ss, us, ourPiece, m.To, bonus);
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

                if (thisThread.AssocPool.StopThreads)
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

            if (legalMoves != 0)
                bestScore = int.Clamp(bestScore, syzygyMin, syzygyMax);

            if (!doSkip && !(isRoot && thisThread.PVIndex > 0))
            {
                //  Don't update the TT if:
                //  This is a singular extensions search (since the a/b bounds were modified and we skipped a move),
                //  This is one of the root nodes in a MultiPV search.

                TTNodeType bound = (bestScore >= beta) ? TTNodeType.Alpha :
                          ((bestScore > startingAlpha) ? TTNodeType.Exact : 
                                                         TTNodeType.Beta);

                Move toSave = (bound == TTNodeType.Beta) ? Move.Null : bestMove;

                tte->Update(pos.Hash, MakeTTScore((short)bestScore, ss->Ply), bound, depth, toSave, rawEval, TT.Age, ss->TTPV);

                if (!ss->InCheck
                    && (bestMove.IsNull() || !pos.IsNoisy(bestMove))
                    && !(bound == TTNodeType.Alpha && bestScore <= ss->StaticEval)
                    && !(bound == TTNodeType.Beta && bestScore >= ss->StaticEval))
                {
                    var diff = bestScore - ss->StaticEval;
                    UpdateCorrectionHistory(pos, diff, depth);
                }
            }

            return bestScore;
        }



        /// <summary>
        /// Searches the available checks, captures, and evasions in the position.
        /// <para></para>
        /// This is similar to Negamax, but there is far less pruning going on here, and we are only interested in ensuring that
        /// the score for a particular Negamax node is reasonable if we look at the forcing moves that can be made after that node.
        /// </summary>
        [SkipLocalsInit]
        public static int QSearch<NodeType>(Position pos, SearchStackEntry* ss, int alpha, int beta) where NodeType : SearchNodeType
        {
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            SearchThread thisThread = pos.Owner;
            TranspositionTable TT = thisThread.TT;

            if (alpha < ScoreDraw && Cuckoo.HasCycle(pos, ss->Ply))
            {
                alpha = MakeDrawScore(thisThread.Nodes);
                if (alpha >= beta)
                    return alpha;
            }

            ref HistoryTable history = ref thisThread.History;
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;

            int us = pos.ToMove;
            bool inCheck = pos.Checked;

            int score = -ScoreMate - MaxPly;
            int bestScore = -ScoreInfinite;
            short futility = -ScoreInfinite;

            short rawEval = ScoreNone;
            short eval = ss->StaticEval;

            int startingAlpha = alpha;

            ss->InCheck = inCheck;
            ss->TTHit = TT.Probe(pos.Hash, out TTEntry* tte);
            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;
            Move ttMove = ss->TTHit ? tte->BestMove : Move.Null;
            bool ttPV = ss->TTHit && tte->PV;

            if (isPV)
            {
                ss->PV[0] = Move.Null;
                thisThread.SelDepth = Math.Max(thisThread.SelDepth, ss->Ply + 1);
            }


            if (pos.IsDraw(ss->Ply))
            {
                return ScoreDraw;
            }

            if (ss->Ply >= MaxSearchStackPly - 1)
            {
                return inCheck ? ScoreDraw : NNUE.GetEvaluation(pos);
            }

            if (!isPV
                && ttScore != ScoreNone
                && (tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
            {
                return ttScore;
            }

            if (inCheck)
            {
                eval = ss->StaticEval = -ScoreInfinite;
            }
            else
            {
                if (ss->TTHit)
                {
                    //  If the TT hit didn't have a static eval, get one now.
                    rawEval = tte->StatEval != ScoreNone ? tte->StatEval : NNUE.GetEvaluation(pos);

                    eval = ss->StaticEval = AdjustEval(thisThread, us, rawEval);

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
                    rawEval = (ss - 1)->CurrentMove.IsNull() ? (short)(-(ss - 1)->StaticEval) : NNUE.GetEvaluation(pos);

                    eval = ss->StaticEval = AdjustEval(thisThread, us, rawEval);
                }

                if (eval >= beta)
                {
                    if (!ss->TTHit)
                        tte->Update(pos.Hash, MakeTTScore(eval, ss->Ply), TTNodeType.Alpha, DepthNone, Move.Null, rawEval, TT.Age, false);

                    if (Math.Abs(eval) < ScoreTTWin) 
                        eval = (short) ((4 * eval + beta) / 5);

                    return eval;
                }

                alpha = Math.Max(eval, alpha);

                bestScore = eval;
                futility = (short)(bestScore + QSFutileMargin);
            }

            int legalMoves = 0;
            int quietEvasions = 0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegalQS(list);
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

                int moveFrom = m.From;
                int moveTo = m.To;
                int ourPiece = bb.GetPieceAtIndex(moveFrom);

                bool isCapture = pos.IsCapture(m);
                if (bestScore > ScoreTTLoss)
                {
                    if (quietEvasions >= 2)
                        break;

                    if (!inCheck && legalMoves > 3)
                        continue;

                    if (!inCheck && futility <= alpha && !SEE_GE(pos, m, 1))
                    {
                        bestScore = Math.Max(bestScore, futility);
                        continue;
                    }

                    if (!inCheck && !SEE_GE(pos, m, -QSSeeMargin))
                    {
                        continue;
                    }
                }

                prefetch(TT.GetCluster(pos.HashAfter(m)));

                if (inCheck && !isCapture)
                {
                    quietEvasions++;
                }

                int histIdx = PieceToHistory.GetIndex(us, ourPiece, moveTo);

                ss->CurrentMove = m;
                ss->ContinuationHistory = history.Continuations[inCheck.AsInt()][isCapture.AsInt()][histIdx];
                thisThread.Nodes++;

                pos.MakeMove(m);
                score = -QSearch<NodeType>(pos, ss + 1, -beta, -alpha);
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
                            if (Math.Abs(bestScore) < ScoreTTWin) 
                                bestScore = ((4 * bestScore + beta) / 5);

                            break;
                        }
                    }
                }
            }

            if (inCheck && legalMoves == 0)
            {
                return MakeMateScore(ss->Ply);
            }

            var bound = (bestScore >= beta) ? TTNodeType.Alpha : TTNodeType.Beta;

            tte->Update(pos.Hash, MakeTTScore((short)bestScore, ss->Ply), bound, 0, bestMove, rawEval, TT.Age, ttPV);

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

            int bonus = StatBonus(depth);
            int malus = StatMalus(depth);

            if (capturedPiece != None && !bestMove.IsCastle)
            {
                history.CaptureHistory[thisColor, thisPiece, moveTo, capturedPiece] <<= bonus;
            }
            else
            {
                if (!bestMove.IsEnPassant)
                {
                    ss->KillerMove = bestMove;
                }

                //  Idea from Ethereal:
                //  Don't reward/punish moves resulting from a trivial, low-depth cutoff
                if (quietCount == 0 && depth <= 3)
                {
                    return;
                }

                history.MainHistory[thisColor, bestMove] <<= bonus;
                if (ss->Ply < PlyHistoryTable.MaxPlies)
                    history.PlyHistory[ss->Ply, bestMove] <<= bonus;

                UpdateContinuations(ss, thisColor, thisPiece, moveTo, bonus);

                for (int i = 0; i < quietCount; i++)
                {
                    Move m = quietMoves[i];
                    thisPiece = bb.GetPieceAtIndex(m.From);

                    history.MainHistory[thisColor, m] <<= -malus;
                    if (ss->Ply < PlyHistoryTable.MaxPlies)
                        history.PlyHistory[ss->Ply, m] <<= -malus;

                    UpdateContinuations(ss, thisColor, thisPiece, m.To, -malus);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                Move m = captureMoves[i];
                thisPiece = bb.GetPieceAtIndex(m.From);
                capturedPiece = bb.GetPieceAtIndex(m.To);

                history.CaptureHistory[thisColor, thisPiece, m.To, capturedPiece] <<= -malus;
            }
        }


        private static short AdjustEval(SearchThread thread, int us, short rawEval)
        {
            Position pos = thread.RootPosition;

            rawEval = (short)(rawEval * (200 - pos.State->HalfmoveClock) / 200);

            var pch = thread.History.PawnCorrection[pos, us] / CorrectionGrain;
            var mchW = thread.History.NonPawnCorrection[pos, us, White] / CorrectionGrain;
            var mchB = thread.History.NonPawnCorrection[pos, us, Black] / CorrectionGrain;

            var corr = (pch * 200 + mchW * 100 + mchB * 100) / 300;

            return (short)(rawEval + corr);
        }


        private static void UpdateCorrectionHistory(Position pos, int diff, int depth)
        {
            var scaledWeight = Math.Min((depth * depth) + 1, 128);

            ref var pawnCh = ref pos.Owner.History.PawnCorrection[pos, pos.ToMove];
            var pawnBonus = (pawnCh * (CorrectionScale - scaledWeight) + (diff * CorrectionGrain * scaledWeight)) / CorrectionScale;
            pawnCh = (StatEntry)Math.Clamp(pawnBonus, -CorrectionMax, CorrectionMax);

            ref var nonPawnChW = ref pos.Owner.History.NonPawnCorrection[pos, pos.ToMove, White];
            var nonPawnBonusW = (nonPawnChW * (CorrectionScale - scaledWeight) + (diff * CorrectionGrain * scaledWeight)) / CorrectionScale;
            nonPawnChW = (StatEntry)Math.Clamp(nonPawnBonusW, -CorrectionMax, CorrectionMax);

            ref var nonPawnChB = ref pos.Owner.History.NonPawnCorrection[pos, pos.ToMove, Black];
            var nonPawnBonusB = (nonPawnChB * (CorrectionScale - scaledWeight) + (diff * CorrectionGrain * scaledWeight)) / CorrectionScale;
            nonPawnChB = (StatEntry)Math.Clamp(nonPawnBonusB, -CorrectionMax, CorrectionMax);
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
            if (m.IsCastle || m.IsEnPassant || m.IsPromotion)
            {
                return threshold <= 0;
            }

            ref Bitboard bb = ref pos.bb;

            int from = m.From;
            int to = m.To;

            int swap = GetSEEValue(bb.GetPieceAtIndex(to)) - threshold;
            if (swap < 0)
                return false;

            swap = GetSEEValue(bb.GetPieceAtIndex(from)) - swap;
            if (swap <= 0)
                return true;

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
                    if ((swap = SEEValuePawn - swap) < res)
                        break;

                    attackers |= GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]);
                }
                else if ((temp = stmAttackers & bb.Pieces[Knight]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = SEEValueKnight - swap) < res)
                        break;
                }
                else if ((temp = stmAttackers & bb.Pieces[Bishop]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = SEEValueBishop - swap) < res)
                        break;

                    attackers |= GetBishopMoves(occ, to) & (bb.Pieces[Bishop] | bb.Pieces[Queen]);
                }
                else if ((temp = stmAttackers & bb.Pieces[Rook]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = SEEValueRook - swap) < res)
                        break;

                    attackers |= GetRookMoves(occ, to) & (bb.Pieces[Rook] | bb.Pieces[Queen]);
                }
                else if ((temp = stmAttackers & bb.Pieces[Queen]) != 0)
                {
                    occ ^= SquareBB[lsb(temp)];
                    if ((swap = SEEValueQueen - swap) < res)
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
