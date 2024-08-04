
using System.Runtime.CompilerServices;

#if AVX512
using VectorT = System.Runtime.Intrinsics.Vector512;
using VShort = System.Runtime.Intrinsics.Vector512<short>;
#else
using VectorT = System.Runtime.Intrinsics.Vector256;
using VShort = System.Runtime.Intrinsics.Vector256<short>;
#endif

#pragma warning disable CS0162 // Unreachable code detected

namespace Lizard.Logic.NN
{
    public static unsafe class FunUnrollThings
    {

#if AVX512
        private const int N = 32;
#else
        private const int N = 16;
#endif

        private const int HL = Bucketed768.HiddenSize;
        private const int StopBefore = HL / N;


        [MethodImpl(Inline)]
        public static void SubAdd(short* _src, short* _dst, short* _sub1, short* _add1)
        {
            VShort* src  = (VShort*)_src;
            VShort* dst  = (VShort*)_dst;
            VShort* sub1 = (VShort*)_sub1;
            VShort* add1 = (VShort*)_add1;

            dst[ 0] = src[ 0] + add1[ 0] - sub1[ 0];

            if (StopBefore == 1) return;

            dst[ 1] = src[ 1] + add1[ 1] - sub1[ 1];

            if (StopBefore == 2) return;

            dst[ 2] = src[ 2] + add1[ 2] - sub1[ 2];
            dst[ 3] = src[ 3] + add1[ 3] - sub1[ 3];

            if (StopBefore == 4) return;

            dst[ 4] = src[ 4] + add1[ 4] - sub1[ 4];
            dst[ 5] = src[ 5] + add1[ 5] - sub1[ 5];
            dst[ 6] = src[ 6] + add1[ 6] - sub1[ 6];
            dst[ 7] = src[ 7] + add1[ 7] - sub1[ 7];

            if (StopBefore == 8) return;

            dst[ 8] = src[ 8] + add1[ 8] - sub1[ 8];
            dst[ 9] = src[ 9] + add1[ 9] - sub1[ 9];
            dst[10] = src[10] + add1[10] - sub1[10];
            dst[11] = src[11] + add1[11] - sub1[11];
            dst[12] = src[12] + add1[12] - sub1[12];
            dst[13] = src[13] + add1[13] - sub1[13];
            dst[14] = src[14] + add1[14] - sub1[14];
            dst[15] = src[15] + add1[15] - sub1[15];

            if (StopBefore == 16) return;

            dst[16] = src[16] + add1[16] - sub1[16];
            dst[17] = src[17] + add1[17] - sub1[17];
            dst[18] = src[18] + add1[18] - sub1[18];
            dst[19] = src[19] + add1[19] - sub1[19];
            dst[20] = src[20] + add1[20] - sub1[20];
            dst[21] = src[21] + add1[21] - sub1[21];
            dst[22] = src[22] + add1[22] - sub1[22];
            dst[23] = src[23] + add1[23] - sub1[23];
            dst[24] = src[24] + add1[24] - sub1[24];
            dst[25] = src[25] + add1[25] - sub1[25];
            dst[26] = src[26] + add1[26] - sub1[26];
            dst[27] = src[27] + add1[27] - sub1[27];
            dst[28] = src[28] + add1[28] - sub1[28];
            dst[29] = src[29] + add1[29] - sub1[29];
            dst[30] = src[30] + add1[30] - sub1[30];
            dst[31] = src[31] + add1[31] - sub1[31];

            if (StopBefore == 32) return;

            dst[32] = src[32] + add1[32] - sub1[32];
            dst[33] = src[33] + add1[33] - sub1[33];
            dst[34] = src[34] + add1[34] - sub1[34];
            dst[35] = src[35] + add1[35] - sub1[35];
            dst[36] = src[36] + add1[36] - sub1[36];
            dst[37] = src[37] + add1[37] - sub1[37];
            dst[38] = src[38] + add1[38] - sub1[38];
            dst[39] = src[39] + add1[39] - sub1[39];

            if (StopBefore == 40) return;

            dst[40] = src[40] + add1[40] - sub1[40];
            dst[41] = src[41] + add1[41] - sub1[41];
            dst[42] = src[42] + add1[42] - sub1[42];
            dst[43] = src[43] + add1[43] - sub1[43];
            dst[44] = src[44] + add1[44] - sub1[44];
            dst[45] = src[45] + add1[45] - sub1[45];
            dst[46] = src[46] + add1[46] - sub1[46];
            dst[47] = src[47] + add1[47] - sub1[47];

            if (StopBefore == 48) return;

            dst[48] = src[48] + add1[48] - sub1[48];
            dst[49] = src[49] + add1[49] - sub1[49];
            dst[50] = src[50] + add1[50] - sub1[50];
            dst[51] = src[51] + add1[51] - sub1[51];
            dst[52] = src[52] + add1[52] - sub1[52];
            dst[53] = src[53] + add1[53] - sub1[53];
            dst[54] = src[54] + add1[54] - sub1[54];
            dst[55] = src[55] + add1[55] - sub1[55];

            if (StopBefore == 56) return;

            dst[56] = src[56] + add1[56] - sub1[56];
            dst[57] = src[57] + add1[57] - sub1[57];
            dst[58] = src[58] + add1[58] - sub1[58];
            dst[59] = src[59] + add1[59] - sub1[59];
            dst[60] = src[60] + add1[60] - sub1[60];
            dst[61] = src[61] + add1[61] - sub1[61];
            dst[62] = src[62] + add1[62] - sub1[62];
            dst[63] = src[63] + add1[63] - sub1[63];

            if (StopBefore == 64) return;

            dst[64] = src[64] + add1[64] - sub1[64];
            dst[65] = src[65] + add1[65] - sub1[65];
            dst[66] = src[66] + add1[66] - sub1[66];
            dst[67] = src[67] + add1[67] - sub1[67];
            dst[68] = src[68] + add1[68] - sub1[68];
            dst[69] = src[69] + add1[69] - sub1[69];
            dst[70] = src[70] + add1[70] - sub1[70];
            dst[71] = src[71] + add1[71] - sub1[71];
            dst[72] = src[72] + add1[72] - sub1[72];
            dst[73] = src[73] + add1[73] - sub1[73];
            dst[74] = src[74] + add1[74] - sub1[74];
            dst[75] = src[75] + add1[75] - sub1[75];
            dst[76] = src[76] + add1[76] - sub1[76];
            dst[77] = src[77] + add1[77] - sub1[77];
            dst[78] = src[78] + add1[78] - sub1[78];
            dst[79] = src[79] + add1[79] - sub1[79];

            if (StopBefore == 80) return;

            dst[80] = src[80] + add1[80] - sub1[80];
            dst[81] = src[81] + add1[81] - sub1[81];
            dst[82] = src[82] + add1[82] - sub1[82];
            dst[83] = src[83] + add1[83] - sub1[83];
            dst[84] = src[84] + add1[84] - sub1[84];
            dst[85] = src[85] + add1[85] - sub1[85];
            dst[86] = src[86] + add1[86] - sub1[86];
            dst[87] = src[87] + add1[87] - sub1[87];
            dst[88] = src[88] + add1[88] - sub1[88];
            dst[89] = src[89] + add1[89] - sub1[89];
            dst[90] = src[90] + add1[90] - sub1[90];
            dst[91] = src[91] + add1[91] - sub1[91];
            dst[92] = src[92] + add1[92] - sub1[92];
            dst[93] = src[93] + add1[93] - sub1[93];
            dst[94] = src[94] + add1[94] - sub1[94];
            dst[95] = src[95] + add1[95] - sub1[95];

            if (StopBefore == 96) return;

            dst[ 96] = src[ 96] + add1[ 96] - sub1[ 96];
            dst[ 97] = src[ 97] + add1[ 97] - sub1[ 97];
            dst[ 98] = src[ 98] + add1[ 98] - sub1[ 98];
            dst[ 99] = src[ 99] + add1[ 99] - sub1[ 99];
            dst[100] = src[100] + add1[100] - sub1[100];
            dst[101] = src[101] + add1[101] - sub1[101];
            dst[102] = src[102] + add1[102] - sub1[102];
            dst[103] = src[103] + add1[103] - sub1[103];
            dst[104] = src[104] + add1[104] - sub1[104];
            dst[105] = src[105] + add1[105] - sub1[105];
            dst[106] = src[106] + add1[106] - sub1[106];
            dst[107] = src[107] + add1[107] - sub1[107];
            dst[108] = src[108] + add1[108] - sub1[108];
            dst[109] = src[109] + add1[109] - sub1[109];
            dst[110] = src[110] + add1[110] - sub1[110];
            dst[111] = src[111] + add1[111] - sub1[111];

            if (StopBefore == 112) return;

            dst[112] = src[112] + add1[112] - sub1[112];
            dst[113] = src[113] + add1[113] - sub1[113];
            dst[114] = src[114] + add1[114] - sub1[114];
            dst[115] = src[115] + add1[115] - sub1[115];
            dst[116] = src[116] + add1[116] - sub1[116];
            dst[117] = src[117] + add1[117] - sub1[117];
            dst[118] = src[118] + add1[118] - sub1[118];
            dst[119] = src[119] + add1[119] - sub1[119];
            dst[120] = src[120] + add1[120] - sub1[120];
            dst[121] = src[121] + add1[121] - sub1[121];
            dst[122] = src[122] + add1[122] - sub1[122];
            dst[123] = src[123] + add1[123] - sub1[123];
            dst[124] = src[124] + add1[124] - sub1[124];
            dst[125] = src[125] + add1[125] - sub1[125];
            dst[126] = src[126] + add1[126] - sub1[126];
            dst[127] = src[127] + add1[127] - sub1[127];

        }


