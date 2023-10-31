


using LTChess.Logic.Data;
namespace LTChess.Logic.Search.Ordering
{
    public static unsafe class MoveOrdering
    {

        /// <summary>
        /// Gives each of the <paramref name="size"/> pseudo-legal moves in the <paramref name = "list"/> scores.
        /// </summary>
        /// <param name="ss">The entry containing Killer moves to prioritize</param>
        /// <param name="history">A reference to a <see cref="HistoryTable"/> with MainHistory/CaptureHistory scores.</param>
        /// <param name="continuationHistory">
        /// An array of 6 pointers to <see cref="SearchStackEntry.ContinuationHistory"/>. This should be [ (ss - 1), (ss - 2), null, (ss - 4), null, (ss - 6) ].
        /// </param>
        /// <param name="ttMove">The <see cref="CondensedMove"/> retrieved from the TT probe, or Move.Null if the probe missed (ss->ttHit == false). </param>
        public static void AssignScores(ref Bitboard bb, SearchStackEntry* ss, in HistoryTable history, in PieceToHistory*[] continuationHistory,
                ScoredMove* list, int size, CondensedMove ttMove, bool doKillers = true)
        {
            int pc = bb.GetColorAtIndex(list[0].Move.From);

            for (int i = 0; i < size; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                if (m.Equals(ttMove))
                {
                    sm.Score = int.MaxValue - 100000;
                }
                else if (doKillers && m == ss->Killer0)
                {
                    sm.Score = int.MaxValue - 1000000;
                }
                else if (doKillers && m == ss->Killer1)
                {
                    sm.Score = int.MaxValue - 2000000;
                }
                else if (m.Capture)
                {
                    int capturedPiece = bb.GetPieceAtIndex(moveTo);
                    int capIdx = HistoryTable.CapIndex(pc, bb.GetPieceAtIndex(moveFrom), moveTo, capturedPiece);
                    sm.Score = (7 * GetPieceValue(capturedPiece)) + history.CaptureHistory[capIdx] / 16;
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pc, bb.GetPieceAtIndex(moveFrom), moveTo);

                    sm.Score =  2 * (history.MainHistory[HistoryTable.HistoryIndex(pc, m)]);
                    sm.Score += 2 * (*continuationHistory[0])[contIdx];
                    sm.Score +=     (*continuationHistory[1])[contIdx];
                    sm.Score +=     (*continuationHistory[3])[contIdx];
                    sm.Score +=     (*continuationHistory[5])[contIdx];

                    if (m.Checks)
                    {
                        sm.Score += 10000;
                    }
                }
            }
        }


        public static void OrderNextMove(ScoredMove* moves, int size, int listIndex)
        {
            if (size < 2)
            {
                return;
            }

            int max = int.MinValue;
            int maxIndex = -1;

            for (int i = listIndex; i < size; i++)
            {
                if (moves[i].Score > max)
                {
                    max = moves[i].Score;
                    maxIndex = i;
                }
            }

            (moves[maxIndex], moves[listIndex]) = (moves[listIndex], moves[maxIndex]);
        }



        public static void AssignScores(ref Bitboard bb, SearchStackEntry* ss, in HistoryTable history, in PieceToHistory*[] continuationHistory,
                Span<ScoredMove> list, int size, CondensedMove ttMove, bool doKillers = true)
        {
            fixed (ScoredMove* ptr = list)
            {
                AssignScores(ref bb, ss, history, continuationHistory, ptr, size, ttMove, doKillers);
            }
        }


        public static void OrderNextMove(Span<ScoredMove> moves, int size, int listIndex)
        {
            fixed (ScoredMove* ptr = moves)
            {
                OrderNextMove(ptr, size, listIndex);
            }
        }



        /// <summary>
        /// A table containing move scores given to captures based on the value of the attacking piece in comparison to the piece being captured.
        /// We want to look at PxQ before QxQ because it would win more material if their queen was actually defended.
        /// <br></br>
        /// For example, the value of a bishop capturing a queen would be <see cref="MvvLva"/>[<see cref="Piece.Queen"/>][<see cref="Piece.Bishop"/>], which is 6003
        /// </summary>
        private static readonly int[][] MvvLva =
        {
            //          pawn, knight, bishop, rook, queen, king
            new int[] { 10005, 10004, 10003, 10002, 10001, 10000 },
            new int[] { 20005, 20004, 20003, 20002, 20001, 20000 },
            new int[] { 30005, 30004, 30003, 30002, 30001, 30000 },
            new int[] { 40005, 40004, 40003, 40002, 40001, 40000 },
            new int[] { 50005, 50004, 50003, 50002, 50001, 50000 },
            new int[] { 60005, 60004, 60003, 60002, 60001, 60000 }
        };

    }


}
