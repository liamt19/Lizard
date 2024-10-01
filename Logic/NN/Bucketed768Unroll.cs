
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

        private const int StopBefore = L1_SIZE / N;


        public static int GetEvaluationUnrolled512(Position pos)
        {
#if NO
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var maxVec = VectorT.Create((short)QA);
            var zeroVec = VShort.Zero;
            var sumVec = VInt.Zero;

            Bucketed768.ProcessUpdates(pos);

            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = Math.Min((63 - occ) * (32 - occ) / 225, 7);

            var od = (short*)(accumulator[pos.ToMove]);
            var td = (short*)(accumulator[Not(pos.ToMove)]);
            var ow = &LayerWeights[outputBucket * (HiddenSize * 2)];
            var tw = &LayerWeights[outputBucket * (HiddenSize * 2) + HiddenSize];

            #region R_STM

            var cu0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[0 * N])));
            var cu1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[1 * N])));
            var cu2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[2 * N])));
            var cu3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[3 * N])));
            var cu4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[4 * N])));
            var cu5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[5 * N])));
            var cu6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[6 * N])));
            var cu7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[7 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu0, cu0 * VectorT.LoadAligned(&ow[0 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu1, cu1 * VectorT.LoadAligned(&ow[1 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu2, cu2 * VectorT.LoadAligned(&ow[2 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu3, cu3 * VectorT.LoadAligned(&ow[3 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu4, cu4 * VectorT.LoadAligned(&ow[4 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu5, cu5 * VectorT.LoadAligned(&ow[5 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu6, cu6 * VectorT.LoadAligned(&ow[6 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu7, cu7 * VectorT.LoadAligned(&ow[7 * N]));

            if (StopBefore == 8) goto NSTM;

            var cu8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[8 * N])));
            var cu9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[9 * N])));
            var cu10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[10 * N])));
            var cu11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[11 * N])));
            var cu12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[12 * N])));
            var cu13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[13 * N])));
            var cu14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[14 * N])));
            var cu15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[15 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu8, cu8 * VectorT.LoadAligned(&ow[8 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu9, cu9 * VectorT.LoadAligned(&ow[9 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu10, cu10 * VectorT.LoadAligned(&ow[10 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu11, cu11 * VectorT.LoadAligned(&ow[11 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu12, cu12 * VectorT.LoadAligned(&ow[12 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu13, cu13 * VectorT.LoadAligned(&ow[13 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu14, cu14 * VectorT.LoadAligned(&ow[14 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu15, cu15 * VectorT.LoadAligned(&ow[15 * N]));

            if (StopBefore == 16) goto NSTM;

            var cu16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[16 * N])));
            var cu17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[17 * N])));
            var cu18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[18 * N])));
            var cu19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[19 * N])));
            var cu20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[20 * N])));
            var cu21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[21 * N])));
            var cu22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[22 * N])));
            var cu23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[23 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu16, cu16 * VectorT.LoadAligned(&ow[16 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu17, cu17 * VectorT.LoadAligned(&ow[17 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu18, cu18 * VectorT.LoadAligned(&ow[18 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu19, cu19 * VectorT.LoadAligned(&ow[19 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu20, cu20 * VectorT.LoadAligned(&ow[20 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu21, cu21 * VectorT.LoadAligned(&ow[21 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu22, cu22 * VectorT.LoadAligned(&ow[22 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu23, cu23 * VectorT.LoadAligned(&ow[23 * N]));

            if (StopBefore == 24) goto NSTM;

            var cu24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[24 * N])));
            var cu25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[25 * N])));
            var cu26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[26 * N])));
            var cu27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[27 * N])));
            var cu28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[28 * N])));
            var cu29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[29 * N])));
            var cu30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[30 * N])));
            var cu31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[31 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu24, cu24 * VectorT.LoadAligned(&ow[24 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu25, cu25 * VectorT.LoadAligned(&ow[25 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu26, cu26 * VectorT.LoadAligned(&ow[26 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu27, cu27 * VectorT.LoadAligned(&ow[27 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu28, cu28 * VectorT.LoadAligned(&ow[28 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu29, cu29 * VectorT.LoadAligned(&ow[29 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu30, cu30 * VectorT.LoadAligned(&ow[30 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu31, cu31 * VectorT.LoadAligned(&ow[31 * N]));

            if (StopBefore == 32) goto NSTM;

            var cu32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[32 * N])));
            var cu33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[33 * N])));
            var cu34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[34 * N])));
            var cu35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[35 * N])));
            var cu36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[36 * N])));
            var cu37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[37 * N])));
            var cu38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[38 * N])));
            var cu39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[39 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu32, cu32 * VectorT.LoadAligned(&ow[32 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu33, cu33 * VectorT.LoadAligned(&ow[33 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu34, cu34 * VectorT.LoadAligned(&ow[34 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu35, cu35 * VectorT.LoadAligned(&ow[35 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu36, cu36 * VectorT.LoadAligned(&ow[36 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu37, cu37 * VectorT.LoadAligned(&ow[37 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu38, cu38 * VectorT.LoadAligned(&ow[38 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu39, cu39 * VectorT.LoadAligned(&ow[39 * N]));

            if (StopBefore == 40) goto NSTM;

            var cu40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[40 * N])));
            var cu41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[41 * N])));
            var cu42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[42 * N])));
            var cu43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[43 * N])));
            var cu44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[44 * N])));
            var cu45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[45 * N])));
            var cu46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[46 * N])));
            var cu47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[47 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu40, cu40 * VectorT.LoadAligned(&ow[40 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu41, cu41 * VectorT.LoadAligned(&ow[41 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu42, cu42 * VectorT.LoadAligned(&ow[42 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu43, cu43 * VectorT.LoadAligned(&ow[43 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu44, cu44 * VectorT.LoadAligned(&ow[44 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu45, cu45 * VectorT.LoadAligned(&ow[45 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu46, cu46 * VectorT.LoadAligned(&ow[46 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu47, cu47 * VectorT.LoadAligned(&ow[47 * N]));

            if (StopBefore == 48) goto NSTM;

            var cu48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[48 * N])));
            var cu49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[49 * N])));
            var cu50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[50 * N])));
            var cu51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[51 * N])));
            var cu52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[52 * N])));
            var cu53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[53 * N])));
            var cu54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[54 * N])));
            var cu55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[55 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu48, cu48 * VectorT.LoadAligned(&ow[48 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu49, cu49 * VectorT.LoadAligned(&ow[49 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu50, cu50 * VectorT.LoadAligned(&ow[50 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu51, cu51 * VectorT.LoadAligned(&ow[51 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu52, cu52 * VectorT.LoadAligned(&ow[52 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu53, cu53 * VectorT.LoadAligned(&ow[53 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu54, cu54 * VectorT.LoadAligned(&ow[54 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu55, cu55 * VectorT.LoadAligned(&ow[55 * N]));

            if (StopBefore == 56) goto NSTM;

            var cu56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[56 * N])));
            var cu57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[57 * N])));
            var cu58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[58 * N])));
            var cu59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[59 * N])));
            var cu60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[60 * N])));
            var cu61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[61 * N])));
            var cu62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[62 * N])));
            var cu63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[63 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu56, cu56 * VectorT.LoadAligned(&ow[56 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu57, cu57 * VectorT.LoadAligned(&ow[57 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu58, cu58 * VectorT.LoadAligned(&ow[58 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu59, cu59 * VectorT.LoadAligned(&ow[59 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu60, cu60 * VectorT.LoadAligned(&ow[60 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu61, cu61 * VectorT.LoadAligned(&ow[61 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu62, cu62 * VectorT.LoadAligned(&ow[62 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu63, cu63 * VectorT.LoadAligned(&ow[63 * N]));

            if (StopBefore == 64) goto NSTM;

            var cu64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[64 * N])));
            var cu65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[65 * N])));
            var cu66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[66 * N])));
            var cu67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[67 * N])));
            var cu68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[68 * N])));
            var cu69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[69 * N])));
            var cu70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[70 * N])));
            var cu71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[71 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu64, cu64 * VectorT.LoadAligned(&ow[64 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu65, cu65 * VectorT.LoadAligned(&ow[65 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu66, cu66 * VectorT.LoadAligned(&ow[66 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu67, cu67 * VectorT.LoadAligned(&ow[67 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu68, cu68 * VectorT.LoadAligned(&ow[68 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu69, cu69 * VectorT.LoadAligned(&ow[69 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu70, cu70 * VectorT.LoadAligned(&ow[70 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu71, cu71 * VectorT.LoadAligned(&ow[71 * N]));

            if (StopBefore == 72) goto NSTM;

            var cu72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[72 * N])));
            var cu73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[73 * N])));
            var cu74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[74 * N])));
            var cu75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[75 * N])));
            var cu76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[76 * N])));
            var cu77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[77 * N])));
            var cu78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[78 * N])));
            var cu79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[79 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu72, cu72 * VectorT.LoadAligned(&ow[72 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu73, cu73 * VectorT.LoadAligned(&ow[73 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu74, cu74 * VectorT.LoadAligned(&ow[74 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu75, cu75 * VectorT.LoadAligned(&ow[75 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu76, cu76 * VectorT.LoadAligned(&ow[76 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu77, cu77 * VectorT.LoadAligned(&ow[77 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu78, cu78 * VectorT.LoadAligned(&ow[78 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu79, cu79 * VectorT.LoadAligned(&ow[79 * N]));

            if (StopBefore == 80) goto NSTM;

            var cu80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[80 * N])));
            var cu81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[81 * N])));
            var cu82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[82 * N])));
            var cu83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[83 * N])));
            var cu84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[84 * N])));
            var cu85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[85 * N])));
            var cu86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[86 * N])));
            var cu87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[87 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu80, cu80 * VectorT.LoadAligned(&ow[80 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu81, cu81 * VectorT.LoadAligned(&ow[81 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu82, cu82 * VectorT.LoadAligned(&ow[82 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu83, cu83 * VectorT.LoadAligned(&ow[83 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu84, cu84 * VectorT.LoadAligned(&ow[84 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu85, cu85 * VectorT.LoadAligned(&ow[85 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu86, cu86 * VectorT.LoadAligned(&ow[86 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu87, cu87 * VectorT.LoadAligned(&ow[87 * N]));

            if (StopBefore == 88) goto NSTM;

            var cu88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[88 * N])));
            var cu89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[89 * N])));
            var cu90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[90 * N])));
            var cu91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[91 * N])));
            var cu92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[92 * N])));
            var cu93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[93 * N])));
            var cu94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[94 * N])));
            var cu95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[95 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu88, cu88 * VectorT.LoadAligned(&ow[88 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu89, cu89 * VectorT.LoadAligned(&ow[89 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu90, cu90 * VectorT.LoadAligned(&ow[90 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu91, cu91 * VectorT.LoadAligned(&ow[91 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu92, cu92 * VectorT.LoadAligned(&ow[92 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu93, cu93 * VectorT.LoadAligned(&ow[93 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu94, cu94 * VectorT.LoadAligned(&ow[94 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu95, cu95 * VectorT.LoadAligned(&ow[95 * N]));

            if (StopBefore == 96) goto NSTM;

            var cu96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[96 * N])));
            var cu97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[97 * N])));
            var cu98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[98 * N])));
            var cu99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[99 * N])));
            var cu100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[100 * N])));
            var cu101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[101 * N])));
            var cu102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[102 * N])));
            var cu103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[103 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu96, cu96 * VectorT.LoadAligned(&ow[96 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu97, cu97 * VectorT.LoadAligned(&ow[97 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu98, cu98 * VectorT.LoadAligned(&ow[98 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu99, cu99 * VectorT.LoadAligned(&ow[99 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu100, cu100 * VectorT.LoadAligned(&ow[100 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu101, cu101 * VectorT.LoadAligned(&ow[101 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu102, cu102 * VectorT.LoadAligned(&ow[102 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu103, cu103 * VectorT.LoadAligned(&ow[103 * N]));

            if (StopBefore == 104) goto NSTM;

            var cu104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[104 * N])));
            var cu105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[105 * N])));
            var cu106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[106 * N])));
            var cu107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[107 * N])));
            var cu108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[108 * N])));
            var cu109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[109 * N])));
            var cu110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[110 * N])));
            var cu111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[111 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu104, cu104 * VectorT.LoadAligned(&ow[104 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu105, cu105 * VectorT.LoadAligned(&ow[105 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu106, cu106 * VectorT.LoadAligned(&ow[106 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu107, cu107 * VectorT.LoadAligned(&ow[107 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu108, cu108 * VectorT.LoadAligned(&ow[108 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu109, cu109 * VectorT.LoadAligned(&ow[109 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu110, cu110 * VectorT.LoadAligned(&ow[110 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu111, cu111 * VectorT.LoadAligned(&ow[111 * N]));

            if (StopBefore == 112) goto NSTM;

            var cu112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[112 * N])));
            var cu113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[113 * N])));
            var cu114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[114 * N])));
            var cu115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[115 * N])));
            var cu116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[116 * N])));
            var cu117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[117 * N])));
            var cu118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[118 * N])));
            var cu119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[119 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu112, cu112 * VectorT.LoadAligned(&ow[112 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu113, cu113 * VectorT.LoadAligned(&ow[113 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu114, cu114 * VectorT.LoadAligned(&ow[114 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu115, cu115 * VectorT.LoadAligned(&ow[115 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu116, cu116 * VectorT.LoadAligned(&ow[116 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu117, cu117 * VectorT.LoadAligned(&ow[117 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu118, cu118 * VectorT.LoadAligned(&ow[118 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu119, cu119 * VectorT.LoadAligned(&ow[119 * N]));

            if (StopBefore == 120) goto NSTM;

            var cu120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[120 * N])));
            var cu121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[121 * N])));
            var cu122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[122 * N])));
            var cu123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[123 * N])));
            var cu124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[124 * N])));
            var cu125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[125 * N])));
            var cu126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[126 * N])));
            var cu127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&od[127 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu120, cu120 * VectorT.LoadAligned(&ow[120 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu121, cu121 * VectorT.LoadAligned(&ow[121 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu122, cu122 * VectorT.LoadAligned(&ow[122 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu123, cu123 * VectorT.LoadAligned(&ow[123 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu124, cu124 * VectorT.LoadAligned(&ow[124 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu125, cu125 * VectorT.LoadAligned(&ow[125 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu126, cu126 * VectorT.LoadAligned(&ow[126 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(cu127, cu127 * VectorT.LoadAligned(&ow[127 * N]));

            #endregion



            NSTM:

            #region R_NSTM

            var ct0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[0 * N])));
            var ct1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[1 * N])));
            var ct2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[2 * N])));
            var ct3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[3 * N])));
            var ct4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[4 * N])));
            var ct5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[5 * N])));
            var ct6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[6 * N])));
            var ct7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[7 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct0, ct0 * VectorT.LoadAligned(&tw[0 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct1, ct1 * VectorT.LoadAligned(&tw[1 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct2, ct2 * VectorT.LoadAligned(&tw[2 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct3, ct3 * VectorT.LoadAligned(&tw[3 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct4, ct4 * VectorT.LoadAligned(&tw[4 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct5, ct5 * VectorT.LoadAligned(&tw[5 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct6, ct6 * VectorT.LoadAligned(&tw[6 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct7, ct7 * VectorT.LoadAligned(&tw[7 * N]));

            if (StopBefore == 8) goto END;

            var ct8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[8 * N])));
            var ct9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[9 * N])));
            var ct10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[10 * N])));
            var ct11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[11 * N])));
            var ct12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[12 * N])));
            var ct13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[13 * N])));
            var ct14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[14 * N])));
            var ct15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[15 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct8, ct8 * VectorT.LoadAligned(&tw[8 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct9, ct9 * VectorT.LoadAligned(&tw[9 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct10, ct10 * VectorT.LoadAligned(&tw[10 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct11, ct11 * VectorT.LoadAligned(&tw[11 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct12, ct12 * VectorT.LoadAligned(&tw[12 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct13, ct13 * VectorT.LoadAligned(&tw[13 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct14, ct14 * VectorT.LoadAligned(&tw[14 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct15, ct15 * VectorT.LoadAligned(&tw[15 * N]));

            if (StopBefore == 16) goto END;

            var ct16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[16 * N])));
            var ct17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[17 * N])));
            var ct18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[18 * N])));
            var ct19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[19 * N])));
            var ct20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[20 * N])));
            var ct21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[21 * N])));
            var ct22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[22 * N])));
            var ct23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[23 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct16, ct16 * VectorT.LoadAligned(&tw[16 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct17, ct17 * VectorT.LoadAligned(&tw[17 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct18, ct18 * VectorT.LoadAligned(&tw[18 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct19, ct19 * VectorT.LoadAligned(&tw[19 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct20, ct20 * VectorT.LoadAligned(&tw[20 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct21, ct21 * VectorT.LoadAligned(&tw[21 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct22, ct22 * VectorT.LoadAligned(&tw[22 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct23, ct23 * VectorT.LoadAligned(&tw[23 * N]));

            if (StopBefore == 24) goto END;

            var ct24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[24 * N])));
            var ct25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[25 * N])));
            var ct26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[26 * N])));
            var ct27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[27 * N])));
            var ct28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[28 * N])));
            var ct29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[29 * N])));
            var ct30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[30 * N])));
            var ct31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[31 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct24, ct24 * VectorT.LoadAligned(&tw[24 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct25, ct25 * VectorT.LoadAligned(&tw[25 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct26, ct26 * VectorT.LoadAligned(&tw[26 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct27, ct27 * VectorT.LoadAligned(&tw[27 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct28, ct28 * VectorT.LoadAligned(&tw[28 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct29, ct29 * VectorT.LoadAligned(&tw[29 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct30, ct30 * VectorT.LoadAligned(&tw[30 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct31, ct31 * VectorT.LoadAligned(&tw[31 * N]));

            if (StopBefore == 32) goto END;

            var ct32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[32 * N])));
            var ct33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[33 * N])));
            var ct34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[34 * N])));
            var ct35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[35 * N])));
            var ct36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[36 * N])));
            var ct37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[37 * N])));
            var ct38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[38 * N])));
            var ct39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[39 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct32, ct32 * VectorT.LoadAligned(&tw[32 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct33, ct33 * VectorT.LoadAligned(&tw[33 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct34, ct34 * VectorT.LoadAligned(&tw[34 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct35, ct35 * VectorT.LoadAligned(&tw[35 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct36, ct36 * VectorT.LoadAligned(&tw[36 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct37, ct37 * VectorT.LoadAligned(&tw[37 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct38, ct38 * VectorT.LoadAligned(&tw[38 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct39, ct39 * VectorT.LoadAligned(&tw[39 * N]));

            if (StopBefore == 40) goto END;

            var ct40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[40 * N])));
            var ct41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[41 * N])));
            var ct42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[42 * N])));
            var ct43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[43 * N])));
            var ct44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[44 * N])));
            var ct45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[45 * N])));
            var ct46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[46 * N])));
            var ct47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[47 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct40, ct40 * VectorT.LoadAligned(&tw[40 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct41, ct41 * VectorT.LoadAligned(&tw[41 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct42, ct42 * VectorT.LoadAligned(&tw[42 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct43, ct43 * VectorT.LoadAligned(&tw[43 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct44, ct44 * VectorT.LoadAligned(&tw[44 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct45, ct45 * VectorT.LoadAligned(&tw[45 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct46, ct46 * VectorT.LoadAligned(&tw[46 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct47, ct47 * VectorT.LoadAligned(&tw[47 * N]));

            if (StopBefore == 48) goto END;

            var ct48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[48 * N])));
            var ct49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[49 * N])));
            var ct50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[50 * N])));
            var ct51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[51 * N])));
            var ct52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[52 * N])));
            var ct53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[53 * N])));
            var ct54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[54 * N])));
            var ct55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[55 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct48, ct48 * VectorT.LoadAligned(&tw[48 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct49, ct49 * VectorT.LoadAligned(&tw[49 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct50, ct50 * VectorT.LoadAligned(&tw[50 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct51, ct51 * VectorT.LoadAligned(&tw[51 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct52, ct52 * VectorT.LoadAligned(&tw[52 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct53, ct53 * VectorT.LoadAligned(&tw[53 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct54, ct54 * VectorT.LoadAligned(&tw[54 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct55, ct55 * VectorT.LoadAligned(&tw[55 * N]));

            if (StopBefore == 56) goto END;

            var ct56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[56 * N])));
            var ct57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[57 * N])));
            var ct58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[58 * N])));
            var ct59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[59 * N])));
            var ct60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[60 * N])));
            var ct61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[61 * N])));
            var ct62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[62 * N])));
            var ct63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[63 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct56, ct56 * VectorT.LoadAligned(&tw[56 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct57, ct57 * VectorT.LoadAligned(&tw[57 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct58, ct58 * VectorT.LoadAligned(&tw[58 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct59, ct59 * VectorT.LoadAligned(&tw[59 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct60, ct60 * VectorT.LoadAligned(&tw[60 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct61, ct61 * VectorT.LoadAligned(&tw[61 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct62, ct62 * VectorT.LoadAligned(&tw[62 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct63, ct63 * VectorT.LoadAligned(&tw[63 * N]));

            if (StopBefore == 64) goto END;

            var ct64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[64 * N])));
            var ct65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[65 * N])));
            var ct66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[66 * N])));
            var ct67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[67 * N])));
            var ct68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[68 * N])));
            var ct69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[69 * N])));
            var ct70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[70 * N])));
            var ct71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[71 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct64, ct64 * VectorT.LoadAligned(&tw[64 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct65, ct65 * VectorT.LoadAligned(&tw[65 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct66, ct66 * VectorT.LoadAligned(&tw[66 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct67, ct67 * VectorT.LoadAligned(&tw[67 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct68, ct68 * VectorT.LoadAligned(&tw[68 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct69, ct69 * VectorT.LoadAligned(&tw[69 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct70, ct70 * VectorT.LoadAligned(&tw[70 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct71, ct71 * VectorT.LoadAligned(&tw[71 * N]));

            if (StopBefore == 72) goto END;

            var ct72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[72 * N])));
            var ct73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[73 * N])));
            var ct74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[74 * N])));
            var ct75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[75 * N])));
            var ct76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[76 * N])));
            var ct77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[77 * N])));
            var ct78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[78 * N])));
            var ct79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[79 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct72, ct72 * VectorT.LoadAligned(&tw[72 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct73, ct73 * VectorT.LoadAligned(&tw[73 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct74, ct74 * VectorT.LoadAligned(&tw[74 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct75, ct75 * VectorT.LoadAligned(&tw[75 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct76, ct76 * VectorT.LoadAligned(&tw[76 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct77, ct77 * VectorT.LoadAligned(&tw[77 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct78, ct78 * VectorT.LoadAligned(&tw[78 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct79, ct79 * VectorT.LoadAligned(&tw[79 * N]));

            if (StopBefore == 80) goto END;

            var ct80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[80 * N])));
            var ct81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[81 * N])));
            var ct82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[82 * N])));
            var ct83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[83 * N])));
            var ct84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[84 * N])));
            var ct85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[85 * N])));
            var ct86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[86 * N])));
            var ct87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[87 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct80, ct80 * VectorT.LoadAligned(&tw[80 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct81, ct81 * VectorT.LoadAligned(&tw[81 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct82, ct82 * VectorT.LoadAligned(&tw[82 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct83, ct83 * VectorT.LoadAligned(&tw[83 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct84, ct84 * VectorT.LoadAligned(&tw[84 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct85, ct85 * VectorT.LoadAligned(&tw[85 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct86, ct86 * VectorT.LoadAligned(&tw[86 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct87, ct87 * VectorT.LoadAligned(&tw[87 * N]));

            if (StopBefore == 88) goto END;

            var ct88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[88 * N])));
            var ct89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[89 * N])));
            var ct90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[90 * N])));
            var ct91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[91 * N])));
            var ct92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[92 * N])));
            var ct93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[93 * N])));
            var ct94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[94 * N])));
            var ct95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[95 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct88, ct88 * VectorT.LoadAligned(&tw[88 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct89, ct89 * VectorT.LoadAligned(&tw[89 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct90, ct90 * VectorT.LoadAligned(&tw[90 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct91, ct91 * VectorT.LoadAligned(&tw[91 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct92, ct92 * VectorT.LoadAligned(&tw[92 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct93, ct93 * VectorT.LoadAligned(&tw[93 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct94, ct94 * VectorT.LoadAligned(&tw[94 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct95, ct95 * VectorT.LoadAligned(&tw[95 * N]));

            if (StopBefore == 96) goto END;

            var ct96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[96 * N])));
            var ct97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[97 * N])));
            var ct98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[98 * N])));
            var ct99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[99 * N])));
            var ct100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[100 * N])));
            var ct101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[101 * N])));
            var ct102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[102 * N])));
            var ct103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[103 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct96, ct96 * VectorT.LoadAligned(&tw[96 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct97, ct97 * VectorT.LoadAligned(&tw[97 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct98, ct98 * VectorT.LoadAligned(&tw[98 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct99, ct99 * VectorT.LoadAligned(&tw[99 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct100, ct100 * VectorT.LoadAligned(&tw[100 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct101, ct101 * VectorT.LoadAligned(&tw[101 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct102, ct102 * VectorT.LoadAligned(&tw[102 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct103, ct103 * VectorT.LoadAligned(&tw[103 * N]));

            if (StopBefore == 104) goto END;

            var ct104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[104 * N])));
            var ct105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[105 * N])));
            var ct106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[106 * N])));
            var ct107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[107 * N])));
            var ct108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[108 * N])));
            var ct109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[109 * N])));
            var ct110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[110 * N])));
            var ct111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[111 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct104, ct104 * VectorT.LoadAligned(&tw[104 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct105, ct105 * VectorT.LoadAligned(&tw[105 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct106, ct106 * VectorT.LoadAligned(&tw[106 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct107, ct107 * VectorT.LoadAligned(&tw[107 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct108, ct108 * VectorT.LoadAligned(&tw[108 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct109, ct109 * VectorT.LoadAligned(&tw[109 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct110, ct110 * VectorT.LoadAligned(&tw[110 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct111, ct111 * VectorT.LoadAligned(&tw[111 * N]));

            if (StopBefore == 112) goto END;

            var ct112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[112 * N])));
            var ct113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[113 * N])));
            var ct114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[114 * N])));
            var ct115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[115 * N])));
            var ct116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[116 * N])));
            var ct117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[117 * N])));
            var ct118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[118 * N])));
            var ct119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[119 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct112, ct112 * VectorT.LoadAligned(&tw[112 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct113, ct113 * VectorT.LoadAligned(&tw[113 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct114, ct114 * VectorT.LoadAligned(&tw[114 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct115, ct115 * VectorT.LoadAligned(&tw[115 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct116, ct116 * VectorT.LoadAligned(&tw[116 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct117, ct117 * VectorT.LoadAligned(&tw[117 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct118, ct118 * VectorT.LoadAligned(&tw[118 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct119, ct119 * VectorT.LoadAligned(&tw[119 * N]));

            if (StopBefore == 120) goto END;

            var ct120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[120 * N])));
            var ct121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[121 * N])));
            var ct122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[122 * N])));
            var ct123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[123 * N])));
            var ct124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[124 * N])));
            var ct125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[125 * N])));
            var ct126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[126 * N])));
            var ct127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(&td[127 * N])));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct120, ct120 * VectorT.LoadAligned(&tw[120 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct121, ct121 * VectorT.LoadAligned(&tw[121 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct122, ct122 * VectorT.LoadAligned(&tw[122 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct123, ct123 * VectorT.LoadAligned(&tw[123 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct124, ct124 * VectorT.LoadAligned(&tw[124 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct125, ct125 * VectorT.LoadAligned(&tw[125 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct126, ct126 * VectorT.LoadAligned(&tw[126 * N]));
            sumVec += SIMDClass.MultiplyAddAdjacent(ct127, ct127 * VectorT.LoadAligned(&tw[127 * N]));

            #endregion



            END:

            int output = NNUE.SumVectorNoHadd(sumVec);

            return (output / QA + LayerBiases[outputBucket]) * OutputScale / (QA * QB);
#endif
            return 1;
        }

    }
}
