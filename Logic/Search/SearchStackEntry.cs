using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Search.Ordering;

namespace Lizard.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public unsafe struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry();

        /// <summary>
        /// The move that the Negamax/QSearch loop is currently on (or Move.Null for Null Move Pruning) at the current <see cref="Ply"/>
        /// <para></para>
        /// This is set before every recursive call to Negamax/QSearch.
        /// </summary>
        [FieldOffset(0)]
        public Move CurrentMove;

        /// <summary>
        /// When legal moves are generated, this move will be skipped.
        /// <br></br>
        /// This is used in Singular Extension searches to determine if every other move is significantly worse
        /// than the excluded one, and if so we will look at the excluded one more deeply.
        /// </summary>
        [FieldOffset(4)]
        public Move Skip;

        /// <summary>
        /// A pointer to a 2D array of scores (short[12][64]) for a particular move.
        /// <br></br>
        /// This should be updated after a move is made, and before a recursive call to Negamax/QSearch.
        /// </summary>
        [FieldOffset(8)]
        public PieceToHistory* ContinuationHistory;



        /// <summary>
        /// The History scores for the current move, which is currently only used in Late Move Reductions.
        /// </summary>
        [FieldOffset(16)]
        public int StatScore;

        /// <summary>
        /// The number of times that previous moves had their search depth extended by two.
        /// </summary>
        [FieldOffset(20)]
        public int Extensions;

        /// <summary>
        /// The number of moves made by both players thus far, which is generally the depth of the search times two.
        /// </summary>
        [FieldOffset(24)]
        public short Ply;

        /// <summary>
        /// The static evaluation for the position at the current <see cref="Ply"/>.
        /// </summary>
        [FieldOffset(26)]
        public short StaticEval;

        /// <summary>
        /// Whether or not the side to move is in check at the current <see cref="Ply"/>.
        /// </summary>
        [FieldOffset(28)]
        public bool InCheck;

        /// <summary>
        /// Set to true for PV/Root searches, or if <see cref="TTHit"/> is <see langword="true"/> 
        /// and the TT entry had TTPV true when it was updated.
        /// </summary>
        [FieldOffset(29)]
        public bool TTPV;

        /// <summary>
        /// Set to true if there was an acceptable <see cref="TTEntry"/> for the position at the current <see cref="Ply"/>.
        /// </summary>
        [FieldOffset(30)]
        public bool TTHit;

        [FieldOffset(31)]
        private fixed byte _pad0[1];



        /// <summary>
        /// A pointer to a <see langword="stackalloc"/>'d array of <see cref="Move"/>, which represents the current PV.
        /// <para></para>
        /// This must be set on a per-thread basis, and before that thread's search begins.
        /// </summary>
        [FieldOffset(32)]
        public Move* PV;

        [FieldOffset(40)]
        private fixed byte _pad1[8];



        /// <summary>
        /// The first killer move for the current <see cref="Ply"/>.
        /// </summary>
        [FieldOffset(48)]
        public Move Killer0;

        /// <summary>
        /// Killer0's score will be at this offset when <see cref="MovePicker"/> casts it as a <see cref="ScoredMove"/>. 
        /// </summary>
        [FieldOffset(52)]
        private fixed byte _pad3[4];

        /// <summary>
        /// The second killer move for the current <see cref="Ply"/>, 
        /// which is given Killer0's Move before Killer0 is overwritten with a new one.
        /// </summary>
        [FieldOffset(56)]
        public Move Killer1;

        /// <summary>
        /// Killer1's score will be at this offset when <see cref="MovePicker"/> casts it as a <see cref="ScoredMove"/>. 
        /// </summary>
        [FieldOffset(60)]
        private fixed byte _pad4[4];


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

            StatScore = 0;
            Ply = 0;
            Extensions = 0;
            StaticEval = ScoreNone;

            InCheck = false;
            TTPV = false;
            TTHit = false;
            PV = null;

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
