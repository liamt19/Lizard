using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        public ulong Hash;

        /// <summary>
        /// A stack containing the moves that have been made since a position has been loaded from a FEN.
        /// </summary>
        public FasterStack<Move> Moves;

        public FasterStack<int> Captures;

        public FasterStack<CastlingStatus> Castles;

        public FasterStack<CheckInfo> Checks;

        public FasterStack<ulong> Hashes;

        public bool debug_abort_perft = false;



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

            this.bb = new Bitboard();

            LoadFromFEN(fen);

            PositionUtilities.DetermineCheck(this.bb, ToMove, ref CheckInfo);
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
            int size = MoveGenerator.GenAllLegalMoves(this, list);
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
        [MethodImpl(Optimize)]
        public void MakeMove(in Move move)
        {
            Castles.Push(Castling);
            Checks.Push(CheckInfo);
            Hashes.Push(Hash);

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
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture " + bb.GetColorAtIndex(move.to).ToString() + "'s king on " + IndexToString(move.to));
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
                }
                else
                {
                    Hash = Hash.ZobristCastle(Castling, (CastlingStatus.BK | CastlingStatus.BQ));
                    Castling &= ~(CastlingStatus.BK | CastlingStatus.BQ);
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
                //Log("Game drawn by repetition!");
            }
            else if (IsInsufficientMaterial())
            {
                //Log("Game drawn by insufficient material!");
            }
            else if (IsFiftyMoveDraw())
            {
                //Log("Game drawn by the 50 move rule!");
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
        public bool IsInsufficientMaterial()
        {
            if ((bb.Pieces[Piece.Queen] | bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Pawn]) != 0)
            {
                return false;
            }

            ulong knights = popcount(bb.Pieces[Piece.Knight]);
            ulong bishops = popcount(bb.Pieces[Piece.Bishop]);

            //  Just kings, only 1 bishop, or 1 or 2 knights is a draw
            if ((knights == 0 && bishops <= 1) || (bishops == 0 && knights < 2))
            {
                return true;
            }

            return false;
        }


        [MethodImpl(Inline)]
        public bool IsThreefoldRepetition()
        {
            int count = 0;
            for (int i = 0; i < Hashes.Count - 1; i++)
            {
                //  TODO: probably don't need to be going through Hashes.Count - 1 to figure this out.
                if (Hashes.Peek(i) == Hash)
                {
                    count++;
                    if (count == 3)
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
            int size = GenAllLegalMoves(this, list);

            if (depth == 1)
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
            int size = MoveGenerator.GenAllLegalMoves(this, mlist);
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

        public List<PerftNode> PerftDivideCancellable(int depth)
        {
            List<PerftNode> list = new List<PerftNode>();
            if (depth <= 0)
            {
                return list;
            }

            Span<Move> mlist = stackalloc Move[NORMAL_CAPACITY];
            int size = MoveGenerator.GenAllLegalMoves(this, mlist);
            for (int i = 0; i < size; i++)
            {
                PerftNode pn = new PerftNode();
                pn.root = mlist[i].ToString();
                list.Add(pn);
            }

            for (int i = 0; i < size; i++)
            {
                MakeMove(mlist[i]);
                PerftNode pn = list[i];
                pn.number = Perft(depth - 1);
                list[i] = pn;
                UnmakeMove();

                if (debug_abort_perft)
                {
                    Log("Aborting after " + (i + 1) + " / " + size + " branches");
                    debug_abort_perft = false;
                    break;
                }

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
