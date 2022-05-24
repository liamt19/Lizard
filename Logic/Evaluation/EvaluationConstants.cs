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
        public const int ValueKnight = 320;
        public const int ValueBishop = 360;
        public const int ValueRook = 620;
        public const int ValueQueen = 1400;

        public const int ScorePawnSupport = 13;
        public const int ScoreIsolatedPawn = -5;
        public const int ScorePasser = 20;
        public const int ScorePromotingPawn = ValueQueen - ValuePawn;

        public const int ScoreKingRingAttack = 60;
        public const int ScoreKingOutterRingAttack = 30;

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
