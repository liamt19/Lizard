using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public readonly unsafe struct CaptureHistoryTable
    {
        private readonly StatEntry* _History;
        private const int CaptureHistoryElements = 2 * 6 * 64 * 6;

        public CaptureHistoryTable()
        {
            _History = AlignedAllocZeroed<StatEntry>(CaptureHistoryElements);
        }

        public StatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public StatEntry this[int pc, int pt, int toSquare, int capturedPt]
        {
            get => _History[HistoryIndex(pc, pt, toSquare, capturedPt)];
            set => _History[HistoryIndex(pc, pt, toSquare, capturedPt)] = value;
        }

        public static int HistoryIndex(int pc, int pt, int toSquare, int capturedPt) => (capturedPt * 768) + (toSquare * 12) + (pt + (pc * 6));

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)sizeof(StatEntry) * CaptureHistoryElements);
    }
}
