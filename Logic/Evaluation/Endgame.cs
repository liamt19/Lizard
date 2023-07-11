using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Magic.MagicBitboards;
using static LTChess.Search.EvaluationConstants;

namespace LTChess.Search
{
    public static class Endgame
    {
        /// <summary>
        /// Evaluates the endgame position <paramref name="p"/> and returns the scores for both players.
        /// <br></br>
        /// <paramref name="ToMove"/> is used to calculate the king's distance from enemy pawns.
        /// The side to move can use their turn to move their king closer to pawns, 
        /// so their king can be 1 square further from pawns and be given the same penalty.
        /// </summary>
        [MethodImpl(Inline)]
        public static double[] EvalEndgame(in Position p, int ToMove)
        {
            double[] score = new double[2];
            Bitboard bb = p.bb;

            int wMat = bb.MaterialCount(Color.White);
            int bMat = bb.MaterialCount(Color.Black);

            bool isKPVKP = (popcount(bb.Colors[Color.White] | bb.Colors[Color.Black]) == popcount(bb.Pieces[Piece.Pawn]) + 2);
            if (isKPVKP)
            {
                for (int pc = Color.White; pc <= Color.Black; pc++)
                {
                    ulong ourPawns = bb.Pieces[Piece.Pawn] & bb.Colors[pc];
                    ulong theirPawns = bb.Pieces[Piece.Pawn] & bb.Colors[Not(pc)];

                    int theirKing = bb.KingIndex(Not(pc));

                    ulong ourPawnsTemp = ourPawns;
                    while (ourPawnsTemp != 0)
                    {
                        int idx = lsb(ourPawnsTemp);

                        int fileDist = FileDistances[theirKing][idx] - (pc != ToMove ? 1 : 0);
                        int promotionDistance = DistanceFromPromotion(idx, pc);

                        if (fileDist > promotionDistance)
                        {
                            //  Their king is too far away to stop this pawn.

                            //  Penalize their king for being distant from this pawn
                            score[Not(pc)] += (ScoreEndgamePawnDistancePenalty * fileDist);

                            score[pc] += (ScorePromotingPawn / 3) * (6 - promotionDistance);
                        }

                        if (bb.IsPasser(idx))
                        {
                            //  Give passedd pawns a bonus based on how advanced they are
                            score[pc] += ScoreEGPawnPromotionDistance[promotionDistance];
                        }
                        else
                        {
                            //  Also see if the pawn is blocked, but is part of a group of pawns which together can make a passed pawn

                            ulong theirBlockingPawns = (theirPawns & PassedPawnMasks[pc][idx]);
                            if (pc == ToMove)
                            {
                                //  If it is this color's turn to move, the pawns that it can capture 
                                //  aren't considered blockers (since we can capture it or simply move past it)
                                theirBlockingPawns &= ~PawnAttackMasks[pc][idx];
                            }

                            ulong ourBehindPawns = (ourPawns & PassedPawnMasks[Not(pc)][idx]);

                            //  If it is unimpeded, or has more pawns supporting it than blocking it,
                            //  give us a bonus based on how close this pawn is to promoting.
                            if (theirBlockingPawns == 0 || (popcount(theirBlockingPawns) < popcount(ourBehindPawns)))
                            {
                                score[pc] += ScoreEGPawnPromotionDistance[promotionDistance];
                            }
                        }

                        ourPawnsTemp = poplsb(ourPawnsTemp);
                    }
                }

            }
            else
            {

                int ourKing = bb.KingIndex(ToMove);
                int theirKing = bb.KingIndex(Not(ToMove));

                int strong = (wMat > bMat) ? Color.White : Color.Black;

                if (wMat == bMat)
                {
                    strong = p.ToMove;
                }

                int weak = Not(strong);

                int strongKing = (strong == ToMove) ? ourKing : theirKing;
                int weakKing = (strong == ToMove) ? theirKing : ourKing;

                int kingDist = SquareDistances[strongKing][weakKing];

                bool strongHasRookQueen = ((bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]) & bb.Colors[strong]) != 0;

                //  If we have a rook or queen, we want it to be close-ish to their king
                if (strongHasRookQueen)
                {
                    //  Very large bonus for the strong side having their king close the the weak side's king
                    score[strong] += ScoreEGKingDistance[kingDist];

                    int ourBestSlider = (lsb(bb.Pieces[Piece.Queen] & bb.Colors[strong]));
                    if (ourBestSlider == LSBEmpty)
                    {
                        ourBestSlider = (lsb(bb.Pieces[Piece.Rook] & bb.Colors[strong]));
                    }

                    int sliderDist = SquareDistances[ourBestSlider][weakKing];

                    //  Medium bonus for having that rook/queen close to their king.
                    score[strong] += ScoreEGSliderDistance[sliderDist];
                }

            }

            score[Color.White] *= ScaleEndgame;
            score[Color.Black] *= ScaleEndgame;

            return score;
        }

    }
}
