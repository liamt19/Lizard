using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Search.History;

namespace Lizard.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public unsafe struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry();

        [FieldOffset( 0)] public Move* PV;
        [FieldOffset( 8)] public PieceToHistory* ContinuationHistory;
        [FieldOffset(16)] public short DoubleExtensions;
        [FieldOffset(18)] public short Ply;
        [FieldOffset(20)] public short StaticEval;
        [FieldOffset(22)] public Move KillerMove;
        [FieldOffset(24)] public Move CurrentMove;
        [FieldOffset(26)] public Move Skip;
        [FieldOffset(28)] public bool InCheck;
        [FieldOffset(29)] public bool TTPV;
        [FieldOffset(30)] public bool TTHit;
        [FieldOffset(31)] private fixed byte _pad0[1];


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
