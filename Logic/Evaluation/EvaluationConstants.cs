namespace LTChess.Logic.Search
{
    public static class EvaluationConstants
    {
        public const int ValuePawn = 210;
        public const int ValueKnight = 840;
        public const int ValueBishop = 920;
        public const int ValueRook = 1370;
        public const int ValueQueen = 2690;

        public static readonly int[] PieceValues = { ValuePawn, ValueKnight, ValueBishop, ValueRook, ValueQueen };

    }
}
