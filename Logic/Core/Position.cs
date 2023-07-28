using System.Diagnostics;
using System.Text;

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

        /// <summary>
        /// A stack containing the ID's of the pieces that have been captured since the position was loaded.
        /// </summary>
        public FasterStack<int> Captures;

        /// <summary>
        /// A stack containing CastlingStatus enums since the position was loaded.
        /// </summary>
        public FasterStack<CastlingStatus> Castles;

        /// <summary>
        /// A stack containing CheckInfo structs since the position was loaded.
        /// </summary>
        public FasterStack<CheckInfo> Checks;

        /// <summary>
        /// A stack containing the Zobrist hashes since the position was loaded.
        /// </summary>
        public FasterStack<ulong> Hashes;

        /// <summary>
        /// A stack containing the the Halfmove clock counts since the position was loaded.
        /// </summary>
        public FasterStack<int> HalfmoveClocks;

        /// <summary>
        /// A stack containing the En Passant targets since the position was loaded.
        /// </summary>
        public FasterStack<int> EnPassantTargets;

        /// <summary>
        /// Set to true if white makes a castling move.
        /// </summary>
        public bool WhiteCastled = false;

        /// <summary>
        /// Set to true if black makes a castling move.
        /// </summary>
        public bool BlackCastled = false;

        public int[] MaterialCount;

        /// <summary>
        /// Creates a new Position object, initializes it's internal FasterStack's and Bitboard, and loads the provided FEN.
        /// </summary>
        public Position(string fen = InitialFEN)
        {
            Moves = new FasterStack<Move>(MaxListCapacity);
            Captures = new FasterStack<int>(NormalListCapacity);
            Castles = new FasterStack<CastlingStatus>(MaxListCapacity);
            Checks = new FasterStack<CheckInfo>(MaxListCapacity);
            Hashes = new FasterStack<ulong>(MaxListCapacity);
            HalfmoveClocks = new FasterStack<int>(MaxListCapacity);
            EnPassantTargets = new FasterStack<int>(MaxListCapacity);
            MaterialCount = new int[2];

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
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = GenAllLegalMovesTogether(list);
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
            EnPassantTargets.Push(EnPassantTarget);

            int ourPiece = bb.GetPieceAtIndex(move.From);
            int ourColor = bb.GetColorAtIndex(move.From);

            int theirPiece = bb.GetPieceAtIndex(move.To);
            int theirColor = Not(ourColor);

#if DEBUG
            if (theirPiece != Piece.None && ourColor == bb.GetColorAtIndex(move.To))
            {
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture our own " + PieceToString(theirPiece) + " on " + IndexToString(move.To));
            }
            if (theirPiece == Piece.King)
            {
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture " + ColorToString(bb.GetColorAtIndex(move.To)) + "'s king on " + IndexToString(move.To));
            }
#endif

            if (ourPiece == Piece.Pawn || theirPiece != Piece.None)
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

            int tempEPSquare = EnPassantTarget;

            if (EnPassantTarget != 0)
            {
                //  Set EnPassantTarget to 0 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset to 0 every move.
                Hash = Hash.ZobristEnPassant(GetIndexFile(EnPassantTarget));
                EnPassantTarget = 0;
            }

            if (move.EnPassant)
            {
                int idxPawn = ((bb.Pieces[Piece.Pawn] & SquareBB[tempEPSquare - 8]) != 0) ? tempEPSquare - 8 : tempEPSquare + 8;
                bb.EnPassant(move.From, move.To, ourColor, idxPawn);
                Hash = Hash.ZobristMove(move.From, move.To, ToMove, Piece.Pawn);
                Hash = Hash.ZobristToggleSquare(theirColor, Piece.Pawn, idxPawn);

                MaterialCount[theirColor] -= GetPieceValue(Piece.Pawn);
            }
            else if (ourPiece == Piece.Pawn && (move.To ^ move.From) == 16)
            {
                bb.MoveSimple(move.From, move.To, ourColor, ourPiece);
                Hash = Hash.ZobristMove(move.From, move.To, ToMove, Piece.Pawn);

                if (ourColor == Color.White && (WhitePawnAttackMasks[move.To - 8] & (bb.Colors[Color.Black] & bb.Pieces[Piece.Pawn])) != 0)
                {
                    EnPassantTarget = move.To - 8;
                }
                else if (ourColor == Color.Black && (BlackPawnAttackMasks[move.To + 8] & (bb.Colors[Color.White] & bb.Pieces[Piece.Pawn])) != 0)
                {
                    EnPassantTarget = move.To + 8;
                }

                //  Update the En Passant file because we just changed EnPassantTarget
                Hash = Hash.ZobristEnPassant(GetIndexFile(EnPassantTarget));
            }
            else if (move.Capture)
            {
                Captures.Push(theirPiece);

                MaterialCount[theirColor] -= GetPieceValue(theirPiece);

                if (theirPiece == Piece.Rook)
                {
                    //  If we are capturing a rook, make sure that if that we remove that castling status from them if necessary.
                    switch (move.To)
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
                    bb.Promote(move.From, move.To, ourColor, theirPiece, move.PromotionTo);
                    Hash = Hash.ZobristToggleSquare(theirColor, theirPiece, move.To);
                    Hash = Hash.ZobristToggleSquare(ourColor, ourPiece, move.From);
                    Hash = Hash.ZobristToggleSquare(ourColor, move.PromotionTo, move.To);

                    MaterialCount[ourColor] -= GetPieceValue(Piece.Pawn);
                    MaterialCount[ourColor] += GetPieceValue(move.PromotionTo);
                }
                else
                {
                    //  Normal capture
                    bb.Move(move.From, move.To, ourColor, ourPiece, theirPiece);
                    Hash = Hash.ZobristToggleSquare(theirColor, theirPiece, move.To);
                    Hash = Hash.ZobristMove(move.From, move.To, ourColor, ourPiece);


                }
            }
            else if (move.Castle)
            {
                //  Move the king
                bb.MoveSimple(move.From, move.To, ourColor, ourPiece);
                Hash = Hash.ZobristMove(move.From, move.To, ourColor, ourPiece);

                //  Then move the rook
                switch (move.To)
                {
                    case C1:
                        bb.MoveSimple(A1, D1, ourColor, Piece.Rook);
                        Hash = Hash.ZobristMove(A1, D1, ourColor, Piece.Rook);
                        break;
                    case G1:
                        bb.MoveSimple(H1, F1, ourColor, Piece.Rook);
                        Hash = Hash.ZobristMove(H1, F1, ourColor, Piece.Rook);
                        break;
                    case C8:
                        bb.MoveSimple(A8, D8, ourColor, Piece.Rook);
                        Hash = Hash.ZobristMove(A8, D8, ourColor, Piece.Rook);
                        break;
                    default:
                        bb.MoveSimple(H8, F8, ourColor, Piece.Rook);
                        Hash = Hash.ZobristMove(H8, F8, ourColor, Piece.Rook);
                        break;
                }

                if (ourColor == Color.White)
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.WK | CastlingStatus.WQ));
                    Castling &= ~(CastlingStatus.WK | CastlingStatus.WQ);
                    WhiteCastled = true;
                }
                else
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.BK | CastlingStatus.BQ));
                    Castling &= ~(CastlingStatus.BK | CastlingStatus.BQ);
                    BlackCastled = true;
                }
            }
            else if (move.Promotion)
            {
                bb.Promote(move.From, move.To, move.PromotionTo);
                Hash = Hash.ZobristToggleSquare(ourColor, ourPiece, move.From);
                Hash = Hash.ZobristToggleSquare(ourColor, move.PromotionTo, move.To);

                MaterialCount[ourColor] -= GetPieceValue(Piece.Pawn);
                MaterialCount[ourColor] += GetPieceValue(move.PromotionTo);
            }
            else
            {
                //  A simple move that isn't a capture/castle/promotion/en passant
                bb.Move(move.From, move.To, ourColor, ourPiece, theirPiece);
                Hash = Hash.ZobristMove(move.From, move.To, ourColor, ourPiece);
            }

            if (ourPiece == Piece.King && !move.Castle)
            {
                //  If we made a king move, we can't castle anymore. If we just castled, don't bother
                if (ourColor == Color.White)
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
            else if (ourPiece == Piece.Rook && Castling != CastlingStatus.None)
            {
                //  If we just moved a rook, update Castling
                switch (move.From)
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
                CheckInfo.idxChecker = move.SqChecker;
                CheckInfo.InCheck = true;
                CheckInfo.InDoubleCheck = false;
            }
            else if (move.CausesDoubleCheck)
            {
                CheckInfo.InCheck = false;
                CheckInfo.idxChecker = move.SqChecker;
                CheckInfo.InDoubleCheck = true;
                CheckInfo.idxDoubleChecker = move.SqDoubleChecker;
            }
            else
            {
                //	We are either getting out of a check, or weren't in check at all
                CheckInfo.InCheck = false;
                CheckInfo.idxChecker = LSBEmpty;
                CheckInfo.InDoubleCheck = false;
                CheckInfo.idxDoubleChecker = LSBEmpty;
            }

            ToMove = Not(ToMove);
            Moves.Push(move);
            Hash = Hash.ZobristChangeToMove();
        }

        /// <summary>
        /// Performs a null move, which only updates the EnPassantTarget (since it is always reset to 0 when a move is made) 
        /// and position hash accordingly.
        /// </summary>
        [MethodImpl(Inline)]
        public void MakeNullMove()
        {
            Hashes.Push(Hash);
            EnPassantTargets.Push(EnPassantTarget);

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

            ToMove = Not(ToMove);
            Hash = Hash.ZobristChangeToMove();
        }

        /// <summary>
        /// Undoes a null move, which returns EnPassantTarget and the hash to their previous values.
        /// </summary>
        [MethodImpl(Inline)]
        public void UnmakeNullMove()
        {

            if (ToMove == Color.White)
            {
                FullMoves--;
            }

            ToMove = Not(ToMove);

            this.Hash = Hashes.Pop();
            this.EnPassantTarget = EnPassantTargets.Pop();
        }


        /// <summary>
        /// Undoes the last move that was made by popping from the Position's stacks.
        /// </summary>
        [MethodImpl(Inline)]
        public void UnmakeMove() => UnmakeMove(Moves.Pop());

        /// <summary>
        /// Undoes the provided <paramref name="move"/> by popping from the stacks.
        /// </summary>
        /// <param name="move">This should only ever be from Moves.Pop() for now.</param>
        [MethodImpl(Inline)]
        private void UnmakeMove(in Move move)
        {
            //  Assume that "we" just made the last move, and "they" are undoing it.
            int ourPiece = bb.GetPieceAtIndex(move.To);
            int ourColor = Not(ToMove);
            int theirColor = ToMove;

            this.Castling = Castles.Pop();
            this.CheckInfo = Checks.Pop();
            this.Hash = Hashes.Pop();
            this.HalfMoves = HalfmoveClocks.Pop();
            this.EnPassantTarget = EnPassantTargets.Pop();

            ulong mask = (SquareBB[move.From] | SquareBB[move.To]);

            if (move.Capture)
            {
                int capturedPiece = Captures.Pop();
                MaterialCount[theirColor] += GetPieceValue(capturedPiece);

                if (move.Promotion)
                {
                    bb.Colors[ourColor] ^= mask;
                    bb.Colors[theirColor] ^= SquareBB[move.To];

                    bb.Pieces[move.PromotionTo] ^= SquareBB[move.To];
                    bb.Pieces[Piece.Pawn] ^= SquareBB[move.From];
                    bb.Pieces[capturedPiece] ^= SquareBB[move.To];

                    bb.PieceTypes[move.To] = capturedPiece;
                    bb.PieceTypes[move.From] = Piece.Pawn;

                    MaterialCount[ourColor] -= GetPieceValue(move.PromotionTo);
                    MaterialCount[ourColor] += GetPieceValue(Piece.Pawn);
                }
                else
                {
                    bb.MoveSimple(move.To, move.From, ourColor, ourPiece);

                    bb.Colors[theirColor] ^= SquareBB[move.To];
                    bb.Pieces[capturedPiece] ^= SquareBB[move.To];

                    bb.PieceTypes[move.To] = capturedPiece;
                }
            }
            else if (move.EnPassant)
            {
                int idxPawn = EnPassantTarget + Up(ToMove);
                ulong epMask = SquareBB[idxPawn];
                bb.Colors[theirColor] ^= epMask;
                bb.Colors[ourColor] ^= mask;
                bb.Pieces[Piece.Pawn] ^= (mask | epMask);

                bb.PieceTypes[move.From] = Piece.Pawn;
                bb.PieceTypes[idxPawn] = Piece.Pawn;
                bb.PieceTypes[move.To] = Piece.None;

                MaterialCount[theirColor] += GetPieceValue(Piece.Pawn);
            }
            else if (move.Castle)
            {
                bb.MoveSimple(move.To, move.From, ourColor, ourPiece);
                if (move.To == C1)
                {
                    bb.MoveSimple(D1, A1, ourColor, Piece.Rook);
                }
                else if (move.To == G1)
                {
                    bb.MoveSimple(F1, H1, ourColor, Piece.Rook);
                }
                else if (move.To == C8)
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
                    WhiteCastled = false;
                }
                else
                {
                    BlackCastled = false;
                }
            }
            else if (move.Promotion)
            {
                bb.Colors[ourColor] ^= mask;
                bb.Pieces[move.PromotionTo] ^= SquareBB[move.To];
                bb.Pieces[Piece.Pawn] ^= SquareBB[move.From];

                bb.PieceTypes[move.To] = Piece.None;
                bb.PieceTypes[move.From] = Piece.Pawn;

                MaterialCount[ourColor] -= GetPieceValue(move.PromotionTo);
                MaterialCount[ourColor] += GetPieceValue(Piece.Pawn);
            }
            else
            {
                bb.MoveSimple(move.To, move.From, ourColor, ourPiece);
            }

            if (ourColor == Color.Black)
            {
                FullMoves--;
            }

            ToMove = Not(ToMove);
        }


        /// <summary>
        /// Generates all legal moves in the provided <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position to generate for</param>
        /// <param name="legal">A Span of Move with sufficient size for the number of legal moves.</param>
        /// <returns>The number of legal moves generated and inserted into <paramref name="legal"/></returns>
        [MethodImpl(Inline)]
        public int GenAllLegalMovesTogether(in Span<Move> legal, bool onlyCaptures = false)
        {
            Span<Move> pseudo = stackalloc Move[NormalListCapacity];
            int size = 0;

            ulong pinned = bb.PinnedPieces(ToMove);
            ulong us = bb.Colors[ToMove] ^ (bb.Pieces[Piece.Pawn] & bb.Colors[ToMove]);
            ulong usCopy = bb.Colors[ToMove];
            ulong ourKingMask = bb.KingMask(ToMove);

            ulong them = bb.Colors[Not(ToMove)];
            ulong theirKingMask = bb.KingMask(Not(ToMove));

            Move move;
            int thisMovesCount = 0;

            if (!CheckInfo.InDoubleCheck)
            {
                thisMovesCount = GenAllPawnMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllKnightMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllBishopMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllRookMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);
                thisMovesCount = GenAllQueenMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);
            }

            thisMovesCount = GenAllKingMoves(bb, usCopy, them, pseudo, thisMovesCount, onlyCaptures);

            for (int i = 0; i < thisMovesCount; i++)
            {
                move = pseudo[i];
                if (IsLegal(this, bb, move, bb.KingIndex(ToMove), bb.KingIndex(Not(ToMove)), pinned))
                {
                    legal[size++] = move;
                }
            }

            return size;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal in the current position.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsLegal(in Move move) => IsLegal(this, this.bb, move, bb.KingIndex(ToMove), bb.KingIndex(Not(ToMove)), bb.PinnedPieces(ToMove));

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the position <paramref name="position"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsLegal(in Position position, in Bitboard bb, Move move, int ourKing, int theirKing) =>
            IsLegal(position, bb, move, ourKing, theirKing, bb.PinnedPieces(bb.GetColorAtIndex(ourKing)));

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the position <paramref name="position"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsLegal(in Position position, in Bitboard bb, Move move, int ourKing, int theirKing, ulong pinnedPieces)
        {
            int pt = bb.GetPieceAtIndex(move.From);
            if (position.CheckInfo.InDoubleCheck && pt != Piece.King)
            {
                //	Must move king out of double check
                return false;
            }

            Debug.Assert(move.Capture == false || (move.Capture == true && bb.GetPieceAtIndex(move.To) != Piece.None),
                "ERROR IsLegal(" + move.ToString() + " = " + move.ToString(position) + ") is trying to capture a piece on an empty square!");

            int ourColor = bb.GetColorAtIndex(move.From);
            int theirColor = Not(ourColor);

            if (position.CheckInfo.InCheck)
            {
                //  We have 3 Options: block the check, take the piece giving check, or move our king out of it.

                if (pt == Piece.King)
                {
                    //  Either move out or capture the piece
                    ulong moveMask = (SquareBB[move.From] | SquareBB[move.To]);
                    bb.Pieces[Piece.King] ^= moveMask;
                    bb.Colors[ourColor] ^= moveMask;
                    if ((bb.AttackersTo(move.To, ourColor) | (NeighborsMask[move.To] & SquareBB[theirKing])) != 0)
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
                bool blocksOrCaptures = (LineBB[ourKing][checker] & SquareBB[move.To]) != 0;

                if (blocksOrCaptures || (move.EnPassant && GetIndexFile(move.To) == GetIndexFile(position.CheckInfo.idxChecker)))
                {
                    //  This move is another piece which has moved into the LineBB between our king and the checking piece.
                    //  This will be legal as long as it isn't pinned.

                    return (pinnedPieces == 0 || (pinnedPieces & SquareBB[move.From]) == 0);
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
                if ((bb.AttackersToMask(move.To, ourColor, SquareBB[ourKing]) | (NeighborsMask[move.To] & SquareBB[theirKing])) != 0)
                {
                    return false;
                }
            }
            else if (move.EnPassant)
            {
                //  En passant will remove both our pawn and the opponents pawn from the rank so this needs a special check
                //  to make sure it is still legal

                int idxPawn = move.To - Up(ourColor);

                ulong moveMask = (SquareBB[move.From] | SquareBB[move.To]);
                bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxPawn]);
                bb.Colors[ourColor] ^= moveMask;
                bb.Colors[theirColor] ^= (SquareBB[idxPawn]);

                if (bb.AttackersTo(ourKing, ourColor) != 0)
                {
                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxPawn]);
                    bb.Colors[ourColor] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[idxPawn]);
                    return false;
                }

                bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxPawn]);
                bb.Colors[ourColor] ^= moveMask;
                bb.Colors[theirColor] ^= (SquareBB[idxPawn]);
            }
            else if (IsPinned(bb, move.From, ourColor, ourKing, out int pinner))
            {
                //	If we are pinned, make sure we are only moving in directions that keep us pinned
                return ((LineBB[ourKing][pinner] & SquareBB[move.To]) != 0);
            }

            return true;
        }

        /// <summary>
        /// Generates all pseudo-legal pawn moves available to the side to move in the position, including en passant moves.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllPawnMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
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

            if (!onlyCaptures)
            {
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

                while (promotions != 0)
                {
                    int to = lsb(promotions);
                    promotions = poplsb(promotions);

                    size = MakePromotionChecks(bb, to - up, to, ToMove, theirKing, ml, size);
                }

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

                    //  TODO: this is slow
                    ulong moveMask = SquareBB[m.From] | SquareBB[m.To];
                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[EnPassantTarget - up]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[EnPassantTarget - up]);

                    ulong attacks = bb.AttackersTo(theirKing, theirColor);

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
                            m.SqDoubleChecker = msb(attacks);
                            break;
                    }

                    bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[EnPassantTarget - up]);
                    bb.Colors[ToMove] ^= moveMask;
                    bb.Colors[theirColor] ^= (SquareBB[EnPassantTarget - up]);

                    ml[size++] = m;
                }
            }

            return size;
        }

        /// <summary>
        /// Generates all pseudo-legal knight moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllKnightMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Knight] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = (PrecomputedData.KnightMasks[idx] & ~us);
                ourPieces = poplsb(ourPieces);

                if (onlyCaptures)
                {
                    moves &= them;
                }

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

        /// <summary>
        /// Generates all pseudo-legal bishop moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllBishopMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Bishop] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = GetBishopMoves(us | them, idx) & ~us;
                ourPieces = poplsb(ourPieces);

                if (onlyCaptures)
                {
                    moves &= them;
                }

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

        /// <summary>
        /// Generates all pseudo-legal rook moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllRookMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Rook] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = GetRookMoves(us | them, idx) & ~us;
                ourPieces = poplsb(ourPieces);

                if (onlyCaptures)
                {
                    moves &= them;
                }

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

        /// <summary>
        /// Generates all pseudo-legal queen moves available to the side to move in the position.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllQueenMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            ulong ourPieces = (bb.Pieces[Piece.Queen] & bb.Colors[ToMove]);
            int theirKing = bb.KingIndex(Not(ToMove));
            while (ourPieces != 0)
            {
                int idx = lsb(ourPieces);
                ulong moves = (GetBishopMoves(us | them, idx) | GetRookMoves(us | them, idx)) & ~us;
                ourPieces = poplsb(ourPieces);

                if (onlyCaptures)
                {
                    moves &= them;
                }

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

        /// <summary>
        /// Generates all pseudo-legal king moves available to the side to move in the position, including castling moves.
        /// </summary>
        [MethodImpl(Inline)]
        public int GenAllKingMoves(in Bitboard bb, ulong us, ulong them, in Span<Move> ml, int size, bool onlyCaptures = false)
        {
            int idx = bb.KingIndex(ToMove);
            int theirKing = bb.KingIndex(Not(ToMove));

            ulong moves = (PrecomputedData.NeighborsMask[idx] & ~us);

            if (onlyCaptures)
            {
                moves &= them;
            }

            if (!CheckInfo.InCheck && !CheckInfo.InDoubleCheck && GetIndexFile(idx) == Files.E && !onlyCaptures)
            {
                ulong ourKingMask = bb.KingMask(ToMove);
                Span<Move> CastleMoves = stackalloc Move[2];
                int numCastleMoves = GenCastlingMoves(bb, idx, us, Castling, CastleMoves, 0);
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
                    else
                    {
                        MakeCheck(bb, Piece.King, ToMove, theirKing, ref m);
                    }

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

        /// <summary>
        /// Determines if the Move <paramref name="m"/> will put the enemy king in check or double check
        /// and updates <paramref name="m"/><c>.CausesCheck</c> and friends accordingly.
        /// </summary>
        [MethodImpl(Inline)]
        private static void MakeCheck(in Bitboard bb, int pt, int ourColor, int theirKing, ref Move m)
        {
            int theirColor = Not(ourColor);
            ulong moveMask = (SquareBB[m.From] | SquareBB[m.To]);
            int capturedPiece = bb.GetPieceAtIndex(m.To);
            if (capturedPiece != Piece.None)
            {
                bb.Pieces[capturedPiece] ^= SquareBB[m.To];
                bb.Colors[theirColor] ^= SquareBB[m.To];
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
                    m.SqChecker = lsb(att);
                    break;
                case 2:
                    m.CausesDoubleCheck = true;
                    m.SqChecker = lsb(att);
                    m.SqDoubleChecker = msb(att);
                    break;
            }

            if (capturedPiece != Piece.None)
            {
                bb.Pieces[capturedPiece] ^= SquareBB[m.To];
                bb.Colors[theirColor] ^= SquareBB[m.To];
            }

            bb.Pieces[pt] ^= moveMask;
            bb.Colors[ourColor] ^= moveMask;
        }

        /// <summary>
        /// Generates all of the possible promotions for the pawn on <paramref name="from"/> and determines
        /// if those promotions will put the enemy king in check or double check.
        /// </summary>
        [MethodImpl(Inline)]
        private static int MakePromotionChecks(in Bitboard bb, int from, int promotionSquare, int ourColor, int theirKing, in Span<Move> ml, int size)
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
                        m.SqChecker = lsb(attacks);
                        break;
                    case 2:
                        m.CausesDoubleCheck = true;
                        m.SqChecker = lsb(attacks);
                        m.SqDoubleChecker = msb(attacks);
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


        /// <summary>
        /// Checks if the position is currently drawn by insufficient material.
        /// This generally only happens for KvK, KvKB, and KvKN endgames.
        /// </summary>
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
            //  Some organizations classify 2 knights a draw and others don't.
            if ((knights == 0 && bishops < 2) || (bishops == 0 && knights <= 2))
            {
                return true;
            }

            return false;
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
            return HalfMoves >= 100;
        }



        /// <summary>
        /// Returns the number of leaf nodes in the current position up to <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The number of moves that will be made from the starting position. Depth 1 returns the current number of legal moves.</param>
        [MethodImpl(Inline)]
        public ulong Perft(int depth)
        {
            Span<Move> list = stackalloc Move[NormalListCapacity];
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

        /// <summary>
        /// Same as PerftDivide, but uses Parallel.For rather than a regular for loop.
        /// This ran 5-6x faster for me on a Core i5-12500H.
        /// </summary>
        public List<PerftNode> PerftDivideParallel(int depth)
        {
            if (depth <= 0)
            {
                return new List<PerftNode>();
            }

            string rootFEN = this.GetFEN();

            Move[] mlist = new Move[NormalListCapacity];
            int size = GenAllLegalMovesTogether(mlist);

            List<PerftNode> list = new List<PerftNode>(size);

            Parallel.For(0, size, i =>
            {
                PerftNode pn = new PerftNode();
                Position threadPosition = new Position(rootFEN);

                pn.root = mlist[i].ToString();
                threadPosition.MakeMove(mlist[i]);
                pn.number = threadPosition.Perft(depth - 1);

                list.Add(pn);
            });

            return list;
        }

        public List<PerftNode> PerftDivide(int depth)
        {
            List<PerftNode> list = new List<PerftNode>();
            if (depth <= 0)
            {
                return list;
            }

            Span<Move> mlist = stackalloc Move[NormalListCapacity];
            int size = GenAllLegalMovesTogether(mlist);
            for (int i = 0; i < size; i++)
            {
                PerftNode pn = new PerftNode();
                pn.root = mlist[i].ToString();
                MakeMove(mlist[i]);
                pn.number = Perft(depth - 1);
                UnmakeMove();
                list.Add(pn);

                //Console.Title = "Progress: " + (i + 1) + " / " + size + " branches";
            }

            return list;
        }


        /// <summary>
        /// Updates the position's Bitboard, ToMove, castling status, en passant target, and half/full move clock.
        /// </summary>
        /// <param name="fen">The FEN to set the position to</param>
        public bool LoadFromFEN(string fen)
        {
            Position temp = (Position)this.MemberwiseClone();

            temp.Moves = this.Moves.Clone();
            temp.Captures = this.Captures.Clone();
            temp.Castles = this.Castles.Clone();
            temp.Checks = this.Checks.Clone();
            temp.Hashes = this.Hashes.Clone();
            temp.HalfmoveClocks = this.HalfmoveClocks.Clone();
            temp.EnPassantTargets = this.EnPassantTargets.Clone();

            try
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
                                Log("ERROR x for i = " + i + " was '" + splits[i][x] + "' and didn't get parsed");
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
            catch (Exception ex)
            {
                Log("Failed parsing '" + fen + "': ");
                Log(ex.ToString());

                this.Moves.CopyFromArray(temp.Moves);
                this.Captures.CopyFromArray(temp.Captures);
                this.Castles.CopyFromArray(temp.Castles);
                this.Checks.CopyFromArray(temp.Checks);
                this.Hashes.CopyFromArray(temp.Hashes);
                this.HalfmoveClocks.CopyFromArray(temp.HalfmoveClocks);
                this.EnPassantTargets.CopyFromArray(temp.EnPassantTargets);

                return false;
            }

            this.bb.DetermineCheck(ToMove, ref CheckInfo);
            Hash = Zobrist.GetHash(this);
            MaterialCount[Color.White] = bb.MaterialCount(Color.White);
            MaterialCount[Color.Black] = bb.MaterialCount(Color.Black);

            return true;
        }

        /// <summary>
        /// Returns the FEN string of the current position.
        /// </summary>
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
