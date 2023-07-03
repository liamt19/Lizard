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

        [MethodImpl(Inline)]
        public static double[] EvalEndgame(in Position p, int pc)
        {
            double[] score = new double[2];
            Bitboard bb = p.bb;

            int wMat = bb.MaterialCount(Color.White);
            int bMat = bb.MaterialCount(Color.Black);

            int ourKing = bb.KingIndex(pc);
            int theirKing = bb.KingIndex(Not(pc));

            int strong = (wMat > bMat) ? Color.White : Color.Black;
            int weak = Not(strong);

            int strongKing = (strong == pc) ? ourKing : theirKing;
            int weakKing = (strong == pc) ? theirKing : ourKing;

            int kingDist = SquareDistances[strongKing][weakKing];

            bool strongHasRookQueen = ((bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]) & bb.Colors[strong]) != 0;

            //  Small bonus for material
            score[Color.White] += wMat;
            score[Color.Black] += bMat;

            //  Very large bonus for the strong side having their king close the the weak side's king
            score[strong] += ScoreEGKingDistance[kingDist];

            //  If we have a rook or queen, we want it to be close-ish to their king
            if (strongHasRookQueen)
            {
                int ourBestSlider = (lsb(bb.Pieces[Piece.Queen] & bb.Colors[strong]));
                if (ourBestSlider == LSBEmpty)
                {
                    ourBestSlider = (lsb(bb.Pieces[Piece.Rook] & bb.Colors[strong]));
                }

                int sliderDist = SquareDistances[ourBestSlider][weakKing];

                //  Medium bonus for having that rook/queen close to their king.
                score[strong] += ScoreEGSliderDistance[sliderDist];
            }


            score[Color.White] *= ScaleEndgame;
            score[Color.Black] *= ScaleEndgame;

            return score;
        }

    }
}
