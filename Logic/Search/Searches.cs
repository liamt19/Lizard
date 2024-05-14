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

            if (ss->InCheck)
            {
                //  If we are in check, don't bother getting a static evaluation or pruning.
                ss->StaticEval = eval = ScoreNone;
                goto MovesLoop;
            }

            if (doSkip)
            {
                eval = ss->StaticEval;
            }
            else if (ss->TTHit)
            {
                eval = ss->StaticEval = tte->StatEval != ScoreNone ? tte->StatEval : NNUE.GetEvaluation(pos);

                //  If the ttScore isn't invalid, use that score instead of the static eval.
                if (ttScore != ScoreNone && (tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0)
                {
                    eval = ttScore;
                }
            }
            else
            {
                eval = ss->StaticEval = NNUE.GetEvaluation(pos);
            }

            //  For TT hits, we can accept and return the TT score if:
            //  We aren't in a PV node,
            //  we aren't in a singular extension search,
            //  the TT hit's depth is above the current depth,
            //  the ttScore isn't invalid,
            //  the ttScore is below alpha (or it is just above alpha and we expected this node to fail high),
            //  and the tt entry's bound fits the criteria.
            if (!isPV
                && tte->Depth >= depth
                && ttScore != ScoreNone
                && (ttScore < alpha)
                && (tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0)
            {
                return ttScore;
            }

            MovesLoop:

            int legalMoves = 0;     //  Number of legal moves that have been encountered so far in the loop.
            int playedMoves = 0;    //  Number of moves that have been MakeMove'd so far.

            bool didSkip = false;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(pos, ss, history, list, size, ttMove);

            for (int i = 0; i < size; i++)
            {
                Move m = OrderNextMove(list, size, i);

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

                int moveFrom = m.From;
                int moveTo = m.To;
                int theirPiece = bb.GetPieceAtIndex(moveTo);
                int ourPiece = bb.GetPieceAtIndex(moveFrom);
                bool isCapture = (theirPiece != None && !m.Castle);

                legalMoves++;

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));

                ss->CurrentMove = m;
                thisThread.Nodes++;

                pos.MakeMove(m);

                playedMoves++;
                ulong prevNodes = thisThread.Nodes;

                if (isPV)
                    System.Runtime.InteropServices.NativeMemory.Clear((ss + 1)->PV, (nuint)(MaxPly * sizeof(Move)));

                if (!isPV || legalMoves > 1)
                {
                    score = -Negamax<NonPVNode>(ref info, ss + 1, -alpha - 1, -alpha, depth - 1, !cutNode);
                }

                if (isPV && (playedMoves == 1 || score > alpha))
                {
                    //  Do a new PV search here.
                    (ss + 1)->PV[0] = Move.Null;
                    score = -Negamax<PVNode>(ref info, ss + 1, -beta, -alpha, depth - 1, false);
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
                            //  This is a beta cutoff: Don't bother searching other moves because the current one is already too good.
                            break;
                        }

                        alpha = score;
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
        public static int QSearch<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {
            bool isPV = typeof(NodeType) != typeof(NonPVNode);

            Position pos = info.Position;
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
            short ttScore = ss->TTHit ? MakeNormalScore(tte->Score, ss->Ply) : ScoreNone;
            Move ttMove = ss->TTHit ? tte->BestMove : Move.Null;

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
                && tte->Depth >= 0
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

                    return eval;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                }

                bestScore = eval;

                futility = (short)(Math.Min(ss->StaticEval, bestScore) + FutilityExchangeBase);
            }

            int legalMoves = 0;
            int movesMade = 0;

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenPseudoLegal(list);
            AssignScores(pos, ss, history, list, size, ttMove);

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
                int theirPiece = bb.GetPieceAtIndex(moveTo);
                int ourPiece = bb.GetPieceAtIndex(moveFrom);
                bool isCapture = (theirPiece != None && !m.Castle);
                bool givesCheck = ((pos.State->CheckSquares[ourPiece] & SquareBB[moveTo]) != 0);

                if (!(isCapture || ss->InCheck))
                {
                    continue;
                }

                movesMade++;

                prefetch(TranspositionTable.GetCluster(pos.HashAfter(m)));
                ss->CurrentMove = m;
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
                      ((bestScore > startingAlpha) ? TTNodeType.Exact : 
                                                     TTNodeType.Beta);

            tte->Update(pos.Hash, MakeTTScore((short)bestScore, ss->Ply), bound, depth, bestMove, ss->StaticEval, ss->TTPV);

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

            if (capturedPiece != None && !bestMove.Castle)
            {
                history.CaptureHistory[thisColor, thisPiece, moveTo, capturedPiece] <<= quietMoveBonus;
            }
            else
            {

                int bestMoveBonus = (bestScore > beta + HistoryCaptureBonusMargin) ? quietMoveBonus : StatBonus(depth);

                if (ss->Killer0 != bestMove && !bestMove.EnPassant)
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
                    UpdateContinuations(ss, thisColor, bb.GetPieceAtIndex(m.From), m.To, -quietMovePenalty);
                }
            }

            for (int i = 0; i < captureCount; i++)
            {
                Move m = captureMoves[i];
                history.CaptureHistory[thisColor, bb.GetPieceAtIndex(m.From), m.To, bb.GetPieceAtIndex(m.To)] <<= -quietMoveBonus;
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

            int swap = GetSEEValue(bb.PieceTypes[to]) - threshold;
            if (swap < 0)
                return false;

            swap = GetSEEValue(bb.PieceTypes[from]) - swap;
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
