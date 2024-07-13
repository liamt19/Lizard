using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.Data;
using Lizard.Logic.NN;
using Lizard.Logic.Threads;

namespace Lizard.Logic.Core
{
    public unsafe partial class Position
    {
        public Bitboard bb;

        /// <summary>
        /// The second number in the FEN, which starts at 1 and increases every time black moves.
        /// </summary>
        public int FullMoves = 1;
        public int ToMove;

        /// <summary>
        /// The sum of the non-pawn material in the position, indexed by piece color.
        /// </summary>
        public int[] MaterialCountNonPawn;

        public bool InCheck;
        public bool InDoubleCheck;

        /// <summary>
        /// Set to the index of the piece giving check, 
        /// or the piece with the lowest square index if there are two pieces giving check.
        /// </summary>
        public int idxChecker;


        public bool Checked => InCheck || InDoubleCheck;

        public ulong Hash => State->Hash;


        /// <summary>
        /// The number of <see cref="StateInfo"/> items that memory will be allocated for within the StateStack, which is 256 KB.
        /// If you find yourself in a game exceeding 2047 moves, go outside.
        /// </summary>
        private const int StateStackSize = 2048;

        private readonly StateInfo* _stateBlock;

        /// <summary>
        /// A pointer to the beginning of the StateStack, which is used to make sure we don't try to access the StateStack at negative indices.
        /// </summary>
        private readonly StateInfo* _SentinelStart;
        private readonly StateInfo* _SentinelEnd;


        public StateInfo* StartingState => _SentinelStart;

        /// <summary>
        /// A pointer to this Position's current <see cref="StateInfo"/> object, which corresponds to the StateStack[GamePly]
        /// </summary>
        public StateInfo* State;
        public StateInfo* NextState => (State + 1);



        private readonly Accumulator* _accumulatorBlock;


        /// <summary>
        /// The number of moves that have been made so far since this Position was created.
        /// <br></br>
        /// Only used to easily keep track of how far along the StateStack we currently are.
        /// </summary>
        public int GamePly = 0;

        /// <summary>
        /// The SearchThread that owns this Position instance.
        /// </summary>
        public readonly SearchThread Owner;

        /// <summary>
        /// Whether or not to incrementally update accumulators when making/unmaking moves.
        /// This must be true if this position object is being used in a search, 
        /// but for purely perft this should be disabled for performance.
        /// </summary>
        public readonly bool UpdateNN;


        public int[] CastlingRookSquares;
        public ulong[] CastlingRookPaths;

        public bool IsChess960 = false;


        public bool CastlingImpeded(ulong occ, CastlingStatus cr) => (occ & CastlingRookPaths[(int)cr]) != 0;
        public bool HasCastlingRook(ulong us, CastlingStatus cr) => (bb.Pieces[Rook] & SquareBB[CastlingRookSquares[(int)cr]] & us) != 0;


        /// <summary>
        /// Creates a new Position object and loads the provided FEN.
        /// <br></br>
        /// If <paramref name="createAccumulators"/> is true, then this Position will create and incrementally update 
        /// its accumulators when making and unmaking moves.
        /// <para></para>
        /// <paramref name="owner"/> should be set to one of the <see cref="SearchThread"/>'s within the <see cref="SearchThreadPool.SearchPool"/>
        /// (the <see cref="SearchThreadPool.MainThread"/> unless there are multiple threads in the pool).
        /// <br></br>
        /// If <paramref name="owner"/> is <see langword="null"/> then <paramref name="createAccumulators"/> should be false.
        /// </summary>
        public Position(string fen = InitialFEN, bool createAccumulators = true, SearchThread owner = null)
        {
            MaterialCountNonPawn = new int[2];
            CastlingRookSquares = new int[(int)CastlingStatus.All];
            CastlingRookPaths = new ulong[(int)CastlingStatus.All];


            this.UpdateNN = createAccumulators;
            this.Owner = owner;

            this.bb = new Bitboard();

            _stateBlock = (StateInfo*)AlignedAllocZeroed((nuint)(sizeof(StateInfo) * StateStackSize), AllocAlignment);

            _SentinelStart = &_stateBlock[0];
            _SentinelEnd = &_stateBlock[StateStackSize - 1];
            State = &_stateBlock[0];

            if (UpdateNN)
            {
                //  Create the accumulators now if we need to.
                //  This is actually a rather significant memory investment
                //  so this constructor should be called as infrequently as possible to keep the memory usage from spiking
                _accumulatorBlock = (Accumulator*)AlignedAllocZeroed((nuint)(sizeof(Accumulator) * StateStackSize), AllocAlignment);
                for (int i = 0; i < StateStackSize; i++)
                {
                    (_stateBlock + i)->Accumulator = _accumulatorBlock + i;
                    *(_stateBlock + i)->Accumulator = new Accumulator();
                }
            }


            if (UpdateNN && Owner == null)
            {
                Debug.WriteLine($"info string Position('{fen}', {createAccumulators}, ...) has NNUE enabled and was given a nullptr for owner! " +
                                $"Assigning this Position instance to the SearchPool's MainThread, UB and other weirdness may occur...");
                Owner = SearchPool.MainThread;
            }

            LoadFromFEN(fen);

            if (UpdateNN)
            {
                NNUE.RefreshAccumulator(this);
            }
        }


