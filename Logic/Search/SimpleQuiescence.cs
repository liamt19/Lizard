
//#define SHOW_STATS


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Search.EvaluationConstants;

namespace LTChess.Search
{
    public static class SimpleQuiescence
    {
        /// <summary>
        /// A table containing move scores given to captures based on the value of the attacking piece in comparison to the piece being captured.
        /// We want to look at PxQ before QxQ because it would win more material if their queen was actually defended.
        /// <br></br>
        /// For example, the value of a bishop capturing a queen would be <see cref="MvvLva"/>[<see cref="Piece.Queen"/>][<see cref="Piece.Bishop"/>], which is 6003
        /// </summary>
        public static readonly int[][] MvvLva =
        {
            //          pawn, knight, bishop, rook, queen, king
            new int[] { 1005, 1004, 1003, 1002, 1001, 1000 },
            new int[] { 2005, 2004, 2003, 2002, 2001, 2000 },
            new int[] { 3005, 3004, 3003, 3002, 3001, 3000 },
            new int[] { 4005, 4004, 4003, 4002, 4001, 4000 },
            new int[] { 5005, 5004, 5003, 5002, 5001, 5000 },
            new int[] { 6005, 6004, 6003, 6002, 6001, 6000 }
        };


        [MethodImpl(Inline)]
        public static int QSearch<NodeType>(ref SearchInformation info, int alpha, int beta, int depth, int ply) where NodeType : SearchNodeType
        {
            if (info.StopSearching)
            {
                return 0;
            }

#if DEBUG || SHOW_STATS
            SearchStatistics.QCalls++;
#endif


            int standingPat = EvaluationTable.ProbeOrEval(ref info);

            bool isPV = typeof(NodeType) == typeof(PVNode);
            if (isPV && ply > info.SelectiveDepth)
            {
                info.SelectiveDepth = ply;
            }

            if (!isPV)
            {
                TTEntry ttEntry = TranspositionTable.Probe(info.Position.Hash);
                if (ttEntry.NodeType != TTNodeType.Invalid && ttEntry.Validate(info.Position.Hash))
                {
                    if (ttEntry.NodeType == TTNodeType.Exact || ttEntry.NodeType == TTNodeType.Beta && ttEntry.Eval >= beta)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QuiescenceNodesTTHits++;
#endif
                        return ttEntry.Eval;
                    }
                }
            }


            if (standingPat >= beta)
            {
                return beta;
            }

            if (alpha < standingPat)
            {
                alpha = standingPat;
            }

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = info.Position.GenAllLegalMovesTogether(list, true);


            if (false && size == 0)
            {
                //  We are only generating captures, so if there aren't any captures left
                //  we will have to see if there are still legal moves to see if this is a draw/mate or not

                if (info.Position.GenAllLegalMovesTogether(list) == 0)
                {
                    //  We have no legal moves

                    if ((info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck))
                    {
                        //  If we have no legal moves and are in check, this is mate.
                        return info.MakeMateScore();
                    }
                    else
                    {
                        //  If we have no legal moves and are NOT in check, this is a stalemate.
                        return -ThreadedEvaluation.ScoreDraw;
                    }
                }
                else
                {
                    //  We do have legal moves left but they aren't captures, so stop doing quiescence and return alpha.
                    return alpha;
                }
            }

            int numCaps = SortByCaptureValueFast(ref info.Position.bb, list, size);
            for (int i = 0; i < numCaps; i++)
            {
                info.NodeCount++;

                if (UseDeltaPruning)
                {
                    int theirPieceVal = GetPieceValue(info.Position.bb.PieceTypes[list[i].to]);

                    if (standingPat + theirPieceVal + DeltaPruningMargin < alpha)
                    {
                        break;
                    }
                }

                if (UseQuiescenceSEE)
                {
                    int see = SEE(ref info.Position.bb, ref list[i]);
                    if (standingPat + see > beta)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QuiescenceSEECuts++;
                        SearchStatistics.QuiescenceSEETotalCuts += (ulong) (numCaps - i);
#endif
                        return standingPat + see;
                    }
                }


                info.Position.MakeMove(list[i]);

#if DEBUG || SHOW_STATS
                SearchStatistics.QuiescenceNodes++;
#endif

                info.NodeCount++;

                //  Keep making moves until there aren't any captures left.
                var score = -QSearch<NodeType>(ref info, -beta, -alpha, depth - 1, ply + 1);
                info.Position.UnmakeMove();

