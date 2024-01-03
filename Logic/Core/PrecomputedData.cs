namespace LTChess.Logic.Data
{

    /// <summary>
    /// Precomputes Knight moves, neighboring squares, and the diagonals that each index is a part of
    /// </summary>
    public static unsafe class PrecomputedData
    {
        /// <summary>
        /// Index using [depth][moveIndex]
        /// </summary>
        public static readonly int[][] LogarithmicReductionTable = new int[MaxPly][];

        public static readonly int[][] LMPTable = new int[2][];

        /// <summary>
        /// At each index, contains a ulong with bits set at each neighboring square.
        /// </summary>
        public static readonly ulong* NeighborsMask = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        /// <summary>
        /// At each index, contains a mask of each of the squares that a knight could move to.
        /// </summary>
        public static readonly ulong* KnightMasks = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        public static readonly ulong* RookRays = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        public static readonly ulong* BishopRays = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        /// <summary>
        /// Bitboards containing all of the squares that a White pawn on an index attacks. A White pawn on A2 attacks B3 etc.
        /// </summary>
        public static readonly ulong* WhitePawnAttackMasks = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        /// <summary>
        /// Bitboards containing all of the squares that a Black pawn on an index attacks. A Black pawn on A7 attacks B6 etc.
        /// </summary>
        public static readonly ulong* BlackPawnAttackMasks = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        public static readonly ulong** PawnAttackMasks = (ulong**)AlignedAllocZeroed(sizeof(ulong) * 2, AllocAlignment);

        /// <summary>
        /// At each index, contains a ulong equal to (1UL << index).
        /// </summary>
        public static readonly ulong* SquareBB = (ulong*)AlignedAllocZeroed((nuint)(sizeof(ulong) * SquareNB), AllocAlignment);

        /// <summary>
        /// Bitboards with bits set at every index that exists in a line between two indices.
        /// Index using LineBB[s1][s2] where s1 might be a king square, and s2 is another piece's square.
        /// <br></br>
        /// The bit for s2 will always be set, no matter what.
        /// <para></para>
        /// So LineBB[A1][H1] gives 254, or 01111111
        /// </summary>
        public static readonly ulong** LineBB = (ulong**)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);


        /// <summary>
        /// Index using BetweenBB[s1][s2], this is the same as LineBB, but the index at s2 is never set.
        /// <para></para>
        /// So BetweenBB[A1][H1] gives 126, or 01111110
        /// </summary>
        public static readonly ulong** BetweenBB = (ulong**)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

        /// <summary>
        /// Bitboards with bits set at every index that exists along the entire ray that two squares have in common.
        /// <br></br>
        /// If two squares are on the same rank/file, then RayBB on those squares would be the entire rank/file 
        /// and likewise for diagonals. Otherwise, those squares' RayBB is 0.
        /// <br></br>
        /// </summary>
        public static readonly ulong** RayBB = (ulong**)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);


        /// <summary>
        /// Bitboards with bits set at every index that exists on the ray beginning on the first square, passing through the second square,
        /// and continuing to the end of the board.
        /// <br></br>
        /// This is similar to RayBB, but in this case there are no bits set "behind" the first square.
        /// <para></para>
        /// This is used to get the mask of all of the squares in the same direction as a discovered check is coming from,
        /// since using RayBB could provide incorrect squares if there are multiple Xrayers on the same rank / file. For example:
        /// <br></br>
        /// "7k/8/8/8/q2PKPpr/8/8/8 b - f3 0 1" was giving incorrect perft results after gxf3+ because the queen on A4 was on the same RayBB
        /// as the rook on H4 and since the queen was on a lower index square the lsb(RayBB) was 
        /// claiming that it was the piece giving check, instead of the rook.
        /// <br></br>
        /// </summary>
        public static readonly ulong** XrayBB = (ulong**)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);


        static PrecomputedData()
        {
            Initialize();
        }

        public static void Initialize()
        {
            DoSquareBB();
            DoPieceRays();

            DoNeighbors();

            DoKnightMoves();
            DoPawnAttacks();

            DoBetweenBBs();

            DoReductionTable();

            DoRayBBs();
        }

        private static void DoSquareBB()
        {
            for (int s = 0; s <= 63; ++s)
            {
                SquareBB[s] = 1UL << s;
            }
        }

        /// <summary>
        /// Calculates the masks for rook and bishop moves on an empty board, must be done after the diagonals have been calculated.
        /// </summary>
        private static void DoPieceRays()
        {
            for (int sq = 0; sq < 64; sq++)
            {
                ulong rookMask = (GetFileBB(sq) | GetRankBB(sq)) & ~(1UL << sq);
                RookRays[sq] = rookMask;

                ulong bishopMask = 0UL;
                foreach (int dir in new int[] { Direction.NORTH_EAST, Direction.NORTH_WEST, Direction.SOUTH_EAST, Direction.SOUTH_WEST })
                {
                    int tempSq = sq;
                    while (DirectionOK(tempSq, dir))
                    {
                        //  Keep moving in this direction until we move off the board,
                        //  masking in squares along the way,
                        tempSq += dir;
                        bishopMask |= 1UL << tempSq;
                    }
                }
                BishopRays[sq] = bishopMask;
            }
        }

        private static void DoRayBBs()
        {
            for (int s1 = 0; s1 < 64; s1++)
            {
                RayBB[s1] = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
                XrayBB[s1] = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);


                for (int s2 = 0; s2 < 64; s2++)
                {
                    if ((RookRays[s1] & SquareBB[s2]) != 0)
                    {
                        RayBB[s1][s2] = (RookRays[s1] & RookRays[s2]) | SquareBB[s1] | SquareBB[s2];
                        XrayBB[s1][s2] = (GetRookMoves(SquareBB[s1], s2) & RookRays[s1]) | SquareBB[s1] | SquareBB[s2];
                    }
                    else if ((BishopRays[s1] & SquareBB[s2]) != 0)
                    {
                        RayBB[s1][s2] = (BishopRays[s1] & BishopRays[s2]) | SquareBB[s1] | SquareBB[s2];
                        XrayBB[s1][s2] = (GetBishopMoves(SquareBB[s1], s2) & BishopRays[s1]) | SquareBB[s1] | SquareBB[s2];
                    }
                    else
                    {
                        RayBB[s1][s2] = 0;
                        XrayBB[s1][s2] = 0;
                    }
                }
            }
        }

        private static void DoNeighbors()
        {
            for (int i = 0; i < 64; i++)
            {
                foreach (int offset in new int[] { 7, 8, 9, -1, 1, -7, -8, -9 }.Where(x => DirectionOK(i, x)))
                {
                    NeighborsMask[i] |= 1UL << (i + offset);
                }
            }
        }

        private static void DoKnightMoves()
        {
            for (int i = 0; i < 64; i++)
            {
                foreach (int offset in new int[] { 6, 10, 15, 17, -6, -10, -15, -17 }.Where(x => DirectionOK(i, x)))
                {
                    KnightMasks[i] |= 1UL << (i + offset);
                }
            }
        }

        private static void DoPawnAttacks()
        {
            PawnAttackMasks[White] = WhitePawnAttackMasks;
            PawnAttackMasks[Black] = BlackPawnAttackMasks;

            for (int i = 0; i < 64; i++)
            {
                ulong whiteAttack = 0;
                ulong whiteMove = 1UL << (i + 8);

                ulong blackAttack = 0;
                ulong blackMove = 1UL << (i - 8);

                IndexToCoord(i, out int x, out int y);

                int wy = y + 1;
                int by = y - 1;

                if (y == 1)
                {
                    whiteMove |= 1UL << (i + 16);
                }

                if (y == 6)
                {
                    blackMove |= 1UL << (i - 16);
                }

                if (x > 0)
                {
                    if (i < A2)
                    {
                        BlackPawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.Black][i] = 0;

                        whiteAttack |= 1UL << CoordToIndex(x - 1, wy);
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.White][i] = 0;

                        blackAttack |= 1UL << CoordToIndex(x - 1, by);
                    }
                    else
                    {
                        whiteAttack |= 1UL << CoordToIndex(x - 1, wy);
                        blackAttack |= 1UL << CoordToIndex(x - 1, by);
                    }

                }
                if (x < 7)
                {
                    if (i < A2)
                    {
                        //  Set this to 0 since pawns don't attack squares that are outside of the bounds of the board.
                        BlackPawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.Black][i] = 0;

                        whiteAttack |= 1UL << CoordToIndex(x + 1, wy);
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.White][i] = 0;

                        blackAttack |= 1UL << CoordToIndex(x + 1, by);
                    }
                    else
                    {
                        whiteAttack |= 1UL << CoordToIndex(x + 1, wy);
                        blackAttack |= 1UL << CoordToIndex(x + 1, by);
                    }
                }

                WhitePawnAttackMasks[i] = whiteAttack;
                BlackPawnAttackMasks[i] = blackAttack;

                PawnAttackMasks[Color.White][i] = whiteAttack;
                PawnAttackMasks[Color.Black][i] = blackAttack;
            }

        }

        /// <summary>
        /// Calculates values for LineBB and BetweenBB, must be done after the diagonals have been calculated.
        /// </summary>
        private static void DoBetweenBBs()
        {

            for (int s1 = 0; s1 < 64; s1++)
            {
                LineBB[s1] = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
                BetweenBB[s1] = (ulong*)AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

                int f1 = GetIndexFile(s1);
                int r1 = GetIndexRank(s1);
                for (int s2 = 0; s2 < 64; s2++)
                {
                    int f2 = GetIndexFile(s2);
                    int r2 = GetIndexRank(s2);

                    if ((RookRays[s1] & SquareBB[s2]) != 0)
                    {
                        BetweenBB[s1][s2] = GetRookMoves(SquareBB[s2], s1) & GetRookMoves(SquareBB[s1], s2);
                        LineBB[s1][s2] = BetweenBB[s1][s2] | SquareBB[s2];
                    }
                    else if ((BishopRays[s1] & SquareBB[s2]) != 0)
                    {
                        BetweenBB[s1][s2] = GetBishopMoves(SquareBB[s2], s1) & GetBishopMoves(SquareBB[s1], s2);
                        LineBB[s1][s2] = BetweenBB[s1][s2] | SquareBB[s2];
                    }
                    else
                    {
                        BetweenBB[s1][s2] = 0;
                        LineBB[s1][s2] = SquareBB[s2];
                    }
                }
            }
        }

        private static void DoReductionTable()
        {
            for (int depth = 0; depth < MaxPly; depth++)
            {
                LogarithmicReductionTable[depth] = new int[MoveListSize];
                for (int moveIndex = 0; moveIndex < MoveListSize; moveIndex++)
                {
                    //LogarithmicReductionTable[depth][moveIndex] = (int)(Math.Log(depth) * Math.Log(moveIndex) / 2 - 0.3);
                    LogarithmicReductionTable[depth][moveIndex] = (int)((Math.Log(depth) * Math.Log(moveIndex) / 2.25) + 0.25);

                    if (LogarithmicReductionTable[depth][moveIndex] < 1)
                    {
                        LogarithmicReductionTable[depth][moveIndex] = 0;
                    }
                }
            }

            const int improving = 1;
            const int not_improving = 0;
            LMPTable[not_improving] = new int[MaxPly];
            LMPTable[improving] = new int[MaxPly];
            for (int depth = 0; depth < MaxPly; depth++)
            {
                LMPTable[not_improving][depth] = (3 + (depth * depth)) / 2;
                LMPTable[improving][depth] = 3 + (depth * depth);
            }
        }
    }

}