        [MethodImpl(Inline)]
        public static void SubSubAdd(short* _src, short* _dst, short* _sub1, short* _sub2, short* _add1)
        {
            VShort* src  = (VShort*)_src;
            VShort* dst  = (VShort*)_dst;
            VShort* sub1 = (VShort*)_sub1;
            VShort* sub2 = (VShort*)_sub2;
            VShort* add1 = (VShort*)_add1;

            dst[ 0] = src[ 0] + add1[ 0] - sub1[ 0] - sub2[ 0];

            if (StopBefore == 1) return;

            dst[ 1] = src[ 1] + add1[ 1] - sub1[ 1] - sub2[ 1];

            if (StopBefore == 2) return;

            dst[ 2] = src[ 2] + add1[ 2] - sub1[ 2] - sub2[ 2];
            dst[ 3] = src[ 3] + add1[ 3] - sub1[ 3] - sub2[ 3];

            if (StopBefore == 4) return;

            dst[ 4] = src[ 4] + add1[ 4] - sub1[ 4] - sub2[ 4];
            dst[ 5] = src[ 5] + add1[ 5] - sub1[ 5] - sub2[ 5];
            dst[ 6] = src[ 6] + add1[ 6] - sub1[ 6] - sub2[ 6];
            dst[ 7] = src[ 7] + add1[ 7] - sub1[ 7] - sub2[ 7];

            if (StopBefore == 8) return;

            dst[ 8] = src[ 8] + add1[ 8] - sub1[ 8] - sub2[ 8];
            dst[ 9] = src[ 9] + add1[ 9] - sub1[ 9] - sub2[ 9];
            dst[10] = src[10] + add1[10] - sub1[10] - sub2[10];
            dst[11] = src[11] + add1[11] - sub1[11] - sub2[11];
            dst[12] = src[12] + add1[12] - sub1[12] - sub2[12];
            dst[13] = src[13] + add1[13] - sub1[13] - sub2[13];
            dst[14] = src[14] + add1[14] - sub1[14] - sub2[14];
            dst[15] = src[15] + add1[15] - sub1[15] - sub2[15];

            if (StopBefore == 16) return;

            dst[16] = src[16] + add1[16] - sub1[16] - sub2[16];
            dst[17] = src[17] + add1[17] - sub1[17] - sub2[17];
            dst[18] = src[18] + add1[18] - sub1[18] - sub2[18];
            dst[19] = src[19] + add1[19] - sub1[19] - sub2[19];
            dst[20] = src[20] + add1[20] - sub1[20] - sub2[20];
            dst[21] = src[21] + add1[21] - sub1[21] - sub2[21];
            dst[22] = src[22] + add1[22] - sub1[22] - sub2[22];
            dst[23] = src[23] + add1[23] - sub1[23] - sub2[23];
            dst[24] = src[24] + add1[24] - sub1[24] - sub2[24];
            dst[25] = src[25] + add1[25] - sub1[25] - sub2[25];
            dst[26] = src[26] + add1[26] - sub1[26] - sub2[26];
            dst[27] = src[27] + add1[27] - sub1[27] - sub2[27];
            dst[28] = src[28] + add1[28] - sub1[28] - sub2[28];
            dst[29] = src[29] + add1[29] - sub1[29] - sub2[29];
            dst[30] = src[30] + add1[30] - sub1[30] - sub2[30];
            dst[31] = src[31] + add1[31] - sub1[31] - sub2[31];

            if (StopBefore == 32) return;

            dst[32] = src[32] + add1[32] - sub1[32] - sub2[32];
            dst[33] = src[33] + add1[33] - sub1[33] - sub2[33];
            dst[34] = src[34] + add1[34] - sub1[34] - sub2[34];
            dst[35] = src[35] + add1[35] - sub1[35] - sub2[35];
            dst[36] = src[36] + add1[36] - sub1[36] - sub2[36];
            dst[37] = src[37] + add1[37] - sub1[37] - sub2[37];
            dst[38] = src[38] + add1[38] - sub1[38] - sub2[38];
            dst[39] = src[39] + add1[39] - sub1[39] - sub2[39];

            if (StopBefore == 40) return;

            dst[40] = src[40] + add1[40] - sub1[40] - sub2[40];
            dst[41] = src[41] + add1[41] - sub1[41] - sub2[41];
            dst[42] = src[42] + add1[42] - sub1[42] - sub2[42];
            dst[43] = src[43] + add1[43] - sub1[43] - sub2[43];
            dst[44] = src[44] + add1[44] - sub1[44] - sub2[44];
            dst[45] = src[45] + add1[45] - sub1[45] - sub2[45];
            dst[46] = src[46] + add1[46] - sub1[46] - sub2[46];
            dst[47] = src[47] + add1[47] - sub1[47] - sub2[47];

            if (StopBefore == 48) return;

            dst[48] = src[48] + add1[48] - sub1[48] - sub2[48];
            dst[49] = src[49] + add1[49] - sub1[49] - sub2[49];
            dst[50] = src[50] + add1[50] - sub1[50] - sub2[50];
            dst[51] = src[51] + add1[51] - sub1[51] - sub2[51];
            dst[52] = src[52] + add1[52] - sub1[52] - sub2[52];
            dst[53] = src[53] + add1[53] - sub1[53] - sub2[53];
            dst[54] = src[54] + add1[54] - sub1[54] - sub2[54];
            dst[55] = src[55] + add1[55] - sub1[55] - sub2[55];

            if (StopBefore == 56) return;

            dst[56] = src[56] + add1[56] - sub1[56] - sub2[56];
            dst[57] = src[57] + add1[57] - sub1[57] - sub2[57];
            dst[58] = src[58] + add1[58] - sub1[58] - sub2[58];
            dst[59] = src[59] + add1[59] - sub1[59] - sub2[59];
            dst[60] = src[60] + add1[60] - sub1[60] - sub2[60];
            dst[61] = src[61] + add1[61] - sub1[61] - sub2[61];
            dst[62] = src[62] + add1[62] - sub1[62] - sub2[62];
            dst[63] = src[63] + add1[63] - sub1[63] - sub2[63];

            if (StopBefore == 64) return;

            dst[64] = src[64] + add1[64] - sub1[64] - sub2[64];
            dst[65] = src[65] + add1[65] - sub1[65] - sub2[65];
            dst[66] = src[66] + add1[66] - sub1[66] - sub2[66];
            dst[67] = src[67] + add1[67] - sub1[67] - sub2[67];
            dst[68] = src[68] + add1[68] - sub1[68] - sub2[68];
            dst[69] = src[69] + add1[69] - sub1[69] - sub2[69];
            dst[70] = src[70] + add1[70] - sub1[70] - sub2[70];
            dst[71] = src[71] + add1[71] - sub1[71] - sub2[71];
            dst[72] = src[72] + add1[72] - sub1[72] - sub2[72];
            dst[73] = src[73] + add1[73] - sub1[73] - sub2[73];
            dst[74] = src[74] + add1[74] - sub1[74] - sub2[74];
            dst[75] = src[75] + add1[75] - sub1[75] - sub2[75];
            dst[76] = src[76] + add1[76] - sub1[76] - sub2[76];
            dst[77] = src[77] + add1[77] - sub1[77] - sub2[77];
            dst[78] = src[78] + add1[78] - sub1[78] - sub2[78];
            dst[79] = src[79] + add1[79] - sub1[79] - sub2[79];

            if (StopBefore == 80) return;

            dst[80] = src[80] + add1[80] - sub1[80] - sub2[80];
            dst[81] = src[81] + add1[81] - sub1[81] - sub2[81];
            dst[82] = src[82] + add1[82] - sub1[82] - sub2[82];
            dst[83] = src[83] + add1[83] - sub1[83] - sub2[83];
            dst[84] = src[84] + add1[84] - sub1[84] - sub2[84];
            dst[85] = src[85] + add1[85] - sub1[85] - sub2[85];
            dst[86] = src[86] + add1[86] - sub1[86] - sub2[86];
            dst[87] = src[87] + add1[87] - sub1[87] - sub2[87];
            dst[88] = src[88] + add1[88] - sub1[88] - sub2[88];
            dst[89] = src[89] + add1[89] - sub1[89] - sub2[89];
            dst[90] = src[90] + add1[90] - sub1[90] - sub2[90];
            dst[91] = src[91] + add1[91] - sub1[91] - sub2[91];
            dst[92] = src[92] + add1[92] - sub1[92] - sub2[92];
            dst[93] = src[93] + add1[93] - sub1[93] - sub2[93];
            dst[94] = src[94] + add1[94] - sub1[94] - sub2[94];
            dst[95] = src[95] + add1[95] - sub1[95] - sub2[95];

            if (StopBefore == 96) return;

            dst[ 96] = src[ 96] + add1[ 96] - sub1[ 96] - sub2[ 96];
            dst[ 97] = src[ 97] + add1[ 97] - sub1[ 97] - sub2[ 97];
            dst[ 98] = src[ 98] + add1[ 98] - sub1[ 98] - sub2[ 98];
            dst[ 99] = src[ 99] + add1[ 99] - sub1[ 99] - sub2[ 99];
            dst[100] = src[100] + add1[100] - sub1[100] - sub2[100];
            dst[101] = src[101] + add1[101] - sub1[101] - sub2[101];
            dst[102] = src[102] + add1[102] - sub1[102] - sub2[102];
            dst[103] = src[103] + add1[103] - sub1[103] - sub2[103];
            dst[104] = src[104] + add1[104] - sub1[104] - sub2[104];
            dst[105] = src[105] + add1[105] - sub1[105] - sub2[105];
            dst[106] = src[106] + add1[106] - sub1[106] - sub2[106];
            dst[107] = src[107] + add1[107] - sub1[107] - sub2[107];
            dst[108] = src[108] + add1[108] - sub1[108] - sub2[108];
            dst[109] = src[109] + add1[109] - sub1[109] - sub2[109];
            dst[110] = src[110] + add1[110] - sub1[110] - sub2[110];
            dst[111] = src[111] + add1[111] - sub1[111] - sub2[111];

            if (StopBefore == 112) return;

            dst[112] = src[112] + add1[112] - sub1[112] - sub2[112];
            dst[113] = src[113] + add1[113] - sub1[113] - sub2[113];
            dst[114] = src[114] + add1[114] - sub1[114] - sub2[114];
            dst[115] = src[115] + add1[115] - sub1[115] - sub2[115];
            dst[116] = src[116] + add1[116] - sub1[116] - sub2[116];
            dst[117] = src[117] + add1[117] - sub1[117] - sub2[117];
            dst[118] = src[118] + add1[118] - sub1[118] - sub2[118];
            dst[119] = src[119] + add1[119] - sub1[119] - sub2[119];
            dst[120] = src[120] + add1[120] - sub1[120] - sub2[120];
            dst[121] = src[121] + add1[121] - sub1[121] - sub2[121];
            dst[122] = src[122] + add1[122] - sub1[122] - sub2[122];
            dst[123] = src[123] + add1[123] - sub1[123] - sub2[123];
            dst[124] = src[124] + add1[124] - sub1[124] - sub2[124];
            dst[125] = src[125] + add1[125] - sub1[125] - sub2[125];
            dst[126] = src[126] + add1[126] - sub1[126] - sub2[126];
            dst[127] = src[127] + add1[127] - sub1[127] - sub2[127];

        }


