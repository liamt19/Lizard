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

using uint8_t = byte;
using uint16_t = ushort;
using uint32_t = uint;
using uint64_t = ulong;

using int8_t = sbyte;
using int16_t = short;
using int32_t = int;
using int64_t = long;

using size_t = ulong;
using unsigned = uint;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Buffers.Binary;
using static Lizard.Logic.Tablebase.TBProbe;

namespace Lizard.Logic.Tablebase;

public static unsafe class TBDefs
{
    public const int BLACK = 0;
    public const int WHITE = 1;

    public const int PAWN = 1;
    public const int KNIGHT = 2;
    public const int BISHOP = 3;
    public const int ROOK = 4;
    public const int QUEEN = 5;
    public const int KING = 6;

    public const int W_PAWN   = 0 + PAWN;
    public const int W_KNIGHT = 0 + KNIGHT;
    public const int W_BISHOP = 0 + BISHOP;
    public const int W_ROOK   = 0 + ROOK;
    public const int W_QUEEN  = 0 + QUEEN;
    public const int W_KING   = 0 + KING;

    public const int B_PAWN = 8 + PAWN;
    public const int B_KNIGHT = 8 + KNIGHT;
    public const int B_BISHOP = 8 + BISHOP;
    public const int B_ROOK = 8 + ROOK;
    public const int B_QUEEN = 8 + QUEEN;
    public const int B_KING = 8 + KING;

    public const int TB_PAWN = 1;
    public const int TB_KNIGHT = 2;
    public const int TB_BISHOP = 3;
    public const int TB_ROOK = 4;
    public const int TB_QUEEN = 5;
    public const int TB_KING = 6;

    public const int TB_WPAWN = TB_PAWN;
    public const int TB_BPAWN = (TB_PAWN | 8);

    public const int WHITE_KING   = (TB_WPAWN + 5);
    public const int WHITE_QUEEN  = (TB_WPAWN + 4);
    public const int WHITE_ROOK   = (TB_WPAWN + 3);
    public const int WHITE_BISHOP = (TB_WPAWN + 2);
    public const int WHITE_KNIGHT = (TB_WPAWN + 1);
    public const int WHITE_PAWN   = TB_WPAWN;
    public const int BLACK_KING   = (TB_BPAWN + 5);
    public const int BLACK_QUEEN  = (TB_BPAWN + 4);
    public const int BLACK_ROOK   = (TB_BPAWN + 3);
    public const int BLACK_BISHOP = (TB_BPAWN + 2);
    public const int BLACK_KNIGHT = (TB_BPAWN + 1);
    public const int BLACK_PAWN = TB_BPAWN;

    public const ulong PRIME_WHITE_QUEEN  = 11811845319353239651ul;
    public const ulong PRIME_WHITE_ROOK   = 10979190538029446137ul;
    public const ulong PRIME_WHITE_BISHOP = 12311744257139811149ul;
    public const ulong PRIME_WHITE_KNIGHT = 15202887380319082783ul;
    public const ulong PRIME_WHITE_PAWN   = 17008651141875982339ul;
    public const ulong PRIME_BLACK_QUEEN  = 15484752644942473553ul;
    public const ulong PRIME_BLACK_ROOK   = 18264461213049635989ul;
    public const ulong PRIME_BLACK_BISHOP = 15394650811035483107ul;
    public const ulong PRIME_BLACK_KNIGHT = 13469005675588064321ul;
    public const ulong PRIME_BLACK_PAWN   = 11695583624105689831ul;

    public const int WDL = 0;
    public const int DTM = 1;
    public const int DTZ = 2;

    public const int PIECE_ENC = 0;
    public const int FILE_ENC = 1;
    public const int RANK_ENC = 2;

    public const ulong BOARD_RANK_EDGE = 0x8181818181818181ul;
    public const ulong BOARD_FILE_EDGE = 0xFF000000000000FFul;
    public const ulong BOARD_EDGE      = (BOARD_RANK_EDGE | BOARD_FILE_EDGE);
    public const ulong BOARD_RANK_1    = 0x00000000000000FFul;
    public const ulong BOARD_FILE_A = 0x8080808080808080ul;

    public const ulong KEY_KvK = 0;

    public const int BEST_NONE     = 0xFFFF;
    public const int SCORE_ILLEGAL = 0x7FFF;

