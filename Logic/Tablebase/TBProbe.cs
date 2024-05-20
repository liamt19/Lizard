
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



Additionally, some parts of this file were fixed using Ceres' port, which you can find here:
https://github.com/dje-dev/Ceres/blob/main/src/Ceres.Chess/TBBackends/Fathom/FathomProbe.cs

The fixes were primarily in init_table and probe_table (and the functionality of BaseEntry/PawnEntry/PieceEntry accordingly),
which I was previously handling in a non-workable way.

 */

using static Lizard.Logic.Tablebase.Fathom;
using static Lizard.Logic.Tablebase.TBProbeHeader;
using static Lizard.Logic.Tablebase.TBChess;


using size_t = ulong;

using int8_t = sbyte;
using uint8_t = byte;
using int16_t = short;
using uint16_t = ushort;
using int32_t = int;
using uint32_t = uint;
using int64_t = long;
using uint64_t = ulong;

using uintptr_t = ulong;

using unsigned = uint;
using Value = int;


using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;

namespace Lizard.Logic.Tablebase
{
    public static unsafe class TBProbe
    {
        private static readonly int8_t[] OffDiag = [
      0,-1,-1,-1,-1,-1,-1,-1,
      1, 0,-1,-1,-1,-1,-1,-1,
      1, 1, 0,-1,-1,-1,-1,-1,
      1, 1, 1, 0,-1,-1,-1,-1,
      1, 1, 1, 1, 0,-1,-1,-1,
      1, 1, 1, 1, 1, 0,-1,-1,
      1, 1, 1, 1, 1, 1, 0,-1,
      1, 1, 1, 1, 1, 1, 1, 0
    ];
        private static readonly uint8_t[] Triangle = [
      6, 0, 1, 2, 2, 1, 0, 6,
      0, 7, 3, 4, 4, 3, 7, 0,
      1, 3, 8, 5, 5, 8, 3, 1,
      2, 4, 5, 9, 9, 5, 4, 2,
      2, 4, 5, 9, 9, 5, 4, 2,
      1, 3, 8, 5, 5, 8, 3, 1,
      0, 7, 3, 4, 4, 3, 7, 0,
      6, 0, 1, 2, 2, 1, 0, 6
    ];
        private static readonly uint8_t[] FlipDiag = [
       0,  8, 16, 24, 32, 40, 48, 56,
       1,  9, 17, 25, 33, 41, 49, 57,
       2, 10, 18, 26, 34, 42, 50, 58,
       3, 11, 19, 27, 35, 43, 51, 59,
       4, 12, 20, 28, 36, 44, 52, 60,
       5, 13, 21, 29, 37, 45, 53, 61,
       6, 14, 22, 30, 38, 46, 54, 62,
       7, 15, 23, 31, 39, 47, 55, 63
    ];
        private static readonly uint8_t[] Lower = [
      28,  0,  1,  2,  3,  4,  5,  6,
       0, 29,  7,  8,  9, 10, 11, 12,
       1,  7, 30, 13, 14, 15, 16, 17,
       2,  8, 13, 31, 18, 19, 20, 21,
       3,  9, 14, 18, 32, 22, 23, 24,
       4, 10, 15, 19, 22, 33, 25, 26,
       5, 11, 16, 20, 23, 25, 34, 27,
       6, 12, 17, 21, 24, 26, 27, 35
    ];
        private static readonly uint8_t[] Diag = [
       0,  0,  0,  0,  0,  0,  0,  8,
       0,  1,  0,  0,  0,  0,  9,  0,
       0,  0,  2,  0,  0, 10,  0,  0,
       0,  0,  0,  3, 11,  0,  0,  0,
       0,  0,  0, 12,  4,  0,  0,  0,
       0,  0, 13,  0,  0,  5,  0,  0,
       0, 14,  0,  0,  0,  0,  6,  0,
      15,  0,  0,  0,  0,  0,  0,  7
    ];
        private static readonly uint8_t[][] Flap = [
      [  0,  0,  0,  0,  0,  0,  0,  0,
         0,  6, 12, 18, 18, 12,  6,  0,
         1,  7, 13, 19, 19, 13,  7,  1,
         2,  8, 14, 20, 20, 14,  8,  2,
         3,  9, 15, 21, 21, 15,  9,  3,
         4, 10, 16, 22, 22, 16, 10,  4,
         5, 11, 17, 23, 23, 17, 11,  5,
         0,  0,  0,  0,  0,  0,  0,  0  ],
      [  0,  0,  0,  0,  0,  0,  0,  0,
         0,  1,  2,  3,  3,  2,  1,  0,
         4,  5,  6,  7,  7,  6,  5,  4,
         8,  9, 10, 11, 11, 10,  9,  8,
        12, 13, 14, 15, 15, 14, 13, 12,
        16, 17, 18, 19, 19, 18, 17, 16,
        20, 21, 22, 23, 23, 22, 21, 20,
         0,  0,  0,  0,  0,  0,  0,  0  ]
    ];
        private static readonly uint8_t[][] PawnTwist = [
      [  0,  0,  0,  0,  0,  0,  0,  0,
        47, 35, 23, 11, 10, 22, 34, 46,
        45, 33, 21,  9,  8, 20, 32, 44,
        43, 31, 19,  7,  6, 18, 30, 42,
        41, 29, 17,  5,  4, 16, 28, 40,
        39, 27, 15,  3,  2, 14, 26, 38,
        37, 25, 13,  1,  0, 12, 24, 36,
         0,  0,  0,  0,  0,  0,  0,  0 ],
        [  0,  0,  0,  0,  0,  0,  0,  0,
        47, 45, 43, 41, 40, 42, 44, 46,
        39, 37, 35, 33, 32, 34, 36, 38,
        31, 29, 27, 25, 24, 26, 28, 30,
        23, 21, 19, 17, 16, 18, 20, 22,
        15, 13, 11,  9,  8, 10, 12, 14,
         7,  5,  3,  1,  0,  2,  4,  6,
         0,  0,  0,  0,  0,  0,  0,  0 ]
    ];
        private static readonly int16_t[][] KKIdx = [
      [ -1, -1, -1,  0,  1,  2,  3,  4,
        -1, -1, -1,  5,  6,  7,  8,  9,
        10, 11, 12, 13, 14, 15, 16, 17,
        18, 19, 20, 21, 22, 23, 24, 25,
        26, 27, 28, 29, 30, 31, 32, 33,
        34, 35, 36, 37, 38, 39, 40, 41,
        42, 43, 44, 45, 46, 47, 48, 49,
        50, 51, 52, 53, 54, 55, 56, 57 ],
      [ 58, -1, -1, -1, 59, 60, 61, 62,
        63, -1, -1, -1, 64, 65, 66, 67,
        68, 69, 70, 71, 72, 73, 74, 75,
        76, 77, 78, 79, 80, 81, 82, 83,
        84, 85, 86, 87, 88, 89, 90, 91,
        92, 93, 94, 95, 96, 97, 98, 99,
       100,101,102,103,104,105,106,107,
       108,109,110,111,112,113,114,115],
      [116,117, -1, -1, -1,118,119,120,
       121,122, -1, -1, -1,123,124,125,
       126,127,128,129,130,131,132,133,
       134,135,136,137,138,139,140,141,
       142,143,144,145,146,147,148,149,
       150,151,152,153,154,155,156,157,
       158,159,160,161,162,163,164,165,
       166,167,168,169,170,171,172,173 ],
      [174, -1, -1, -1,175,176,177,178,
       179, -1, -1, -1,180,181,182,183,
       184, -1, -1, -1,185,186,187,188,
       189,190,191,192,193,194,195,196,
       197,198,199,200,201,202,203,204,
       205,206,207,208,209,210,211,212,
       213,214,215,216,217,218,219,220,
       221,222,223,224,225,226,227,228 ],
      [229,230, -1, -1, -1,231,232,233,
       234,235, -1, -1, -1,236,237,238,
       239,240, -1, -1, -1,241,242,243,
       244,245,246,247,248,249,250,251,
       252,253,254,255,256,257,258,259,
       260,261,262,263,264,265,266,267,
       268,269,270,271,272,273,274,275,
       276,277,278,279,280,281,282,283 ],
      [284,285,286,287,288,289,290,291,
       292,293, -1, -1, -1,294,295,296,
       297,298, -1, -1, -1,299,300,301,
       302,303, -1, -1, -1,304,305,306,
       307,308,309,310,311,312,313,314,
       315,316,317,318,319,320,321,322,
       323,324,325,326,327,328,329,330,
       331,332,333,334,335,336,337,338 ],
      [ -1, -1,339,340,341,342,343,344,
        -1, -1,345,346,347,348,349,350,
        -1, -1,441,351,352,353,354,355,
        -1, -1, -1,442,356,357,358,359,
        -1, -1, -1, -1,443,360,361,362,
        -1, -1, -1, -1, -1,444,363,364,
        -1, -1, -1, -1, -1, -1,445,365,
        -1, -1, -1, -1, -1, -1, -1,446 ],
      [ -1, -1, -1,366,367,368,369,370,
        -1, -1, -1,371,372,373,374,375,
        -1, -1, -1,376,377,378,379,380,
        -1, -1, -1,447,381,382,383,384,
        -1, -1, -1, -1,448,385,386,387,
        -1, -1, -1, -1, -1,449,388,389,
        -1, -1, -1, -1, -1, -1,450,390,
        -1, -1, -1, -1, -1, -1, -1,451 ],
      [452,391,392,393,394,395,396,397,
        -1, -1, -1, -1,398,399,400,401,
        -1, -1, -1, -1,402,403,404,405,
        -1, -1, -1, -1,406,407,408,409,
        -1, -1, -1, -1,453,410,411,412,
        -1, -1, -1, -1, -1,454,413,414,
        -1, -1, -1, -1, -1, -1,455,415,
        -1, -1, -1, -1, -1, -1, -1,456 ],
      [457,416,417,418,419,420,421,422,
        -1,458,423,424,425,426,427,428,
        -1, -1, -1, -1, -1,429,430,431,
        -1, -1, -1, -1, -1,432,433,434,
        -1, -1, -1, -1, -1,435,436,437,
        -1, -1, -1, -1, -1,459,438,439,
        -1, -1, -1, -1, -1, -1,460,440,
        -1, -1, -1, -1, -1, -1, -1,461 ]
    ];
        private static readonly uint8_t[] FileToFile = [0, 1, 2, 3, 3, 2, 1, 0];
        private static readonly int[] WdlToMap = [1, 3, 0, 2, 0];
        private static readonly uint8_t[] PAFlags = [8, 0, 0, 0, 4];
        private static readonly int[] WdlToDtz = [-1, -101, 0, 101, 1];
        private static readonly int[] wdl_to_dtz = [-1, -101, 0, 101, 1];

