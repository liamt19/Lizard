namespace LTChess.Logic.Search.Ordering
{
    /// <summary>
    /// Wrapper class for storing killer moves, which are non-capture moves which caused a beta cutoff 
    /// in a nearby branch and would likely do the same in other branches near it.
    /// <br></br>
    /// Pawn moves are stored here more often than others because they can threaten other pieces safely,
    /// and because pawn moves would otherwise be ordered very late in the list, this can potentially cause
    /// the best pawn move to be tried 2nd rather than 15th.
    /// <br></br><br></br>
    /// This is similar to StockNemo's implementation: https://github.com/TheBlackPlague/StockNemo/blob/master/Backend/Data/KillerMoveTable.cs
    /// 
    /// </summary>
    public class KillerMoveTable
    {
        public const int MaxSize = NormalListCapacity;
        public const int Slots = 2;

        private readonly Move[] Table;

        public KillerMoveTable()
        {
            Table = new Move[MaxSize * Slots];
        }

        /// <summary>
        /// Get/Set the move stored at (<see cref="MaxSize"/> * <paramref name="slot"/>) + <paramref name="ply"/>
        /// </summary>
        public Move this[int ply, int slot]
        {
            get => Table[MaxSize * slot + ply];
            set => Table[MaxSize * slot + ply] = value;
        }

        /// <summary>
        /// Places the current killer move located at Table[<paramref name="ply"/>] to that ply's second killer move slot,
        /// and sets the first killer move slot to be <paramref name="move"/>
        /// </summary>
        /// <param name="ply">The ply to place this move at</param>
        /// <param name="move">The new killer move to be placed in slot 1</param>
        [MethodImpl(Inline)]
        public void Replace(int ply, Move move)
        {
            Table[MaxSize + ply] = Table[ply];
            Table[ply] = move;
        }

        [MethodImpl(Inline)]
        public void Clear()
        {
            Array.Clear(Table);
        }
    }
}
