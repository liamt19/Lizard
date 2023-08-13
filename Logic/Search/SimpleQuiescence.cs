
#define SHOW_STATS


using LTChess.Logic.Data;

using static LTChess.Logic.Search.EvaluationConstants;
using static LTChess.Logic.Search.Ordering.MoveOrdering;

namespace LTChess.Logic.Search
{
    public static class SimpleQuiescence
    {

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

            bool isPV = typeof(NodeType) == typeof(PVNode);
            if (isPV && ply > info.SelectiveDepth)
            {
                info.SelectiveDepth = ply;
            }

            int standingPat = EvaluationTable.ProbeOrEval(ref info);

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
            
            Span<int> scores = stackalloc int[size];
            AssignQuiescenceMoveScores(ref info.Position.bb, list, scores, size);

            for (int i = 0; i < size; i++)
            {
                info.NodeCount++;
#if DEBUG || SHOW_STATS
                SearchStatistics.QuiescenceNodes++;
#endif

                OrderNextMove(list, scores, size, i);

                if (UseAggressiveQPruning)
                {
                    if (scores[i] < -(ValueRook - ValueKnight))
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.AggressiveQPruningCuts++;
                        SearchStatistics.AggressiveQPruningTotalCuts += (ulong)(size - i);
#endif
                        break;
                    }
                }

                if (UseDeltaPruning)
                {
                    int theirPieceVal = GetPieceValue(info.Position.bb.PieceTypes[list[i].To]);

                    if (standingPat + theirPieceVal + DeltaPruningMargin < alpha)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QuiescenceFutilityPrunes++;
                        SearchStatistics.QuiescenceFutilityPrunesTotal += (ulong)(size - i);
#endif
                        break;
                    }
                }

                if (UseQuiescenceSEE)
                {
                    int see = SEE(ref info.Position.bb, ref list[i]);
                    //int see = scores[i];
                    if (standingPat + see > beta)
                    {
#if DEBUG || SHOW_STATS
                        SearchStatistics.QuiescenceSEECuts++;
                        SearchStatistics.QuiescenceSEETotalCuts += (ulong)(size - i);
#endif
                        return standingPat + see;
                    }
                }


                info.Position.MakeMove(list[i]);

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
            int theirValue = (move.EnPassant ? ValuePawn : GetPieceValue(bb.GetPieceAtIndex(move.To)));

            if (move.Promotion)
            {
                return theirValue - (GetPieceValue(move.PromotionTo) - ValuePawn);
            }

            int ourValue = GetPieceValue(bb.GetPieceAtIndex(move.From));

            return theirValue - ourValue;
        }
    }
}
