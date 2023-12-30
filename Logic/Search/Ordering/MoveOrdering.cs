


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


        /// <summary>
        /// Assigns scores to each of the <paramref name="size"/> moves in the <paramref name="list"/>.
        /// <br></br>
        /// This is only called for ProbCut, so the only moves in <paramref name="list"/> should be generated 
        /// using <see cref="GenLoud"/>, which only generates captures and promotions.
        /// </summary>
        public static void AssignProbCutScores(ref Bitboard bb, ScoredMove* list, int size)
        {
            for (int i = 0; i < size; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;

                sm.Score = EvaluationConstants.SEEValues[m.EnPassant ? Pawn : bb.GetPieceAtIndex(m.To)];
                if (m.Promotion)
                {
                    //  Gives promotions a higher score than captures.
                    //  We can assume a queen promotion is better than most captures.
                    sm.Score += EvaluationConstants.SEEValues[Queen] + 1;
                }
            }
        }


        /// <summary>
        /// Passes over the list of <paramref name="moves"/>, bringing the move with the highest <see cref="ScoredMove.Score"/>
        /// within the range of <paramref name="listIndex"/> and <paramref name="size"/> to the front and returning it.
        /// </summary>
        public static Move OrderNextMove(ScoredMove* moves, int size, int listIndex)
        {
            if (size < 2)
            {
                return moves[listIndex].Move;
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

            return moves[listIndex].Move;
        }


        public static void AssignScores(ref Bitboard bb, SearchStackEntry* ss, in HistoryTable history, in PieceToHistory*[] continuationHistory,
                Span<ScoredMove> list, int size, CondensedMove ttMove, bool doKillers = true)
        {
            fixed (ScoredMove* ptr = list)
            {
                AssignScores(ref bb, ss, history, continuationHistory, ptr, size, ttMove, doKillers);
            }
        }

        /// <inheritdoc cref="OrderNextMove"/>
        public static Move OrderNextMove(Span<ScoredMove> moves, int size, int listIndex)
        {
            fixed (ScoredMove* ptr = moves)
            {
                return OrderNextMove(ptr, size, listIndex);
            }
        }

    }


}
