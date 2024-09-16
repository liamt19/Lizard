
namespace Lizard.Logic.Search.History
{

    public class PawnCorrectionTable : ICorrectionTable
    {
        public override int CorrectionIndex(Position pos, int pc)
        {
            return (pc * TableSize) + (int)((pos.PawnHash) & ((ulong)TableSize - 1));
        }
    }


    /// Idea from Starzix:
    /// https://zzzzz151.pythonanywhere.com/test/729/
    public class NonPawnCorrectionTable : ICorrectionTable
    {
        public override int CorrectionIndex(Position pos, int pc)
        {
            return (pc * TableSize) + (int)((pos.NonPawnHash(pc)) & ((ulong)TableSize - 1));
        }
    }
}
