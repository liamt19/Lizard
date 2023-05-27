using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static System.Formats.Asn1.AsnWriter;
using static LTChess.Search.SearchConstants;

namespace LTChess.Search
{
    public static class SimpleSearch
    {

        public static void StartSearching(ref SearchInformation info)
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
            int score = SimpleSearch.FindBest(ref info, alpha, beta, info.MaxDepth);
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

            if (depth <= 0)
            {
                return SimpleQuiescence.FindBest(ref info, alpha, beta, depth);
            }

            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            Move BestMove = Move.Null;
            int startingAlpha = alpha;

            int staticEval = 0;
            ETEntry etEntry = EvaluationTable.Probe(posHash);
            if (etEntry.key == EvaluationTable.InvalidKey || !etEntry.Validate(posHash))
            {
                staticEval = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
                EvaluationTable.Save(posHash, (short)staticEval);
            }
            else
            {
                staticEval = etEntry.score;
            }

            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = pos.GenAllLegalMoves(list);

            if (size == 0)
            {
                if (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck)
                {
                    return -Evaluation.ScoreMate - ((pos.Moves.Count + 1) / 2);
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            list.SortByCheck();

            // Eventually this will work

            for (int i = 0; i < size; i++)
            {
                pos.MakeMove(list[i]);
                int score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1);
                pos.UnmakeMove();

                if (score >= beta)
                {
#if DEBUG
                    //Log(depth + " beta cutoff on move " + list[i].ToString(pos) + ", score " + score + " >= beta " + beta);
#endif
                    return beta;
                }

                if (score > alpha)
                {
#if DEBUG
                    //Log(depth + " new best move " + list[i].ToString(pos) + ", score " + score + " > alpha " + alpha);
#endif

                    alpha = score;
                    BestMove = list[i];
                }
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
                TranspositionTable.Save(posHash, (short)alpha, nodeType, depth, BestMove);

                info.BestMove = BestMove;
                info.BestScore = alpha;
            }

#if DEBUG
            //Log(depth + " ret best move " + BestMove.ToString(pos) + " alpha " + alpha);
#endif
            return alpha;
        }


    }
}
