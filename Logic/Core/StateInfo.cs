using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace LTChess.Logic.Core
{
    /// <summary>
    /// Contains information for a single moment of a <see cref="Position"/>.
    /// <br></br>
    /// When a <see cref="Move"/> is made, the <see cref="Position"/> will update one of these 
    /// with information such as the move's captured piece and changes to either players <see cref="CastlingStatus"/>, 
    /// and keep track of the squares that checks can occur on.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct StateInfo
    {
        public fixed ulong CheckSquares[PieceNB];
        public fixed ulong BlockingPieces[2];
        public fixed ulong Pinners[2];
        public fixed ulong Xrays[2];

        public ulong Hash = 0;
        public ulong Checkers = 0;

        public CastlingStatus CastleStatus = CastlingStatus.None;
        public int HalfmoveClock = 0;
        public int EPSquare = SquareNB;
        public int CapturedPiece = None;

        public StateInfo()
        {

        }

        /// <summary>
        /// Returns true if <paramref name="st"/> isn't null, and appears to contain valid data.
        /// <para></para>
        /// <see cref="NativeMemory.AlignedFree"/> will cause a crash if it attempts to free a memory block twice, or if it wasn't allocated with <see cref="NativeMemory.AlignedAlloc"/>
        /// <br></br>
        /// Hopefully, if <paramref name="st"/> isn't valid or has already been freed it will contain junk data in some of the fields,
        /// so we can guess that if st->EPSquare is 1521356, this pointer is junk.
        /// </summary>
        public static bool PointerValid(StateInfo* st)
        {
            if (st != null)
            {
                if (((nuint)st) == 1)
                {
                    Log("ERROR st was 1, not NULL or valid!");
                }

                if ((st->EPSquare >= 0 && st->EPSquare <= 64) &&
                    (st->CapturedPiece >= Pawn && st->CapturedPiece <= None) &&
                    (popcount(st->Checkers) <= 2))
                {
                    return true;
                }
            }

            return false;
        }

        public static string StringFormat(StateInfo* st)
        {
            return ("[" + ((nuint)st).ToString("X12") + ", Previous: " + ((nuint)(st-1)).ToString("X12") + "]");
        }
    }
}
