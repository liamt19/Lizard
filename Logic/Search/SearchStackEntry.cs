using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Search.History;

namespace Lizard.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public unsafe struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry();

        [FieldOffset( 0)] public Move CurrentMove;
        [FieldOffset( 4)] public Move Skip;
        [FieldOffset( 8)] public PieceToHistory* ContinuationHistory;
        [FieldOffset(16)] public int DoubleExtensions;
        [FieldOffset(20)] public short Ply;
        [FieldOffset(22)] public short StaticEval;
        [FieldOffset(24)] public bool InCheck;
        [FieldOffset(25)] public bool TTPV;
        [FieldOffset(26)] public bool TTHit;
        [FieldOffset(27)] private fixed byte _pad0[1];
        [FieldOffset(28)] public Move KillerMove;
        [FieldOffset(32)] public Move* PV;


        public SearchStackEntry()
        {
            Clear();
        }

        /// <summary>
        /// Zeroes the fields within this Entry.
        /// </summary>
        public void Clear()
        {
            CurrentMove = Move.Null;
            Skip = Move.Null;
            ContinuationHistory = null;

            Ply = 0;
            DoubleExtensions = 0;
            StaticEval = ScoreNone;

            InCheck = false;
            TTPV = false;
            TTHit = false;

            if (PV != null)
            {
                NativeMemory.AlignedFree(PV);
                PV = null;
            }

            KillerMove = Move.Null;
        }

        public static string GetMovesPlayed(SearchStackEntry* curr)
        {
            StringBuilder sb = new StringBuilder();

            //  Not using a while loop here to prevent infinite loops or some other nonsense.
            for (int i = curr->Ply; i >= 0; i--)
            {
                sb.Insert(0, curr->CurrentMove.ToString() + " ");
                curr--;
            }

            return sb.ToString();
        }
    }
}
