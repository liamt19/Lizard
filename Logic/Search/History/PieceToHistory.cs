
using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    /// <summary>
    /// Records how successful different moves have been in the past by recording that move's
    /// piece type and color, and the square that it is moving to. 
    /// <br></br>
    /// This is a short array with dimensions [12][64], with a size of <inheritdoc cref="ByteSize"/>.
    /// </summary>
    public unsafe struct PieceToHistory
    {
        private StatEntry* _History;

        private const short FillValue = -50;

        private const int DimX = PieceNB * 2;
        private const int DimY = SquareNB;

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const nuint Length = DimX * DimY;

        /// <summary>
        /// 2 * (<inheritdoc cref="Length"/>) == 1536 bytes
        /// </summary>
        public const nuint ByteSize = sizeof(short) * Length;

        public PieceToHistory() { }

        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

        public StatEntry this[int pc, int pt, int sq]
        {
            get => _History[GetIndex(pc, pt, sq)];
            set => _History[GetIndex(pc, pt, sq)] = value;
        }

        public StatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        /// <summary>
        /// Returns the index of the score in the History array for a piece of color <paramref name="pc"/> 
        /// and type <paramref name="pt"/> moving to the square <paramref name="sq"/>.
        /// </summary>
        public static int GetIndex(int pc, int pt, int sq)
        {
            Assert((((pt + (PieceNB * pc)) * DimY) + sq) is >= 0 and < (int)Length, $"GetIndex({pc}, {pt}, {sq}) should be < {Length}");

            return ((pt + (PieceNB * pc)) * DimY) + sq;
        }

        /// <summary>
        /// Allocates memory for this instance's array.
        /// </summary>
        public void Alloc()
        {
            _History = (StatEntry*)AlignedAllocZeroed(ByteSize, AllocAlignment);
        }

        /// <summary>
        /// Fills this instance's array with the value of <see cref="FillValue"/>.
        /// </summary>
        public void Clear()
        {
            NativeMemory.Clear(_History, ByteSize);
            Span<StatEntry> span = new Span<StatEntry>(_History, (int)Length);
            span.Fill(FillValue);
        }
    }

}
