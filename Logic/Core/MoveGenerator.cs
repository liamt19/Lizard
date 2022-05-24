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


namespace LTChess.Core
{
    /// <summary>
    /// Class to generate moves
    /// </summary>
    public static unsafe class MoveGenerator
    {
        /// <summary>
        /// Generates all legal moves in the provided <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to generate for</param>
        /// <param name="legal">A Span of Move with sufficient size for the number of legal moves.</param>
        /// <returns>The number of legal moves generated and inserted into <paramref name="legal"/></returns>
        [MethodImpl(Optimize)]
        public static int GenAllLegalMoves(Position position, in Span<Move> legal)
        {
            Span<Move> pseudo = stackalloc Move[NORMAL_CAPACITY];
            int size = 0;
            Bitboard bb = position.bb;

            ulong them = bb.Colors[Not(position.ToMove)];
            ulong us = bb.Colors[position.ToMove];
            ulong usCopy = us;

            int theirKing = bb.KingIndex(Not(position.ToMove));
            int ourKing = bb.KingIndex(position.ToMove);

            Move move;
            while (us != 0)
            {
                int thisMovesCount = GenPseudoMoves(position, bb, lsb(us), usCopy, them, theirKing, pseudo, 0);
                for (int i = 0; i < thisMovesCount; i++)
                {
                    move = pseudo[i];
                    if (PositionUtilities.IsLegal(position, bb, move, ourKing, theirKing))
                    {
                        legal[size] = move;
                        size++;
                    }
                }

                us = poplsb(us);
            }

            return size;
        }

