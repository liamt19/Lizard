namespace Lizard.Logic.Core
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

        /// <summary>
        /// Mask of occupied squares, which is always equal to <c>Colors[White] | Colors[Black]</c>
        /// </summary>
        public ulong Occupancy = 0;

        public Bitboard()
        {
            Reset();
        }

        public ulong GetPieces(int pc, (int, int, int, int, int, int) types) => Colors[pc] & (Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4] | Pieces[types.Item5] | Pieces[types.Item6]);
        public ulong GetPieces(int pc, (int, int, int, int, int) types) => Colors[pc] & (Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4] | Pieces[types.Item5]);
        public ulong GetPieces(int pc, (int, int, int, int) types) => Colors[pc] & (Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4]);
        public ulong GetPieces(int pc, (int, int, int) types) => Colors[pc] & (Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3]);
        public ulong GetPieces(int pc, (int, int) types) => Colors[pc] & (Pieces[types.Item1] | Pieces[types.Item2]);
        public ulong GetPieces(int pc, int type) => Colors[pc] & (Pieces[type]);
        public ulong GetPieces((int, int, int, int, int, int) types) => Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4] | Pieces[types.Item5] | Pieces[types.Item6];
        public ulong GetPieces((int, int, int, int, int) types) => Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4] | Pieces[types.Item5];
        public ulong GetPieces((int, int, int, int) types) => Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3] | Pieces[types.Item4];
        public ulong GetPieces((int, int, int) types) => Pieces[types.Item1] | Pieces[types.Item2] | Pieces[types.Item3];
        public ulong GetPieces((int, int) types) => Pieces[types.Item1] | Pieces[types.Item2];
        public ulong GetPieces(int type) => Pieces[type];

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

            Occupancy = 0UL;

            for (int i = 0; i < SquareNB; i++)
            {
                PieceTypes[i] = Piece.None;
            }
        }

        public void CopyTo(ref Bitboard other)
        {
            fixed (ulong* srcPieces = Pieces, dstPieces = other.Pieces)
            {
                Unsafe.CopyBlock(dstPieces, srcPieces, sizeof(ulong) * PieceNB);
            }

            fixed (ulong* srcColors = Colors, dstColors = other.Colors)
            {
                Unsafe.CopyBlock(dstColors, srcColors, sizeof(ulong) * ColorNB);
            }
        }

        /// <summary>
        /// Returns true if White or Black has a piece on <paramref name="idx"/>
        /// </summary>
        public bool Occupied(int idx)
        {
            return PieceTypes[idx] != Piece.None;
        }

        /// <summary>
        /// Adds a piece of type <paramref name="pt"/> and color <paramref name="pc"/> on the square <paramref name="idx"/>.
        /// </summary>
        public void AddPiece(int idx, int pc, int pt)
        {
            PieceTypes[idx] = pt;

            Assert((Colors[pc] & SquareBB[idx]) == 0, $"{ColorToString(pc)} already has a piece on the square {IndexToString(idx)}");
            Assert((Pieces[pt] & SquareBB[idx]) == 0, $"A {PieceToString(pt)} already exists on the square {IndexToString(idx)}");

            Colors[pc] ^= SquareBB[idx];
            Pieces[pt] ^= SquareBB[idx];

            Occupancy |= SquareBB[idx];
        }

        /// <summary>
        /// Removes the piece of type <paramref name="pt"/> and color <paramref name="pc"/> on the square <paramref name="idx"/>.
        /// </summary>
        public void RemovePiece(int idx, int pc, int pt)
        {
            PieceTypes[idx] = Piece.None;

            Assert((Colors[pc] & SquareBB[idx]) != 0, $"{ColorToString(pc)} doesn't have a piece to remove on the square {IndexToString(idx)}");
            Assert((Pieces[pt] & SquareBB[idx]) != 0, $"The square {IndexToString(idx)} doesn't have a {PieceToString(pt)} to remove");

            Colors[pc] ^= SquareBB[idx];
            Pieces[pt] ^= SquareBB[idx];

            Occupancy ^= SquareBB[idx];
        }

        /// <summary>
        /// Moves the piece at index <paramref name="from"/> to index <paramref name="to"/>, where <paramref name="to"/> is an empty square.
        /// </summary>
        /// <param name="from">The square the piece is moving from</param>
        /// <param name="to">The square the piece is moving to</param>
        /// <param name="pieceColor">The color of the piece that is moving</param>
        /// <param name="pieceType">The type of the piece that is moving</param>
        public void MoveSimple(int from, int to, int pieceColor, int pieceType)
        {
            RemovePiece(from, pieceColor, pieceType);
            AddPiece(to, pieceColor, pieceType);
        }

        /// <summary>
        /// Returns the <see cref="Color"/> of the piece on the square <paramref name="idx"/>
        /// </summary>
        public int GetColorAtIndex(int idx)
        {
            return ((Colors[Color.White] & SquareBB[idx]) != 0) ? Color.White : Color.Black;
        }

        /// <summary>
        /// Returns the type of the <see cref="Piece"/> on the square <paramref name="idx"/>
        /// </summary>
        public int GetPieceAtIndex(int idx)
        {
            return PieceTypes[idx];
        }

        /// <summary>
        /// Returns true if the square <paramref name="idx"/> has a piece of the <see cref="Color"/> <paramref name="pc"/> on it.
        /// </summary>
        public bool IsColorSet(int pc, int idx)
        {
            return (Colors[pc] & SquareBB[idx]) != 0;
        }

        /// <summary>
        /// Returns a mask with a single bit set at the index of the <see cref="Color"/> <paramref name="pc"/>'s king.
        /// </summary>
        public ulong KingMask(int pc)
        {
            return Colors[pc] & Pieces[Piece.King];
        }

        /// <summary>
        /// Returns the index of the square that the <see cref="Color"/> <paramref name="pc"/>'s king is on.
        /// </summary>
        public int KingIndex(int pc)
        {
            Assert(lsb(KingMask(pc)) != SquareNB, $"KingIndex({ColorToString(pc)}) got a KingMask of {KingMask(pc)}");

            return lsb(KingMask(pc));
        }

        /// <summary>
        /// Returns the sum of the <see cref="Piece"/> values for the <see cref="Color"/> <paramref name="pc"/>.
        /// </summary>
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
        /// Returns a mask of the pieces
        /// <para></para>
        /// <paramref name="pinners"/> is a mask of the other side's pieces that would be 
        /// putting <paramref name="pc"/>'s king in check if a blocker of color <paramref name="pc"/> wasn't in the way
        /// <br></br>
        /// <paramref name="xrayers"/> is a mask for blockers that are the opposite color of <paramref name="pc"/>.
        /// These are pieces that would cause a discovery if they move off of the ray.
        /// </summary>
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

            ulong occ = us | them;

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
        public ulong AttackersTo(int idx, ulong occupied)
        {
            return (GetBishopMoves(occupied, idx) & (Pieces[Bishop] | Pieces[Queen]))
              | (GetRookMoves(occupied, idx) & (Pieces[Rook] | Pieces[Queen]))
              | (KnightMasks[idx] & Pieces[Knight])
              | (WhitePawnAttackMasks[idx] & Colors[Black] & Pieces[Pawn])
              | (BlackPawnAttackMasks[idx] & Colors[White] & Pieces[Pawn]);
        }

        /// <summary>
        /// Returns a ulong with bits set at the positions of every Knight, Bishop, Rook, and Queen that can attack the square <paramref name="idx"/>, 
        /// given the board occupancy <paramref name="occupied"/>.
        /// </summary>
        public ulong AttackersToMajors(int idx, ulong occupied)
        {
            return (GetBishopMoves(occupied, idx) & (Pieces[Piece.Bishop] | Pieces[Piece.Queen]))
                  | (GetRookMoves(occupied, idx) & (Pieces[Piece.Rook] | Pieces[Piece.Queen]))
                  | (Pieces[Piece.Knight] & KnightMasks[idx]);
        }

        /// <summary>
        /// Returns a mask of the squares that a piece of type <paramref name="pt"/> and color <paramref name="pc"/> 
        /// on the square <paramref name="idx"/> attacks, given the board occupancy <paramref name="occupied"/>
        /// </summary>
        public ulong AttackMask(int idx, int pc, int pt, ulong occupied)
        {
            return pt switch
            {
                Pawn => PawnAttackMasks[pc][idx],
                Knight => KnightMasks[idx],
                Bishop => GetBishopMoves(occupied, idx),
                Rook => GetRookMoves(occupied, idx),
                Queen => GetBishopMoves(occupied, idx) | GetRookMoves(occupied, idx),
                King => NeighborsMask[idx],
                _ => 0,
            };
        }

        /// <summary>
        /// Returns a mask of all of the squares that pieces of type <paramref name="pt"/> and color <paramref name="pc"/> attack.
        /// </summary>
        public ulong AttackMask(int pc, int pt)
        {
            ulong mask = 0;
            ulong occ = Occupancy;
            ulong pieces = Colors[pc] & Pieces[pt];
            while (pieces != 0)
            {
                mask |= AttackMask(poplsb(&pieces), pc, pt, occ);
            }
            return mask;
        }
    }
}
