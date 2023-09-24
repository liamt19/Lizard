using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;

using LTChess.Logic.Data;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.Simple768;

namespace LTChess.Logic.Core
{
    public unsafe partial class Position
    {
        /// <summary>
        /// This Position's <see cref="Bitboard"/>, which contains masks for each type of piece and player color.
        /// </summary>
        public Bitboard bb;

        /// <summary>
        /// The address of <see cref="bb"/>'s memory block, since C# doesn't like taking the address of structs.
        /// </summary>
        private readonly nint _bbBlock;

        /// <summary>
        /// The first number in the FEN, which starts at 0 and resets to 0 every time a pawn moves or a piece is captured.
        /// If this reaches 100, the game is a draw by the 50-move rule.
        /// </summary>
        public int HalfMoves = 0;

        /// <summary>
        /// The second number in the FEN, which starts at 1 and increases every time black moves.
        /// </summary>
        public int FullMoves = 1;

        public CastlingStatus Castling;

        public CheckInfo CheckInfo;


        /// <summary>
        /// Set to the color of the player whose turn it is to move.
        /// </summary>
        public int ToMove;

        /// <summary>
        /// Current zobrist hash of the position.
        /// </summary>
        public ulong Hash;

        /// <summary>
        /// The sum of all material in the position, indexed by piece color.
        /// </summary>
        public int[] MaterialCount;

        /// <summary>
        /// The sum of the non-pawn material in the position, indexed by piece color.
        /// </summary>
        public int[] MaterialCountNonPawn;

        public bool Checked => (CheckInfo.InCheck || CheckInfo.InDoubleCheck);

        /// <summary>
        /// The number of <see cref="StateInfo"/> items that memory will be allocated for within the StateStack, which is 256 KB.
        /// If you find yourself in a game exceeding 2047 moves, go outside.
        /// </summary>
        private const int StateStackSize = 2048;
        private StateInfo* StateStack;
        private nint _stateBlock;


        /// <summary>
        /// A pointer to the beginning of the StateStack, which is used to make sure we don't try to access the StateStack at negative indices.
        /// </summary>
        private readonly StateInfo* _SentinelStart;

        /// <summary>
        /// A pointer to this Position's current <see cref="StateInfo"/> object, which corresponds to the StateStack[GamePly]
        /// </summary>
        public StateInfo* State;

        /// <summary>
        /// The number of moves that have been made so far since this Position was created.
        /// <br></br>
        /// Only used to easily keep track of how far along the StateStack we currently are.
        /// </summary>
        private int GamePly = 0;

        /// <summary>
        /// Creates a new Position object, initializes it's internal FasterStack's and Bitboard, and loads the provided FEN.
        /// </summary>
        public Position(string fen = InitialFEN, bool ResetNN = true)
        {
            MaterialCount = new int[2];
            MaterialCountNonPawn = new int[2];


            _bbBlock = (nint) AlignedAllocZeroed((nuint)(sizeof(Bitboard) * 1), AllocAlignment);
            this.bb = *(Bitboard*)_bbBlock;

            _stateBlock = (nint) AlignedAllocZeroed((nuint)(sizeof(StateInfo) * StateStackSize), AllocAlignment);
            StateStack = (StateInfo*)_stateBlock;

            _SentinelStart = &StateStack[0];
            State = &StateStack[0];

            LoadFromFEN(fen);

            if (UseSimple768 && ResetNN)
            {
                NNUEEvaluation.RefreshNN(this);
                NNUEEvaluation.ResetNN();
            }

            if (UseHalfKA && ResetNN)
            {
                HalfKA_HM.RefreshNN();
                HalfKA_HM.ResetNN();
            }
        }


