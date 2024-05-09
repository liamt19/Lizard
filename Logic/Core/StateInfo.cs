using System.Runtime.InteropServices;

using Lizard.Logic.NN;

namespace Lizard.Logic.Core
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
        public static readonly nuint StateCopySize = (nuint)(sizeof(StateInfo) - sizeof(Accumulator*));
        static StateInfo()
        {
            int accOffset = ((FieldOffsetAttribute)typeof(StateInfo).GetField("Accumulator").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0]).Value;
            Assert(accOffset == (int)StateCopySize,
                $"StateInfo's Accumulator pointer is {accOffset} / {sizeof(StateInfo)}, should be {StateCopySize}");
        }

        [FieldOffset(0)]
        public fixed ulong CheckSquares[PieceNB];

        [FieldOffset(48)]
        public fixed int KingSquares[2];

        [FieldOffset(56)]
        public fixed ulong BlockingPieces[2];

        [FieldOffset(72)]
        public fixed ulong Pinners[2];

        [FieldOffset(88)]
        public ulong Hash = 0;

        [FieldOffset(96)]
        public ulong Checkers = 0;

        [FieldOffset(104)]
        public CastlingStatus CastleStatus = CastlingStatus.None;

        /// <summary>
        /// The first number in the FEN, which starts at 0 and resets to 0 every time a pawn moves or a piece is captured.
        /// If this reaches 100, the game is a draw by the 50-move rule.
        /// </summary>
        [FieldOffset(108)]
        public int HalfmoveClock = 0;

        [FieldOffset(112)]
        public int EPSquare = EPNone;

        [FieldOffset(116)]
        public int CapturedPiece = None;

        [FieldOffset(120)]
        public Accumulator* Accumulator;

        public StateInfo()
        {

        }

    }
}
