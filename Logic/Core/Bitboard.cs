
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LTChess.Core
{
    /// <summary>
    /// Manages the bitboards for the position
    /// </summary>
    public struct Bitboard
    {
        /// <summary>
        /// Bitboard array for Pieces, from Piece.Pawn to Piece.King
        /// </summary>
        public ulong[] Pieces;

        /// <summary>
        /// Bitboard array for Colors, from Color.White to Color.Black
        /// </summary>
        public ulong[] Colors;

        /// <summary>
        /// Piece array indexed by square
        /// </summary>
        public int[] PieceTypes;

        public Bitboard()
        {
            Pieces = new ulong[6];
            Colors = new ulong[2];

            PieceTypes = new int[64];
            Array.Fill(PieceTypes, Piece.None);
        }

        public string SquareToString(int idx)
        {
            return ColorToString(GetColorAtIndex(idx)) + " " +
                   PieceToString(PieceTypes[idx]) + " on " + 
                   IndexToString(idx);
        }

        /// <summary>
        /// 0's the Piece and Color arrays and fills the PieceType array with Piece.None .
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < Pieces.Length; i++)
            {
                Pieces[i] = 0UL;
            }

            Colors[Color.White] = 0UL;
            Colors[Color.Black] = 0UL;

            Array.Fill(PieceTypes, Piece.None);
        }

        /// <summary>
        /// Returns true if White or Black has a piece on <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public bool Occupied(int idx)
        {
            return PieceTypes[idx] != Piece.None;
        }

        [MethodImpl(Inline)]
        public bool OccupiedUnsafe(int idx)
        {
            return (Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(PieceTypes), idx) != Piece.None);
        }

        [MethodImpl(Inline)]
        public unsafe bool OccupiedPtr(int idx)
        {
            fixed (int* ptr = PieceTypes)
            {
                return ptr[idx] != Piece.None;
            }
        }

        /// <summary>
        /// Removes the pawn at <paramref name="from"/>, and replaces it with the <paramref name="promotionPiece"/> at the index <paramref name="to"/>.
        /// Also captures the piece on <paramref name="to"/> if there is one.
        /// </summary>
        [MethodImpl(Inline)]
        public void Promote(int from, int to, int promotionPiece)
        {
            int pc = GetColorAtIndex(from);

            int capturedPiece = PieceTypes[to];
            if (capturedPiece != Piece.None)
            {
                //  Delete that piece now
                Pieces[capturedPiece] ^= SquareBB[to];
                Colors[Not(pc)] ^= SquareBB[to];
            }

            Pieces[Piece.Pawn] ^= SquareBB[from];
            Pieces[promotionPiece] ^= SquareBB[to];

            Colors[pc] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = promotionPiece;
        }

        [MethodImpl(Inline)]
        public void Promote(int from, int to, int thisColor, int capturedPiece, int promotionPiece)
        {
            Pieces[capturedPiece] ^= SquareBB[to];
            Colors[Not(thisColor)] ^= SquareBB[to];

            Pieces[Piece.Pawn] ^= SquareBB[from];
            Pieces[promotionPiece] ^= SquareBB[to];

            Colors[thisColor] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = promotionPiece;
        }

        /// <summary>
        /// Move the pawn that promoted by moving from <paramref name="originalSq"/> to it's promotion square on <paramref name="promotionSq"/> back to <paramref name="originalSq"/>
        /// </summary>
        [MethodImpl(Inline)]
        public void UnPromote(int originalSq, int promotionSq, int promotionPiece)
        {
            int pc = GetColorAtIndex(promotionSq);

            //  Put the pawn back on originalSq
            Pieces[Piece.Pawn] ^= SquareBB[originalSq];

            //  Remove the piece that it promoted to
            Pieces[promotionPiece] ^= SquareBB[promotionSq];

            //  Reset it's color
            Colors[pc] ^= (SquareBB[originalSq] | SquareBB[promotionSq]);

            //  And reset the type indices
            PieceTypes[promotionSq] = Piece.None;
            PieceTypes[originalSq] = Piece.Pawn;
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, capturing the piece of type <paramref name="capturedPieceType"/>.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="pieceType">The type of the piece that is moving</param>
        /// <param name="capturedPieceType">The type of the piece that is being captured</param>
        [MethodImpl(Inline)]
        public void Move(int from, int to, int pieceColor, int pieceType, int capturedPieceType)
        {
            if (capturedPieceType != Piece.None)
            {
#if DEBUG
                Debug.Assert(capturedPieceType != Piece.King, "Moving from " + IndexToString(from) + " to " + IndexToString(to) + " captures " + ColorToString(pieceColor) + "'s king!"
                    + "\r\nCalled by " + (new StackTrace()).GetFrame(1).GetMethod().Name);
#endif
                Pieces[capturedPieceType] ^= SquareBB[to];
                Colors[Not(pieceColor)] ^= SquareBB[to];
            }

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, capturing the piece of type <paramref name="capturedPieceType"/>.
        /// </summary>
        /// <param name="from">The square the piece originally came from</param>
        /// <param name="to">The square the piece is currently on</param>
        /// <param name="pieceColor">The color of the piece that moved</param>
        /// <param name="pieceType">The type of the piece that moved</param>
        /// <param name="capturedPieceType">The type of the piece that was captured</param>
        [MethodImpl(Inline)]
        public void UnmakeCapture(int from, int to, int pieceColor, int pieceType, int capturedPieceType)
        {
            if (capturedPieceType != Piece.None)
            {
                Pieces[capturedPieceType] ^= SquareBB[to];
                Colors[Not(pieceColor)] ^= SquareBB[to];
            }

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            PieceTypes[from] = pieceType;
            PieceTypes[to] = capturedPieceType;
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, where <paramref name="to"/> is an empty square.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="pieceType">The type of the piece that is moving</param>
        [MethodImpl(Inline)]
        public void MoveSimple(int from, int to, int pieceColor, int pieceType)
        {
            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            //Pieces[pieceType] ^= (SquareBB[from] | SquareBB[to]);
            //Colors[pieceColor] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Removes the piece at <paramref name="idx"/> by clearing that index in Pieces, Colors, and PieceTypes.
        /// </summary>
        [MethodImpl(Inline)]
        public void Clear(int idx)
        {
            int pt = PieceTypes[idx];
            if (pt != Piece.None)
            {
                Pieces[pt] ^= SquareBB[idx];
                Colors[Color.White] ^= SquareBB[idx];
                Colors[Color.Black] ^= SquareBB[idx];
                PieceTypes[idx] = Piece.None;
            }
        }

        /// <summary>
        /// Moves the pawn at <paramref name="from"/> to <paramref name="to"/>, and clears the index at <paramref name="idxEnPassant"/>.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="idxEnPassant">The index of the pawn that is being taken, which should be 1 square left/right of <paramref name="from"/></param>
        [MethodImpl(Inline)]
        public void EnPassant(int from, int to, int pieceColor, int idxEnPassant)
        {

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxEnPassant]);
            Colors[pieceColor] ^= (moveMask);

            Colors[Not(pieceColor)] ^= SquareBB[idxEnPassant];
            PieceTypes[from] = Piece.None;
            PieceTypes[idxEnPassant] = Piece.None;
            PieceTypes[to] = Piece.Pawn;
        }

        [MethodImpl(Inline)]
        public int GetColorAtIndex(int idx)
        {
            return ((Colors[Color.White] & SquareBB[idx]) != 0) ? Color.White : Color.Black;
        }

        [MethodImpl(Inline)]
        public int GetPieceAtIndex(int idx)
        {
            return PieceTypes[idx];
        }

        [MethodImpl(Inline)]
        public bool IsColorSet(int pc, int idx)
        {
            return (Colors[pc] & SquareBB[idx]) != 0;
        }

        [MethodImpl(Inline)]
        public ulong KingMask(int pc)
        {
            return (Colors[pc] & Pieces[Piece.King]);
        }

        [MethodImpl(Inline)]
        public int KingIndex(int pc)
        {
#if DEBUG
            ulong u = KingMask(pc);
            Debug.Assert(lsb(u) != 64);
#endif

            return lsb(KingMask(pc));
        }

        [MethodImpl(Inline)]
        public int MaterialCount(int pc)
        {
            int mat = 0;
            ulong temp = Colors[pc];
            while (temp != 0)
            {
                int idx = lsb(temp);

                mat += Evaluation.GetPieceValue(GetPieceAtIndex(idx));

                temp = poplsb(temp);
            }

            return mat;
        }

        [MethodImpl(Inline)]
        public bool IsPasser(int idx)
        {
            if (GetPieceAtIndex(idx) != Piece.Pawn)
            {
                return false;
            }

            //  TODO use WhitePassedPawnMasks
            int ourColor = GetColorAtIndex(idx);
            ulong them = Colors[Not(ourColor)];
            ulong theirPawns = (them & Pieces[Piece.Pawn]);


            if (ourColor == Color.White)
            {
                return ((WhitePassedPawnMasks[idx] & theirPawns) == 0);
            }
            else
            {
                return ((BlackPassedPawnMasks[idx] & theirPawns) == 0);
            }
        }

        [MethodImpl(Inline)]
        public ulong PinnedPieces(int pc)
        {
            ulong pinned = 0UL;
            ulong temp;
            ulong them = Colors[Not(pc)];

            int ourKing = KingIndex(pc);
            ulong pinners = ((RookRays[ourKing] & (Pieces[Piece.Rook] | Pieces[Piece.Queen])) | 
                           (BishopRays[ourKing] & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))) & them;
            
            while (pinners != 0)
            {
                int idx = lsb(pinners);
                pinners = poplsb(pinners);

                temp = BetweenBB[ourKing][idx] & (Colors[pc] | them);

                if (popcount(temp) == 1 && (temp & them) == 0)
                {
                    pinned |= temp;
                }
            }

            return pinned;
        }

        /// <summary>
        /// Returns a ulong with bits set at the positions of pieces that can attack <paramref name="idx"/>. 
        /// So for a bishop on A1, AttackersTo H8 returns a ulong with a bit set at A1.
        /// defendingColor is the color whose pieces are being attacked, and Not(defendingColor) is the color of the pieces that attack that square. 
        /// So bb.AttackersTo(..., White) will reference any attacking Black pieces.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackersTo(int idx, int defendingColor)
        {
            ulong us = Colors[defendingColor];
            ulong them = Colors[Not(defendingColor)];

            return AttackersToFast(idx, us | them) & them;
        }

        [MethodImpl(Inline)]
        public ulong AttackersToFast(int idx, ulong occupied)
        {
            return ((GetBishopMoves(occupied, idx) & (Pieces[Piece.Bishop] | Pieces[Piece.Queen])) 
                  | (GetRookMoves(occupied, idx) & (Pieces[Piece.Rook] | Pieces[Piece.Queen])) 
                  | (Pieces[Piece.Knight] & KnightMasks[idx])
                  | ((WhitePawnAttackMasks[idx] & Colors[Color.Black] & Pieces[Piece.Pawn])
                  |  (BlackPawnAttackMasks[idx] & Colors[Color.White] & Pieces[Piece.Pawn])));

            //return (diagonals | straights | knights | pawns);
        }

        /// <summary>
        /// Same as AttackersTo, but the bishop and rook moves are calculated after AND NOT'ing the mask from the Color bitboards.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackersToMask(int idx, int defendingColor, ulong mask)
        {
            /// TODO: this.
            ulong us = Colors[defendingColor];
            ulong them = Colors[Not(defendingColor)];

            //  pawnBB is set to our color's pawn attacks.
            //  We see if the piece at idx could capture another piece as if it were a pawn
            ulong pawnBB = (defendingColor == Color.White) ? WhitePawnAttackMasks[idx] : BlackPawnAttackMasks[idx];

            ulong occupied = (us | them) & ~mask;
                 
            ulong diagonals = (GetBishopMoves(occupied, idx) & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]));
            ulong straights = (GetRookMoves(occupied, idx) & (Pieces[Piece.Rook] | Pieces[Piece.Queen]));

            ulong knights = (Pieces[Piece.Knight] & KnightMasks[idx]);
            ulong pawns = (Pieces[Piece.Pawn] & pawnBB);

            return (diagonals | straights | knights | pawns) & them;
        }

        /// <summary>
        /// Returns the index of the square of the attacker of lowest value,
        /// which is a pawn, knight, bishop, rook, queen, or king in that order.
        /// </summary>
        /// <param name="idx">The square to look at</param>
        /// <param name="defendingColor">The color of the pieces BEING attacked.</param>
        [MethodImpl(Inline)]
        public int LowestValueAttacker(int idx, int defendingColor)
        {
            ulong us = Colors[defendingColor];
            ulong them = Colors[Not(defendingColor)];

            ulong pawns = ((defendingColor == Color.White) ? WhitePawnAttackMasks[idx] : BlackPawnAttackMasks[idx]) & Pieces[Piece.Pawn] & them;
            if (pawns != 0)
            {
                return lsb(pawns);
            }

            ulong knights = (Pieces[Piece.Knight] & KnightMasks[idx] & them);
            if (knights != 0)
            {
                return lsb(knights);
            }

            ulong occupied = (us | them);

            ulong diagSliders = GetBishopMoves(occupied, idx);
            if ((diagSliders & Pieces[Piece.Bishop] & them) != 0)
            {
                return lsb((diagSliders & Pieces[Piece.Bishop] & them));
            }

            ulong straightSliders = GetRookMoves(occupied, idx);
            if ((straightSliders & Pieces[Piece.Rook] & them) != 0)
            {
                return lsb((straightSliders & Pieces[Piece.Rook] & them));
            }

            if (((diagSliders | straightSliders) & Pieces[Piece.Queen] & them) != 0)
            {
                return lsb((diagSliders | straightSliders) & Pieces[Piece.Queen] & them);
            }

            return LSBEmpty;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is pseudo-legal.
        /// Only determines if there is a piece at move.from and the piece at move.to isn't the same color.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsPseudoLegal(in Move move)
        {
            if (GetPieceAtIndex(move.from) != Piece.None)
            {
                if (GetPieceAtIndex(move.to) != Piece.None)
                {
                    //  We can't capture our own color pieces
                    return GetColorAtIndex(move.from) != GetColorAtIndex(move.to);
                }

                //  This is a move to an empty square.
                return true;
            }

            //  There isn't a piece on the move's "from" square.
            return false;
        }

        /// <summary>
        /// Sets <paramref name="info"/> according to the number of pieces that attack the king of color <paramref name="ourColor"/>
        /// </summary>
        [MethodImpl(Inline)]
        public void DetermineCheck(int ourColor, ref CheckInfo info)
        {
            int ourKing = KingIndex(ourColor);

            ulong att = AttackersTo(ourKing, ourColor);
            switch (popcount(att))
            {
                case 0:
                    break;
                case 1:
                    info.InCheck = true;
                    info.idxChecker = lsb(att);
                    break;
                case 2:
                    info.InDoubleCheck = true;
                    info.idxChecker = lsb(att);
                    info.idxDoubleChecker = msb(att);
                    break;
            }
        }
    }
}
