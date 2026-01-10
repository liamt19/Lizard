using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Search.History;

namespace Lizard.Logic.Search
{
    /// <summary>
    /// Used during a search to keep track of information from earlier plies/depths
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public unsafe struct SearchStackEntry
    {
        public static SearchStackEntry NullEntry = new SearchStackEntry();

        [FieldOffset( 0)] public Move* PV;
        [FieldOffset( 8)] public short DoubleExtensions;
        [FieldOffset(10)] public short Ply;
        [FieldOffset(12)] public short StaticEval;
        [FieldOffset(14)] public Move KillerMove;
        [FieldOffset(16)] public Move Skip;
        [FieldOffset(18)] public bool InCheck;
        [FieldOffset(19)] public bool TTPV;
        [FieldOffset(20)] public bool TTHit;
        [FieldOffset(21)] private fixed byte _pad0[3];


        public SearchStackEntry()
        {
            Clear();
        }

        /// <summary>
        /// Zeroes the fields within this Entry.
        /// </summary>
        public void Clear()
        {
            Skip = Move.Null;

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

    }
}