        [MethodImpl(Inline)]
        public static void SubSubAddAdd(short* _src, short* _dst, short* _sub1, short* _sub2, short* _add1, short* _add2)
        {
            VShort* src  = (VShort*)_src;
            VShort* dst  = (VShort*)_dst;
            VShort* sub1 = (VShort*)_sub1;
            VShort* sub2 = (VShort*)_sub2;
            VShort* add1 = (VShort*)_add1;
            VShort* add2 = (VShort*)_add2;

            dst[ 0] = src[ 0] + add1[ 0] + add2[ 0] - sub1[ 0] - sub2[ 0];

            if (StopBefore == 1) return;

            dst[ 1] = src[ 1] + add1[ 1] + add2[ 1] - sub1[ 1] - sub2[ 1];

            if (StopBefore == 2) return;

            dst[ 2] = src[ 2] + add1[ 2] + add2[ 2] - sub1[ 2] - sub2[ 2];
            dst[ 3] = src[ 3] + add1[ 3] + add2[ 3] - sub1[ 3] - sub2[ 3];

            if (StopBefore == 4) return;

            dst[ 4] = src[ 4] + add1[ 4] + add2[ 4] - sub1[ 4] - sub2[ 4];
            dst[ 5] = src[ 5] + add1[ 5] + add2[ 5] - sub1[ 5] - sub2[ 5];
            dst[ 6] = src[ 6] + add1[ 6] + add2[ 6] - sub1[ 6] - sub2[ 6];
            dst[ 7] = src[ 7] + add1[ 7] + add2[ 7] - sub1[ 7] - sub2[ 7];

            if (StopBefore == 8) return;

            dst[ 8] = src[ 8] + add1[ 8] + add2[ 8] - sub1[ 8] - sub2[ 8];
            dst[ 9] = src[ 9] + add1[ 9] + add2[ 9] - sub1[ 9] - sub2[ 9];
            dst[10] = src[10] + add1[10] + add2[10] - sub1[10] - sub2[10];
            dst[11] = src[11] + add1[11] + add2[11] - sub1[11] - sub2[11];
            dst[12] = src[12] + add1[12] + add2[12] - sub1[12] - sub2[12];
            dst[13] = src[13] + add1[13] + add2[13] - sub1[13] - sub2[13];
            dst[14] = src[14] + add1[14] + add2[14] - sub1[14] - sub2[14];
            dst[15] = src[15] + add1[15] + add2[15] - sub1[15] - sub2[15];

            if (StopBefore == 16) return;

            dst[16] = src[16] + add1[16] + add2[16] - sub1[16] - sub2[16];
            dst[17] = src[17] + add1[17] + add2[17] - sub1[17] - sub2[17];
            dst[18] = src[18] + add1[18] + add2[18] - sub1[18] - sub2[18];
            dst[19] = src[19] + add1[19] + add2[19] - sub1[19] - sub2[19];
            dst[20] = src[20] + add1[20] + add2[20] - sub1[20] - sub2[20];
            dst[21] = src[21] + add1[21] + add2[21] - sub1[21] - sub2[21];
            dst[22] = src[22] + add1[22] + add2[22] - sub1[22] - sub2[22];
            dst[23] = src[23] + add1[23] + add2[23] - sub1[23] - sub2[23];
            dst[24] = src[24] + add1[24] + add2[24] - sub1[24] - sub2[24];
            dst[25] = src[25] + add1[25] + add2[25] - sub1[25] - sub2[25];
            dst[26] = src[26] + add1[26] + add2[26] - sub1[26] - sub2[26];
            dst[27] = src[27] + add1[27] + add2[27] - sub1[27] - sub2[27];
            dst[28] = src[28] + add1[28] + add2[28] - sub1[28] - sub2[28];
            dst[29] = src[29] + add1[29] + add2[29] - sub1[29] - sub2[29];
            dst[30] = src[30] + add1[30] + add2[30] - sub1[30] - sub2[30];
            dst[31] = src[31] + add1[31] + add2[31] - sub1[31] - sub2[31];

            if (StopBefore == 32) return;

            dst[32] = src[32] + add1[32] + add2[32] - sub1[32] - sub2[32];
            dst[33] = src[33] + add1[33] + add2[33] - sub1[33] - sub2[33];
            dst[34] = src[34] + add1[34] + add2[34] - sub1[34] - sub2[34];
            dst[35] = src[35] + add1[35] + add2[35] - sub1[35] - sub2[35];
            dst[36] = src[36] + add1[36] + add2[36] - sub1[36] - sub2[36];
            dst[37] = src[37] + add1[37] + add2[37] - sub1[37] - sub2[37];
            dst[38] = src[38] + add1[38] + add2[38] - sub1[38] - sub2[38];
            dst[39] = src[39] + add1[39] + add2[39] - sub1[39] - sub2[39];

            if (StopBefore == 40) return;

            dst[40] = src[40] + add1[40] + add2[40] - sub1[40] - sub2[40];
            dst[41] = src[41] + add1[41] + add2[41] - sub1[41] - sub2[41];
            dst[42] = src[42] + add1[42] + add2[42] - sub1[42] - sub2[42];
            dst[43] = src[43] + add1[43] + add2[43] - sub1[43] - sub2[43];
            dst[44] = src[44] + add1[44] + add2[44] - sub1[44] - sub2[44];
            dst[45] = src[45] + add1[45] + add2[45] - sub1[45] - sub2[45];
            dst[46] = src[46] + add1[46] + add2[46] - sub1[46] - sub2[46];
            dst[47] = src[47] + add1[47] + add2[47] - sub1[47] - sub2[47];

            if (StopBefore == 48) return;

            dst[48] = src[48] + add1[48] + add2[48] - sub1[48] - sub2[48];
            dst[49] = src[49] + add1[49] + add2[49] - sub1[49] - sub2[49];
            dst[50] = src[50] + add1[50] + add2[50] - sub1[50] - sub2[50];
            dst[51] = src[51] + add1[51] + add2[51] - sub1[51] - sub2[51];
            dst[52] = src[52] + add1[52] + add2[52] - sub1[52] - sub2[52];
            dst[53] = src[53] + add1[53] + add2[53] - sub1[53] - sub2[53];
            dst[54] = src[54] + add1[54] + add2[54] - sub1[54] - sub2[54];
            dst[55] = src[55] + add1[55] + add2[55] - sub1[55] - sub2[55];

            if (StopBefore == 56) return;

            dst[56] = src[56] + add1[56] + add2[56] - sub1[56] - sub2[56];
            dst[57] = src[57] + add1[57] + add2[57] - sub1[57] - sub2[57];
            dst[58] = src[58] + add1[58] + add2[58] - sub1[58] - sub2[58];
            dst[59] = src[59] + add1[59] + add2[59] - sub1[59] - sub2[59];
            dst[60] = src[60] + add1[60] + add2[60] - sub1[60] - sub2[60];
            dst[61] = src[61] + add1[61] + add2[61] - sub1[61] - sub2[61];
            dst[62] = src[62] + add1[62] + add2[62] - sub1[62] - sub2[62];
            dst[63] = src[63] + add1[63] + add2[63] - sub1[63] - sub2[63];

            if (StopBefore == 64) return;

            dst[64] = src[64] + add1[64] + add2[64] - sub1[64] - sub2[64];
            dst[65] = src[65] + add1[65] + add2[65] - sub1[65] - sub2[65];
            dst[66] = src[66] + add1[66] + add2[66] - sub1[66] - sub2[66];
            dst[67] = src[67] + add1[67] + add2[67] - sub1[67] - sub2[67];
            dst[68] = src[68] + add1[68] + add2[68] - sub1[68] - sub2[68];
            dst[69] = src[69] + add1[69] + add2[69] - sub1[69] - sub2[69];
            dst[70] = src[70] + add1[70] + add2[70] - sub1[70] - sub2[70];
            dst[71] = src[71] + add1[71] + add2[71] - sub1[71] - sub2[71];
            dst[72] = src[72] + add1[72] + add2[72] - sub1[72] - sub2[72];
            dst[73] = src[73] + add1[73] + add2[73] - sub1[73] - sub2[73];
            dst[74] = src[74] + add1[74] + add2[74] - sub1[74] - sub2[74];
            dst[75] = src[75] + add1[75] + add2[75] - sub1[75] - sub2[75];
            dst[76] = src[76] + add1[76] + add2[76] - sub1[76] - sub2[76];
            dst[77] = src[77] + add1[77] + add2[77] - sub1[77] - sub2[77];
            dst[78] = src[78] + add1[78] + add2[78] - sub1[78] - sub2[78];
            dst[79] = src[79] + add1[79] + add2[79] - sub1[79] - sub2[79];

            if (StopBefore == 80) return;

            dst[80] = src[80] + add1[80] + add2[80] - sub1[80] - sub2[80];
            dst[81] = src[81] + add1[81] + add2[81] - sub1[81] - sub2[81];
            dst[82] = src[82] + add1[82] + add2[82] - sub1[82] - sub2[82];
            dst[83] = src[83] + add1[83] + add2[83] - sub1[83] - sub2[83];
            dst[84] = src[84] + add1[84] + add2[84] - sub1[84] - sub2[84];
            dst[85] = src[85] + add1[85] + add2[85] - sub1[85] - sub2[85];
            dst[86] = src[86] + add1[86] + add2[86] - sub1[86] - sub2[86];
            dst[87] = src[87] + add1[87] + add2[87] - sub1[87] - sub2[87];
            dst[88] = src[88] + add1[88] + add2[88] - sub1[88] - sub2[88];
            dst[89] = src[89] + add1[89] + add2[89] - sub1[89] - sub2[89];
            dst[90] = src[90] + add1[90] + add2[90] - sub1[90] - sub2[90];
            dst[91] = src[91] + add1[91] + add2[91] - sub1[91] - sub2[91];
            dst[92] = src[92] + add1[92] + add2[92] - sub1[92] - sub2[92];
            dst[93] = src[93] + add1[93] + add2[93] - sub1[93] - sub2[93];
            dst[94] = src[94] + add1[94] + add2[94] - sub1[94] - sub2[94];
            dst[95] = src[95] + add1[95] + add2[95] - sub1[95] - sub2[95];

            if (StopBefore == 96) return;

            dst[ 96] = src[ 96] + add1[ 96] + add2[ 96] - sub1[ 96] - sub2[ 96];
            dst[ 97] = src[ 97] + add1[ 97] + add2[ 97] - sub1[ 97] - sub2[ 97];
            dst[ 98] = src[ 98] + add1[ 98] + add2[ 98] - sub1[ 98] - sub2[ 98];
            dst[ 99] = src[ 99] + add1[ 99] + add2[ 99] - sub1[ 99] - sub2[ 99];
            dst[100] = src[100] + add1[100] + add2[100] - sub1[100] - sub2[100];
            dst[101] = src[101] + add1[101] + add2[101] - sub1[101] - sub2[101];
            dst[102] = src[102] + add1[102] + add2[102] - sub1[102] - sub2[102];
            dst[103] = src[103] + add1[103] + add2[103] - sub1[103] - sub2[103];
            dst[104] = src[104] + add1[104] + add2[104] - sub1[104] - sub2[104];
            dst[105] = src[105] + add1[105] + add2[105] - sub1[105] - sub2[105];
            dst[106] = src[106] + add1[106] + add2[106] - sub1[106] - sub2[106];
            dst[107] = src[107] + add1[107] + add2[107] - sub1[107] - sub2[107];
            dst[108] = src[108] + add1[108] + add2[108] - sub1[108] - sub2[108];
            dst[109] = src[109] + add1[109] + add2[109] - sub1[109] - sub2[109];
            dst[110] = src[110] + add1[110] + add2[110] - sub1[110] - sub2[110];
            dst[111] = src[111] + add1[111] + add2[111] - sub1[111] - sub2[111];

            if (StopBefore == 112) return;

            dst[112] = src[112] + add1[112] + add2[112] - sub1[112] - sub2[112];
            dst[113] = src[113] + add1[113] + add2[113] - sub1[113] - sub2[113];
            dst[114] = src[114] + add1[114] + add2[114] - sub1[114] - sub2[114];
            dst[115] = src[115] + add1[115] + add2[115] - sub1[115] - sub2[115];
            dst[116] = src[116] + add1[116] + add2[116] - sub1[116] - sub2[116];
            dst[117] = src[117] + add1[117] + add2[117] - sub1[117] - sub2[117];
            dst[118] = src[118] + add1[118] + add2[118] - sub1[118] - sub2[118];
            dst[119] = src[119] + add1[119] + add2[119] - sub1[119] - sub2[119];
            dst[120] = src[120] + add1[120] + add2[120] - sub1[120] - sub2[120];
            dst[121] = src[121] + add1[121] + add2[121] - sub1[121] - sub2[121];
            dst[122] = src[122] + add1[122] + add2[122] - sub1[122] - sub2[122];
            dst[123] = src[123] + add1[123] + add2[123] - sub1[123] - sub2[123];
            dst[124] = src[124] + add1[124] + add2[124] - sub1[124] - sub2[124];
            dst[125] = src[125] + add1[125] + add2[125] - sub1[125] - sub2[125];
            dst[126] = src[126] + add1[126] + add2[126] - sub1[126] - sub2[126];
            dst[127] = src[127] + add1[127] + add2[127] - sub1[127] - sub2[127];

        }


