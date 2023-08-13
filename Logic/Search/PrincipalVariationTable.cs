namespace LTChess.Logic.Search
{
    /// <summary>
    /// Wrapper class storing moves that are within our principal variation line.
    /// <br></br><br></br>
    /// This is similar to StockNemo's implementation: https://github.com/TheBlackPlague/StockNemo/blob/master/Backend/Data/PrincipleVariationTable.cs
    /// 
    /// </summary>
    public class PrincipalVariationTable
    {
        private const int TableSize = NormalListCapacity;

        private readonly Move[] Table;
        private readonly int[] LineLengths;

        public PrincipalVariationTable()
        {
            Table = new Move[TableSize * TableSize];
            LineLengths = new int[TableSize];
        }


        [MethodImpl(Inline)]
        public void InitializeLength(int ply)
        {
            LineLengths[ply] = ply;
        }


        [MethodImpl(Inline)]
        public void Insert(int ply, Move move)
        {
            Table[ply * TableSize + ply] = move;
        }


        [MethodImpl(Inline)]
        public void Copy(int currentPly, int nextPly)
        {
            Table[currentPly * TableSize + nextPly] = Table[(currentPly + 1) * TableSize + nextPly];
        }


        [MethodImpl(Inline)]
        public bool PlyInitialized(int currentPly, int nextPly)
        {
            return nextPly < LineLengths[currentPly + 1];
        }


        [MethodImpl(Inline)]
        public void UpdateLength(int ply)
        {
            LineLengths[ply] = LineLengths[ply + 1];
        }


        [MethodImpl(Inline)]
        public int Count()
        {
            return LineLengths[0];
        }


        [MethodImpl(Inline)]
        public Move Get(int plyIndex)
        {
            return Table[plyIndex];
        }

        [MethodImpl(Inline)]
        public void Clear()
        {
            Array.Clear(Table);
            Array.Clear(LineLengths);
        }
    }
}
