namespace LTChess.Logic.Transposition
{
    public struct ETEntry
    {
        public const short InvalidScore = short.MaxValue - 3;
        public const int KeyShift = 48;

        public ushort Key;
        public short Score = InvalidScore;


        public ETEntry(ulong hash, short score)
        {
            this.Key = (ushort)(hash >> KeyShift);
            this.Score = score;
        }

        [MethodImpl(Inline)]
        public bool ValidateKey(ulong hash)
        {
            return Key == (ushort)(hash >> KeyShift);
        }
    }
}
