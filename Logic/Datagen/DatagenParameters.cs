
namespace Lizard.Logic.Datagen
{
    public static class DatagenParameters
    {
        public const int HashSize = 8;

        public const int MinOpeningPly = 8;
        public const int MaxOpeningPly = 9;

        public const int SoftNodeLimit = 10000;
        public const int HardNodeLimit = SoftNodeLimit * 20;
        public const int DepthLimit = 24;

        public const int WritableDataLimit = 512;

        public const int AdjudicateMoves = 4;
        public const int AdjudicateScore = 3000;
        public const int MaxFilteringScore = 6000;

        public const int MaxOpeningScore = 1200;
        public const int MaxScrambledOpeningScore = 600;
    }
}
