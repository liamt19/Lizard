
namespace Lizard.Logic.Datagen
{
    public static class DatagenParameters
    {
        public const int HashSize = 8;

        public const int MinOpeningPly = 8;
        public const int MaxOpeningPly = 9;

        public const int SoftNodeLimit = 5000;
        public const int DepthLimit = 14;

        public const int WritableDataLimit = 512;

        public const int WinAdjudicateMoves = 3;
        public const int WinAdjudicateScore = 3000;

        public const int DrawAdjudicateMoves = 4;
        public const int DrawAdjudicateScore = 8;

        public const int MaxFilteringScore = 6000;

        public const int MaxOpeningScore = 600;
        public const int MaxScrambledOpeningScore = 600;
    }
}