        /// <summary>
        /// Free the memory block that we allocated for this Position's <see cref="Bitboard"/>
        /// </summary>
        ~Position()
        {
            if (UpdateNN)
            {
                //  Free each accumulator, then the block
                for (int i = 0; i < StateStackSize; i++)
                {
                    var acc = *(_SentinelStart + i)->Accumulator;
                    acc.Dispose();
                }

                NativeMemory.AlignedFree((void*)_accumulatorBlock);
            }

            NativeMemory.AlignedFree((void*)_stateBlock);
        }

        /// <summary>
        /// Generates the legal moves in the current position and makes the move that corresponds to the <paramref name="moveStr"/> if one exists.
        /// </summary>
        /// <param name="moveStr">The Algebraic notation for a move i.e. Nxd4+, or the Smith notation i.e. e2e4</param>
        /// <returns>True if <paramref name="moveStr"/> was a recognized and legal move.</returns>
        public bool TryMakeMove(string moveStr)
        {
            Span<ScoredMove> list = stackalloc ScoredMove[MoveListSize];
            int size = GenLegal(list);
            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                if (m.ToString(this).ToLower().Equals(moveStr.ToLower()) || m.ToString(IsChess960).ToLower().Equals(moveStr.ToLower()))
                {
                    MakeMove(m);
                    return true;
                }
                if (i == size - 1)
                {
                    Log("No move '" + moveStr + "' found, try one of the following: ");
                    Log(Stringify(list, this) + "\r\n" + Stringify(list));
                }
            }
            return false;
        }

        /// <summary>
        /// Generates the legal moves in the current position and makes the move that corresponds to the <paramref name="moveStr"/> if one exists.
        /// </summary>
        /// <param name="moveStr">The Algebraic notation for a move i.e. Nxd4+, or the Smith notation i.e. e2e4</param>
        /// <returns>True if <paramref name="moveStr"/> was a recognized and legal move.</returns>
        public bool TryFindMove(string moveStr, out Move move)
        {
            Span<ScoredMove> list = stackalloc ScoredMove[MoveListSize];
            int size = GenLegal(list);
            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;

                if (m.ToString(this).ToLower().Equals(moveStr.ToLower()) || m.ToString(IsChess960).ToLower().Equals(moveStr.ToLower()))
                {
                    move = m;
                    return true;
                }
            }

