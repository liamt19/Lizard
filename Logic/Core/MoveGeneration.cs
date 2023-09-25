using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Data;

namespace LTChess.Logic.Core
{
    public unsafe partial class Position
    {

        /// <summary>
        /// Generates all legal moves in the current position.
        /// </summary>
        /// <param name="legal">A Span of Move with sufficient size for the number of legal moves.</param>
        /// <returns>The number of legal moves generated and inserted into <paramref name="legal"/></returns>
        [MethodImpl(Inline)]
        public int GenAllLegalMovesTogether(in Span<Move> legal, bool onlyCaptures = false)
        {
            Span<Move> pseudo = stackalloc Move[NormalListCapacity];
            int size = 0;

            ulong pinned = State->BlockingPieces[ToMove];

            ulong us = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];

            Move move;
            int thisMovesCount = 0;

            if (!CheckInfo.InDoubleCheck)
            {
                thisMovesCount = GenAllPawnMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllKnightMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllBishopMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllRookMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllQueenMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
            }

            thisMovesCount = GenAllKingMoves(us, them, pseudo, thisMovesCount, onlyCaptures);

            for (int i = 0; i < thisMovesCount; i++)
            {
                move = pseudo[i];
                if (IsLegal(move, bb.KingIndex(ToMove), bb.KingIndex(Not(ToMove)), pinned))
                {
                    legal[size++] = move;
                }
            }

            return size;
        }


        /// <summary>
        /// Generates all pseudo-legal moves in the current position.
        /// </summary>
        /// <param name="pseudo">A Span of Move with sufficient size for the number of pseudo-legal moves.</param>
        /// <returns>The number of pseudo-legal moves generated and inserted into <paramref name="pseudo"/></returns>
        [MethodImpl(Inline)]
        public int GenAllPseudoLegalMovesTogether(in Span<Move> pseudo, bool onlyCaptures = false)
        {
            ulong us = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];

            int thisMovesCount = 0;
            if (!CheckInfo.InDoubleCheck)
            {
                thisMovesCount = GenAllPawnMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllKnightMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllBishopMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllRookMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllQueenMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
            }

