using static Lizard.Logic.Search.Ordering.HistoryTable;

namespace Lizard.Logic.Search.History
{
    public readonly struct StatEntry(short v)
    {
        public readonly short Value = v;

        public static implicit operator short(StatEntry entry) => entry.Value;
        public static implicit operator StatEntry(short s) => new(s);
        public static StatEntry operator <<(StatEntry entry, int bonus) => (StatEntry)(entry + (bonus - (entry * Math.Abs(bonus) / NormalClamp)));
    }

    public readonly struct ContinuationEntry(short v)
    {
        private const int ContinuationMax = 16384;

        public readonly short Value = v;

        public static implicit operator short(ContinuationEntry entry) => entry.Value;
        public static implicit operator ContinuationEntry(short s) => new(s);
        public static ContinuationEntry operator <<(ContinuationEntry entry, int bonus) => (ContinuationEntry)(entry + (bonus - (entry * Math.Abs(bonus) / ContinuationMax)));
    }

    public readonly struct CorrectionEntry(short v)
    {
        public const int CorrectionClamp = 1024;
        public const int CorrectionBonusLimit = CorrectionClamp / 4;

        public readonly short Value = v;

        public static implicit operator short(CorrectionEntry entry) => entry.Value;
        public static implicit operator CorrectionEntry(short s) => new(s);
        public static CorrectionEntry operator <<(CorrectionEntry entry, int bonus) => (CorrectionEntry)(entry + (bonus - (entry * Math.Abs(bonus) / CorrectionClamp)));
    }
}
