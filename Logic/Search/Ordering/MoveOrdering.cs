﻿
using Lizard.Logic.Search.History;

namespace Lizard.Logic.Search.Ordering
{
    public static unsafe class MoveOrdering
    {

        /// <summary>
        /// Gives each of the <paramref name="size"/> pseudo-legal moves in the <paramref name = "list"/> scores.
        /// </summary>
        /// <param name="ss">The entry containing Killer moves to prioritize</param>
        /// <param name="history">A reference to a <see cref="HistoryTable"/> with MainHistory/CaptureHistory scores.</param>
        /// <param name="ttMove">The <see cref="Move"/> retrieved from the TT probe, or Move.Null if the probe missed (ss->ttHit == false). </param>
        public static void AssignScores(Position pos, SearchStackEntry* ss, in HistoryTable history,
                ScoredMove* list, int size, Move ttMove)
        {
            ref Bitboard bb = ref pos.bb;
            int pc = pos.ToMove;

            var pawnThreats = pos.ThreatsBy(Not(pc), Pawn);
            var minorThreats = pos.ThreatsBy(Not(pc), Knight) | pos.ThreatsBy(Not(pc), Bishop) | pawnThreats;
            var rookThreats = pos.ThreatsBy(Not(pc), Rook) | minorThreats;

            for (int i = 0; i < size; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;
                int pt = bb.GetPieceAtIndex(moveFrom);

                if (m.Equals(ttMove))
                {
                    sm.Score = int.MaxValue - 1_000_000;
                }
                else if (m == ss->KillerMove)
                {
                    sm.Score = int.MaxValue - 10_000_000;
                }
                else if (bb.GetPieceAtIndex(moveTo) != None && !m.IsCastle)
                {
                    int capturedPiece = bb.GetPieceAtIndex(moveTo);
                    sm.Score = (OrderingVictimMult * GetPieceValue(capturedPiece)) + 
                               (history.CaptureHistory[pc, pt, moveTo, capturedPiece]);
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pc, pt, moveTo);

                    sm.Score  = 2 * history.MainHistory[pc, m];
                    sm.Score += 2 * (*(ss - 1)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 2)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 4)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 6)->ContinuationHistory)[contIdx];

                    if (ss->Ply < PlyHistoryTable.MaxPlies)
                    {
                        sm.Score += ((2 * PlyHistoryTable.MaxPlies + 1) * history.PlyHistory[ss->Ply, m]) / (2 * ss->Ply + 1);
                    }

                    if (pos.GivesCheck(pt, moveTo))
                    {
                        sm.Score += OrderingCheckBonus;
                    }

                    int threat = 0;
                    var fromBB = SquareBB[moveFrom];
                    var toBB = SquareBB[moveTo];
                    if (pt == Queen)
                    {
                        threat += ((fromBB & rookThreats) != 0) ? 12288 : 0;
                        threat -= ((toBB   & rookThreats) != 0) ? 11264 : 0;
                    }
                    else if (pt == Rook)
                    {
                        threat += ((fromBB & minorThreats) != 0) ? 10240 : 0;
                        threat -= ((toBB   & minorThreats) != 0) ?  9216 : 0;
                    }
                    else if (pt == Bishop || pt == Knight)
                    {
                        threat += ((fromBB & pawnThreats)!= 0) ? 8192 : 0;
                        threat -= ((toBB   & pawnThreats)!= 0) ? 7168 : 0;
                    }

                    list[i].Score += threat;
                }

                if (pt == Knight)
                {
                    sm.Score += 200;
                }
            }
        }

        /// <summary>
        /// Gives each of the <paramref name="size"/> pseudo-legal moves in the <paramref name = "list"/> scores, 
        /// ignoring any killer moves placed in the <paramref name="ss"/> entry.
        /// </summary>
        /// <param name="history">A reference to a <see cref="HistoryTable"/> with MainHistory/CaptureHistory scores.</param>
        /// <param name="ttMove">The <see cref="Move"/> retrieved from the TT probe, or Move.Null if the probe missed (ss->ttHit == false). </param>
        public static void AssignQuiescenceScores(Position pos, SearchStackEntry* ss, in HistoryTable history,
                ScoredMove* list, int size, Move ttMove)
        {
            ref Bitboard bb = ref pos.bb;
            int pc = pos.ToMove;

            for (int i = 0; i < size; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;
                int pt = bb.GetPieceAtIndex(moveFrom);

                if (m.Equals(ttMove))
                {
                    sm.Score = int.MaxValue - 1_000_000;
                }
                else if (bb.GetPieceAtIndex(moveTo) != None && !m.IsCastle)
                {
                    int capturedPiece = bb.GetPieceAtIndex(moveTo);
                    sm.Score = (OrderingVictimMult * GetPieceValue(capturedPiece)) + 
                               (history.CaptureHistory[pc, pt, moveTo, capturedPiece]);
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pc, pt, moveTo);

                    sm.Score  = 2 * history.MainHistory[pc, m];
                    sm.Score += 2 * (*(ss - 1)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 2)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 4)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 6)->ContinuationHistory)[contIdx];

                    if (pos.GivesCheck(pt, moveTo))
                    {
                        sm.Score += OrderingCheckBonus;
                    }
                }

                if (pt == Knight)
                {
                    sm.Score += 200;
                }
            }
        }

        /// <summary>
        /// Assigns scores to each of the <paramref name="size"/> moves in the <paramref name="list"/>.
        /// <br></br>
        /// This is only called for ProbCut, so the only moves in <paramref name="list"/> should be generated 
        /// using <see cref="GenNoisy"/>, which only generates captures and promotions.
        /// </summary>
        public static void AssignProbCutScores(ref Bitboard bb, ScoredMove* list, int size)
        {
            for (int i = 0; i < size; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;

                sm.Score = m.IsEnPassant ? Pawn : bb.GetPieceAtIndex(m.To);
                if (m.IsPromotion)
                {
                    //  Gives promotions a higher score than captures.
                    //  We can assume a queen promotion is better than most captures.
                    sm.Score += 10;
                }
            }
        }


        /// <summary>
        /// Passes over the list of <paramref name="moves"/>, bringing the move with the highest <see cref="ScoredMove.Score"/>
        /// within the range of <paramref name="listIndex"/> and <paramref name="size"/> to the front and returning it.
        /// </summary>
        public static Move OrderNextMove(ScoredMove* moves, int size, int listIndex)
        {
            int max = int.MinValue;
            int maxIndex = listIndex;

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

    }


}
