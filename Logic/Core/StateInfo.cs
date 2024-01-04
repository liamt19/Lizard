using System.Runtime.InteropServices;

using LTChess.Logic.NN;

namespace LTChess.Logic.Core
{
    /// <summary>
    /// Contains information for a single moment of a <see cref="Position"/>.
    /// <br></br>
    /// When a <see cref="Move"/> is made, the <see cref="Position"/> will update one of these 
    /// with information such as the move's captured piece and changes to either players <see cref="CastlingStatus"/>, 
    /// and keep track of the squares that checks can occur on.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct StateInfo
    {
        public static readonly nuint StateCopySize;
        static StateInfo()
        {
            StateCopySize = (nuint)(sizeof(StateInfo) - sizeof(Accumulator*));

            if (EnableAssertions)
            {
                //  Static assertion
                int accOffset = ((FieldOffsetAttribute)typeof(StateInfo).GetField("Accumulator").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0]).Value;

                Assert(accOffset == (int)StateCopySize,
                    "A StateInfo's Accumulator pointer must be the last field in the struct! " +
                    "It's offset is currently " + accOffset + " / " + sizeof(StateInfo) + ", but it should be at " + StateCopySize);
            }
        }

        [FieldOffset(0)]
        public fixed ulong CheckSquares[PieceNB - 1];

        [FieldOffset(40)]
        public fixed int KingSquares[2];

        [FieldOffset(48)]
        public fixed ulong BlockingPieces[2];

        [FieldOffset(64)]
        public fixed ulong Pinners[2];

        [FieldOffset(80)]
        public fixed ulong Xrays[2];

        [FieldOffset(96)]
        public ulong Hash = 0;

        [FieldOffset(104)]
        public ulong Checkers = 0;

        [FieldOffset(112)]
        public CastlingStatus CastleStatus = CastlingStatus.None;

        /// <summary>
        /// The first number in the FEN, which starts at 0 and resets to 0 every time a pawn moves or a piece is captured.
        /// If this reaches 100, the game is a draw by the 50-move rule.
        /// </summary>
        [FieldOffset(116)]
        public int HalfmoveClock = 0;

        [FieldOffset(120)]
        public int EPSquare = EPNone;

        [FieldOffset(124)]
        public int CapturedPiece = None;

        [FieldOffset(128)]
        public Accumulator* Accumulator;

        public StateInfo()
        {

        }

    }
}
