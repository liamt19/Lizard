namespace Lizard.Logic.Magic
{
    /// <summary>
    /// Contains information for one square of the magic bitboard for rooks or bishops
    /// </summary>
    public unsafe struct FancyMagicSquare
    {
        public ulong Mask;
        public ulong* Attacks;
        public int Shift;
    }
}