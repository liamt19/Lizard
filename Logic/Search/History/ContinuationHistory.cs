using System.Runtime.InteropServices;

using Lizard.Logic.Search.Ordering;

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

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const nuint Length = DimX * DimY;

        /// <summary>
        /// 8 * (<inheritdoc cref="Length"/>) == 6144 bytes
        /// </summary>
        private const nuint ByteSize = (nuint)(sizeof(ulong) * Length);

        public ContinuationHistory()
        {
            _History = (PieceToHistory*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Alloc();
            }
        }

        public void Dispose()
        {
            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Dispose();
            }

            NativeMemory.AlignedFree(_History);
        }


        public PieceToHistory* this[int pc, int pt, int sq]
        {
            get
            {
                int idx = PieceToHistory.GetIndex(pc, pt, sq);
                return &_History[idx];
            }
        }

        public PieceToHistory* this[int idx]
        {
            get
            {
                return &_History[idx];
            }
        }


        public void Clear()
        {
            for (nuint i = 0; i < Length; i++)
            {
                _History[i].Clear();
            }
        }
    }
}
