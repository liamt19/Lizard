using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LTChess.Logic.NN.HalfKA_HM;

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
        public fixed ulong CheckSquares[PieceNB - 1];
        public fixed int KingSquares[2];
        public fixed ulong BlockingPieces[2];
        public fixed ulong Pinners[2];
        public fixed ulong Xrays[2];

        public ulong Hash = 0;
        public ulong Checkers = 0;

        public CastlingStatus CastleStatus = CastlingStatus.None;
        public int HalfmoveClock = 0;
        public int EPSquare = EPNone;
        public int CapturedPiece = None;

        public AccumulatorPSQT* Accumulator;

        public StateInfo()
        {

        }

    }
}
