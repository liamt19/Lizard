namespace LTChess.Logic.Core
{
    /// <summary>
    /// Manages the bitboards for a position, which are 64-bit number arrays for each piece type and color.
    /// <para></para>
    /// This implementation uses 6 ulongs for the 6 piece types, and 2 for White/Black. 
    /// This struct also has an array for the piece type that exists on each square.
    /// </summary>
    public unsafe struct Bitboard
    {
        /// <summary>
        /// Bitboard array for Pieces, from Piece.Pawn to Piece.King
        /// </summary>
        public fixed ulong Pieces[6];

        /// <summary>
        /// Bitboard array for Colors, from Color.White to Color.Black
        /// </summary>
        public fixed ulong Colors[2];

        /// <summary>
        /// Piece array indexed by square
        /// </summary>
        public fixed int PieceTypes[64];

        public Bitboard()
        {
            Reset();
        }

        public ulong Occupancy => Colors[Color.White] | Colors[Color.Black];

        public string SquareToString(int idx)
        {
            return ColorToString(GetColorAtIndex(idx)) + " " +
                   PieceToString(PieceTypes[idx]) + " on " +
                   IndexToString(idx);
        }

        /// <summary>
        /// 0's the Piece and Color arrays and fills the PieceType array with Piece.None .
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < PieceNB; i++)
            {
                Pieces[i] = 0UL;
            }

            for (int i = 0; i < ColorNB; i++)
            {
                Colors[i] = 0UL;
            }

            for (int i = 0; i < SquareNB; i++)
            {
                PieceTypes[i] = Piece.None;
            }
        }

        /// <summary>
        /// Returns true if White or Black has a piece on <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public bool Occupied(int idx)
        {
            return PieceTypes[idx] != Piece.None;
        }

        /// <summary>
        /// Adds a piece of type <paramref name="pt"/> and color <paramref name="pc"/> on the square <paramref name="idx"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void AddPiece(int idx, int pc, int pt)
        {
            PieceTypes[idx] = pt;

            Debug.Assert((Colors[pc] & SquareBB[idx]) == 0, ColorToString(pc) + " already has a piece on the square " + IndexToString(idx));
            Debug.Assert((Pieces[pt] & SquareBB[idx]) == 0, "A " + PieceToString(pt) + " already exists on the square " + IndexToString(idx));

            Colors[pc] ^= SquareBB[idx];
            Pieces[pt] ^= SquareBB[idx];
        }

        /// <summary>
        /// Removes the piece of type <paramref name="pt"/> and color <paramref name="pc"/> on the square <paramref name="idx"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void RemovePiece(int idx, int pc, int pt)
        {
            PieceTypes[idx] = Piece.None;

            Debug.Assert((Colors[pc] & SquareBB[idx]) != 0, ColorToString(pc) + " doesn't have a piece to remove on the square " + IndexToString(idx));
            Debug.Assert((Pieces[pt] & SquareBB[idx]) != 0, "The square " + IndexToString(idx) + " doesn't have a " + PieceToString(pt) + " to remove");

            Colors[pc] ^= SquareBB[idx];
            Pieces[pt] ^= SquareBB[idx];
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, where <paramref name="to"/> is an empty square.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="pieceType">The type of the piece that is moving</param>
        [MethodImpl(Inline)]
        public void MoveSimple(int from, int to, int pieceColor, int pieceType)
        {
            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Returns the <see cref="Color"/> of the piece on the square <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public int GetColorAtIndex(int idx)
        {
            return ((Colors[Color.White] & SquareBB[idx]) != 0) ? Color.White : Color.Black;
        }

        /// <summary>
        /// Returns the type of the <see cref="Piece"/> on the square <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public int GetPieceAtIndex(int idx)
        {
            return PieceTypes[idx];
        }

        /// <summary>
        /// Returns true if the square <paramref name="idx"/> has a piece of the <see cref="Color"/> <paramref name="pc"/> on it.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsColorSet(int pc, int idx)
        {
            return (Colors[pc] & SquareBB[idx]) != 0;
        }

        /// <summary>
        /// Returns a mask with a single bit set at the index of the <see cref="Color"/> <paramref name="pc"/>'s king.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong KingMask(int pc)
        {
            return (Colors[pc] & Pieces[Piece.King]);
        }

        /// <summary>
        /// Returns the index of the square that the <see cref="Color"/> <paramref name="pc"/>'s king is on.
        /// </summary>
        [MethodImpl(Inline)]
        public int KingIndex(int pc)
        {
#if DEBUG
            ulong u = KingMask(pc);
            Debug.Assert(lsb(u) != LSBEmpty);
#endif

            return lsb(KingMask(pc));
        }

        /// <summary>
        /// Returns the sum of the <see cref="Piece"/> values for the <see cref="Color"/> <paramref name="pc"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public int MaterialCount(int pc, bool excludePawns = false)
        {
            int mat = 0;
            ulong temp = Colors[pc];
            while (temp != 0)
            {
                int idx = poplsb(&temp);

                int pt = GetPieceAtIndex(idx);
                if (!(excludePawns && pt == Pawn))
                {
                    mat += GetPieceValue(pt);
                }
            }

            return mat;
        }

        /// <summary>
        /// Returns true if there is a pawn on the square <paramref name="idx"/>, 
        /// and there are no enemy pawns on its file or to the files beside it for
        /// each of the ranks it needs to move through to promote. 
        /// <br></br>
        /// So a White pawn on E4 is a passer if there are no black pawns on D7-F5.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsPasser(int idx)
        {
            if (GetPieceAtIndex(idx) != Piece.Pawn)
            {
                return false;
            }

            int ourColor = GetColorAtIndex(idx);
            ulong them = Colors[Not(ourColor)];
            ulong theirPawns = (them & Pieces[Piece.Pawn]);


            if (ourColor == Color.White)
            {
                return ((WhitePassedPawnMasks[idx] & theirPawns) == 0);
            }
            else
            {
                return ((BlackPassedPawnMasks[idx] & theirPawns) == 0);
            }
        }


        /// <summary>
        /// Returns a mask of the pieces
        /// <para></para>
        /// <paramref name="pinners"/> is a mask of the other side's pieces that would be 
        /// putting <paramref name="pc"/>'s king in check if a blocker of color <paramref name="pc"/> wasn't in the way
        /// <br></br>
        /// <paramref name="xrayers"/> is a mask for blockers that are the opposite color of <paramref name="pc"/>.
        /// These are pieces that would cause a discovery if they move off of the ray.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong BlockingPieces(int pc, ulong* pinners, ulong* xrayers)
        {
            ulong blockers = 0UL;
            *pinners = 0;
            *xrayers = 0;

            ulong temp;
            ulong us = Colors[pc];
            ulong them = Colors[Not(pc)];

            int ourKing = KingIndex(pc);

            //  Candidates are their pieces that are on the same rank/file/diagonal as our king.
            ulong candidates = ((RookRays[ourKing] & (Pieces[Piece.Rook] | Pieces[Piece.Queen])) |
                                (BishopRays[ourKing] & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))) & them;

            ulong occ = (us | them);

            while (candidates != 0)
            {
                int idx = poplsb(&candidates);

                temp = BetweenBB[ourKing][idx] & occ;

                if (temp != 0 && !MoreThanOne(temp))
                {
                    //  If there is one and only one piece between the candidate and our king, that piece is a blocker
                    blockers |= temp;

                    if ((temp & us) != 0)
                    {
                        //  If the blocker is ours, then the candidate on the square "idx" is a pinner
                        *pinners |= SquareBB[idx];
                    }
                    else
                    {
                        //  If the blocker isn't ours, then it will cause a discovered check if it moves
                        *xrayers |= SquareBB[idx];
                    }
                }
            }

            return blockers;
        }

        /// <summary>
        /// Returns a ulong with bits set at the positions of any piece that can attack the square <paramref name="idx"/>, 
        /// given the board occupancy <paramref name="occupied"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackersTo(int idx, ulong occupied)
        {
            return ((GetBishopMoves(occupied, idx) & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))
                  | (GetRookMoves(occupied, idx) & (Pieces[Piece.Rook] | Pieces[Piece.Queen]))
                  | (Pieces[Piece.Knight] & KnightMasks[idx])
                  | ((WhitePawnAttackMasks[idx] & Colors[Color.Black] & Pieces[Piece.Pawn])
                  | (BlackPawnAttackMasks[idx] & Colors[Color.White] & Pieces[Piece.Pawn])));

        }

        /// <summary>
        /// Returns a mask of the squares that a piece of type <paramref name="pt"/> and color <paramref name="pc"/> 
        /// on the square <paramref name="idx"/> attacks, given the board occupancy <paramref name="occupied"/>
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackMask(int idx, int pc, int pt, ulong occupied)
        {
            switch (pt)
            {
                case Pawn:
                    return PawnAttackMasks[pc][idx];
                case Knight:
                    return (KnightMasks[idx]);
                case Bishop:
                    return (GetBishopMoves(occupied, idx));
                case Rook:
                    return (GetRookMoves(occupied, idx));
                case Queen:
                    return (GetBishopMoves(occupied, idx) | GetRookMoves(occupied, idx));
            }

            return 0;
        }


        /// <summary>
        /// Returns true if the move <paramref name="move"/> is pseudo-legal.
        /// Only determines if there is a piece at move.From and the piece at move.To isn't the same color.
        /// </summary>
        [MethodImpl(Inline)]
        public bool IsPseudoLegal(in Move move)
        {
            if (GetPieceAtIndex(move.From) != Piece.None)
            {
                if (GetPieceAtIndex(move.To) != Piece.None)
                {
                    //  We can't capture our own color pieces
                    return (move.Capture && GetColorAtIndex(move.From) != GetColorAtIndex(move.To));
                }

                if (move.Capture)
                {
                    //  This move is trying to capture a piece when the square is empty.
                    return false;
                }

                //  This is a move to an empty square.
                return true;
            }

            //  There isn't a piece on the move's "from" square.
            return false;
        }

        /// <summary>
        /// Sets <paramref name="info"/> according to the number of pieces that attack the king of color <paramref name="ourColor"/>
        /// </summary>
        [MethodImpl(Inline)]
        public void DetermineCheck(int ourColor, ref CheckInfo info)
        {
            int ourKing = KingIndex(ourColor);

            ulong att = AttackersTo(ourKing, Occupancy) & Colors[Not(ourColor)];
            switch (popcount(att))
            {
                case 0:
                    info.InCheck = false;
                    info.InDoubleCheck = false;
                    info.idxChecker = LSBEmpty;
                    break;
                case 1:
                    info.InCheck = true;
                    info.idxChecker = lsb(att);
                    break;
                case 2:
                    info.InDoubleCheck = true;
                    info.idxChecker = lsb(att);
                    break;
            }
        }
    }
}
