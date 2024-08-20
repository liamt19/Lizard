using System.Runtime.InteropServices;

using Lizard.Logic.Threads;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct CorrectionHistoryTable
    {
        private readonly CorrectionStatEntry* _History;
        public const int CorrectionHistoryClamp = CorrectionMax;
        public const int CorrectionHistoryElements = CORR_HISTORY_SIZE * ColorNB;

        private const int PAWN_HISTORY_SIZE = 512;
        private const int CORR_HISTORY_SIZE = 16384;

        public CorrectionHistoryTable()
        {
            _History = (CorrectionStatEntry*)AlignedAllocZeroed((nuint)sizeof(CorrectionStatEntry) * CorrectionHistoryElements, AllocAlignment);
        }

        public ref CorrectionStatEntry this[int idx]
        {
            get => ref _History[idx];
        }

        public ref CorrectionStatEntry this[Position pos, int pc]
        {
            get => ref _History[CorrectionIndex(pos, pc)];
        }

        public CorrectionStatEntry this[SearchThread thread, int pc]
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
            NativeMemory.Clear(_History, (nuint)sizeof(CorrectionStatEntry) * CorrectionHistoryElements);
        }

        public static int CorrectionIndex(Position pos, int pc)
        {
            return (pc * CORR_HISTORY_SIZE) + (int)((pos.PawnHash) & (CORR_HISTORY_SIZE - 1));
        }

        public readonly struct CorrectionStatEntry(short v)
        {
            public readonly short Value = v;

            public static implicit operator short(CorrectionStatEntry entry) => entry.Value;
            public static implicit operator CorrectionStatEntry(short s) => new(s);
            public static CorrectionStatEntry operator <<(CorrectionStatEntry entry, int adjust) =>
                (CorrectionStatEntry)(Math.Clamp(adjust, -CorrectionMax, CorrectionMax));
        }
    }
}