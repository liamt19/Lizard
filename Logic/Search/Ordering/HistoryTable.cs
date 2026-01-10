using Lizard.Logic.Search.History;
using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.Ordering
{
    /// <summary>
    /// Holds instances of the MainHistory, CaptureHistory, and 4 ContinuationHistory's for a single SearchThread.
    /// </summary>
    public unsafe readonly struct HistoryTable
    {
        public const int NormalClamp = 16384;

        public readonly MainHistoryTable MainHistory;
        public readonly CaptureHistoryTable CaptureHistory;
        public readonly PlyHistoryTable PlyHistory;

        public readonly PawnCorrectionTable PawnCorrection;
        public readonly NonPawnCorrectionTable NonPawnCorrection;

        /// <summary>
        /// Index with [inCheck] [Capture]
        /// <para></para>
        /// Continuations[0][0] is the PieceToHistory[][] for a non-capture while we aren't in check,
        /// and that PieceToHistory[0, 1, 2] is the correct PieceToHistory for a white (0) knight (1) moving to C1 (2).
        /// This is then used by <see cref="MoveOrdering"/>.AssignScores
        /// </summary>
        public readonly ContinuationHistory** Continuations;

        public readonly PieceToHistory* NullContHist => &(Continuations[0][0][0][0]);

        public HistoryTable()
        {
            MainHistory = new MainHistoryTable();
            CaptureHistory = new CaptureHistoryTable();
            PlyHistory = new PlyHistoryTable();
            PawnCorrection = new PawnCorrectionTable();
            NonPawnCorrection = new NonPawnCorrectionTable();

            //  5D arrays aren't real, they can't hurt you.
            //  5D arrays:
            Continuations = (ContinuationHistory**)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));
            Continuations[0] = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));
            Continuations[1] = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2));

            Continuations[0][0] = new ContinuationHistory();
            Continuations[0][1] = new ContinuationHistory();

            Continuations[1][0] = new ContinuationHistory();
            Continuations[1][1] = new ContinuationHistory();
        }

        public void Dispose()
        {
            MainHistory.Dispose();
            CaptureHistory.Dispose();
            PlyHistory.Dispose();
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
            PlyHistory.Clear();
            PawnCorrection.Clear();
            NonPawnCorrection.Clear();

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Clear();
                Continuations[i][1].Clear();
            }
        }

        public readonly void UpdateMainHistory(int stm, Move move, int bonus) => MainHistory[stm, move] <<= bonus;
        public readonly short GetMainHistory(int stm, Move move) => MainHistory[stm, move];

        public readonly void UpdatePlyHistory(short ply, Move move, int bonus) => PlyHistory[ply, move] <<= bonus;
        public readonly short GetPlyHistory(short ply, Move move) => PlyHistory[ply, move];

        public readonly void UpdateNoisyHistory(int movingPiece, int dstSq, int theirPieceType, int bonus) => CaptureHistory[movingPiece, dstSq, theirPieceType] <<= bonus;
        public readonly short GetNoisyHistory(int movingPiece, int dstSq, int theirPieceType) => CaptureHistory[movingPiece, dstSq, theirPieceType];

        public readonly void UpdateQuietScore(PieceToHistory** cont, Move* moves, short ply, int piece, Move move, int bonus, bool inCheck)
        {
            var stm = ColorOfPiece(piece);

            UpdateMainHistory(stm, move, bonus);
            if (ply < PlyHistoryTable.MaxPlies)
                UpdatePlyHistory(ply, move, bonus);

            UpdateContinuations(cont, moves, ply, piece, move.To, bonus, inCheck);
        }

        private static readonly int[] ContinuationOffsets = [1, 2, 4, 6];
        public static void UpdateContinuations(PieceToHistory** cont, Move* moves, short ply, int piece, int dstSq, int bonus, bool inCheck) {
            foreach (var i in ContinuationOffsets) {
                if (ply < i) break;
                if (inCheck && i > 2) break;

                if (!moves[ply - i])
                    continue;

                (*cont[ply - i])[piece, dstSq] <<= bonus;
            }
        }

        public static short GetContinuationEntry(PieceToHistory** cont, short ply, int i, int piece, int dstSq) {
            if (ply < i) return PieceToHistory.FillValue;

            return (*cont[ply - i])[piece, dstSq];
        }

        public readonly int GetCorrection(in Position pos)
        {
            var us = pos.ToMove;

            int corr = 0;
            corr += PawnCorrCoeff * PawnCorrection[pos, us];
            corr += NonPawnCorrCoeff * NonPawnCorrection[pos, us, White];
            corr += NonPawnCorrCoeff * NonPawnCorrection[pos, us, Black];
            corr /= CorrDivisor;

            return corr;
        }

        public readonly void UpdateCorrections(in Position pos, int diff, int depth)
        {
            var us = pos.ToMove;
            var bonus = Math.Clamp((diff * depth) / 8, -CorrectionEntry.CorrectionBonusLimit, CorrectionEntry.CorrectionBonusLimit);

            PawnCorrection[pos, us] <<= bonus;
            NonPawnCorrection[pos, us, White] <<= bonus;
            NonPawnCorrection[pos, us, Black] <<= bonus;
        }
    }
}
