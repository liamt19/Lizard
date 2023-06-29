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
        public static EvalByColor EvalEndgame(in Position p, int pToMove)
        {
            EvalByColor endgameScore = new EvalByColor();

            Bitboard bb = p.bb;
            ulong white = bb.Colors[Color.White];
            ulong black = bb.Colors[Color.Black];
            ulong all = white | black;

            int wMat = bb.MaterialCount(Color.White);
            int bMat = bb.MaterialCount(Color.Black);

            endgameScore.white += (wMat * 1.5);
            endgameScore.black += (bMat * 1.5);

            int strong = (wMat > bMat) ? Color.White : Color.Black;
            int weak = Not(strong);
            int whiteKing = bb.KingIndex(Color.White);
            int blackKing = bb.KingIndex(Color.Black);
            int kingDist = SquareDistances[whiteKing][blackKing];

            bool strongHasRookQueen = ((bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]) & bb.Colors[strong]) != 0;

            if (strong == Color.White)
            {
                endgameScore.white += ScoreEGKingDistance[kingDist];

                if (strongHasRookQueen)
                {
                    int ourBestSlider = (lsb(bb.Pieces[Piece.Queen] & bb.Colors[strong]));
                    if (ourBestSlider == LSBEmpty)
                    {
                        ourBestSlider = (lsb(bb.Pieces[Piece.Rook] & bb.Colors[strong]));
                    }

                    int sliderDist = SquareDistances[ourBestSlider][blackKing];
                    endgameScore.white += ScoreEGSliderDistance[sliderDist];
                }
            }
            else
            {
                endgameScore.white += (PSQT.EGWeakKingPosition[blackKing] * CoefficientPSQTEKG);
                endgameScore.black += ScoreEGKingDistance[kingDist];

                if (strongHasRookQueen)
                {
                    int ourBestSlider = (lsb(bb.Pieces[Piece.Queen] & bb.Colors[strong]));
                    if (ourBestSlider == LSBEmpty)
                    {
                        ourBestSlider = (lsb(bb.Pieces[Piece.Rook] & bb.Colors[strong]));
                    }

                    int sliderDist = SquareDistances[ourBestSlider][whiteKing];
                    endgameScore.white += ScoreEGSliderDistance[sliderDist];
                }
            }

            return endgameScore;
        }

    }
}
