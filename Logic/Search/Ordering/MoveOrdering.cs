
#define SHOW_STATS

namespace LTChess.Logic.Search.Ordering
{
    public static unsafe class MoveOrdering
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
            new int[] { 10005, 10004, 10003, 10002, 10001, 10000 },
            new int[] { 20005, 20004, 20003, 20002, 20001, 20000 },
            new int[] { 30005, 30004, 30003, 30002, 30001, 30000 },
            new int[] { 40005, 40004, 40003, 40002, 40001, 40000 },
            new int[] { 50005, 50004, 50003, 50002, 50001, 50000 },
            new int[] { 60005, 60004, 60003, 60002, 60001, 60000 }
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
        public static unsafe void AssignNormalMoveScores(in Position pos, in HistoryTable history, in Span<Move> list, in Span<int> scores, SearchStackEntry* ss, int size, int ply, CondensedMove pvOrTTMove)
        {
            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                if (m.Equals(pvOrTTMove))
                {
                    scores[i] = int.MaxValue - 10;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_PV_TT_Move++;
#endif
                }
                else if (m.Promotion)
                {
                    scores[i] = int.MaxValue - 100 + m.PromotionTo;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Promotion++;
#endif
                }
                else if (m.Equals(ss->Killer0))
                {
                    scores[i] = int.MaxValue - 200;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Killer_1++;
#endif          
                }
                else if (m.Equals(ss->Killer1))
                {
                    scores[i] = int.MaxValue - 300;
#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_Killer_2++;
#endif
                }
                else if (m.Capture)
                {
                    int victim = pos.bb.GetPieceAtIndex(m.To);
                    int aggressor = pos.bb.GetPieceAtIndex(m.From);
                    //scores[i] = MvvLva[victim][aggressor];

                    scores[i] += history.CaptureHistory[HistoryTable.CapIndex(aggressor, pos.ToMove, m.To, victim)];

#if DEBUG || SHOW_STATS
                    SearchStatistics.Scores_MvvLva++;
#endif
                }
                else
                {
                    //scores[i] = 0;
                    scores[i] = history.MainHistory[(pos.ToMove * HistoryTable.MainHistoryPCStride) + m.MoveMask];
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
        public static void AssignQuiescenceMoveScores(in Bitboard bb, in Span<Move> list, in Span<int> scores, int size)
        {
            for (int i = 0; i < size; i++)
            {
                scores[i] = GetPieceValue(bb.PieceTypes[list[i].To]);
            }
        }

        [MethodImpl(Inline)]
        public static void AssignQuiescenceMoveScores(in Position pos, in HistoryTable history, in Span<Move> list, in Span<int> scores, int size)
        {
            ref Bitboard bb = ref pos.bb;

            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                int moveTo = m.To;
                int moveFrom = m.From;

                if (m.Capture)
                {
                    int capturedPiece = bb.GetPieceAtIndex(moveTo);
                    int capIdx = HistoryTable.CapIndex(bb.GetPieceAtIndex(moveFrom), bb.GetColorAtIndex(moveFrom), moveTo, capturedPiece);
                    scores[i] = (7 * GetPieceValue(capturedPiece)) + history.CaptureHistory[capIdx] / 16;
                }
                else
                {
                    scores[i] = history.MainHistory[((pos.ToMove * HistoryTable.MainHistoryPCStride) + m.MoveMask)];
                }
                
            }
        }

        [MethodImpl(Inline)]
        public static void OrderNextMove(in Span<Move> moves, in Span<int> scores, int size, int listIndex)
        {
            if (size < 2)
            {
                return;
            }

            int max = short.MinValue;
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
