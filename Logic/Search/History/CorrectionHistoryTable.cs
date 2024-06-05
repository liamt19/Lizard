using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct CorrectionHistoryTable
    {
        private readonly CorrectionStatEntry* _History;
        public const int CorrectionHistoryClamp = 1024;
        public const int CorrectionHistoryElements = CORR_HISTORY_SIZE * ColorNB;

        private const int PAWN_HISTORY_SIZE = 512;
        private const int CORR_HISTORY_SIZE = 16384;

        public CorrectionHistoryTable()
        {
            _History = (CorrectionStatEntry*)AlignedAllocZeroed((nuint)sizeof(CorrectionStatEntry) * CorrectionHistoryElements, AllocAlignment);
        }

        public CorrectionStatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public CorrectionStatEntry this[Position pos, int pc]
        {
            get => _History[CorrectionIndex(pos, pc)];
            set => _History[CorrectionIndex(pos, pc)] = value;
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
            public static CorrectionStatEntry operator <<(CorrectionStatEntry entry, int bonus) => 
                (CorrectionStatEntry)(entry + (bonus - (entry * Math.Abs(bonus) / CorrectionHistoryClamp)));
        }
    }
}
