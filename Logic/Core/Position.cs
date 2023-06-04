using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using LTChess.Data;
using LTChess.Util;

namespace LTChess.Core
{
    public unsafe class Position
    {
        public Bitboard bb;

        /// <summary>
        /// The first number in the FEN, which starts at 1 and resets to 1 every time a pawn moves or a piece is captured.
        /// If this reaches 100, the game is a draw by the 50-move rule.
        /// </summary>
        public int HalfMoves = 0;

        /// <summary>
        /// The second number in the FEN, which starts at 0 and increases every time black moves.
        /// </summary>
        public int FullMoves = 1;

        public CastlingStatus Castling;

        public CheckInfo CheckInfo;

        /// <summary>
        /// Set equal to the index of the square that is one "behind" the pawn that just moved forward two squares.
        /// For example, if a white pawn is on d5, then moving the black c7 pawn to c5 sets this to the index of the square c6.
        /// </summary>
        public int EnPassantTarget = 0;

        /// <summary>
        /// Returns the color of the player whose turn it is to move.
        /// </summary>
        public int ToMove;

        /// <summary>
        /// Current zobrist hash of the position.
        /// </summary>
        public ulong Hash;

        /// <summary>
        /// A stack containing the moves that have been made since a position has been loaded from a FEN.
        /// </summary>
        public FasterStack<Move> Moves;

        public FasterStack<int> Captures;

        public FasterStack<CastlingStatus> Castles;

        public FasterStack<CheckInfo> Checks;

        public FasterStack<ulong> Hashes;

        public FasterStack<int> HalfmoveClocks;

        public static bool Perft_Stop = false;

        public bool whiteCastled = false;
        public bool blackCastled = false;


        /// <summary>
        /// Creates a new Position object, initializes it's internal FasterStack's and Bitboard, and loads the provided FEN.
        /// </summary>
        public Position(string fen = InitialFEN)
        {
            Moves = new FasterStack<Move>(MAX_CAPACITY);
            Captures = new FasterStack<int>(NORMAL_CAPACITY);
            Castles = new FasterStack<CastlingStatus>(MAX_CAPACITY);
            Checks = new FasterStack<CheckInfo>(MAX_CAPACITY);
            Hashes = new FasterStack<ulong>(MAX_CAPACITY);
            HalfmoveClocks = new FasterStack<int>(MAX_CAPACITY);

            this.bb = new Bitboard();

            LoadFromFEN(fen);

            this.bb.DetermineCheck(ToMove, ref CheckInfo);
            Hash = Zobrist.GetHash(this);
        }

        /// <summary>
        /// Generates the legal moves in the current position and makes the move that corresponds to the <paramref name="moveStr"/> if one exists.
        /// </summary>
        /// <param name="moveStr">The Algebraic notation for a move i.e. Nxd4+, or the Smith notation i.e. e2e4</param>
        /// <returns>True if <paramref name="moveStr"/> was a recognized and legal move.</returns>
        public bool TryMakeMove(string moveStr)
        {
            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = GenAllLegalMoves(list);
            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                if (m.ToString(this).ToLower().Equals(moveStr) || m.ToString().ToLower().Equals(moveStr))
                {
                    MakeMove(m);
                    return true;
                }
                if (i == size - 1)
                {
                    Log("No move '" + moveStr + "' found, try one of the following: ");
                    Log(list.Stringify(this) + "\r\n" + list.Stringify());
                }
            }
            return false;
        }

