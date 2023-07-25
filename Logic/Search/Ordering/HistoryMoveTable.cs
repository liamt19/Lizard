namespace LTChess.Search.Ordering
{
    /// <summary>
    /// Wrapper class for applying the history heuristic, which gives bonuses to the squares that 
    /// each player's pieces moved to if that move caused a beta cutoff.
    /// <br></br>
    /// This is less about assigning more points to a particular move and more about giving points to 
    /// moves based on where good moves were made recently by the different piece types.
    /// <br></br><br></br>
    /// This is similar to StockNemo's implementation: https://github.com/TheBlackPlague/StockNemo/blob/master/Backend/Data/HistoryTable.cs
    /// but ages moves differently.
    /// 
    /// </summary>
    public class HistoryMoveTable
    {
        private int[][][] Table;

        public HistoryMoveTable()
        {
            Table = new int[2][][];
            for (int pc = Color.White; pc <= Color.Black; pc++)
            {
                Table[pc] = new int[6][];
                for (int pt = Piece.Pawn; pt <= Piece.King; pt++)
                {
                    Table[pc][pt] = new int[64];
                }
            }
        }

        public int this[int pc, int pt, int idx]
        {
            get => Table[pc][pt][idx];
            set => Table[pc][pt][idx] = value;
        }

        /// <summary>
        /// Ages the scores stored in this table by halving them.
        /// </summary>
        [MethodImpl(Inline)]
        public void Reduce()
        {
            for (int pc = Color.White; pc <= Color.Black; pc++)
            {
                for (int pt = Piece.Pawn; pt <= Piece.King; pt++)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        Table[pc][pt][i] /= 2;
                    }
                }
            }
        }
    }
}
