
#if AVX512
using SIMDClass = System.Runtime.Intrinsics.X86.Avx512BW;
using VectorT = System.Runtime.Intrinsics.Vector512;
using VShort = System.Runtime.Intrinsics.Vector512<short>;
using VInt = System.Runtime.Intrinsics.Vector512<int>;
#else
using SIMDClass = System.Runtime.Intrinsics.X86.Avx2;
using VectorT = System.Runtime.Intrinsics.Vector256;
using VInt = System.Runtime.Intrinsics.Vector256<int>;
using VShort = System.Runtime.Intrinsics.Vector256<short>;
#endif

#pragma warning disable CS0162 // Unreachable code detected

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {

#if AVX512
        private const int N = 32;
#else
        private const int N = 16;
#endif

        private const int StopBefore = HiddenSize / 2 / N;


        public static int GetEvaluationUnrolled512(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var maxVec = VectorT.Create((short)QA);
            var zeroVec = VShort.Zero;
            var sumVec = VInt.Zero;

            Bucketed768.ProcessUpdates(pos);

            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = Math.Min((63 - occ) * (32 - occ) / 225, 7);

            const int Stride = (HiddenSize / N) / 2;

            VShort c0a, c1a, c2a, c3a;
            VShort c0b, c1b, c2b, c3b;

            #region R_STM

            var da = (VShort*) accumulator[pos.ToMove];
            var db = da + Stride;
            var weights = (VShort*)(&LayerWeights[(outputBucket * HiddenSize)]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[0]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[1]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[2]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[3]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[0]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[1]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[2]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[3]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[0]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[1]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[2]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[3]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[4]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[5]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[6]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[7]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[4]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[5]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[6]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[7]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[4]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[5]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[6]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[7]);

            if (StopBefore == 8) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[8]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[9]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[10]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[11]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[8]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[9]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[10]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[11]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[8]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[9]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[10]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[11]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[12]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[13]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[14]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[15]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[12]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[13]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[14]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[15]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[12]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[13]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[14]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[15]);

            if (StopBefore == 16) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[16]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[17]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[18]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[19]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[16]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[17]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[18]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[19]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[16]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[17]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[18]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[19]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[20]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[21]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[22]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[23]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[20]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[21]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[22]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[23]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[20]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[21]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[22]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[23]);

            if (StopBefore == 24) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[24]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[25]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[26]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[27]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[24]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[25]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[26]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[27]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[24]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[25]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[26]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[27]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[28]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[29]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[30]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[31]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[28]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[29]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[30]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[31]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[28]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[29]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[30]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[31]);

            if (StopBefore == 32) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[32]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[33]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[34]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[35]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[32]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[33]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[34]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[35]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[32]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[33]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[34]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[35]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[36]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[37]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[38]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[39]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[36]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[37]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[38]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[39]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[36]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[37]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[38]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[39]);

            if (StopBefore == 40) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[40]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[41]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[42]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[43]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[40]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[41]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[42]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[43]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[40]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[41]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[42]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[43]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[44]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[45]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[46]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[47]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[44]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[45]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[46]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[47]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[44]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[45]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[46]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[47]);

            if (StopBefore == 48) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[48]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[49]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[50]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[51]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[48]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[49]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[50]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[51]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[48]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[49]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[50]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[51]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[52]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[53]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[54]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[55]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[52]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[53]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[54]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[55]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[52]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[53]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[54]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[55]);

            if (StopBefore == 56) goto NSTM;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[56]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[57]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[58]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[59]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[56]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[57]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[58]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[59]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[56]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[57]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[58]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[59]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[60]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[61]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[62]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[63]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[60]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[61]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[62]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[63]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[60]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[61]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[62]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[63]);

            #endregion


            NSTM:

            #region R_NSTM

            da = (VShort*) accumulator[Not(pos.ToMove)];
            db = da + Stride;
            weights = (VShort*)(&LayerWeights[(outputBucket * HiddenSize) + HiddenSize / 2]);
            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[0]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[1]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[2]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[3]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[0]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[1]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[2]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[3]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[0]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[1]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[2]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[3]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[4]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[5]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[6]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[7]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[4]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[5]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[6]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[7]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[4]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[5]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[6]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[7]);

            if (StopBefore == 8) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[8]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[9]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[10]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[11]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[8]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[9]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[10]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[11]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[8]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[9]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[10]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[11]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[12]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[13]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[14]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[15]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[12]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[13]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[14]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[15]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[12]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[13]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[14]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[15]);

            if (StopBefore == 16) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[16]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[17]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[18]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[19]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[16]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[17]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[18]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[19]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[16]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[17]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[18]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[19]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[20]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[21]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[22]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[23]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[20]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[21]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[22]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[23]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[20]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[21]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[22]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[23]);

            if (StopBefore == 24) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[24]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[25]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[26]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[27]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[24]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[25]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[26]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[27]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[24]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[25]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[26]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[27]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[28]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[29]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[30]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[31]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[28]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[29]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[30]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[31]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[28]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[29]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[30]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[31]);

            if (StopBefore == 32) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[32]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[33]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[34]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[35]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[32]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[33]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[34]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[35]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[32]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[33]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[34]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[35]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[36]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[37]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[38]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[39]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[36]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[37]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[38]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[39]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[36]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[37]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[38]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[39]);

            if (StopBefore == 40) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[40]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[41]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[42]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[43]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[40]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[41]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[42]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[43]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[40]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[41]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[42]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[43]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[44]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[45]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[46]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[47]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[44]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[45]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[46]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[47]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[44]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[45]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[46]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[47]);

            if (StopBefore == 48) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[48]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[49]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[50]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[51]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[48]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[49]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[50]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[51]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[48]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[49]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[50]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[51]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[52]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[53]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[54]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[55]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[52]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[53]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[54]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[55]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[52]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[53]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[54]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[55]);

            if (StopBefore == 56) goto END;

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[56]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[57]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[58]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[59]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[56]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[57]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[58]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[59]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[56]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[57]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[58]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[59]);

            c0a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[60]));
            c1a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[61]));
            c2a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[62]));
            c3a = VectorT.Min(maxVec, VectorT.Max(zeroVec, da[63]));
            c0b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[60]));
            c1b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[61]));
            c2b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[62]));
            c3b = VectorT.Min(maxVec, VectorT.Max(zeroVec, db[63]));
            sumVec += SIMDClass.MultiplyAddAdjacent(c0b, c0a * weights[60]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c1b, c1a * weights[61]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c2b, c2a * weights[62]);
            sumVec += SIMDClass.MultiplyAddAdjacent(c3b, c3a * weights[63]);

            #endregion


            END:

            int output = NNUE.SumVectorNoHadd(sumVec);

            return (output / QA + LayerBiases[outputBucket]) * OutputScale / (QA * QB);
        }
    }
}
