using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Search.History;

namespace Lizard.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
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
        [FieldOffset(27)] private fixed byte _pad0[5];
        [FieldOffset(32)] public Move* PV;
        [FieldOffset(40)] private fixed byte _pad1[8];
        [FieldOffset(48)] public Move Killer0;
        [FieldOffset(52)] private fixed byte _pad2[4];
        [FieldOffset(56)] public Move Killer1;
        [FieldOffset(60)] private fixed byte _pad3[4];


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

            Killer0 = Move.Null;
            Killer1 = Move.Null;
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
