
namespace Lizard.Logic.Datagen
{
    public static class DatagenParameters
    {
        public const int HashSize = 8;

        public const int MinOpeningPly = 8;
        public const int MaxOpeningPly = 9;

        public const int SoftNodeLimit = 5000;
        public const int HardNodeLimit = 100000;
        public const int DepthLimit = 10;

        public const int WritableDataLimit = 512;

        public const int AdjudicateMoves = 2;
        public const int AdjudicateScore = 4000;
        public const int MaxFilteringScore = 5000;

        public const int MaxOpeningScore = 1200;
        public const int MaxScrambledOpeningScore = 600;
    }
}