                if (score > alpha)
                {
                    alpha = score;
                }

                if (score >= beta)
                {
                    return beta;
                }
            }

#if DEBUG || SHOW_STATS
            SearchStatistics.QCompletes++;
#endif
            return alpha;
        }

        [MethodImpl(Inline)]
        public static int SortByCaptureValue(in Bitboard bb, in Span<Move> list, int size)
        {
            Span<int> scores = stackalloc int[size];
            int numCaps = 0;
            for (int i = 0; i < size; i++)
            {
                if (list[i].Capture)
                {
                    int theirPieceVal = GetPieceValue(bb.PieceTypes[list[i].to]);
                    scores[i] = theirPieceVal;
                    numCaps++;
                }
            }

            int max;
            for (int i = 0; i < size - 1; i++)
            {
                max = i;
                for (int j = i + 1; j < size; j++)
                {
                    if (scores[j] > scores[max])
                    {
                        max = j;
                    }
                }

                (list[max], list[i]) = (list[i], list[max]);

                (scores[max], scores[i]) = (scores[i], scores[max]);
            }

            return numCaps;
        }


        [MethodImpl(Inline)]
        public static int SortByCaptureValueFast(ref Bitboard bb, in Span<Move> list, int size)
        {
            int max;
            for (int i = 0; i < size - 1; i++)
            {
                max = i;
                for (int j = i + 1; j < size; j++)
                {
                    if (GetPieceValue(bb.PieceTypes[list[j].to]) > GetPieceValue(bb.PieceTypes[list[max].to]))
                    {
                        max = j;
                    }
                }

                (list[max], list[i]) = (list[i], list[max]);
            }

            return size;
        }




        [MethodImpl(Inline)]
        public static int StaticExchange(ref Bitboard bb, int square, int ToMove)
        {
#if DEBUG || SHOW_STATS
            SearchStatistics.Scores_SEE_calls++;
#endif
            //  https://www.chessprogramming.org/SEE_-_The_Swap_Algorithm

            int depth = 0;
            int[] gain = new int[32];
            gain[0] = GetPieceValue(bb.GetPieceAtIndex(square));

            ulong debug_wtemp = bb.Colors[Color.White];
            ulong debug_btemp = bb.Colors[Color.Black];
            int[] debug_pt = new int[64];
            Array.Copy(bb.PieceTypes, debug_pt, 64);

            while (true)
            {
                int ourPieceIndex = bb.LowestValueAttacker(square, Not(ToMove));
                if (ourPieceIndex == LSBEmpty)
                {
                    //Log("Depth " + depth + ", " + "ourPieceIndex == LSBEmpty breaking");
                    break;
                }

                depth++;
                int ourPieceType = bb.GetPieceAtIndex(ourPieceIndex);
                gain[depth] = (GetPieceValue(ourPieceType) - gain[depth - 1]);

                if (Math.Max(-gain[depth - 1], gain[depth]) < 0)
                {
                    //Log("Math.Max(-gain[depth - 1], gain[depth] was " + (Math.Max(-gain[depth - 1], gain[depth])) + " breaking");
                    break;
                }

                //Log("Depth " + depth + ", " + bb.SquareToString(ourPieceIndex) + " will capture " + bb.SquareToString(square));

                bb.Colors[ToMove] ^= (SquareBB[square] | SquareBB[ourPieceIndex]);
                bb.Colors[Not(ToMove)] ^= (SquareBB[square]);

                bb.PieceTypes[ourPieceIndex] = Piece.None;
                bb.PieceTypes[square] = ourPieceType;

                ToMove = Not(ToMove);
            }

            bb.Colors[Color.White] = debug_wtemp;
            bb.Colors[Color.Black] = debug_btemp;
            Array.Copy(debug_pt, bb.PieceTypes, 64);

            while (--depth > 0)
            {
                gain[depth - 1] = -Math.Max(-gain[depth - 1], gain[depth]);
            }

            return gain[0];
        }

        [MethodImpl(Inline)]
        public static int SEE(ref Bitboard bb, ref Move move)
        {
            int theirValue = (move.EnPassant ? ValuePawn : GetPieceValue(bb.GetPieceAtIndex(move.to)));
            
            if (move.Promotion)
            {
                return theirValue - (GetPieceValue(move.PromotionTo) - ValuePawn);
            }

            int ourValue = GetPieceValue(bb.GetPieceAtIndex(move.from));

            return theirValue - ourValue;
        }
    }
}
