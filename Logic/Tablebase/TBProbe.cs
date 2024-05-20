
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
using static Lizard.Logic.Tablebase.TBProbeCore;
using static Lizard.Logic.Tablebase.TBConfig;
using static Lizard.Logic.Tablebase.TBChess;

using TbMove = ushort;
using size_t = ulong;

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

using System.IO.MemoryMappedFiles;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Data;
using System.IO;
using System.Drawing;


namespace Lizard.Logic.Tablebase
{
    public static unsafe class TBProbe
    {
        public static int TB_MaxCardinalityDTM = 0;
        public static int TB_MaxCardinality = 0;
        /*
         * The tablebase can be probed for any position where #pieces <= TB_LARGEST.
         */
        public static uint TB_LARGEST = 7;

        private static string SyzygyPath = "<empty>";
        public static int numPaths = 0;
        public const char SEP_CHAR = ';';

        public static void SetSyzygyPath(string path)
        {
            SyzygyPath = path;
        }




        public static void init_tb(string str)
        {
            if (!test_tb(str, tbSuffix[WDL]))
            {
                return;
            }
            else
            {
                //Log($"init_tb({str}) was found!");
            }


            int* pcs = stackalloc int[16];

            int color = 0;
            foreach (char s in str)
                if (s == 'v')
                    color = 8;
                else
                {
                    int piece_type = char_to_piece_type(s);
                    if (piece_type != 0)
                    {
                        assert((piece_type | color) < 16);
                        pcs[piece_type | color]++;
                    }
                }

            ulong key = calc_key_from_pcs(&pcs[0], 0);
            ulong key2 = calc_key_from_pcs(&pcs[0], 1);


            bool hasPawns = (pcs[W_PAWN] != 0 || pcs[B_PAWN] != 0);

            BaseEntry be = hasPawns ?  pawnEntry[tbNumPawn++]
                                    : pieceEntry[tbNumPiece++];

            be.hasPawns = hasPawns;
            be.key = key;
            be.symmetric = key == key2;
            be.num = 0;
            for (int i = 0; i < 16; i++)
                be.num += (byte)pcs[i];

            numWdl++;
            numDtm += (be.hasDtm = test_tb(str, tbSuffix[DTM])) ? 1 : 0;
            numDtz += (be.hasDtz = test_tb(str, tbSuffix[DTZ])) ? 1 : 0;

            if (be.num > TB_MaxCardinality)
            {
                TB_MaxCardinality = be.num;
            }
            if (be.hasDtm)
            {
                if (be.num > TB_MaxCardinalityDTM)
                {
                    TB_MaxCardinalityDTM = be.num;
                }
            }

            for (int type = 0; type < 3; type++)
            {
                //be.ready[type] = false;
            }

            if (!be.hasPawns)
            {
                int j = 0;
                for (int i = 0; i < 16; i++)
                    if (pcs[i] == 1) j++;
                be.kk_enc = j == 2;
            }
            else
            {
                be.pawns[0] = (byte)pcs[W_PAWN];
                be.pawns[1] = (byte)pcs[B_PAWN];
                //  if (pcs[B_PAWN] && (!pcs[W_PAWN] || pcs[W_PAWN] > pcs[B_PAWN]))
                if (pcs[B_PAWN] != 0 && ((pcs[W_PAWN] == 0) || pcs[W_PAWN] > pcs[B_PAWN]))
                {
                    //  Swap(be.pawns[0], be.pawns[1]);
                    (be.pawns[0], be.pawns[1]) = (be.pawns[1], be.pawns[0]);
                }
            }

            add_to_hash(be, key);
            if (key != key2)
            {
                add_to_hash(be, key2);
            }

        }


        /*
         * Free any resources allocated by tb_init
         */
        public static void tb_free()
        {

        }




        public static uint tb_probe_wdl(Position pos)
        {
            return tb_probe_wdl(
                pos.bb.Colors[White],
                pos.bb.Colors[Black],
                pos.bb.Pieces[King],
                pos.bb.Pieces[Queen],
                pos.bb.Pieces[Rook],
                pos.bb.Pieces[Bishop],
                pos.bb.Pieces[Knight],
                pos.bb.Pieces[Pawn],
                (uint)pos.State->EPSquare,
                (pos.ToMove == White ? true : false),
                (uint)pos.State->HalfmoveClock
                );
        }

