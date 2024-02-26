namespace Lizard.Logic.Search
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
    }
}
