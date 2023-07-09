
#define SHOW_STATS


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
        [MethodImpl(Inline)]
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int curDepth)
        {
            if (info.StopSearching)
            {
                return 0;
            }

            info.NodeCount++;

            int standingPat;

#if DEBUG || SHOW_STATS
            SearchStatistics.QuiescenceNodes++;
#endif

            ulong posHash = info.Position.Hash;
            ETEntry stored = EvaluationTable.Probe(posHash);
            if (stored.key != EvaluationTable.InvalidKey)
            {
                if (stored.Validate(posHash))
                {
                    //  Use stored evaluation
                    standingPat = stored.score;
#if DEBUG || SHOW_STATS
                    SearchStatistics.ETHits++;
#endif
                }
                else
                {
#if DEBUG || SHOW_STATS
                    SearchStatistics.ETWrongHashKey++;
#endif
                    //  This is the lower bound for the score
                    standingPat = info.GetEvaluation(info.Position, info.Position.ToMove);
                    EvaluationTable.Save(posHash, (short)standingPat);
                }
            }
            else
            {
                //  This is the lower bound for the score
                standingPat = info.GetEvaluation(info.Position, info.Position.ToMove);
                EvaluationTable.Save(posHash, (short)standingPat);
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


            if (size == 0)
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
                if (UseDeltaPruning)
                {
                    int theirPieceVal = ThreadedEvaluation.GetPieceValue(info.Position.bb.PieceTypes[list[i].to]);

                    if (standingPat + theirPieceVal + DeltaPruningMargin < alpha)
                    {
                        break;
                    }
                }


                info.Position.MakeMove(list[i]);

                if (info.Position.IsThreefoldRepetition() || info.Position.IsInsufficientMaterial())
                {
                    info.Position.UnmakeMove();
                    return -ThreadedEvaluation.ScoreDraw;
                }

                //  Keep making moves until there aren't any captures left.
                var score = -SimpleQuiescence.FindBest(ref info, -beta, -alpha, curDepth - 1);
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
                    int theirPieceVal = ThreadedEvaluation.GetPieceValue(bb.PieceTypes[list[i].to]);
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

                Move tempMove = list[i];
                list[i] = list[max];
                list[max] = tempMove;

                int tempScore = scores[i];
                scores[i] = scores[max];
                scores[max] = tempScore;
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
                    if (ThreadedEvaluation.GetPieceValue(bb.PieceTypes[list[j].to]) > ThreadedEvaluation.GetPieceValue(bb.PieceTypes[list[max].to]))
                    {
                        max = j;
                    }
                }

                Move tempMove = list[i];
                list[i] = list[max];
                list[max] = tempMove;
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
            gain[0] = ThreadedEvaluation.GetPieceValue(bb.GetPieceAtIndex(square));

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
                gain[depth] = (ThreadedEvaluation.GetPieceValue(ourPieceType) - gain[depth - 1]);

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
    }
}