        /*
         * Probe the Win-Draw-Loss (WDL) table.
         *
         * PARAMETERS:
         * - white, black, kings, queens, rooks, bishops, knights, pawns:
         *   The current position (bitboards).
         * - rule50:
         *   The 50-move half-move clock.
         * - castling:
         *   Castling rights.  Set to zero if no castling is possible.
         * - ep:
         *   The en passant square (if exists).  Set to zero if there is no en passant
         *   square.
         * - turn:
         *   true=white, false=black
         *
         * RETURN:
         * - One of {TB_LOSS, TB_BLESSED_LOSS, TB_DRAW, TB_CURSED_WIN, TB_WIN}.
         *   Otherwise returns TB_RESULT_FAILED if the probe failed.
         *
         * NOTES:
         * - Engines should use this function during search.
         * - This function is thread safe assuming TB_NO_THREADS is disabled.
         */
        public static uint tb_probe_wdl(
            ulong white,
            ulong black,
            ulong kings,
            ulong queens,
            ulong rooks,
            ulong bishops,
            ulong knights,
            ulong pawns,
            uint ep,
            bool turn,
            uint rule50 = 0)
        {
            Pos pos = new Pos(white, black, kings, queens, rooks, bishops, knights, pawns, (byte)rule50, (byte)ep, turn);

            int success;
            int v = probe_wdl(&pos, &success);
            if (success == 0)
            {
                Log("probe_wdl failed");
                return TB_RESULT_FAILED;
            }
            return (unsigned)(v + 2);
        }

        public static int dtz_to_wdl(uint cnt50, int dtz)
        {
            int wdl = 0;
            if (dtz > 0)
                wdl = (dtz + cnt50 <= 100 ? 2 : 1);
            else if (dtz < 0)
                wdl = (-dtz + cnt50 <= 100 ? -2 : -1);
            return (wdl + 2);
        }


        public static uint tb_probe_root(Position pos, uint* results)
        {
            var root = tb_probe_root(
                pos.bb.Colors[White],
                pos.bb.Colors[Black],
                pos.bb.Pieces[King],
                pos.bb.Pieces[Queen],
                pos.bb.Pieces[Rook],
                pos.bb.Pieces[Bishop],
                pos.bb.Pieces[Knight],
                pos.bb.Pieces[Pawn],
                (uint)(pos.State->HalfmoveClock),
                (uint)(pos.State->CastleStatus == CastlingStatus.None ? 0 : 1),
                (uint)pos.State->EPSquare,
                (pos.ToMove == White ? true : false),
                results
                );


            TbMove tbMove = (TbMove)(*results);
            int tbFrom = move_from(tbMove);
            int tbTo = move_to(tbMove);

            

            ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenLegal(legal);

            for (int i = 0; i < size; i++)
            {
                int r = (int)results[i];

                var from = TB_GET_FROM(r);
                var to = TB_GET_TO(r);
                var wdl = TB_GET_WDL(r);
                var dtz = TB_GET_DTZ(results[i]);

                Log($"Results[{i}]" +
                    $"\t from:{IndexToString(from)}" +
                    $"\t to: {IndexToString(to)}" +
                    $"\t wdl: {GetWDLResult((uint)wdl)}" +
                    $"\t dtz: {dtz}");
            }

            for (int i = 0; i < size; i++)
            {
                Move m = legal[0].Move;
                if (m.From == tbFrom && m.To == tbTo)
                    Log($"tb_probe_root move is {legal[i].Move.ToString(pos)}");
            }

            return root;
        }

