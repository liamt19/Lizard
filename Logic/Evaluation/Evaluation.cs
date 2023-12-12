using System.Linq.Expressions;

using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.HalfKP;
using LTChess.Logic.NN.Simple768;

using static LTChess.Logic.Search.EvaluationConstants;

namespace LTChess.Logic.Search
{
    public unsafe static class Evaluation
    {

        public const short ScoreNone = 32760;
        public const int ScoreInfinite = 31200;
        public const int ScoreMate = 30000;
        public const int ScoreDraw = 0;

        public const int ScoreTTWin = ScoreMate - 512;
        public const int ScoreTTLoss = -ScoreTTWin;

        public const int ScoreMateMax = ScoreMate - 256;
        public const int ScoreMatedMax = -ScoreMateMax;

        public const int ScoreAssuredWin = 20000;
        public const int ScoreWin = 10000;



        /// <summary>
        /// Returns the evaluation of the position relative to <paramref name="pc"/>, which is the side to move.
        /// </summary>
        [MethodImpl(Inline)]
        public static short GetEvaluation(in Position position)
        {
            if (UseHalfKA)
            {
                return (short)HalfKA_HM.GetEvaluation(position, FavorPositionalEval);
            }
            
            if (UseHalfKP)
            {
                return (short)HalfKP.GetEvaluation(position);
            }

            return (short)Simple768.GetEvaluation(position);
        }



        [MethodImpl(Inline)]
        public static int MakeMateScore(int ply)
        {
            return -ScoreMate + ply;
        }

        [MethodImpl(Inline)]
        public static bool IsScoreMate(int score)
        {
            return (Math.Abs(Math.Abs(score) - ScoreMate) < MaxDepth);
        }

        [MethodImpl(Inline)]
        public static int GetPieceValue(int pt)
        {
            switch (pt)
            {
                case Piece.Pawn:
                    return ValuePawn;
                case Piece.Knight:
                    return ValueKnight;
                case Piece.Bishop:
                    return ValueBishop;
                case Piece.Rook:
                    return ValueRook;
                case Piece.Queen:
                    return ValueQueen;
                default:
                    break;
            }

            return 0;
        }
    }
}