        /// <summary>
        /// Generates the pseudo legal moves for the piece at index <paramref name="idx"/>.
        /// </summary>
        /// <param name="position">The position to generate for</param>
        /// <param name="bb">The <paramref name="position"/>'s Bitboard</param>
        /// <param name="idx">The index of the piece</param>
        /// <param name="us">This piece's Color bitboard from the <paramref name="position"/></param>
        /// <param name="ml">A Span of Move with sufficient size</param>
        /// <param name="size">The number of moves already generated in <paramref name="ml"/>, which should be 0 if this is being called for the first time.</param>
        /// <returns>The new value of <paramref name="size"/>, which was incremented for every move placed into <paramref name="ml"/></returns>
        [MethodImpl(Inline)]
        private static int GenPseudoMoves(Position position, in Bitboard bb, int idx, ulong us, ulong them, int theirKing, in Span<Move> ml, int size)
        {
            int ourColor = position.ToMove;
            int theirColor = Not(ourColor);
            int pt = bb.GetPieceAtIndex(idx);

            ulong all = us | them;

            ulong moves = 0;

            switch (pt)
            {
                case Piece.Pawn:
                    moves = GenPseudoPawnMask(bb, idx, out _, out ulong Promotions);
                    //  Make promotion moves
                    while (Promotions != 0)
                    {
                        int promotionSquare = lsb(Promotions);
                        Promotions = poplsb(Promotions);

                        for (int promotionPiece = Piece.Knight; promotionPiece <= Piece.Queen; promotionPiece++)
                        {
                            Move m = new Move(idx, promotionSquare, promotionPiece);

                            int cap = bb.GetPieceAtIndex(promotionSquare);
                            if (cap != Piece.None)
                            {
                                bb.Pieces[cap] ^= SquareBB[promotionSquare];
                                bb.Colors[theirColor] ^= SquareBB[promotionSquare];
                                m.Capture = true;
                            }
                            bb.Pieces[Piece.Pawn] ^= SquareBB[idx];
                            bb.Pieces[promotionPiece] ^= SquareBB[promotionSquare];
                            bb.Colors[ourColor] ^= (SquareBB[idx] | SquareBB[promotionSquare]);

                            ulong attacks = AttackersTo(bb, theirKing, theirColor);
                            switch (popcount(attacks))
                            {
                                case 0:
                                    break;
                                case 1:
                                    m.CausesCheck = true;
                                    m.idxChecker = lsb(attacks);
                                    break;
                                case 2:
                                    m.CausesDoubleCheck = true;
                                    m.idxChecker = lsb(attacks);
                                    m.idxDoubleChecker = msb(attacks);
                                    break;
                            }

                            if (cap != Piece.None)
                            {
                                bb.Pieces[cap] ^= SquareBB[promotionSquare];
                                bb.Colors[theirColor] ^= SquareBB[promotionSquare];
                            }
                            bb.Pieces[Piece.Pawn] ^= SquareBB[idx];
                            bb.Pieces[promotionPiece] ^= SquareBB[promotionSquare];
                            bb.Colors[ourColor] ^= (SquareBB[idx] | SquareBB[promotionSquare]);

                            ml[size++] = m;
                        }
                    }
                    if (position.EnPassantTarget != 0 && GenEnPassantMove(bb, idx, position.EnPassantTarget, out Move EnPassant))
                    {
                        ulong moveMask = SquareBB[EnPassant.from] | SquareBB[EnPassant.to];
                        bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[EnPassant.idxEnPassant]);
                        bb.Colors[ourColor] ^= moveMask;
                        bb.Colors[theirColor] ^= (SquareBB[EnPassant.idxEnPassant]);

                        ulong attacks = AttackersTo(bb, theirKing, theirColor);

                        if (attacks != 0)
                        {
                            EnPassant.CausesCheck = true;
                            EnPassant.idxChecker = lsb(attacks);
                        }

                        bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[EnPassant.idxEnPassant]);
                        bb.Colors[ourColor] ^= moveMask;
                        bb.Colors[theirColor] ^= (SquareBB[EnPassant.idxEnPassant]);

                        ml[size++] = EnPassant;
                    }
                    break;
                case Piece.Knight:
                    //moves = GenPseudoKnightMask(position, idx);
                    moves = (PrecomputedData.KnightMasks[idx] & ~us);
                    break;
                case Piece.Bishop:
                    moves = GetBishopMoves(all, idx) & ~us;
                    break;
                case Piece.Rook:
                    moves = GetRookMoves(all, idx) & ~us;
                    break;
                case Piece.Queen:
                    moves = (GetBishopMoves(all, idx) | GetRookMoves(all, idx)) & ~us;
                    break;
                case Piece.King:
                    moves = (PrecomputedData.NeighborsMask[idx] & ~us);
                    if (!position.CheckInfo.InCheck && !position.CheckInfo.InDoubleCheck && GetIndexFile(idx) == Files.E)
                    {
                        int ourKing = bb.KingIndex(ourColor);
                        Span<Move> CastleMoves = stackalloc Move[2];
                        int numCastleMoves = GenCastlingMoves(bb, idx, us, position.Castling, CastleMoves, 0);
                        for (int i = 0; i < numCastleMoves; i++)
                        {
                            //  See if this will cause check.
                            Move m = CastleMoves[i];
                            int rookTo = m.to switch
                            {
                                C1 => D1,
                                G1 => F1,
                                C8 => D8,
                                G8 => F8,
                            };
                            ulong between = BetweenBB[rookTo][theirKing];
                            if (between != 0 && ((between & (all ^ SquareBB[ourKing])) == 0))
                            {
                                //  Then their king is on the same rank/file/diagonal as the square that our rook will end up at,
                                //  and there are no pieces which are blocking that ray.
                                if (GetIndexFile(rookTo) == GetIndexFile(theirKing) || GetIndexRank(rookTo) == GetIndexRank(theirKing))
                                {
                                    m.CausesCheck = true;
                                    m.idxChecker = rookTo;
                                }
                            }

                            ml[size++] = m;
                        }
                    }
                    break;
            }

            while (moves != 0)
            {
                //  moves = a ulong with bits set wherever the piece can move to.
                int to = lsb(moves);
                moves = poplsb(moves);

                Move m = new Move(idx, to);

                ulong moveMask = (SquareBB[idx] | SquareBB[to]);
                int capturedPiece = bb.GetPieceAtIndex(to);
                if (capturedPiece != Piece.None)
                {
                    bb.Pieces[capturedPiece] ^= SquareBB[to];
                    bb.Colors[theirColor] ^= SquareBB[to];
                    m.Capture = true;
                }

                bb.Pieces[pt] ^= moveMask;
                bb.Colors[ourColor] ^= moveMask;

                ulong att = AttackersTo(bb, theirKing, theirColor);
                switch (popcount(att))
                {
                    case 0:
                        break;
                    case 1:
                        m.CausesCheck = true;
                        m.idxChecker = lsb(att);
                        break;
                    case 2:
                        m.CausesDoubleCheck = true;
                        m.idxChecker = lsb(att);
                        m.idxDoubleChecker = msb(att);
                        break;
                }

                if (capturedPiece != Piece.None)
                {
                    bb.Pieces[capturedPiece] ^= SquareBB[to];
                    bb.Colors[theirColor] ^= SquareBB[to];
                }

                bb.Pieces[pt] ^= moveMask;
                bb.Colors[ourColor] ^= moveMask;

                ml[size++] = m;
            }

