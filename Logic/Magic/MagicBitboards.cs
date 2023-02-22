using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LTChess.Magic
{
    public static class MagicBitboards
    {
        /// <summary>
        /// Contains bitboards whose bits are set where blockers for a rook on a given square could be
        /// </summary>
        public static ulong[] RookBlockerMask = new ulong[64];

        /// <summary>
        /// Contains bitboards whose bits are set where blockers for a bishop on a given square could be
        /// </summary>
        public static ulong[] BishopBlockerMask = new ulong[64];

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

        public static MagicSquare[] RookMagics;
        public static MagicSquare[] BishopMagics;
        private static Random rand;

        public static bool UseBetterMagics = true;
        public static bool UseKnownMagics = true;

        private static bool Initialized = false;

        static MagicBitboards()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            rand = new Random();
            DoBlockerMasks();
            GenAllBlockerBoards();
            RookMagics = InitializeMagics(Piece.Rook);
            BishopMagics = InitializeMagics(Piece.Bishop);
            Initialized = true;
        }

        public static void Recalculate()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Initialize();
            sw.Stop();
            Log("MagicBitboards done in " + sw.Elapsed.TotalSeconds + " s");
        }


        private static void DoBlockerMasks()
        {
            for (int idx = 0; idx < 64; idx++)
            {
                ulong rookMask = GetBlockerMask(Piece.Rook, idx);
                ulong bishopMask = GetBlockerMask(Piece.Bishop, idx);

                RookBlockerMask[idx] = rookMask;
                BishopBlockerMask[idx] = bishopMask;
            }
        }

        // Generate a unique blocker board, given an index (0..2^bits) and the blocker mask for the piece/square.
        // Each index will give a unique blocker board. 
        // https://stackoverflow.com/questions/30680559/how-to-find-magic-bitboards
        private static ulong GenBlockerBoard(int index, ulong blockermask)
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

        private static void GenAllBlockerBoards()
        {
            for (int square = 0; square < 64; square++)
            {
                int rbits = (int)popcount(RookBlockerMask[square]);
                int bbits = (int)popcount(BishopBlockerMask[square]);

                RookAttackBoards[square] = new ulong[(1 << rbits)];
                RookBlockerBoards[square] = new ulong[(1 << rbits)];

                BishopAttackBoards[square] = new ulong[(1 << bbits)];
                BishopBlockerBoards[square] = new ulong[(1 << bbits)];

                for (int i = 0; i < (1 << rbits); i++)
                {
                    ulong blockBoard = GenBlockerBoard(i, RookBlockerMask[square]);
                    ulong attackBoard = SlidingAttacks(Piece.Rook, square, blockBoard);

                    RookAttackBoards[square][i] = attackBoard;
                    RookBlockerBoards[square][i] = blockBoard;
                }
                for (int i = 0; i < (1 << bbits); i++)
                {
                    ulong blockBoard = GenBlockerBoard(i, BishopBlockerMask[square]);
                    ulong attackBoard = SlidingAttacks(Piece.Bishop, square, blockBoard);

                    BishopAttackBoards[square][i] = attackBoard;
                    BishopBlockerBoards[square][i] = blockBoard;
                }
            }
        }

        [MethodImpl(Inline)]
        public static ulong GetRookMoves(ulong boardAll, int idx)
        {
            MagicSquare m = RookMagics[idx];
            return m.attacks[((boardAll & m.mask) * m.number) >> m.shift];
        }

        [MethodImpl(Inline)]
        public static ulong GetBishopMoves(ulong boardAll, int idx)
        {
            MagicSquare m = BishopMagics[idx];
            return m.attacks[((boardAll & m.mask) * m.number) >> m.shift];
        }

        private static MagicSquare[] InitializeMagics(int pt)
        {
            ulong[] blockerMasks = RookBlockerMask;
            ulong[][] blockerBoards = RookBlockerBoards;
            ulong[][] attackBoards = RookAttackBoards;
            int[] bits = RookBits;
            ulong[] BetterMagics = BetterRookMagics;
            ulong[] KnownMagics = KnownRookMagics;

            if (pt == Piece.Bishop)
            {
                blockerMasks = BishopBlockerMask;
                blockerBoards = BishopBlockerBoards;
                attackBoards = BishopAttackBoards;
                bits = BishopBits;
                BetterMagics = BetterBishopMagics;
                KnownMagics = KnownBishopMagics;
            }

            MagicSquare[] magicArray = new MagicSquare[64];

            for (int sq = A1; sq <= H8; sq++)
            {
                MagicSquare newMagic = new MagicSquare
                {
                    shift = (64 - bits[sq]),
                    mask = blockerMasks[sq],
                    attacks = new ulong[1 << bits[sq]]
                };

                bool MagicWorks = false;
                while (MagicWorks == false)
                {
                    MagicWorks = true;

                    if (BetterMagics[sq] != 0)
                    {
                        newMagic.number = BetterMagics[sq];
                        newMagic.shift++;
                        newMagic.attacks = new ulong[1 << (bits[sq] - 1)];
                    }
                    else if (UseKnownMagics)
                    {
                        newMagic.number = KnownMagics[sq];
                    }
                    else
                    {
                        //  Try a new magic number
                        newMagic.number = NewMagicNumber();
                    }

                    //  Check every combination of blockers
                    for (int blockerIndex = 0; blockerIndex < blockerBoards[sq].Length; blockerIndex++)
                    {
                        ulong indexMap = blockerBoards[sq][blockerIndex] * newMagic.number;
                        ulong attackIndex = indexMap >> newMagic.shift;

                        //  This magic doesn't work
                        if (newMagic.attacks[attackIndex] != 0 && newMagic.attacks[attackIndex] != attackBoards[sq][blockerIndex])
                        {
                            Array.Clear(newMagic.attacks);
                            MagicWorks = false;
                            break;
                        }

                        //  Magic works for this blocker index, so add the attack to the square.
                        newMagic.attacks[attackIndex] = attackBoards[sq][blockerIndex];
                    }
                }

                magicArray[sq] = newMagic;
            }

            return magicArray;
        }

        private static ulong NewMagicNumber()
        {
            //  It is faster to & together multiple numbers since we want numbers with fewer 1 bits than 0 bits.
            //  3 seems like the sweet spot?
            return rand.NextUlong() & rand.NextUlong() & rand.NextUlong();
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
        /// https://www.chessprogramming.org/Best_Magics_so_far
        /// All of the non-zero numbers here use 1 fewer bit than normal.
        /// </summary>
        private static ulong[] BetterRookMagics =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0x48FFFE99FECFAA00, 0x48FFFE99FECFAA00, 0x497FFFADFF9C2E00, 0x613FFFDDFFCE9200, 0xffffffe9ffe7ce00, 0xfffffff5fff3e600, 0x0003ff95e5e6a4c0, 0x510FFFF5F63C96A0,
            0xEBFFFFB9FF9FC526, 0x61FFFEDDFEEDAEAE, 0x53BFFFEDFFDEB1A2, 0x127FFFB9FFDFB5F6, 0x411FFFDDFFDBF4D6, 0, 0x0003ffef27eebe74, 0x7645FFFECBFEA79E,
        };


        /// <summary>
        /// https://www.chessprogramming.org/Best_Magics_so_far
        /// All of the non-zero numbers here use 1 fewer bit than normal.
        /// </summary>
        private static ulong[] BetterBishopMagics =
        {
            0xffedf9fd7cfcffff, 0xfc0962854a77f576, 0, 0, 0, 0, 0xfc0a66c64a7ef576, 0x7ffdfdfcbd79ffff,
            0xfc0846a64a34fff6, 0xfc087a874a3cf7f6, 0, 0, 0, 0, 0xfc0864ae59b4ff76, 0x3c0860af4b35ff76,
            0x73C01AF56CF4CFFB, 0x41A01CFAD64AAFFC, 0, 0, 0, 0, 0x7c0c028f5b34ff76, 0xfc0a028e5ab4df76,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0,
            0xDCEFD9B54BFCC09F, 0xF95FFA765AFD602B, 0, 0, 0, 0, 0x43ff9a5cf4ca0c01, 0x4BFFCD8E7C587601,
            0xfc0ff2865334f576, 0xfc0bf6ce5924f576, 0, 0, 0, 0, 0xc3ffb7dc36ca8c89, 0xc3ff8a54f4ca2c89,
            0xfffffcfcfd79edff, 0xfc0863fccb147576, 0, 0, 0, 0, 0xfc087e8e4bb2f736, 0x43ff9e4ef4ca2c89,
        };


        /// <summary>
        /// Magics I've already found so we don't waste time at startup...
        /// </summary>
        private static ulong[] KnownRookMagics =
        {
            0x1080002084104000, 0x4000B002200840, 0x480200080300018, 0x500042010010008, 0x480140080180086, 0x8080020013800400, 0x100020007000184, 0x1000200E0864100,
            0x8008000C0002082, 0x800200420A608102, 0x22004091820420, 0x211000820100302, 0x2080040080C800, 0x2000800200800400, 0x84000201080410, 0x4046000200840043,
            0x40008002842040, 0x90282802000C000, 0x8100150041022000, 0x18400A0020420010, 0xD0010080100, 0x1244808004000200, 0x400040006281011, 0x10020A0010408401,
            0x200408200210201, 0x400080201082, 0x10200100110044, 0x8310008900210030, 0x208008500185100, 0x2004008080020004, 0x4023100400020801, 0x841002500004A8A,
            0x840002042800480, 0x40100800200023, 0x81100080802000, 0xCC401042020020C8, 0x80082800400, 0x1000401000608, 0x18000A0104001810, 0x5000114882000401,
            0x1804000238000, 0xC90002004484000, 0xA001042820021, 0x202100008008080, 0x43268000C008080, 0xC0010A0140108, 0x1901026182C0001, 0x2430008245020034,
            0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0x20100080A64000F, 0, 0,
        };

        private static ulong[] KnownBishopMagics =
        {
            0, 0, 0x804200860088A020, 0x4040080280011, 0x4104500041401, 0x500A021044200010, 0, 0,
            0, 0, 0x180430A082014008, 0x9008040400800400, 0xA600040308340808, 0x10020222200A00, 0, 0,
            0, 0, 0x2108021000401022, 0x8004220244045, 0x6012001012100031, 0x1002000088010800, 0, 0,
            0xA0200110043909, 0x1008000202040C, 0x804100012088012, 0x8010C88008020040, 0x40840200802000, 0x12022004100804, 0x100A0E0100A19011, 0x204004058210400,
            0x6A30980506481001, 0x3420901081050400, 0x2020100020804, 0x5000020081080181, 0x54042100300C0040, 0x2480040460044, 0x58502A020194C900, 0xA0204050000208C,
            0, 0, 0x113140201080804, 0x6001004202813802, 0x400601410400405, 0x1810102000100, 0, 0,
            0, 0, 0x102010041100810, 0x202000020881080, 0x302004110824100, 0xA084C00803010000, 0, 0,
            0, 0, 0x100112201048800, 0x800000080208838, 0x8080200010020202, 0x42002C0484080201, 0, 0,
        };


    }
}