            Log("No move '" + moveStr + "' found, try one of the following: ");
            Log(Stringify(list, this) + "\r\n" + Stringify(list));
            move = Move.Null;
            return false;
        }

        /// <summary>
        /// Copies the current state into the <paramref name="newState"/>, then performs the move <paramref name="move"/>.
        /// <br></br>
        /// If <see cref="UpdateNN"/> is true, this method will also update the NNUE networks.
        /// </summary>
        /// <param name="move">The move to make, which needs to be a legal move or strange things might happen.</param>
        public void MakeMove(Move move)
        {
            //  Copy everything except the pointer to the accumulator, which should never change.
            //  The data within the accumulator will be copied, but each state needs its own pointer to its own accumulator.
            Unsafe.CopyBlock(NextState, State, (uint)StateInfo.StateCopySize);

            if (UpdateNN)
            {
                NNUE.MakeMove(this, move);
            }

            //  Move onto the next state
            State++;

            State->HalfmoveClock++;
            State->PliesFromNull++;
            GamePly++;

            if (ToMove == Black)
            {
                FullMoves++;
            }

            int moveFrom = move.From;
            int moveTo = move.To;

            int ourPiece = bb.GetPieceAtIndex(moveFrom);
            int ourColor = ToMove;

            int theirPiece = bb.GetPieceAtIndex(moveTo);
            int theirColor = Not(ourColor);

            Assert(ourPiece != None,   $"Move {move.ToString()} in FEN '{GetFEN()}' doesn't have a piece on the From square!");
            Assert(theirPiece != King, $"Move {move.ToString()} in FEN '{GetFEN()}' captures a king!");
            Assert(theirPiece == None || (ourColor != bb.GetColorAtIndex(moveTo) || move.Castle),
                $"Move {move.ToString()} in FEN '{GetFEN()}' captures our own {PieceToString(theirPiece)} on {IndexToString(moveTo)}");

            if (ourPiece == King)
            {
                if (move.Castle)
                {
                    //  Castling moves are KxR, so "theirPiece" is actually our own rook.
                    theirPiece = None;

                    //  Move our rook and update the hash
                    DoCastling(ourColor, moveFrom, moveTo, undo: false);
                    State->KingSquares[ourColor] = move.CastlingKingSquare;
                }
                else
                {
                    State->KingSquares[ourColor] = moveTo;
                }

                //  Remove all of our castling rights
                RemoveCastling(ourColor == White ? CastlingStatus.White : CastlingStatus.Black);
            }
            else if (ourPiece == Rook && State->CastleStatus != CastlingStatus.None)
            {
                //  If we just moved a rook, update st->CastleStatus
                RemoveCastling(GetCastlingForRook(moveFrom));
            }

            State->CapturedPiece = theirPiece;
            if (theirPiece != None)
            {
                //  Remove their piece, and update the hash
                bb.RemovePiece(moveTo, theirColor, theirPiece);
                State->Hash.ZobristToggleSquare(theirColor, theirPiece, moveTo);

                if (theirPiece == Rook)
                {
                    //  If we are capturing a rook, make sure that if that we remove that castling status from them if necessary.
                    RemoveCastling(GetCastlingForRook(moveTo));
                }

                if (theirPiece != Pawn)
                {
                    MaterialCountNonPawn[theirColor] -= GetPieceValue(theirPiece);
                }

                //  Reset the halfmove clock
                State->HalfmoveClock = 0;
            }


            int tempEPSquare = State->EPSquare;
            if (State->EPSquare != EPNone)
            {
                //  Set st->EPSquare to 64 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset every move.
                State->Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                State->EPSquare = EPNone;
            }

            if (ourPiece == Pawn)
            {
                if (move.EnPassant)
                {

                    int idxPawn = ((bb.Pieces[Pawn] & SquareBB[tempEPSquare - 8]) != 0) ? tempEPSquare - 8 : tempEPSquare + 8;
                    bb.RemovePiece(idxPawn, theirColor, Pawn);
                    State->Hash.ZobristToggleSquare(theirColor, Pawn, idxPawn);

                    //  The EnPassant/Capture flags are mutually exclusive, so set CapturedPiece here
                    State->CapturedPiece = Pawn;
                }
                else if ((moveTo ^ moveFrom) == 16)
                {
                    int down = -ShiftUpDir(ourColor);

                    //  st->EPSquare is only set if they have a pawn that can capture this one (via en passant)
                    if ((PawnAttackMasks[ourColor][moveTo + down] & bb.Colors[theirColor] & bb.Pieces[Pawn]) != 0)
                    {
                        State->EPSquare = moveTo + down;
                        State->Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                    }
                }

                //  Reset the halfmove clock
                State->HalfmoveClock = 0;
            }

            if (!move.Castle)
            {
                bb.MoveSimple(moveFrom, moveTo, ourColor, ourPiece);
                State->Hash.ZobristMove(moveFrom, moveTo, ourColor, ourPiece);
            }

            if (move.Promotion)
            {
                //  Get rid of the pawn we just put there
                bb.RemovePiece(moveTo, ourColor, ourPiece);

                //  And replace it with the promotion piece
                bb.AddPiece(moveTo, ourColor, move.PromotionTo);

                State->Hash.ZobristToggleSquare(ourColor, ourPiece, moveTo);
                State->Hash.ZobristToggleSquare(ourColor, move.PromotionTo, moveTo);

                MaterialCountNonPawn[ourColor] += GetPieceValue(move.PromotionTo);
            }

            State->Hash.ZobristChangeToMove();
            ToMove = Not(ToMove);

            State->Checkers = bb.AttackersTo(State->KingSquares[theirColor], bb.Occupancy) & bb.Colors[ourColor];
            switch (popcount(State->Checkers))
            {
                case 0:
                    InCheck = false;
                    InDoubleCheck = false;
                    idxChecker = SquareNB;
                    break;
                case 1:
                    InCheck = true;
                    InDoubleCheck = false;
                    idxChecker = lsb(State->Checkers);
                    break;
                case 2:
                    InCheck = false;
                    InDoubleCheck = true;
                    idxChecker = lsb(State->Checkers);
                    break;
            }

            SetCheckInfo();
        }

        public void UnmakeMove(Move move)
        {
            int moveFrom = move.From;
            int moveTo = move.To;

            //  Assume that "we" just made the last move, and "they" are undoing it.
            int ourPiece = bb.GetPieceAtIndex(moveTo);
            int ourColor = Not(ToMove);
            int theirColor = ToMove;

            GamePly--;

            if (move.Promotion)
            {
                //  Remove the promotion piece and replace it with a pawn
                bb.RemovePiece(moveTo, ourColor, ourPiece);

                ourPiece = Piece.Pawn;

                bb.AddPiece(moveTo, ourColor, ourPiece);

                MaterialCountNonPawn[ourColor] -= GetPieceValue(move.PromotionTo);
            }
            else if (move.Castle)
            {
                //  Put both pieces back
                DoCastling(ourColor, moveFrom, moveTo, undo: true);
            }

            if (!move.Castle)
            {
                //  Put our piece back to the square it came from.
                bb.MoveSimple(moveTo, moveFrom, ourColor, ourPiece);
            }

            if (State->CapturedPiece != Piece.None)
            {
                //  CapturedPiece is set for captures and en passant, so check which it was
                if (move.EnPassant)
                {
                    //  If the move was an en passant, put the captured pawn back

                    int idxPawn = moveTo + ShiftUpDir(ToMove);
                    bb.AddPiece(idxPawn, theirColor, Piece.Pawn);
                }
                else
                {
                    //  Otherwise it was a capture, so put the captured piece back
                    bb.AddPiece(moveTo, theirColor, State->CapturedPiece);

                    if (State->CapturedPiece != Pawn)
                    {
                        MaterialCountNonPawn[theirColor] += GetPieceValue(State->CapturedPiece);
                    }
                }
            }

            if (ourColor == Color.Black)
            {
                //  If ourColor == Color.Black (and ToMove == White), then we incremented FullMoves when this move was made.
                FullMoves--;
            }

            State--;

            switch (popcount(State->Checkers))
            {
                case 0:
                    InCheck = false;
                    InDoubleCheck = false;
                    idxChecker = SquareNB;
                    break;
                case 1:
                    InCheck = true;
                    InDoubleCheck = false;
                    idxChecker = lsb(State->Checkers);
                    break;
                case 2:
                    InCheck = false;
                    InDoubleCheck = true;
                    idxChecker = lsb(State->Checkers);
                    break;
            }


            ToMove = Not(ToMove);
        }


        /// <summary>
        /// Performs a null move, which only updates the EnPassantTarget (since it is always reset to 64 when a move is made) 
        /// and position hash accordingly.
        /// </summary>
        public void MakeNullMove()
        {
            //  Copy everything except the pointer to the accumulator, which should never change.
            Unsafe.CopyBlock(NextState, State, (uint)StateInfo.StateCopySize);

            if (UpdateNN)
            {
                NNUE.MakeNullMove(this);
            }
            
            State++;

            if (State->EPSquare != EPNone)
            {
                //  Set EnPassantTarget to 64 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset every move.
                State->Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                State->EPSquare = EPNone;
            }

            State->Hash.ZobristChangeToMove();
            ToMove = Not(ToMove);
            State->HalfmoveClock++;
            State->PliesFromNull = 0;

            SetCheckInfo();

            prefetch(TranspositionTable.GetCluster(State->Hash));

        }

        /// <summary>
        /// Undoes a null move, which returns EnPassantTarget and the hash to their previous values.
        /// </summary>
        public void UnmakeNullMove()
        {
            State--;

            ToMove = Not(ToMove);
        }

        /// <summary>
        /// Moves the king and rook for castle moves, and updates the position hash accordingly.
        /// Adapted from https://github.com/official-stockfish/Stockfish/blob/632f1c21cd271e7c4c242fdafa328a55ec63b9cb/src/position.cpp#L931
        /// </summary>
        public void DoCastling(int ourColor, int from, int to, bool undo = false)
        {
            bool kingSide = to > from;
            int rfrom = to;
            int rto = (kingSide ? Squares.F1 : Squares.D1) ^ (ourColor * 56);
            to = (kingSide ? Squares.G1 : Squares.C1) ^ (ourColor * 56);

            if (undo)
            {
                bb.RemovePiece(to, ourColor, King);
                bb.RemovePiece(rto, ourColor, Rook);

                bb.AddPiece(from, ourColor, King);
                bb.AddPiece(rfrom, ourColor, Rook);
            }
            else
            {
                bb.RemovePiece(from, ourColor, King);
                bb.RemovePiece(rfrom, ourColor, Rook);

                bb.AddPiece(to, ourColor, King);
                bb.AddPiece(rto, ourColor, Rook);

                State->Hash.ZobristMove(from, to, ourColor, Piece.King);
                State->Hash.ZobristMove(rfrom, rto, ourColor, Piece.Rook);
            }
        }






        public void SetState()
        {
            State->Checkers = bb.AttackersTo(State->KingSquares[ToMove], bb.Occupancy) & bb.Colors[Not(ToMove)];
            switch (popcount(State->Checkers))
            {
                case 0:
                    InCheck = false;
                    InDoubleCheck = false;
                    idxChecker = SquareNB;
                    break;
                case 1:
                    InCheck = true;
                    InDoubleCheck = false;
                    idxChecker = lsb(State->Checkers);
                    break;
                case 2:
                    InCheck = false;
                    InDoubleCheck = true;
                    idxChecker = lsb(State->Checkers);
                    break;
            }

            SetCheckInfo();

            State->Hash = Zobrist.GetHash(this);
        }


        public void SetCheckInfo()
        {
            State->BlockingPieces[White] = bb.BlockingPieces(White, &State->Pinners[Black]);
            State->BlockingPieces[Black] = bb.BlockingPieces(Black, &State->Pinners[White]);

            int kingSq = State->KingSquares[Not(ToMove)];

            State->CheckSquares[Pawn] = PawnAttackMasks[Not(ToMove)][kingSq];
            State->CheckSquares[Knight] = KnightMasks[kingSq];
            State->CheckSquares[Bishop] = GetBishopMoves(bb.Occupancy, kingSq);
            State->CheckSquares[Rook] = GetRookMoves(bb.Occupancy, kingSq);
            State->CheckSquares[Queen] = State->CheckSquares[Bishop] | State->CheckSquares[Rook];
            State->CheckSquares[King] = 0;
        }

        [MethodImpl(Inline)]
        public void SetCastlingStatus(int c, int rfrom)
        {
            int kfrom = bb.KingIndex(c);

            CastlingStatus cr = (c == White && kfrom < rfrom) ? CastlingStatus.WK :
                                (c == Black && kfrom < rfrom) ? CastlingStatus.BK :
                                (c == White) ?                  CastlingStatus.WQ :
                                                                CastlingStatus.BQ;

            CastlingRookSquares[(int)cr] = rfrom;

            int kto = ((cr & CastlingStatus.Kingside) != CastlingStatus.None ? G1 : C1) ^ (56 * c);
            int rto = ((cr & CastlingStatus.Kingside) != CastlingStatus.None ? F1 : D1) ^ (56 * c);

            CastlingRookPaths[(int)cr] = (LineBB[rfrom][rto] | LineBB[kfrom][kto]) & ~(SquareBB[kfrom] | SquareBB[rfrom]);

            State->CastleStatus |= cr;
        }


        /// <summary>
        /// Returns the Zobrist hash of the position after the move <paramref name="m"/> is made.
        /// <para></para>
        /// This is only for simple moves and captures: en passant and castling is not considered. 
        /// This is only used for prefetching the <see cref="TTCluster"/>, and if the move actually 
        /// is an en passant or castle then the prefetch won't end up helping anyways.
        /// </summary>
        public ulong HashAfter(Move m)
        {
            ulong hash = State->Hash;

            int from = m.From;
            int to = m.To;
            int us = bb.GetColorAtIndex(from);
            int ourPiece = bb.GetPieceAtIndex(from);

            if (bb.GetPieceAtIndex(to) != None)
            {
                Assert(bb.GetPieceAtIndex(to) != None, $"HashAfter({m}) in FEN {GetFEN()} is a capture move but {IndexToString(to)} is empty");
                hash.ZobristToggleSquare(Not(us), bb.GetPieceAtIndex(to), to);
            }

            Assert(ourPiece != None, $"HashAfter({m}) in FEN {GetFEN()} doesn't have a piece on its From square!");

            hash.ZobristMove(from, to, us, ourPiece);
            hash.ZobristChangeToMove();

            return hash;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is pseudo-legal.
        /// Only determines if there is a piece at move.From and the piece at move.To isn't the same color.
        /// </summary>
        public bool IsPseudoLegal(in Move move)
        {
            int moveTo = move.To;
            int moveFrom = move.From;

            int pt = bb.GetPieceAtIndex(moveFrom);
            if (pt == None)
            {
                //  There isn't a piece on the move's "from" square.
                return false;
            }

            int pc = bb.GetColorAtIndex(moveFrom);
            if (pc != ToMove)
            {
                //  This isn't our piece, so we can't move it.
                return false;
            }

            if (bb.GetPieceAtIndex(moveTo) != None && pc == bb.GetColorAtIndex(moveTo) && !move.Castle)
            {
                //  There is a piece on the square we are moving to, and it is ours, so we can't capture it.
                //  The one exception is castling, which is encoded as king captures rook.
                return false;
            }

            if (pt == Pawn)
            {
                if (move.EnPassant)
                {
                    return State->EPSquare != EPNone && (SquareBB[moveTo - ShiftUpDir(ToMove)] & bb.Pieces[Pawn] & bb.Colors[Not(ToMove)]) != 0;
                }

                ulong empty = ~bb.Occupancy;
                if ((moveTo ^ moveFrom) != 16)
                {
                    //  This is NOT a pawn double move, so it can only go to a square it attacks or the empty square directly above/below.
                    return (bb.AttackMask(moveFrom, pc, pt, bb.Occupancy) & SquareBB[moveTo]) != 0
                        || (empty & SquareBB[moveTo]) != 0;
                }
                else
                {
                    //  This IS a pawn double move, so it can only go to a square it attacks,
                    //  or the empty square 2 ranks above/below provided the square 1 rank above/below is also empty.
                    return (bb.AttackMask(moveFrom, pc, pt, bb.Occupancy) & SquareBB[moveTo]) != 0
                        || ((empty & SquareBB[moveTo - ShiftUpDir(pc)]) != 0 && (empty & SquareBB[moveTo]) != 0);
                }

            }

            //  This move is only pseudo-legal if the piece that is moving is actually able to get there.
            //  Pieces can only move to squares that they attack, with the one exception of queenside castling
            return (bb.AttackMask(moveFrom, pc, pt, bb.Occupancy) & SquareBB[moveTo]) != 0 || move.Castle;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal in the current position.
        /// </summary>
        public bool IsLegal(in Move move) => IsLegal(move, State->KingSquares[ToMove], State->KingSquares[Not(ToMove)], State->BlockingPieces[ToMove]);

        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the current position.
        /// </summary>
        public bool IsLegal(Move move, int ourKing, int theirKing, ulong pinnedPieces)
        {
            int moveFrom = move.From;
            int moveTo = move.To;

            int pt = bb.GetPieceAtIndex(moveFrom);

            if (pt == None)
            {
                return false;
            }

            if (InDoubleCheck && pt != Piece.King)
            {
                //  Must move king out of double check
                return false;
            }

            int ourColor = bb.GetColorAtIndex(moveFrom);
            int theirColor = Not(ourColor);

            if (InCheck)
            {
                //  We have 3 Options: block the check, take the piece giving check, or move our king out of it.

                if (pt == Piece.King)
                {
                    //  We need to move to a square that they don't attack.
                    //  We also need to consider (NeighborsMask[moveTo] & SquareBB[theirKing]), because bb.AttackersTo does NOT include king attacks
                    //  and we can't move to a square that their king attacks.
                    return ((bb.AttackersTo(moveTo, bb.Occupancy ^ SquareBB[moveFrom]) & bb.Colors[theirColor]) | (NeighborsMask[moveTo] & SquareBB[theirKing])) == 0;
                }

                int checker = idxChecker;
                if (((LineBB[ourKing][checker] & SquareBB[moveTo]) != 0)
                    || (move.EnPassant && GetIndexFile(moveTo) == GetIndexFile(checker)))
                {
                    //  This move is another piece which has moved into the LineBB between our king and the checking piece.
                    //  This will be legal as long as it isn't pinned.

                    return pinnedPieces == 0 || (pinnedPieces & SquareBB[moveFrom]) == 0;
                }

                //  This isn't a king move and doesn't get us out of check, so it's illegal.
                return false;
            }

            if (pt == Piece.King)
            {
                if (move.Castle)
                {
                    var thisCr = move.RelevantCastlingRight;
                    int rookSq = CastlingRookSquares[(int)thisCr];

                    if ((SquareBB[rookSq] & bb.Pieces[Rook] & bb.Colors[ourColor]) == 0)
                    {
                        //  There isn't a rook on the square that we are trying to castle towards.
                        return false;
                    }

                    if (IsChess960 && (State->BlockingPieces[ourColor] & SquareBB[moveTo]) != 0)
                    {
                        //  This rook was blocking a check, and it would put our king in check
                        //  once the rook and king switched places
                        return false;
                    }

                    int kingTo = (moveTo > moveFrom ? G1 : C1) ^ (ourColor * 56);
                    ulong them = bb.Colors[theirColor];
                    int dir = (moveFrom < kingTo) ? -1 : 1;
                    for (int sq = kingTo; sq != moveFrom; sq += dir)
                    {
                        if ((bb.AttackersTo(sq, bb.Occupancy) & them) != 0)
                        {
                            //  Moving here would put us in check
                            return false;
                        }
                    }

                    return ((bb.AttackersTo(kingTo, bb.Occupancy ^ SquareBB[ourKing]) & bb.Colors[theirColor])
                           | (NeighborsMask[kingTo] & SquareBB[theirKing])) == 0;
                }

                //  We can move anywhere as long as it isn't attacked by them.

                //  SquareBB[ourKing] is masked out from bb.Occupancy to prevent kings from being able to move backwards out of check,
                //  meaning a king on B1 in check from a rook on C1 can't actually go to A1.
                return ((bb.AttackersTo(moveTo, bb.Occupancy ^ SquareBB[ourKing]) & bb.Colors[theirColor])
                       | (NeighborsMask[moveTo] & SquareBB[theirKing])) == 0;
            }
            else if (move.EnPassant)
            {
                //  En passant will remove both our pawn and the opponents pawn from the rank so this needs a special check
                //  to make sure it is still legal

                int idxPawn = moveTo - ShiftUpDir(ourColor);
                ulong moveMask = SquareBB[moveFrom] | SquareBB[moveTo];

                //  This is only legal if our king is NOT attacked after the EP is made
                return (bb.AttackersTo(ourKing, bb.Occupancy ^ (moveMask | SquareBB[idxPawn])) & bb.Colors[theirColor]) == 0;
            }

            //  Otherwise, this move is legal if:
            //  The piece we are moving isn't a blocker for our king
            //  The piece is a blocker for our king, but it is moving along the same ray that it had been blocking previously.
            //  (i.e. a rook on B1 moving to A1 to capture a rook that was pinning it to our king on C1)
            return ((State->BlockingPieces[ourColor] & SquareBB[moveFrom]) == 0) ||
                   ((RayBB[moveFrom][moveTo] & SquareBB[ourKing]) != 0);
        }




        public bool IsDraw()
        {
            return IsFiftyMoveDraw() || IsInsufficientMaterial() || IsThreefoldRepetition();
        }


        /// <summary>
        /// Checks if the position is currently drawn by insufficient material.
        /// This generally only happens for KvK, KvKB, and KvKN endgames.
        /// </summary>
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
            return (knights == 0 && bishops < 2) || (bishops == 0 && knights <= 2);
        }


        /// <summary>
        /// Checks if the position is currently drawn by threefold repetition.
        /// Only considers moves made past the last time the HalfMoves clock was reset,
        /// which occurs when captures are made or a pawn moves.
        /// </summary>
        public bool IsThreefoldRepetition()
        {
            //  At least 8 moves must be made before a draw can occur.
            if (GamePly < 8)
            {
                return false;
            }

            ulong currHash = State->Hash;

            //  Beginning with the current state's Hash, step backwards in increments of 2 until reaching the first move that we made.
            //  If we encounter the current hash 2 additional times, then this is a draw.

            int count = 0;
            StateInfo* temp = State;
            for (int i = 0; i < GamePly - 1; i += 2)
            {
                if (temp->Hash == currHash)
                {
                    count++;

                    if (count == 3)
                    {
                        return true;
                    }
                }

                if ((temp - 1) == _SentinelStart || (temp - 2) == _SentinelStart)
                {
                    break;
                }

                temp -= 2;
            }
            return false;
        }

        public bool IsFiftyMoveDraw()
        {
            return State->HalfmoveClock >= 100;
        }


        /// <summary>
        /// Returns the <see cref="CastlingStatus"/> for the piece on the <paramref name="sq"/>, 
        /// which is <see cref="CastlingStatus.None"/> if the square isn't one of the values in <see cref="CastlingRookSquares"/>.
        /// </summary>
        private CastlingStatus GetCastlingForRook(int sq)
        {
            CastlingStatus cr = sq == CastlingRookSquares[(int)CastlingStatus.WQ] ? CastlingStatus.WQ :
                                sq == CastlingRookSquares[(int)CastlingStatus.WK] ? CastlingStatus.WK :
                                sq == CastlingRookSquares[(int)CastlingStatus.BQ] ? CastlingStatus.BQ :
                                sq == CastlingRookSquares[(int)CastlingStatus.BK] ? CastlingStatus.BK : 
                                                                                    CastlingStatus.None;

            return cr;
        }

        /// <summary>
        /// Updates the hash to reflect the changes in castling rights, and removes the given rights from the current state.
        /// </summary>
        private void RemoveCastling(CastlingStatus cr)
        {
            State->Hash.ZobristCastle(State->CastleStatus, cr);
            State->CastleStatus &= ~cr;
        }


        /// <summary>
        /// Returns the number of leaf nodes in the current position up to <paramref name="depth"/>.
        /// </summary>
        /// <param name="depth">The number of moves that will be made from the starting position. Depth 1 returns the current number of legal moves.</param>
        public ulong Perft(int depth)
        {
            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = GenLegal(list);

            if (depth == 1)
            {
                return (ulong)size;
            }

            ulong n = 0;
            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                MakeMove(m);
                n += Perft(depth - 1);
                UnmakeMove(m);
            }
            return n;
        }

        private Stopwatch PerftTimer = new Stopwatch();
        private const int PerftParallelMinDepth = 6;
        public ulong PerftParallel(int depth, bool isRoot = false)
        {
            if (isRoot)
            {
                PerftTimer.Restart();
            }

            //  This needs to be a pointer since a Span is a ref local and they can't be used inside of lambda functions.
            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = GenLegal(list);

            ulong n = 0;

            string rootFEN = GetFEN();

            ParallelOptions opts = new ParallelOptions();
            opts.MaxDegreeOfParallelism = MoveListSize;
            Parallel.For(0u, size, opts, i =>
            {
                Position threadPosition = new Position(rootFEN, false, owner: SearchPool.MainThread);

                threadPosition.MakeMove(list[i].Move);
                ulong result = (depth >= PerftParallelMinDepth) ? threadPosition.PerftParallel(depth - 1) : threadPosition.Perft(depth - 1);
                if (isRoot)
                {
                    Log(list[i].Move.ToString() + ": " + result);
                }
                n += result;
            });

            if (isRoot)
            {
                PerftTimer.Stop();
                Log("\r\nNodes searched:  " + n + " in " + PerftTimer.Elapsed.TotalSeconds + " s (" + ((int)(n / PerftTimer.Elapsed.TotalSeconds)).ToString("N0") + " nps)" + "\r\n");
                PerftTimer.Reset();
            }

            return n;
        }

        /// <summary>
        /// Same as perft but returns the evaluation at each of the leaves. 
        /// Only for benchmarking/debugging.
        /// </summary>
        public long PerftNN(int depth)
        {
            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = GenLegal(list);

            if (depth == 0)
            {
                return (long)NNUE.GetEvaluation(this);
            }

            long n = 0;
            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                MakeMove(m);
                n += PerftNN(depth - 1);
                UnmakeMove(m);
            }

            return n;
        }



        private static readonly char[] FENSeparators = ['/', ' '];

        /// <summary>
        /// Updates the position's Bitboard, ToMove, castling status, en passant target, and half/full move clock.
        /// </summary>
        /// <param name="fen">The FEN to set the position to</param>
        public bool LoadFromFEN(string fen)
        {
            try
            {
                string[] splits = fen.Split(FENSeparators);

                bb.Reset();
                FullMoves = 1;

                State = StartingState;
                NativeMemory.Clear(State, StateInfo.StateCopySize);
                State->CastleStatus = CastlingStatus.None;
                State->HalfmoveClock = 0;
                State->PliesFromNull = 0;

                //  TODO: set GamePly to 0 here?
                GamePly = 0;

                for (int i = 0; i < splits.Length; i++)
                {
                    //  it's a row on the board
                    if (i <= 7)
                    {
                        int pieceX = 0;
                        for (int x = 0; x < splits[i].Length; x++)
                        {
                            if (char.IsLetter(splits[i][x]))
                            {
                                int idx = CoordToIndex(pieceX, 7 - i);
                                int pc = char.IsUpper(splits[i][x]) ? White : Black;
                                int pt = FENToPiece(splits[i][x]);

                                bb.AddPiece(idx, pc, pt);

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
                    //  who moves next
                    else if (i == 8)
                    {
                        ToMove = splits[i].Equals("w") ? Color.White : Color.Black;
                    }
                    //  castling availability
                    else if (i == 9)
                    {
                        State->CastleStatus = CastlingStatus.None;

                        foreach (char ch in splits[i])
                        {
                            int rsq = SquareNB;
                            int color = char.IsUpper(ch) ? White : Black;
                            char upper = char.ToUpper(ch);

                            if (upper == 'K')
                            {
                                for (rsq = (H1 ^ (56 * color)); bb.GetPieceAtIndex(rsq) != Rook; --rsq) { }
                            }
                            else if (upper == 'Q')
                            {
                                for (rsq = (A1 ^ (56 * color)); bb.GetPieceAtIndex(rsq) != Rook; ++rsq) { }
                            }
                            else if (upper >= 'A' && upper <= 'H')
                            {
                                IsChess960 = true;

                                rsq = CoordToIndex((int)upper - 'A', (0 ^ (color * 7)));
                            }
                            else
                            {
                                continue;
                            }

                            SetCastlingStatus(color, rsq);
                        }
                    }
                    //  en passant target or last double pawn move
                    else if (i == 10)
                    {
                        if (!splits[i].Contains('-'))
                        {
                            //  White moved a pawn last
                            if (splits[i][1].Equals('3'))
                            {
                                State->EPSquare = StringToIndex(splits[i]);
                            }
                            else if (splits[i][1].Equals('6'))
                            {
                                State->EPSquare = StringToIndex(splits[i]);
                            }
                        }
                    }
                    //  halfmove number
                    else if (i == 11)
                    {
                        if (int.TryParse(splits[i], out int halfMoves))
                        {
                            State->HalfmoveClock = halfMoves;
                        }
                    }
                    //  fullmove number
                    else if (i == 12)
                    {
                        if (int.TryParse(splits[i], out int fullMoves))
                        {
                            FullMoves = fullMoves;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Failed parsing '" + fen + "': ");
                Log(ex.ToString());

                return false;
            }

            State->KingSquares[White] = bb.KingIndex(White);
            State->KingSquares[Black] = bb.KingIndex(Black);

            SetState();

            State->CapturedPiece = None;

            MaterialCountNonPawn[Color.White] = bb.MaterialCount(Color.White, true);
            MaterialCountNonPawn[Color.Black] = bb.MaterialCount(Color.Black, true);

            if (UpdateNN)
            {
                NNUE.RefreshAccumulator(this);
            }

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
                    int pc = bb.GetColorAtIndex(index);
                    int pt = bb.GetPieceAtIndex(index);

                    if (pt != None)
                    {
                        if (i != 0)
                        {
                            fen.Append(i);
                            i = 0;
                        }

                        char c = PieceToFENChar(pt);
                        fen.Append(pc == White ? char.ToUpper(c) : 
                                                 char.ToLower(c));

                        continue;
                    }
                    else
                    {
                        i++;
                    }

                    if (x == 7)
                        fen.Append(i);
                }

                if (y != 0)
                    fen.Append('/');
            }

            fen.Append(ToMove == Color.White ? " w " : " b ");

            if (State->CastleStatus != CastlingStatus.None)
            {
                if (State->CastleStatus.HasFlag(CastlingStatus.WK))
                {
                    fen.Append(IsChess960 ? (char)('A' + GetIndexFile(CastlingRookSquares[(int)CastlingStatus.WK])) : 'K');
                }
                if (State->CastleStatus.HasFlag(CastlingStatus.WQ))
                {
                    fen.Append(IsChess960 ? (char)('A' + GetIndexFile(CastlingRookSquares[(int)CastlingStatus.WQ])) : 'Q');
                }
                if (State->CastleStatus.HasFlag(CastlingStatus.BK))
                {
                    fen.Append(IsChess960 ? (char)('a' + GetIndexFile(CastlingRookSquares[(int)CastlingStatus.BK])) : 'k');
                }
                if (State->CastleStatus.HasFlag(CastlingStatus.BQ))
                {
                    fen.Append(IsChess960 ? (char)('a' + GetIndexFile(CastlingRookSquares[(int)CastlingStatus.BQ])) : 'q');
                }
            }
            else
            {
                fen.Append('-');
            }

            if (State->EPSquare != EPNone)
            {
                fen.Append(" " + IndexToString(State->EPSquare));
            }
            else
            {
                fen.Append(" -");
            }

            fen.Append(" " + State->HalfmoveClock + " " + FullMoves);

            return fen.ToString();
        }

        public override string ToString()
        {
            return PrintBoard(bb);
        }

    }
}