        public static object tbMutex = new object();

        public static int TB_MaxCardinalityDTM = 0;
        public static int TB_MaxCardinality = 0;
        /*
         * The tablebase can be probed for any position where #pieces <= TB_LARGEST.
         */
        public static uint TB_LARGEST = 7;

        private static string SyzygyPath = "<empty>";
        public static int numPaths = 0;
        public const char SEP_CHAR = ';';

        private static size_t[][] Binomial;
        private static size_t[][][] PawnIdx;
        private static size_t[][] PawnFactorFile;
        private static size_t[][] PawnFactorRank;

        public static void init_indices()
        {
            int i, j, k;

            Binomial = new size_t[7][];
            for (int a = 0; a < 7; a++)
            {
                Binomial[a] = new size_t[64];
            }

            PawnIdx = new size_t[2][][];
            for (int a = 0; a < 2; a++)
            {
                PawnIdx[a] = new size_t[6][];
                for (int b = 0; b < 6; b++)
                {
                    PawnIdx[a][b] = new size_t[24];
                }
            }

            PawnFactorFile = new size_t[6][];
            for (int a = 0; a < 6; a++)
            {
                PawnFactorFile[a] = new size_t[4];
            }

            PawnFactorRank = new size_t[6][];
            for (int a = 0; a < 6; a++)
            {
                PawnFactorRank[a] = new size_t[6];
            }


            // Binomial[k][n] = Bin(n, k)
            for (i = 0; i < 7; i++)
                for (j = 0; j < 64; j++)
                {
                    size_t f = 1;
                    size_t l = 1;
                    for (k = 0; k < i; k++)
                    {
                        f *= (size_t)(j - k);
                        l *= (size_t)(k + 1);
                    }
                    Binomial[i][j] = f / l;
                }

            for (i = 0; i < 6; i++)
            {
                size_t s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[0][i][j] = s;
                    s += Binomial[i][PawnTwist[0][(1 + (j % 6)) * 8 + (j / 6)]];
                    if ((j + 1) % 6 == 0)
                    {
                        PawnFactorFile[i][j / 6] = s;
                        s = 0;
                    }
                }
            }

            for (i = 0; i < 6; i++)
            {
                size_t s = 0;
                for (j = 0; j < 24; j++)
                {
                    PawnIdx[1][i][j] = s;
                    s += Binomial[i][PawnTwist[1][(1 + (j / 4)) * 8 + (j % 4)]];
                    if ((j + 1) % 4 == 0)
                    {
                        PawnFactorRank[i][j / 4] = s;
                        s = 0;
                    }
                }
            }
        }

        public static int leading_pawn(int* p, BaseEntry be, int enc)
        {
            for (int i = 1; i < be.pawns[0]; i++)
                if (Flap[enc - 1][p[0]] > Flap[enc - 1][p[i]])
                    (p[0], p[i]) = (p[i], p[0]);

            return enc == FILE_ENC ? FileToFile[p[0] & 7] : (p[0] - 8) >> 3;
        }

        public static size_t encode(int* p, EncInfo* ei, BaseEntry be, int enc)
        {
            int n = be.num;
            size_t idx;
            int k;

            if ((p[0] & 0x04) != 0)
                for (int i = 0; i < n; i++)
                    p[i] ^= 0x07;

            if (enc == PIECE_ENC)
            {
                if ((p[0] & 0x20) != 0)
                    for (int i = 0; i < n; i++)
                        p[i] ^= 0x38;

                for (int i = 0; i < n; i++)
                    if (OffDiag[p[i]] != 0)
                    {
                        if (OffDiag[p[i]] > 0 && i < (be.kk_enc ? 2 : 3))
                            for (int j = 0; j < n; j++)
                                p[j] = FlipDiag[p[j]];
                        break;
                    }

                if (be.kk_enc)
                {
                    idx = (ulong)KKIdx[Triangle[p[0]]][p[1]];
                    k = 2;
                }
                else
                {
                    int s1 = ((p[1] > p[0]) ? 1 : 0);
                    int s2 = (((p[2] > p[0]) ? 1 : 0) + ((p[2] > p[1]) ? 1 : 0)) != 0 ? 1 : 0;

                    if (OffDiag[p[0]] != 0)
                        idx = (ulong)(Triangle[p[0]] * 63 * 62 + (p[1] - s1) * 62 + (p[2] - s2));
                    else if (OffDiag[p[1]] != 0)
                        idx = (ulong)(6 * 63 * 62 + Diag[p[0]] * 28 * 62 + Lower[p[1]] * 62 + p[2] - s2);
                    else if (OffDiag[p[2]] != 0)
                        idx = (ulong)(6 * 63 * 62 + 4 * 28 * 62 + Diag[p[0]] * 7 * 28 + (Diag[p[1]] - s1) * 28 + Lower[p[2]]);
                    else
                        idx = (ulong)(6 * 63 * 62 + 4 * 28 * 62 + 4 * 7 * 28 + Diag[p[0]] * 7 * 6 + (Diag[p[1]] - s1) * 6 + (Diag[p[2]] - s2));
                    k = 3;
                }
                idx *= ei->factor[0];
            }
            else
            {
                for (int i = 1; i < be.pawns[0]; i++)
                    for (int j = i + 1; j < be.pawns[0]; j++)
                        if (PawnTwist[enc - 1][p[i]] < PawnTwist[enc - 1][p[j]])
                            (p[i], p[j]) = (p[j], p[i]);

                k = be.pawns[0];
                idx = PawnIdx[enc - 1][k - 1][Flap[enc - 1][p[0]]];
                for (int i = 1; i < k; i++)
                    idx += Binomial[k - i][PawnTwist[enc - 1][p[i]]];
                idx *= ei->factor[0];

                // Pawns of other color
                if (be.pawns[1] != 0)
                {
                    int t = k + be.pawns[1];
                    for (int i = k; i < t; i++)
                        for (int j = i + 1; j < t; j++)
                            if (p[i] > p[j])
                            {
                                (p[i], p[j]) = (p[j], p[i]);
                            }
                    size_t s = 0;
                    for (int i = k; i < t; i++)
                    {
                        int sq = p[i];
                        int skips = 0;
                        for (int j = 0; j < k; j++)
                            skips += ((sq > p[j]) ? 1 : 0);
                        s += Binomial[i - k + 1][sq - skips - 8];
                    }
                    idx += s * ei->factor[k];
                    k = t;
                }
            }

            for (; k < n;)
            {
                int t = k + ei->norm[k];
                for (int i = k; i < t; i++)
                    for (int j = i + 1; j < t; j++)
                        //  if (p[i] > p[j]) Swap(p[i], p[j]);
                        if (p[i] > p[j])
                        {
                            (p[i], p[j]) = (p[j], p[i]);
                        }
                size_t s = 0;
                for (int i = k; i < t; i++)
                {
                    int sq = p[i];
                    int skips = 0;
                    for (int j = 0; j < k; j++)
                        skips += ((sq > p[j]) ? 1 : 0);
                    s += Binomial[i - k + 1][sq - skips];
                }
                idx += s * ei->factor[k];
                k = t;
            }

            return idx;
        }

