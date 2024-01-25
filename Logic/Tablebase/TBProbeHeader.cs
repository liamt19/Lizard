﻿
/*

Translated from C to C# based on https://github.com/jdart1/Fathom, which uses the MIT license:

The MIT License (MIT)

Copyright (c) 2013-2018 Ronald de Man
Copyright (c) 2015 basil00
Copyright (c) 2016-2023 by Jon Dart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using static Lizard.Logic.Tablebase.Fathom;
using static Lizard.Logic.Tablebase.TBProbeHeader;
using static Lizard.Logic.Tablebase.TBChess;
using static Lizard.Logic.Tablebase.TBChessHeader;
using static Lizard.Logic.Tablebase.TBProbe;
using static Lizard.Logic.Tablebase.TBProbeCore;
using static Lizard.Logic.Tablebase.TBConfig;
using Lizard.Logic.Tablebase;

using TbMove = ushort;
using size_t = ulong;
using map_t = ulong;

using int8_t = sbyte;
using uint8_t = byte;
using int16_t = short;
using uint16_t = ushort;
using int32_t = int;
using uint32_t = uint;
using int64_t = long;
using uint64_t = ulong;

using unsigned = uint;
using Value = int;

using Lizard.Logic.Magic;
using System.IO.MemoryMappedFiles;

namespace Lizard.Logic.Tablebase
{
    public static unsafe class TBProbeHeader
    {
        #region Constants

        public const int PAWN = 1;
        public const int KNIGHT = 2;
        public const int BISHOP = 3;
        public const int ROOK = 4;
        public const int QUEEN = 5;
        public const int KING = 6;

        public const int W_PAWN = 1;
        public const int W_KNIGHT = 2;
        public const int W_BISHOP = 3;
        public const int W_ROOK = 4;
        public const int W_QUEEN = 5;
        public const int W_KING = 6;

        public const int B_PAWN = 9;
        public const int B_KNIGHT = 10;
        public const int B_BISHOP = 11;
        public const int B_ROOK = 12;
        public const int B_QUEEN = 13;
        public const int B_KING = 14;

        public const int BLACK = 0;
        public const int WHITE = 1;


        public const int WDL = 0;
        public const int DTM = 1;
        public const int DTZ = 2;

        public const int PIECE_ENC = 0;
        public const int FILE_ENC = 1;
        public const int RANK_ENC = 2;


        private static readonly int TB_PAWN = 1;
        private static readonly int TB_KNIGHT = 2;
        private static readonly int TB_BISHOP = 3;
        private static readonly int TB_ROOK = 4;
        private static readonly int TB_QUEEN = 5;
        private static readonly int TB_KING = 6;

        private static readonly int TB_WPAWN = TB_PAWN;
        private static readonly int TB_BPAWN = (TB_PAWN | 8);

        private static readonly int WHITE_KING = (TB_WPAWN + 5);
        private static readonly int WHITE_QUEEN = (TB_WPAWN + 4);
        private static readonly int WHITE_ROOK = (TB_WPAWN + 3);
        private static readonly int WHITE_BISHOP = (TB_WPAWN + 2);
        private static readonly int WHITE_KNIGHT = (TB_WPAWN + 1);
        private static readonly int WHITE_PAWN = TB_WPAWN;
        private static readonly int BLACK_KING = (TB_BPAWN + 5);
        private static readonly int BLACK_QUEEN = (TB_BPAWN + 4);
        private static readonly int BLACK_ROOK = (TB_BPAWN + 3);
        private static readonly int BLACK_BISHOP = (TB_BPAWN + 2);
        private static readonly int BLACK_KNIGHT = (TB_BPAWN + 1);
        private static readonly int BLACK_PAWN = TB_BPAWN;

        private static readonly ulong PRIME_WHITE_QUEEN = 11811845319353239651UL;
        private static readonly ulong PRIME_WHITE_ROOK = 10979190538029446137UL;
        private static readonly ulong PRIME_WHITE_BISHOP = 12311744257139811149UL;
        private static readonly ulong PRIME_WHITE_KNIGHT = 15202887380319082783UL;
        private static readonly ulong PRIME_WHITE_PAWN = 17008651141875982339UL;
        private static readonly ulong PRIME_BLACK_QUEEN = 15484752644942473553UL;
        private static readonly ulong PRIME_BLACK_ROOK = 18264461213049635989UL;
        private static readonly ulong PRIME_BLACK_BISHOP = 15394650811035483107UL;
        private static readonly ulong PRIME_BLACK_KNIGHT = 13469005675588064321UL;
        private static readonly ulong PRIME_BLACK_PAWN = 11695583624105689831UL;

        private static readonly ulong BOARD_RANK_EDGE = 0x8181818181818181UL;
        private static readonly ulong BOARD_FILE_EDGE = 0xFF000000000000FFUL;
        private static readonly ulong BOARD_EDGE = (BOARD_RANK_EDGE | BOARD_FILE_EDGE);
        private static readonly ulong BOARD_RANK_1 = 0x00000000000000FFUL;
        private static readonly ulong BOARD_FILE_A = 0x8080808080808080UL;

        public static readonly int KEY_KvK = 0;

        public static readonly ushort BEST_NONE = 0xFFFF;
        public static readonly ushort SCORE_ILLEGAL = 0x7FFF;





        public const int TB_PIECES = 5;
        public const int TB_HASHBITS = (TB_PIECES < 7 ? 11 : 12);
        public const int TB_MAX_PIECE = (TB_PIECES < 7 ? 254 : 650);
        public const int TB_MAX_PAWN = (TB_PIECES < 7 ? 256 : 861);
        public const int TB_MAX_SYMS = 4096;
        public const int TB_MAX_MOVES = (192 + 1);
        public const int TB_MAX_CAPTURES = 64;
        public const int TB_MAX_PLY = 256;
        public const int TB_CASTLING_K = 0x1;     /* White king-side. */
        public const int TB_CASTLING_Q = 0x2;     /* White queen-side. */
        public const int TB_CASTLING_k = 0x4;     /* Black king-side. */
        public const int TB_CASTLING_q = 0x8;     /* Black queen-side. */

        public const int TB_LOSS = 0;       /* LOSS */
        public const int TB_BLESSED_LOSS = 1;       /* LOSS but 50-move draw */
        public const int TB_DRAW = 2;       /* DRAW */
        public const int TB_CURSED_WIN = 3;       /* WIN but 50-move draw  */
        public const int TB_WIN = 4;       /* WIN  */

        public const int TB_PROMOTES_NONE = 0;
        public const int TB_PROMOTES_QUEEN = 1;
        public const int TB_PROMOTES_ROOK = 2;
        public const int TB_PROMOTES_BISHOP = 3;
        public const int TB_PROMOTES_KNIGHT = 4;

        public const int TB_RESULT_WDL_MASK = 0x0000000F;
        public const int TB_RESULT_TO_MASK = 0x000003F0;
        public const int TB_RESULT_FROM_MASK = 0x0000FC00;
        public const int TB_RESULT_PROMOTES_MASK = 0x00070000;
        public const int TB_RESULT_EP_MASK = 0x00080000;
        public const uint TB_RESULT_DTZ_MASK = 0xFFF00000;
        public const int TB_RESULT_WDL_SHIFT = 0;
        public const int TB_RESULT_TO_SHIFT = 4;
        public const int TB_RESULT_FROM_SHIFT = 10;
        public const int TB_RESULT_PROMOTES_SHIFT = 16;
        public const int TB_RESULT_EP_SHIFT = 19;
        public const int TB_RESULT_DTZ_SHIFT = 20;

        #endregion



        #region Static readonly stuff

        public static readonly uint TB_RESULT_CHECKMATE = TB_SET_WDL(0, TB_WIN);
        public static readonly uint TB_RESULT_STALEMATE = TB_SET_WDL(0, TB_DRAW);
        public static readonly uint TB_RESULT_FAILED = 0xFFFFFFFF;

        #endregion



        #region DEFINE Stuff

        public static int TB_GET_WDL(int _res) => (((_res) & TB_RESULT_WDL_MASK) >> TB_RESULT_WDL_SHIFT);

        public static int TB_GET_TO(int _res) => (((_res) & TB_RESULT_TO_MASK) >> TB_RESULT_TO_SHIFT);

        public static int TB_GET_FROM(int _res) => (((_res) & TB_RESULT_FROM_MASK) >> TB_RESULT_FROM_SHIFT);

        public static int TB_GET_PROMOTES(int _res) => (((_res) & TB_RESULT_PROMOTES_MASK) >> TB_RESULT_PROMOTES_SHIFT);

        public static int TB_GET_EP(int _res) => (((_res) & TB_RESULT_EP_MASK) >> TB_RESULT_EP_SHIFT);

        public static uint TB_GET_DTZ(uint _res) => ((((_res) & TB_RESULT_DTZ_MASK) >> TB_RESULT_DTZ_SHIFT));

        public static uint TB_SET_WDL(uint _res, int _wdl) => (uint)(((((_res) & ~TB_RESULT_WDL_MASK) | (((_wdl) << TB_RESULT_WDL_SHIFT) & TB_RESULT_WDL_MASK))));

        public static uint TB_SET_TO(uint _res, int _to) => (uint)(((_res) & ~TB_RESULT_TO_MASK) | (((_to) << TB_RESULT_TO_SHIFT) & TB_RESULT_TO_MASK));

        public static uint TB_SET_FROM(uint _res, int _from) => (uint)(((_res) & ~TB_RESULT_FROM_MASK) | (((_from) << TB_RESULT_FROM_SHIFT) & TB_RESULT_FROM_MASK));

        public static uint TB_SET_PROMOTES(uint _res, int _promotes) => (uint)(((_res) & ~TB_RESULT_PROMOTES_MASK) | (((_promotes) << TB_RESULT_PROMOTES_SHIFT) & TB_RESULT_PROMOTES_MASK));

        public static uint TB_SET_EP(uint _res, int _ep) => (uint)(((_res) & ~TB_RESULT_EP_MASK) | (((_ep) << TB_RESULT_EP_SHIFT) & TB_RESULT_EP_MASK));

        public static uint TB_SET_DTZ(uint _res, int _dtz) => (uint)(((_res) & ~TB_RESULT_DTZ_MASK) | (((_dtz) << TB_RESULT_DTZ_SHIFT) & TB_RESULT_DTZ_MASK));

        public static int TB_MOVE_FROM(TbMove move) => (((move) >> 6) & 0x3F);

        public static int TB_MOVE_TO(TbMove move) => ((move) & 0x3F);

        public static bool TB_MOVE_PROMOTES(TbMove move) => (((move) >> 12) & 0x7) != 0;


        #endregion



        #region Structs

        public struct TbRootMove
        {
            public TbMove move;
            public TbMove[] pv;
            public uint pvSize;
            public int tbScore, tbRank;

            public TbRootMove()
            {
                pv = new TbMove[TB_MAX_PLY];
            }

            public TbRootMove(TbMove move, TbMove[] pv, uint pvSize, int tbScore, int tbRank)
            {
                this.move = move;
                this.pv = pv;
                this.pvSize = pvSize;
                this.tbScore = tbScore;
                this.tbRank = tbRank;
            }
        };

        public struct TbRootMoves
        {
            public uint size;
            public TbRootMove[] moves;

            public TbRootMoves()
            {
                moves = new TbRootMove[TB_MAX_MOVES];
            }
        };

        #endregion








        public static readonly string[] tbSuffix = { ".rtbw", ".rtbm", ".rtbz" };
        public static readonly uint[] tbMagic = { 0x5d23e871, 0x88ac504b, 0xa50c66d7 };

        public struct PairsData
        {
            public byte* indexTable;
            public ushort* sizeTable;
            public byte* data;
            public ushort* offset;
            public byte* symLen;
            public byte* symPat;
            public byte blockSize;
            public byte idxBits;
            public byte minLen;
            public fixed byte constValue[2];
            public fixed ulong _base[1];

            public PairsData()
            {
                //_base = new ulong[1];
            }
        };

        public struct EncInfo
        {
            public PairsData* precomp;
            public fixed size_t factor[TB_PIECES];
            public fixed byte pieces[TB_PIECES];
            public fixed byte norm[TB_PIECES];

            public EncInfo()
            {

            }
        };

        public struct BaseEntry
        {
            public ulong key;
            public byte*[] data;
            //public MemoryMappedFile[] mapping;


            //  TODO: This is supposed to be atomic
            public fixed bool ready[3];
            public byte num;
            public bool symmetric, hasPawns, hasDtm, hasDtz;

            public bool kk_enc;
            public fixed byte pawns[2];

            public bool dtmLossOnly;

            public BaseEntry()
            {
                data = new byte*[3];
                //mapping = new MemoryMappedFile[3];
                //ready = new bool[3];

                //pawns = new byte[2];
            }
        };

        public struct PieceEntry
        {
            public BaseEntry be;
            public EncInfo[] ei; // 2 + 2 + 1
            public ushort* dtmMap;
            public ushort[][] dtmMapIdx;
            public void* dtzMap;
            public ushort[] dtzMapIdx;
            public byte dtzFlags;

            public PieceEntry()
            {
                ei = new EncInfo[5];
                dtmMapIdx = [[0, 0], [0, 0]];
                dtzMapIdx = new ushort[4];
            }
        };

        public struct PawnEntry
        {
            public BaseEntry be;
            public EncInfo[] ei; // 4 * 2 + 6 * 2 + 4
            public ushort* dtmMap;
            public ushort[][][] dtmMapIdx;
            public void* dtzMap;
            public ushort[][] dtzMapIdx;
            public byte[] dtzFlags;
            public bool dtmSwitched;

            public PawnEntry()
            {
                ei = new EncInfo[24];

                dtmMapIdx = new ushort[6][][];
                for (int i = 0; i < 6; i++)
                {
                    dtmMapIdx[i] = new ushort[2][] { new ushort[2], new ushort[2] };
                }

                dtzMapIdx = new ushort[4][];
                for (int i = 0; i < 4; i++)
                {
                    dtzMapIdx[i] = new ushort[4];
                }

                dtzFlags = new byte[4];
            }
        };

        public struct TbHashEntry
        {
            public ulong key;
            public BaseEntry* ptr;

            public TbHashEntry()
            {

            }
        };


        public static int tbNumPiece, tbNumPawn;
        public static int numWdl, numDtm, numDtz;

        public static PieceEntry* pieceEntry;
        public static PawnEntry* pawnEntry;
        public static TbHashEntry[] tbHash = new TbHashEntry[1 << TB_HASHBITS];



        public static int ColorOfPiece(int piece)
        {
            //  return (Color)(!(piece >> 3));
            return ((piece >> 3) == 0) ? WHITE : BLACK;
        }

        public static int TypeOfPiece(int piece)
        {
            //return (PieceType)(piece & 7);
            return (piece & 7);
        }

        public static uint64_t pieces_by_type(Pos* pos, int c, int p)
        {
            uint64_t mask = (c == WHITE) ? pos->white : pos->black;
            switch (p)
            {
                case PAWN:
                    return pos->pawns & mask;
                case KNIGHT:
                    return pos->knights & mask;
                case BISHOP:
                    return pos->bishops & mask;
                case ROOK:
                    return pos->rooks & mask;
                case QUEEN:
                    return pos->queens & mask;
                case KING:
                    return pos->kings & mask;
                default:
                    assert(0);
                    return 0;
            }
        }

        public static readonly string piece_to_char = " PNBRQK  pnbrqk";

        public static int char_to_piece_type(char c)
        {
            for (int i = PAWN; i <= KING; i++)
            {
                if (c == piece_to_char[i])
                {
                    return i;
                }
            }

            return 0;
        }

        public static char pchr(int i) => piece_to_char[QUEEN - (i)];

        public static int rank(int s) => GetIndexRank(s);

        public static int file(int s) => GetIndexFile(s);

        public static ulong board(int s) => SquareBB[s];

        public static int square(int r, int f) => CoordToIndex(r, f);

        public static ulong king_attacks(int s) => PrecomputedData.NeighborsMask[s];

        public static ulong knight_attacks(int s) => PrecomputedData.KnightMasks[s];

        public static ulong bishop_attacks(int s, ulong occ) => MagicBitboards.GetBishopMoves(occ, s);

        public static ulong rook_attacks(int s, ulong occ) => MagicBitboards.GetRookMoves(occ, s);

        public static ulong queen_attacks(int s, ulong occ) => (MagicBitboards.GetBishopMoves(occ, s) | MagicBitboards.GetRookMoves(occ, s));

        //  Use Not(c), TBProbe has White and Black the opposite way that Lizard does
        public static ulong pawn_attacks(int s, int c) => (PrecomputedData.PawnAttackMasks[Not(c)][s]);



        public static uint64_t calc_key(Pos* pos, bool mirror)
        {
            uint64_t white = pos->white, black = pos->black;
            if (mirror)
            {
                uint64_t tmp = white;
                white = black;
                black = tmp;
            }
            return popcount(white & pos->queens) * PRIME_WHITE_QUEEN +
                   popcount(white & pos->rooks) * PRIME_WHITE_ROOK +
                   popcount(white & pos->bishops) * PRIME_WHITE_BISHOP +
                   popcount(white & pos->knights) * PRIME_WHITE_KNIGHT +
                   popcount(white & pos->pawns) * PRIME_WHITE_PAWN +
                   popcount(black & pos->queens) * PRIME_BLACK_QUEEN +
                   popcount(black & pos->rooks) * PRIME_BLACK_ROOK +
                   popcount(black & pos->bishops) * PRIME_BLACK_BISHOP +
                   popcount(black & pos->knights) * PRIME_BLACK_KNIGHT +
                   popcount(black & pos->pawns) * PRIME_BLACK_PAWN;
        }

        // Produce a 64-bit material key corresponding to the material combination
        // defined by pcs[16], where pcs[1], ..., pcs[6] are the number of white
        // pawns, ..., kings and pcs[9], ..., pcs[14] are the number of black
        // pawns, ..., kings.
        public static uint64_t calc_key_from_pcs(int* pcs, int mirror)
        {
            //mirror = (mirror ? 8 : 0);
            mirror = ((mirror != 0) ? 8 : 0);

            return
                (ulong)pcs[WHITE_QUEEN ^ mirror] * PRIME_WHITE_QUEEN +
           (ulong)pcs[WHITE_ROOK ^ mirror] * PRIME_WHITE_ROOK +
           (ulong)pcs[WHITE_BISHOP ^ mirror] * PRIME_WHITE_BISHOP +
           (ulong)pcs[WHITE_KNIGHT ^ mirror] * PRIME_WHITE_KNIGHT +
           (ulong)pcs[WHITE_PAWN ^ mirror] * PRIME_WHITE_PAWN +
           (ulong)pcs[BLACK_QUEEN ^ mirror] * PRIME_BLACK_QUEEN +
           (ulong)pcs[BLACK_ROOK ^ mirror] * PRIME_BLACK_ROOK +
           (ulong)pcs[BLACK_BISHOP ^ mirror] * PRIME_BLACK_BISHOP +
           (ulong)pcs[BLACK_KNIGHT ^ mirror] * PRIME_BLACK_KNIGHT +
           (ulong)pcs[BLACK_PAWN ^ mirror] * PRIME_BLACK_PAWN;
        }

        // Produce a 64-bit material key corresponding to the material combination
        // piece[0], ..., piece[num - 1], where each value corresponds to a piece
        // (1-6 for white pawn-king, 9-14 for black pawn-king).
        public static uint64_t calc_key_from_pieces(uint8_t* piece, int num)
        {
            ulong key = 0;

            for (int i = 0; i < num; i++)
            {
                assert(piece[i] < 16);
                key += calc_key_from_pieces_keys[piece[i]];
            }
            return key;
        }

        private static readonly ulong[] calc_key_from_pieces_keys = [
            0,
            PRIME_WHITE_PAWN, PRIME_WHITE_KNIGHT, PRIME_WHITE_BISHOP, PRIME_WHITE_ROOK, PRIME_WHITE_QUEEN,
            0, 0,
            PRIME_BLACK_PAWN, PRIME_BLACK_KNIGHT, PRIME_BLACK_BISHOP, PRIME_BLACK_ROOK, PRIME_BLACK_QUEEN,
            0, 0,
            0
        ];


        public static TbMove make_move(int promote, int from, int to) => (ushort)(((((promote) & 0x7) << 12) | (((from) & 0x3F) << 6) | ((to) & 0x3F)));
        public static int move_from(TbMove move) => (((move) >> 6) & 0x3F);
        public static int move_to(TbMove move) => ((move) & 0x3F);
        public static int move_promotes(TbMove move) => (((move) >> 12) & 0x7);

        public static int type_of_piece_moved(Pos* pos, TbMove move)
        {
            for (int i = PAWN; i <= KING; i++)
            {
                if ((pieces_by_type(pos, ((pos->turn ? 1 : 0) == WHITE) ? WHITE : BLACK, i) & board(move_from(move))) != 0)
                {
                    return i;
                }
            }
            assert(0);
            return 0;
        }

        public static readonly int MAX_MOVES = TB_MAX_MOVES;
        public static readonly int MOVE_STALEMATE = 0xFFFF;
        public static readonly int MOVE_CHECKMATE = 0xFFFE;






        public static void assert(bool cond)
        {
            if (!cond)
            {
                Log("ASSERTION FAILED! " + new System.Diagnostics.StackTrace(true).ToString());
            }
        }

        public static void assert(int i)
        {
            assert(i != 0);
        }
    }
}
