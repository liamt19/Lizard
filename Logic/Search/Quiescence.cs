#define USETABLE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace LTChess.Search
{
    public static class Quiescence
    {



        [MethodImpl(Inline)]
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int maxDepth = 4)
        {
            //  https://www.chessprogramming.org/Quiescence_Search

            if (maxDepth <= -2)
            {
                //  This isn't giveaway chess, and the evaluation is probably wrong anyways
                return beta;
            }

            int standingPat;

#if DEBUG
            SearchStatistics.QuiescenceNodes++;
#endif

            ulong posHash = info.Position.Hash;
            ETEntry stored = EvaluationTable.Probe(posHash);
            if (stored.key != 0UL)
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
                return standingPat;
            }

            if (standingPat > alpha)
            {
                alpha = standingPat;
            }

            Span<Move> legal = stackalloc Move[NORMAL_CAPACITY];
            int size = GenAllLegalMoves(info.Position, legal);


            if (size == 0)
            {
                if (info.Position.CheckInfo.InCheck || info.Position.CheckInfo.InDoubleCheck)
                {
                    //Log("Quiescence - No legal moves after " + info.Position.Moves.Peek());
                    return -Evaluation.ScoreMate - ((info.Position.Moves.Count + 1) / 2);

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

                info.Position.MakeMove(legal[i]);
                //  Keep making moves until there aren't any captures left.
                var score = -Quiescence.FindBest(ref info, -beta, -alpha, maxDepth - 1);
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
