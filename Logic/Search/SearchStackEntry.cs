using System.Runtime.InteropServices;
using System.Text;

namespace LTChess.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of the static evaluations of previous positions.
    /// <br></br>
    /// It is helpful to know if the static evaluation of the position is better now than it was on our last turn
    /// since that would suggest that our score is improving.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public unsafe struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry();

        [FieldOffset(0)]
        public Move Killer0;

        [FieldOffset(4)]
        public Move Killer1;

        [FieldOffset(8)]
        public Move CurrentMove;

        [FieldOffset(12)]
        public int Ply;

        [FieldOffset(16)]
        public short StaticEval;

        [FieldOffset(18)]
        public bool InCheck;

        [FieldOffset(19)]
        public bool TTPV;

        [FieldOffset(20)]
        public bool TTHit;

        [FieldOffset(21)]
        public fixed byte pad[11];

        public SearchStackEntry()
        {
            Clear();

            byte[] temp = "hello world"u8.ToArray();
            for (int i = 0; i < 11; i++)
            {
                pad[i] = temp[i];
            }
        }

        public void Clear()
        {
            CurrentMove = Move.Null;
            Killer0 = Move.Null;
            Killer1 = Move.Null;
            Ply = 0;
            StaticEval = ScoreNone;

            InCheck = false;
            TTPV = false;
            TTHit = false;
        }
    }
}
