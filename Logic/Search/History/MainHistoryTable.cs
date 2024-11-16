using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public readonly unsafe struct MainHistoryTable
    {
        private readonly StatEntry* _History;
        private const int MainHistoryElements = ColorNB * SquareNB * SquareNB;

        public MainHistoryTable()
        {
            _History = AlignedAllocZeroed<StatEntry>(MainHistoryElements);
        }

        public StatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public StatEntry this[int pc, Move m]
        {
            get => _History[HistoryIndex(pc, m)];
            set => _History[HistoryIndex(pc, m)] = value;
        }

        public static int HistoryIndex(int pc, Move m) => (pc * 4096) + m.MoveMask;

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)sizeof(StatEntry) * MainHistoryElements);
    }
}
