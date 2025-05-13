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



using static Lizard.Logic.Tablebase.TBDefs;

namespace Lizard.Logic.Tablebase;

public static unsafe class TBChess
{
    public static int ColorOfPiece(int piece) => (Not(piece >> 3));
    public static int TypeOfPiece(int piece) => (piece & 7);

    public static uint64_t pieces_by_type(FathomPos* pos, int c, int p)
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
                Debug.Assert(false);
                return 0;
        }
    }

    // map upper-case characters to piece types
    public static int char_to_piece_type(char c)
    {
        for (int i = PAWN; i <= KING; i++)
            if (c == piece_to_char[i])
                return i;

        return 0;
    }

    public static int rank(int s) => (s >> 3);
    public static int file(int s) => (s & 0x07);
    public static uint64_t board(int s) => (1UL << s);
    public static int square(int r, int f) => (8 * r + f);

    public static int rank(uint s) => ((int)s >> 3);
    public static int file(uint s) => ((int)s & 0x07);
    public static uint64_t board(uint s) => (1UL << (int)s);

    public static uint64_t pawn_attacks(int s, int c) => PawnAttackMasks[Not(c)][s];
    public static uint64_t knight_attacks(int s) => KnightMasks[s];
    public static uint64_t bishop_attacks(int s, ulong occ) => GetBishopMoves(occ, s);
    public static uint64_t rook_attacks(int s, ulong occ) => GetRookMoves(occ, s);
    public static uint64_t queen_attacks(int s, ulong occ) => GetBishopMoves(occ, s) | GetRookMoves(occ, s);
    public static uint64_t king_attacks(int s) => NeighborsMask[s];

    public static uint64_t pawn_attacks(uint s, bool c) => PawnAttackMasks[Not(AsInt(c))][s];
    public static uint64_t pawn_attacks(uint s, int c) => PawnAttackMasks[Not(c)][s];
    public static uint64_t knight_attacks(uint s) => KnightMasks[s];
    public static uint64_t bishop_attacks(uint s, ulong occ) => bishop_attacks((int)s, occ);
    public static uint64_t rook_attacks(uint s, ulong occ) => rook_attacks((int)s, occ);
    public static uint64_t queen_attacks(uint s, ulong occ) => queen_attacks((int)s, occ);
    public static uint64_t king_attacks(uint s) => NeighborsMask[s];

    /*
     * Given a position, produce a 64-bit material signature key.
     */
    public static uint64_t calc_key(FathomPos* pos, bool mirror)
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
        mirror = (AsBool(mirror) ? 8 : 0);
        return (ulong)pcs[WHITE_QUEEN ^ mirror] * PRIME_WHITE_QUEEN +
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
    public static uint64_t calc_key_from_pieces(Span<uint8_t> piece, int num)
    {
        uint64_t key = 0;
        uint64_t[] keys = {0,PRIME_WHITE_PAWN,PRIME_WHITE_KNIGHT,
                                      PRIME_WHITE_BISHOP,PRIME_WHITE_ROOK,
                                      PRIME_WHITE_QUEEN,0,0,PRIME_BLACK_PAWN,
                                      PRIME_BLACK_KNIGHT,PRIME_BLACK_BISHOP,
                                      PRIME_BLACK_ROOK,PRIME_BLACK_QUEEN,0};
        for (int i = 0; i < num; i++)
        {
            Debug.Assert(piece[i] < 16);
            key += keys[piece[i]];
        }
        return key;
    }

    public static int type_of_piece_moved(FathomPos* pos, TbMove move)
    {
        for (int i = PAWN; i <= KING; i++)
        {
            if ((pieces_by_type(pos, AsInt(AsInt(pos->turn) == WHITE), (int)i) & board(move_from(move))) != 0)
            {
                return i;
            }
        }
        Debug.Assert(false);
        return 0;
    }


    public static TbMove* add_move(TbMove* moves, bool promotes, unsigned from, unsigned to)
    {
        if (!promotes)
            *moves++ = make_move(TB_PROMOTES_NONE, from, to);
        else
        {
            *moves++ = make_move(TB_PROMOTES_QUEEN, from, to);
            *moves++ = make_move(TB_PROMOTES_KNIGHT, from, to);
            *moves++ = make_move(TB_PROMOTES_ROOK, from, to);
            *moves++ = make_move(TB_PROMOTES_BISHOP, from, to);
        }
        return moves;
    }

    /*
     * Generate all captures, including all underpomotions
     */
    public static TbMove* gen_captures(FathomPos* pos, TbMove* moves)
    {
        uint64_t occ = pos->white | pos->black;
        uint64_t us = (pos->turn ? pos->white : pos->black),
                 them = (pos->turn ? pos->black : pos->white);
        uint64_t b, att;
        {
            unsigned from = (uint)lsb(pos->kings & us);
            Debug.Assert(from < 64);
            for (att = king_attacks(from) & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->queens; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = queen_attacks(from, occ) & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->rooks; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = rook_attacks(from, occ) & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->bishops; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = bishop_attacks(from, occ) & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->knights; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = knight_attacks(from) & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->pawns; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            att = pawn_attacks(from, pos->turn);
            if (pos->ep != 0 && ((att & board(pos->ep)) != 0))
            {
                unsigned to = pos->ep;
                moves = add_move(moves, false, from, to);
            }
            for (att = att & them; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, (rank(to) == 7 || rank(to) == 0), from,
                    to);
            }
        }
        return moves;
    }

    /*
     * Generate all moves.
     */
    public static TbMove* gen_moves(FathomPos* pos, TbMove* moves)
    {
        uint64_t occ = pos->white | pos->black;
        uint64_t us = (pos->turn ? pos->white : pos->black),
                 them = (pos->turn ? pos->black : pos->white);
        uint64_t b, att;

        {
            unsigned from = (uint)lsb(pos->kings & us);
            for (att = king_attacks(from) & ~us; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->queens; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = queen_attacks(from, occ) & ~us; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->rooks; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = rook_attacks(from, occ) & ~us; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->bishops; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = bishop_attacks(from, occ) & ~us; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->knights; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            for (att = knight_attacks(from) & ~us; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, false, from, to);
            }
        }
        for (b = us & pos->pawns; AsBool(b); b = poplsb(b))
        {
            unsigned from = (uint)lsb(b);
            unsigned next = (uint)(from + (pos->turn ? 8 : -8));
            att = pawn_attacks(from, pos->turn);
            if (pos->ep != 0 && ((att & board(pos->ep)) != 0))
            {
                unsigned to = pos->ep;
                moves = add_move(moves, false, from, to);
            }
            att &= them;
            if ((board(next) & occ) == 0)
            {
                att |= board(next);
                unsigned next2 = (uint)(from + (pos->turn ? 16 : -16));
                if ((pos->turn ? rank(from) == 1 : rank(from) == 6) &&
                        ((board(next2) & occ) == 0))
                    att |= board(next2);
            }
            for (; AsBool(att); att = poplsb(att))
            {
                unsigned to = (uint)lsb(att);
                moves = add_move(moves, (rank(to) == 7 || rank(to) == 0), from,
                    to);
            }
        }
        return moves;
    }

    /*
     * Test if the given move is an en passant capture.
     */
    public static bool is_en_passant(FathomPos* pos, TbMove move)
    {
        uint16_t from = move_from(move);
        uint16_t to = move_to(move);
        uint64_t us = (pos->turn ? pos->white : pos->black);
        if (pos->ep == 0)
            return false;
        if (to != pos->ep)
            return false;
        if ((board(from) & us & pos->pawns) == 0)
            return false;
        return true;
    }


    /*
     * Test if the given move is a capture.
     */
    public static bool is_capture(FathomPos* pos, TbMove move)
    {
        uint16_t to = move_to(move);
        uint64_t them = (pos->turn ? pos->black : pos->white);
        return (them & board(to)) != 0 || is_en_passant(pos, move);
    }


    /*
     * Test if the given position is legal.
     * (Pawns on backrank? Can the king be captured?)
     */
    public static bool is_legal(FathomPos* pos)
    {
        uint64_t occ = pos->white | pos->black;
        uint64_t us = (pos->turn ? pos->black : pos->white),
                 them = (pos->turn ? pos->white : pos->black);
        uint64_t king = pos->kings & us;
        if (!AsBool(king))
            return false;
        unsigned sq = (uint)lsb(king);
        if (AsBool(king_attacks(sq) & (pos->kings & them)))
            return false;
        uint64_t ratt = rook_attacks(sq, occ);
        uint64_t batt = bishop_attacks(sq, occ);
        if (AsBool(ratt & (pos->rooks & them)))
            return false;
        if (AsBool(batt & (pos->bishops & them)))
            return false;
        if (AsBool((ratt | batt) & (pos->queens & them)))
            return false;
        if (AsBool(knight_attacks(sq) & (pos->knights & them)))
            return false;
        if (AsBool(pawn_attacks(sq, !pos->turn) & (pos->pawns & them)))
            return false;
        return true;
    }

    /*
     * Test if the king is in check.
     */
    public static bool is_check(FathomPos* pos)
    {
        uint64_t occ = pos->white | pos->black;
        uint64_t us = (pos->turn ? pos->white : pos->black),
                 them = (pos->turn ? pos->black : pos->white);
        uint64_t king = pos->kings & us;
        Debug.Assert(king != 0);
        unsigned sq = (uint)lsb(king);
        uint64_t ratt = rook_attacks(sq, occ);
        uint64_t batt = bishop_attacks(sq, occ);
        if (AsBool(ratt & (pos->rooks & them)))
            return true;
        if (AsBool(batt & (pos->bishops & them)))
            return true;
        if (AsBool((ratt | batt) & (pos->queens & them)))
            return true;
        if (AsBool(knight_attacks(sq) & (pos->knights & them)))
            return true;
        if (AsBool(pawn_attacks(sq, pos->turn) & (pos->pawns & them)))
            return true;
        return false;
    }

    /*
     * Test if the position is valid.
     */
    public static bool is_valid(FathomPos* pos)
    {
        if (popcount(pos->kings) != 2)
            return false;
        if (popcount(pos->kings & pos->white) != 1)
            return false;
        if (popcount(pos->kings & pos->black) != 1)
            return false;
        if ((pos->white & pos->black) != 0)
            return false;
        if ((pos->kings & pos->queens) != 0)
            return false;
        if ((pos->kings & pos->rooks) != 0)
            return false;
        if ((pos->kings & pos->bishops) != 0)
            return false;
        if ((pos->kings & pos->knights) != 0)
            return false;
        if ((pos->kings & pos->pawns) != 0)
            return false;
        if ((pos->queens & pos->rooks) != 0)
            return false;
        if ((pos->queens & pos->bishops) != 0)
            return false;
        if ((pos->queens & pos->knights) != 0)
            return false;
        if ((pos->queens & pos->pawns) != 0)
            return false;
        if ((pos->rooks & pos->bishops) != 0)
            return false;
        if ((pos->rooks & pos->knights) != 0)
            return false;
        if ((pos->rooks & pos->pawns) != 0)
            return false;
        if ((pos->bishops & pos->knights) != 0)
            return false;
        if ((pos->bishops & pos->pawns) != 0)
            return false;
        if ((pos->knights & pos->pawns) != 0)
            return false;
        if (AsBool(pos->pawns & BOARD_FILE_EDGE))
            return false;
        if ((pos->white | pos->black) !=
            (pos->kings | pos->queens | pos->rooks | pos->bishops | pos->knights |
             pos->pawns))
            return false;
        return is_legal(pos);
    }

    public static ulong do_bb_move(ulong b, uint from, uint to) => (((b) & (~board(to)) & (~board(from))) | ((((b) >> ((int)from)) & 0x1) << ((int)to)));

    public static bool do_move(FathomPos* pos, FathomPos* pos0, TbMove move)
    {
        unsigned from = move_from(move);
        unsigned to = move_to(move);
        unsigned promotes = move_promotes(move);
        pos->turn = !pos0->turn;
        pos->white = do_bb_move(pos0->white, from, to);
        pos->black = do_bb_move(pos0->black, from, to);
        pos->kings = do_bb_move(pos0->kings, from, to);
        pos->queens = do_bb_move(pos0->queens, from, to);
        pos->rooks = do_bb_move(pos0->rooks, from, to);
        pos->bishops = do_bb_move(pos0->bishops, from, to);
        pos->knights = do_bb_move(pos0->knights, from, to);
        pos->pawns = do_bb_move(pos0->pawns, from, to);
        pos->ep = 0;
        if (promotes != TB_PROMOTES_NONE)
        {
            pos->pawns &= ~board(to);       // Promotion
            switch (promotes)
            {
                case TB_PROMOTES_QUEEN:
                    pos->queens |= board(to); break;
                case TB_PROMOTES_ROOK:
                    pos->rooks |= board(to); break;
                case TB_PROMOTES_BISHOP:
                    pos->bishops |= board(to); break;
                case TB_PROMOTES_KNIGHT:
                    pos->knights |= board(to); break;
            }
            pos->rule50 = 0;
        }
        else if ((board(from) & pos0->pawns) != 0)
        {
            pos->rule50 = 0;                // Pawn move
            if (rank(from) == 1 && rank(to) == 3 &&
                (pawn_attacks(from + 8, true) & pos0->pawns & pos0->black) != 0)
                pos->ep = (byte)(from + 8);
            else if (rank(from) == 6 && rank(to) == 4 &&
                (pawn_attacks(from - 8, false) & pos0->pawns & pos0->white) != 0)
                pos->ep = (byte)(from - 8);
            else if (to == pos0->ep)
            {
                unsigned ep_to = (pos0->turn ? to - 8 : to + 8);
                uint64_t ep_mask = ~board(ep_to);
                pos->white &= ep_mask;
                pos->black &= ep_mask;
                pos->pawns &= ep_mask;
            }
        }
        else if ((board(to) & (pos0->white | pos0->black)) != 0)
            pos->rule50 = 0;                // Capture
        else
            pos->rule50 = (byte)(pos0->rule50 + 1); // Normal move
        if (!is_legal(pos))
            return false;
        return true;
    }

    public static bool legal_move(FathomPos* pos, TbMove move)
    {
        FathomPos pos1;
        return do_move(&pos1, pos, move);
    }

    /*
     * Test if the king is in checkmate.
     */
    public static bool is_mate(FathomPos* pos)
    {
        if (!is_check(pos))
            return false;
        TbMove* moves0 = stackalloc TbMove[MAX_MOVES];
        TbMove* moves = moves0;
        TbMove* end = gen_moves(pos, moves);
        for (; moves < end; moves++)
        {
            FathomPos pos1;
            if (do_move(&pos1, pos, *moves))
                return false;
        }
        return true;
    }

    /*
     * Generate all legal moves.
     */
    public static TbMove* gen_legal(FathomPos* pos, TbMove* moves)
    {
        TbMove* pl_moves = stackalloc TbMove[TB_MAX_MOVES];
        TbMove* end = gen_moves(pos, pl_moves);
        TbMove* results = moves;
        for (TbMove* m = pl_moves; m < end; m++)
        {
            if (legal_move(pos, *m))
            {
                *results++ = *m;
            }
        }
        return results;
    }

}
