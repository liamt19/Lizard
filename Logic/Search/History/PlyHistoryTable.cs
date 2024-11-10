using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct PlyHistoryTable
    {
        private readonly PlyHistEntry* _History;
        private const int Clamp = 8192;

        public const int MaxPlies = 4;
        private const int HistoryElements = MaxPlies * SquareNB * SquareNB;

        public PlyHistoryTable()
        {
            _History = AlignedAllocZeroed<PlyHistEntry>(HistoryElements);
        }

        public PlyHistEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public PlyHistEntry this[int pc, Move m]
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
            NativeMemory.Clear(_History, (nuint)sizeof(PlyHistEntry) * HistoryElements);
        }

        public static int HistoryIndex(int ply, Move m)
        {
            return (ply * 4096) + m.MoveMask;
        }

        public readonly struct PlyHistEntry(short v)
        {
            public readonly short Value = v;

            public static implicit operator short(PlyHistEntry entry) => entry.Value;
            public static implicit operator PlyHistEntry(short s) => new(s);
            public static PlyHistEntry operator <<(PlyHistEntry entry, int bonus) => (PlyHistEntry)(entry + (bonus - (entry * Math.Abs(bonus) / Clamp)));
        }
    }
}
