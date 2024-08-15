
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Lizard.Logic.Magic
{
    public static unsafe class MagicBitboards
    {
        private static FancyMagicSquare* FancyRookMagics;
        private static FancyMagicSquare* FancyBishopMagics;

        private static ulong* RookTable;
        private static ulong* BishopTable;

        /// <summary>
        /// Contains bitboards whose bits are set where blockers for a rook on a given square could be
        /// </summary>
        private static ulong[] RookBlockerMask = new ulong[64];

        /// <summary>
        /// Contains bitboards whose bits are set where blockers for a bishop on a given square could be
        /// </summary>
        private static ulong[] BishopBlockerMask = new ulong[64];

        /// <summary>
        /// For a given index, contains every possible combination of blockers for a rook at that index
        /// </summary>
        private static ulong[][] RookBlockerBoards = new ulong[64][];

        private static ulong[][] RookAttackBoards = new ulong[64][];

        /// <summary>
        /// For a given index, contains every possible combination of blockers for a bishop at that index
        /// </summary>
        private static ulong[][] BishopBlockerBoards = new ulong[64][];
        private static ulong[][] BishopAttackBoards = new ulong[64][];

        private static MagicSquare[] RookMagics;
        private static MagicSquare[] BishopMagics;


        public static readonly bool UsePext = true;
        static MagicBitboards()
        {
            GenAllBlockerBoards();
            RookMagics = InitializeMagics(Piece.Rook);
            BishopMagics = InitializeMagics(Piece.Bishop);

            if (!Bmi2.X64.IsSupported)
            {
                //  If your CPU doesn't have pext, then we can't call InitializeFancyMagics at all.
                UsePext = false;
                return;
            }

            RookTable = AlignedAllocZeroed<ulong>(0x19000);
            BishopTable = AlignedAllocZeroed<ulong>(0x19000);
            FancyRookMagics = InitializeFancyMagics(Piece.Rook, RookTable);
            FancyBishopMagics = InitializeFancyMagics(Piece.Bishop, BishopTable);

            //  Check if using pext is faster than the normal way.
            //  Idea slightly modified from Pedantic:
            //  https://github.com/JoAnnP38/Pedantic/blob/master/Pedantic.Chess/BoardPext.cs#L153
            {
                ulong temp = 0;

                Stopwatch sw = Stopwatch.StartNew();
                long fancyElapsed = 0;
                long normalElapsed = 0;

                const int TestIters = 50;
                for (int n = 0; n < TestIters; n++)
                {
                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextNormal(blockers, sq);

                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextFancy(blockers, sq);
                }

                for (int n = 0; n < TestIters; n++)
                {
                    sw.Restart();
                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextFancy(blockers, sq);
                    if (n > 0) fancyElapsed += sw.ElapsedTicks;

                    sw.Restart();
                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextNormal(blockers, sq);
                    if (n > 0) normalElapsed += sw.ElapsedTicks;

                    sw.Restart();
                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextFancy(blockers, sq);
                    if (n > 0) fancyElapsed += sw.ElapsedTicks;

                    sw.Restart();
                    for (int m = 0; m < 100; m++)
                        foreach ((int sq, ulong blockers) in pextTests)
                            temp ^= TestPextNormal(blockers, sq);
                    if (n > 0) normalElapsed += sw.ElapsedTicks;
                }

                UsePext = (fancyElapsed < normalElapsed);

                if (UsePext) Log($"Pext enabled for a {normalElapsed / (double)fancyElapsed:N3}x speedup");
            }

            if (UsePext)
            {
                //  We will be using the fancy magics, so free the memory for these
                RookMagics = null;
                RookAttackBoards = null;
                RookBlockerBoards = null;
                RookBlockerMask = null;

                BishopMagics = null;
                BishopAttackBoards = null;
                BishopBlockerBoards = null;
                BishopBlockerMask = null;
            }
            else
            {
                //  We will be using the normal magics, so free the memory of the fancy ones
                NativeMemory.AlignedFree(RookTable);
                NativeMemory.AlignedFree(FancyRookMagics);

                NativeMemory.AlignedFree(BishopTable);
                NativeMemory.AlignedFree(FancyBishopMagics);
            }

        }


        /// <summary>
        /// Returns all of the squares that a rook on <paramref name="idx"/> could move to.
        /// <br></br>
        /// <paramref name="boardAll"/> should be a mask of every piece on the board, 
        /// and the returned mask treats the mask <paramref name="boardAll"/> as if every piece can be captured.
        /// Friendly pieces will need to be masked out so we aren't able to capture them.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetRookMoves(ulong boardAll, int idx)
        {
            if (UsePext)
            {
                FancyMagicSquare m = FancyRookMagics[idx];
                return m.attacks[pext(boardAll, m.mask)];
            }
            else
            {
                MagicSquare m = RookMagics[idx];
                return m.attacks[((boardAll & m.mask) * m.number) >> m.shift];
            }
        }

        /// <summary>
        /// Returns all of the squares that a bishop on <paramref name="idx"/> could move to.
        /// <br></br>
        /// <paramref name="boardAll"/> should be a mask of every piece on the board, 
        /// and the returned mask treats the mask <paramref name="boardAll"/> as if every piece can be captured.
        /// Friendly pieces will need to be masked out so we aren't able to capture them.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetBishopMoves(ulong boardAll, int idx)
        {
            if (UsePext)
            {
                FancyMagicSquare m = FancyBishopMagics[idx];
                return m.attacks[pext(boardAll, m.mask)];
            }
            else
            {
                MagicSquare m = BishopMagics[idx];
                return m.attacks[((boardAll & m.mask) * m.number) >> m.shift];
            }
        }

        private static ulong TestPextFancy(ulong boardAll, int idx)
        {
            FancyMagicSquare bm = FancyBishopMagics[idx];
            FancyMagicSquare rm = FancyRookMagics[idx];
            return (bm.attacks[pext(boardAll, bm.mask)] | rm.attacks[pext(boardAll, rm.mask)]);
        }

        private static ulong TestPextNormal(ulong boardAll, int idx)
        {
            MagicSquare bm = BishopMagics[idx];
            MagicSquare rm = RookMagics[idx];
            return (bm.attacks[((boardAll & bm.mask) * bm.number) >> bm.shift] | rm.attacks[((boardAll & rm.mask) * rm.number) >> rm.shift]);
        }


        /// <summary>
        /// Sets up the "fancy" magic squares for the given piece type <paramref name="pt"/>.
        /// <br></br>
        /// If your CPU supports Bmi2, then it is able to use ParallelBitExtract to calculate attack indices
        /// rather than having to mask the board occupancy, multiply that by the magic number, and bit shift.
        /// 
        /// <para></para>
        /// 
        /// Using Pext with move generation is about 5% faster in my testing, which adds up over time.
        /// </summary>
        private static FancyMagicSquare* InitializeFancyMagics(int pt, ulong* table)
        {
            FancyMagicSquare* magicArray = AlignedAllocZeroed<FancyMagicSquare>(64);

            ulong b;
            int size = 0;
            for (int sq = A1; sq <= H8; sq++)
            {
                ref FancyMagicSquare m = ref magicArray[sq];
                m.mask = GetBlockerMask(pt, sq);
                m.shift = (int)(64 - popcount(m.mask));
                if (sq == A1)
                {
                    m.attacks = (ulong*)(table + 0);
                }
                else
                {
                    m.attacks = magicArray[sq - 1].attacks + size;
                }

                b = 0;
                size = 0;
                do
                {
                    m.attacks[pext(b, m.mask)] = SlidingAttacks(pt, sq, b);
                    size++;
                    b = (b - m.mask) & m.mask;
                }
                while (b != 0);
            }

            return magicArray;
        }

        private static MagicSquare[] InitializeMagics(int pt)
        {
            ulong[] blockerMasks = (pt == Piece.Bishop) ? BishopBlockerMask : RookBlockerMask;
            ulong[][] blockerBoards = (pt == Piece.Bishop) ? BishopBlockerBoards : RookBlockerBoards;
            ulong[][] attackBoards = (pt == Piece.Bishop) ? BishopAttackBoards : RookAttackBoards;
            int[] bits = (pt == Piece.Bishop) ? BishopBits : RookBits;
            ulong[] magics = (pt == Piece.Bishop) ? BishopMagicNumbers : RookMagicNumbers;

            MagicSquare[] magicArray = new MagicSquare[64];

            for (int sq = A1; sq <= H8; sq++)
            {
                MagicSquare newMagic = new MagicSquare
                {
                    number = magics[sq],
                    shift = 64 - bits[sq],
                    mask = blockerMasks[sq],
                    attacks = new ulong[1 << bits[sq]]
                };

                //  Check every combination of blockers
                for (int blockerIndex = 0; blockerIndex < blockerBoards[sq].Length; blockerIndex++)
                {
                    ulong indexMap = blockerBoards[sq][blockerIndex] * newMagic.number;
                    ulong attackIndex = indexMap >> newMagic.shift;

                    //  This magic doesn't work
                    if (newMagic.attacks[attackIndex] != 0 && newMagic.attacks[attackIndex] != attackBoards[sq][blockerIndex])
                    {
                        Assert(false,
                            "Magic number for " + pt + " on " + sq + " should have worked, but doesn't!");
                        break;
                    }

                    //  Magic works for this blocker index, so add the attack to the square.
                    newMagic.attacks[attackIndex] = attackBoards[sq][blockerIndex];
                }

                magicArray[sq] = newMagic;
            }

            return magicArray;
        }

        private static void GenAllBlockerBoards()
        {
            for (int square = 0; square < 64; square++)
            {
                RookBlockerMask[square] = GetBlockerMask(Piece.Rook, square);
                int rbits = (int)popcount(RookBlockerMask[square]);
                RookAttackBoards[square] = new ulong[(1 << rbits)];
                RookBlockerBoards[square] = new ulong[(1 << rbits)];
                for (int i = 0; i < (1 << rbits); i++)
                {
                    ulong blockBoard = GenBlockerBoard(i, RookBlockerMask[square]);
                    ulong attackBoard = SlidingAttacks(Piece.Rook, square, blockBoard);

                    RookAttackBoards[square][i] = attackBoard;
                    RookBlockerBoards[square][i] = blockBoard;
                }

                BishopBlockerMask[square] = GetBlockerMask(Piece.Bishop, square);
                int bbits = (int)popcount(BishopBlockerMask[square]);
                BishopAttackBoards[square] = new ulong[(1 << bbits)];
                BishopBlockerBoards[square] = new ulong[(1 << bbits)];

                for (int i = 0; i < (1 << bbits); i++)
                {
                    ulong blockBoard = GenBlockerBoard(i, BishopBlockerMask[square]);
                    ulong attackBoard = SlidingAttacks(Piece.Bishop, square, blockBoard);

                    BishopAttackBoards[square][i] = attackBoard;
                    BishopBlockerBoards[square][i] = blockBoard;
                }
            }

            // Generate a unique blocker board, given an index (0..2^bits) and the blocker mask for the piece/square.
            // Each index will give a unique blocker board. 
            // https://stackoverflow.com/questions/30680559/how-to-find-magic-bitboards
            static ulong GenBlockerBoard(int index, ulong blockermask)
            {
                ulong blockerboard = blockermask;

                short bitindex = 0;
                for (short i = 0; i < 64; i++)
                {
                    if ((blockermask & (1UL << i)) != 0)
                    {
                        if ((index & (1 << bitindex)) == 0)
                        {
                            blockerboard &= ~(1UL << i);
                        }
                        bitindex++;
                    }
                }
                return blockerboard;
            }
        }


        private static int[] RookBits = {
            12, 11, 11, 11, 11, 11, 11, 12,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            11, 10, 10, 10, 10, 10, 10, 11,
            12, 11, 11, 11, 11, 11, 11, 12,
        };

        private static int[] BishopBits = {
            6, 5, 5, 5, 5, 5, 5, 6,
            5, 5, 5, 5, 5, 5, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 9, 9, 7, 5, 5,
            5, 5, 7, 7, 7, 7, 5, 5,
            5, 5, 5, 5, 5, 5, 5, 5,
            6, 5, 5, 5, 5, 5, 5, 6,
        };


        /// <summary>
        /// Returns a mask containing every square the piece on <paramref name="idx"/> can move to on the board <paramref name="occupied"/>.
        /// Excludes all edges of the board unless the piece is on that edge. So a rook on A1 has every bit along the A file and 1st rank set,
        /// except for A8 and H1.
        /// </summary>
        private static ulong SlidingAttacks(int pt, int idx, ulong occupied)
        {
            ulong mask = 0UL;

            if (pt == Bishop)
            {
                foreach (int dir in new int[] { Direction.NORTH_EAST, Direction.NORTH_WEST, Direction.SOUTH_EAST, Direction.SOUTH_WEST })
                {
                    int tempSq = idx;
                    while (DirectionOK(tempSq, dir))
                    {
                        //  Keep moving in this direction until we move off the board, masking in squares along the way.
                        tempSq += dir;
                        mask |= 1UL << tempSq;

                        if ((occupied & (1UL << tempSq)) != 0)
                        {
                            //  If we get to an occupied square, then stop moving in this direction.
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (int dir in new int[] { Direction.NORTH, Direction.EAST, Direction.SOUTH, Direction.WEST })
                {
                    int tempSq = idx;
                    while (DirectionOK(tempSq, dir))
                    {
                        //  Keep moving in this direction until we move off the board, masking in squares along the way.
                        tempSq += dir;
                        mask |= 1UL << tempSq;

                        if ((occupied & (1UL << tempSq)) != 0)
                        {
                            //  If we get to an occupied square, then stop moving in this direction.
                            break;
                        }
                    }
                }
            }

            return mask;
        }

        /// <summary>
        /// Returns a mask containing every square the piece on <paramref name="idx"/> can move to on an empty board.
        /// Excludes all edges of the board unless the piece is on that edge. So a rook on A1 has every bit along the A file and 1st rank set,
        /// except for A8 and H1.
        /// </summary>
        private static ulong GetBlockerMask(int pt, int idx)
        {
            ulong mask = (pt == Piece.Bishop) ? BishopRay(idx) : RookRay(idx);

            int rank = idx >> 3;
            int file = idx & 7;
            if (rank == 7)
            {
                mask &= ~Rank1BB;
            }
            else if (rank == 0)
            {
                mask &= ~Rank8BB;
            }
            else
            {
                mask &= ~Rank1BB & ~Rank8BB;
            }

            if (file == 0)
            {
                mask &= ~FileHBB;
            }
            else if (file == 7)
            {
                mask &= ~FileABB;
            }
            else
            {
                mask &= ~FileHBB & ~FileABB;
            }

            return mask;


            static ulong RookRay(int sq)
            {
                return (GetFileBB(sq) | GetRankBB(sq)) & ~(1UL << sq);
            }

            static ulong BishopRay(int sq)
            {
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
                return bishopMask;
            }


        }



        private static readonly ulong[] RookMagicNumbers =
        {
            0x1080002084104000, 0x004000B002200840, 0x0480200080300018, 0x0500042010010008, 0x0480140080180086, 0x8080020013800400, 0x0100020007000184, 0x01000200E0864100,
            0x08008000C0002082, 0x800200420A608102, 0x0022004091820420, 0x0211000820100302, 0x002080040080C800, 0x2000800200800400, 0x0084000201080410, 0x4046000200840043,
            0x0040008002842040, 0x090282802000C000, 0x8100150041022000, 0x18400A0020420010, 0x00000D0010080100, 0x1244808004000200, 0x0400040006281011, 0x10020A0010408401,
            0x0200408200210201, 0x0000400080201082, 0x0010200100110044, 0x8310008900210030, 0x0208008500185100, 0x2004008080020004, 0x4023100400020801, 0x0841002500004A8A,
            0x0840002042800480, 0x0040100800200023, 0x0081100080802000, 0xCC401042020020C8, 0x0000080082800400, 0x0001000401000608, 0x18000A0104001810, 0x5000114882000401,
            0x0001804000238000, 0x0C90002004484000, 0x000A001042820021, 0x0202100008008080, 0x043268000C008080, 0x000C0010A0140108, 0x01901026182C0001, 0x2430008245020034,
            0x48FFFE99FECFAA00, 0x48FFFE99FECFAA00, 0x497FFFADFF9C2E00, 0x613FFFDDFFCE9200, 0xFFFFFFE9FFE7CE00, 0xFFFFFFF5FFF3E600, 0x0003FF95E5E6A4C0, 0x510FFFF5F63C96A0,
            0xEBFFFFB9FF9FC526, 0x61FFFEDDFEEDAEAE, 0x53BFFFEDFFDEB1A2, 0x127FFFB9FFDFB5F6, 0x411FFFDDFFDBF4D6, 0x020100080A64000F, 0x0003FFEF27EEBE74, 0x7645FFFECBFEA79E,
        };

        private static readonly ulong[] BishopMagicNumbers =
        {
            0xFFEDF9FD7CFCFFFF, 0xFC0962854A77F576, 0x804200860088A020, 0x0004040080280011, 0x0004104500041401, 0x500A021044200010, 0xFC0A66C64A7EF576, 0x7FFDFDFCBD79FFFF,
            0xFC0846A64A34FFF6, 0xFC087A874A3CF7F6, 0x180430A082014008, 0x9008040400800400, 0xA600040308340808, 0x0010020222200A00, 0xFC0864AE59B4FF76, 0x3C0860AF4B35FF76,
            0x73C01AF56CF4CFFB, 0x41A01CFAD64AAFFC, 0x2108021000401022, 0x0008004220244045, 0x6012001012100031, 0x1002000088010800, 0x7C0C028F5B34FF76, 0xFC0A028E5AB4DF76,
            0x00A0200110043909, 0x001008000202040C, 0x0804100012088012, 0x8010C88008020040, 0x0040840200802000, 0x0012022004100804, 0x100A0E0100A19011, 0x0204004058210400,
            0x6A30980506481001, 0x3420901081050400, 0x0002020100020804, 0x5000020081080181, 0x54042100300C0040, 0x0002480040460044, 0x58502A020194C900, 0x0A0204050000208C,
            0xDCEFD9B54BFCC09F, 0xF95FFA765AFD602B, 0x0113140201080804, 0x6001004202813802, 0x0400601410400405, 0x0001810102000100, 0x43FF9A5CF4CA0C01, 0x4BFFCD8E7C587601,
            0xFC0FF2865334F576, 0xFC0BF6CE5924F576, 0x0102010041100810, 0x0202000020881080, 0x0302004110824100, 0xA084C00803010000, 0xC3FFB7DC36CA8C89, 0xC3FF8A54F4CA2C89,
            0xFFFFFCFCFD79EDFF, 0xFC0863FCCB147576, 0x0100112201048800, 0x0800000080208838, 0x8080200010020202, 0x42002C0484080201, 0xFC087E8E4BB2F736, 0x43FF9E4EF4CA2C89,
        };


        /// <summary>
        /// Entries from Pedantic: https://github.com/JoAnnP38/Pedantic/blob/master/Pedantic.Chess/BoardPext.cs#L153
        /// </summary>
        private static readonly (int sq, ulong blockers)[] pextTests =
        [
            (56, 0x69EB3C000828EF65ul), (29, 0x000002C424040000ul), (58, 0x042001002080A000ul), (49, 0x0002B001A0284100ul),
            ( 8, 0x40E0080C2430F308ul), (56, 0x6DF96604043AF36Bul), (35, 0x000046E84EAC0048ul), (56, 0x6DE0221C033CC795ul),
            (57, 0x0262A21050244000ul), (27, 0x08080DA019C42442ul), ( 9, 0x7987100B8D00F261ul), (56, 0x01500060E6240501ul),
            ( 9, 0x52E23D10A650CA68ul), (56, 0x51EE490308CC6B60ul), (46, 0x0001400441500000ul), ( 0, 0xC07E095897602461ul),
            (19, 0xB4EB34080A987D62ul), (10, 0x11466094516C850Cul), (30, 0x08C4200748824001ul), (57, 0x4200789228408000ul),
            (26, 0x08AA3180B46A0B00ul), (62, 0x4080000000200001ul), (12, 0x943413C00E10578Eul), (56, 0x416A7104003FC248ul),
            (33, 0x0010006301060000ul), (29, 0x00000501E6120000ul), (59, 0x2C36C611159654A1ul), (19, 0x00000402A2082000ul),
            (41, 0x0001235050000000ul), (47, 0xDDA3E41890265F8Cul), (43, 0x00002CAC8C407100ul), (51, 0x00894A0A49202000ul),
            (62, 0xC0A1F21848218860ul), (26, 0x00B04A8505B06042ul), (26, 0x00100022A4048500ul), (37, 0x400106240E010002ul),
            (10, 0x404040C283010600ul), (19, 0x2860A4002AE86000ul), (20, 0x80211A00F011DA40ul), (35, 0x0018208802D00000ul),
            (16, 0x5860C22B9D557808ul), (19, 0x59C75430A98CDA71ul), (20, 0x4010684B08110060ul), (61, 0x206C38851F9A49A2ul),
            (14, 0x0079D1048C487040ul), (58, 0x14EE8C033816C260ul), (21, 0xB6E42D101126FB69ul), (56, 0x9572B1118806D68Cul),
            (59, 0x68E2570C04D46F49ul), (38, 0x9979D4580825E361ul), (59, 0x68E10422005AF000ul), (60, 0x50622404A088E000ul),
            (12, 0x0586C0350002F388ul), (56, 0x6DFF00901900E744ul), (14, 0x2861A8AA74004302ul), (38, 0x040101F498522144ul),
            (34, 0x0206115C00800002ul), (20, 0x606B907808B06B44ul), (59, 0x0880DA200C6BC910ul), (23, 0x01001011058C0000ul),
            (58, 0x84C129100444B040ul), (50, 0x0004408839418000ul), (56, 0xB5E912143408C961ul), ( 0, 0x00C2220C00D2A195ul)
        ];
    }
}
