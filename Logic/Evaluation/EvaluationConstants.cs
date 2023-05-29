using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class EvaluationConstants
    {
        public const int ValuePawn = 100;
        public const int ValueKnight = 290;
        public const int ValueBishop = 310;
        public const int ValueRook = 450;
        public const int ValueQueen = 1000;

        public const double ScaleMaterial = 1.0;
        public const double ScalePawns = 0.4;
        public const double ScaleKnights = 0.6;
        public const double ScaleBishops = 0.7;
        public const double ScaleRooks = 0.7;
        public const double ScaleQueens = 0.8;
        public const double ScaleKingSafety = 0.15;
        public const double ScaleThreats = 0.4;
        public const double ScaleSpace = 0.2;

        public const int ScorePawnSupport = 2;
        public const int ScoreIsolatedPawn = -3;
        public const int ScorePasser = 5;
        public const int ScorePromotingPawn = ValueQueen - ValuePawn;

        public const int ScoreKingRingAttack = 25;
        public const int ScoreKingOutterRingAttack = 10;

        public const int ScorePerSquare = 10;

        public const int ScoreBishopOutpost = 15;
        public const int ScoreBishopPair = 40;

        public const int ScoreRookOpenFile = 30;
        public const int ScoreRookSemiOpenFile = 15;

        public const double CoefficientPawnAttack = 1.6;
        public const double CoefficientHanging = 1.7;
        public const double CoefficientUnderdefended = 1.4;
        public const double CoefficientPositiveTrade = 1.2;
        public const double CoefficientPinnedQueen = 0.5;

        public const double CoefficientPSQTCenter = 0.6;
        public const double CoefficientPSQTPawns = 1;
        public const double CoefficientPSQTKnights = 1;
    }
}