    public const int INT32_MAX = int32_t.MaxValue;

    public const int TB_VALUE_PAWN = 100;  /* value of pawn in endgame */
    public const int TB_VALUE_MATE = 32000;
    public const int TB_VALUE_INFINITE = 32767; /* value above all normal score values */
    public const int TB_VALUE_DRAW = 0;
    public const int TB_MAX_MATE_PLY = 255;

    public const int TB_PIECES = 7;
    public const int TB_HASHBITS = (TB_PIECES < 7 ? 11 : 12);
    public const int TB_MAX_PIECE = (TB_PIECES < 7 ? 254 : 650);
    public const int TB_MAX_PAWN = (TB_PIECES < 7 ? 256 : 861);
    public const int TB_MAX_SYMS = 4096;

    public const int MAX_MOVES = TB_MAX_MOVES;
    public const int TB_MAX_MOVES = (192 + 1);
    public const int TB_MAX_CAPTURES = 64;
    public const int TB_MAX_PLY = 256;
    public const int TB_CASTLING_K = 0x1;
    public const int TB_CASTLING_Q = 0x2;
    public const int TB_CASTLING_k = 0x4;
    public const int TB_CASTLING_q = 0x8;

    public const int TB_LOSS = 0;
    public const int TB_BLESSED_LOSS = 1;
    public const int TB_DRAW = 2;
    public const int TB_CURSED_WIN = 3;
    public const int TB_WIN = 4;

    public const int TB_PROMOTES_NONE = 0;
    public const int TB_PROMOTES_QUEEN = 1;
    public const int TB_PROMOTES_ROOK = 2;
    public const int TB_PROMOTES_BISHOP = 3;
    public const int TB_PROMOTES_KNIGHT = 4;

    public const uint TB_RESULT_WDL_MASK = 0x0000000F;
    public const uint TB_RESULT_TO_MASK = 0x000003F0;
    public const uint TB_RESULT_FROM_MASK = 0x0000FC00;
    public const uint TB_RESULT_PROMOTES_MASK = 0x00070000;
    public const uint TB_RESULT_EP_MASK = 0x00080000;
    public const uint TB_RESULT_DTZ_MASK = 0xFFF00000;
    public const int TB_RESULT_WDL_SHIFT = 0;
    public const int TB_RESULT_TO_SHIFT = 4;
    public const int TB_RESULT_FROM_SHIFT = 10;
    public const int TB_RESULT_PROMOTES_SHIFT = 16;
    public const int TB_RESULT_EP_SHIFT = 19;
    public const int TB_RESULT_DTZ_SHIFT = 20;

    public static uint TB_GET_WDL(uint _res) => (((_res) & TB_RESULT_WDL_MASK) >> TB_RESULT_WDL_SHIFT);
    public static uint TB_GET_TO(uint _res) => (((_res) & TB_RESULT_TO_MASK) >> TB_RESULT_TO_SHIFT);
    public static uint TB_GET_FROM(uint _res) => (((_res) & TB_RESULT_FROM_MASK) >> TB_RESULT_FROM_SHIFT);
    public static uint TB_GET_PROMOTES(uint _res) => (((_res) & TB_RESULT_PROMOTES_MASK) >> TB_RESULT_PROMOTES_SHIFT);
    public static uint TB_GET_EP(uint _res) => (((_res) & TB_RESULT_EP_MASK) >> TB_RESULT_EP_SHIFT);
    public static uint TB_GET_DTZ(uint _res) => (((_res) & TB_RESULT_DTZ_MASK) >> TB_RESULT_DTZ_SHIFT);

