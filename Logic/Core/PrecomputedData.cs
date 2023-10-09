using System.Diagnostics;


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
        public static int[][] LogarithmicReductionTable = new int[MaxPly][];

        public static int[][] LMPTable = new int[2][];

        /// <summary>
        /// At each index, contains a ulong with bits set at each neighboring square.
        /// </summary>
        public static ulong* NeighborsMask;

        /// <summary>
        /// At each index, contains a mask of squares which neighbor the indices neighbors. So the mask for A1 contains A3, B3, C3, C2, C1.
        /// </summary>
        public static ulong* OutterNeighborsMask;

        /// <summary>
        /// At each index, contains a mask of each of the squares that a knight could move to.
        /// </summary>
        public static ulong* KnightMasks;

        public static ulong* RookRays;

        public static ulong* BishopRays;

        public static int[][] DiagonalIndicesA1H8 = new int[64][];

        public static int[][] DiagonalIndicesA8H1 = new int[64][];

        /// <summary>
        /// Contains ulongs for each square with bits set along the A1-H8 diagonal (bottom left to top right, from White's perspective).
        /// So square E4 has bits set at B1, C2, D3, E4, F5... and G1 only has G1 and H2.
        /// </summary>
        public static ulong[] DiagonalMasksA1H8 = new ulong[64];

        /// <summary>
        /// Contains ulongs for each square with bits set along the A8-H1 diagonal (top left to bottom right, from White's perspective).
        /// So square E4 has bits set at A8, B7, C6, D5, E4, F3... and B1 only has B1 and A2.
        /// </summary>
        public static ulong[] DiagonalMasksA8H1 = new ulong[64];

        public static DiagonalInfo[][] InfoA1H8 = new DiagonalInfo[64][];
        public static DiagonalInfo[][] InfoA8H1 = new DiagonalInfo[64][];


        /// <summary>
        /// Bitboards containing all of the squares that a White pawn on an index attacks. A White pawn on A2 attacks B3 etc.
        /// </summary>
        public static ulong* WhitePawnAttackMasks;

        /// <summary>
        /// Bitboards containing all of the squares that a Black pawn on an index attacks. A Black pawn on A7 attacks B6 etc.
        /// </summary>
        public static ulong* BlackPawnAttackMasks;

        public static ulong** PawnAttackMasks;

        /// <summary>
        /// At each index, contains a mask of all of the squares above the index which determine whether or not a pawn is passed.
        /// </summary>
        public static ulong[] WhitePassedPawnMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of all of the squares below the index which determine whether or not a pawn is passed.
        /// </summary>
        public static ulong[] BlackPassedPawnMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of the files to the left and right of the square as well as the file the square is in.
        /// So NeighborsFileMasks[D6] would have every square in the B/C/D files masked.
        /// </summary>
        public static ulong[] NeighborsFileMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a ulong equal to (1UL << index).
        /// </summary>
        public static ulong* SquareBB;

        /// <summary>
        /// Bitboards with bits set at every index that exists in a line between two indices.
        /// Index using LineBB[s1][s2] where s1 might be a king square, and s2 is another piece's square.
        /// <br></br>
        /// The bit for s2 will always be set, no matter what.
        /// <para></para>
        /// So LineBB[A1][H1] gives 254, or 01111111
        /// </summary>
        public static ulong** LineBB;

        /// <summary>
        /// Index using BetweenBB[s1][s2], this is the same as LineBB, but the index at s2 is never set.
        /// <para></para>
        /// So BetweenBB[A1][H1] gives 126, or 01111110
        /// </summary>
        public static ulong** BetweenBB;

        /// <summary>
        /// Bitboards with bits set at every index that exists along the entire ray that two squares have in common.
        /// <br></br>
        /// If two squares are on the same rank/file, then RayBB on those squares would be the entire rank/file 
        /// and likewise for diagonals. Otherwise, those squares' RayBB is 0.
        /// <br></br>
        /// </summary>
        public static ulong** RayBB;

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
        public static ulong** XrayBB;

        public static int[][] FileDistances = new int[64][];
        public static int[][] RankDistances = new int[64][];

        /// <summary>
        /// At each index, contains the distance from that square to every other square.
        /// This is the Min(file distance, rank distance) between the two squares.
        /// </summary>
        public static int[][] SquareDistances = new int[64][];

        private static bool Initialized = false;

        static PrecomputedData()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            DoSquareBBs();
            DoSquareDistances();
            DoDiagonals();
            DoPieceRays();

            DoNeighbors();
            DoOutterNeighbors();

            DoKnightMoves();
            DoPawnAttacks();
            DoPassedPawns();

            DoBetweenBBs();

            DoReductionTable();

            Initialized = true;
        }


        /// <summary>
        /// This should be run after every other constructor has been initialized.
        /// This uses the MagicBitboards class to get bishop/rook moves, but the MagicBitboards
        /// class needs this one to be initialized first...
        /// </summary>
        public static void RunPostInitialization()
        {
            if (RayBB == null || RayBB[0] == null)
            {
                DoRayBBs();
            }
        }


        private static void DoSquareBBs()
        {
            SquareBB = (ulong*) AlignedAllocZeroed((nuint)(sizeof(ulong) * SquareNB), AllocAlignment);
            for (int s = 0; s <= 63; ++s)
            {
                SquareBB[s] = (1UL << s);
            }
        }

        private static void DoSquareDistances()
        {
            for (int s1 = 0; s1 < 64; s1++)
            {
                FileDistances[s1] = new int[64];
                RankDistances[s1] = new int[64];
                SquareDistances[s1] = new int[64];
                IndexToCoord(s1, out int s1x, out int s1y);
                for (int s2 = 0; s2 < 64; s2++)
                {
                    IndexToCoord(s2, out int s2x, out int s2y);
                    int fileDistance = Math.Abs(s1x - s2x);
                    int rankDistance = Math.Abs(s1y - s2y);
                    FileDistances[s1][s2] = fileDistance;
                    RankDistances[s1][s2] = rankDistance;
                    SquareDistances[s1][s2] = Math.Max(fileDistance, rankDistance);
                }
            }
        }

        private static void DoDiagonals()
        {
            for (int i = 0; i < 64; i++)
            {
                IndexToCoord(i, out int x, out int y);
                List<int> bltr = new List<int>();
                List<int> tlbr = new List<int>();

                int ix = x - 1;
                int iy = y - 1;
                while (ix >= 0 && iy >= 0)
                {
                    bltr.Insert(0, CoordToIndex(ix, iy));
                    ix--;
                    iy--;
                }

                ix = x;
                iy = y;
                while (ix <= 7 && iy <= 7)
                {
                    bltr.Add(CoordToIndex(ix, iy));
                    ix++;
                    iy++;
                }


                ix = x - 1;
                iy = y + 1;
                while (ix >= 0 && iy <= 7)
                {
                    tlbr.Insert(0, CoordToIndex(ix, iy));
                    ix--;
                    iy++;
                }

                ix = x;
                iy = y;
                //index2 = tlbr.Count;
                while (ix <= 7 && iy >= 0)
                {
                    tlbr.Add(CoordToIndex(ix, iy));
                    ix++;
                    iy--;
                }

                ulong maskA1 = 0UL;
                foreach (int mv in bltr)
                {
                    maskA1 |= (1UL << mv);
                }
                DiagonalMasksA1H8[i] = maskA1;

                ulong maskA8 = 0UL;
                foreach (int mv in tlbr)
                {
                    maskA8 |= (1UL << mv);
                }
                DiagonalMasksA8H1[i] = maskA8;


                DiagonalIndicesA1H8[i] = bltr.ToArray();
                DiagonalIndicesA8H1[i] = tlbr.ToArray();
            }

            for (int i = 0; i < 64; i++)
            {
                InfoA1H8[i] = new DiagonalInfo[64];
                InfoA8H1[i] = new DiagonalInfo[64];
                for (int j = 0; j < 64; j++)
                {
                    bool onSame = DetOnSameDiagonal(i, j, Diagonal.Diagonal_A1H8, out int a, out int b);
                    DiagonalInfo d1 = new DiagonalInfo(i, j, Diagonal.Diagonal_A1H8, onSame, a, b);
                    InfoA1H8[i][j] = d1;

                    bool onSame1 = DetOnSameDiagonal(i, j, Diagonal.Diagonal_A8H1, out int c, out int d);
                    DiagonalInfo d2 = new DiagonalInfo(i, j, Diagonal.Diagonal_A8H1, onSame1, c, d);
                    InfoA8H1[i][j] = d2;
                }
            }
        }

        /// <summary>
        /// Calculates the masks for rook and bishop moves on an empty board, must be done after the diagonals have been calculated.
        /// </summary>
        private static void DoPieceRays()
        {
            RookRays   = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            BishopRays = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            for (int sq = 0; sq < 64; sq++)
            {
                ulong rookMask = (GetFileBB(sq) | GetRankBB(sq)) & ~(1UL << sq);
                RookRays[sq] = rookMask;

                ulong bishopMask = (DiagonalMasksA1H8[sq] | DiagonalMasksA8H1[sq]) & ~(1UL << sq);
                BishopRays[sq] = bishopMask;
            }
        }

        private static void DoRayBBs()
        {
            RayBB  = (ulong**) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            XrayBB = (ulong**) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

            for (int s1 = 0; s1 < 64; s1++)
            {
                RayBB[s1]  = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
                XrayBB[s1] = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);


                for (int s2 = 0; s2 < 64; s2++)
                {
                    if ((RookRays[s1] & SquareBB[s2]) != 0)
                    {
                        RayBB[s1][s2] = (RookRays[s1] & RookRays[s2]) | (SquareBB[s1] | SquareBB[s2]);
                        XrayBB[s1][s2] = (GetRookMoves(SquareBB[s1], s2) & RookRays[s1]) | (SquareBB[s1] | SquareBB[s2]);
                    }
                    else if ((BishopRays[s1] & SquareBB[s2]) != 0)
                    {
                        RayBB[s1][s2] = (BishopRays[s1] & BishopRays[s2]) | (SquareBB[s1] | SquareBB[s2]);
                        XrayBB[s1][s2] = (GetBishopMoves(SquareBB[s1], s2) & BishopRays[s1]) | (SquareBB[s1] | SquareBB[s2]);
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
            NeighborsMask       = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            OutterNeighborsMask = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

            for (int i = 0; i < 64; i++)
            {
                List<int> list = new List<int>();
                IndexToCoord(i, out int x, out int y);

                if (InBounds(x - 1, y + 1))
                {
                    list.Add(CoordToIndex(x - 1, y + 1));
                }
                if (InBounds(x, y + 1))
                {
                    list.Add(CoordToIndex(x, y + 1));
                }
                if (InBounds(x + 1, y + 1))
                {
                    list.Add(CoordToIndex(x + 1, y + 1));
                }

                if (InBounds(x - 1, y))
                {
                    list.Add(CoordToIndex(x - 1, y));
                }
                if (InBounds(x + 1, y))
                {
                    list.Add(CoordToIndex(x + 1, y));
                }

                if (InBounds(x - 1, y - 1))
                {
                    list.Add(CoordToIndex(x - 1, y - 1));
                }
                if (InBounds(x, y - 1))
                {
                    list.Add(CoordToIndex(x, y - 1));
                }
                if (InBounds(x + 1, y - 1))
                {
                    list.Add(CoordToIndex(x + 1, y - 1));
                }

                ulong mask = 0UL;
                foreach (int mv in list)
                {
                    mask |= (1UL << mv);
                }
                NeighborsMask[i] = mask;

                if (GetIndexFile(i) > Files.A)
                {
                    //  Mask squares to the left
                    NeighborsFileMasks[i] |= GetFileBB(i - 1);
                }

                //  Mask squares above and below.
                NeighborsFileMasks[i] |= GetFileBB(i);

                if (GetIndexFile(i) < Files.H)
                {
                    //  Mask squares to the right
                    NeighborsFileMasks[i] |= GetFileBB(i + 1);
                }

            }
        }

        private static void DoOutterNeighbors()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong mask = 0UL;
                ulong temp = NeighborsMask[i];
                while (temp != 0)
                {
                    mask |= NeighborsMask[lsb(temp)];
                    temp = poplsb(temp);
                }

                //  Mask out the original square.
                mask &= ~(NeighborsMask[i] | SquareBB[i]);

                OutterNeighborsMask[i] = mask;
            }
        }

        private static void DoKnightMoves()
        {
            KnightMasks = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

            for (int i = 0; i < 64; i++)
            {
                int x = i % 8;
                int y = i / 8;

                List<int> temp = new List<int>();
                if (InBounds(x - 1, y + 2))
                {
                    temp.Add(CoordToIndex(x - 1, y + 2));
                }
                if (InBounds(x - 2, y + 1))
                {
                    temp.Add(CoordToIndex(x - 2, y + 1));
                }

                if (InBounds(x - 1, y - 2))
                {
                    temp.Add(CoordToIndex(x - 1, y - 2));
                }
                if (InBounds(x - 2, y - 1))
                {
                    temp.Add(CoordToIndex(x - 2, y - 1));
                }

                if (InBounds(x + 1, y + 2))
                {
                    temp.Add(CoordToIndex(x + 1, y + 2));
                }
                if (InBounds(x + 2, y + 1))
                {
                    temp.Add(CoordToIndex(x + 2, y + 1));
                }

                if (InBounds(x + 1, y - 2))
                {
                    temp.Add(CoordToIndex(x + 1, y - 2));
                }
                if (InBounds(x + 2, y - 1))
                {
                    temp.Add(CoordToIndex(x + 2, y - 1));
                }

                ulong mask = 0UL;
                foreach (int mv in temp)
                {
                    mask |= (1UL << mv);
                }
                KnightMasks[i] = mask;
            }
        }

        private static void DoPawnAttacks()
        {
            WhitePawnAttackMasks = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            BlackPawnAttackMasks = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

            PawnAttackMasks = (ulong**) AlignedAllocZeroed(sizeof(ulong) * 2, AllocAlignment);

            PawnAttackMasks[White] = WhitePawnAttackMasks;
            PawnAttackMasks[Black] = BlackPawnAttackMasks;

            for (int i = 0; i < 64; i++)
            {
                ulong whiteAttack = 0;
                ulong whiteMove = (1UL << (i + 8));

                ulong blackAttack = 0;
                ulong blackMove = (1UL << (i - 8));

                IndexToCoord(i, out int x, out int y);

                int wy = (y + 1);
                int by = (y - 1);

                if (y == 1)
                {
                    whiteMove |= (1UL << (i + 16));
                }

                if (y == 6)
                {
                    blackMove |= (1UL << (i - 16));
                }

                if (x > 0)
                {
                    if (i < A2)
                    {
                        BlackPawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.Black][i] = 0;

                        whiteAttack |= (1UL << CoordToIndex(x - 1, wy));
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.White][i] = 0;

                        blackAttack |= (1UL << CoordToIndex(x - 1, by));
                    }
                    else
                    {
                        whiteAttack |= (1UL << CoordToIndex(x - 1, wy));
                        blackAttack |= (1UL << CoordToIndex(x - 1, by));
                    }

                }
                if (x < 7)
                {
                    if (i < A2)
                    {
                        //  Set this to 0 since pawns don't attack squares that are outside of the bounds of the board.
                        BlackPawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.Black][i] = 0;

                        whiteAttack |= (1UL << CoordToIndex(x + 1, wy));
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;
                        PawnAttackMasks[Color.White][i] = 0;

                        blackAttack |= (1UL << CoordToIndex(x + 1, by));
                    }
                    else
                    {
                        whiteAttack |= (1UL << CoordToIndex(x + 1, wy));
                        blackAttack |= (1UL << CoordToIndex(x + 1, by));
                    }
                }

                WhitePawnAttackMasks[i] = whiteAttack;
                BlackPawnAttackMasks[i] = blackAttack;

                PawnAttackMasks[Color.White][i] = whiteAttack;
                PawnAttackMasks[Color.Black][i] = blackAttack;
            }

        }

        private static void DoPassedPawns()
        {

            for (int idx = 0; idx < 64; idx++)
            {
                IndexToCoord(idx, out int x, out int y);
                ulong whiteRanks = 0UL;
                for (int rank = y + 1; rank < 7; rank++)
                {
                    whiteRanks |= (Rank1BB << (8 * rank));
                }

                ulong blackRanks = 0UL;
                for (int rank = y - 1; rank > 0; rank--)
                {
                    blackRanks |= (Rank1BB << (8 * rank));
                }

                //  files includes idx's file, and the files to idx's left and right if they are on the same rank still (between B and G)
                ulong files = GetFileBB(idx);
                if (idx > A1 && GetIndexRank(idx - 1) == y)
                {
                    files |= GetFileBB(idx - 1);
                }
                if (idx < H8 && GetIndexRank(idx + 1) == y)
                {
                    files |= GetFileBB(idx + 1);
                }

                WhitePassedPawnMasks[idx] = whiteRanks & files;
                BlackPassedPawnMasks[idx] = blackRanks & files;
            }
        }

        /// <summary>
        /// Calculates values for LineBB and BetweenBB, must be done after the diagonals have been calculated.
        /// </summary>
        private static void DoBetweenBBs()
        {
            LineBB    = (ulong**) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
            BetweenBB = (ulong**) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

            for (int s1 = 0; s1 < 64; s1++)
            {
                LineBB[s1]    = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);
                BetweenBB[s1] = (ulong*) AlignedAllocZeroed(sizeof(ulong) * SquareNB, AllocAlignment);

                int f1 = GetIndexFile(s1);
                int r1 = GetIndexRank(s1);
                for (int s2 = 0; s2 < 64; s2++)
                {
                    int f2 = GetIndexFile(s2);
                    int r2 = GetIndexRank(s2);
                    LineBB[s1][s2] |= SquareBB[s2];

                    if (OnSameDiagonal(s1, s2, out DiagonalInfo info))
                    {
                        int[] arr = (info.direction == Diagonal.Diagonal_A1H8) ? DiagonalIndicesA1H8[s1] : DiagonalIndicesA8H1[s1];
                        for (int i = Math.Max(info.i1, info.i2) - 1; i > Math.Min(info.i1, info.i2); i--)
                        {
                            LineBB[s1][s2] |= SquareBB[arr[i]];
                            BetweenBB[s1][s2] |= SquareBB[arr[i]];
                        }
                    }

                    if (f1 == f2)
                    {
                        if (s1 > s2)
                        {
                            for (int i = s1 - 8; i > s2; i -= 8)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                        else
                        {
                            for (int i = s1 + 8; i < s2; i += 8)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                    }
                    else if (r1 == r2)
                    {
                        if (s1 > s2)
                        {
                            for (int i = s1 - 1; i > s2; i--)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                        else
                        {
                            for (int i = s1 + 1; i < s2; i++)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                    }

                }
            }
        }

        private static void DoReductionTable()
        {
            for (int depth = 0; depth < MaxPly; depth++)
            {
                LogarithmicReductionTable[depth] = new int[MaxListCapacity];
                for (int moveIndex = 0; moveIndex < MaxListCapacity; moveIndex++)
                {
                    //LogarithmicReductionTable[depth][moveIndex] = (int)(Math.Log(depth) * Math.Log(moveIndex) / 2 - 0.3);
                    LogarithmicReductionTable[depth][moveIndex] = (int)(Math.Log(depth) * Math.Log(moveIndex) / 2.25 + 0.25);

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
                LMPTable[not_improving][depth] = (3 + depth * depth) / 2;
                LMPTable[improving][depth] = (3 + depth * depth);
            }
        }




        /// <summary>
        /// Returns true if <paramref name="index1"/> and <paramref name="index2"/> exist on the same diagonal.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="diagonal">Set to the Diagonal that the two indicies share, or Diagonal.Diagonal_A1H8 if they don't.</param>
        /// <param name="iIndex1">The index that <paramref name="index1"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        /// <param name="iIndex2">The index that <paramref name="index2"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        [MethodImpl(Inline)]
        private static unsafe bool DetOnSameDiagonal(int index1, int index2, int direction, out int iIndex1, out int iIndex2)
        {
            if (direction == Diagonal.Diagonal_A1H8)
            {
                iIndex1 = iIndex2 = 8;
                ulong d1 = DiagonalMasksA1H8[index1];
                if ((d1 & (SquareBB[index2])) != 0)
                {
                    int pops = 0;
                    while (d1 != 0)
                    {
                        int idx = lsb(d1);
                        d1 = poplsb(d1);

                        if (idx == index1)
                        {
                            iIndex1 = pops;
                        }
                        if (idx == index2)
                        {
                            iIndex2 = pops;
                        }
                        if (iIndex1 != 8 && iIndex2 != 8)
                        {
                            return true;
                        }

                        pops++;
                    }
                }
            }
            else
            {
                iIndex1 = iIndex2 = 8;
                ulong d2 = DiagonalMasksA8H1[index1];
                if ((d2 & (SquareBB[index2])) != 0)
                {
                    int pops = 0;
                    while (d2 != 0)
                    {
                        int idx = msb(d2);

                        if (idx == index1)
                        {
                            iIndex1 = pops;
                        }
                        if (idx == index2)
                        {
                            iIndex2 = pops;
                        }
                        if (iIndex1 != 8 && iIndex2 != 8)
                        {
                            return true;
                        }

                        d2 = popmsb(d2);
                        pops++;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="index1"/> and <paramref name="index2"/> exist on the same diagonal.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="diagonal">Set to the Diagonal that the two indicies share, or Diagonal.Diagonal_A1H8 if they don't.</param>
        /// <param name="iIndex1">The index that <paramref name="index1"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        /// <param name="iIndex2">The index that <paramref name="index2"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        [MethodImpl(Inline)]
        public static unsafe bool OnSameDiagonal(int index1, int index2, out DiagonalInfo info)
        {
            info = InfoA1H8[index1][index2];
            if (info.onSame)
            {
                return true;
            }

            info = InfoA8H1[index1][index2];
            if (info.onSame)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Returns true if <paramref name="x"/> and <paramref name="y"/> are both between 0 and 7.
        /// </summary>
        [MethodImpl(Inline)]
        private static bool InBounds(int x, int y)
        {
            return (x >= 0 && x <= 7 && y >= 0 && y <= 7);
        }
    }

    public readonly struct DiagonalInfo
    {
        public readonly int index1;
        public readonly int index2;
        public readonly int direction;
        public readonly bool onSame;
        public readonly int i1;
        public readonly int i2;

        public DiagonalInfo(int index1, int index2, int direction, bool onSame, int i1, int i2)
        {
            this.index1 = index1;
            this.index2 = index2;
            this.direction = direction;
            this.onSame = onSame;
            this.i1 = i1;
            this.i2 = i2;
        }
    }


}
