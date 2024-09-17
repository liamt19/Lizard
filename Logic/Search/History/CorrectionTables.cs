
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
}
