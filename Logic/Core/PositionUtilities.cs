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

namespace LTChess.Util
{
    public static unsafe class PositionUtilities
    {
        /// <summary>
        /// The halfmove clock needs to be at least 8 before a draw by threefold repetition can occur.
        /// </summary>
        public const int LowestRepetitionCount = 8;

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the position <paramref name="position"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsLegal(Position position, in Bitboard bb, in Move move, int ourKing, int theirKing)
        {
            ulong pinned = bb.PinnedPieces(bb.GetColorAtIndex(ourKing));
            return IsLegal(position, bb, move, ourKing, theirKing, pinned);
        }

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the position <paramref name="position"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsLegal(Position position, in Bitboard bb, in Move move, int ourKing, int theirKing, ulong pinnedPieces)
        {
            int pt = bb.GetPieceAtIndex(move.from);
            if (position.CheckInfo.InDoubleCheck && pt != Piece.King)
            {
                //	Must move king out of double check
                return false;
            }

            int ourColor = bb.GetColorAtIndex(move.from);
            int theirColor = Not(ourColor);

            if (position.CheckInfo.InCheck)
            {
                //  We have 3 options: block the check, take the piece giving check, or move our king out of it.

                if (pt == Piece.King)
                {
                    //  Either move out or capture the piece
                    ulong moveMask = (SquareBB[move.from] | SquareBB[move.to]);
                    bb.Pieces[Piece.King] ^= moveMask;
                    bb.Colors[ourColor] ^= moveMask;
                    if ((bb.AttackersTo(move.to, ourColor) | (NeighborsMask[move.to] & SquareBB[theirKing])) != 0)
                    {
                        bb.Pieces[Piece.King] ^= moveMask;
                        bb.Colors[ourColor] ^= moveMask;
                        return false;
                    }

                    bb.Pieces[Piece.King] ^= moveMask;
                    bb.Colors[ourColor] ^= moveMask;
                    return true;
                }

                int checker = position.CheckInfo.idxChecker;
                bool blocksOrCaptures = (LineBB[ourKing][checker] & SquareBB[move.to]) != 0;

                if (blocksOrCaptures || (move.EnPassant && move.idxEnPassant == position.CheckInfo.idxChecker))
                {
                    //  This move is another piece which has moved into the LineBB between our king and the checking piece.
                    //  This will be legal as long as it isn't pinned.

                    return (pinnedPieces == 0 || (pinnedPieces & SquareBB[move.from]) == 0);
                }

                //  This isn't a king move and doesn't get us out of check, so it's illegal.
                return false;
            }

            if (pt == Piece.King)
            {
                //  We can move anywhere as long as it isn't attacked by them.

                //  AttackersToMask gives a bitboard of the squares that they attack,
                //  but it doesn't consider their king as an attacker in that sense.
                //  so also OR the squares surrounding their king as "attacked"
                if ((bb.AttackersToMask(move.to, ourColor, SquareBB[ourKing]) | (NeighborsMask[move.to] & SquareBB[theirKing])) != 0)
                {
                    return false;
                }
            }
            else if (move.EnPassant)
            {
                //  En passant will remove both our pawn and the opponents pawn from the rank so this needs a special check
                //  to make sure it is still legal
                ulong moveMask = (SquareBB[move.from] | SquareBB[move.to]);
                bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[move.idxEnPassant]);
                bb.Colors[ourColor] ^= moveMask;
                bb.Colors[theirColor] ^= (SquareBB[move.idxEnPassant]);

                if (bb.AttackersTo(ourKing, ourColor) != 0)
                {
                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[move.idxEnPassant]);
                    bb.Colors[ourColor] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[move.idxEnPassant]);
                    return false;
                }

                bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[move.idxEnPassant]);
                bb.Colors[ourColor] ^= moveMask;
                bb.Colors[theirColor] ^= (SquareBB[move.idxEnPassant]);
            }
            else if (IsPinned(bb, move.from, ourColor, ourKing, out int pinner))
            {
                //	If we are pinned, make sure we are only moving in directions that keep us pinned
                return ((LineBB[ourKing][pinner] & SquareBB[move.to]) != 0);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the piece at <paramref name="idx"/> is pinned to it's king
        /// </summary>
        /// <param name="pinner">The index of the piece that is pinning this one</param>
        [MethodImpl(Inline)]
        public static bool IsPinned(in Bitboard bb, int idx, int pc, int ourKing, out int pinner)
        {
            /// TODO: Optimize this since we don't care about every pinner, just pinners for idx.
            ulong temp;
            ulong them = bb.Colors[Not(pc)];

            //  Only rooks, bishops, and queens can pin pieces.
            ulong pinners = ((RookRays[ourKing] & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen])) |
                           (BishopRays[ourKing] & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen]))) & them;

            while (pinners != 0)
            {
                int maybePinner = lsb(pinners);
                pinners = poplsb(pinners);

                //  "The pieces between our king and one of their pieces"
                temp = BetweenBB[ourKing][maybePinner] & (bb.Colors[pc] | them);

                //  "If there is only 1 piece between our king and their piece, and that 1 piece is on the square 'idx'"
                if (popcount(temp) == 1 && lsb(temp) == idx)
                {
                    pinner = maybePinner;
                    return true;
                }
            }

            pinner = idx;
            return false;
        }

        /// <summary>
        /// Returns true if any pieces of color <paramref name="attackingColor"/> attack the square <paramref name="idx"/>, 
        /// and sets <paramref name="attackers"/> to be the attacker of the lowest index.
        /// </summary>
        public static bool IsSquareAttacked(Position position, int idx, int attackingColor, out int attackers)
        {
            Bitboard bb = position.bb;
            
            //  pawnBB is set to our friendlyColor attacks.
            //  We see if the piece at idx could capture another piece as if it were a pawn
            ulong pawnBB;
            ulong attackingPieces;
            if (attackingColor == Color.White)
            {
                attackingPieces = bb.Colors[Color.White];
                pawnBB = WhitePawnAttackMasks[idx];
            }
            else
            {
                attackingPieces = bb.Colors[Color.Black];
                pawnBB = BlackPawnAttackMasks[idx];
            }

            attackers = lsb(bb.Pieces[Piece.Pawn] & attackingPieces & pawnBB);
            if (attackers != LSBEmpty)
            {
                return true;
            }

            attackers = lsb(bb.Pieces[Piece.Knight] & attackingPieces & KnightMasks[idx]);
            if (attackers != LSBEmpty)
            {
                return true;
            }

            attackers = lsb(GenPseudoSlidersMask(position.bb, idx, Piece.Bishop, Not(attackingColor)) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen]) & attackingPieces);
            if (attackers != LSBEmpty)
            {
                return true;
            }

            attackers = lsb(GenPseudoSlidersMask(position.bb, idx, Piece.Rook, Not(attackingColor)) & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]) & attackingPieces);
            if (attackers != LSBEmpty)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if in the current position a piece of type <paramref name="pt"/> at index <paramref name="idx"/> 
        /// would attack the index of <paramref name="otherSquare"/>
        /// </summary>
        public static bool WouldPieceAttack(Position position, int idx, int pt, int pieceColor, int otherSquare)
        {
            ulong otherSqMask = SquareBB[otherSquare];

            switch (pt)
            {
                case Piece.Pawn:
                    ulong pawnBB = (pieceColor == Color.White) ? WhitePawnAttackMasks[idx] : BlackPawnAttackMasks[idx];
                    return (pawnBB & otherSqMask) != 0;
                case Piece.Knight:
                    return (PrecomputedData.KnightMasks[idx] & otherSqMask) != 0;
                case Piece.Bishop:
                    return (GenPseudoSlidersMask(position.bb, idx, Piece.Bishop, pieceColor) & otherSqMask) != 0;
                case Piece.Rook:
                    return (GenPseudoSlidersMask(position.bb, idx, Piece.Rook, pieceColor) & otherSqMask) != 0;
                case Piece.Queen:
                    ulong diag = GenPseudoSlidersMask(position.bb, idx, Piece.Bishop, pieceColor);
                    ulong straight = GenPseudoSlidersMask(position.bb, idx, Piece.Rook, pieceColor);
                    return ((diag | straight) & otherSqMask) != 0;
                case Piece.King:
                    return (PrecomputedData.NeighborsMask[idx] & otherSqMask) != 0;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the piece at <paramref name="idx"/> is currently blocking a check,
        /// and would cause a discovery if it moves
        /// </summary>
        public static bool IsXrayed(Position position, int idx, out int pinner)
        {
            Bitboard bb = position.bb;
            int ourColor = bb.GetColorAtIndex(idx);
            int pt = bb.GetPieceAtIndex(idx);
            int theirKing = bb.KingIndex(Not(ourColor));
            pinner = 64;

            ref ulong bbRef = ref bb.Pieces[pt];
            bbRef ^= SquareBB[idx];
            if (ourColor == Color.White)
            {
                bb.Colors[Color.White] ^= SquareBB[idx];
            }
            else
            {
                bb.Colors[Color.Black] ^= SquareBB[idx];
            }

            
            ulong all = bb.AttackersTo(theirKing, Not(ourColor));

            bbRef ^= SquareBB[idx];
            if (ourColor == Color.White)
            {
                bb.Colors[Color.White] ^= SquareBB[idx];
            }
            else
            {
                bb.Colors[Color.Black] ^= SquareBB[idx];
            }

            int first = lsb(all);
            if (first != 64)
            {
                pinner = first;
                return (LineBB[theirKing][first] & all) != 0;
            }

            return false;
        }

        /// <summary>
        /// Returns a bitboard with bits set at the indices of pieces of color <paramref name="ourColor"/> that support their piece at <paramref name="idx"/>
        /// </summary>
        public static bool IsHanging(in Bitboard bb, int idx, int ourColor)
        {
            ulong us;
            ulong them;
            if (ourColor == Color.White)
            {
                us = bb.Colors[Color.White];
                them = bb.Colors[Color.Black];
            }
            else
            {
                us = bb.Colors[Color.Black];
                them = bb.Colors[Color.White];
            }

            ulong diagonals = Magic.MagicBitboards.GetBishopMoves(us | them, idx);
            if ((diagonals & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen])) != 0)
            {
                return false;
            }

            ulong straights = Magic.MagicBitboards.GetRookMoves(us | them, idx);
            if ((straights & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen])) != 0)
            {
                return false;
            }

            if ((bb.Pieces[Piece.Knight] & us & KnightMasks[idx]) != 0)
            {
                return false;
            }

            ulong[] pawnBB = (ourColor == Color.White ? BlackPawnAttackMasks : WhitePawnAttackMasks);
            if ((bb.Pieces[Piece.Pawn] & us & pawnBB[idx]) != 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a bitboard with bits set at the indices of pieces that support their piece at <paramref name="idx"/>.
        /// This doesn't consider "xray defense" where a king was in between a rook and another piece and is forced to move away.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong DefendersOf(in Bitboard bb, int idx)
        {
            ulong[] pawnBB;
            int ourColor = bb.GetColorAtIndex(idx);
            ulong us = bb.Colors[ourColor];
            ulong them = bb.Colors[Not(ourColor)];
            if (ourColor == Color.White)
            {
                pawnBB = BlackPawnAttackMasks;
            }
            else
            {
                pawnBB = WhitePawnAttackMasks;
            }

            ulong ourDiags = (GetBishopMoves(us | them, idx) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen]));
            ulong ourStraights = (GetRookMoves(us | them, idx) & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]));

            ulong ourKnightAttacks = (bb.Pieces[Piece.Knight] & KnightMasks[idx]);
            ulong ourPawnAttacks = (bb.Pieces[Piece.Pawn] & pawnBB[idx]);
            ulong ourKingDefender = (SquareBB[bb.KingIndex(ourColor)] & NeighborsMask[idx]);
            ulong defenders = (ourDiags | ourStraights | ourKnightAttacks | ourPawnAttacks | ourKingDefender) & us;
            
            return defenders;
        }

        /// <summary>
        /// Returns a bitboard with bits set at every square that the pieces of color <paramref name="color"/> attack.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetAttackedSquaresColorMask(in Bitboard bb, int color)
        {
            ulong mask = 0;

            ulong b = bb.Colors[color];
            ulong all = b | bb.Colors[Not(color)];

            while (b != 0)
            {
                int idx = lsb(b);
                mask |= GetAttackedSquaresMask(bb, idx, color, all);
                b = poplsb(b);
            }

            return mask;
        }

        /// <summary>
        /// Returns a bitboard with bits set at every square that the piece of color <paramref name="ourColor"/> on <paramref name="idx"/> attacks.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetAttackedSquaresMask(in Bitboard bb, int idx, int ourColor, ulong all)
        {
            ulong mask = 0;
            int pt = bb.GetPieceAtIndex(idx);

            switch (pt)
            {
                case Piece.Pawn:
                    {
                        //GenPseudoPawnMask(position, idx, out ulong att, out _);
                        mask |= (ourColor == Color.White) ? WhitePawnAttackMasks[idx] : BlackPawnAttackMasks[idx];
                        break;
                    }

                case Piece.Knight:
                    mask |= KnightMasks[idx];
                    break;
                case Piece.Bishop:
                    {
                        mask |= GetBishopMoves(all, idx);
                        break;
                    }

                case Piece.Rook:
                    {
                        mask |= GetRookMoves(all, idx);
                        break;
                    }

                case Piece.Queen:
                    {
                        mask |= GetBishopMoves(all, idx);
                        mask |= GetRookMoves(all, idx);
                        break;
                    }

                case Piece.King:
                    mask |= NeighborsMask[idx];
                    break;
            }

            return mask;
        }

        public static void PrintMoveTree(Position p, int maxDepth, int curDepth = 1)
        {
            int thisDepth = maxDepth - curDepth + 1;
            List<PerftNode> initList = p.PerftDivide(thisDepth);
            foreach (PerftNode initMove in initList)
            {
                for (int i = 1; i < curDepth; i++)
                {
                    Console.Write("\t");
                    Debug.Write("\t");
                }
                Log(initMove.root + ": " + initMove.number);

                if (curDepth < maxDepth && p.TryMakeMove(initMove.root))
                {
                    PrintMoveTree(p, maxDepth, curDepth + 1);
                    p.UnmakeMove();
                }
            }
        }

        public static string FormatMoves(Position p)
        {
            StringBuilder sb = new StringBuilder();
            FasterStack<Move> tempStack = new FasterStack<Move>();
            while (p.Moves.Count > 0)
            {
                Move m = p.Moves.Peek();
                tempStack.Push(m);
                p.UnmakeMove();
            }

            while (tempStack.Count > 0)
            {
                Move m = tempStack.Pop();
                sb.Append(m.ToString(p) + ", ");
                p.MakeMove(m);
            }

            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

    }
}
