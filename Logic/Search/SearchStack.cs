namespace LTChess.Search
{
    /// <summary>
    /// Used during a search to keep track of the static evaluations of previous positions.
    /// <br></br>
    /// It is helpful to know if the static evaluation of the position is better now than it was on our last turn
    /// since that would suggest that our score is improving.
    /// </summary>
    public class SearchStack
    {
        public const int MaxSize = NormalListCapacity * 2;

        private readonly SearchStackEntry[] Stack;

        public SearchStack()
        {
            Stack = new SearchStackEntry[MaxSize];
        }

        public ref SearchStackEntry this[int ply]
        {
            get
            {
                if (ply <= 0 || ply >= MaxSize)
                {
                    return ref SearchStackEntry.NullEntry;
                }

                return ref Stack[ply];
            }
        }

        [MethodImpl(Inline)]
        public void Clear()
        {
            Array.Clear(Stack);
        }
    }

    public struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry(Move.Null, ETEntry.InvalidScore);

        public Move Move;
        public int StaticEval;

        public SearchStackEntry(Move move, int staticEval)
        {
            this.Move = move;
            this.StaticEval = staticEval;
        }
    }
}