        private static size_t encode_piece(int* p, EncInfo* ei, BaseEntry be)
        {
            return encode(p, ei, be, PIECE_ENC);
        }

        private static size_t encode_pawn_f(int* p, EncInfo* ei, BaseEntry be)
        {
            return encode(p, ei, be, FILE_ENC);
        }

        private static size_t encode_pawn_r(int* p, EncInfo* ei, BaseEntry be)
        {
            return encode(p, ei, be, RANK_ENC);
        }

        // Count number of placements of k like pieces on n squares
        private static size_t subfactor(size_t k, size_t n)
        {
            size_t f = n;
            size_t l = 1;
            for (size_t i = 1; i < k; i++)
            {
                f *= n - i;
                l *= i + 1;
            }

            return f / l;
        }

        private static size_t init_enc_info(ref EncInfo ei, BaseEntry be, uint8_t* tb, int shift, int t, int enc)
        {
            bool morePawns = enc != PIECE_ENC && be.pawns[1] > 0;

            for (int i = 0; i < be.num; i++)
            {
                ei.pieces[i] = (byte)((tb[i + 1 + (morePawns ? 1 : 0)] >> shift) & 0x0f);
                ei.norm[i] = 0;
            }

            int order = (tb[0] >> shift) & 0x0f;
            int order2 = morePawns ? (tb[1] >> shift) & 0x0f : 0x0f;

            int k = ei.norm[0] = (byte)(enc != PIECE_ENC ? be.pawns[0]
                                 : be.kk_enc ? 2 : 3);

            if (morePawns)
            {
                ei.norm[k] = be.pawns[1];
                k += ei.norm[k];
            }

            for (int i = k; i < be.num; i += ei.norm[i])
                for (int j = i; j < be.num && ei.pieces[j] == ei.pieces[i]; j++)
                    ei.norm[i]++;

            int n = 64 - k;
            size_t f = 1;

            for (int i = 0; k < be.num || i == order || i == order2; i++)
            {
                if (i == order)
                {
                    ei.factor[0] = f;
                    //  f *= enc == FILE_ENC ? PawnFactorFile[ei->norm[0] - 1][t]
                    //  : enc == RANK_ENC ? PawnFactorRank[ei->norm[0] - 1][t]
                    //  : be->kk_enc ? 462 : 31332;

                    if (enc == FILE_ENC)
                    {
                        f *= PawnFactorFile[ei.norm[0] - 1][t];
                    }
                    else if (enc == RANK_ENC)
                    {
                        f *= PawnFactorRank[ei.norm[0] - 1][t];
                    }
                    else if (be.kk_enc)
                    {
                        f *= 462;
                    }
                    else
                    {
                        f *= 31332;
                    }
                }
                else if (i == order2)
                {
                    ei.factor[ei.norm[0]] = f;
                    f *= subfactor(ei.norm[ei.norm[0]], (ulong)(48 - ei.norm[0]));
                }
                else
                {
                    ei.factor[k] = f;
                    f *= subfactor(ei.norm[k], (ulong)n);
                    n -= ei.norm[k];
                    k += ei.norm[k];
                }
            }

            return f;
        }

        private static void calc_symLen(PairsData* d, uint32_t s, char* tmp)
        {
            uint8_t* w = d->symPat + 3 * s;
            uint32_t s2 = (uint)((w[2] << 4) | (w[1] >> 4));
            if (s2 == 0x0fff)
                d->symLen[s] = 0;
            else
            {
                uint32_t s1 = (uint)(((w[1] & 0xf) << 8) | w[0]);
                //  if (!tmp[s1]) calc_symLen(d, s1, tmp);
                if (tmp[s1] == 0) calc_symLen(d, s1, tmp);
                //  if (!tmp[s2]) calc_symLen(d, s2, tmp);
                if (tmp[s2] == 0) calc_symLen(d, s2, tmp);
                d->symLen[s] = (byte)(d->symLen[s1] + d->symLen[s2] + 1);
            }
            tmp[s] = (char)1;
        }

        private static PairsData* setup_pairs(uint8_t** ptr, size_t tb_size, size_t* size, uint8_t* flags, int type)
        {
            PairsData* d;
            uint8_t* data = *ptr;

            *flags = data[0];
            if ((data[0] & 0x80) != 0)
            {
                d = (PairsData*)malloc(sizeof(PairsData));
                d->idxBits = 0;
                d->constValue[0] = (byte)(type == WDL ? data[1] : 0);
                d->constValue[1] = 0;
                *ptr = data + 2;
                size[0] = size[1] = size[2] = 0;
                return d;
            }

            uint8_t blockSize = data[1];
            uint8_t idxBits = data[2];
            uint32_t realNumBlocks = read_le_u32(data + 4);
            uint32_t numBlocks = realNumBlocks + data[3];
            int maxLen = data[8];
            int minLen = data[9];
            int h = maxLen - minLen + 1;
            uint32_t numSyms = (uint32_t)read_le_u16(data + 10 + 2 * h);
            d = (PairsData*)malloc(sizeof(PairsData) + h * sizeof(uint64_t) + numSyms);
            d->blockSize = blockSize;
            d->idxBits = idxBits;
            d->offset = (uint16_t*)(&data[10]);
            d->symLen = (uint8_t*)d + sizeof(PairsData) + h * sizeof(uint64_t);
            d->symPat = &data[12 + 2 * h];
            d->minLen = (byte)minLen;
            *ptr = &data[12 + 2 * h + 3 * numSyms + (numSyms & 1)];

            size_t num_indices = (tb_size + (1UL << idxBits) - 1) >> idxBits;
            size[0] = 6UL * num_indices;
            size[1] = 2UL * numBlocks;
            size[2] = (size_t)realNumBlocks << blockSize;

            assert(numSyms < TB_MAX_SYMS);
            char[] tmpBuff = new char[TB_MAX_SYMS];
            fixed (char* tmp = &tmpBuff[0])
            {
                for (uint32_t s = 0; s < numSyms; s++)
                    //  if (!tmp[s])
                    if (tmp[s] == 0)
                        calc_symLen(d, s, tmp);
            }

            d->_base[h - 1] = 0;
            for (int i = h - 2; i >= 0; i--)
                d->_base[i] = (d->_base[i + 1] + read_le_u16((uint8_t*)(d->offset + i)) - read_le_u16((uint8_t*)(d->offset + i + 1))) / 2;
#if DECOMP64
  for (int i = 0; i < h; i++)
    d->_base[i] <<= 64 - (minLen + i);
#else
            for (int i = 0; i < h; i++)
                d->_base[i] <<= 32 - (minLen + i);
#endif
            d->offset -= d->minLen;

            return d;
        }

