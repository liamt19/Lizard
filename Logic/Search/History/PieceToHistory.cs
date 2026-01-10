
using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    /// <summary>
    /// Records how successful different moves have been in the past by recording that move's
    /// piece type and color, and the square that it is moving to. 
    /// <br></br>
    /// This is a short array with dimensions [12][64], with a size of <inheritdoc cref="Length"/>.
    /// </summary>
    public unsafe readonly struct PieceToHistory
    {
        private readonly StatEntry* _History;

        public const short FillValue = -50;

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const int Length = 12 * 64;

        public PieceToHistory()
        {
            _History = AlignedAllocZeroed<StatEntry>(Length);
        }

        public void Clear() => new Span<StatEntry>(_History, Length).Fill(FillValue);
        public void Dispose() => NativeMemory.AlignedFree(_History);

        public StatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public StatEntry this[int piece, int sq]
        {
            get => _History[GetIndex(piece, sq)];
            set => _History[GetIndex(piece, sq)] = value;
        }

        /// <summary>
        /// Returns the index of the score in the History array for a piece of color <paramref name="pc"/> 
        /// and type <paramref name="pt"/> moving to the square <paramref name="sq"/>.
        /// </summary>
        public static int GetIndex(int piece, int sq)
        {
            Assert(((piece * 64) + sq) is >= 0 and < (int)Length, $"GetIndex({piece}, {sq}) should be < {Length}");
            return (piece * 64) + sq;
        }

    }
}
