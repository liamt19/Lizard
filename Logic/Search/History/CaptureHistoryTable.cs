using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public unsafe readonly struct CaptureHistoryTable
    {
        private readonly StatEntry* _History;
        public const int CaptureHistoryElements = ColorNB * (PieceNB) * SquareNB * (PieceNB);

        public CaptureHistoryTable()
        {
            _History = (StatEntry*)AlignedAllocZeroed((nuint)sizeof(StatEntry) * CaptureHistoryElements, AllocAlignment);
        }

        public StatEntry this[int idx]
        {
            get => _History[idx];
            set => _History[idx] = value;
        }

        public StatEntry this[int pc, int pt, int toSquare, int capturedPt]
        {
            get => _History[CapIndex(pc, pt, toSquare, capturedPt)];
            set => _History[CapIndex(pc, pt, toSquare, capturedPt)] = value;
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

        public void Clear()
        {
            NativeMemory.Clear(_History, (nuint)sizeof(StatEntry) * CaptureHistoryElements);
        }

        /// <summary>
        /// Returns the index of the score that should be applied to a piece of type <paramref name="pt"/> and color <paramref name="pc"/> 
        /// capturing a piece of type <paramref name="capturedPt"/> on the square <paramref name="toSquare"/>.
        /// <br></br>
        /// This just calculates the flattened 3D array index for 
        /// <see cref="CaptureHistory"/>[<paramref name="pt"/> + <paramref name="pc"/>][<paramref name="toSquare"/>][<paramref name="capturedPt"/>].
        /// </summary>
        public static int CapIndex(int pc, int pt, int toSquare, int capturedPt)
        {
            return (capturedPt * 768) + (toSquare * 12) + (pt + (pc * 6));
        }
    }
}