        private static bool init_table(BaseEntry be, string str, int type)
        {
            //  uint8_t* data = map_tb(str, tbSuffix[type], be->mapping[type]); 
            uint8_t* data = map_tb(str, tbSuffix[type]);
            
            if (data == null)
            {
                Log("Failed to map_tb");
                return false;
            }

            if (read_le_u32(data) != tbMagic[type])
            {
                Log("Corrupted table.\n");
                //unmap_file((void*)data, be->mapping[type]);
                return false;
            }

            be.data[type] = data;

            bool split = type != DTZ && ((data[4] & 0x01) != 0);
            if (type == DTM)
                be.dtmLossOnly = (data[4] & 0x04) != 0;

            data += 5;

            size_t[,] tb_size = new size_t[6, 2];

            int num = num_tables(be, type);

            Span<EncInfo> ei = be.first_ei(type);
            int enc = !be.hasPawns ? PIECE_ENC : type != DTM ? FILE_ENC : RANK_ENC;

            for (int t = 0; t < num; t++)
            {
                tb_size[t, 0] = init_enc_info(ref ei[t], be, data, 0, t, enc);
                if (split)
                {
                    tb_size[t, 1] = init_enc_info(ref ei[num + t], be, data, 4, t, enc);
                }
                data += be.num + 1 + BoolInt((be.hasPawns && IntBool(be.pawns[1])));
            }

            data += (uint)data & 1;

            ulong[,][] size = new ulong[6, 2][];
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    size[i, j] = new ulong[3]; // TODO: make stackalloc:
                }
            }

            for (int t = 0; t < num; t++)
            {
                byte flags = default;

                fixed (size_t* sizePtr = size[t, 0])
                    ei[t].precomp = setup_pairs(&data, tb_size[t, 0], sizePtr, &flags, type);

                if (type == DTZ)
                {
                    if (!be.hasPawns)
                    {
                        (be as PieceEntry).dtzFlags[0] = flags;
                    }
                    else
                    {
                        (be as PawnEntry).dtzFlags[t] = flags;
                    }
                }
                if (split)
                {
                    fixed (size_t* sizePtr = size[t, 1])
                        ei[num + t].precomp = setup_pairs(&data, tb_size[t, 1], sizePtr, &flags, type);
                }
                else if (type != DTZ)
                {
                    ei[num + t].precomp = default;
                }
            }


            if (type == DTM && !be.dtmLossOnly)
            {
                ushort* map = (ushort*)data;
                if (be is PieceEntry)
                {
                    (be as PieceEntry).dtmMap = map;
                }
                else
                {
                    (be as PawnEntry).dtmMap = map;
                }


                ref ushort[,,] refMapIdx = ref (be is PawnEntry) ? ref ((be as  PawnEntry).dtmMapIdx)
                                                                 : ref ((be as PieceEntry).dtmMapIdx);

                //  ushort(*mapIdx)[2][2] = be->hasPawns ? &PAWN(be)->dtmMapIdx[0] : &PIECE(be)->dtmMapIdx;
                for (int t = 0; t < num; t++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        refMapIdx[t, 0, i] = (ushort)(data + 1 - (byte*)map);
                        data += 2 + 2 * read_le_u16(data);
                    }
                    if (split)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            refMapIdx[t, 1, i] = (ushort)(data + 1 - (byte*)map);
                            data += 2 + 2 * read_le_u16(data);
                        }
                    }
                }
            }

            if (type == DTZ)
            {
                //  void* map = data;
                //  *(be->hasPawns ? &PAWN(be)->dtzMap : &PIECE(be)->dtzMap) = map;

                ushort* map1 = (ushort*)data;
                if (be is PieceEntry)
                {
                    (be as PieceEntry).dtzMap = map1;
                }
                else
                {
                    (be as PawnEntry).dtzMap = map1;
                }

                //      ushort(*mapIdx)[4] = be->hasPawns ? &PAWN(be)->dtzMapIdx[0]
                //                                        : &PIECE(be)->dtzMapIdx;
                ref ushort[,] refMapIdx = ref (be is PawnEntry) ? ref ((be as PawnEntry).dtzMapIdx)
                                                                : ref ((be as PieceEntry).dtzMapIdx);

                byte[] flags = be.hasPawns ? (be as PawnEntry).dtzFlags
                                           : (be as PieceEntry).dtzFlags;
                for (int t = 0; t < num; t++)
                {
                    if (IntBool(flags[t] & 2))
                    {
                        if (!IntBool((flags[t] & 16)))
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                refMapIdx[t, i] = (ushort)(data + 1 - (byte*)map1);
                                data += 1 + data[0];
                            }
                        }
                        else
                        {
                            data += ((IntPtr)data).ToInt64() & 0x01;
                            for (int i = 0; i < 4; i++)
                            {
                                refMapIdx[t, i] = (ushort)(data + 1 - (byte*)map1);
                                data += 2 + 2 * read_le_u16(data);
                            }
                        }
                    }
                }
                data += ((IntPtr)data).ToInt64() & 0x01;
            }

            for (int t = 0; t < num; t++)
            {
                ei[t].precomp->indexTable = data;
                data += size[t, 0][0];
                if (split)
                {
                    ei[num + t].precomp->indexTable = data;
                    data += size[t, 1][0];
                }
            }

            for (int t = 0; t < num; t++)
            {
                ei[t].precomp->sizeTable = (uint16_t*)data;
                data += size[t, 0][1];
                if (split)
                {
                    ei[num + t].precomp->sizeTable = (uint16_t*)data;
                    data += size[t, 1][1];
                }
            }

            for (int t = 0; t < num; t++)
            {
                //  data = (uint8_t*)(((uintptr_t)data + 0x3f) & ~0x3f);
                data = (uint8_t*)(((uintptr_t)data + 0x3f) & ~0x3fUL);
                ei[t].precomp->data = data;
                data += size[t, 0][2];
                if (split)
                {
                    //  data = (uint8_t*)(((uintptr_t)data + 0x3f) & ~0x3f);
                    data = (uint8_t*)(((uintptr_t)data + 0x3f) & ~0x3fUL);
                    ei[num + t].precomp->data = data;
                    data += size[t, 1][2];
                }
            }

            if (type == DTM && be.hasPawns)
            {
                fixed (uint8_t* pcs = ei[0].pieces)
                    (be as PawnEntry).dtmSwitched = calc_key_from_pieces(pcs, be.num) != be.key;
            }

            //Log("init_table returning true");

            return true;
        }

        private static uint8_t* decompress_pairs(PairsData* d, size_t idx)
        {
            //if (!d->idxBits)
            if (d->idxBits == 0)
            {
                return d->constValue;
            }
                

            uint32_t mainIdx = (uint32_t)(idx >> d->idxBits);
            int litIdx = (int)((idx & (((size_t)1 << d->idxBits) - 1)) - ((size_t)1 << (d->idxBits - 1)));
            uint32_t block;
            memcpy(&block, d->indexTable + 6 * mainIdx, sizeof(uint32_t));
            block = from_le_u32(block);

            uint16_t idxOffset = *(uint16_t*)(d->indexTable + 6 * mainIdx + 4);
            litIdx += from_le_u16(idxOffset);

            if (litIdx < 0)
                while (litIdx < 0)
                    litIdx += d->sizeTable[--block] + 1;
            else
                while (litIdx > d->sizeTable[block])
                    litIdx -= d->sizeTable[block++] + 1;

            uint32_t* ptr = (uint32_t*)(d->data + ((size_t)block << d->blockSize));

            int m = d->minLen;
            uint16_t* offset = d->offset;
            uint64_t* _base = d->_base - m;
            uint8_t* symLen = d->symLen;
            uint32_t sym, bitCnt;

#if DECOMP64
  uint64_t code = from_be_u64(*(uint64_t *)ptr);

  ptr += 2;
  bitCnt = 0; // number of "empty bits" in code
  for (;;) {
    int l = m;
    while (code < _base[l]) l++;
    sym = from_le_u16(offset[l]);
    sym += (uint32_t)((code - _base[l]) >> (64 - l));
    if (litIdx < (int)symLen[sym] + 1) break;
    litIdx -= (int)symLen[sym] + 1;
    code <<= l;
    bitCnt += l;
    if (bitCnt >= 32) {
      bitCnt -= 32;
      uint32_t tmp = from_be_u32(*ptr++);
      code |= (uint64_t)tmp << bitCnt;
    }
  }
#else
            uint32_t next = 0;
            uint32_t data = *ptr++;
            uint32_t code = from_be_u32(data);
            bitCnt = 0; // number of bits in next
            for (; ; )
            {
                int l = m;
                while (code < _base[l]) l++;
                sym = (uint)(offset[l] + ((code - _base[l]) >> (32 - l)));
                if (litIdx < (int)symLen[sym] + 1) break;
                litIdx -= (int)symLen[sym] + 1;
                code <<= l;
                if (bitCnt < l)
                {
                    if (bitCnt != 0)
                    {
                        code |= (next >> (32 - l));
                        l -= (int)bitCnt;
                    }
                    data = *ptr++;
                    next = from_be_u32(data);
                    bitCnt = 32;
                }
                code |= (next >> (32 - l));
                next <<= l;
                bitCnt -= (uint)l;
            }
#endif
            uint8_t* symPat = d->symPat;
            while (symLen[sym] != 0)
            {
                uint8_t* w = symPat + (3 * sym);
                int s1 = ((w[1] & 0xf) << 8) | w[0];
                if (litIdx < (int)symLen[s1] + 1)
                    sym = (uint)s1;
                else
                {
                    litIdx -= (int)symLen[s1] + 1;
                    sym = (uint)((w[2] << 4) | (w[1] >> 4));
                }
            }

            return &symPat[3 * sym];
        }

        private static int fill_squares(Pos *pos, uint8_t* pc, bool flip, int mirror, int* p, int i)
        {
            int color = ColorOfPiece(pc[i]);
            //  if (flip) color = (Color)(!(int)color);
            if (flip) color = (Not(color));
            uint64_t bb = pieces_by_type(pos, color, TypeOfPiece(pc[i]));
            unsigned sq;
            do
            {
                sq = (uint)lsb(bb);
                p[i++] = (int)(sq ^ mirror);
                bb = poplsb(bb);
            } while (bb != 0);
            return i;
        }

        public static int probe_table(Pos *pos, int s, int* success, int type)
        {
            // Obtain the position's material-signature key
            uint64_t key = calc_key(pos, false);

            // Test for KvK
            // Note: Cfish has key == 2UL for KvK but we have 0
            if (type == WDL && key == 0UL)
            {
                return 0;
            }

            int hashIdx = (int)(key >> (64 - TB_HASHBITS));
            while ((tbHash[hashIdx].key != 0) && tbHash[hashIdx].key != key)
                hashIdx = (hashIdx + 1) & ((1 << TB_HASHBITS) - 1);

            if (tbHash[hashIdx].ptr == null)
            {
                *success = 0;
                //  The TB file for this position doesn't exist...
                return 0;
            }

            BaseEntry be = tbHash[hashIdx].ptr;
            if ((type == DTM && !be.hasDtm) || (type == DTZ && !be.hasDtz))
            {
                *success = 0;
                return 0;
            }

            // Use double-checked locking to reduce locking overhead
            //  if (!atomic_load_explicit(&be.ready[type], memory_order_acquire)) {
            if (!be.ready[type])
            {
                LOCK(tbMutex);

                //  if (!atomic_load_explicit(&be.ready[type], memory_order_relaxed)) {
                if (!be.ready[type])
                {
                    string str = prt_str(pos, be.key != key);
                    if (!init_table(be, str, type))
                    {
                        tbHash[hashIdx].ptr = null; // mark as deleted
                        *success = 0;
                        UNLOCK(tbMutex);
                        Log($"probe_table couldn't init_table, returning 0");
                        return 0;
                    }
                    //atomic_store_explicit(&be.ready[type], true, memory_order_release);
                    be.ready[type] = true;
                }
                UNLOCK(tbMutex);
            }

            bool bside, flip;
            if (!be.symmetric)
            {
                flip = key != be.key;
                bside = ((pos->turn ? 1 : 0) == WHITE) == flip;
                if (type == DTM && be.hasPawns && (be as PawnEntry).dtmSwitched)
                {
                    flip = !flip;
                    bside = !bside;
                }
            }
            else
            {
                flip = (pos->turn ? 1 : 0) != WHITE;
                bside = false;
            }

            Span<EncInfo> ei = be.first_ei(type);
            ref EncInfo thisEI = ref ei[0];
            int* p = stackalloc int[TB_PIECES];
            ulong idx;
            int t = 0;
            byte flags = 0;

            if (!be.hasPawns)
            {
                if (type == DTZ)
                {
                    flags = (be as PieceEntry).dtzFlags[0];
                    if (IntBool(flags & 1) != bside && !be.symmetric)
                    {
                        *success = -1;
                        return 0;
                    }
                }

                thisEI = ref type != DTZ ? ref ei[BoolInt(bside)] : ref ei[0];
                for (int i = 0; i < be.num;)
                {
                    fixed (uint8_t* pcs = thisEI.pieces)
                        i = fill_squares(pos, pcs, flip, 0, p, i);
                }

                fixed (EncInfo* eiPtr = &thisEI)
                    idx = encode_piece(p, eiPtr, be);
            }
            else
            {
                int i;
                fixed (uint8_t* pcs = ei[0].pieces)
                    i = fill_squares(pos, pcs, flip, flip ? 0x38 : 0, p, 0);

                t = leading_pawn(p, be, type != DTM ? FILE_ENC : RANK_ENC);
                if (type == DTZ)
                {
                    flags = (be as PawnEntry).dtzFlags[t];
                    if (((flags & 1) == 0 ? false : true) != bside && !be.symmetric)
                    {
                        *success = -1;
                        return 0;
                    }
                }

                thisEI = ref type == WDL ? ref ei[t + 4 * BoolInt(bside)]
                                         : ref type == DTM ? ref ei[t + 6 * BoolInt(bside)] : ref ei[t];
                while (i < be.num)
                {
                    fixed (uint8_t* pcs = thisEI.pieces)
                        i = fill_squares(pos, pcs, flip, flip ? 0x38 : 0, p, i);
                }

                fixed (EncInfo* eiPtr = &thisEI)
                    idx = type != DTM ? encode_pawn_f(p, eiPtr, be) : encode_pawn_r(p, eiPtr, be);
            }

            byte* _w = decompress_pairs(thisEI.precomp, idx);
            Span<byte> w = new Span<byte>(_w, 2);

            if (type == WDL)
            {
                return (int)w[0] - 2;
            }

            int v = w[0] + ((w[1] & 0x0f) << 8);

            if (type == DTM)
            {
                if (!be.dtmLossOnly)
                {
                    v = (int)from_le_u16(be.hasPawns
                        ? (be as  PawnEntry).dtmMap[(be as  PawnEntry).dtmMapIdx[t, bside ? 1 : 0, s] + v]
                        : (be as PieceEntry).dtmMap[(be as PieceEntry).dtmMapIdx[0, bside ? 1 : 0, s] + v]);
                }
            }
            else
            {
                if ((flags & 2) != 0)
                {
                    int m = WdlToMap[s + 2];
                    if ((flags & 16) == 0)
                    {
                        v = be.hasPawns
                            ? ((byte*)(be as  PawnEntry).dtzMap)[(be as  PawnEntry).dtzMapIdx[t, m] + v]
                            : ((byte*)(be as PieceEntry).dtzMap)[(be as PieceEntry).dtzMapIdx[0, m] + v];
                    }
                    else
                    {
                        v = (int)from_le_u16(be.hasPawns
                            ? ((byte*)(be as  PawnEntry).dtzMap)[(be as  PawnEntry).dtzMapIdx[t, m] + v]
                            : ((byte*)(be as PieceEntry).dtzMap)[(be as PieceEntry).dtzMapIdx[0, m] + v]);
                    }
                }

                if ((flags & PAFlags[s + 2]) == 0 || (s & 1) != 0)
                {
                    v *= 2;
                }
            }

            return v;
        }

        private static int probe_wdl_table(Pos* pos, int* success)
        {
            return probe_table(pos, 0, success, WDL);
        }

        private static int probe_dtm_table(Pos* pos, int won, int* success)
        {
            return probe_table(pos, won, success, DTM);
        }

        private static int probe_dtz_table(Pos* pos, int wdl, int* success)
        {
            return probe_table(pos, wdl, success, DTZ);
        }

        private static int probe_ab(Pos* pos, int alpha, int beta, int* success)
        {
            assert(pos->ep == 0);

            TbMove* moves0 = stackalloc TbMove[TB_MAX_CAPTURES];
            TbMove* m = moves0;
            // Generate (at least) all legal captures including (under)promotions.
            // It is OK to generate more, as long as they are filtered out below.
            TbMove* end = gen_captures(pos, m);
            int v;

            for (; m < end; m++)
            {
                Pos pos1;
                TbMove move = *m;
                if (!is_capture(pos, move))
                    continue;
                if (!do_move(&pos1, pos, move))
                    continue; // illegal move
                v = -probe_ab(&pos1, -beta, -alpha, success);
                if (*success == 0) return 0;
                if (v > alpha)
                {
                    if (v >= beta)
                        return v;
                    alpha = v;
                }
            }

            v = probe_wdl_table(pos, success);

            return alpha >= v ? alpha : v;
        }

        // Probe the WDL table for a particular position.
        //
        // If *success != 0, the probe was successful.
        //
        // If *success == 2, the position has a winning capture, or the position
        // is a cursed win and has a cursed winning capture, or the position
        // has an ep capture as only best move.
        // This is used in probe_dtz().
        //
        // The return value is from the point of view of the side to move:
        // -2 : loss
        // -1 : loss, but draw under 50-move rule
        //  0 : draw
        //  1 : win, but draw under 50-move rule
        //  2 : win
        public static int probe_wdl(Pos *pos, int* success)
        {
            *success = 1;

            // Generate (at least) all legal captures including (under)promotions.
            TbMove* moves0 = stackalloc TbMove[TB_MAX_CAPTURES];
            TbMove* m = moves0;
            TbMove* end = gen_captures(pos, m);
            int bestCap = -3, bestEp = -3;

            int v;

            // We do capture resolution, letting bestCap keep track of the best
            // capture without ep rights and letting bestEp keep track of still
            // better ep captures if they exist.

            for (; m < end; m++)
            {
                Pos pos1;
                TbMove move = *m;
                if (!is_capture(pos, move))
                    continue;
                if (!do_move(&pos1, pos, move))
                    continue; // illegal move
                v = -probe_ab(&pos1, -2, -bestCap, success);
                if (*success == 0)
                {
                    //Log("probe_wdl is returning 0 at 1");
                    return 0;
                }
                if (v > bestCap)
                {
                    if (v == 2)
                    {
                        *success = 2;
                        return 2;
                    }
                    if (!is_en_passant(pos, move))
                        bestCap = v;
                    else if (v > bestEp)
                        bestEp = v;
                }
            }

            v = probe_wdl_table(pos, success);
            if (*success == 0)
            {
                //Log("probe_wdl is returning 0 at 2");
                return 0;
            }

            // Now max(v, bestCap) is the WDL value of the position without ep rights.
            // If the position without ep rights is not stalemate or no ep captures
            // exist, then the value of the position is max(v, bestCap, bestEp).
            // If the position without ep rights is stalemate and bestEp > -3,
            // then the value of the position is bestEp (and we will have v == 0).

            if (bestEp > bestCap)
            {
                if (bestEp > v)
                { // ep capture (possibly cursed losing) is best.
                    *success = 2;
                    return bestEp;
                }
                bestCap = bestEp;
            }

            // Now max(v, bestCap) is the WDL value of the position unless
            // the position without ep rights is stalemate and bestEp > -3.

            if (bestCap >= v)
            {
                // No need to test for the stalemate case here: either there are
                // non-ep captures, or bestCap == bestEp >= v anyway.
                *success = 1 + (bestCap > 0 ? 1 : 0);
                return bestCap;
            }

            // Now handle the stalemate case.
            if (bestEp > -3 && v == 0)
            {
                TbMove* moves = stackalloc TbMove[TB_MAX_MOVES];
                TbMove* end2 = gen_moves(pos, moves);
                // Check for stalemate in the position with ep captures.
                for (m = moves; m < end2; m++)
                {
                    if (!is_en_passant(pos, *m) && legal_move(pos, *m)) break;
                }
                if (m == end2 && !is_check(pos))
                {
                    // stalemate score from tb (w/o e.p.), but an en-passant capture
                    // is possible.
                    *success = 2;
                    return bestEp;
                }
            }
            // Stalemate / en passant not an issue, so v is the correct value.

            return v;
        }


        // Probe a position known to lose by probing the DTM table and looking
        // at captures.
        private static Value probe_dtm_loss(Pos *pos, int* success)
        {
            Value v, best = -TB_VALUE_INFINITE, numEp = 0;

            TbMove* moves0 = stackalloc TbMove[TB_MAX_CAPTURES];
            // Generate at least all legal captures including (under)promotions
            TbMove* end;
            TbMove* m = moves0;
            end = gen_captures(pos, m);

            Pos pos1;
            for (; m < end; m++)
            {
                TbMove move = *m;
                if (!is_capture(pos, move) || !legal_move(pos, move))
                    continue;
                if (is_en_passant(pos, move))
                    numEp++;
                do_move(&pos1, pos, move);
                v = -probe_dtm_win(&pos1, success) + 1;
                if (v > best)
                {
                    best = v;
                }
                if (*success == 0)
                    return 0;
            }

            // If there are en passant captures, the position without ep rights
            // may be a stalemate. If it is, we must avoid probing the DTM table.
            if (numEp != 0 && gen_legal(pos, m) == m + numEp)
                return best;

            v = -TB_VALUE_MATE + 2 * probe_dtm_table(pos, 0, success);
            return best > v ? best : v;
        }

        private static Value probe_dtm_win(Pos *pos, int* success)
        {
            Value v, best = -TB_VALUE_INFINITE;

            // Generate all moves
            TbMove* moves0 = stackalloc TbMove[TB_MAX_CAPTURES];
            TbMove* m = moves0;
            TbMove* end = gen_moves(pos, m);
            // Perform a 1-ply search
            Pos pos1;
            for (; m < end; m++)
            {
                TbMove move = *m;
                if (do_move(&pos1, pos, move))
                {
                    // not legal
                    continue;
                }
                if ((pos1.ep > 0 ? probe_wdl(&pos1, success)
                     : probe_ab(&pos1, -1, 0, success)) < 0 && (*success) != 0)
                {
                    v = -probe_dtm_loss(&pos1, success) - 1;
                }
                else
                {
                    v = -TB_VALUE_INFINITE;
                }

                if (v > best)
                {
                    best = v;
                }
                if (*success == 0) return 0;
            }

            return best;
        }

        private static Value TB_probe_dtm(Pos *pos, int wdl, int* success)
        {
            assert(wdl != 0);

            *success = 1;

            return wdl > 0 ? probe_dtm_win(pos, success)
                           : probe_dtm_loss(pos, success);
        }


        // Probe the DTZ table for a particular position.
        // If *success != 0, the probe was successful.
        // The return value is from the point of view of the side to move:
        //         n < -100 : loss, but draw under 50-move rule
        // -100 <= n < -1   : loss in n ply (assuming 50-move counter == 0)
        //         0        : draw
        //     1 < n <= 100 : win in n ply (assuming 50-move counter == 0)
        //   100 < n        : win, but draw under 50-move rule
        //
        // If the position mate, -1 is returned instead of 0.
        //
        // The return value n can be off by 1: a return value -n can mean a loss
        // in n+1 ply and a return value +n can mean a win in n+1 ply. This
        // cannot happen for tables with positions exactly on the "edge" of
        // the 50-move rule.
        //
        // This means that if dtz > 0 is returned, the position is certainly
        // a win if dtz + 50-move-counter <= 99. Care must be taken that the engine
        // picks moves that preserve dtz + 50-move-counter <= 99.
        //
        // If n = 100 immediately after a capture or pawn move, then the position
        // is also certainly a win, and during the whole phase until the next
        // capture or pawn move, the inequality to be preserved is
        // dtz + 50-movecounter <= 100.
        //
        // In short, if a move is available resulting in dtz + 50-move-counter <= 99,
        // then do not accept moves leading to dtz + 50-move-counter == 100.
        //
        public static int probe_dtz(Pos *pos, int* success)
        {
            int wdl = probe_wdl(pos, success);
            if (*success == 0) return 0;

            // If draw, then dtz = 0.
            if (wdl == 0) return 0;

            // Check for winning capture or en passant capture as only best move.
            if (*success == 2)
                return WdlToDtz[wdl + 2];

            TbMove* moves = stackalloc TbMove[TB_MAX_MOVES];
            TbMove* m = moves;
            TbMove* end = null;
            Pos pos1;

            // If winning, check for a winning pawn move.
            if (wdl > 0)
            {
                // Generate at least all legal non-capturing pawn moves
                // including non-capturing promotions.
                // (The following call in fact generates all moves.)
                end = gen_legal(pos, moves);

                for (m = moves; m < end; m++)
                {
                    TbMove move = *m;
                    if (type_of_piece_moved(pos, move) != PAWN || is_capture(pos, move))
                        continue;
                    if (!do_move(&pos1, pos, move))
                        continue; // not legal
                    int v = -probe_wdl(&pos1, success);
                    if (*success == 0) return 0;
                    if (v == wdl)
                    {
                        assert(wdl < 3);
                        return WdlToDtz[wdl + 2];
                    }
                }
            }

            // If we are here, we know that the best move is not an ep capture.
            // In other words, the value of wdl corresponds to the WDL value of
            // the position without ep rights. It is therefore safe to probe the
            // DTZ table with the current value of wdl.

            int dtz = probe_dtz_table(pos, wdl, success);
            if (*success >= 0)
                return WdlToDtz[wdl + 2] + ((wdl > 0) ? dtz : -dtz);

            // *success < 0 means we need to probe DTZ for the other side to move.
            int best;
            if (wdl > 0)
            {
                best = Int32.MaxValue;
            }
            else
            {
                // If (cursed) loss, the worst case is a losing capture or pawn move
                // as the "best" move, leading to dtz of -1 or -101.
                // In case of mate, this will cause -1 to be returned.
                best = WdlToDtz[wdl + 2];
                // If wdl < 0, we still have to generate all moves.
                end = gen_moves(pos, m);
            }
            assert(end != null);

            for (m = moves; m < end; m++)
            {
                TbMove move = *m;
                // We can skip pawn moves and captures.
                // If wdl > 0, we already caught them. If wdl < 0, the initial value
                // of best already takes account of them.
                if (is_capture(pos, move) || type_of_piece_moved(pos, move) == PAWN)
                    continue;
                if (!do_move(&pos1, pos, move))
                {
                    // move was not legal
                    continue;
                }
                int v = -probe_dtz(&pos1, success);
                // Check for the case of mate in 1
                if (v == 1 && is_mate(&pos1))
                    best = 1;
                else if (wdl > 0)
                {
                    if (v > 0 && v + 1 < best)
                        best = v + 1;
                }
                else
                {
                    if (v - 1 < best)
                        best = v - 1;
                }
                if (*success == 0) return 0;
            }
            return best;
        }

        // Use the DTZ tables to rank and score all root moves in the list.
        // A return value of 0 means that not all probes were successful.
        public static int root_probe_dtz(Pos *pos, bool hasRepeated, bool useRule50, TbRootMoves* rm)
        {
            int v, success;

            // Obtain 50-move counter for the root position.
            int cnt50 = pos->rule50;

            // The border between draw and win lies at rank 1 or rank 900, depending
            // on whether the 50-move rule is used.
            int bound = useRule50 ? 900 : 1;

            // Probe, rank and score each move.
            TbMove* rootMoves = stackalloc TbMove[TB_MAX_MOVES];
            TbMove* end = gen_legal(pos, rootMoves);
            rm->size = (unsigned)(end - rootMoves);
            Pos pos1;
            for (unsigned i = 0; i < rm->size; i++)
            {
                fixed (TbRootMove* m = &(rm->moves[i]))
                {
                    m->move = rootMoves[i];
                    do_move(&pos1, pos, m->move);

                    // Calculate dtz for the current move counting from the root position.
                    if (pos1.rule50 == 0)
                    {
                        // If the move resets the 50-move counter, dtz is -101/-1/0/1/101.
                        v = -probe_wdl(&pos1, &success);
                        assert(v < 3);
                        v = WdlToDtz[v + 2];
                    }
                    else
                    {
                        // Otherwise, take dtz for the new position and correct by 1 ply.
                        v = -probe_dtz(&pos1, &success);
                        if (v > 0) v++;
                        else if (v < 0) v--;
                    }
                    // Make sure that a mating move gets value 1.
                    if (v == 2 && is_mate(&pos1))
                    {
                        v = 1;
                    }

                    if (success == 0) return 0;

                    // Better moves are ranked higher. Guaranteed wins are ranked equally.
                    // Losing moves are ranked equally unless a 50-move draw is in sight.
                    // Note that moves ranked 900 have dtz + cnt50 == 100, which in rare
                    // cases may be insufficient to win as dtz may be one off (see the
                    // comments before TB_probe_dtz()).
                    int r = v > 0 ? (v + cnt50 <= 99 && !hasRepeated ? 1000 : 1000 - (v + cnt50))
                           : v < 0 ? (-v * 2 + cnt50 < 100 ? -1000 : -1000 + (-v + cnt50))
                           : 0;
                    m->tbRank = r;

                    // Determine the score to be displayed for this move. Assign at least
                    // 1 cp to cursed wins and let it grow to 49 cp as the position gets
                    // closer to a real win.
                    m->tbScore = r >= bound ? TB_VALUE_MATE - TB_MAX_MATE_PLY - 1
                                : r > 0 ? Math.Max(3, r - 800) * TB_VALUE_PAWN / 200
                                : r == 0 ? TB_VALUE_DRAW
                                : r > -bound ? Math.Min(-3, r + 800) * TB_VALUE_PAWN / 200
                                : -TB_VALUE_MATE + TB_MAX_MATE_PLY + 1;
                }
            }
            return 1;
        }

        // Use the WDL tables to rank all root moves in the list.
        // This is a fallback for the case that some or all DTZ tables are missing.
        // A return value of 0 means that not all probes were successful.
        public static int root_probe_wdl(Pos *pos, bool useRule50, TbRootMoves* rm)
        {
            int[] WdlToRank = [-1000, -899, 0, 899, 1000];
            Value[] WdlToValue = [
                -TB_VALUE_MATE + TB_MAX_MATE_PLY + 1,
                TB_VALUE_DRAW - 2,
                TB_VALUE_DRAW,
                TB_VALUE_DRAW + 2,
                TB_VALUE_MATE - TB_MAX_MATE_PLY - 1
            ];

            int v, success;

            // Probe, rank and score each move.
            TbMove* moves = stackalloc TbMove[TB_MAX_MOVES];
            TbMove* end = gen_legal(pos, moves);
            rm->size = (unsigned)(end - moves);
            Pos pos1;
            for (unsigned i = 0; i < rm->size; i++)
            {
                fixed(TbRootMove* m = &rm->moves[i]) 
                {
                    m->move = moves[i];
                    do_move(&pos1, pos, m->move);
                    v = -probe_wdl(&pos1, &success);
                    //  if (!success) return 0;
                    if (success == 0) return 0;
                    if (!useRule50)
                        v = v > 0 ? 2 : v < 0 ? -2 : 0;
                    m->tbRank = WdlToRank[v + 2];
                    m->tbScore = WdlToValue[v + 2];
                }
            }

            return 1;
        }

        // Use the DTM tables to find mate scores.
        // Either DTZ or WDL must have been probed successfully earlier.
        // A return value of 0 means that not all probes were successful.
        private static int root_probe_dtm(Pos *pos, TbRootMoves* rm)
        {
            int success;
            Value[] tmpScore = new Value[TB_MAX_MOVES];

            // Probe each move.
            for (unsigned i = 0; i < rm->size; i++)
            {
                Pos pos1;
                fixed (TbRootMove* m = &rm->moves[i])
                {
                    // Use tbScore to find out if the position is won or lost.
                    int wdl = m->tbScore > TB_VALUE_PAWN ? 2
                             : m->tbScore < -TB_VALUE_PAWN ? -2 : 0;

                    if (wdl == 0)
                        tmpScore[i] = 0;
                    else
                    {
                        // Probe and adjust mate score by 1 ply.
                        do_move(&pos1, pos, m->pv[0]);
                        Value v = -TB_probe_dtm(&pos1, -wdl, &success);
                        tmpScore[i] = wdl > 0 ? v - 1 : v + 1;
                        if (success == 0)
                            return 0;
                    }
                }
            }

            // All probes were successful. Now adjust TB scores and ranks.
            for (unsigned i = 0; i < rm->size; i++)
            {
                fixed (TbRootMove* m = &rm->moves[i])
                {
                    m->tbScore = tmpScore[i];

                    // Let rank correspond to mate score, except for critical moves
                    // ranked 900, which we rank below all other mates for safety.
                    // By ranking mates above 1000 or below -1000, we let the search
                    // know it need not search those moves.
                    m->tbRank = m->tbRank == 900 ? 1001 : m->tbScore;
                }
            }

            return 1;
        }


        public static void tb_expand_mate(Pos* pos, TbRootMove* move, Value moveScore, unsigned cardinalityDTM)
        {
            int success = 1, chk = 0;
            Value v = moveScore, w = 0;
            int wdl = v > 0 ? 2 : -2;

            if (move->pvSize == TB_MAX_PLY)
                return;

            Pos root = *pos;
            // First get to the end of the incomplete PV.
            for (unsigned i = 0; i < move->pvSize; i++)
            {
                v = v > 0 ? -v - 1 : -v + 1;
                wdl = -wdl;
                Pos pos0 = *pos;
                do_move(pos, &pos0, move->pv[i]);
            }

            // Now try to expand until the actual mate.
            if (popcount(pos->white | pos->black) <= cardinalityDTM)
            {
                while (v != -TB_VALUE_MATE && move->pvSize < TB_MAX_PLY)
                {
                    v = v > 0 ? -v - 1 : -v + 1;
                    wdl = -wdl;
                    TbMove* moves = stackalloc TbMove[TB_MAX_MOVES];
                    TbMove* end = gen_legal(pos, moves);
                    TbMove* m = moves;
                    for (; m < end; m++)
                    {
                        Pos pos1;
                        do_move(&pos1, pos, *m);
                        if (wdl < 0)
                            chk = probe_wdl(&pos1, &success); // verify that move wins
                        w = (success != 0) && (wdl > 0 || chk < 0)
                           ? TB_probe_dtm(&pos1, wdl, &success)
                           : 0;

                        //  if (!success || v == w) break;
                        if (success == 0 || v == w) break;
                    }

                    //  if (!success || v != w)
                    if ((success == 0) || v != w)
                        break;
                    move->pv[move->pvSize++] = *m;
                    Pos pos0 = *pos;
                    do_move(pos, &pos0, *m);
                }
            }
            // Get back to the root position.
            *pos = root;
        }




        public static uint16_t probe_root(Pos *pos, int* score, uint* results)
        {
            int success;
            int dtz = probe_dtz(pos, &success);
            if (success == 0)
                return 0;

            int16_t* scores = stackalloc int16_t[MAX_MOVES];
            TbMove* moves0 = stackalloc TbMove[MAX_MOVES];
            TbMove* moves = moves0;
            TbMove* end = gen_moves(pos, moves);
            size_t len = (ulong)(end - moves);
            size_t num_draw = 0;
            unsigned j = 0;
            for (unsigned i = 0; i < len; i++)
            {
                Pos pos1;
                if (!do_move(&pos1, pos, moves[i]))
                {
                    scores[i] = (short)SCORE_ILLEGAL;
                    continue;
                }
                int v = 0;
                //        print_move(pos,moves[i]);
                if (dtz > 0 && is_mate(&pos1))
                    v = 1;
                else
                {
                    if (pos1.rule50 != 0)
                    {
                        v = -probe_dtz(&pos1, &success);
                        if (v > 0)
                            v++;
                        else if (v < 0)
                            v--;
                    }
                    else
                    {
                        v = -probe_wdl(&pos1, &success);
                        v = wdl_to_dtz[v + 2];
                    }
                }
                num_draw += (ulong) (v == 0 ? 1 : 0);
                if (success == 0)
                    return 0;

                scores[i] = (short)v;
                if (results != null)
                {
                    unsigned res = 0;
                    res = TB_SET_WDL(res, dtz_to_wdl(pos->rule50, v));
                    res = TB_SET_FROM(res, move_from(moves[i]));
                    res = TB_SET_TO(res, move_to(moves[i]));
                    res = TB_SET_PROMOTES(res, move_promotes(moves[i]));
                    res = TB_SET_EP(res, (is_en_passant(pos, moves[i]) ? 1 : 0));
                    res = TB_SET_DTZ(res, (v < 0 ? -v : v));
                    results[j++] = res;
                }
            }
            if (results != null)
                results[j++] = TB_RESULT_FAILED;
            if (score != null)
                *score = dtz;

            // Now be a bit smart about filtering out moves.
            if (dtz > 0)        // winning (or 50-move rule draw)
            {
                int best = BEST_NONE;
                uint16_t best_move = 0;
                for (unsigned i = 0; i < len; i++)
                {
                    int v = scores[i];
                    if (v == SCORE_ILLEGAL)
                        continue;
                    if (v > 0 && v < best)
                    {
                        best = v;
                        best_move = moves[i];
                    }
                }
                return ((ushort)(best == BEST_NONE ? 0 : best_move));
            }
            else if (dtz < 0)   // losing (or 50-move rule draw)
            {
                int best = 0;
                uint16_t best_move = 0;
                for (unsigned i = 0; i < len; i++)
                {
                    int v = scores[i];
                    if (v == SCORE_ILLEGAL)
                        continue;
                    if (v < best)
                    {
                        best = v;
                        best_move = moves[i];
                    }
                }
                return ((ushort)(best == 0 ? MOVE_CHECKMATE : best_move));
            }
            else                // drawing
            {
                // Check for stalemate:
                if (num_draw == 0)
                    return (ushort)MOVE_STALEMATE;

                // Select a "random" move that preserves the draw.
                // Uses calc_key as the PRNG.
                size_t count = calc_key(pos, !pos->turn) % num_draw;
                for (unsigned i = 0; i < len; i++)
                {
                    int v = scores[i];
                    if (v == SCORE_ILLEGAL)
                        continue;
                    if (v == 0)
                    {
                        if (count == 0)
                            return moves[i];
                        count--;
                    }
                }
                return 0;
            }
        }





        public static void SetSyzygyPath(string path)
        {
            SyzygyPath = path;
        }




        private static void init_tb(string str)
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

            BaseEntry be = hasPawns ? pawnEntry[tbNumPawn++]
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
        private static void tb_free()
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
        private static uint tb_probe_wdl(
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


            ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenLegal(legal);

            for (int i = 0; i < size; i++)
            {
                int r = (int)results[i];

                var from = TB_GET_FROM(r);
                var to = TB_GET_TO(r);
                var wdl = TB_GET_WDL(r);
                var dtz = TB_GET_DTZ(results[i]);

                Log($"Results[{i} = {results[i]}]" +
                    $"\t from:{IndexToString(from)}" +
                    $"\t to: {IndexToString(to)}" +
                    $"\t wdl: {GetWDLResult((uint)wdl)}" +
                    $"\t dtz: {dtz}" +
                    $"\t promo: {TB_GET_PROMOTES(r)}" +
                    $"\t ep: {TB_GET_EP(r)}");
            }

            OrderResults(results, size);
            Log("SORTED\n");
            for (int i = 0; i < size; i++)
            {
                int r = (int)results[i];

                var from = TB_GET_FROM(r);
                var to = TB_GET_TO(r);
                var wdl = TB_GET_WDL(r);
                var dtz = TB_GET_DTZ(results[i]);

                Log($"Results[{i} = {results[i]}]" +
                    $"\t from:{IndexToString(from)}" +
                    $"\t to: {IndexToString(to)}" +
                    $"\t wdl: {GetWDLResult((uint)wdl)}" +
                    $"\t dtz: {dtz}" +
                    $"\t promo: {TB_GET_PROMOTES(r)}" +
                    $"\t ep: {TB_GET_EP(r)}");
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

            Log($"In tb_probe_root, probe_root returned {move}");

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
        private static int tb_probe_root_dtz(
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
        private static int tb_probe_root_wdl(
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

        private static bool test_tb(string file, string ext) => test_tb(file + ext);
        private static bool test_tb(string file)
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
                pieceEntry = (PieceEntry*)malloc(TB_MAX_PIECE * sizeof(PieceEntry));
                pawnEntry = (PawnEntry*)malloc(TB_MAX_PAWN * sizeof(PawnEntry));
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

        private static uint32_t from_le_u32(uint32_t x)
        {
            return x;
        }

        private static uint16_t from_le_u16(uint16_t x)
        {
            return x;
        }

        private static uint64_t from_be_u64(uint64_t input)
        {
            //return bswap64(input);
            return BinaryPrimitives.ReverseEndianness(input);
        }

        private static uint32_t from_be_u32(uint32_t input)
        {
            //return bswap32(input);
            return BinaryPrimitives.ReverseEndianness(input);
        }

        private static uint32_t read_le_u32(void* p)
        {
            return from_le_u32(*(uint32_t*)p);
        }

        private static uint16_t read_le_u16(void* p)
        {
            return from_le_u16(*(uint16_t*)p);
        }

        public static FileStream OpenSingleFile(string path)
        {
            //  CreateFile(ucode_name, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_RANDOM_ACCESS, NULL
            
            if (!File.Exists(path))
            {
                return null;
            }
            
            FileStreamOptions fso = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Mode = FileMode.Open,
                Options = FileOptions.RandomAccess
            };

            //Log($"Found {path}!");

            return File.Open(path, fso);
        }

    }
}
