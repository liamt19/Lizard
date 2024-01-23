using Lizard.Logic.NN;
using Lizard.Logic.NN.HalfKA_HM;

namespace Lizard.Logic.Search
{
    public static unsafe class Evaluation
    {

        /// <summary>
        /// Returns the evaluation of the position relative to <paramref name="pc"/>, which is the side to move.
        /// </summary>
        [MethodImpl(Inline)]
        public static short GetEvaluation(in Position position)
        {
            return (short)HalfKA_HM.GetEvaluation(position, true);
            //return (short)Simple768.GetEvaluation(position);
        }



        [MethodImpl(Inline)]
        public static int MakeMateScore(int ply)
        {
            return -ScoreMate + ply;
        }

        [MethodImpl(Inline)]
        public static bool IsScoreMate(int score)
        {
            return Math.Abs(Math.Abs(score) - ScoreMate) < MaxDepth;
        }

        [MethodImpl(Inline)]
        public static int GetPieceValue(int pt)
        {
            switch (pt)
            {
                case Pawn:
                    return ValuePawn;
                case Knight:
                    return ValueKnight;
                case Bishop:
                    return ValueBishop;
                case Rook:
                    return ValueRook;
                case Queen:
                    return ValueQueen;
                default:
                    break;
            }

            return 0;
        }
    }
}