        /*
         * Probe the Distance-To-Zero (DTZ) table.
         *
         * PARAMETERS:
         * - white, black, kings, queens, rooks, bishops, knights, pawns:
         *   The current position (bitboards).
         * - rule50:
         *   The 50-move half-move clock.
         * - castling:
         *   Castling rights.  Set to zero if no castling is possible.
         * - ep:
         *   The en passant square (if exists).  Set to zero if there is no en passant
         *   square.
         * - turn:
         *   true=white, false=black
         * - results (OPTIONAL):
         *   Alternative results, one for each possible legal move.  The passed array
         *   must be TB_MAX_MOVES in size.
         *   If alternative results are not desired then set results=NULL.
         *
         * RETURN:
         * - A TB_RESULT value comprising:
         *   1) The WDL value (TB_GET_WDL)
         *   2) The suggested move (TB_GET_FROM, TB_GET_TO, TB_GET_PROMOTES, TB_GET_EP)
         *   3) The DTZ value (TB_GET_DTZ)
         *   The suggested move is guaranteed to preserved the WDL value.
         *
         *   Otherwise:
         *   1) TB_RESULT_STALEMATE is returned if the position is in stalemate.
         *   2) TB_RESULT_CHECKMATE is returned if the position is in checkmate.
         *   3) TB_RESULT_FAILED is returned if the probe failed.
         *
         *   If results!=NULL, then a TB_RESULT for each legal move will be generated
         *   and stored in the results array.  The results array will be terminated
         *   by TB_RESULT_FAILED.
         *
         * NOTES:
         * - Engines can use this function to probe at the root.  This function should
         *   not be used during search.
         * - DTZ tablebases can suggest unnatural moves, especially for losing
         *   positions.  Engines may prefer to traditional search combined with WDL
         *   move filtering using the alternative results array.
         * - This function is NOT thread safe.  For engines this function should only
         *   be called once at the root per search.
         */
        public static uint tb_probe_root(
            ulong white,
            ulong black,
            ulong kings,
            ulong queens,
            ulong rooks,
            ulong bishops,
            ulong knights,
            ulong pawns,
            uint rule50,
            uint castling,
            uint ep,
            bool turn,
            uint* results)
        {
            if (castling != 0)
                return TB_RESULT_FAILED;

            Pos pos = new Pos(white, black, kings, queens, rooks, bishops, knights, pawns, (byte)rule50, (byte)ep, turn);

            int dtz;
            TbMove move = probe_root(&pos, &dtz, results);

            Log($"In tb_probe_root, probe_root returned {move} = {IndexToString(move_from(move))}{IndexToString(move_to(move))}");

            if (move == 0)
                return TB_RESULT_FAILED;
            if (move == MOVE_CHECKMATE)
                return TB_RESULT_CHECKMATE;
            if (move == MOVE_STALEMATE)
                return TB_RESULT_STALEMATE;
            uint res = 0;
            res = TB_SET_WDL(res, dtz_to_wdl(rule50, dtz));
            res = TB_SET_DTZ(res, (dtz < 0 ? -dtz : dtz));
            res = TB_SET_FROM(res, move_from(move));
            res = TB_SET_TO(res, move_to(move));
            res = TB_SET_PROMOTES(res, move_promotes(move));
            res = TB_SET_EP(res, (is_en_passant(&pos, move) ? 1 : 0));
            return res;
        }


        /*
         * Use the DTZ tables to rank and score all root moves.
         * INPUT: as for tb_probe_root
         * OUTPUT: TbRootMoves structure is filled in. This contains
         * an array of TbRootMove structures.
         * Each structure instance contains a rank, a score, and a
         * predicted principal variation.
         * RETURN VALUE:
         *   non-zero if ok, 0 means not all probes were successful
         *
         */
        public static int tb_probe_root_dtz(
            ulong white,
            ulong black,
            ulong kings,
            ulong queens,
            ulong rooks,
            ulong bishops,
            ulong knights,
            ulong pawns,
            uint rule50,
            uint castling,
            uint ep,
            bool turn,
            bool hasRepeated,
            bool useRule50,
            TbRootMoves* results)
        {
            Pos pos = new Pos(white, black, kings, queens, rooks, bishops, knights, pawns, (byte)rule50, (byte)ep, turn);

            if (castling != 0) return 0;
            return root_probe_dtz(&pos, hasRepeated, useRule50, results);
        }

