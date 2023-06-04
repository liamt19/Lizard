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
        public const int ValueKnight = 310;
        public const int ValueBishop = 330;
        public const int ValueRook = 510;
        public const int ValueQueen = 1000;

        public static readonly double[] ScaleMaterial = { 1.0, 2.0 };
        public static readonly double[] ScalePawns = { 1.0, 1.0 };
        public static readonly double[] ScaleKnights = { 1.0, 1.0 };
        public static readonly double[] ScaleBishops = { 1.0, 1.0 };
        public static readonly double[] ScaleRooks = { 1.0, 1.0 };
        public static readonly double[] ScaleQueens = { 1.0, 1.2 };
        public static readonly double[] ScaleKingSafety = { 1.0, 1.0 };
        public static readonly double[] ScaleThreats = { 0.5, 0.5 };
        public static readonly double[] ScaleSpace = { 1.0, 1.0 };

        public const double ScorePawnDoubled = -30;
        public const double ScorePawnSupport = 15;
        public const double ScoreIsolatedPawn = -10;
        public const double ScorePasser = 10;
        public const double ScorePromotingPawn = ValueQueen - ValuePawn;

        public const double ScoreKingRingAttack = 10;
        public const double ScoreKingOutterRingAttack = 2;

        public const double ScorePerSquare = 10;
        public const double ScoreUndevelopedPiece = -20;

        public const double ScoreBishopOutpost = 15;
        public const double ScoreBishopPair = 40;

        public const double ScoreRookOpenFile = 30;
        public const double ScoreRookSemiOpenFile = 15;

        public const double CoefficientPawnAttack = 1.3;
        public const double CoefficientHanging = 1.6;
        public const double CoefficientUnderdefended = 1.4;
        public const double CoefficientPositiveTrade = 1.2;
        public const double CoefficientPinnedQueen = 0.4;

        public const double CoefficientPSQTCenter = 0.7;
        public const double CoefficientPSQTPawns = 0.4;
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
