
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
using static Lizard.Logic.Tablebase.TBProbe;

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


namespace Lizard.Logic.Tablebase
{
    public static unsafe class TBChess
    {

        public static TbMove* add_move(TbMove* moves, bool promotes, int from, int to)
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
        public static TbMove* gen_captures(Pos* pos, TbMove* moves)
        {
            uint64_t occ = pos->white | pos->black;
            uint64_t us = (pos->turn ? pos->white : pos->black),
                     them = (pos->turn ? pos->black : pos->white);
            uint64_t b, att;
            {
                int from = lsb(pos->kings & us);
                assert(from < 64);
                for (att = king_attacks(from) & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->queens; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = queen_attacks(from, occ) & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->rooks; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = rook_attacks(from, occ) & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->bishops; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = bishop_attacks(from, occ) & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->knights; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = knight_attacks(from) & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->pawns; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                att = pawn_attacks(from, (pos->turn ? 1 : 0));
                if (pos->ep != 0 && ((att & board(pos->ep)) != 0))
                {
                    int to = pos->ep;
                    moves = add_move(moves, false, from, to);
                }
                for (att = att & them; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, (rank(to) == 7 || rank(to) == 0), from, to);
                }
            }
            return moves;
        }

        /*
         * Generate all moves.
         */
        public static TbMove* gen_moves(Pos* pos, TbMove* moves)
        {
            uint64_t occ = pos->white | pos->black;
            uint64_t us = (pos->turn ? pos->white : pos->black),
                     them = (pos->turn ? pos->black : pos->white);
            uint64_t b, att;

            {
                int from = lsb(pos->kings & us);
                for (att = king_attacks(from) & ~us; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->queens; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = queen_attacks(from, occ) & ~us; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->rooks; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = rook_attacks(from, occ) & ~us; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->bishops; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = bishop_attacks(from, occ) & ~us; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->knights; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                for (att = knight_attacks(from) & ~us; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, false, from, to);
                }
            }
            for (b = us & pos->pawns; b != 0; b = poplsb(b))
            {
                int from = lsb(b);
                int next = from + (pos->turn ? 8 : -8);
                att = pawn_attacks(from, (pos->turn ? 1 : 0));
                if (pos->ep != 0 && ((att & board(pos->ep)) != 0))
                {
                    int to = pos->ep;
                    moves = add_move(moves, false, from, to);
                }
                att &= them;
                if ((board(next) & occ) == 0)
                {
                    att |= board(next);
                    int next2 = from + (pos->turn ? 16 : -16);
                    if ((pos->turn ? rank(from) == 1 : rank(from) == 6) &&
                            ((board(next2) & occ) == 0))
                        att |= board(next2);
                }
                for (; att != 0; att = poplsb(att))
                {
                    int to = lsb(att);
                    moves = add_move(moves, (rank(to) == 7 || rank(to) == 0), from,
                        to);
                }
            }
            return moves;
        }


        public static bool is_en_passant(Pos* pos, TbMove move) => is_en_passant(pos, (uint64_t)move_from(move), (uint64_t)move_to(move));
        public static bool is_en_passant(Pos* pos, uint64_t from, uint64_t to)
        {
            uint64_t us = (pos->turn ? pos->white : pos->black);
            //uint64_t us = (pos.ToMove == Color.White ? pos.bb.Colors[Color.White] : pos.bb.Colors[Color.Black]);

            if (pos->ep == 0)
                //if (pos.State->EPSquare == EPNone)
                return false;

            if (to != pos->ep)
                //if ((int)to != pos.State->EPSquare)
                return false;

            if ((board((int)from) & us & pos->pawns) == 0)
                return false;

            return true;
        }

        public static bool is_capture(Pos* pos, TbMove move)
        {
            uint16_t to = (uint16_t)move_to(move);
            uint64_t them = (pos->turn ? pos->black : pos->white);
            //uint64_t them = (pos.ToMove == WHITE ? pos.bb.Colors[BLACK] : pos.bb.Colors[WHITE]);
            return (them & board(to)) != 0 || is_en_passant(pos, move);
        }

