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

using static LTChess.Magic.MagicBitboards;
using LTChess.Data;


namespace LTChess.Core
{
    /// <summary>
    /// Class to generate moves
    /// </summary>
    public static unsafe class MoveGenerator
    {


        /// <summary>
        /// Generates the castling moves for the king on E1 or E8 
        /// by checking if the squares the king will pass through aren't under attack
        /// and the squares between the king and its rooks are empty.
        /// </summary>
        /// <returns>The number of castling moves generated, at most 2.</returns>
        [MethodImpl(Inline)]
        public static int GenCastlingMoves(in Bitboard bb, int idx, in ulong us, CastlingStatus Castling, in Span<Move> ml, int size)
        {
            if (idx == E1)
            {
                ulong all = (bb.Colors[Color.White] | bb.Colors[Color.Black]);

                if (Castling.HasFlag(CastlingStatus.WK))
                {
                    if ((all & WhiteKingsideMask) == 0 && (bb.AttackersToFast(F1, all) & bb.Colors[Color.Black]) == 0 && (bb.AttackersToFast(G1, all) & bb.Colors[Color.Black]) == 0)
                    {
                        if ((bb.Pieces[Piece.Rook] & SquareBB[H1] & us) != 0)
                        {
                            Move m = new Move(E1, G1);
                            m.Castle = true;
                            ml[size++] = m;
                        }
                    }
                }
                if (Castling.HasFlag(CastlingStatus.WQ))
                {
                    //  B1 empty, C1+D1 are empty and not attacked
                    if ((all & WhiteQueensideMask) == 0 && (bb.AttackersToFast(C1, all) & bb.Colors[Color.Black]) == 0 && (bb.AttackersToFast(D1, all) & bb.Colors[Color.Black]) == 0)
                    {
                        if ((bb.Pieces[Piece.Rook] & SquareBB[A1] & us) != 0)
                        {
                            Move m = new Move(E1, C1);
                            m.Castle = true;
                            ml[size++] = m;
                        }
                    }
                }
            }
            else if (idx == E8)
            {
                ulong all = (bb.Colors[Color.White] | bb.Colors[Color.Black]);

                if (Castling.HasFlag(CastlingStatus.BK))
                {
                    if ((all & BlackKingsideMask) == 0 && (bb.AttackersToFast(F8, all) & bb.Colors[Color.White]) == 0 && (bb.AttackersToFast(G8, all) & bb.Colors[Color.White]) == 0)
                    {
                        if ((bb.Pieces[Piece.Rook] & SquareBB[H8] & us) != 0)
                        {
                            Move m = new Move(E8, G8);
                            m.Castle = true;
                            ml[size++] = m;
                        }
                    }
                }
                if (Castling.HasFlag(CastlingStatus.BQ))
                {
                    if ((all & BlackQueensideMask) == 0 && (bb.AttackersToFast(C8, all) & bb.Colors[Color.White]) == 0 && (bb.AttackersToFast(D8, all) & bb.Colors[Color.White]) == 0)
                    {
                        if ((bb.Pieces[Piece.Rook] & SquareBB[A8] & us) != 0)
                        {
                            Move m = new Move(E8, C8);
                            m.Castle = true;
                            ml[size++] = m;
                        }
                    }
                }
            }

            return size;
        }

        /// <summary>
        /// Returns a ulong with bits set where the slider piece at <paramref name="idx"/> can move to. 
        /// <paramref name="support"/> includes every square in that ulong as well as any squares with the same color pieces.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GenPseudoSlidersMask(in Bitboard bb, int idx, int pt, int ourColor)
        {
            return GenPseudoSlidersMask(bb, idx, pt, ((ourColor == Color.White) ? bb.Colors[Color.White] : bb.Colors[Color.Black]), out _);
        }

        /// <summary>
        /// Returns a ulong with bits set where the slider piece at <paramref name="idx"/> can move to. 
        /// <paramref name="support"/> includes every square in that ulong as well as any squares with the same color pieces.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GenPseudoSlidersMask(in Bitboard bb, int idx, int pt, ulong us, out ulong support)
        {
            if (pt == Piece.Rook)
            {
                support = GetRookMoves((bb.Colors[Color.White] | bb.Colors[Color.Black]), idx);
            }
            else
            {
                support = GetBishopMoves((bb.Colors[Color.White] | bb.Colors[Color.Black]), idx);
            }
            return support & ~us;
        }


        /// <summary>
        /// Returns true and sets <paramref name="move"/> if an en passant move was generated.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public static bool GenEnPassantMove(in Bitboard bb, int idx, int EnPassantTarget, out Move move)
        {
            move = Move.Null;

            IndexToCoord(idx, out int x, out int y);

            //  Only consider pawns on the 3rd and 4th rank
            if (y != 3 && y != 4)
            {
                return false;
            }

            int us = bb.GetColorAtIndex(idx);

            int idxPawn = 64;
            if ((bb.Pieces[Piece.Pawn] & SquareBB[EnPassantTarget - 8]) != 0)
            {
                idxPawn = EnPassantTarget - 8;
            }
            else
            {
#if DEBUG
                Debug.Assert((bb.Pieces[Piece.Pawn] & SquareBB[EnPassantTarget + 8]) != 0, 
                    "EnPassantTarget is " + IndexToString(EnPassantTarget) + " but no pawns are set on " + 
                    IndexToString(EnPassantTarget - 8) + " or " + IndexToString(EnPassantTarget + 8));
#endif

                idxPawn = EnPassantTarget + 8;
            }

#if DEBUG
            //Debug.Assert(bb.GetColorAtIndex(idxPawn) != us);
            if (bb.GetColorAtIndex(idxPawn) == us)
            {
                //Log("WARN pawn being en passant'ed -> " + bb.SquareToString(idxPawn) + " is the same color as the one we are generating -> " + bb.SquareToString(idx));
            }
#endif

            if ((idxPawn % 8 == (x - 1) && idxPawn / 8 == y) && (x > Files.A))
            {
                int y1 = (us == Color.White) ? (y + 1) : (y - 1);
                int cl = CoordToIndex(x - 1, y1);
                move = new Move(idx, cl);
                move.EnPassant = true;
                move.idxEnPassant = idxPawn;
                return true;
            }

            if ((idxPawn % 8 == (x + 1) && idxPawn / 8 == y) && (x < Files.H))
            {
                int y1 = (us == Color.White) ? (y + 1) : (y - 1);
                int cr = CoordToIndex(x + 1, y1);
                move = new Move(idx, cr);
                move.EnPassant = true;
                move.idxEnPassant = idxPawn;
                return true;
            }

            return false;
        }

    }
}