        /*
        // Use the WDL tables to rank and score all root moves.
        // This is a fallback for the case that some or all DTZ tables are missing.
         * INPUT: as for tb_probe_root
         * OUTPUT: TbRootMoves structure is filled in. This contains
         * an array of TbRootMove structures.
         * Each structure instance contains a rank, a score, and a
         * predicted principal variation.
         * RETURN VALUE:
         *   non-zero if ok, 0 means not all probes were successful
         *
         */
        public static int tb_probe_root_wdl(
            ulong white,
            ulong black,
            ulong kings,
            ulong queens,
            ulong rooks,
            ulong bishops,
            ulong knights,
            ulong pawns,
            uint rule50,
            uint castling,
            uint ep,
            bool turn,
            bool useRule50,
            TbRootMoves* results)
        {
            Pos pos = new Pos(white, black, kings, queens, rooks, bishops, knights, pawns, (byte)rule50, (byte)ep, turn);

            if (castling != 0) return 0;
            return root_probe_wdl(&pos, useRule50, results);
        }

        public static string prt_str(Pos* pos, bool flip)
        {
            char[] str = new char[16];

            int color = flip ? BLACK : WHITE;
            int charIdx = 0;

            for (int pt = KING; pt >= PAWN; pt--)
                for (int i = (int)popcount(pieces_by_type(pos, color, pt)); i > 0; i--)
                    str[charIdx++] = piece_to_char[pt];
            str[charIdx++] = 'v';
            color ^= 1;
            for (int pt = KING; pt >= PAWN; pt--)
                for (int i = (int)popcount(pieces_by_type(pos, color, pt)); i > 0; i--)
                    str[charIdx++] = piece_to_char[pt];
            //str[charIdx++] = '\0';

            return new string(str, 0, charIdx);

        }

        public static bool test_tb(string file, string ext) => test_tb(file + ext);
        public static bool test_tb(string file)
        {
            var fs = OpenSingleFile(SyzygyPath + file);
            if (fs == null)
            {
                return false;
            }

            long size = fs.Length;
            if ((size & 63) != 16)
            {
                Log($"Incomplete tablebase file {file}\n");
                fs.Close();
                return false;
            }

            return true;
        }

