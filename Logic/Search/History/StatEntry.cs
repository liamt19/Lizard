
using static Lizard.Logic.Search.History.HistoryTable;

namespace Lizard.Logic.Search.History
{
    public readonly struct StatEntry(short v)
    {
        public readonly short Value = v;

        public static implicit operator short(StatEntry entry) => entry.Value;
        public static implicit operator StatEntry(short s) => new(s);
        public static StatEntry operator <<(StatEntry entry, int bonus) => (StatEntry)(entry + (bonus - (entry * Math.Abs(bonus) / NormalClamp)));
    }
}
