namespace LTChess.Logic.Search
{
    public static unsafe class EvaluationConstants
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



        public const int ValuePawn = 210;
        public const int ValueKnight = 840;
        public const int ValueBishop = 920;
        public const int ValueRook = 1370;
        public const int ValueQueen = 2690;

        public static readonly int* PieceValues;
        public static readonly int* SEEValues;

        static EvaluationConstants()
        {
            var lazyPieceValues = new int[] { ValuePawn, ValueKnight, ValueBishop, ValueRook, ValueQueen };
            PieceValues = (int*)AlignedAllocZeroed(5 * sizeof(int), AllocAlignment);
            for (int i = 0; i < 5; i++)
            {
                PieceValues[i] = lazyPieceValues[i];
            }


            var lazySEEValues = new int[] { 126, 781, 825, 1276, 2538, 0, 0 };
            SEEValues = (int*)AlignedAllocZeroed((PieceNB + 1) * sizeof(int), AllocAlignment);
            for (int i = 0; i < PieceNB + 1; i++)
            {
                SEEValues[i] = lazySEEValues[i];
            }
        }
    }
}
