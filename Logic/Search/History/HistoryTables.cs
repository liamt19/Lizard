
using static Lizard.Logic.Search.History.CaptureHistoryTable;
using static Lizard.Logic.Search.History.MainHistoryTable;

namespace Lizard.Logic.Search.History
{

    public unsafe class MainHistoryTable: IHistoryTable<MHEntry>
    {
        private const int MainHistoryClamp = 16384;

        public MainHistoryTable() : base(tables: 2, size: 64 * 64) { }

        public ref MHEntry this[int pc, Move m] => ref _History[HistoryIndex(pc, m)];
        private static int HistoryIndex(int pc, Move m) => (pc * 4096) + m.MoveMask;

        public readonly struct MHEntry(short v)
        {
            public readonly short Value = v;
            public static implicit operator short(MHEntry entry) => entry.Value;
            public static implicit operator MHEntry(short s) => new(s);
            public static MHEntry operator <<(MHEntry entry, int bonus) => (MHEntry)(entry + (bonus - (entry * Math.Abs(bonus) / MainHistoryClamp)));
        }
    }


    public unsafe class CaptureHistoryTable : IHistoryTable<CHEntry>
    {
        private const int CaptureHistoryClamp = 16384;

        public CaptureHistoryTable() : base(tables: 2, size: 6 * 64 * 6) { }

        public ref CHEntry this[int pc, int pt, int toSquare, int capturedPt] => ref _History[HistoryIndex(pc, pt, toSquare, capturedPt)];
        private static int HistoryIndex(int pc, int pt, int toSquare, int capturedPt) => ((capturedPt * 768) + (toSquare * 12) + (pt + (pc * 6)));

        public readonly struct CHEntry(short v)
        {
            public readonly short Value = v;
            public static implicit operator short(CHEntry entry) => entry.Value;
            public static implicit operator CHEntry(short s) => new(s);
            public static CHEntry operator <<(CHEntry entry, int bonus) => (CHEntry)(entry + (bonus - (entry * Math.Abs(bonus) / CaptureHistoryClamp)));
        }
    }
}
