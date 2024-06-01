namespace Lizard.Logic.NN
{
    public unsafe struct BucketCache
    {
        /// <summary>
        /// 2 Boards, 1 for each perspective
        /// </summary>
        public Bitboard WhiteBoard = new Bitboard();
        public Bitboard BlackBoard = new Bitboard();
        public Accumulator Accumulator = new Accumulator();

        public BucketCache() { }
    }
}
