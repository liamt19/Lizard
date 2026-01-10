
using Lizard.Logic.Data;
using Lizard.Logic.Search.History;
using Lizard.Logic.Threads;
using System.Collections.Generic;

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
        public static void AssignScores(Position pos, SearchStackEntry* ss, ScoredMove* list, int size, Move ttMove)
        {
            SearchThread thisThread = pos.Owner;
            ref Bitboard bb = ref pos.bb;
            var pc = pos.ToMove;
            var ply = ss->Ply;

            var pawnThreats = pos.ThreatsBy(Not(pc), Pawn);
            var minorThreats = pos.ThreatsBy(Not(pc), Knight) | pos.ThreatsBy(Not(pc), Bishop) | pawnThreats;
            var rookThreats = pos.ThreatsBy(Not(pc), Rook) | minorThreats;

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                var (moveFrom, moveTo) = m;

                int pt = bb.GetPieceAtIndex(moveFrom);
                int capturedPiece = bb.GetPieceAtIndex(moveTo);
                var piece = MakePiece(pc, pt);

                int score = 0;

                if (m.Equals(ttMove))
                {
                    score = int.MaxValue - 1_000_000;
                }
                else if (m == ss->KillerMove)
                {
                    score = int.MaxValue - 10_000_000;
                }
                else if (capturedPiece != None && !m.IsCastle)
                {
                    var hist = thisThread.GetNoisyHistory(piece, moveTo, capturedPiece);
                    score = hist + ((MVVMult * GetPieceValue(capturedPiece)) / 32);
                }
                else
                {
                    score += NMOrderingMH * thisThread.GetMainHistory(pc, m);
                    score += NMOrderingSS1 * thisThread.GetContinuationEntry(ply, 1, piece, moveTo);
                    score += NMOrderingSS2 * thisThread.GetContinuationEntry(ply, 2, piece, moveTo);
                    score += NMOrderingSS4 * thisThread.GetContinuationEntry(ply, 4, piece, moveTo);
                    score += NMOrderingSS6 * thisThread.GetContinuationEntry(ply, 6, piece, moveTo);
                    score /= 256;

                    score += thisThread.GetPlyHistory(ply, m);

                    if (pos.GivesCheck(pt, moveTo))
                    {
                        score += CheckBonus;
                    }

                    int threat = 0;
                    var fromBB = SquareBB[moveFrom];
                    var toBB = SquareBB[moveTo];
                    if (pt == Queen)
                    {
                        threat += ((fromBB & rookThreats) != 0) ? (24 * OrderingEnPriseMult) : 0;
                        threat -= ((toBB   & rookThreats) != 0) ? (22 * OrderingEnPriseMult) : 0;
                    }
                    else if (pt == Rook)
                    {
                        threat += ((fromBB & minorThreats) != 0) ? (20 * OrderingEnPriseMult) : 0;
                        threat -= ((toBB   & minorThreats) != 0) ? (18 * OrderingEnPriseMult) : 0;
                    }
                    else if (pt == Bishop || pt == Knight)
                    {
                        threat += ((fromBB & pawnThreats)!= 0) ? (16 * OrderingEnPriseMult) : 0;
                        threat -= ((toBB   & pawnThreats)!= 0) ? (14 * OrderingEnPriseMult) : 0;
                    }

                    score += threat;
                }

                if (pt == Knight)
                {
                    score += 200;
                }

                list[i].Score = score;
            }
        }

        /// <summary>
        /// Gives each of the <paramref name="size"/> pseudo-legal moves in the <paramref name = "list"/> scores, 
        /// ignoring any killer moves placed in the <paramref name="ss"/> entry.
        /// </summary>
        /// <param name="history">A reference to a <see cref="HistoryTable"/> with MainHistory/CaptureHistory scores.</param>
        /// <param name="ttMove">The <see cref="Move"/> retrieved from the TT probe, or Move.Null if the probe missed (ss->ttHit == false). </param>
        public static void AssignQuiescenceScores(Position pos, SearchStackEntry* ss, ScoredMove* list, int size, Move ttMove)
        {
            SearchThread thisThread = pos.Owner;
            ref Bitboard bb = ref pos.bb;
            var pc = pos.ToMove;
            var ply = ss->Ply;

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                var (moveFrom, moveTo) = m;

                int pt = bb.GetPieceAtIndex(moveFrom);
                int capturedPiece = bb.GetPieceAtIndex(moveTo);
                var piece = MakePiece(pc, pt);

                int score = 0;
                if (m == ttMove)
                {
                    score = int.MaxValue - 1_000_000;
                }
                else if (capturedPiece != None && !m.IsCastle)
                {
                    var hist = thisThread.GetNoisyHistory(piece, moveTo, capturedPiece);
                    score = hist + ((MVVMult * GetPieceValue(capturedPiece)) / 32);
                }
                else
                {
                    score += QSOrderingMH * thisThread.GetMainHistory(pc, m);
                    score += QSOrderingSS1 * thisThread.GetContinuationEntry(ply, 1, piece, moveTo);
                    score += QSOrderingSS2 * thisThread.GetContinuationEntry(ply, 2, piece, moveTo);
                    score += QSOrderingSS4 * thisThread.GetContinuationEntry(ply, 4, piece, moveTo);
                    score += QSOrderingSS6 * thisThread.GetContinuationEntry(ply, 6, piece, moveTo);
                    score /= 256;

                    if (pos.GivesCheck(pt, moveTo))
                    {
                        score += CheckBonus;
                    }
                }

                if (pt == Knight)
                {
                    score += 200;
                }

                list[i].Score = score;
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
