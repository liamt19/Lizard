
namespace Lizard.Logic.Search.History
{

    public unsafe class PawnCorrectionTable : ICorrectionTable
    {
        public ref StatEntry this[Position pos, int pc] => ref _History[CorrectionIndex(pos, pc)];

        public int CorrectionIndex(Position pos, int pc)
        {
            return (pc * TableSize) + (int)((pos.PawnHash) & ((ulong)TableSize - 1));
        }
    }


    /// Idea from Starzix:
    /// https://zzzzz151.pythonanywhere.com/test/729/
    public unsafe class NonPawnCorrectionTable : ICorrectionTable
    {
        public ref StatEntry this[Position pos, int pc, int side] => ref _History[CorrectionIndex(pos, pc, side)];

        public int CorrectionIndex(Position pos, int pc, int side)
        {
            return (pc * TableSize) + (int)((pos.NonPawnHash(side)) & ((ulong)TableSize - 1));
        }
    }

    /// Idea by MinusKelvin:
    /// https://github.com/MinusKelvin
    public unsafe class ContinuationCorrectionTable : ICorrectionTable
    {
        private const int ContCorrSize = (PieceNB + 1) * SquareNB * (PieceNB + 1) * SquareNB;

        public ContinuationCorrectionTable() : base(ContCorrSize, 1) { }

        public ref StatEntry this[int pt1, int to1, int pt2, int to2] => ref _History[CorrectionIndex(pt1, to1, to2, to2)];

        public int CorrectionIndex(int pt1, int to1, int pt2, int to2)
        {
            return (pt1 * SquareNB * (PieceNB + 1) * SquareNB) + (to1 * SquareNB * (PieceNB + 1)) + (pt2 * SquareNB) + to2;
        }
    }
}
