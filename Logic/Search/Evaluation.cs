using System.Runtime.CompilerServices;

namespace Lizard.Logic.Search
{
    public static unsafe class Evaluation
    {
        [MethodImpl(Inline)]
        public static int MakeMateScore(int ply)
        {
            return -ScoreMate + ply;
        }

        public static bool IsScoreMate(int score)
        {
            return Math.Abs(Math.Abs(score) - ScoreMate) < MaxDepth;
        }

        [MethodImpl(Inline)]
        public static int GetPieceValue(int pt)
        {
            return pt switch
            {
                Pawn   => ValuePawn,
                Knight => ValueKnight,
                Bishop => ValueBishop,
                Rook   => ValueRook,
                Queen  => ValueQueen,
                _      => 0,
            };
        }

        [MethodImpl(Inline)]
        public static int GetSEEValue(int pt)
        {
            return pt switch
            {
                Pawn   => SEEValue_Pawn,
                Knight => SEEValue_Knight,
                Bishop => SEEValue_Bishop,
                Rook   => SEEValue_Rook,
                Queen  => SEEValue_Queen,
                _      => 0,
            };
        }
    }
}
