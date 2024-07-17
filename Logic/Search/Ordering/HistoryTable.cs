using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    /// <summary>
    /// Holds instances of the MainHistory, CaptureHistory, and 4 ContinuationHistory's for a single SearchThread.
    /// </summary>
    public unsafe struct HistoryTable
    {
        public const int NormalClamp = 16384;

        /// <summary>
        /// Index using [ourColor] [Move.MoveMask]
        /// </summary>
        public readonly MainHistoryTable MainHistory;

        /// <summary>
        /// Index using [ourColor] [ourPieceType] [Move.To] [theirPieceType]
        /// </summary>
        public readonly CaptureHistoryTable CaptureHistory;

        /// <summary>
        /// Index with [inCheck] [Capture]
        /// <para></para>
        /// Continuations[0][0] is the PieceToHistory[][] for a non-capture while we aren't in check,
        /// and that PieceToHistory[0, 1, 2] is the correct PieceToHistory for a white (0) knight (1) moving to C1 (2).
        /// This is then used by <see cref="MoveOrdering"/>.AssignScores
        /// </summary>
        public readonly ContinuationHistory** Continuations;

        public HistoryTable()
        {
            MainHistory = new MainHistoryTable();
            CaptureHistory = new CaptureHistoryTable();

            //  5D arrays aren't real, they can't hurt you.
            //  5D arrays:
            Continuations = (ContinuationHistory**)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));
            ContinuationHistory* cont0 = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));
            ContinuationHistory* cont1 = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));

            cont0[0] = new ContinuationHistory();
            cont0[1] = new ContinuationHistory();

            cont1[0] = new ContinuationHistory();
            cont1[1] = new ContinuationHistory();

            Continuations[0] = cont0;
            Continuations[1] = cont1;
        }

        public void Dispose()
        {
            MainHistory.Dispose();
            CaptureHistory.Dispose();

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Dispose();
                Continuations[i][1].Dispose();
            }

            NativeMemory.AlignedFree(Continuations);
        }
    }
}