            return size;
        }

        [MethodImpl(Inline)]
        public static int GenCastlingMoves(in Bitboard bb, int idx, in ulong us, CastlingStatus Castling, in Span<Move> ml, int size)
        {
            if (idx == E1)
            {

                ulong all = (bb.Colors[Color.White] | bb.Colors[Color.Black]);

                if (Castling.HasFlag(CastlingStatus.WK))
                {
                    if ((all & WhiteKingsideMask) == 0 && AttackersTo(bb, F1, Color.White) == 0 && AttackersTo(bb, G1, Color.White) == 0)
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
                    if ((all & WhiteQueensideMask) == 0 && AttackersTo(bb, C1, Color.White) == 0 && AttackersTo(bb, D1, Color.White) == 0)
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
                    if ((all & BlackKingsideMask) == 0 && AttackersTo(bb, F8, Color.Black) == 0 && AttackersTo(bb, G8, Color.Black) == 0)
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
                    if ((all & BlackQueensideMask) == 0 && AttackersTo(bb, C8, Color.Black) == 0 && AttackersTo(bb, D8, Color.Black) == 0)
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

        [MethodImpl(Inline)]
        public static ulong GenPseudoKnightMask(int idx, ulong us)
        {
            return PrecomputedData.KnightMasks[idx] & ~us;
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

        [MethodImpl(Optimize)]
        public static ulong GenPseudoPawnMask(in Bitboard bb, int idx, out ulong attacks, out ulong PromotionSquares)
        {
            int ourColor = bb.GetColorAtIndex(idx);
            ulong them = bb.Colors[Not(ourColor)];
            ulong moves;

            if (ourColor == Color.White)
            {
                attacks = WhitePawnAttackMasks[idx];
                moves = WhitePawnMoveMasks[idx];
            }
            else
            {
                attacks = BlackPawnAttackMasks[idx];
                moves = BlackPawnMoveMasks[idx];
            }

            //  This pawn can also move 2 squares
            if (popcount(moves) == 2)
            {
                int off = (ourColor == Color.White) ? 8 : -8;
                if (bb.Occupied(idx + off))
                {
                    //  Then a pawn is blocking this pawn from moving forward.
                    //  This also stops a pawn stuck on the 2nd/6th rank from
                    //  "jumping" over whatever is in it's way.
                    moves = 0;
                }
            }

            //  moves includes only empty spaces and any of their pieces that we could capture
            moves &= ~(bb.Colors[Color.White] | bb.Colors[Color.Black]);
            moves |= (them & attacks);

            //  Promotions are special cases, so take them out
            PromotionSquares = (moves & PawnPromotionSquares);
            moves &= ~PawnPromotionSquares;

            return moves;
        }

        /// <summary>
        /// Returns true and sets <paramref name="move"/> if an en passant move was generated.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(Optimize)]
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
            Debug.Assert(bb.GetColorAtIndex(idxPawn) != us);
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

        public static int GenAllPseudoMoves(Position position, in Span<Move> pseudo)
        {
            Span<Move> tempList = stackalloc Move[NORMAL_CAPACITY];
            int size = 0;
            Bitboard bb = position.bb;

            ulong them = bb.Colors[Not(position.ToMove)];
            ulong us = bb.Colors[position.ToMove];
            ulong usCopy = us;

            int theirKing = bb.KingIndex(Not(position.ToMove));

            while (us != 0)
            {
                int pieceIdx = lsb(us);
                us = poplsb(us);

                int thisMovesCount = GenPseudoMoves(position, bb, pieceIdx, usCopy, them, theirKing, tempList, 0);
                for (int i = 0; i < thisMovesCount; i++)
                {
                    pseudo[size++] = tempList[i];
                }
            }

            return size;
        }
    }
}