        /// <summary>
        /// Pushes the current state, then performs the move <paramref name="move"/> and updates the state of the board.
        /// </summary>
        /// <param name="move">The move to make, which needs to be generated from MoveGenerator.GenAllLegalMoves or strange things might happen.</param>
        [MethodImpl(Inline)]
        public void MakeMove(in Move move)
        {
            Castles.Push(Castling);
            Checks.Push(CheckInfo);
            Hashes.Push(Hash);
            HalfmoveClocks.Push(HalfMoves);

            int thisPiece = bb.GetPieceAtIndex(move.from);
            int thisColor = bb.GetColorAtIndex(move.from);

            int otherPiece = bb.GetPieceAtIndex(move.to);
            int otherColor = Not(thisColor);

#if DEBUG
            if (otherPiece != Piece.None && thisColor == bb.GetColorAtIndex(move.to))
            {
                 Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture our own " + PieceToString(otherPiece) + " on " + IndexToString(move.to));
            }
            if (otherPiece == Piece.King)
            {
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture " + ColorToString(bb.GetColorAtIndex(move.to)) + "'s king on " + IndexToString(move.to));
            }
#endif

            if (thisPiece == Piece.Pawn || otherPiece != Piece.None)
            {
                HalfMoves = 0;
            }
            else
            {
                HalfMoves++;
            }

            if (ToMove == Color.Black)
            {
                FullMoves++;
            }

            if (EnPassantTarget != 0)
            {
                //  Set EnPassantTarget to 0 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset to 0 every move.
                Hash = Hash.ZobristEnPassant(GetIndexFile(EnPassantTarget));
                EnPassantTarget = 0;
            }

            bool PawnDoubleMove = thisPiece == Piece.Pawn && (move.to ^ move.from) == 16;
            if (PawnDoubleMove)
            {
                bb.MoveSimple(move.from, move.to, thisColor, thisPiece);
                Hash = Hash.ZobristMove(move.from, move.to, ToMove, Piece.Pawn);

                if (thisColor == Color.White && (WhitePawnAttackMasks[move.to - 8] & (bb.Colors[Color.Black] & bb.Pieces[Piece.Pawn])) != 0)
                {
                    EnPassantTarget = move.to - 8;
                }
                else if (thisColor == Color.Black && (BlackPawnAttackMasks[move.to + 8] & (bb.Colors[Color.White] & bb.Pieces[Piece.Pawn])) != 0)
                {
                    EnPassantTarget = move.to + 8;
                }

                //  Update the En Passant file because we just changed EnPassantTarget
                Hash = Hash.ZobristEnPassant(GetIndexFile(EnPassantTarget));
            }
            else if (move.EnPassant)
            {
                bb.EnPassant(move.from, move.to, thisColor, move.idxEnPassant);
                Hash = Hash.ZobristMove(move.from, move.to, ToMove, Piece.Pawn);
                Hash = Hash.ZobristToggleSquare(otherColor, Piece.Pawn, move.idxEnPassant);
            }
            else if (move.Capture)
            {
                Captures.Push(otherPiece);

                if (otherPiece == Piece.Rook)
                {
                    //  If we are capturing a rook, make sure that if that we remove that castling status from them if necessary.
                    switch (move.to)
                    {
                        case A1:
                            Hash = Hash.ZobristCastle(Castling, CastlingStatus.WQ);
                            Castling &= ~CastlingStatus.WQ;
                            break;
                        case H1:
                            Hash = Hash.ZobristCastle(Castling, CastlingStatus.WK);
                            Castling &= ~CastlingStatus.WK;
                            break;
                        case A8:
                            Hash = Hash.ZobristCastle(Castling, CastlingStatus.BQ);
                            Castling &= ~CastlingStatus.BQ;
                            break;
                        case H8:
                            Hash = Hash.ZobristCastle(Castling, CastlingStatus.BK);
                            Castling &= ~CastlingStatus.BK;
                            break;
                    }
                }

                if (move.Promotion)
                {
                    //  Pawn capturing and promoting
                    bb.Promote(move.from, move.to, thisColor, otherPiece, move.PromotionTo);
                    Hash = Hash.ZobristToggleSquare(otherColor, otherPiece, move.to);
                    Hash = Hash.ZobristToggleSquare(thisColor, thisPiece, move.from);
                    Hash = Hash.ZobristToggleSquare(thisColor, move.PromotionTo, move.to);
                }
                else
                {
                    //  Normal capture
                    bb.Move(move.from, move.to, thisColor, thisPiece, otherPiece);
                    Hash = Hash.ZobristToggleSquare(otherColor, otherPiece, move.to);
                    Hash = Hash.ZobristMove(move.from, move.to, thisColor, thisPiece);
                }
            }
            else if (move.Castle)
            {
                //  Move the king
                bb.MoveSimple(move.from, move.to, thisColor, thisPiece);
                Hash = Hash.ZobristMove(move.from, move.to, thisColor, thisPiece);

                //  Then move the rook
                switch (move.to)
                {
                    case C1:
                        bb.MoveSimple(A1, D1, thisColor, Piece.Rook);
                        Hash = Hash.ZobristMove(A1, D1, thisColor, Piece.Rook);
                        break;
                    case G1:
                        bb.MoveSimple(H1, F1, thisColor, Piece.Rook);
                        Hash = Hash.ZobristMove(H1, F1, thisColor, Piece.Rook);
                        break;
                    case C8:
                        bb.MoveSimple(A8, D8, thisColor, Piece.Rook);
                        Hash = Hash.ZobristMove(A8, D8, thisColor, Piece.Rook);
                        break;
                    default:
                        bb.MoveSimple(H8, F8, thisColor, Piece.Rook);
                        Hash = Hash.ZobristMove(H8, F8, thisColor, Piece.Rook);
                        break;
                }

                if (thisColor == Color.White)
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.WK | CastlingStatus.WQ));
                    Castling &= ~(CastlingStatus.WK | CastlingStatus.WQ);
                    whiteCastled = true;
                }
                else
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.BK | CastlingStatus.BQ));
                    Castling &= ~(CastlingStatus.BK | CastlingStatus.BQ);
                    blackCastled = true;
                }
            }
            else if (move.Promotion)
            {
                bb.Promote(move.from, move.to, move.PromotionTo);
                Hash = Hash.ZobristToggleSquare(thisColor, thisPiece, move.from);
                Hash = Hash.ZobristToggleSquare(thisColor, move.PromotionTo, move.to);
            }
            else
            {
                //  A simple move that isn't a capture/castle/promotion/en passant
                bb.Move(move.from, move.to, thisColor, thisPiece, otherPiece);
                Hash = Hash.ZobristMove(move.from, move.to, thisColor, thisPiece);
            }

            if (thisPiece == Piece.King && !move.Castle)
            {
                //  If we made a king move, we can't castle anymore. If we just castled, don't bother
                if (thisColor == Color.White)
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.WK | CastlingStatus.WQ));
                    Castling &= ~(CastlingStatus.WK | CastlingStatus.WQ);
                }
                else
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.WK | CastlingStatus.WQ));
                    Castling &= ~(CastlingStatus.BK | CastlingStatus.BQ);
                }
            }
            else if (thisPiece == Piece.Rook && Castling != CastlingStatus.None)
            {
                //  If we just moved a rook, update Castling
                switch (move.from)
                {
                    case A1:
                        Hash = Hash.ZobristCastle(Castling, CastlingStatus.WQ);
                        Castling &= ~CastlingStatus.WQ;
                        break;
                    case H1:
                        Hash = Hash.ZobristCastle(Castling, CastlingStatus.WK);
                        Castling &= ~CastlingStatus.WK;
                        break;
                    case A8:
                        Hash = Hash.ZobristCastle(Castling, CastlingStatus.BQ);
                        Castling &= ~CastlingStatus.BQ;
                        break;
                    case H8:
                        Hash = Hash.ZobristCastle(Castling, CastlingStatus.BK);
                        Castling &= ~CastlingStatus.BK;
                        break;
                }
            }

            if (move.CausesCheck)
            {
                CheckInfo.idxChecker = move.idxChecker;
                CheckInfo.InCheck = true;
                CheckInfo.InDoubleCheck = false;
            }
            else if (move.CausesDoubleCheck)
            {
                CheckInfo.InCheck = false;
                CheckInfo.idxChecker = move.idxChecker;
                CheckInfo.InDoubleCheck = true;
                CheckInfo.idxDoubleChecker = move.idxDoubleChecker;
            }
            else
            {
                //	We are either getting out of a check, or weren't in check at all
                CheckInfo.InCheck = false;
                CheckInfo.idxChecker = 64;
                CheckInfo.InDoubleCheck = false;
                CheckInfo.idxDoubleChecker = 64;
            }

            ToMove = Not(ToMove);
            Moves.Push(move);
            Hash = Hash.ZobristChangeToMove();

