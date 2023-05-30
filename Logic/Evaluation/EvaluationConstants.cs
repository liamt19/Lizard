using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public static class EvaluationConstants
    {
        public const int EndgameNoEval = short.MinValue - 1;
        public const int GamePhaseNormal = 0;
        public const int GamePhaseEndgame = 1;


        public const int ValuePawn = 100;
        public const int ValueKnight = 290;
        public const int ValueBishop = 310;
        public const int ValueRook = 450;
        public const int ValueQueen = 1000;

        public static readonly double[] ScaleMaterial = { 1.0, 2.0 };
        public static readonly double[] ScalePawns = { 0.4, 0.75 };
        public static readonly double[] ScaleKnights = { 0.6, 0.6 };
        public static readonly double[] ScaleBishops = { 0.7, 0.7 };
        public static readonly double[] ScaleRooks = { 0.7, 0.7 };
        public static readonly double[] ScaleQueens = { 0.8, 0.8 };
        public static readonly double[] ScaleKingSafety = { 0.15, 0.3 };
        public static readonly double[] ScaleThreats = { 0.4, 0.4 };
        public static readonly double[] ScaleSpace = { 0.2, 0.2 };

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


        public static readonly double ScaleEndgame = 1.0;

        public const double CoefficientPSQTEKG = 2.0;

        /// <summary>
        /// Score given to the strong side for their king being however many squares away from the weak side's.
        /// We generally want to be 1 square away, since many endgames (KQvK, KRvK) require our king to be close
        /// to take away squares.
        /// </summary>
        public static readonly int[] ScoreEGKingDistance = { 0, 140, 80, 40, 20, 10, 0, -20 };
    }
}
