
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
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, capturing the piece on <paramref name="to"/> if there is one.
        /// </summary>
        [MethodImpl(Inline)]
        public void Move(int from, int to)
        {
            int pieceType = GetPieceAtIndex(from);
            int pieceColor = GetColorAtIndex(from);

            int capturedPiece = PieceTypes[to];
            if (capturedPiece != Piece.None)
            {
#if DEBUG
                Debug.Assert(capturedPiece != Piece.King);
#endif
                Pieces[capturedPiece] ^= SquareBB[to];
                Colors[Not(pieceColor)] ^= SquareBB[to];
            }

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, capturing the piece of type <paramref name="capturedPiece"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void Move(int from, int to, int pieceColor, int pieceType, int capturedPiece)
        {
            if (capturedPiece != Piece.None)
            {
#if DEBUG
                Debug.Assert(capturedPiece != Piece.King, "Moving from " + IndexToString(from) + " to " + IndexToString(to) + " captures " + ColorToString(pieceColor) + "'s king!"
                    + "\r\nCalled by " + (new StackTrace()).GetFrame(1).GetMethod().Name);
#endif
                Pieces[capturedPiece] ^= SquareBB[to];
                Colors[Not(pieceColor)] ^= SquareBB[to];
            }

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            //Pieces[pieceType] ^= (SquareBB[from] | SquareBB[to]);
            //Colors[pieceColor] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, where <paramref name="to"/> is an empty square.
        /// </summary>
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
            if ((Colors[Color.White] & SquareBB[idx]) != 0)
            {
                return Color.White;
            }
#if DEBUG
            Debug.Assert((Colors[Color.Black] & SquareBB[idx]) != 0, "GetPieceColorAtIndex(" + IndexToString(idx) + ") is failing because nothing is set at that index!");
#endif
            return Color.Black;
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
        public int KingIndex(int pc)
        {
#if DEBUG
            ulong u = Colors[pc] & Pieces[Piece.King];
            Debug.Assert(lsb(u) != 64);
#endif

            return lsb(Colors[pc] & Pieces[Piece.King]);
        }

    }
}