            thisMovesCount = GenAllKingMoves(us, them, pseudo, thisMovesCount, onlyCaptures);
            return thisMovesCount;
        }



        /// <summary>
        /// Generates all pseudo-legal pawn moves available to the side to move in the position, including en passant moves.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllPawnMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong rank7 = (ToMove == Color.White) ? Rank7BB : Rank2BB;
            ulong rank3 = (ToMove == Color.White) ? Rank3BB : Rank6BB;

            int up = ShiftUpDir(ToMove);

            int theirColor = Not(ToMove);

            ulong occupiedSquares = them | us;
            ulong emptySquares = ~occupiedSquares;

            ulong ourPawns = us & bb.Pieces[Piece.Pawn];
            ulong promotingPawns = ourPawns & rank7;
            ulong notPromotingPawns = ourPawns & ~rank7;

            ulong moves = Forward(ToMove, notPromotingPawns) & emptySquares;
            ulong twoMoves = Forward(ToMove, moves & rank3) & emptySquares;
            ulong promotions = Forward(ToMove, promotingPawns) & emptySquares;

            ulong capturesL = Shift(up + Direction.WEST, notPromotingPawns) & them;
            ulong capturesR = Shift(up + Direction.EAST, notPromotingPawns) & them;
            ulong promotionCapturesL = Shift(up + Direction.WEST, promotingPawns) & them;
            ulong promotionCapturesR = Shift(up + Direction.EAST, promotingPawns) & them;

            int theirKing = bb.KingIndex(theirColor);

            if (!onlyCaptures)
            {
                while (moves != 0)
                {
                    int to = poplsb(&moves);

                    Move m = new Move(to - up, to);
                    MakeCheck(Piece.Pawn, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }

                while (twoMoves != 0)
                {
                    int to = poplsb(&twoMoves);

                    Move m = new Move(to - up - up, to);
                    MakeCheck(Piece.Pawn, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }

                while (promotions != 0)
                {
                    int to = poplsb(&promotions);

                    size = MakePromotionChecks(to - up, to, ToMove, theirKing, ml, size);
                }

            }

            while (capturesL != 0)
            {
                int to = poplsb(&capturesL);

                Move m = new Move(to - up - Direction.WEST, to);
                m.Capture = true;

                MakeCheck(Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (capturesR != 0)
            {
                int to = poplsb(&capturesR);

                Move m = new Move(to - up - Direction.EAST, to);
                m.Capture = true;

                MakeCheck(Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (promotionCapturesL != 0)
            {
                int to = poplsb(&promotionCapturesL);

                size = MakePromotionChecks(to - up - Direction.WEST, to, ToMove, theirKing, ml, size);
            }

            while (promotionCapturesR != 0)
            {
                int to = poplsb(&promotionCapturesR);

                size = MakePromotionChecks(to - up - Direction.EAST, to, ToMove, theirKing, ml, size);
            }

            if (State->EPSquare != EPNone)
            {
                ulong[] pawnAttacks = (ToMove == Color.White) ? BlackPawnAttackMasks : WhitePawnAttackMasks;
                ulong mask = notPromotingPawns & pawnAttacks[State->EPSquare];

                while (mask != 0)
                {
                    int from = poplsb(&mask);

                    Move m = new Move(from, State->EPSquare);
                    m.EnPassant = true;

                    //  TODO: this is slow
                    ulong moveMask = SquareBB[from] | SquareBB[State->EPSquare];
                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[State->EPSquare - up]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[State->EPSquare - up]);

                    ulong attacks = bb.AttackersTo(theirKing, bb.Occupancy) & bb.Colors[ToMove];

                    switch (popcount(attacks))
                    {
                        case 0:
                            break;
                        case 1:
                            m.CausesCheck = true;
                            m.SqChecker = lsb(attacks);
                            break;
                        case 2:
                            m.CausesDoubleCheck = true;
                            m.SqChecker = lsb(attacks);
                            //m.SqDoubleChecker = msb(attacks);
                            break;
                    }

                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[State->EPSquare - up]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[State->EPSquare - up]);

                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal knight moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllKnightMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Knight] & us);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = (KnightMasks[idx] & ~us);

                if (onlyCaptures)
                {
                    moves &= them;
                }

                while (moves != 0)
                {
                    int to = poplsb(&moves);

                    Move m = new Move(idx, to);
                    if ((them & SquareBB[to]) != 0)
                    {
                        m.Capture = true;
                    }

                    MakeCheck(Knight, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal bishop moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllBishopMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Bishop] & us);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = GetBishopMoves(us | them, idx) & ~us;

                if (onlyCaptures)
                {
                    moves &= them;
                }

                while (moves != 0)
                {
                    int to = poplsb(&moves);

                    Move m = new Move(idx, to);
                    if ((them & SquareBB[to]) != 0)
                    {
                        m.Capture = true;
                    }

                    MakeCheck(Piece.Bishop, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal rook moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllRookMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Rook] & us);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = GetRookMoves(us | them, idx) & ~us;

                if (onlyCaptures)
                {
                    moves &= them;
                }

                while (moves != 0)
                {
                    int to = poplsb(&moves);

                    Move m = new Move(idx, to);
                    if ((them & SquareBB[to]) != 0)
                    {
                        m.Capture = true;
                    }

                    MakeCheck(Piece.Rook, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal queen moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllQueenMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Queen] & us);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = (GetBishopMoves(us | them, idx) | GetRookMoves(us | them, idx)) & ~us;

                if (onlyCaptures)
                {
                    moves &= them;
                }

                while (moves != 0)
                {
                    int to = poplsb(&moves);

                    Move m = new Move(idx, to);
                    if ((them & SquareBB[to]) != 0)
                    {
                        m.Capture = true;
                    }

                    MakeCheck(Piece.Queen, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal king moves available to the side to move in the position, including castling moves.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllKingMoves(ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            int idx = bb.KingIndex(ToMove);
            int theirKing = bb.KingIndex(Not(ToMove));

            ulong moves = (NeighborsMask[idx] & ~us);

            if (onlyCaptures)
            {
                moves &= them;
            }

            if (!CheckInfo.InCheck && !CheckInfo.InDoubleCheck && GetIndexFile(idx) == Files.E && !onlyCaptures)
            {
                ulong ourKingMask = bb.KingMask(ToMove);
                Span<Move> CastleMoves = stackalloc Move[2];
                int numCastleMoves = GenCastlingMoves(idx, us, State->CastleStatus, CastleMoves, 0);
                for (int i = 0; i < numCastleMoves; i++)
                {
                    //  See if this will cause check.
                    Move m = CastleMoves[i];
                    int rookTo = m.To switch
                    {
                        C1 => D1,
                        G1 => F1,
                        C8 => D8,
                        G8 => F8,
                    };

                    ulong between = BetweenBB[rookTo][theirKing];

                    if (between != 0 &&
                        ((between & ((us | them) ^ ourKingMask)) == 0) &&
                        (GetIndexFile(rookTo) == GetIndexFile(theirKing) || GetIndexRank(rookTo) == GetIndexRank(theirKing)))
                    {
                        //  Then their king is on the same rank/file/diagonal as the square that our rook will end up at,
                        //  and there are no pieces which are blocking that ray.

                        m.CausesCheck = true;
                        m.SqChecker = rookTo;
                    }

                    ml[size++] = m;
                }
            }

            while (moves != 0)
            {
                int to = poplsb(&moves);

                Move m = new Move(idx, to);
                if ((them & SquareBB[to]) != 0)
                {
                    m.Capture = true;
                }

                MakeCheck(Piece.King, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            return size;
        }

        /// <summary>
        /// Generates the castling moves for the king on E1 or E8 
        /// by checking if the squares the king will pass through aren't under attack
        /// and the squares between the king and its rooks are empty.
        /// </summary>
        /// <returns>The number of castling moves generated, at most 2.</returns>
        [MethodImpl(Inline)]
        public int GenCastlingMoves(int idx, in ulong us, CastlingStatus Castling, in Span<Move> ml, int size)
        {
            if (idx == E1)
            {
                ulong all = (bb.Colors[Color.White] | bb.Colors[Color.Black]);

                if (Castling.HasFlag(CastlingStatus.WK))
                {
                    if ((all & WhiteKingsideMask) == 0 && (bb.AttackersTo(F1, all) & bb.Colors[Color.Black]) == 0 && (bb.AttackersTo(G1, all) & bb.Colors[Color.Black]) == 0)
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
                    if ((all & WhiteQueensideMask) == 0 && (bb.AttackersTo(C1, all) & bb.Colors[Color.Black]) == 0 && (bb.AttackersTo(D1, all) & bb.Colors[Color.Black]) == 0)
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
                    if ((all & BlackKingsideMask) == 0 && (bb.AttackersTo(F8, all) & bb.Colors[Color.White]) == 0 && (bb.AttackersTo(G8, all) & bb.Colors[Color.White]) == 0)
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
                    if ((all & BlackQueensideMask) == 0 && (bb.AttackersTo(C8, all) & bb.Colors[Color.White]) == 0 && (bb.AttackersTo(D8, all) & bb.Colors[Color.White]) == 0)
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
        /// Determines if the Move <paramref name="m"/> will put the enemy king in check or double check
        /// and updates <paramref name="m"/>'s check information.
        /// </summary>
        [MethodImpl(Inline)]
        private void MakeCheck(int pt, int ourColor, int theirKing, ref Move m)
        {
            int moveFrom = m.From;
            int moveTo = m.To;

            if ((State->CheckSquares[pt] & SquareBB[moveTo]) != 0)
            {
                //  This piece is making a direct check
                m.CausesCheck = true;
                m.SqChecker = moveTo;
            }

            if ((State->BlockingPieces[Not(ourColor)] & SquareBB[moveFrom]) != 0)
            {
                //  This piece is blocking a check on their king
                if (((RayBB[moveFrom][moveTo] & SquareBB[theirKing]) == 0) || m.Castle)
                {
                    //  If it moved off of the ray that it was blocking the check on,
                    //  then it is causing a discovery

                    if (m.CausesCheck)
                    {
                        //  If the piece that had been blocking also moved to one of the CheckSquares,
                        //  then this is actually double check.

                        m.CausesCheck = false;
                        m.CausesDoubleCheck = true;
                    }
                    else
                    {
                        m.CausesCheck = true;
                    }

                    Debug.Assert((State->Xrays[ourColor] & RayBB[moveFrom][theirKing]) != 0);

                    //  The piece causing the discovery is the xrayer of our color 
                    //  that is on the same ray that the piece we were moving shared with the king.
                    m.SqChecker = lsb(State->Xrays[ourColor] & RayBB[moveFrom][theirKing]);
                }
            }

            //  En passant, promotions, and castling checks are already handled
        }

        /// <summary>
        /// Generates all of the possible promotions for the pawn on <paramref name="from"/> and determines
        /// if those promotions will put the enemy king in check or double check.
        /// </summary>
        [MethodImpl(Inline)]
        private int MakePromotionChecks(int from, int promotionSquare, int ourColor, int theirKing, in Span<Move> ml, int size)
        {
            //int theirColor = Not(ourColor);
            ulong us = bb.Colors[ourColor];
            ulong them = bb.Colors[Not(ourColor)];
            ulong occ = us | them;

            for (int promotionPiece = Piece.Knight; promotionPiece <= Piece.Queen; promotionPiece++)
            {
                Move m = new Move(from, promotionSquare, promotionPiece);
                if ((them & SquareBB[promotionSquare]) != 0)
                {
                    m.Capture = true;
                }

                if ((bb.AttackMask(promotionSquare, ourColor, promotionPiece, (occ ^ SquareBB[from])) & SquareBB[theirKing]) != 0)
                {
                    m.CausesCheck = true;
                    m.SqChecker = promotionSquare;
                }

                if ((State->BlockingPieces[Not(ourColor)] & SquareBB[from]) != 0)
                {
                    //  This piece is blocking a check on their king
                    if ((RayBB[from][promotionSquare] & SquareBB[theirKing]) == 0)
                    {
                        //  If it moved off of the ray that it was blocking the check on,
                        //  then it is causing a discovery

                        if (m.CausesCheck)
                        {
                            //  If the piece that had been blocking also moved to one of the CheckSquares,
                            //  then this is actually double check.

                            m.CausesCheck = false;
                            m.CausesDoubleCheck = true;
                        }
                        else
                        {
                            m.CausesCheck = true;
                        }

                        Debug.Assert((State->Xrays[ourColor] & RayBB[from][theirKing]) != 0);

                        //  The piece causing the discovery is the xrayer of our color 
                        //  that is on the same ray that the piece we were moving shared with the king.
                        m.SqChecker = lsb(State->Xrays[ourColor] & RayBB[from][theirKing]);
                    }
                }

                ml[size++] = m;
            }
            return size;
        }

    }
}
