using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    /// <summary>
    /// Records the history for a pair of moves.
    /// <br></br>
    /// This is an array of <see cref="PieceToHistory"/> [12][64], with a size of <inheritdoc cref="ByteSize"/>.
    /// </summary>
    public unsafe struct ContinuationHistory
    {
        private PieceToHistory* _History;

        private const int DimX = PieceNB * 2;
        private const int DimY = SquareNB;

        public const nuint Length = DimX * DimY;
        private const nuint ByteSize = (nuint)(sizeof(ulong) * Length);

        public ContinuationHistory()
        {
            _History = (PieceToHistory*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            for (nuint i = 0; i < Length; i++)
                (_History + i)->Alloc();
        }

        public PieceToHistory* this[int idx]
        {
            get => &_History[idx];
        }

        public PieceToHistory* this[int pc, int pt, int sq]
        {
            get => &_History[PieceToHistory.GetIndex(pc, pt, sq)];
        }

        public void Dispose()
        {
            for (nuint i = 0; i < Length; i++)
                (_History + i)->Dispose();

            NativeMemory.AlignedFree(_History);
        }

        public void Clear()
        {
            for (nuint i = 0; i < Length; i++)
                _History[i].Clear();
        }
    }
}
