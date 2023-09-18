


using System.Runtime.InteropServices;

using LTChess.Logic.Data;

namespace LTChess.Logic.Search.Ordering
{
    public unsafe struct HistoryTable
    {
        /// <summary>
        /// Index using Color * <see cref="MainHistoryPCStride"/> + Move.MoveMask
        /// </summary>
        public short* MainHistory;
        public const int MainHistoryClamp = 7500;
        public const int MainHistoryPCStride = 4096;
        public const int MainHistoryElements = (ColorNB * SquareNB * SquareNB);


        public short* CaptureHistory;
        public const int CaptureClamp = 10000;
        public const int CaptureHistoryElements = ((ColorNB * PieceNB) * SquareNB * PieceNB);


        //  5D arrays aren't real, they can't hurt you.
        //  5D arrays:
        public short* ContinuationHistory;
        public const int ContinuationHistoryElements = (2 * 2 * (16 * 64) * (16 * 64));

        public HistoryTable()
        {
            MainHistory         = (short*) AlignedAllocZeroed(sizeof(short) * MainHistoryElements, AllocAlignment);
            CaptureHistory      = (short*) AlignedAllocZeroed(sizeof(short) * CaptureHistoryElements, AllocAlignment);
        }

        [MethodImpl(Inline)]
        public void ApplyBonus(short* field, int index, int bonus, int clamp)
        {
            field[index] += (short)(bonus - (field[index] * Math.Abs(bonus) / clamp));
        }

        [MethodImpl(Inline)]
        public static int CapIndex(int pt, int pc, int toSquare, int capturedPt)
        {
            const int xMax = (PieceNB * ColorNB);
            const int yMax = (SquareNB);
            const int zMax = (PieceNB);

            int x = (pt + (PieceNB * pc));
            int y = (toSquare);
            int z = (capturedPt);

            return (z * xMax * yMax) + (y * xMax) + x;
        }
    }

}
