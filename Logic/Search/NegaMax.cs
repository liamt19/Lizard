﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static LTChess.Search.SearchConstants;

namespace LTChess.Search
{
    public static class NegaMax
    {

        public static void IterativeDeepen(ref SearchInformation info)
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            
            int depth = 1;
            int maxDepth = info.MaxDepth;
            double maxTime = info.MaxSeachTime;
            bool continueDeepening = true;

            while (continueDeepening)
            {
                info.MaxDepth = depth;
                Deepen(ref info);
                if (continueDeepening && Evaluation.IsScoreMate(info.BestScore, out _))
                {
                    totalTime.Stop();
                    Log("Forced mate found (" + info.BestMove + "#), aborting at depth " + depth + " after " + totalTime.Elapsed.TotalSeconds + " seconds");
                    return;
                }
                if (info.StopSearching)
                {
                    totalTime.Stop();
                    Log("Received StopSearching command, aborting at depth " + depth + " after " + totalTime.Elapsed.TotalSeconds + " seconds");
                    return;
                }

                depth++;

                continueDeepening = (depth <= maxDepth && totalTime.Elapsed.TotalMilliseconds <= maxTime);
            }

            totalTime.Stop();
        }

        public static void Deepen(ref SearchInformation info)
        {
            int alpha = AlphaStart;
            int beta = BetaStart;
            Stopwatch sw = Stopwatch.StartNew();
            int score = NegaMax.FindBest(ref info, alpha, beta, info.MaxDepth);
            sw.Stop();
            info.SearchTime = sw.Elapsed.TotalMilliseconds;
            info.BestScore = score;
            info.OnSearchDone?.Invoke();
        }