#if DEBUG
            if (IsThreefoldRepetition())
            {
                Log("Game drawn by threefold repetition!");
            }
            else if (IsFiftyMoveDraw())
            {
                Log("Game drawn by the 50 move rule!");
            }
#endif

        }


        /// <summary>
        /// Undoes the last move that was made by popping from the Position's stacks.
        /// </summary>
        [MethodImpl(Inline)]
        public void UnmakeMove()
        {
            UnmakeMove(Moves.Pop());
        }

        /// <summary>
        /// Undoes the provided <paramref name="move"/> by popping from the stacks.
        /// </summary>
        /// <param name="move">This should only ever be from Moves.Pop() for now.</param>
        [MethodImpl(Inline)]
        private void UnmakeMove(in Move move)
        {
            //  Assume that "we" just made the last move, and "they" are undoing it.
            int ourPiece = bb.GetPieceAtIndex(move.to);
            int ourColor = Not(ToMove);
            int theirColor = ToMove;

            this.Castling = Castles.Pop();
            this.CheckInfo = Checks.Pop();
            this.Hash = Hashes.Pop();
            this.HalfMoves = HalfmoveClocks.Pop();

            ulong mask = (SquareBB[move.from] | SquareBB[move.to]);

            if (move.Capture)
            {
                int capturedPiece = Captures.Pop();

                if (move.Promotion)
                {
                    bb.Colors[ourColor] ^= mask;
                    bb.Colors[theirColor] ^= SquareBB[move.to];

                    bb.Pieces[move.PromotionTo] ^= SquareBB[move.to];
                    bb.Pieces[Piece.Pawn] ^= SquareBB[move.from];
                    bb.Pieces[capturedPiece] ^= SquareBB[move.to];

                    bb.PieceTypes[move.to] = capturedPiece;
                    bb.PieceTypes[move.from] = Piece.Pawn;
                }
                else
                {
                    bb.MoveSimple(move.to, move.from, ourColor, ourPiece);

                    bb.Colors[theirColor] ^= SquareBB[move.to];
                    bb.Pieces[capturedPiece] ^= SquareBB[move.to];

                    bb.PieceTypes[move.to] = capturedPiece;
                }
            }
            else if (move.EnPassant)
            {
                ulong epMask = SquareBB[move.idxEnPassant];
                bb.Colors[theirColor] ^= epMask;
                bb.Colors[ourColor] ^= mask;
                bb.Pieces[Piece.Pawn] ^= (mask | epMask);

                bb.PieceTypes[move.from] = Piece.Pawn;
                bb.PieceTypes[move.idxEnPassant] = Piece.Pawn;
                bb.PieceTypes[move.to] = Piece.None;
            }
            else if (move.Castle)
            {
                bb.MoveSimple(move.to, move.from, ourColor, ourPiece);
                if (move.to == C1)
                {
                    bb.MoveSimple(D1, A1, ourColor, Piece.Rook);
                }
                else if (move.to == G1)
                {
                    bb.MoveSimple(F1, H1, ourColor, Piece.Rook);
                }
                else if (move.to == C8)
                {
                    bb.MoveSimple(D8, A8, ourColor, Piece.Rook);
                }
                else
                {
                    //  move.to == G8
                    bb.MoveSimple(F8, H8, ourColor, Piece.Rook);
                }

                if (ourColor == Color.White)
                {
                    whiteCastled = true;
                }
                else
                {
                    blackCastled = false;
                }
            }
            else if (move.Promotion)
            {
                bb.Colors[ourColor] ^= mask;
                bb.Pieces[move.PromotionTo] ^= SquareBB[move.to];
                bb.Pieces[Piece.Pawn] ^= SquareBB[move.from];

                bb.PieceTypes[move.to] = Piece.None;
                bb.PieceTypes[move.from] = Piece.Pawn;
            }
            else
            {
                bb.MoveSimple(move.to, move.from, ourColor, ourPiece);
            }

            if (ourColor == Color.Black)
            {
                FullMoves--;
            }

            ToMove = Not(ToMove);
        }

        [MethodImpl(Inline)]
        public int GenAllPseudoMoves(in Span<Move> pseudo)
        {
            int size = 0;

            ulong us = bb.Colors[ToMove];
            ulong usCopy = us;

            ulong them = bb.Colors[Not(ToMove)];
            ulong theirKing = bb.KingMask(Not(ToMove));

            while (us != 0)
            {
                size += GenPseudoMoves(lsb(us), usCopy, them, theirKing, pseudo, size);
            }

            return size;
        }

        /// <summary>
        /// Generates all legal moves in the provided <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to generate for</param>
        /// <param name="legal">A Span of Move with sufficient size for the number of legal moves.</param>
        /// <returns>The number of legal moves generated and inserted into <paramref name="legal"/></returns>
        [MethodImpl(Inline)]
        public int GenAllLegalMoves(in Span<Move> legal)
        {
            Span<Move> pseudo = stackalloc Move[NORMAL_CAPACITY];
            int size = 0;

            ulong pinned = bb.PinnedPieces(ToMove);
            ulong us = bb.Colors[ToMove];
            ulong usCopy = us;
            ulong ourKingMask = bb.KingMask(ToMove);

            ulong them = bb.Colors[Not(ToMove)];
            ulong theirKingMask = bb.KingMask(Not(ToMove));

            Move move;
            int thisMovesCount;
            while (us != 0)
            {
                thisMovesCount = GenPseudoMoves(lsb(us), usCopy, them, theirKingMask, pseudo, 0);
                us = poplsb(us);
                for (int i = 0; i < thisMovesCount; i++)
                {
                    move = pseudo[i];
                    if (IsLegal(move, ourKingMask, theirKingMask, pinned))
                    {
                        legal[size++] = move;
                    }
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllLegalMovesTogether(in Span<Move> legal)
        {
            Span<Move> pseudo = stackalloc Move[NORMAL_CAPACITY];
            int size = 0;

            ulong pinned = bb.PinnedPieces(ToMove);
            ulong us = bb.Colors[ToMove] ^ (bb.Pieces[Piece.Pawn] & bb.Colors[ToMove]);
            ulong usCopy = bb.Colors[ToMove];
            ulong ourKingMask = bb.KingMask(ToMove);

            ulong them = bb.Colors[Not(ToMove)];
            ulong theirKingMask = bb.KingMask(Not(ToMove));

            Move move;
            int thisMovesCount = 0;

            thisMovesCount = GenAllPawnMoves(bb, usCopy, them, pseudo, thisMovesCount);
            thisMovesCount = GenAllKnightMoves(bb, usCopy, them, pseudo, thisMovesCount);
            thisMovesCount = GenAllBishopMoves(bb, usCopy, them, pseudo, thisMovesCount);
            thisMovesCount = GenAllRookMoves(bb, usCopy, them, pseudo, thisMovesCount);
            thisMovesCount = GenAllQueenMoves(bb, usCopy, them, pseudo, thisMovesCount);
            thisMovesCount = GenAllKingMoves(bb, usCopy, them, pseudo, thisMovesCount);

            for (int i = 0; i < thisMovesCount; i++)
            {
                move = pseudo[i];
                if (IsLegal(move, ourKingMask, theirKingMask, pinned))
                {
                    legal[size++] = move;
                }
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
        public int GenPseudoMoves(int idx, ulong us, ulong them, ulong theirKingMask, in Span<Move> ml, int size)
        {
            int ourColor = ToMove;
            int theirColor = Not(ourColor);
            int pt = bb.GetPieceAtIndex(idx);

            int theirKing = lsb(theirKingMask);

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

                            ulong attacks = bb.AttackersTo(theirKing, theirColor);
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
                    if (EnPassantTarget != 0 && GenEnPassantMove(bb, idx, EnPassantTarget, out Move EnPassant))
                    {
                        ulong moveMask = SquareBB[EnPassant.from] | SquareBB[EnPassant.to];
                        bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[EnPassant.idxEnPassant]);
                        bb.Colors[ourColor] ^= moveMask;
                        bb.Colors[theirColor] ^= (SquareBB[EnPassant.idxEnPassant]);

                        ulong attacks = bb.AttackersTo(theirKing, theirColor);

                        switch (popcount(attacks))
                        {
                            case 0:
                                break;
                            case 1:
                                EnPassant.CausesCheck = true;
                                EnPassant.idxChecker = lsb(attacks);
                                break;
                            case 2:
                                EnPassant.CausesDoubleCheck = true;
                                EnPassant.idxChecker = lsb(attacks);
                                EnPassant.idxDoubleChecker = msb(attacks);
                                break;
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
                    if (!CheckInfo.InCheck && !CheckInfo.InDoubleCheck && GetIndexFile(idx) == Files.E)
                    {
                        ulong ourKingMask = bb.KingMask(ourColor);
                        Span<Move> CastleMoves = stackalloc Move[2];
                        int numCastleMoves = GenCastlingMoves(bb, idx, us, Castling, CastleMoves, 0);
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
                            if (between != 0 && ((between & (all ^ ourKingMask)) == 0))
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

                ulong att = bb.AttackersToFast(theirKing, bb.Colors[ourColor] | bb.Colors[theirColor]) & bb.Colors[ourColor];

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
        public bool IsLegal(in Move move)
        {
            return IsLegal(move, bb.KingMask(ToMove), bb.KingMask(Not(ToMove)), bb.PinnedPieces(ToMove));
        }

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the position <paramref name="position"/>
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsLegal(in Move move, ulong ourKingMask, ulong theirKingMask, ulong pinnedPieces)
        {
            int ourKing = lsb(ourKingMask);
            int theirKing = lsb(theirKingMask);
            
            int pt = bb.GetPieceAtIndex(move.from);
            if (pt == Piece.None)
            {
                return false;
            }

            if (CheckInfo.InDoubleCheck && pt != Piece.King)
            {
                //	Must move king out of double check
                return false;
            }

            //  TODO: Look at this later to make sure GetColorAtIndex(ToMove) is still ok.
            //int ourColor = bb.GetColorAtIndex(move.from);
            int ourColor = ToMove;
            int theirColor = Not(ourColor);

            if (CheckInfo.InCheck)
            {
                //  We have 3 options: block the check, take the piece giving check, or move our king out of it.

                if (pt == Piece.King)
                {
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

                    /**
                    if ((bb.AttackersToMask(move.to, ourColor, SquareBB[ourKing]) | (NeighborsMask[move.to] & SquareBB[theirKing])) == 0)
                    {
                        return false;
                    }
                     */
                }

                //  If this will move the piece into the ray between our king and the checker,
                //  or if this move is capturing a checking pawn via en passant,
                //  then it is legal as long as it wasn't pinned.
                bool blocksOrCaptures = (LineBB[ourKing][CheckInfo.idxChecker] & SquareBB[move.to]) != 0;
                if (blocksOrCaptures || (move.EnPassant && move.idxEnPassant == CheckInfo.idxChecker))
                {
                    //  This move is another piece which has moved into the LineBB between our king and the checking piece.
                    //  This will be legal as long as it isn't pinned.

                    //return !IsPinned(bb, move.from, ourColor, ourKing, out _);
                    //return (pinnedPieces == 0 || (pinnedPieces & SquareBB[move.from]) == 0);
                    return (pinnedPieces & SquareBB[move.from]) == 0;
                }

                //  This isn't a king move and doesn't get us out of check, so it's illegal.
                return false;
            }

            if (pt == Piece.King)
            {
                //  We can move anywhere as long as it isn't attacked by them.

                /**
                ulong moveMask = (SquareBB[move.from] | SquareBB[move.to]);
                bb.Pieces[Piece.King] ^= moveMask;
                bb.Colors[ourColor] ^= moveMask;

                bool legal = true;
                if ((bb.AttackersTo(move.to, ourColor) | (NeighborsMask[move.to] & SquareBB[theirKing])) != 0)
                {
                    legal = false;
                }

                bb.Pieces[Piece.King] ^= moveMask;
                bb.Colors[ourColor] ^= moveMask;

                return legal;
                 */

                if ((bb.AttackersToMask(move.to, ourColor, ourKingMask) | (NeighborsMask[move.to] & theirKingMask)) != 0)
                {
                    return false;
                }
            }
            else if (move.EnPassant)
            //else if (!position.CheckInfo.InCheck && move.EnPassant)
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


        [MethodImpl(Inline)]
        public ulong GenPseudoPawnMask(in Bitboard bb, int idx, out ulong attacks, out ulong PromotionSquares)
        {
            //int ourColor = bb.GetColorAtIndex(idx);
            ulong them = bb.Colors[Not(ToMove)];
            ulong moves;

            if (ToMove == Color.White)
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
                int off = (ToMove == Color.White) ? 8 : -8;
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


        [MethodImpl(Inline)]
        public int GenAllPawnMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            ulong rank7 = (ToMove == Color.White) ? Rank7BB : Rank2BB;
            ulong rank3 = (ToMove == Color.White) ? Rank3BB : Rank6BB;

            int up = Up(ToMove);

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

            while (moves != 0)
            {
                int to = lsb(moves);
                moves = poplsb(moves);

                Move m = new Move(to - up, to);
                MakeCheck(bb, Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (twoMoves != 0)
            {
                int to = lsb(twoMoves);
                twoMoves = poplsb(twoMoves);

                Move m = new Move(to - up - up, to);
                MakeCheck(bb, Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (capturesL != 0)
            {
                int to = lsb(capturesL);
                capturesL = poplsb(capturesL);

                Move m = new Move(to - up - Direction.WEST, to);
                MakeCheck(bb, Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (capturesR != 0)
            {
                int to = lsb(capturesR);
                capturesR = poplsb(capturesR);

                Move m = new Move(to - up - Direction.EAST, to);
                MakeCheck(bb, Piece.Pawn, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            while (promotions != 0)
            {
                int to = lsb(promotions);
                promotions = poplsb(promotions);

                size = MakePromotionChecks(bb, to - up, to, ToMove, theirKing, ml, size);
            }

            while (promotionCapturesL != 0)
            {
                int to = lsb(promotionCapturesL);
                promotionCapturesL = poplsb(promotionCapturesL);

                size = MakePromotionChecks(bb, to - up - Direction.WEST, to, ToMove, theirKing, ml, size);
            }

            while (promotionCapturesR != 0)
            {
                int to = lsb(promotionCapturesR);
                promotionCapturesR = poplsb(promotionCapturesR);

                size = MakePromotionChecks(bb, to - up - Direction.EAST, to, ToMove, theirKing, ml, size);
            }

            if (EnPassantTarget != 0)
            {
                ulong[] pawnAttacks = (ToMove == Color.White) ? BlackPawnAttackMasks : WhitePawnAttackMasks;
                ulong mask = notPromotingPawns & pawnAttacks[EnPassantTarget];

                while (mask != 0)
                {
                    int from = lsb(mask);
                    mask = poplsb(mask);

                    Move m = new Move(from, EnPassantTarget);
                    m.EnPassant = true;
                    m.idxEnPassant = EnPassantTarget - up;

                    //  TODO: this is slow
                    ulong moveMask = SquareBB[m.from] | SquareBB[m.to];
                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[m.idxEnPassant]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[m.idxEnPassant]);

                    ulong attacks = bb.AttackersTo(theirKing, theirColor);

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

                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[m.idxEnPassant]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[m.idxEnPassant]);

                    ml[size++] = m;
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllKnightMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            ulong ourPieces = (bb.Pieces[Piece.Knight] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = (PrecomputedData.KnightMasks[idx] & ~us);
                ourPieces = poplsb(ourPieces);

                while (moves != 0)
                {
                    int to = lsb(moves);
                    moves = poplsb(moves);

                    Move m = new Move(idx, to);
                    MakeCheck(bb, Piece.Knight, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllBishopMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            ulong ourPieces = (bb.Pieces[Piece.Bishop] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = GetBishopMoves(us | them, idx) & ~us;
                ourPieces = poplsb(ourPieces);

                while (moves != 0)
                {
                    int to = lsb(moves);
                    moves = poplsb(moves);

                    Move m = new Move(idx, to);
                    MakeCheck(bb, Piece.Bishop, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllRookMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            ulong ourPieces = (bb.Pieces[Piece.Rook] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = GetRookMoves(us | them, idx) & ~us;
                ourPieces = poplsb(ourPieces);

                while (moves != 0)
                {
                    int to = lsb(moves);
                    moves = poplsb(moves);

                    Move m = new Move(idx, to);
                    MakeCheck(bb, Piece.Rook, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllQueenMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            ulong ourPieces = (bb.Pieces[Piece.Queen] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = (GetBishopMoves(us | them, idx) | GetRookMoves(us | them, idx)) & ~us;
                ourPieces = poplsb(ourPieces);

                while (moves != 0)
                {
                    int to = lsb(moves);
                    moves = poplsb(moves);

                    Move m = new Move(idx, to);
                    MakeCheck(bb, Piece.Queen, ToMove, theirKing, ref m);
                    ml[size++] = m;
                }
            }

            return size;
        }

        [MethodImpl(Inline)]
        public int GenAllKingMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size)
        {
            int idx = bb.KingIndex(ToMove);
            int theirKing = bb.KingIndex(Not(ToMove));

            ulong moves = (PrecomputedData.NeighborsMask[idx] & ~us);
            if (!CheckInfo.InCheck && !CheckInfo.InDoubleCheck && GetIndexFile(idx) == Files.E)
            {
                ulong ourKingMask = bb.KingMask(ToMove);
                Span<Move> CastleMoves = stackalloc Move[2];
                int numCastleMoves = GenCastlingMoves(bb, idx, us, Castling, CastleMoves, 0);
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
                    if (between != 0 && ((between & ((us | them) ^ ourKingMask)) == 0))
                    {
                        //  Then their king is on the same rank/file/diagonal as the square that our rook will end up at,
                        //  and there are no pieces which are blocking that ray.
                        if (GetIndexFile(rookTo) == GetIndexFile(theirKing) || GetIndexRank(rookTo) == GetIndexRank(theirKing))
                        {
                            m.CausesCheck = true;
                            m.idxChecker = rookTo;
                        }
                    }

                    MakeCheck(bb, Piece.King, ToMove, theirKing, ref m);

                    ml[size++] = m;
                }
            }

            while (moves != 0)
            {
                int to = lsb(moves);
                moves = poplsb(moves);

                Move m = new Move(idx, to);
                MakeCheck(bb, Piece.King, ToMove, theirKing, ref m);
                ml[size++] = m;
            }

            return size;
        }

        [MethodImpl(Inline)]
        public static void MakeCheck(in Bitboard bb, int pt, int ourColor, int theirKing, ref Move m)
        {
            int theirColor = Not(ourColor);
            ulong moveMask = (SquareBB[m.from] | SquareBB[m.to]);
            int capturedPiece = bb.GetPieceAtIndex(m.to);
            if (capturedPiece != Piece.None)
            {
                bb.Pieces[capturedPiece] ^= SquareBB[m.to];
                bb.Colors[theirColor] ^= SquareBB[m.to];
                m.Capture = true;
            }

            bb.Pieces[pt] ^= moveMask;
            bb.Colors[ourColor] ^= moveMask;

            ulong att = bb.AttackersToFast(theirKing, bb.Colors[ourColor] | bb.Colors[theirColor]) & bb.Colors[ourColor];

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
                bb.Pieces[capturedPiece] ^= SquareBB[m.to];
                bb.Colors[theirColor] ^= SquareBB[m.to];
            }

            bb.Pieces[pt] ^= moveMask;
            bb.Colors[ourColor] ^= moveMask;
        }


        [MethodImpl(Inline)]
        public static int MakePromotionChecks(in Bitboard bb, int from, int promotionSquare, int ourColor, int theirKing, in Span<Move> ml, int size)
        {
            int theirColor = Not(ourColor);

            for (int promotionPiece = Piece.Knight; promotionPiece <= Piece.Queen; promotionPiece++)
            {
                Move m = new Move(from, promotionSquare, promotionPiece);
                int cap = bb.GetPieceAtIndex(promotionSquare);
                if (cap != Piece.None)
                {
                    bb.Pieces[cap] ^= SquareBB[promotionSquare];
                    bb.Colors[theirColor] ^= SquareBB[promotionSquare];
                    m.Capture = true;
                }
                bb.Pieces[Piece.Pawn] ^= SquareBB[from];
                bb.Pieces[promotionPiece] ^= SquareBB[promotionSquare];
                bb.Colors[ourColor] ^= (SquareBB[from] | SquareBB[promotionSquare]);

                ulong attacks = bb.AttackersTo(theirKing, theirColor);
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
                bb.Pieces[Piece.Pawn] ^= SquareBB[from];
                bb.Pieces[promotionPiece] ^= SquareBB[promotionSquare];
                bb.Colors[ourColor] ^= (SquareBB[from] | SquareBB[promotionSquare]);

                ml[size++] = m;
            }
            return size;
        }


        [MethodImpl(Inline)]
        public bool IsInsufficientMaterial()
        {
            if ((bb.Pieces[Piece.Queen] | bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Pawn]) != 0)
            {
                return false;
            }

            ulong knights = popcount(bb.Pieces[Piece.Knight]);
            ulong bishops = popcount(bb.Pieces[Piece.Bishop]);

            //  Just kings, only 1 bishop, or 1 or 2 knights is a draw
            if ((knights == 0 && bishops < 2) || (bishops == 0 && knights <= 2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the move <paramref name="m"/> would cause a draw by threefold repetition or insufficient material.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public bool WouldCauseDraw(Move m)
        {
            MakeMove(m);
            bool check = (IsThreefoldRepetition() || IsInsufficientMaterial());
            UnmakeMove();

            return check;
        }


        /// <summary>
        /// Checks if the position is currently drawn by threefold repetition.
        /// Only considers moves made past the last time the HalfMoves clock was reset,
        /// which occurs when captures are made or a pawn moves.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsThreefoldRepetition()
        {
            if (HalfMoves < LowestRepetitionCount || Hashes.Count < LowestRepetitionCount)
            {
                return false;
            }

            int count = 0;
            for (int i = 0; i < HalfMoves; i++)
            {
                if (i == Hashes.Count)
                {
                    //  TODO actually fix this...
                    break;
                }

                if (Hashes.Peek(i) == Hash)
                {
                    count++;
                    if (count == 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        [MethodImpl(Inline)]
        public bool IsFiftyMoveDraw()
        {
            return HalfMoves >= 50;
        }

        

        /// <summary>
        /// Returns the number of leaf nodes in the current position up to <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The number of moves that will be made from the starting position. Depth 1 returns the current number of legal moves.</param>
        [MethodImpl(Inline)]
        public ulong Perft(int depth)
        {
            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            //int size = GenAllLegalMoves(list);
            int size = GenAllLegalMovesTogether(list);

            if (depth == 0)
            {
                return 1UL;
            }
            else if (depth == 1)
            {
                return (ulong)size;
            }

            ulong n = 0;

            for (int i = 0; i < size; i++)
            {
                MakeMove(list[i]);
                n += Perft(depth - 1);
                UnmakeMove();
            }

            return n;
        }

        public List<PerftNode> PerftDivide(int depth)
        {
            List<PerftNode> list = new List<PerftNode>();
            if (depth <= 0)
            {
                return list;
            }

            Span<Move> mlist = stackalloc Move[NORMAL_CAPACITY];
            //int size = GenAllLegalMoves(mlist);
            int size = GenAllLegalMovesTogether(mlist);
            for (int i = 0; i < size; i++)
            {
                PerftNode pn = new PerftNode();
                pn.root = mlist[i].ToString();
                MakeMove(mlist[i]);
                pn.number = Perft(depth - 1);
                UnmakeMove();
                list.Add(pn);

                Console.Title = "Progress: " + (i + 1) + " / " + size + " branches";
            }

            return list;
        }

        /// <summary>
        /// Updates the position's Bitboard, ToMove, castling status, en passant target, and half/full move clock.
        /// </summary>
        /// <param name="fen">The FEN to set the position to</param>
        public void LoadFromFEN(string fen)
        {
            string[] splits = fen.Split(new char[] { '/', ' ' });

            bb.Reset();
            Castling = CastlingStatus.None;
            HalfMoves = 0;
            FullMoves = 1;

            for (int i = 0; i < splits.Length; i++)
            {
                //	it's a row on the board
                if (i <= 7)
                {
                    int pieceX = 0;
                    for (int x = 0; x < splits[i].Length; x++)
                    {
                        if (char.IsLetter(splits[i][x]))
                        {
                            int pt = FENToPiece(splits[i][x]);
                            int idx = CoordToIndex(pieceX, 7 - i);
                            bb.Pieces[pt] |= SquareBB[idx];
                            bb.PieceTypes[idx] = pt;
                            if (char.IsUpper(splits[i][x]))
                            {
                                bb.Colors[Color.White] |= SquareBB[idx];
                            }
                            else
                            {
                                bb.Colors[Color.Black] |= SquareBB[idx];
                            }

                            pieceX++;
                        }
                        else if (char.IsDigit(splits[i][x]))
                        {
                            int add = int.Parse(splits[i][x].ToString());
                            pieceX += add;
                        }
                        else
                        {
                            LogE("x for i = " + i + " was '" + splits[i][x] + "' and didn't get parsed");
                        }
                    }
                }
                //	who moves next
                else if (i == 8)
                {
                    ToMove = splits[i].Equals("w") ? Color.White : Color.Black;
                }
                //	castling availability
                else if (i == 9)
                {
                    if (splits[i].Contains("-"))
                    {
                        Castling = 0;
                    }
                    else
                    {
                        Castling |= splits[i].Contains("K") ? CastlingStatus.WK : 0;
                        Castling |= splits[i].Contains("Q") ? CastlingStatus.WQ : 0;
                        Castling |= splits[i].Contains("k") ? CastlingStatus.BK : 0;
                        Castling |= splits[i].Contains("q") ? CastlingStatus.BQ : 0;
                    }
                }
                //	en passant target or last double pawn move
                else if (i == 10)
                {
                    if (!splits[i].Contains("-"))
                    {
                        //	White moved a pawn last
                        if (splits[i][1].Equals('3'))
                        {
                            //int id = CoordToIndex(GetFileInt(splits[i][0]), 4);
                            EnPassantTarget = StringToIndex(splits[i]);
                        }
                        else if (splits[i][1].Equals('6'))
                        {
                            //int id = CoordToIndex(GetFileInt(splits[i][0]), 5);
                            EnPassantTarget = StringToIndex(splits[i]);
                        }
                    }
                }
                //	halfmove number
                else if (i == 11)
                {
                    HalfMoves = int.Parse(splits[i]);
                }
                //	fullmove number
                else if (i == 12)
                {
                    FullMoves = int.Parse(splits[i]);
                }
            }
        }

        public string GetFEN()
        {
            StringBuilder fen = new StringBuilder();

            for (int y = 7; y >= 0; y--)
            {
                int i = 0;
                for (int x = 0; x <= 7; x++)
                {
                    int index = CoordToIndex(x, y);
                    if (bb.IsColorSet(Color.White, index))
                    {
                        if (i != 0)
                        {
                            fen.Append(i.ToString());
                            i = 0;
                        }

                        int pt = bb.GetPieceAtIndex(index);
                        if (pt != Piece.None)
                        {
                            char c = PieceToFENChar(pt);
                            fen.Append(char.ToUpper(c));
                        }

                        continue;
                    }
                    else if (bb.IsColorSet(Color.Black, index))
                    {
                        if (i != 0)
                        {
                            fen.Append(i.ToString());
                            i = 0;
                        }

                        int pt = bb.GetPieceAtIndex(index);
                        if (pt != Piece.None)
                        {
                            char c = PieceToFENChar(pt);
                            fen.Append(char.ToLower(c));
                        }

                        continue;
                    }
                    else
                    {
                        i++;
                    }
                    if (x == 7)
                    {
                        fen.Append(i);
                    }
                }
                if (y != 0)
                {
                    fen.Append("/");
                }
            }

            fen.Append(ToMove == Color.White ? " w " : " b ");

            bool CanC = false;
            if (Castling.HasFlag(CastlingStatus.WK))
            {
                fen.Append("K");
                CanC = true;
            }
            if (Castling.HasFlag(CastlingStatus.WQ))
            {
                fen.Append("Q");
                CanC = true;
            }
            if (Castling.HasFlag(CastlingStatus.BK))
            {
                fen.Append("k");
                CanC = true;
            }
            if (Castling.HasFlag(CastlingStatus.BQ))
            {
                fen.Append("q");
                CanC = true;
            }
            if (!CanC)
            {
                fen.Append("-");
            }
            if (EnPassantTarget != 0)
            {
                fen.Append(" " + IndexToString(EnPassantTarget));
            }
            else
            {
                fen.Append(" -");
            }

            fen.Append(" " + HalfMoves + " " + FullMoves);

            return fen.ToString();
        }

        public override string ToString()
        {
            return PrintBoard(bb);
        }

    }
}
