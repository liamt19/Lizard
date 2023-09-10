
#define SHOW_STATS


using LTChess.Logic.Core;
using System.ComponentModel;
using System.Net.NetworkInformation;

using LTChess.Logic.Data;

using static LTChess.Logic.Search.EvaluationConstants;
using static LTChess.Logic.Search.Ordering.MoveOrdering;

namespace LTChess.Logic.Search
{
    public static unsafe class SimpleQuiescence
    {
        private static readonly int[] SEE_VALUE = new int[] { 208, 781, 825, 1276, 2538, 30000, 0 };
        public const int DEPTH_QS_CHECKS = 0;
        public const int DEPTH_QS_NO_CHECKS = -1;

        [MethodImpl(Inline)]
        public static int QSearch<NodeType>(ref SearchInformation info, SearchStackEntry* ss, int alpha, int beta, int depth) where NodeType : SearchNodeType
        {

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

            if (info.StopSearching)
            {
                return 0;
            }

            if (info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial())
            {
                return -ScoreDraw;
            }

            if (ss->Ply >= MaxSearchStackPly - 1)
            {
                return ss->InCheck ? 0 : info.GetEvaluation(info.Position);
            }

            bool isPV = (typeof(NodeType) != typeof(NonPVNode));

#if DEBUG || SHOW_STATS
            SearchStatistics.QCalls++;
#endif

            if (isPV && ss->Ply > info.SelectiveDepth)
            {
                info.SelectiveDepth = ss->Ply;
            }

            Position pos = info.Position;
            ulong posHash = pos.Hash;
            Move bestMove = Move.Null;


            int score;

            short eval;
            short bestScore = (short) (-ScoreInfinite + ss->Ply);
            short futility;

            ss->InCheck = pos.Checked;
            ss->TTHit = TranspositionTable.Probe(posHash, out TTEntry* tte);
            int ttDepth = (ss->InCheck || depth >= DEPTH_QS_CHECKS ? DEPTH_QS_CHECKS : DEPTH_QS_NO_CHECKS);
            short ttScore = (ss->TTHit ? tte->Score : ScoreNone);
            //CondensedMove ttMove = (ss->TTHit ? tte->BestMove : CondensedMove.Null);
            bool ttPV = (isPV || (ss->TTHit && tte->PV));
            bool pvHit = (ss->TTHit && tte->PV);

            if (!isPV && 
                tte->Depth >= ttDepth && 
                ttScore != ScoreNone &&
                ((tte->Bound & (ttScore >= beta ? BoundLower : BoundUpper)) != 0))
            {
                return ttScore;
            }

            if (ss->InCheck)
            {
#if DEBUG || SHOW_STATS
                SearchStatistics.TT_InCheck_QS++;
#endif

                eval = futility = -ScoreInfinite;
            }
            else
            {
                if (ss->TTHit)
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.TTHits_QS++;
#endif

                    if ((ss->StaticEval = eval = tte->StatEval) == ScoreNone)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.TTHitNoScore_QS++;
#endif
                        ss->StaticEval = eval = info.GetEvaluation(pos);
                    }
#if DEBUG || SHOW_STATS
                    else
                    {

                        SearchStatistics.TTHitGoodScore_QS++;
                    }
#endif

                    if (ttScore != ScoreNone && ((tte->Bound & (ttScore > eval ? BoundLower : BoundUpper)) != 0))
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.TTScoreFit_QS++;
#endif

                        eval = ttScore;
                    }
                }
                else
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.TTMisses_QS++;
#endif
                    if ((ss - 1)->CurrentMove.IsNull())
                    {
                        ss->StaticEval = eval = (ss - 1)->StaticEval;
                    }
                    else
                    {
                        ss->StaticEval = eval = info.GetEvaluation(pos);
                    }
                }

                if (eval >= beta)
                {
                    if (!ss->TTHit)
                    {
                        //tte->Update(posHash, bestScore, TTNodeType.Alpha, TTEntry.DepthNone, Move.Null, ss->StaticEval, false);
                        tte->Update(posHash, ScoreNone, TTNodeType.Alpha, TTEntry.DepthNone, Move.Null, ss->StaticEval, false);
                    }

                    return eval;
                }

                if (eval > alpha)
                {
                    alpha = eval;
                }

                bestScore = eval;

                futility = (short) (Math.Min(ss->StaticEval, bestScore) + 200);
            }

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = pos.GenAllPseudoLegalMovesTogether(list);
            
            Span<int> scores = stackalloc int[size];
            //AssignQuiescenceMoveScores(bb, list, scores, size);
            AssignQuiescenceMoveScores(pos, SimpleSearch.History, list, scores, size);

            int legalMoves = 0;
            int captures = 0;

            for (int i = 0; i < size; i++)
            {
                OrderNextMove(list, scores, size, i);
                Move m = list[i];

                if (!pos.IsLegal(m)) {
                    continue;
                }

                legalMoves++;

                //  Captures and moves made while in check are always OK.
                //  Moves that give check are only OK if the depth is above the threshold.
                if (!(m.Capture || ss->InCheck || (m.Checks && depth > DEPTH_QS_NO_CHECKS)))
                {
                    continue;
                }

                captures++;
                info.NodeCount++;

#if DEBUG || SHOW_STATS
                SearchStatistics.QuiescenceNodes++;
#endif
                
                if (bestScore > ScoreTTLoss)
                {
                    if (ss->InCheck && !(m.Capture || m.Promotion))
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QCheckedBreaks++;
#endif
                        break;
                    }

                    if (!ss->InCheck && futility <= alpha && !SEE_Swap(pos, m, 1))
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QSwaps_1++;
#endif

                        bestScore = Math.Max(bestScore, futility);
                        continue;
                    }

                    if (!SEE_Swap(pos, m, 0))
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QSwaps_0++;
#endif
                        continue;
                    }
                }


                prefetch(Unsafe.AsPointer(ref TranspositionTable.GetCluster(pos.HashAfter(m))));
                ss->CurrentMove = m;

                pos.MakeMove(m);

                //  Keep making moves until we hit a beta cut.
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
#if DEBUG || SHOW_STATS
                            SearchStatistics.QBetaCuts++;