        public static uint8_t* map_tb(string name, string suffix)
        {
            var fd = OpenSingleFile(SyzygyPath + name + suffix);
            //  TODO: this leaks

            MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(fd, name + suffix, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            var accessView = mapping.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            uint8_t* ptr = null;
            accessView.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

            return ptr;
        }


        public static void add_to_hash(BaseEntry be, ulong key)
        {
            //Log($"add_to_hash({be.key}, {key})");
            int idx;

            idx = (int)(key >> (64 - TB_HASHBITS));
            while (tbHash[idx].ptr != null)
                idx = (idx + 1) & ((1 << TB_HASHBITS) - 1);

            tbHash[idx].key = key;
            tbHash[idx].ptr = be;
        }


        public static int num_tables(BaseEntry be, int type)
        {
            return be.hasPawns ? type == DTM ? 6 : 4 : 1;
        }

        public static void free_tb_entry(BaseEntry be)
        {
            for (int type = 0; type < 3; type++)
            {

                //  if (atomic_load_explicit(&be->ready[type], memory_order_relaxed))
                if (be.ready[type])
                {
                    //unmap_file((void*)(be->data[type]), be->mapping[type]);
                    int num = num_tables(be, type);

                    Span<EncInfo> ei = be.first_ei(type);

                    for (int t = 0; t < num; t++)
                    {
                        free(ei[t].precomp);
                        if (type != DTZ)
                            free(ei[num + t].precomp);
                    }

                    //atomic_store_explicit(&be->ready[type], false, memory_order_relaxed);
                    be.ready[type] = false;
                }
            }
        }


        public static object tbMutex = new object();
        public static bool tb_init()
        {
            init_indices();

            int i, j, k, l, m;
            string str = string.Empty;
            TB_LARGEST = 0;

            // if path is an empty string or equals "<empty>", we are done.
            if (SyzygyPath.Length == 0 || SyzygyPath == "<empty>")
            {
                Log($"Skipping tb_init, SyzygyPath is {SyzygyPath}");
                return true;
            }

            LOCK_INIT(ref tbMutex);

            tbNumPiece = tbNumPawn = 0;
            TB_MaxCardinality = TB_MaxCardinalityDTM = 0;

            //  if (!pieceEntry)
            if (pieceEntry == null)
            {
                pieceEntry = (PieceEntry*) malloc(TB_MAX_PIECE * sizeof(PieceEntry));
                pawnEntry = (PawnEntry*) malloc(TB_MAX_PAWN * sizeof(PawnEntry));
                //  if (!pieceEntry || !pawnEntry)
                if (pieceEntry == null || pawnEntry == null)
                {
                    Log("Out of memory.\n");
                    exit(1);
                }

                for (int a = 0; a < TB_MAX_PIECE; a++)
                {
                    pieceEntry[a] = new PieceEntry();
                }

                for (int a = 0; a < TB_MAX_PAWN; a++)
                {
                    pawnEntry[a] = new PawnEntry();
                }
            }

            for (i = 0; i < (1 << TB_HASHBITS); i++)
            {
                tbHash[i].key = 0;
                tbHash[i].ptr = null;
            }


            for (i = 0; i < 5; i++)
            {
                //  snprintf(str, 16, "K%cvK", pchr(i));
                str = "K" + pchr(i) + "vK";
                init_tb(str);
            }

            for (i = 0; i < 5; i++)
                for (j = i; j < 5; j++)
                {
                    //  snprintf(str, 16, "K%cvK%c", pchr(i), pchr(j));
                    str = "K" + pchr(i) + "vK" + pchr(j);
                    init_tb(str);
                }

            for (i = 0; i < 5; i++)
                for (j = i; j < 5; j++)
                {
                    //  snprintf(str, 16, "K%c%cvK", pchr(i), pchr(j));
                    str = "K" + pchr(i) + pchr(j) + "vK";
                    init_tb(str);
                }

            for (i = 0; i < 5; i++)
                for (j = i; j < 5; j++)
                    for (k = 0; k < 5; k++)
                    {
                        //  snprintf(str, 16, "K%c%cvK%c", pchr(i), pchr(j), pchr(k));
                        str = "K" + pchr(i) + pchr(j) + "vK" + pchr(k);
                        init_tb(str);
                    }

            for (i = 0; i < 5; i++)
                for (j = i; j < 5; j++)
                    for (k = j; k < 5; k++)
                    {
                        //  snprintf(str, 16, "K%c%c%cvK", pchr(i), pchr(j), pchr(k));
                        str = "K" + pchr(i) + pchr(j) + pchr(k) + "vK";
                        init_tb(str);
                    }

            /* TBD - assumes UCI
            printf("info string Found %d WDL, %d DTM and %d DTZ tablebase files.\n",
                numWdl, numDtm, numDtz);
            fflush(stdout);
            */
            // Set TB_LARGEST, for backward compatibility with pre-7-man Fathom
            TB_LARGEST = (unsigned)TB_MaxCardinality;
            if ((unsigned)TB_MaxCardinalityDTM > TB_LARGEST)
            {
                TB_LARGEST = (uint)TB_MaxCardinalityDTM;
            }
            return true;
        }

        public static string GetWDLResult(uint result)
        {
            return result switch
            {
                TB_LOSS => "Loss",
                TB_BLESSED_LOSS => "Blessed loss (50-move draw)",
                TB_DRAW => "Draw",
                TB_CURSED_WIN => "Cursed win (50-move draw)",
                TB_WIN => "Win",
                _ => "Unknown!"
            };
        }

        public static string GetDTZResult(uint result)
        {
            return result switch
            {
                TB_RESULT_CHECKMATE => "Checkmate",
                TB_RESULT_STALEMATE => "Stalemate",
                TB_RESULT_FAILED => "Failed!",
                _ => $"Unknown! ({result})"
            };
        }
    }
}
