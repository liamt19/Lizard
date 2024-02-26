using Lizard.Logic.NN;

namespace Lizard.Logic.Search
{
    public static unsafe class Evaluation
    {
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

        [MethodImpl(Inline)]
        public static int GetSEEValue(int pt)
        {
            switch (pt)
            {
                case Pawn:
                    return SEEValue_Pawn;
                case Knight:
                    return SEEValue_Knight;
                case Bishop:
                    return SEEValue_Bishop;
                case Rook:
                    return SEEValue_Rook;
                case Queen:
                    return SEEValue_Queen;
                default:
                    return 0;
            }
        }
    }
}