    public static uint TB_SET_WDL(uint _res, int _wdl) => (((_res) & ~TB_RESULT_WDL_MASK) | ((((uint)_wdl) << TB_RESULT_WDL_SHIFT) & TB_RESULT_WDL_MASK));
    public static uint TB_SET_TO(uint _res, int _to) => (((_res) & ~TB_RESULT_TO_MASK) | ((((uint)_to) << TB_RESULT_TO_SHIFT) & TB_RESULT_TO_MASK));
    public static uint TB_SET_FROM(uint _res, int _from) => (((_res) & ~TB_RESULT_FROM_MASK) | ((((uint)_from) << TB_RESULT_FROM_SHIFT) & TB_RESULT_FROM_MASK));
    public static uint TB_SET_PROMOTES(uint _res, int _promotes) => (((_res) & ~TB_RESULT_PROMOTES_MASK) | ((((uint)_promotes) << TB_RESULT_PROMOTES_SHIFT) & TB_RESULT_PROMOTES_MASK));
    public static uint TB_SET_EP(uint _res, int _ep) => (((_res) & ~TB_RESULT_EP_MASK) | ((((uint)_ep) << TB_RESULT_EP_SHIFT) & TB_RESULT_EP_MASK));
    public static uint TB_SET_DTZ(uint _res, int _dtz) => (((_res) & ~TB_RESULT_DTZ_MASK) | ((((uint)_dtz) << TB_RESULT_DTZ_SHIFT) & TB_RESULT_DTZ_MASK));


    public static uint TB_SET_TO(uint _res, uint _to) => (((_res) & ~TB_RESULT_TO_MASK) | (((_to) << TB_RESULT_TO_SHIFT) & TB_RESULT_TO_MASK));
    public static uint TB_SET_FROM(uint _res, uint _from) => (((_res) & ~TB_RESULT_FROM_MASK) | (((_from) << TB_RESULT_FROM_SHIFT) & TB_RESULT_FROM_MASK));
    public static uint TB_SET_PROMOTES(uint _res, uint _promotes) => (((_res) & ~TB_RESULT_PROMOTES_MASK) | (((_promotes) << TB_RESULT_PROMOTES_SHIFT) & TB_RESULT_PROMOTES_MASK));
    public static uint TB_SET_EP(uint _res, uint _ep) => (((_res) & ~TB_RESULT_EP_MASK) | (((_ep) << TB_RESULT_EP_SHIFT) & TB_RESULT_EP_MASK));


    public static readonly uint TB_RESULT_CHECKMATE = TB_SET_WDL(0, TB_WIN);
    public static readonly uint TB_RESULT_STALEMATE = TB_SET_WDL(0, TB_DRAW);
    public const uint TB_RESULT_FAILED = 0xFFFFFFFF;

    public static TbMove make_move(int promote, int from, int to) => new TbMove((ushort)((((promote) & 0x7) << 12) | (((from) & 0x3F) << 6) | ((to) & 0x3F)));
    public static TbMove make_move(int promote, uint from, uint to) => make_move(promote, (int)from, (int)to);
    public static uint16_t move_from(TbMove m) => (uint16_t)m.From;
    public static uint16_t move_to(TbMove m) => (uint16_t)m.To;
    public static uint16_t move_promotes(TbMove m) => (uint16_t)m.Promotes;

    public static readonly string piece_to_char = " PNBRQK  pnbrqk";
    public static char pchr(int i) => piece_to_char[QUEEN - (i)];
    public static void Swap(ref int a, ref int b) { (b, a) = (a, b); }
    public static void Swap(ref uint8_t a, ref uint8_t b) { (b, a) = (a, b); }

    public static void LOCK_INIT(ref Lock x) => x = new Lock();
    public static void LOCK_DESTROY(ref Lock x) => x = null;
    public static void LOCK(Lock x) => x.Enter();
    public static void UNLOCK(Lock x) => x.Exit();

    public static uint32_t from_le_u32(uint32_t x) => x;
    public static uint16_t from_le_u16(uint16_t x) => x;
    public static uint64_t from_be_u64(uint64_t input) => BinaryPrimitives.ReverseEndianness(input);
    public static uint32_t from_be_u32(uint32_t input) => BinaryPrimitives.ReverseEndianness(input);

    public static uint32_t read_le_u32(void* p) => from_le_u32(*(uint32_t*)p);
    public static uint16_t read_le_u16(void* p) => from_le_u16(*(uint16_t*)p);

    public static Lock tbMutex;

    public static size_t file_size(FileStream fd) => (size_t)fd.Length;

    public static bool AsBool(int32_t v) => v != 0;
    public static bool AsBool(uint32_t v) => v != 0;
    public static bool AsBool(uint64_t v) => v != 0;
    public static bool AsBool(void* v) => v != null;
    public static bool AsBool(BaseEntry be) => be != null;

    public static int AsInt(bool b) => (b ? 1 : 0);
}
