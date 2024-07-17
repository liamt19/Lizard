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

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const int Length = DimX * DimY;


        public ContinuationHistory()
        {
            _History = (PieceToHistory*)AlignedAllocZeroed((nuint)(sizeof(ulong) * Length), AllocAlignment);

            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Alloc();
            }
        }

        public PieceToHistory* this[int pc, int pt, int sq] => &_History[PieceToHistory.GetIndex(pc, pt, sq)];
        public PieceToHistory* this[int idx] => &_History[idx];

        public void Dispose()
        {
            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Dispose();
            }

            NativeMemory.AlignedFree(_History);
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
