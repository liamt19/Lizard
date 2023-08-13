
#define SHOW_STATS

namespace LTChess.Logic.Search.Ordering
{
    public static class MoveOrdering
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


        /// <summary>
        /// Assigns each move in the list a Score based on things like whether it is a capture, causes check, etc.
        /// This is important for iterative deepening since we generally have a good idea of which moves are good/bad
        /// based on the results from the previous depth, and don't necessarily want to spend time looking at 
        /// a "bad" move's entire search tree again when we already have a couple moves that look promising.
        /// </summary>
        /// <param name="ply">The current ply of the search, used to determine what the killer moves are for that ply</param>
        /// <param name="pvOrTTMove">This is set to the TTEntry.BestMove from the previous depth, or possibly Move.Null</param>
        [MethodImpl(Inline)]
        public static void AssignNormalMoveScores(ref SearchInformation info, in Span<Move> list, in Span<int> scores, int size, int ply, Move pvOrTTMove)
        {
            int theirKing = info.Position.bb.KingIndex(Not(info.Position.ToMove));
            int pt;

            for (int i = 0; i < size; i++)
            {
                if (list[i].Equals(pvOrTTMove))
                {
                    scores[i] = int.MaxValue - 10;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_PV_TT_Move++;
#endif
                }
                else if (list[i].Promotion)
                {
                    scores[i] = int.MaxValue - 100 + list[i].PromotionTo;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Promotion++;
#endif
                }
                else if (list[i].Capture)
                {
                    int victim = info.Position.bb.GetPieceAtIndex(list[i].To);
                    int aggressor = info.Position.bb.GetPieceAtIndex(list[i].To);
                    scores[i] = MvvLva[victim][aggressor] * 10000;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_MvvLva++;
#endif
                }
                else if (UseKillerHeuristic && list[i].Equals(SimpleSearch.KillerMoves[ply, 0]))
                {
                    scores[i] = 100000;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Killer_1++;
#endif          
                }
                else if (UseKillerHeuristic && list[i].Equals(SimpleSearch.KillerMoves[ply, 1]))
                {
                    scores[i] = 90000;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Killer_2++;
#endif
                }

                int pc = info.Position.ToMove;
                pt = info.Position.bb.GetPieceAtIndex(list[i].From);

                if (UseHistoryHeuristic)
                {
                    int history = SimpleSearch.HistoryMoves[pc, pt, list[i].To];
                    scores[i] = history;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_HistoryHeuristic++;
#endif
                }
            }

        }



        /// <summary>
        /// Orders the <paramref name="list"/> of moves based on the value of the piece being captured. 
        /// All moves that capture queens will be sorted before moves that capture rooks, etc.
        /// <br></br>
        /// The values in <paramref name="scores"/> are the difference in piece value between the piece that is moving
        /// and the piece that is being captured (i.e. a bishop capturing a queen == 1000 - 330).
        /// </summary>
        /// <param name="list">Move list to be sorted in descending order of capture value</param>
        /// <param name="scores">Set to the difference in material for each move</param>
        /// <param name="size">The number of captures in the move list</param>
        [MethodImpl(Inline)]
        public static void SortByCaptureValueFast(ref Bitboard bb, in Span<Move> list, in Span<int> scores, int size)
        {
            for (int i = 0; i < size; i++)
            {
                scores[i] = GetPieceValue(bb.PieceTypes[list[i].To]) - GetPieceValue(bb.PieceTypes[list[i].From]);
                if (list[i].EnPassant)
                {
                    scores[i] = EvaluationConstants.ValuePawn;
                }
                else if (list[i].Promotion)
                {
                    scores[i] += GetPieceValue(list[i].PromotionTo);
                }
            }

            int max;
            for (int i = 0; i < size - 1; i++)
            {
                max = i;
                for (int j = i + 1; j < size; j++)
                {
                    //  TODO: EnPassant not considered here
                    if (GetPieceValue(bb.PieceTypes[list[j].To]) > GetPieceValue(bb.PieceTypes[list[max].To]))
                    {
                        max = j;
                    }
                }

                (list[max], list[i]) = (list[i], list[max]);
                (scores[max], scores[i]) = (scores[i], scores[max]);
            }
        }



        [MethodImpl(Inline)]
        public static void AssignQuiescenceMoveScores(ref Bitboard bb, in Span<Move> list, in Span<int> scores, int size)
        {
            for (int i = 0; i < size; i++)
            {
                //scores[i] = SimpleQuiescence.SEESimple(ref bb, ref list[i]);

                //scores[i] = GetPieceValue(bb.PieceTypes[list[i].To]) - GetPieceValue(bb.PieceTypes[list[i].From]);
                //if (list[i].EnPassant)
                //{
                //    scores[i] = EvaluationConstants.ValuePawn;
                //}
                //else if (list[i].Promotion)
                //{
                //    scores[i] += GetPieceValue(list[i].PromotionTo);
                //}

                scores[i] = GetPieceValue(bb.PieceTypes[list[i].To]);
            }
        }

        [MethodImpl(Inline)]
        public static void OrderNextMove(in Span<Move> moves, in Span<int> scores, int size, int listIndex)
        {
            if (size < 2)
            {
                return;
            }

            int max = AlphaStart;
            int maxIndex = -1;

            for (int i = listIndex; i < size; i++)
            {
                if (scores[i] > max)
                {
                    max = scores[i];
                    maxIndex = i;
                }
            }

            (moves[maxIndex], moves[listIndex]) = (moves[listIndex], moves[maxIndex]);
            (scores[maxIndex], scores[listIndex]) = (scores[listIndex], scores[maxIndex]);
        }
    }


}
