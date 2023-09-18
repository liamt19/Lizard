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
        public const int TableSize = MaxPly;

        private readonly Move[][] Table;
        private readonly int[] LineLengths;

        public PrincipalVariationTable()
        {
            Table = new Move[TableSize][];
            for (int i = 0; i < TableSize; i++)
            {
                Table[i] = new Move[TableSize];
                for (int j = 0; j < TableSize; j++)
                {
                    Table[i][j] = Move.Null;
                }
            }
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
            Table[ply][ply] = move;

            int nextPly = ply + 1;
            while (PlyInitialized(ply, nextPly))
            {
                Copy(ply, nextPly);
                nextPly++;
            }

            UpdateLength(ply);
        }


        [MethodImpl(Inline)]
        public void Copy(int currentPly, int nextPly)
        {
            Table[currentPly][nextPly] = Table[(currentPly + 1)][nextPly];
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
            return Table[0][plyIndex];
        }

        [MethodImpl(Inline)]
        public void Clear()
        {
            for (int i = 0; i < TableSize; i++)
            {
                Array.Clear(Table[i]);
            }
            Array.Clear(LineLengths);
        }
    }
}
