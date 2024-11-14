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
        /// Stores history for a color moving from one square to another
        /// </summary>
        public readonly MainHistoryTable MainHistory;

        /// <summary>
        /// Stores history for a piece + color capturing a piece on a square
        /// </summary>
        public readonly CaptureHistoryTable CaptureHistory;

        /// <summary>
        /// Stores history for how far off the static evaluation was from the result of a search for either color,
        /// indexed by a position's pawn PSQT hash.
        /// </summary>
        public readonly PawnCorrectionTable PawnCorrection;

        /// <summary>
        /// Stores history for how far off the static evaluation was from the result of a search for either color,
        /// indexed by a position's non-pawn PSQT hash.
        /// </summary>
        public readonly NonPawnCorrectionTable NonPawnCorrection;

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
            PawnCorrection = new PawnCorrectionTable();
            NonPawnCorrection = new NonPawnCorrectionTable();

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
            PawnCorrection.Dispose();
            NonPawnCorrection.Dispose();

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Dispose();
                Continuations[i][1].Dispose();
            }

            NativeMemory.AlignedFree(Continuations);
        }

        public void Clear()
        {
            MainHistory.Clear();
            CaptureHistory.Clear();
            PawnCorrection.Clear();
            NonPawnCorrection.Clear();

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Clear();
                Continuations[i][1].Clear();
            }
        }
    }
}