        [MethodImpl(Inline)]
        public static void UnrollAdd(short* _src, short* _dst, short* _add1)
        {
            VShort* src  = (VShort*)_src;
            VShort* dst  = (VShort*)_dst;
            VShort* add1 = (VShort*)_add1;

            dst[ 0] = src[ 0] + add1[ 0];

            if (StopBefore == 1) return;

            dst[ 1] = src[ 1] + add1[ 1];

            if (StopBefore == 2) return;

            dst[ 2] = src[ 2] + add1[ 2];
            dst[ 3] = src[ 3] + add1[ 3];

            if (StopBefore == 4) return;

            dst[ 4] = src[ 4] + add1[ 4];
            dst[ 5] = src[ 5] + add1[ 5];
            dst[ 6] = src[ 6] + add1[ 6];
            dst[ 7] = src[ 7] + add1[ 7];

            if (StopBefore == 8) return;

            dst[ 8] = src[ 8] + add1[ 8];
            dst[ 9] = src[ 9] + add1[ 9];
            dst[10] = src[10] + add1[10];
            dst[11] = src[11] + add1[11];
            dst[12] = src[12] + add1[12];
            dst[13] = src[13] + add1[13];
            dst[14] = src[14] + add1[14];
            dst[15] = src[15] + add1[15];

            if (StopBefore == 16) return;

            dst[16] = src[16] + add1[16];
            dst[17] = src[17] + add1[17];
            dst[18] = src[18] + add1[18];
            dst[19] = src[19] + add1[19];
            dst[20] = src[20] + add1[20];
            dst[21] = src[21] + add1[21];
            dst[22] = src[22] + add1[22];
            dst[23] = src[23] + add1[23];
            dst[24] = src[24] + add1[24];
            dst[25] = src[25] + add1[25];
            dst[26] = src[26] + add1[26];
            dst[27] = src[27] + add1[27];
            dst[28] = src[28] + add1[28];
            dst[29] = src[29] + add1[29];
            dst[30] = src[30] + add1[30];
            dst[31] = src[31] + add1[31];

            if (StopBefore == 32) return;

            dst[32] = src[32] + add1[32];
            dst[33] = src[33] + add1[33];
            dst[34] = src[34] + add1[34];
            dst[35] = src[35] + add1[35];
            dst[36] = src[36] + add1[36];
            dst[37] = src[37] + add1[37];
            dst[38] = src[38] + add1[38];
            dst[39] = src[39] + add1[39];

            if (StopBefore == 40) return;

            dst[40] = src[40] + add1[40];
            dst[41] = src[41] + add1[41];
            dst[42] = src[42] + add1[42];
            dst[43] = src[43] + add1[43];
            dst[44] = src[44] + add1[44];
            dst[45] = src[45] + add1[45];
            dst[46] = src[46] + add1[46];
            dst[47] = src[47] + add1[47];

            if (StopBefore == 48) return;

            dst[48] = src[48] + add1[48];
            dst[49] = src[49] + add1[49];
            dst[50] = src[50] + add1[50];
            dst[51] = src[51] + add1[51];
            dst[52] = src[52] + add1[52];
            dst[53] = src[53] + add1[53];
            dst[54] = src[54] + add1[54];
            dst[55] = src[55] + add1[55];

            if (StopBefore == 56) return;

            dst[56] = src[56] + add1[56];
            dst[57] = src[57] + add1[57];
            dst[58] = src[58] + add1[58];
            dst[59] = src[59] + add1[59];
            dst[60] = src[60] + add1[60];
            dst[61] = src[61] + add1[61];
            dst[62] = src[62] + add1[62];
            dst[63] = src[63] + add1[63];

            if (StopBefore == 64) return;

            dst[64] = src[64] + add1[64];
            dst[65] = src[65] + add1[65];
            dst[66] = src[66] + add1[66];
            dst[67] = src[67] + add1[67];
            dst[68] = src[68] + add1[68];
            dst[69] = src[69] + add1[69];
            dst[70] = src[70] + add1[70];
            dst[71] = src[71] + add1[71];
            dst[72] = src[72] + add1[72];
            dst[73] = src[73] + add1[73];
            dst[74] = src[74] + add1[74];
            dst[75] = src[75] + add1[75];
            dst[76] = src[76] + add1[76];
            dst[77] = src[77] + add1[77];
            dst[78] = src[78] + add1[78];
            dst[79] = src[79] + add1[79];

            if (StopBefore == 80) return;

            dst[80] = src[80] + add1[80];
            dst[81] = src[81] + add1[81];
            dst[82] = src[82] + add1[82];
            dst[83] = src[83] + add1[83];
            dst[84] = src[84] + add1[84];
            dst[85] = src[85] + add1[85];
            dst[86] = src[86] + add1[86];
            dst[87] = src[87] + add1[87];
            dst[88] = src[88] + add1[88];
            dst[89] = src[89] + add1[89];
            dst[90] = src[90] + add1[90];
            dst[91] = src[91] + add1[91];
            dst[92] = src[92] + add1[92];
            dst[93] = src[93] + add1[93];
            dst[94] = src[94] + add1[94];
            dst[95] = src[95] + add1[95];

            if (StopBefore == 96) return;

            dst[ 96] = src[ 96] + add1[ 96];
            dst[ 97] = src[ 97] + add1[ 97];
            dst[ 98] = src[ 98] + add1[ 98];
            dst[ 99] = src[ 99] + add1[ 99];
            dst[100] = src[100] + add1[100];
            dst[101] = src[101] + add1[101];
            dst[102] = src[102] + add1[102];
            dst[103] = src[103] + add1[103];
            dst[104] = src[104] + add1[104];
            dst[105] = src[105] + add1[105];
            dst[106] = src[106] + add1[106];
            dst[107] = src[107] + add1[107];
            dst[108] = src[108] + add1[108];
            dst[109] = src[109] + add1[109];
            dst[110] = src[110] + add1[110];
            dst[111] = src[111] + add1[111];

            if (StopBefore == 112) return;

            dst[112] = src[112] + add1[112];
            dst[113] = src[113] + add1[113];
            dst[114] = src[114] + add1[114];
            dst[115] = src[115] + add1[115];
            dst[116] = src[116] + add1[116];
            dst[117] = src[117] + add1[117];
            dst[118] = src[118] + add1[118];
            dst[119] = src[119] + add1[119];
            dst[120] = src[120] + add1[120];
            dst[121] = src[121] + add1[121];
            dst[122] = src[122] + add1[122];
            dst[123] = src[123] + add1[123];
            dst[124] = src[124] + add1[124];
            dst[125] = src[125] + add1[125];
            dst[126] = src[126] + add1[126];
            dst[127] = src[127] + add1[127];

        }


