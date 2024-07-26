namespace Lizard.Logic.Core
{
    public unsafe partial class Position
    {
        /// The "GenNoisy" approach was inspired by the move generation of Stockfish.


        /// <summary>
        /// Generates the pseudo-legal moves for all of the pawns in the position, placing them into the 
        /// ScoredMove <paramref name="list"/> starting at the index <paramref name="size"/> and the new number
        /// of moves in the list is returned.
        /// <para></para>
        /// Only moves which have a To square whose bit is set in <paramref name="targets"/> will be generated.
        /// <br></br>
        /// For example:
        /// <br></br>
        /// When generating captures, <paramref name="targets"/> should be set to our opponent's color mask.
        /// <br></br>
        /// When generating evasions, <paramref name="targets"/> should be set to the <see cref="LineBB"/> between our king and the checker, which is the mask
        /// of squares that would block the check or capture the piece giving check.
        /// </summary>
        public int GenPawns<GenType>(ScoredMove* list, ulong targets, int size) where GenType : MoveGenerationType
        {
            bool noisyMoves  = typeof(GenType) == typeof(GenNoisy);
            bool evasions    = typeof(GenType) == typeof(GenEvasions);
            bool nonEvasions = typeof(GenType) == typeof(GenNonEvasions);

            ulong rank7 = (ToMove == White) ? Rank7BB : Rank2BB;
            ulong rank3 = (ToMove == White) ? Rank3BB : Rank6BB;

            int up = ShiftUpDir(ToMove);

            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];
            ulong captureSquares = evasions ? State->Checkers : them;

            ulong emptySquares = ~bb.Occupancy;

            ulong ourPawns = us & bb.Pieces[Pawn];
            ulong promotingPawns    = ourPawns & rank7;
            ulong notPromotingPawns = ourPawns & ~rank7;

            int theirKing = State->KingSquares[Not(ToMove)];

            if (!noisyMoves)
            {
                //  Include pawn pushes
                ulong moves    = Shift(up, notPromotingPawns) & emptySquares;
                ulong twoMoves = Shift(up, moves & rank3)     & emptySquares;

                if (evasions)
                {
                    //  Only include pushes which block the check
                    moves    &= targets;
                    twoMoves &= targets;
                }

                while (moves != 0)
                {
                    int to = poplsb(&moves);
                    list[size++].Move = new Move(to - up, to);
                }

                while (twoMoves != 0)
                {
                    int to = poplsb(&twoMoves);
                    list[size++].Move = new Move(to - up - up, to);
                }
            }

            if (promotingPawns != 0)
            {
                ulong promotions = Shift(up, promotingPawns) & emptySquares;
                ulong promotionCapturesL = Shift(up + Direction.WEST, promotingPawns) & captureSquares;
                ulong promotionCapturesR = Shift(up + Direction.EAST, promotingPawns) & captureSquares;

                if (evasions)
                {
                    //  Only promote on squares that block the check or capture the checker.
                    promotions &= targets;
                }

                while (promotions != 0)
                {
                    int to = poplsb(&promotions);
                    size = MakePromotionChecks(list, to - up, to, false, size);
                }

                while (promotionCapturesL != 0)
                {
                    int to = poplsb(&promotionCapturesL);
                    size = MakePromotionChecks(list, to - up - Direction.WEST, to, true, size);
                }

                while (promotionCapturesR != 0)
                {
                    int to = poplsb(&promotionCapturesR);
                    size = MakePromotionChecks(list, to - up - Direction.EAST, to, true, size);
                }
            }

            //  Don't generate captures for quiets
            ulong capturesL = Shift(up + Direction.WEST, notPromotingPawns) & captureSquares;
            ulong capturesR = Shift(up + Direction.EAST, notPromotingPawns) & captureSquares;

            while (capturesL != 0)
            {
                int to = poplsb(&capturesL);
                list[size++].Move = new Move(to - up - Direction.WEST, to);
            }

            while (capturesR != 0)
            {
                int to = poplsb(&capturesR);
                list[size++].Move = new Move(to - up - Direction.EAST, to);
            }

            if (State->EPSquare != EPNone)
            {
                if (evasions && (targets & (SquareBB[State->EPSquare + up])) != 0)
                {
                    //  When in check, we can only en passant if the pawn being captured is the one giving check
                    return size;
                }

                ulong mask = notPromotingPawns & PawnAttackMasks[Not(ToMove)][State->EPSquare];
                while (mask != 0)
                {
                    int from = poplsb(&mask);
                    list[size++].Move = new Move(from, State->EPSquare, Move.FlagEnPassant);
                }
            }

            return size;


            int MakePromotionChecks(ScoredMove* list, int from, int promotionSquare, bool isCapture, int size)
            {
                list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoQueen);

                if (!noisyMoves || isCapture)
                {
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoKnight);
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoRook);
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoBishop);
                }

                return size;
            }
        }


        /// <summary>
        /// Generates all the pseudo-legal moves for the player whose turn it is to move, given the <see cref="MoveGenerationType"/>.
        /// These are placed in the ScoredMove <paramref name="list"/> starting at the index <paramref name="size"/> and the new number
        /// of moves in the list is returned.
        /// </summary>
        public int GenAll<GenType>(ScoredMove* list, int size = 0) where GenType : MoveGenerationType
        {
            bool noisyMoves  = typeof(GenType) == typeof(GenNoisy);
            bool evasions    = typeof(GenType) == typeof(GenEvasions);
            bool nonEvasions = typeof(GenType) == typeof(GenNonEvasions);

            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];
            ulong occ  = bb.Occupancy;

            int ourKing   = State->KingSquares[ToMove];
            int theirKing = State->KingSquares[Not(ToMove)];

            ulong targets = 0;

            // If we are generating evasions and in double check, then skip non-king moves.
            if (!(evasions && MoreThanOne(State->Checkers)))
            {
                targets = evasions    ? LineBB[ourKing][lsb(State->Checkers)]
                        : nonEvasions ? ~us
                        : noisyMoves   ? them
                        :               ~occ;

                size = GenPawns<GenType>(list, targets, size);
                size = GenNormal(list, Knight, targets, size);
                size = GenNormal(list, Bishop, targets, size);
                size = GenNormal(list, Rook, targets, size);
                size = GenNormal(list, Queen, targets, size);
            }

            ulong moves = NeighborsMask[ourKing] & (evasions ? ~us : targets);
            while (moves != 0)
            {
                int to = poplsb(&moves);
                list[size++].Move = new Move(ourKing, to);
            }

            if (nonEvasions)
            {
                if (ToMove == White && (ourKing == E1 || IsChess960))
                {
                    if (CanCastle(occ, us, CastlingStatus.WK))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.WK], Move.FlagCastle);

                    if (CanCastle(occ, us, CastlingStatus.WQ))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.WQ], Move.FlagCastle);
                }
                else if (ToMove == Black && (ourKing == E8 || IsChess960))
                {
                    if (CanCastle(occ, us, CastlingStatus.BK))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.BK], Move.FlagCastle);

                    if (CanCastle(occ, us, CastlingStatus.BQ))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.BQ], Move.FlagCastle);
                }
            }

            return size;
        }


        /// <summary>
        /// Generates all of the legal moves that the player whose turn it is to move is able to make.
        /// The moves are placed into the array that <paramref name="legal"/> points to, 
        /// and the number of moves that were created is returned.
        /// </summary>
        public int GenLegal(ScoredMove* legal)
        {
            int numMoves = (State->Checkers != 0) ? GenAll<GenEvasions>(legal) :
                                                    GenAll<GenNonEvasions>(legal);

            int ourKing   = State->KingSquares[ToMove];
            int theirKing = State->KingSquares[Not(ToMove)];
            ulong pinned  = State->BlockingPieces[ToMove];

            ScoredMove* curr = legal;
            ScoredMove* end = legal + numMoves;

            while (curr != end)
            {
                if (!IsLegal(curr->Move, ourKing, theirKing, pinned))
                {
                    *curr = *--end;
                    numMoves--;
                }
                else
                {
                    ++curr;
                }
            }

            return numMoves;
        }


        /// <summary>
        /// Generates the pseudo-legal evasion or non-evasion moves for the position, depending on if the side to move is in check.
        /// The moves are placed into the array that <paramref name="pseudo"/> points to, 
        /// and the number of moves that were created is returned.
        /// </summary>
        public int GenPseudoLegal(ScoredMove* pseudo)
        {
            return (State->Checkers != 0) ? GenAll<GenEvasions>(pseudo) :
                                            GenAll<GenNonEvasions>(pseudo);
        }


        /// <summary>
        /// Generates the pseudo-legal moves for all of the pieces of type <paramref name="pt"/>, placing them into the 
        /// ScoredMove <paramref name="list"/> starting at the index <paramref name="size"/> and returning the new number
        /// of moves in the list.
        /// <para></para>
        /// Only moves which have a To square whose bit is set in <paramref name="targets"/> will be generated.
        /// <br></br>
        /// For example:
        /// <br></br>
        /// When generating captures, <paramref name="targets"/> should be set to our opponent's color mask.
        /// <br></br>
        /// When generating evasions, <paramref name="targets"/> should be set to the <see cref="LineBB"/> between our king and the checker, which is the mask
        /// of squares that would block the check or capture the piece giving check.
        /// <para></para>
        /// If <paramref name="checks"/> is true, then only pseudo-legal moves that give check will be generated.
        /// </summary>
        public int GenNormal(ScoredMove* list, int pt, ulong targets, int size)
        {
            // TODO: JIT seems to prefer having separate methods for each piece type, instead of a 'pt' parameter
            // This is far more convenient though

            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];
            ulong occ  = bb.Occupancy;

            ulong ourPieces = bb.Pieces[pt] & bb.Colors[ToMove];
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = bb.AttackMask(idx, ToMove, pt, occ) & targets;

                while (moves != 0)
                {
                    int to = poplsb(&moves);
                    list[size++].Move = new Move(idx, to);
                }
            }

            return size;
        }



        /// <summary>
        /// <inheritdoc cref="GenLegal(ScoredMove*)"/>
        /// </summary>
        public int GenLegal(Span<ScoredMove> legal)
        {
            //  The Span that this method receives is almost certainly already pinned (created via 'stackalloc'),
            //  but fixing it here is essentially free performance-wise and lets us use Span's when possible.
            fixed (ScoredMove* ptr = legal)
            {
                return GenLegal(ptr);
            }
        }


        public int GenPseudoLegalQS(ScoredMove* pseudo, int ttDepth)
        {
            return (State->Checkers != 0) ? GenAll<GenEvasions>(pseudo) :
                                            GenAllQS(pseudo, ttDepth);
        }

        public int GenAllQS(ScoredMove* list, int ttDepth, int size = 0)
        {
            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];
            ulong occ  = bb.Occupancy;

            int ourKing   = State->KingSquares[ToMove];
            int theirKing = State->KingSquares[Not(ToMove)];

            bool allowChecks = (ttDepth > Searches.DepthQNoChecks);

            size = GenPawnsQS(list, allowChecks, size);
            size = GenNormalQS(list, Knight, allowChecks, size);
            size = GenNormalQS(list, Bishop, allowChecks, size);
            size = GenNormalQS(list, Rook, allowChecks, size);
            size = GenNormalQS(list, Queen, allowChecks, size);


            if ((allowChecks && (State->BlockingPieces[Not(ToMove)] & SquareBB[ourKing]) != 0))
            {
                ulong moves = NeighborsMask[ourKing] & them;
                if (allowChecks)
                {
                    moves |= (NeighborsMask[ourKing] & ~us & ~bb.AttackMask(theirKing, Not(ToMove), Queen, occ));
                }

                while (moves != 0)
                {
                    list[size++].Move = new Move(ourKing, poplsb(&moves));
                }

                if (ToMove == White && (ourKing == E1 || IsChess960))
                {
                    if (CanCastle(occ, us, CastlingStatus.WK))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.WK], Move.FlagCastle);

                    if (CanCastle(occ, us, CastlingStatus.WQ))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.WQ], Move.FlagCastle);
                }
                else if (ToMove == Black && (ourKing == E8 || IsChess960))
                {
                    if (CanCastle(occ, us, CastlingStatus.BK))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.BK], Move.FlagCastle);

                    if (CanCastle(occ, us, CastlingStatus.BQ))
                        list[size++].Move = new Move(ourKing, CastlingRookSquares[(int)CastlingStatus.BQ], Move.FlagCastle);
                }
            }

            return size;
        }

        public int GenNormalQS(ScoredMove* list, int pt, bool allowChecks, int size)
        {
            // TODO: JIT seems to prefer having separate methods for each piece type, instead of a 'pt' parameter
            // This is far more convenient though

            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[Not(ToMove)];
            ulong occ  = bb.Occupancy;

            ulong ourPieces = bb.Pieces[pt] & bb.Colors[ToMove];
            while (ourPieces != 0)
            {
                int idx = poplsb(&ourPieces);
                ulong moves = bb.AttackMask(idx, ToMove, pt, occ) & (them | (allowChecks ? (~us & State->CheckSquares[pt]) : 0));

                while (moves != 0)
                {
                    int to = poplsb(&moves);
                    list[size++].Move = new Move(idx, to);
                }
            }

            return size;
        }

        public int GenPawnsQS(ScoredMove* list, bool allowChecks, int size)
        {
            ulong rank7 = (ToMove == White) ? Rank7BB : Rank2BB;
            ulong rank3 = (ToMove == White) ? Rank3BB : Rank6BB;

            int up = ShiftUpDir(ToMove);

            int theirColor = Not(ToMove);

            ulong us   = bb.Colors[ToMove];
            ulong them = bb.Colors[theirColor];
            ulong emptySquares = ~bb.Occupancy;

            ulong ourPawns = us & bb.Pieces[Pawn];
            ulong promotingPawns    = ourPawns &  rank7;
            ulong notPromotingPawns = ourPawns & ~rank7;

            int theirKing = State->KingSquares[theirColor];

            if (allowChecks)
            {
                ulong moves    = Shift(up, notPromotingPawns) & emptySquares & PawnAttackMasks[theirColor][theirKing];
                ulong twoMoves = Shift(up, moves & rank3)     & emptySquares & PawnAttackMasks[theirColor][theirKing];

                while (moves != 0)
                {
                    int to = poplsb(&moves);
                    list[size++].Move = new Move(to - up, to);
                }

                while (twoMoves != 0)
                {
                    int to = poplsb(&twoMoves);
                    list[size++].Move = new Move(to - up - up, to);
                }
            }
            

            if (promotingPawns != 0)
            {
                ulong promotions = Shift(up, promotingPawns) & emptySquares;
                ulong promotionCapturesL = Shift(up + Direction.WEST, promotingPawns) & them;
                ulong promotionCapturesR = Shift(up + Direction.EAST, promotingPawns) & them;

                while (promotions != 0)
                {
                    int to = poplsb(&promotions);
                    size = MakePromotionChecks(list, to - up, to, false, size);
                }

                while (promotionCapturesL != 0)
                {
                    int to = poplsb(&promotionCapturesL);
                    size = MakePromotionChecks(list, to - up - Direction.WEST, to, true, size);
                }

                while (promotionCapturesR != 0)
                {
                    int to = poplsb(&promotionCapturesR);
                    size = MakePromotionChecks(list, to - up - Direction.EAST, to, true, size);
                }
            }

            ulong capturesL = Shift(up + Direction.WEST, notPromotingPawns) & them;
            ulong capturesR = Shift(up + Direction.EAST, notPromotingPawns) & them;

            while (capturesL != 0)
            {
                int to = poplsb(&capturesL);
                list[size++].Move = new Move(to - up - Direction.WEST, to);
            }

            while (capturesR != 0)
            {
                int to = poplsb(&capturesR);
                list[size++].Move = new Move(to - up - Direction.EAST, to);
            }

            return size;


            int MakePromotionChecks(ScoredMove* list, int from, int promotionSquare, bool isCapture, int size)
            {
                if (isCapture)
                {
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoQueen);
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoKnight);
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoRook);
                    list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoBishop);
                }
                else if (allowChecks)
                {
                    if ((SquareBB[promotionSquare] & State->CheckSquares[Queen]) != 0)
                        list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoQueen);

                    if ((SquareBB[promotionSquare] & State->CheckSquares[Knight]) != 0)
                        list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoKnight);

                    if ((SquareBB[promotionSquare] & State->CheckSquares[Rook]) != 0)
                        list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoRook);

                    if ((SquareBB[promotionSquare] & State->CheckSquares[Bishop]) != 0)
                        list[size++].Move = new Move(from, promotionSquare, Move.FlagPromoBishop);
                }

                return size;
            }
        }

    }
}
