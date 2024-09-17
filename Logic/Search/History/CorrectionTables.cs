
namespace Lizard.Logic.Search.History
{

    public unsafe class PawnCorrectionTable : ICorrectionTable
    {
        public override ref StatEntry this[Position pos, int pc] => ref _History[CorrectionIndex(pos, pc)];
        public override ref StatEntry this[Position pos, int pc, int side] => throw new NotImplementedException();

        public override int CorrectionIndex(Position pos, int pc, int side = 0)
        {
            return (pc * TableSize) + (int)((pos.PawnHash) & ((ulong)TableSize - 1));
        }
    }


    /// Idea from Starzix:
    /// https://zzzzz151.pythonanywhere.com/test/729/
    public unsafe class NonPawnCorrectionTable : ICorrectionTable
    {
        public override ref StatEntry this[Position pos, int pc] => throw new NotImplementedException();
        public override ref StatEntry this[Position pos, int pc, int side] => ref _History[CorrectionIndex(pos, pc, side)];

        public override int CorrectionIndex(Position pos, int pc, int side)
        {
            return (pc * TableSize) + (int)((pos.NonPawnHash(side)) & ((ulong)TableSize - 1));
        }
    }
}
