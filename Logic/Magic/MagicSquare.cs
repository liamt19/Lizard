namespace Lizard.Logic.Magic
{
    /// <summary>
    /// Contains information for one square of the magic bitboard for rooks or bishops
    /// </summary>
    public struct MagicSquare
    {
        public ulong Mask;
        public ulong Number;
        public ulong[] Attacks;
        public int Shift;
    }
}
