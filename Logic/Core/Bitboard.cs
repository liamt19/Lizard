namespace LTChess.Logic.Core
{
    /// <summary>
    /// Manages the bitboards for the position
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

            Debug.Assert((Colors[pc] & SquareBB[idx]) == 0);
            Debug.Assert((Pieces[pt] & SquareBB[idx]) == 0);

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

            Debug.Assert((Colors[pc] & SquareBB[idx]) != 0);
            Debug.Assert((Pieces[pt] & SquareBB[idx]) != 0);

            Colors[pc] ^= SquareBB[idx];
            Pieces[pt] ^= SquareBB[idx];
        }


        /// <summary>
        /// Removes the pawn at <paramref name="from"/>, and replaces it with the <paramref name="promotionPiece"/> at the index <paramref name="to"/>.
        /// Also captures the piece on <paramref name="to"/> if there is one.
        /// </summary>
        [MethodImpl(Inline)]
        public void Promote(int from, int to, int promotionPiece)
        {
            int pc = GetColorAtIndex(from);

            int capturedPiece = PieceTypes[to];
            if (capturedPiece != Piece.None)
            {
                //  Delete that piece now
                Pieces[capturedPiece] ^= SquareBB[to];
                Colors[Not(pc)] ^= SquareBB[to];
            }

            Pieces[Piece.Pawn] ^= SquareBB[from];
            Pieces[promotionPiece] ^= SquareBB[to];

            Colors[pc] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = promotionPiece;
        }

        [MethodImpl(Inline)]
        public void Promote(int from, int to, int thisColor, int capturedPiece, int promotionPiece)
        {
            Pieces[capturedPiece] ^= SquareBB[to];
            Colors[Not(thisColor)] ^= SquareBB[to];

            Pieces[Piece.Pawn] ^= SquareBB[from];
            Pieces[promotionPiece] ^= SquareBB[to];

            Colors[thisColor] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = promotionPiece;
        }



        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, capturing the piece of type <paramref name="capturedPieceType"/>.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="pieceType">The type of the piece that is moving</param>
        /// <param name="capturedPieceType">The type of the piece that is being captured</param>
        [MethodImpl(Inline)]
        public void Move(int from, int to, int pieceColor, int pieceType, int capturedPieceType)
        {
            if (capturedPieceType != Piece.None)
            {
#if DEBUG
                Debug.Assert(capturedPieceType != Piece.King, "Moving from " + IndexToString(from) + " to " + IndexToString(to) + " captures " + ColorToString(pieceColor) + "'s king!"
                    + "\r\nCalled by " + (new StackTrace()).GetFrame(1).GetMethod().Name);
#endif
                Pieces[capturedPieceType] ^= SquareBB[to];
                Colors[Not(pieceColor)] ^= SquareBB[to];
            }

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[pieceType] ^= moveMask;
            Colors[pieceColor] ^= moveMask;

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
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

            //Pieces[pieceType] ^= (SquareBB[from] | SquareBB[to]);
            //Colors[pieceColor] ^= (SquareBB[from] | SquareBB[to]);

            PieceTypes[from] = Piece.None;
            PieceTypes[to] = pieceType;
        }

        /// <summary>
        /// Moves the pawn at <paramref name="from"/> to <paramref name="to"/>, and clears the index at <paramref name="idxEnPassant"/>.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="idxEnPassant">The index of the pawn that is being taken, which should be 1 square left/right of <paramref name="from"/></param>
        [MethodImpl(Inline)]
        public void EnPassant(int from, int to, int pieceColor, int idxEnPassant)
        {

            ulong moveMask = (SquareBB[from] | SquareBB[to]);
            Pieces[Piece.Pawn] ^= (moveMask | SquareBB[idxEnPassant]);
            Colors[pieceColor] ^= (moveMask);

            Colors[Not(pieceColor)] ^= SquareBB[idxEnPassant];
            PieceTypes[from] = Piece.None;
            PieceTypes[idxEnPassant] = Piece.None;
            PieceTypes[to] = Piece.Pawn;
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
        public int MaterialCount(int pc)
        {
            int mat = 0;
            ulong temp = Colors[pc];
            while (temp != 0)
            {
                int idx = lsb(temp);

                mat += GetPieceValue(GetPieceAtIndex(idx));

                temp = poplsb(temp);
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
        /// Returns a mask of pieces which are pinned to <paramref name="pc"/>'s king.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong PinnedPieces(int pc)
        {
            ulong pinned = 0UL;
            ulong temp;
            ulong them = Colors[Not(pc)];

            int ourKing = KingIndex(pc);
            ulong pinners = ((RookRays[ourKing] & (Pieces[Piece.Rook] | Pieces[Piece.Queen])) |
                           (BishopRays[ourKing] & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))) & them;

            while (pinners != 0)
            {
                int idx = lsb(pinners);

                temp = BetweenBB[ourKing][idx] & (Colors[pc] | them);

                if (temp != 0 && !MoreThanOne(temp))
                {
                    pinned |= temp;
                }

                pinners = poplsb(pinners);
            }

            return pinned;
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
        /// Returns a ulong with bits set at the positions of pieces that can attack <paramref name="idx"/>. 
        /// So for a bishop on A1, AttackersTo H8 returns a ulong with a bit set at A1.
        /// defendingColor is the color whose pieces are being attacked, and Not(defendingColor) is the color of the pieces that attack that square. 
        /// So bb.AttackersTo(..., White) will reference any attacking Black pieces.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackersTo(int idx, int defendingColor)
        {
            ulong us = Colors[defendingColor];
            ulong them = Colors[Not(defendingColor)];

            return AttackersToFast(idx, us | them) & them;
        }

        /// <summary>
        /// Returns a ulong with bits set at the positions of pieces that can attack <paramref name="idx"/>. 
        /// </summary>
        [MethodImpl(Inline)]
        public ulong AttackersToFast(int idx, ulong occupied)
        {
            return ((GetBishopMoves(occupied, idx) & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))
                  | (GetRookMoves(occupied, idx) & (Pieces[Piece.Rook] | Pieces[Piece.Queen]))
                  | (Pieces[Piece.Knight] & KnightMasks[idx])
                  | ((WhitePawnAttackMasks[idx] & Colors[Color.Black] & Pieces[Piece.Pawn])
                  | (BlackPawnAttackMasks[idx] & Colors[Color.White] & Pieces[Piece.Pawn])));

        }



        /// <summary>
        /// Returns the index of the square of the attacker of lowest value,
        /// which is a pawn, knight, bishop, rook, queen, or king in that order.
        /// </summary>
        /// <param name="idx">The square to look at</param>
        /// <param name="defendingColor">The color of the pieces BEING attacked.</param>
        [MethodImpl(Inline)]
        public int LowestValueAttacker(int idx, int defendingColor)
        {
            ulong us = Colors[defendingColor];
            ulong them = Colors[Not(defendingColor)];

            ulong pawns = ((defendingColor == Color.White) ? WhitePawnAttackMasks[idx] : BlackPawnAttackMasks[idx]) & Pieces[Piece.Pawn] & them;
            if (pawns != 0)
            {
                return lsb(pawns);
            }

            ulong knights = (Pieces[Piece.Knight] & KnightMasks[idx] & them);
            if (knights != 0)
            {
                return lsb(knights);
            }

            ulong occupied = (us | them);

            ulong diagSliders = GetBishopMoves(occupied, idx);
            if ((diagSliders & Pieces[Piece.Bishop] & them) != 0)
            {
                return lsb((diagSliders & Pieces[Piece.Bishop] & them));
            }

            ulong straightSliders = GetRookMoves(occupied, idx);
            if ((straightSliders & Pieces[Piece.Rook] & them) != 0)
            {
                return lsb((straightSliders & Pieces[Piece.Rook] & them));
            }

            if (((diagSliders | straightSliders) & Pieces[Piece.Queen] & them) != 0)
            {
                return lsb((diagSliders | straightSliders) & Pieces[Piece.Queen] & them);
            }

            return LSBEmpty;
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

            ulong att = AttackersToFast(ourKing, Occupancy) & Colors[Not(ourColor)];
            switch (popcount(att))
            {
                case 0:
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