        public static bool is_legal(Pos* pos)
        {
            uint64_t occ = pos->white | pos->black;
            uint64_t us = (pos->turn ? pos->black : pos->white),
                     them = (pos->turn ? pos->white : pos->black);
            uint64_t king = pos->kings & us;
            if (king == 0)
                return false;
            unsigned sq = (uint)lsb(king);
            if ((king_attacks((int)sq) & (pos->kings & them)) != 0)
                return false;
            uint64_t ratt = rook_attacks((int)sq, occ);
            uint64_t batt = bishop_attacks((int)sq, occ);
            if ((ratt & (pos->rooks & them)) != 0)
                return false;
            if ((batt & (pos->bishops & them)) != 0)
                return false;
            if (((ratt | batt) & (pos->queens & them)) != 0)
                return false;
            if ((knight_attacks((int)sq) & (pos->knights & them)) != 0)
                return false;
            if ((pawn_attacks((int)sq, (!pos->turn ? 1 : 0)) & (pos->pawns & them)) != 0)
                return false;
            return true;
        }

        public static bool is_check(Pos* pos)
        {
            uint64_t occ = pos->white | pos->black;
            uint64_t us = (pos->turn ? pos->white : pos->black),
                     them = (pos->turn ? pos->black : pos->white);
            uint64_t king = pos->kings & us;
            assert(king != 0);
            unsigned sq = (uint)lsb(king);
            uint64_t ratt = rook_attacks((int)sq, occ);
            uint64_t batt = bishop_attacks((int)sq, occ);
            if ((ratt & (pos->rooks & them)) != 0)
                return true;
            if ((batt & (pos->bishops & them)) != 0)
                return true;
            if (((ratt | batt) & (pos->queens & them)) != 0)
                return true;
            if ((knight_attacks((int)sq) & (pos->knights & them)) != 0)
                return true;
            if ((pawn_attacks((int)sq, (pos->turn ? 1 : 0)) & (pos->pawns & them)) != 0)
                return true;
            return false;
        }

        static bool is_valid(Pos* pos)
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
            if ((pos->pawns & BOARD_FILE_EDGE) != 0)
                return false;
            if ((pos->white | pos->black) !=
                (pos->kings | pos->queens | pos->rooks | pos->bishops | pos->knights |
                 pos->pawns))
                return false;
            return is_legal(pos);
        }

        public static uint64_t do_bb_move(uint64_t b, int32_t from, int32_t to) =>
            (((b) & (~board(to)) & (~board(from))) |
            ((((b) >> (from)) & 0x1) << (to)));

        public static bool do_move(Pos* pos, Pos* pos0, TbMove move)
        {
            int from = move_from(move);
            int to = move_to(move);
            int promotes = move_promotes(move);
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
                    (pawn_attacks(from + 8, WHITE) & pos0->pawns & pos0->black) != 0)
                    pos->ep = (byte)(from + 8);
                else if (rank(from) == 6 && rank(to) == 4 &&
                    (pawn_attacks(from - 8, BLACK) & pos0->pawns & pos0->white) != 0)
                    pos->ep = (byte)(from - 8);
                else if (to == pos0->ep)
                {
                    int ep_to = ((pos0->turn ? to - 8 : to + 8));
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

        public static bool legal_move(Pos* pos, TbMove move)
        {
            Pos pos1;
            return do_move(&pos1, pos, move);
        }

        /*
         * Test if the king is in checkmate.
         */
        public static bool is_mate(Pos* pos)
        {
            if (!is_check(pos))
                return false;
            TbMove* moves0 = stackalloc TbMove[MAX_MOVES];
            TbMove* moves = moves0;
            TbMove* end = gen_moves(pos, moves);
            for (; moves < end; moves++)
            {
                Pos pos1;
                if (do_move(&pos1, pos, *moves))
                    return false;
            }
            return true;
        }

        /*
         * Generate all legal moves.
         */
        public static TbMove* gen_legal(Pos* pos, TbMove* moves)
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























        #region Defines

        #endregion
    }
}
