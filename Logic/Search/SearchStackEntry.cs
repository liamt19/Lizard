using System.Runtime.InteropServices;
using System.Text;

using LTChess.Logic.Search.Ordering;

namespace LTChess.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
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
        public Move ExcludedMove;


        /// <summary>
        /// A pointer to a 2D array of scores (short[12][64]) for a particular move.
        /// <br></br>
        /// This should be updated after a move is made, and before a recursive call to Negamax/QSearch.
        /// </summary>
        [FieldOffset(16)]
        public PieceToHistory* ContinuationHistory;

        [FieldOffset(24)]
        public int StatScore;

        [FieldOffset(28)]
        public int Ply;



        [FieldOffset(32)]
        public int MoveCount;

        [FieldOffset(36)]
        public int Extensions;

        [FieldOffset(40)]
        public int Cutoffs;

        [FieldOffset(44)]
        public short StaticEval;

        [FieldOffset(46)]
        private fixed byte _pad0[2];


        [FieldOffset(48)]
        public bool InCheck;

        [FieldOffset(49)]
        public bool TTPV;

        [FieldOffset(50)]
        public bool TTHit;

        [FieldOffset(51)]
        private fixed byte _pad1[5];

        [FieldOffset(56)]
        public Move* PV;


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
            Killer0 = Move.Null;
            Killer1 = Move.Null;
            ExcludedMove = Move.Null;

            ContinuationHistory = null;
            StatScore = 0;
            Ply = 0;


            MoveCount = 0;
            Extensions = 0;
            Cutoffs = 0;
            StaticEval = ScoreNone;

            InCheck = false;
            TTPV = false;
            TTHit = false;

            PV = null;
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