        /// <summary>
        /// Free the memory block that we allocated for this Position's <see cref="Bitboard"/>
        /// </summary>
        ~Position()
        {
            NativeMemory.AlignedFree((void*)_bbBlock);
            NativeMemory.AlignedFree((void*)_stateBlock);
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
                if (m.ToString(this).ToLower().Equals(moveStr.ToLower()) || m.ToString().ToLower().Equals(moveStr.ToLower()))
                {
                    MakeMove(m, false);
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
        /// Generates the legal moves in the current position and makes the move that corresponds to the <paramref name="moveStr"/> if one exists.
        /// </summary>
        /// <param name="moveStr">The Algebraic notation for a move i.e. Nxd4+, or the Smith notation i.e. e2e4</param>
        /// <returns>True if <paramref name="moveStr"/> was a recognized and legal move.</returns>
        public bool TryFindMove(string moveStr, out Move move)
        {
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = GenAllLegalMovesTogether(list);
            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                if (m.ToString(this).ToLower().Equals(moveStr.ToLower()) || m.ToString().ToLower().Equals(moveStr.ToLower()))
                {
                    move = m;
                    return true;
                }
                if (i == size - 1)
                {
                    Log("No move '" + moveStr + "' found, try one of the following: ");
                    Log(list.Stringify(this) + "\r\n" + list.Stringify());
                }
            }

            move = Move.Null;
            return false;
        }

        /// <summary>
        /// Copies the current state into the <paramref name="newState"/>, then performs the move <paramref name="move"/>.
        /// </summary>
        /// <param name="move">The move to make, which needs to be a legal move or strange things might happen.</param>
        /// <param name="MakeMoveNN">If true, updates the NNUE networks.</param>
        [MethodImpl(Inline)]
        public void MakeMove(Move move, bool MakeMoveNN = true)
        {
            //  Copy the current state into the next one
            Unsafe.CopyBlockUnaligned((State + 1), State, (uint)sizeof(StateInfo));

            //  Move onto the next state
            State++;

            State->HalfmoveClock++;
            GamePly++;

            if (UseSimple768 && MakeMoveNN)
            {
                NNUEEvaluation.MakeMoveNN(this, move);
            }
            else if (UseHalfKA && MakeMoveNN)
            {
                HalfKA_HM.MakeMove(this, move);
            }


            if (ToMove == Color.Black)
            {
                FullMoves++;
            }

            int moveFrom = move.From;
            int moveTo = move.To;

            int ourPiece = bb.GetPieceAtIndex(moveFrom);
            int ourColor = bb.GetColorAtIndex(moveFrom);

            int theirPiece = bb.GetPieceAtIndex(moveTo);
            int theirColor = Not(ourColor);

#if DEBUG
            if (ourPiece == Piece.None)
            {
                Debug.Assert(false, "Move " + move.ToString() + " doesn't have a piece on the From square!");
            }
            if (theirPiece != Piece.None && ourColor == bb.GetColorAtIndex(moveTo))
            {
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture our own " + PieceToString(theirPiece) + " on " + IndexToString(moveTo));
            }
            if (theirPiece == Piece.King)
            {
                Debug.Assert(false, "Move " + move.ToString(this) + " is trying to capture " + ColorToString(bb.GetColorAtIndex(moveTo)) + "'s king on " + IndexToString(moveTo));
            }
#endif

            if (ourPiece == Piece.King)
            {
                if (move.Castle)
                {
                    //  Move our rook and update the hash
                    switch (moveTo)
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
                }

                //  Remove all of our castling rights
                if (ourColor == Color.White)
                {
                    Hash = Hash.ZobristCastle(State->CastleStatus, (CastlingStatus.WK | CastlingStatus.WQ));
                    State->CastleStatus &= ~(CastlingStatus.WK | CastlingStatus.WQ);
                }
                else
                {
                    Hash = Hash.ZobristCastle(State->CastleStatus, (CastlingStatus.BK | CastlingStatus.BQ));
                    State->CastleStatus &= ~(CastlingStatus.BK | CastlingStatus.BQ);
                }
            }
            else if (ourPiece == Piece.Rook && State->CastleStatus != CastlingStatus.None)
            {
                //  If we just moved a rook, update st->CastleStatus
                switch (moveFrom)
                {
                    case A1:
                        Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.WQ);
                        State->CastleStatus &= ~CastlingStatus.WQ;
                        break;
                    case H1:
                        Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.WK);
                        State->CastleStatus &= ~CastlingStatus.WK;
                        break;
                    case A8:
                        Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.BQ);
                        State->CastleStatus &= ~CastlingStatus.BQ;
                        break;
                    case H8:
                        Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.BK);
                        State->CastleStatus &= ~CastlingStatus.BK;
                        break;
                }
            }

            if (move.Capture)
            {
                //  Remove their piece, and update the hash
                bb.RemovePiece(moveTo, theirColor, theirPiece);
                Hash = Hash.ZobristToggleSquare(theirColor, theirPiece, moveTo);

                if (theirPiece == Piece.Rook)
                {
                    //  If we are capturing a rook, make sure that if that we remove that castling status from them if necessary.
                    switch (moveTo)
                    {
                        case A1:
                            Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.WQ);
                            State->CastleStatus &= ~CastlingStatus.WQ;
                            break;
                        case H1:
                            Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.WK);
                            State->CastleStatus &= ~CastlingStatus.WK;
                            break;
                        case A8:
                            Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.BQ);
                            State->CastleStatus &= ~CastlingStatus.BQ;
                            break;
                        case H8:
                            Hash = Hash.ZobristCastle(State->CastleStatus, CastlingStatus.BK);
                            State->CastleStatus &= ~CastlingStatus.BK;
                            break;
                    }
                }

                MaterialCount[theirColor] -= GetPieceValue(theirPiece);

                if (theirPiece != Pawn)
                {
                    MaterialCountNonPawn[theirColor] -= GetPieceValue(theirPiece);
                }

                State->CapturedPiece = theirPiece;

                //  Reset the halfmove clock
                State->HalfmoveClock = 0;
            }
            else
            {
                State->CapturedPiece = None;
            }


            int tempEPSquare = State->EPSquare;

            if (State->EPSquare != SquareNB)
            {
                //  Set st->EPSquare to 64 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset every move.
                Hash = Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                State->EPSquare = SquareNB;
            }

            if (ourPiece == Piece.Pawn)
            {
                if (move.EnPassant)
                {
                    Debug.Assert(tempEPSquare != SquareNB);
                    int idxPawn = ((bb.Pieces[Piece.Pawn] & SquareBB[tempEPSquare - 8]) != 0) ? tempEPSquare - 8 : tempEPSquare + 8;
                    bb.RemovePiece(idxPawn, theirColor, Piece.Pawn);
                    Hash = Hash.ZobristToggleSquare(theirColor, Piece.Pawn, idxPawn);

                    //  The EnPassant/Capture flags are mutually exclusive, so set CapturedPiece here
                    State->CapturedPiece = Piece.Pawn;

                    MaterialCount[theirColor] -= GetPieceValue(Piece.Pawn);
                }
                else if ((moveTo ^ moveFrom) == 16)
                {
                    //  st->EPSquare is only set if they have a pawn that can capture this one (via en passant)
                    if (ourColor == Color.White && (WhitePawnAttackMasks[moveTo - 8] & (bb.Colors[Color.Black] & bb.Pieces[Piece.Pawn])) != 0)
                    {
                        State->EPSquare = moveTo - 8;
                    }
                    else if (ourColor == Color.Black && (BlackPawnAttackMasks[moveTo + 8] & (bb.Colors[Color.White] & bb.Pieces[Piece.Pawn])) != 0)
                    {
                        State->EPSquare = moveTo + 8;
                    }

                    if (State->EPSquare != SquareNB)
                    {
                        //  Update the En Passant file if we just changed st->EPSquare
                        Hash = Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                    }
                }

                //  Reset the halfmove clock
                State->HalfmoveClock = 0;
            }


            bb.MoveSimple(moveFrom, moveTo, ourColor, ourPiece);
            Hash = Hash.ZobristMove(moveFrom, moveTo, ourColor, ourPiece);


            if (move.Promotion)
            {
                //  Get rid of the pawn we just put there
                bb.RemovePiece(moveTo, ourColor, ourPiece);

                //  And replace it with the promotion piece
                bb.AddPiece(moveTo, ourColor, move.PromotionTo);

                Hash = Hash.ZobristToggleSquare(ourColor, ourPiece, moveFrom);
                Hash = Hash.ZobristToggleSquare(ourColor, move.PromotionTo, moveTo);

                MaterialCount[ourColor] -= GetPieceValue(Piece.Pawn);
                MaterialCount[ourColor] += GetPieceValue(move.PromotionTo);
                MaterialCountNonPawn[ourColor] += GetPieceValue(move.PromotionTo);
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
            }
            else
            {
                //	We are either getting out of a check, or weren't in check at all
                CheckInfo.InCheck = false;
                CheckInfo.idxChecker = LSBEmpty;
                CheckInfo.InDoubleCheck = false;
            }

            Hash = Hash.ZobristChangeToMove();
            ToMove = Not(ToMove);

            State->Hash = Hash;
            if (move.Checks)
            {
                State->Checkers = bb.AttackersTo(bb.KingIndex(theirColor), bb.Occupancy) & bb.Colors[ourColor];
            }
            else
            {
                State->Checkers = 0;
            }

            SetCheckInfo();
        }


        [MethodImpl(Inline)]
        public void UnmakeMove(Move move, bool UnmakeMoveNN = true)
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

                MaterialCount[ourColor] -= GetPieceValue(move.PromotionTo);
                MaterialCount[ourColor] += GetPieceValue(Piece.Pawn);

                MaterialCountNonPawn[ourColor] -= GetPieceValue(move.PromotionTo);
            }
            else if (move.Castle)
            {
                //  Put the rook back
                if (moveTo == C1)
                {
                    bb.MoveSimple(D1, A1, ourColor, Piece.Rook);
                }
                else if (moveTo == G1)
                {
                    bb.MoveSimple(F1, H1, ourColor, Piece.Rook);
                }
                else if (moveTo == C8)
                {
                    bb.MoveSimple(D8, A8, ourColor, Piece.Rook);
                }
                else
                {
                    //  moveTo == G8
                    bb.MoveSimple(F8, H8, ourColor, Piece.Rook);
                }

            }

            //  Put our piece back to the square it came from.
            bb.MoveSimple(moveTo, moveFrom, ourColor, ourPiece);

            if (State->CapturedPiece != Piece.None)
            {
                //  CapturedPiece is set for captures and en passant, so check which it was
                if (move.EnPassant)
                {
                    //  If the move was an en passant, put the captured pawn back

                    int idxPawn = moveTo + ShiftUpDir(ToMove);
                    bb.AddPiece(idxPawn, theirColor, Piece.Pawn);

                    MaterialCount[theirColor] += GetPieceValue(Piece.Pawn);
                }
                else
                {
                    //  Otherwise it was a capture, so put the captured piece back
                    bb.AddPiece(moveTo, theirColor, State->CapturedPiece);

                    MaterialCount[theirColor] += GetPieceValue(State->CapturedPiece);
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
            this.Hash = State->Hash;

            switch (popcount(State->Checkers))
            {
                case 0:
                    CheckInfo.InCheck = false;
                    CheckInfo.InDoubleCheck = false;
                    CheckInfo.idxChecker = LSBEmpty;
                    break;
                case 1:
                    CheckInfo.InCheck = true;
                    CheckInfo.InDoubleCheck = false;
                    CheckInfo.idxChecker = lsb(State->Checkers);
                    break;
                case 2:
                    CheckInfo.InCheck = false;
                    CheckInfo.InDoubleCheck = true;
                    CheckInfo.idxChecker = lsb(State->Checkers);
                    break;
            }


            ToMove = Not(ToMove);

            if (UseSimple768 && UnmakeMoveNN)
            {
                NNUEEvaluation.UnmakeMoveNN();
            }

            if (UseHalfKA && UnmakeMoveNN)
            {
                HalfKA_HM.UnmakeMoveNN();
            }
        }


        /// <summary>
        /// Performs a null move, which only updates the EnPassantTarget (since it is always reset to 64 when a move is made) 
        /// and position hash accordingly.
        /// </summary>
        [MethodImpl(Inline)]
        public void MakeNullMove()
        {
            Unsafe.CopyBlockUnaligned((State + 1), State, (uint)sizeof(StateInfo));
            State++;


            if (State->EPSquare != SquareNB)
            {
                //  Set EnPassantTarget to 64 now.
                //  If we are capturing en passant, move.EnPassant is true. In any case it should be reset every move.
                Hash = Hash.ZobristEnPassant(GetIndexFile(State->EPSquare));
                State->EPSquare = SquareNB;
            }

            Hash = Hash.ZobristChangeToMove();
            ToMove = Not(ToMove);
            State->Hash = Hash;
            State->HalfmoveClock++;

            SetCheckInfo();

            prefetch(Unsafe.AsPointer(ref TranspositionTable.GetCluster(Hash)));

        }

        /// <summary>
        /// Undoes a null move, which returns EnPassantTarget and the hash to their previous values.
        /// </summary>
        [MethodImpl(Inline)]
        public void UnmakeNullMove()
        {
            State--;

            Hash = State->Hash;
            ToMove = Not(ToMove);

        }








        [MethodImpl(Inline)]
        public void SetState()
        {
            State->Checkers = bb.AttackersTo(bb.KingIndex(ToMove), bb.Occupancy) & bb.Colors[Not(ToMove)];

            SetCheckInfo();

            State->Hash = Zobrist.GetHash(this);
        }


        [MethodImpl(Inline)]
        public void SetCheckInfo()
        {
            State->BlockingPieces[White] = bb.BlockingPieces(White, &State->Pinners[Black], &State->Xrays[Black]);
            State->BlockingPieces[Black] = bb.BlockingPieces(Black, &State->Pinners[White], &State->Xrays[White]);

            int kingSq = bb.KingIndex(Not(ToMove));

            State->CheckSquares[Pawn]   = PawnAttackMasks[Not(ToMove)][kingSq];
            State->CheckSquares[Knight] = KnightMasks[kingSq];
            State->CheckSquares[Bishop] = GetBishopMoves(bb.Occupancy, kingSq);
            State->CheckSquares[Rook]   = GetRookMoves(bb.Occupancy, kingSq);
            State->CheckSquares[Queen]  = (State->CheckSquares[Bishop] | State->CheckSquares[Rook]);
            State->CheckSquares[King]   = 0;
        }



        /// <summary>
        /// Returns the Zobrist hash of the position after the move <paramref name="m"/> is made.
        /// <para></para>
        /// This is only for simple moves and captures: en passant and castling is not considered. 
        /// This is only used for prefetching the <see cref="TTCluster"/>, and if the move actually 
        /// is an en passant or castle then the prefetch won't end up helping anyways.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong HashAfter(Move m)
        {
            ulong hash = Hash.ZobristChangeToMove();
            int from = m.From;
            int to = m.To;
            int us = bb.GetColorAtIndex(from);
            int ourPiece = bb.GetPieceAtIndex(from);

            if (m.Capture)
            {
                hash = hash.ZobristToggleSquare(Not(us), bb.GetPieceAtIndex(to), to);
            }
            hash = hash.ZobristMove(from, to, us, ourPiece);

            return hash;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal in the current position.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsLegal(in Move move) => IsLegal(move, bb.KingIndex(ToMove), bb.KingIndex(Not(ToMove)), State->BlockingPieces[ToMove]);


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is legal given the current position.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsLegal(Move move, int ourKing, int theirKing, ulong pinnedPieces)
        {
            int moveFrom = move.From;
            int moveTo = move.To;

            int pt = bb.GetPieceAtIndex(moveFrom);
            if (CheckInfo.InDoubleCheck && pt != Piece.King)
            {
                //	Must move king out of double check
                return false;
            }

            Debug.Assert(move.Capture == false || (move.Capture == true && bb.GetPieceAtIndex(moveTo) != Piece.None),
                "ERROR IsLegal(" + move.ToString() + " = " + move.ToString(this) + ") is trying to capture a piece on an empty square!");

            int ourColor = bb.GetColorAtIndex(moveFrom);
            int theirColor = Not(ourColor);

            if (CheckInfo.InCheck)
            {
                //  We have 3 Options: block the check, take the piece giving check, or move our king out of it.

                if (pt == Piece.King)
                {
                    //  Either move out or capture the piece
                    ulong moveMask = (SquareBB[moveFrom] | SquareBB[moveTo]);
                    bb.Pieces[Piece.King] ^= moveMask;
                    bb.Colors[ourColor] ^= moveMask;
                    if (((bb.AttackersTo(moveTo, bb.Occupancy) & bb.Colors[theirColor]) | (NeighborsMask[moveTo] & SquareBB[theirKing])) != 0)
                    {
                        bb.Pieces[Piece.King] ^= moveMask;
                        bb.Colors[ourColor] ^= moveMask;
                        return false;
                    }

                    bb.Pieces[Piece.King] ^= moveMask;
                    bb.Colors[ourColor] ^= moveMask;
                    return true;
                }

                int checker = CheckInfo.idxChecker;
                bool blocksOrCaptures = (LineBB[ourKing][checker] & SquareBB[moveTo]) != 0;

                if (blocksOrCaptures || (move.EnPassant && GetIndexFile(moveTo) == GetIndexFile(CheckInfo.idxChecker)))
                {
                    //  This move is another piece which has moved into the LineBB between our king and the checking piece.
                    //  This will be legal as long as it isn't pinned.

                    return (pinnedPieces == 0 || (pinnedPieces & SquareBB[moveFrom]) == 0);
                }

                //  This isn't a king move and doesn't get us out of check, so it's illegal.
                return false;
            }

            if (pt == Piece.King)
            {
                //  We can move anywhere as long as it isn't attacked by them.

                //  SquareBB[ourKing] is masked out from bb.Occupancy to prevent kings from being able to move backwards out of check,
                //  meaning a king on B1 in check from a rook on C1 can't actually go to A1.
                return ((bb.AttackersTo(moveTo, (bb.Occupancy ^ SquareBB[ourKing])) & bb.Colors[theirColor]) 
                       | (NeighborsMask[moveTo] & SquareBB[theirKing])) == 0;
            }
            else if (move.EnPassant)
            {
                //  En passant will remove both our pawn and the opponents pawn from the rank so this needs a special check
                //  to make sure it is still legal

                int idxPawn = moveTo - ShiftUpDir(ourColor);

                ulong moveMask = (SquareBB[moveFrom] | SquareBB[moveTo]);
                bb.Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxPawn]);
                bb.Colors[ourColor] ^= moveMask;
                bb.Colors[theirColor] ^= (SquareBB[idxPawn]);

                if ((bb.AttackersTo(ourKing, bb.Occupancy) & bb.Colors[Not(ourColor)]) != 0)
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

            //  Otherwise, this move is legal if:
            //  The piece we are moving isn't a blocker for our king
            //  The piece is a blocker for our king, but it is moving along the same ray that it had been blocking previously.
            //  (i.e. a rook on B1 moving to A1 to capture a rook that was pinning it to our king on C1)
            return ((State->BlockingPieces[ourColor] & SquareBB[moveFrom]) == 0) ||
                   ((RayBB[moveFrom][moveTo] & SquareBB[ourKing]) != 0);
        }




        [MethodImpl(Inline)]
        public bool IsDraw()
        {
            return (IsFiftyMoveDraw() || IsInsufficientMaterial() || IsThreefoldRepetition());
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
            return ((knights == 0 && bishops < 2) || (bishops == 0 && knights <= 2));
        }


        /// <summary>
        /// Checks if the position is currently drawn by threefold repetition.
        /// Only considers moves made past the last time the HalfMoves clock was reset,
        /// which occurs when captures are made or a pawn moves.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsThreefoldRepetition()
        {
            //  At least 8 moves must be made before a draw can occur.
            if (GamePly < LowestRepetitionCount)
            {
                return false;
            }

            int count = 0;
            ulong currHash = State->Hash;

#if DEBUG
            StateInfo* dbg = State;
            List<ulong> Hashes = new List<ulong>(State->HalfmoveClock);
            for (int i = 0; i < GamePly; i++)
            {
                Hashes.Add(dbg->Hash);
                if (dbg == _SentinelStart)
                {
                    break;
                }
                dbg = (dbg - 1);
            }
#endif

            //  Beginning with the current state's Hash, step backwards in increments of 2 until reaching the first move that we made.
            //  If we encounter the current hash 2 additional times, then this is a draw.
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

                if ((temp - 1) == _SentinelStart || (temp - 2) == _SentinelStart) {
                    break;
                }

#if DEBUG
                if ((temp - 1) == null || !StateInfo.PointerValid((temp - 1)))
                {
                    Log(StateInfo.StringFormat((temp - 1)) + "'s Previous was freed while it was in use by " + StateInfo.StringFormat(State));
                    break;
                }

                if ((temp - 2) == null || !StateInfo.PointerValid((temp - 2)))
                {
                    Log(StateInfo.StringFormat((temp - 1)) + "'s Previous->Previous was freed while it was in use by " + StateInfo.StringFormat(State));
                    break;
                }
#endif

                temp = (temp - 2);
            }
            return false;
        }

        [MethodImpl(Inline)]
        public bool IsFiftyMoveDraw()
        {
            return State->HalfmoveClock >= 100;
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

            if (depth == 1)
            {
                return (ulong)size;
            }

            ulong n = 0;
            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                MakeMove(m, false);
                n += Perft(depth - 1);
                UnmakeMove(m, false);
            }
            return n;
        }

        /// <summary>
        /// Same as perft but returns the evaluation at each of the leaves. 
        /// Only for benchmarking/debugging.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong PerftNN(int depth)
        {
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = GenAllLegalMovesTogether(list);

            if (depth == 0)
            {
                return (ulong)HalfKA_HM.GetEvaluation(this);
            }

            ulong n = 0;
            for (int i = 0; i < size; i++)
            {
                Move m = list[i];
                MakeMove(m, true);
                n += PerftNN(depth - 1);
                UnmakeMove(m, true);
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
                Position threadPosition = new Position(rootFEN, false);
                pn.root = mlist[i].ToString();
                threadPosition.MakeMove(mlist[i], false);
                pn.number = threadPosition.Perft(depth - 1);

                list.Add(pn);
            });

            return list;
        }

        /// <summary>
        /// Updates the position's Bitboard, ToMove, castling status, en passant target, and half/full move clock.
        /// </summary>
        /// <param name="fen">The FEN to set the position to</param>
        public bool LoadFromFEN(string fen)
        {
            try
            {
                string[] splits = fen.Split(new char[] { '/', ' ' });

                bb.Reset();
                Castling = CastlingStatus.None;
                HalfMoves = 0;
                FullMoves = 1;

                State->CastleStatus = CastlingStatus.None;
                State->HalfmoveClock = 0;

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
                            State->CastleStatus = CastlingStatus.None;
                        }
                        else
                        {
                            Castling |= splits[i].Contains("K") ? CastlingStatus.WK : 0;
                            Castling |= splits[i].Contains("Q") ? CastlingStatus.WQ : 0;
                            Castling |= splits[i].Contains("k") ? CastlingStatus.BK : 0;
                            Castling |= splits[i].Contains("q") ? CastlingStatus.BQ : 0;

                            State->CastleStatus |= splits[i].Contains("K") ? CastlingStatus.WK : 0;
                            State->CastleStatus |= splits[i].Contains("Q") ? CastlingStatus.WQ : 0;
                            State->CastleStatus |= splits[i].Contains("k") ? CastlingStatus.BK : 0;
                            State->CastleStatus |= splits[i].Contains("q") ? CastlingStatus.BQ : 0;
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
                                State->EPSquare = StringToIndex(splits[i]);
                            }
                            else if (splits[i][1].Equals('6'))
                            {
                                State->EPSquare = StringToIndex(splits[i]);
                            }

                            
                        }
                    }
                    //	halfmove number
                    else if (i == 11)
                    {
                        HalfMoves = int.Parse(splits[i]);
                        State->HalfmoveClock = int.Parse(splits[i]);
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

                return false;
            }


            this.bb.DetermineCheck(ToMove, ref CheckInfo);
            Hash = Zobrist.GetHash(this);

            SetState();

            State->CapturedPiece = None;

            MaterialCount[Color.White] = bb.MaterialCount(Color.White);
            MaterialCount[Color.Black] = bb.MaterialCount(Color.Black);

            MaterialCountNonPawn[White] = MaterialCount[White] - (GetPieceValue(Pawn) * (int)popcount(bb.Colors[White] & bb.Pieces[Pawn]));
            MaterialCountNonPawn[Black] = MaterialCount[Black] - (GetPieceValue(Pawn) * (int)popcount(bb.Colors[Black] & bb.Pieces[Pawn]));

            if (UseHalfKA && popcount(bb.Occupancy) > HalfKA_HM.MaxActiveDimensions)
            {
                throw new IndexOutOfRangeException("ERROR FEN '" + fen + "' has more than " + HalfKA_HM.MaxActiveDimensions + " pieces, which isn't allowed with the HalfKA architecture!");
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
            if (State->CastleStatus.HasFlag(CastlingStatus.WK))
            {
                fen.Append("K");
                CanC = true;
            }
            if (State->CastleStatus.HasFlag(CastlingStatus.WQ))
            {
                fen.Append("Q");
                CanC = true;
            }
            if (State->CastleStatus.HasFlag(CastlingStatus.BK))
            {
                fen.Append("k");
                CanC = true;
            }
            if (State->CastleStatus.HasFlag(CastlingStatus.BQ))
            {
                fen.Append("q");
                CanC = true;
            }
            if (!CanC)
            {
                fen.Append("-");
            }
            if (State->EPSquare != SquareNB)
            {
                fen.Append(" " + IndexToString(State->EPSquare));
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
