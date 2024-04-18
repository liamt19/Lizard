using System.Runtime.InteropServices;

using Lizard.Logic.Search.Ordering;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct MainHistoryTable
    {
        public readonly StatEntry* _History;
        public const int MainHistoryPCStride = 4096;
        public const int MainHistoryElements = ColorNB * SquareNB * SquareNB;

        public MainHistoryTable()
        {
            _History = (StatEntry*)AlignedAllocZeroed((nuint)sizeof(StatEntry) * MainHistoryElements, AllocAlignment);
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

        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

        public void Clear()
        {
            NativeMemory.Clear(_History, (nuint)sizeof(StatEntry) * MainHistoryElements);
        }

        public static int HistoryIndex(int pc, Move m)
        {
            if (EnableAssertions)
            {
                Assert(((pc * MainHistoryPCStride) + m.MoveMask) is >= 0 and < MainHistoryElements,
                    "HistoryIndex(" + pc + ", " + m.MoveMask + ") is OOB! (should be 0 <= idx < " + MainHistoryElements + ")");
            }

            return (pc * MainHistoryPCStride) + m.MoveMask;
        }
    }
}
