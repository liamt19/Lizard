using System.Runtime.InteropServices;

using Lizard.Logic.Threads;

namespace Lizard.Logic.Search.History
{
    /// Idea from Starzix:
    /// https://zzzzz151.pythonanywhere.com/test/729/
    public unsafe readonly struct MajorCorrectionHistoryTable
    {
        private readonly StatEntry* _History;
        public const int CorrectionHistoryClamp = CorrectionMax;
        public const int CorrectionHistoryElements = CORR_HISTORY_SIZE * ColorNB;

        private const int CORR_HISTORY_SIZE = 16384;

        public MajorCorrectionHistoryTable()
        {
            _History = (StatEntry*)AlignedAllocZeroed((nuint)sizeof(StatEntry) * CorrectionHistoryElements, AllocAlignment);
        }

        public ref StatEntry this[int idx]
        {
            get => ref _History[idx];
        }

        public ref StatEntry this[Position pos, int pc]
        {
            get => ref _History[CorrectionIndex(pos, pc)];
        }

        public StatEntry this[SearchThread thread, int pc]
        {
            get => this[thread.RootPosition, pc];
            set => this[thread.RootPosition, pc] = value;
        }


        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

        public void Clear()
        {
            NativeMemory.Clear(_History, (nuint)sizeof(StatEntry) * CorrectionHistoryElements);
        }

        public static int CorrectionIndex(Position pos, int pc)
        {
            return (pc * CORR_HISTORY_SIZE) + (int)((pos.NonPawnHash(pc)) & (CORR_HISTORY_SIZE - 1));
        }
    }
}