#endif
                            break;
                        }
                    }
                }
            }

            if (legalMoves == 0 && ss->InCheck)
            {
                //return info.MakeMateScore();
                return -ScoreMate + ss->Ply;
            }

            TTNodeType nodeType = (bestScore >= beta) ? TTNodeType.Alpha : TTNodeType.Beta;
            tte->Update(posHash, bestScore, nodeType, ttDepth, bestMove, ss->StaticEval, ttPV);

#if DEBUG || SHOW_STATS
            SearchStatistics.QCompletes++;
#endif


            return bestScore;
        }


        [MethodImpl(Inline)]
        public static int SEE(ref Bitboard bb, ref Move move)
        {
            int theirValue = (move.EnPassant ? ValuePawn : GetPieceValue(bb.GetPieceAtIndex(move.To)));

            if (move.Promotion)
            {
                return theirValue - (GetPieceValue(move.PromotionTo) - ValuePawn);
            }

            int ourValue = GetPieceValue(bb.GetPieceAtIndex(move.From));

            return theirValue - ourValue;
        }
        
        /// <summary>
        /// https://github.com/jhonnold/berserk/blob/25bd00c443dc26b7a15ba93da867198fbfa84c11/src/see.c#L31C57-L31C57
        /// </summary>
        [MethodImpl(Inline)]
        public static bool SEE_Swap(Position pos, in Move m, int threshold = 1)
        {
            if (m.Castle || m.EnPassant || m.Promotion)
            {
                return true;
            }

            Bitboard bb = pos.bb;

            int from = m.From;
            int to = m.To;

            int v = SEE_VALUE[bb.PieceTypes[to]] - threshold;
            if (v < 0)
                return false;

            v -= SEE_VALUE[bb.PieceTypes[from]];
            if (v >= 0)
                return true;

            ulong occ = (bb.Occupancy ^ SquareBB[from]) | SquareBB[to];
            ulong attackers = bb.AttackersToFast(to, occ);
            
            ulong diag = bb.Pieces[Bishop] | bb.Pieces[Queen];
            ulong straight = bb.Pieces[Rook] | bb.Pieces[Queen];
            int stm = pos.ToMove;
            while (true)
            {
                attackers &= occ;

                ulong mine = attackers & bb.Colors[stm];
                if (mine == 0)
                    break;

                int piece = Pawn;
                for (piece = Pawn; piece < King; piece++)
                    if ((mine & (bb.Pieces[piece] & bb.Colors[stm])) != 0)
                        break;
                stm ^= 1;

                if ((v = -v - 1 - SEE_VALUE[piece]) >= 0)
                {
                    if (piece == King && (attackers & bb.Colors[stm]) != 0)
                        stm ^= 1;

                    break;
                }

                occ ^= SquareBB[lsb(mine & (bb.Pieces[piece] & bb.Colors[stm ^ 1]))];

                if (piece == Pawn || piece == Bishop || piece == Queen)
                    attackers |= GetBishopMoves(occ, to) & diag;
                if (piece == Rook || piece == Queen)
                    attackers |= GetRookMoves(occ, to) & straight;
            }

            return stm != pos.ToMove;
        }
    }
}