        [MethodImpl(Inline)]
        public static void UnrollSubtract(short* _src, short* _dst, short* _sub1)
        {
            VShort* src  = (VShort*)_src;
            VShort* dst  = (VShort*)_dst;
            VShort* sub1 = (VShort*)_sub1;

            dst[ 0] = src[ 0] - sub1[ 0];

            if (StopBefore == 1) return;

            dst[ 1] = src[ 1] - sub1[ 1];

            if (StopBefore == 2) return;

            dst[ 2] = src[ 2] - sub1[ 2];
            dst[ 3] = src[ 3] - sub1[ 3];

            if (StopBefore == 4) return;

            dst[ 4] = src[ 4] - sub1[ 4];
            dst[ 5] = src[ 5] - sub1[ 5];
            dst[ 6] = src[ 6] - sub1[ 6];
            dst[ 7] = src[ 7] - sub1[ 7];
            
            if (StopBefore == 8) return;

            dst[ 8] = src[ 8] - sub1[ 8];
            dst[ 9] = src[ 9] - sub1[ 9];
            dst[10] = src[10] - sub1[10];
            dst[11] = src[11] - sub1[11];
            dst[12] = src[12] - sub1[12];
            dst[13] = src[13] - sub1[13];
            dst[14] = src[14] - sub1[14];
            dst[15] = src[15] - sub1[15];

            if (StopBefore == 16) return;

            dst[16] = src[16] - sub1[16];
            dst[17] = src[17] - sub1[17];
            dst[18] = src[18] - sub1[18];
            dst[19] = src[19] - sub1[19];
            dst[20] = src[20] - sub1[20];
            dst[21] = src[21] - sub1[21];
            dst[22] = src[22] - sub1[22];
            dst[23] = src[23] - sub1[23];
            dst[24] = src[24] - sub1[24];
            dst[25] = src[25] - sub1[25];
            dst[26] = src[26] - sub1[26];
            dst[27] = src[27] - sub1[27];
            dst[28] = src[28] - sub1[28];
            dst[29] = src[29] - sub1[29];
            dst[30] = src[30] - sub1[30];
            dst[31] = src[31] - sub1[31];

            if (StopBefore == 32) return;

            dst[32] = src[32] - sub1[32];
            dst[33] = src[33] - sub1[33];
            dst[34] = src[34] - sub1[34];
            dst[35] = src[35] - sub1[35];
            dst[36] = src[36] - sub1[36];
            dst[37] = src[37] - sub1[37];
            dst[38] = src[38] - sub1[38];
            dst[39] = src[39] - sub1[39];

            if (StopBefore == 40) return;

            dst[40] = src[40] - sub1[40];
            dst[41] = src[41] - sub1[41];
            dst[42] = src[42] - sub1[42];
            dst[43] = src[43] - sub1[43];
            dst[44] = src[44] - sub1[44];
            dst[45] = src[45] - sub1[45];
            dst[46] = src[46] - sub1[46];
            dst[47] = src[47] - sub1[47];

            if (StopBefore == 48) return;

            dst[48] = src[48] - sub1[48];
            dst[49] = src[49] - sub1[49];
            dst[50] = src[50] - sub1[50];
            dst[51] = src[51] - sub1[51];
            dst[52] = src[52] - sub1[52];
            dst[53] = src[53] - sub1[53];
            dst[54] = src[54] - sub1[54];
            dst[55] = src[55] - sub1[55];

            if (StopBefore == 56) return;

            dst[56] = src[56] - sub1[56];
            dst[57] = src[57] - sub1[57];
            dst[58] = src[58] - sub1[58];
            dst[59] = src[59] - sub1[59];
            dst[60] = src[60] - sub1[60];
            dst[61] = src[61] - sub1[61];
            dst[62] = src[62] - sub1[62];
            dst[63] = src[63] - sub1[63];

            if (StopBefore == 64) return;

            dst[64] = src[64] - sub1[64];
            dst[65] = src[65] - sub1[65];
            dst[66] = src[66] - sub1[66];
            dst[67] = src[67] - sub1[67];
            dst[68] = src[68] - sub1[68];
            dst[69] = src[69] - sub1[69];
            dst[70] = src[70] - sub1[70];
            dst[71] = src[71] - sub1[71];
            dst[72] = src[72] - sub1[72];
            dst[73] = src[73] - sub1[73];
            dst[74] = src[74] - sub1[74];
            dst[75] = src[75] - sub1[75];
            dst[76] = src[76] - sub1[76];
            dst[77] = src[77] - sub1[77];
            dst[78] = src[78] - sub1[78];
            dst[79] = src[79] - sub1[79];

            if (StopBefore == 80) return;

            dst[80] = src[80] - sub1[80];
            dst[81] = src[81] - sub1[81];
            dst[82] = src[82] - sub1[82];
            dst[83] = src[83] - sub1[83];
            dst[84] = src[84] - sub1[84];
            dst[85] = src[85] - sub1[85];
            dst[86] = src[86] - sub1[86];
            dst[87] = src[87] - sub1[87];
            dst[88] = src[88] - sub1[88];
            dst[89] = src[89] - sub1[89];
            dst[90] = src[90] - sub1[90];
            dst[91] = src[91] - sub1[91];
            dst[92] = src[92] - sub1[92];
            dst[93] = src[93] - sub1[93];
            dst[94] = src[94] - sub1[94];
            dst[95] = src[95] - sub1[95];

            if (StopBefore == 96) return;

            dst[ 96] = src[ 96] - sub1[ 96];
            dst[ 97] = src[ 97] - sub1[ 97];
            dst[ 98] = src[ 98] - sub1[ 98];
            dst[ 99] = src[ 99] - sub1[ 99];
            dst[100] = src[100] - sub1[100];
            dst[101] = src[101] - sub1[101];
            dst[102] = src[102] - sub1[102];
            dst[103] = src[103] - sub1[103];
            dst[104] = src[104] - sub1[104];
            dst[105] = src[105] - sub1[105];
            dst[106] = src[106] - sub1[106];
            dst[107] = src[107] - sub1[107];
            dst[108] = src[108] - sub1[108];
            dst[109] = src[109] - sub1[109];
            dst[110] = src[110] - sub1[110];
            dst[111] = src[111] - sub1[111];

            if (StopBefore == 112) return;

            dst[112] = src[112] - sub1[112];
            dst[113] = src[113] - sub1[113];
            dst[114] = src[114] - sub1[114];
            dst[115] = src[115] - sub1[115];
            dst[116] = src[116] - sub1[116];
            dst[117] = src[117] - sub1[117];
            dst[118] = src[118] - sub1[118];
            dst[119] = src[119] - sub1[119];
            dst[120] = src[120] - sub1[120];
            dst[121] = src[121] - sub1[121];
            dst[122] = src[122] - sub1[122];
            dst[123] = src[123] - sub1[123];
            dst[124] = src[124] - sub1[124];
            dst[125] = src[125] - sub1[125];
            dst[126] = src[126] - sub1[126];
            dst[127] = src[127] - sub1[127];

        }

    }
}