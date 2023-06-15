using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

#if DEBUG
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
#if DEBUG
                    SearchStatistics.ETHits++;
#endif
                }
                else
                {
#if DEBUG
                    SearchStatistics.ETWrongHashKey++;
#endif
                    //  This is the lower bound for the score
                    standingPat = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
                    EvaluationTable.Save(posHash, (short)standingPat);
                }
            }
            else
            {
                //  This is the lower bound for the score
                standingPat = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
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

            Span<Move> legal = stackalloc Move[NormalListCapacity];
            int size = info.Position.GenAllLegalMoves(legal);


            if (size == 0)
            {
                if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck)
                {
                    return -Evaluation.ScoreMate - ((info.MaxDepth / 2) + 1);

                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            for (int i = 0; i < size; i++)
            {

                if (!legal[i].Capture)
                {
                    continue;
                }

                if (info.Position.WouldCauseDraw(legal[i]))
                {
                    return -Evaluation.ScoreDraw;
                }

                if (UseDeltaPruning)
                {
                    int theirPieceVal = Evaluation.GetPieceValue(info.Position.bb.PieceTypes[legal[i].to]);

                    if (standingPat + theirPieceVal + DeltaPruningMargin < alpha)
                    {
                        //Log("Skipping " + legal[i].ToString(info.Position) + " because " + "standingPat: " + standingPat + " + theirPieceVal: " + theirPieceVal + " + Margin: " + DeltaPruningMargin + " < alpha: " + alpha);
                        continue;
                    }
                }


                info.Position.MakeMove(legal[i]);
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

    }
}
