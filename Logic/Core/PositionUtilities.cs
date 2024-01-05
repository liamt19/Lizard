namespace Lizard.Logic.Util
{
    public static unsafe class PositionUtilities
    {

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
                int maybePinner = poplsb(&pinners);

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
        /// Returns a bitboard with bits set at the indices of pieces that support their piece at <paramref name="idx"/>.
        /// This doesn't consider "xray defense" where a king was in between a rook and another piece and is forced to move away.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong DefendersOf(in Bitboard bb, int idx)
        {
            int ourColor = bb.GetColorAtIndex(idx);
            ulong us = bb.Colors[ourColor];
            ulong them = bb.Colors[Not(ourColor)];

            var pawnBB = (ourColor == Color.White) ? BlackPawnAttackMasks : WhitePawnAttackMasks;

            ulong ourDiags = GetBishopMoves(us | them, idx) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen]);
            ulong ourStraights = GetRookMoves(us | them, idx) & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]);

            ulong ourKnightAttacks = bb.Pieces[Piece.Knight] & KnightMasks[idx];
            ulong ourPawnAttacks = bb.Pieces[Piece.Pawn] & pawnBB[idx];
            ulong ourKingDefender = SquareBB[bb.KingIndex(ourColor)] & NeighborsMask[idx];
            ulong defenders = (ourDiags | ourStraights | ourKnightAttacks | ourPawnAttacks | ourKingDefender) & us;

            return defenders;
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

        [MethodImpl(Inline)]
        public static int DistanceFromPromotion(int idx, int color)
        {
            if (color == Color.White)
            {
                return 7 - GetIndexRank(idx);
            }
            else
            {
                return GetIndexRank(idx);
            }
        }
    }
}