        [MethodImpl(Inline)]
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int depth)
        {
            if (info.NodeCount >= info.MaxNodes)
            {
                info.StopSearching = true;
                return 0;
            }

            info.NodeCount++;

#if DEBUG
            SearchStatistics.NegamaxNodes++;
#endif

            if (depth <= 0)
            {
                return Quiescence.FindBest(ref info, alpha, beta);
            }

            
            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            Move BestMove = Move.Null;
            int startingAlpha = alpha;
            int BestScore = 0;
            bool isPV = (beta - alpha < 1);

            int staticEval = 0;
            ETEntry etEntry = EvaluationTable.Probe(posHash);
            if (etEntry.key == EvaluationTable.InvalidKey|| !etEntry.Validate(posHash))
            {
                staticEval = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
                EvaluationTable.Save(posHash, (short)staticEval);
            }
            else
            {
                staticEval = etEntry.score;
            }

            if (ttEntry.NodeType != NodeType.Invalid && ttEntry.Key == TTEntry.MakeKey(posHash) && !ttEntry.BestMove.IsNull())
            {
                //  Make sure it's legal since collisions are common
                //  If we found a node for this position
                if (ttEntry.NodeType != NodeType.Alpha && bb.IsPseudoLegal(ttEntry.BestMove) && IsLegal(pos, bb, ttEntry.BestMove, bb.KingIndex(Not(pos.ToMove)), bb.KingIndex(pos.ToMove)))
                {
                    //  Exclude alpha nodes because they are lower bounds and probably not "the best"
                    info.BestMove = ttEntry.BestMove;
                }

                if (ttEntry.Depth >= depth)
                {
                    //  If we have already searched this node at an equal or higher depth, we can update alpha and beta accordingly
                    if (ttEntry.NodeType == NodeType.Alpha)
                    {
                        if (ttEntry.Eval < beta)
                        {
                            //  This node is known to be worse than beta
                            beta = ttEntry.Eval;
                        }
                    }
                    else if (ttEntry.NodeType == NodeType.Beta)
                    {
                        if (ttEntry.Eval > alpha)
                        {
                            //  This nose is known to be better than alpha
                            alpha = ttEntry.Eval;
                        }
                    }
                    else if (ttEntry.NodeType == NodeType.Exact)
                    {
                        if (!isPV || Evaluation.IsScoreMate(ttEntry.Eval, out _))
                        {
#if DEBUG
                            SearchStatistics.NegamaxTTExactHits++;
#endif
                            return ttEntry.Eval;
                        }
                    }

                    //  Beta cutoff
                    if (alpha >= beta)
                    {
#if DEBUG
                        SearchStatistics.BetaCutoffs++;
#endif
                        return ttEntry.Eval;
                    }
                }
            }

            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = info.Position.GenAllLegalMoves(list);

            if (size == 0)
            {
                if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck)
                {
                    return -Evaluation.ScoreMate - ((info.Position.Moves.Count + 1) / 2);
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            //list.SortByCheck();

            bool moveDuringCheck = (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck);
            bool isFirst = true;

            // Eventually this will work

            for (int i = 0; i < size; i++)
            {
                info.Position.MakeMove(list[i]);
                int score;
                if (isFirst)
                {
                    BestScore = -NegaMax.FindBest(ref info, -beta, -alpha, depth - 1);
                    isFirst = false;
                }
                else
                {
                    /**
                    
                    if (CanFutilityPrune(list[i], moveDuringCheck, depth, staticEval, alpha, beta))
                    {
                        int futility = GetFutilityScore(depth);
                    }

                     */

                    int lmr = 0;
                    if (CanApplyLMR(bb, list[i], moveDuringCheck, depth))
                    {
#if DEBUG
                        SearchStatistics.LMRReductions++;
#endif
                        lmr += LMRReductionAmount;
                    }
                    score = -NegaMax.FindBest(ref info, -beta, -alpha, depth - lmr - 1);

                    if (score > BestScore)
                    {
                        BestScore = score;
                    }
                }

                info.Position.UnmakeMove();

                

                if (BestScore > alpha)
                {
                    alpha = BestScore;
                    BestMove = list[i];

                    if (alpha >= beta)
                    {
#if DEBUG
                        SearchStatistics.BetaCutoffs++;
#endif
                        break;
                    }
                }

                /**
                if (score >= beta)
                {
                    if (depth == info.MaxDepth)
                    {
                        info.BestMove = list[i];
                    }
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                    if (depth == info.MaxDepth)
                    {
                        info.BestMove = list[i];
                    }
                }
                 */
            }

            bool setTT = (ttEntry.Depth <= depth && (ttEntry.NodeType == NodeType.Invalid || alpha != startingAlpha));
            if (setTT)
            {
                NodeType nodeType;
                if (alpha < startingAlpha)
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
                TranspositionTable.Save(posHash, (short)BestScore, nodeType, depth, BestMove);

                info.BestMove = BestMove;
                info.BestScore = BestScore;
            }

            return BestScore;
        }

        private static void MakeMoveScores(in Bitboard bb, in Move Best, in Span<Move> moves, in Span<ushort> scores, int numMoves, bool BestIsPV)
        {
            //  https://www.chessprogramming.org/Move_Ordering#Typical_move_ordering
            for (int i = 0; i < numMoves; i++)
            {
                if (moves[i].EnPassant)
                {
                    scores[i] = MoveScores.EnPassant;
                }
                else if (moves[i].Equals(Best))
                {
                    scores[i] = BestIsPV ? MoveScores.PVMove : MoveScores.TTHit;
                }
                else if (moves[i].Capture)
                {
                    //  TODO: use count defenders and stuff here
                    scores[i] = MoveScores.WinningCapture;
                }
                else if (moves[i].CausesCheck)
                {
                    scores[i] = MoveScores.Check;
                }
                else if (moves[i].Castle)
                {
                    scores[i] = MoveScores.Castle;
                }
                else
                {
                    scores[i] = MoveScores.Normal;
                }
            }
        }

        [MethodImpl(Inline)]
        private static bool CanFutilityPrune(in Move m, bool isToMoveInCheck, int depth, int eval, int alpha, int beta)
        {
            if (depth <= FutilityPruningDepth && isToMoveInCheck == false && m.CausesCheck == false && m.CausesDoubleCheck == false && m.Capture == false)
            {
                if (eval < beta && !Evaluation.IsScoreMate(eval, out _) && !Evaluation.IsScoreMate(alpha, out _) && !Evaluation.IsScoreMate(beta, out _))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static int GetFutilityScore(int depth)
        {
            return (FutilityPruningScore * depth);
        }

        [MethodImpl(Inline)]
        private static bool CanApplyLMR(in Bitboard bb, in Move m, bool moveDuringCheck, int depth)
        {
            //  https://www.chessprogramming.org/Late_Move_Reductions
            if (moveDuringCheck == false && depth >= LMRDepth && m.CausesCheck == false && m.CausesDoubleCheck == false && m.Promotion == false && m.Capture == false)
            {
                //  Don't reduce passed pawn moves
                if ((bb.Pieces[Piece.Pawn] & SquareBB[m.from]) != 0)
                {
                    return !Evaluation.IsPasser(bb, m.to);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// This is from https://github.com/Tearth/Cosette/blob/master/Cosette/Engine/Ai/Search/IterativeDeepening.cs .
        /// Tried using this since what I tried doing worked but was just redoing the search at lower depths.
        /// </summary>
        public static int GetPV(SearchInformation info, in Move[] moves, int numMoves)
        {
            Position position = info.Position;
            Bitboard bb = position.bb;

            int theirKing = bb.KingIndex(Not(position.ToMove));
            int ourKing = bb.KingIndex(position.ToMove);

            TTEntry stored = TranspositionTable.Probe(position.Hash);
            if (stored.NodeType == NodeType.Exact && stored.Key == TTEntry.MakeKey(position.Hash) && numMoves < MAX_DEPTH)
            {
                if (!bb.IsPseudoLegal(stored.BestMove) || !IsLegal(position, bb, stored.BestMove, ourKing, theirKing))
                {
                    return numMoves;
                }

                moves[numMoves] = stored.BestMove;
                position.MakeMove(stored.BestMove);

                //  Not sure what the point of this check is, I think Cosette handles checks differently.
                /*
                
                if (bb.AttackersTo(theirKing, Not(position.ToMove)) != 0)
                {
                    position.UnmakeMove();
                    return numMoves;
                }

                 */


                numMoves = GetPV(info, moves, numMoves + 1);
                position.UnmakeMove();
            }

            return numMoves;
        }

    }
}
