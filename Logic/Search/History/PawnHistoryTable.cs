using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct PawnHistoryTable
    {
        private readonly PawnStatEntry* _History;
        private const int PawnHistoryClamp = 8192;
        private const int PawnHistoryElements = PAWN_HISTORY_SIZE * PieceNB * ColorNB * SquareNB;

        private const int PAWN_HISTORY_SIZE = 512;
        private const int CORR_HISTORY_SIZE = 16384;

        public PawnHistoryTable()
        {
            _History = (PawnStatEntry*)AlignedAllocZeroed((nuint)sizeof(PawnStatEntry) * PawnHistoryElements, AllocAlignment);
        }

        public PawnStatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public PawnStatEntry this[Position pos, int pc, int pt, int toSquare]
        {
            get => _History[PawnHistoryIndex(pos, pc, pt, toSquare)];
            set => _History[PawnHistoryIndex(pos, pc, pt, toSquare)] = value;
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

        public void Clear()
        {
            NativeMemory.Clear(_History, (nuint)sizeof(PawnStatEntry) * PawnHistoryElements);
        }

        public static int PawnHistoryIndex(Position pos, int pc, int pt, int toSquare)
        {
            const int xMax = PAWN_HISTORY_SIZE;
            const int yMax = (PieceNB * ColorNB);
            const int zMax = SquareNB;

            int x = (int)((pos.PawnHash) & (PAWN_HISTORY_SIZE - 1));
            int y = pt + ((PieceNB) * pc);
            int z = toSquare;

            return (z * xMax * yMax) + (y * xMax) + x;
        }

        public readonly struct PawnStatEntry(short v)
        {
            public readonly short Value = v;

            public static implicit operator short(PawnStatEntry entry) => entry.Value;
            public static implicit operator PawnStatEntry(short s) => new(s);
            public static PawnStatEntry operator <<(PawnStatEntry entry, int bonus) =>
                (PawnStatEntry)(entry + (bonus - (entry * Math.Abs(bonus) / PawnHistoryClamp)));
        }
    }
}