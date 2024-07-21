
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

        private const int StopBefore = HiddenSize / N;

        private const int AVX512_1024HL = 1024 / 32;
        private const int AVX512_1280HL = 1280 / 32;
        private const int AVX512_1536HL = 1536 / 32;
        private const int AVX512_1792HL = 1792 / 32;
        private const int AVX512_2048HL = 2048 / 32;

        private const int AVX256_1024HL = 1024 / 16;
        private const int AVX256_1280HL = 1280 / 16;
        private const int AVX256_1536HL = 1536 / 16;
        private const int AVX256_1792HL = 1792 / 16;
        private const int AVX256_2048HL = 2048 / 16;

        public static int GetEvaluationUnrolled512(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var maxVec = VectorT.Create((short)QA);
            var zeroVec = VShort.Zero;
            var sumVec = VInt.Zero;

            Bucketed768.ProcessUpdates(pos);

            int occ = (int)popcount(pos.bb.Occupancy);
            const int divisor = ((32 + OutputBuckets - 1) / OutputBuckets);
            int outputBucket = (occ - 2) / divisor;

            var ourData   = (VShort*)(accumulator[pos.ToMove]);
            var theirData = (VShort*)(accumulator[Not(pos.ToMove)]);
            var ourWeights  = (VShort*)(LayerWeights + (outputBucket * (HiddenSize * 2)));
            var theirWeights = (VShort*)(LayerWeights + (outputBucket * (HiddenSize * 2)) + HiddenSize);

            #region R_STM

            var c_us_0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[0]));
            var c_us_1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[1]));
            var c_us_2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[2]));
            var c_us_3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[3]));
            var c_us_4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[4]));
            var c_us_5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[5]));
            var c_us_6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[6]));
            var c_us_7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[7]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_0, SIMDClass.MultiplyLow(c_us_0, ourWeights[0])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_1, SIMDClass.MultiplyLow(c_us_1, ourWeights[1])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_2, SIMDClass.MultiplyLow(c_us_2, ourWeights[2])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_3, SIMDClass.MultiplyLow(c_us_3, ourWeights[3])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_4, SIMDClass.MultiplyLow(c_us_4, ourWeights[4])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_5, SIMDClass.MultiplyLow(c_us_5, ourWeights[5])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_6, SIMDClass.MultiplyLow(c_us_6, ourWeights[6])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_7, SIMDClass.MultiplyLow(c_us_7, ourWeights[7])));

            var c_us_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[8]));
            var c_us_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[9]));
            var c_us_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[10]));
            var c_us_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[11]));
            var c_us_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[12]));
            var c_us_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[13]));
            var c_us_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[14]));
            var c_us_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[15]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_8, SIMDClass.MultiplyLow(c_us_8, ourWeights[8])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_9, SIMDClass.MultiplyLow(c_us_9, ourWeights[9])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_10, SIMDClass.MultiplyLow(c_us_10, ourWeights[10])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_11, SIMDClass.MultiplyLow(c_us_11, ourWeights[11])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_12, SIMDClass.MultiplyLow(c_us_12, ourWeights[12])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_13, SIMDClass.MultiplyLow(c_us_13, ourWeights[13])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_14, SIMDClass.MultiplyLow(c_us_14, ourWeights[14])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_15, SIMDClass.MultiplyLow(c_us_15, ourWeights[15])));

            var c_us_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[16]));
            var c_us_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[17]));
            var c_us_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[18]));
            var c_us_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[19]));
            var c_us_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[20]));
            var c_us_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[21]));
            var c_us_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[22]));
            var c_us_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[23]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_16, SIMDClass.MultiplyLow(c_us_16, ourWeights[16])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_17, SIMDClass.MultiplyLow(c_us_17, ourWeights[17])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_18, SIMDClass.MultiplyLow(c_us_18, ourWeights[18])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_19, SIMDClass.MultiplyLow(c_us_19, ourWeights[19])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_20, SIMDClass.MultiplyLow(c_us_20, ourWeights[20])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_21, SIMDClass.MultiplyLow(c_us_21, ourWeights[21])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_22, SIMDClass.MultiplyLow(c_us_22, ourWeights[22])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_23, SIMDClass.MultiplyLow(c_us_23, ourWeights[23])));

            var c_us_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[24]));
            var c_us_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[25]));
            var c_us_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[26]));
            var c_us_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[27]));
            var c_us_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[28]));
            var c_us_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[29]));
            var c_us_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[30]));
            var c_us_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[31]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_24, SIMDClass.MultiplyLow(c_us_24, ourWeights[24])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_25, SIMDClass.MultiplyLow(c_us_25, ourWeights[25])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_26, SIMDClass.MultiplyLow(c_us_26, ourWeights[26])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_27, SIMDClass.MultiplyLow(c_us_27, ourWeights[27])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_28, SIMDClass.MultiplyLow(c_us_28, ourWeights[28])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_29, SIMDClass.MultiplyLow(c_us_29, ourWeights[29])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_30, SIMDClass.MultiplyLow(c_us_30, ourWeights[30])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_31, SIMDClass.MultiplyLow(c_us_31, ourWeights[31])));

            if (StopBefore == AVX512_1024HL)
                goto NSTM;

            var c_us_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[32]));
            var c_us_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[33]));
            var c_us_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[34]));
            var c_us_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[35]));
            var c_us_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[36]));
            var c_us_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[37]));
            var c_us_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[38]));
            var c_us_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[39]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_32, SIMDClass.MultiplyLow(c_us_32, ourWeights[32])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_33, SIMDClass.MultiplyLow(c_us_33, ourWeights[33])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_34, SIMDClass.MultiplyLow(c_us_34, ourWeights[34])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_35, SIMDClass.MultiplyLow(c_us_35, ourWeights[35])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_36, SIMDClass.MultiplyLow(c_us_36, ourWeights[36])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_37, SIMDClass.MultiplyLow(c_us_37, ourWeights[37])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_38, SIMDClass.MultiplyLow(c_us_38, ourWeights[38])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_39, SIMDClass.MultiplyLow(c_us_39, ourWeights[39])));

            if (StopBefore == AVX512_1280HL)
                goto NSTM;

            var c_us_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[40]));
            var c_us_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[41]));
            var c_us_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[42]));
            var c_us_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[43]));
            var c_us_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[44]));
            var c_us_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[45]));
            var c_us_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[46]));
            var c_us_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[47]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_40, SIMDClass.MultiplyLow(c_us_40, ourWeights[40])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_41, SIMDClass.MultiplyLow(c_us_41, ourWeights[41])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_42, SIMDClass.MultiplyLow(c_us_42, ourWeights[42])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_43, SIMDClass.MultiplyLow(c_us_43, ourWeights[43])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_44, SIMDClass.MultiplyLow(c_us_44, ourWeights[44])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_45, SIMDClass.MultiplyLow(c_us_45, ourWeights[45])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_46, SIMDClass.MultiplyLow(c_us_46, ourWeights[46])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_47, SIMDClass.MultiplyLow(c_us_47, ourWeights[47])));

            if (StopBefore == AVX512_1536HL)
                goto NSTM;

            var c_us_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[48]));
            var c_us_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[49]));
            var c_us_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[50]));
            var c_us_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[51]));
            var c_us_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[52]));
            var c_us_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[53]));
            var c_us_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[54]));
            var c_us_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[55]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_48, SIMDClass.MultiplyLow(c_us_48, ourWeights[48])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_49, SIMDClass.MultiplyLow(c_us_49, ourWeights[49])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_50, SIMDClass.MultiplyLow(c_us_50, ourWeights[50])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_51, SIMDClass.MultiplyLow(c_us_51, ourWeights[51])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_52, SIMDClass.MultiplyLow(c_us_52, ourWeights[52])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_53, SIMDClass.MultiplyLow(c_us_53, ourWeights[53])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_54, SIMDClass.MultiplyLow(c_us_54, ourWeights[54])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_55, SIMDClass.MultiplyLow(c_us_55, ourWeights[55])));

            if (StopBefore == AVX512_1792HL)
                goto NSTM;

            var c_us_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[56]));
            var c_us_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[57]));
            var c_us_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[58]));
            var c_us_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[59]));
            var c_us_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[60]));
            var c_us_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[61]));
            var c_us_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[62]));
            var c_us_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[63]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_56, SIMDClass.MultiplyLow(c_us_56, ourWeights[56])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_57, SIMDClass.MultiplyLow(c_us_57, ourWeights[57])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_58, SIMDClass.MultiplyLow(c_us_58, ourWeights[58])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_59, SIMDClass.MultiplyLow(c_us_59, ourWeights[59])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_60, SIMDClass.MultiplyLow(c_us_60, ourWeights[60])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_61, SIMDClass.MultiplyLow(c_us_61, ourWeights[61])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_62, SIMDClass.MultiplyLow(c_us_62, ourWeights[62])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_63, SIMDClass.MultiplyLow(c_us_63, ourWeights[63])));

            if (StopBefore == AVX512_2048HL)
                goto NSTM;

            if (StopBefore == AVX256_1024HL)
                goto NSTM;

            var c_us_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[64]));
            var c_us_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[65]));
            var c_us_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[66]));
            var c_us_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[67]));
            var c_us_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[68]));
            var c_us_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[69]));
            var c_us_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[70]));
            var c_us_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[71]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_64, SIMDClass.MultiplyLow(c_us_64, ourWeights[64])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_65, SIMDClass.MultiplyLow(c_us_65, ourWeights[65])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_66, SIMDClass.MultiplyLow(c_us_66, ourWeights[66])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_67, SIMDClass.MultiplyLow(c_us_67, ourWeights[67])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_68, SIMDClass.MultiplyLow(c_us_68, ourWeights[68])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_69, SIMDClass.MultiplyLow(c_us_69, ourWeights[69])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_70, SIMDClass.MultiplyLow(c_us_70, ourWeights[70])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_71, SIMDClass.MultiplyLow(c_us_71, ourWeights[71])));

            var c_us_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[72]));
            var c_us_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[73]));
            var c_us_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[74]));
            var c_us_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[75]));
            var c_us_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[76]));
            var c_us_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[77]));
            var c_us_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[78]));
            var c_us_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[79]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_72, SIMDClass.MultiplyLow(c_us_72, ourWeights[72])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_73, SIMDClass.MultiplyLow(c_us_73, ourWeights[73])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_74, SIMDClass.MultiplyLow(c_us_74, ourWeights[74])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_75, SIMDClass.MultiplyLow(c_us_75, ourWeights[75])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_76, SIMDClass.MultiplyLow(c_us_76, ourWeights[76])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_77, SIMDClass.MultiplyLow(c_us_77, ourWeights[77])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_78, SIMDClass.MultiplyLow(c_us_78, ourWeights[78])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_79, SIMDClass.MultiplyLow(c_us_79, ourWeights[79])));

            if (StopBefore == AVX256_1280HL)
                goto NSTM;

            var c_us_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[80]));
            var c_us_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[81]));
            var c_us_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[82]));
            var c_us_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[83]));
            var c_us_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[84]));
            var c_us_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[85]));
            var c_us_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[86]));
            var c_us_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[87]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_80, SIMDClass.MultiplyLow(c_us_80, ourWeights[80])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_81, SIMDClass.MultiplyLow(c_us_81, ourWeights[81])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_82, SIMDClass.MultiplyLow(c_us_82, ourWeights[82])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_83, SIMDClass.MultiplyLow(c_us_83, ourWeights[83])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_84, SIMDClass.MultiplyLow(c_us_84, ourWeights[84])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_85, SIMDClass.MultiplyLow(c_us_85, ourWeights[85])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_86, SIMDClass.MultiplyLow(c_us_86, ourWeights[86])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_87, SIMDClass.MultiplyLow(c_us_87, ourWeights[87])));

            var c_us_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[88]));
            var c_us_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[89]));
            var c_us_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[90]));
            var c_us_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[91]));
            var c_us_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[92]));
            var c_us_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[93]));
            var c_us_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[94]));
            var c_us_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[95]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_88, SIMDClass.MultiplyLow(c_us_88, ourWeights[88])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_89, SIMDClass.MultiplyLow(c_us_89, ourWeights[89])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_90, SIMDClass.MultiplyLow(c_us_90, ourWeights[90])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_91, SIMDClass.MultiplyLow(c_us_91, ourWeights[91])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_92, SIMDClass.MultiplyLow(c_us_92, ourWeights[92])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_93, SIMDClass.MultiplyLow(c_us_93, ourWeights[93])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_94, SIMDClass.MultiplyLow(c_us_94, ourWeights[94])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_95, SIMDClass.MultiplyLow(c_us_95, ourWeights[95])));

            if (StopBefore == AVX256_1536HL)
                goto NSTM;

            var c_us_96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[96]));
            var c_us_97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[97]));
            var c_us_98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[98]));
            var c_us_99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[99]));
            var c_us_100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[100]));
            var c_us_101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[101]));
            var c_us_102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[102]));
            var c_us_103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[103]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_96, SIMDClass.MultiplyLow(c_us_96, ourWeights[96])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_97, SIMDClass.MultiplyLow(c_us_97, ourWeights[97])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_98, SIMDClass.MultiplyLow(c_us_98, ourWeights[98])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_99, SIMDClass.MultiplyLow(c_us_99, ourWeights[99])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_100, SIMDClass.MultiplyLow(c_us_100, ourWeights[100])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_101, SIMDClass.MultiplyLow(c_us_101, ourWeights[101])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_102, SIMDClass.MultiplyLow(c_us_102, ourWeights[102])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_103, SIMDClass.MultiplyLow(c_us_103, ourWeights[103])));

            var c_us_104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[104]));
            var c_us_105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[105]));
            var c_us_106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[106]));
            var c_us_107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[107]));
            var c_us_108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[108]));
            var c_us_109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[109]));
            var c_us_110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[110]));
            var c_us_111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[111]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_104, SIMDClass.MultiplyLow(c_us_104, ourWeights[104])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_105, SIMDClass.MultiplyLow(c_us_105, ourWeights[105])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_106, SIMDClass.MultiplyLow(c_us_106, ourWeights[106])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_107, SIMDClass.MultiplyLow(c_us_107, ourWeights[107])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_108, SIMDClass.MultiplyLow(c_us_108, ourWeights[108])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_109, SIMDClass.MultiplyLow(c_us_109, ourWeights[109])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_110, SIMDClass.MultiplyLow(c_us_110, ourWeights[110])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_111, SIMDClass.MultiplyLow(c_us_111, ourWeights[111])));

            if (StopBefore == AVX256_1792HL)
                goto NSTM;

            var c_us_112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[112]));
            var c_us_113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[113]));
            var c_us_114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[114]));
            var c_us_115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[115]));
            var c_us_116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[116]));
            var c_us_117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[117]));
            var c_us_118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[118]));
            var c_us_119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[119]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_112, SIMDClass.MultiplyLow(c_us_112, ourWeights[112])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_113, SIMDClass.MultiplyLow(c_us_113, ourWeights[113])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_114, SIMDClass.MultiplyLow(c_us_114, ourWeights[114])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_115, SIMDClass.MultiplyLow(c_us_115, ourWeights[115])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_116, SIMDClass.MultiplyLow(c_us_116, ourWeights[116])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_117, SIMDClass.MultiplyLow(c_us_117, ourWeights[117])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_118, SIMDClass.MultiplyLow(c_us_118, ourWeights[118])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_119, SIMDClass.MultiplyLow(c_us_119, ourWeights[119])));

            var c_us_120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[120]));
            var c_us_121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[121]));
            var c_us_122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[122]));
            var c_us_123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[123]));
            var c_us_124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[124]));
            var c_us_125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[125]));
            var c_us_126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[126]));
            var c_us_127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, ourData[127]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_120, SIMDClass.MultiplyLow(c_us_120, ourWeights[120])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_121, SIMDClass.MultiplyLow(c_us_121, ourWeights[121])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_122, SIMDClass.MultiplyLow(c_us_122, ourWeights[122])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_123, SIMDClass.MultiplyLow(c_us_123, ourWeights[123])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_124, SIMDClass.MultiplyLow(c_us_124, ourWeights[124])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_125, SIMDClass.MultiplyLow(c_us_125, ourWeights[125])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_126, SIMDClass.MultiplyLow(c_us_126, ourWeights[126])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_us_127, SIMDClass.MultiplyLow(c_us_127, ourWeights[127])));

            #endregion



            NSTM:

            #region R_NSTM

            var c_them_0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[0]));
            var c_them_1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[1]));
            var c_them_2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[2]));
            var c_them_3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[3]));
            var c_them_4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[4]));
            var c_them_5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[5]));
            var c_them_6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[6]));
            var c_them_7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[7]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_0, SIMDClass.MultiplyLow(c_them_0, theirWeights[0])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_1, SIMDClass.MultiplyLow(c_them_1, theirWeights[1])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_2, SIMDClass.MultiplyLow(c_them_2, theirWeights[2])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_3, SIMDClass.MultiplyLow(c_them_3, theirWeights[3])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_4, SIMDClass.MultiplyLow(c_them_4, theirWeights[4])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_5, SIMDClass.MultiplyLow(c_them_5, theirWeights[5])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_6, SIMDClass.MultiplyLow(c_them_6, theirWeights[6])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_7, SIMDClass.MultiplyLow(c_them_7, theirWeights[7])));

            var c_them_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[8]));
            var c_them_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[9]));
            var c_them_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[10]));
            var c_them_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[11]));
            var c_them_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[12]));
            var c_them_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[13]));
            var c_them_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[14]));
            var c_them_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[15]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_8, SIMDClass.MultiplyLow(c_them_8, theirWeights[8])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_9, SIMDClass.MultiplyLow(c_them_9, theirWeights[9])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_10, SIMDClass.MultiplyLow(c_them_10, theirWeights[10])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_11, SIMDClass.MultiplyLow(c_them_11, theirWeights[11])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_12, SIMDClass.MultiplyLow(c_them_12, theirWeights[12])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_13, SIMDClass.MultiplyLow(c_them_13, theirWeights[13])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_14, SIMDClass.MultiplyLow(c_them_14, theirWeights[14])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_15, SIMDClass.MultiplyLow(c_them_15, theirWeights[15])));

            var c_them_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[16]));
            var c_them_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[17]));
            var c_them_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[18]));
            var c_them_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[19]));
            var c_them_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[20]));
            var c_them_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[21]));
            var c_them_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[22]));
            var c_them_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[23]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_16, SIMDClass.MultiplyLow(c_them_16, theirWeights[16])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_17, SIMDClass.MultiplyLow(c_them_17, theirWeights[17])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_18, SIMDClass.MultiplyLow(c_them_18, theirWeights[18])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_19, SIMDClass.MultiplyLow(c_them_19, theirWeights[19])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_20, SIMDClass.MultiplyLow(c_them_20, theirWeights[20])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_21, SIMDClass.MultiplyLow(c_them_21, theirWeights[21])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_22, SIMDClass.MultiplyLow(c_them_22, theirWeights[22])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_23, SIMDClass.MultiplyLow(c_them_23, theirWeights[23])));

            var c_them_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[24]));
            var c_them_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[25]));
            var c_them_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[26]));
            var c_them_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[27]));
            var c_them_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[28]));
            var c_them_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[29]));
            var c_them_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[30]));
            var c_them_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[31]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_24, SIMDClass.MultiplyLow(c_them_24, theirWeights[24])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_25, SIMDClass.MultiplyLow(c_them_25, theirWeights[25])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_26, SIMDClass.MultiplyLow(c_them_26, theirWeights[26])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_27, SIMDClass.MultiplyLow(c_them_27, theirWeights[27])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_28, SIMDClass.MultiplyLow(c_them_28, theirWeights[28])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_29, SIMDClass.MultiplyLow(c_them_29, theirWeights[29])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_30, SIMDClass.MultiplyLow(c_them_30, theirWeights[30])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_31, SIMDClass.MultiplyLow(c_them_31, theirWeights[31])));

            if (StopBefore == AVX512_1024HL)
                goto END;

            var c_them_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[32]));
            var c_them_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[33]));
            var c_them_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[34]));
            var c_them_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[35]));
            var c_them_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[36]));
            var c_them_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[37]));
            var c_them_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[38]));
            var c_them_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[39]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_32, SIMDClass.MultiplyLow(c_them_32, theirWeights[32])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_33, SIMDClass.MultiplyLow(c_them_33, theirWeights[33])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_34, SIMDClass.MultiplyLow(c_them_34, theirWeights[34])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_35, SIMDClass.MultiplyLow(c_them_35, theirWeights[35])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_36, SIMDClass.MultiplyLow(c_them_36, theirWeights[36])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_37, SIMDClass.MultiplyLow(c_them_37, theirWeights[37])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_38, SIMDClass.MultiplyLow(c_them_38, theirWeights[38])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_39, SIMDClass.MultiplyLow(c_them_39, theirWeights[39])));

            if (StopBefore == AVX512_1280HL)
                goto END;

            var c_them_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[40]));
            var c_them_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[41]));
            var c_them_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[42]));
            var c_them_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[43]));
            var c_them_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[44]));
            var c_them_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[45]));
            var c_them_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[46]));
            var c_them_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[47]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_40, SIMDClass.MultiplyLow(c_them_40, theirWeights[40])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_41, SIMDClass.MultiplyLow(c_them_41, theirWeights[41])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_42, SIMDClass.MultiplyLow(c_them_42, theirWeights[42])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_43, SIMDClass.MultiplyLow(c_them_43, theirWeights[43])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_44, SIMDClass.MultiplyLow(c_them_44, theirWeights[44])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_45, SIMDClass.MultiplyLow(c_them_45, theirWeights[45])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_46, SIMDClass.MultiplyLow(c_them_46, theirWeights[46])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_47, SIMDClass.MultiplyLow(c_them_47, theirWeights[47])));

            if (StopBefore == AVX512_1536HL)
                goto END;

            var c_them_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[48]));
            var c_them_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[49]));
            var c_them_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[50]));
            var c_them_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[51]));
            var c_them_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[52]));
            var c_them_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[53]));
            var c_them_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[54]));
            var c_them_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[55]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_48, SIMDClass.MultiplyLow(c_them_48, theirWeights[48])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_49, SIMDClass.MultiplyLow(c_them_49, theirWeights[49])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_50, SIMDClass.MultiplyLow(c_them_50, theirWeights[50])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_51, SIMDClass.MultiplyLow(c_them_51, theirWeights[51])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_52, SIMDClass.MultiplyLow(c_them_52, theirWeights[52])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_53, SIMDClass.MultiplyLow(c_them_53, theirWeights[53])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_54, SIMDClass.MultiplyLow(c_them_54, theirWeights[54])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_55, SIMDClass.MultiplyLow(c_them_55, theirWeights[55])));

            if (StopBefore == AVX512_1792HL)
                goto END;

            var c_them_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[56]));
            var c_them_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[57]));
            var c_them_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[58]));
            var c_them_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[59]));
            var c_them_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[60]));
            var c_them_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[61]));
            var c_them_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[62]));
            var c_them_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[63]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_56, SIMDClass.MultiplyLow(c_them_56, theirWeights[56])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_57, SIMDClass.MultiplyLow(c_them_57, theirWeights[57])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_58, SIMDClass.MultiplyLow(c_them_58, theirWeights[58])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_59, SIMDClass.MultiplyLow(c_them_59, theirWeights[59])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_60, SIMDClass.MultiplyLow(c_them_60, theirWeights[60])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_61, SIMDClass.MultiplyLow(c_them_61, theirWeights[61])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_62, SIMDClass.MultiplyLow(c_them_62, theirWeights[62])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_63, SIMDClass.MultiplyLow(c_them_63, theirWeights[63])));

            if (StopBefore == AVX512_2048HL)
                goto END;

            if (StopBefore == AVX256_1024HL)
                goto END;

            var c_them_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[64]));
            var c_them_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[65]));
            var c_them_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[66]));
            var c_them_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[67]));
            var c_them_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[68]));
            var c_them_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[69]));
            var c_them_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[70]));
            var c_them_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[71]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_64, SIMDClass.MultiplyLow(c_them_64, theirWeights[64])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_65, SIMDClass.MultiplyLow(c_them_65, theirWeights[65])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_66, SIMDClass.MultiplyLow(c_them_66, theirWeights[66])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_67, SIMDClass.MultiplyLow(c_them_67, theirWeights[67])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_68, SIMDClass.MultiplyLow(c_them_68, theirWeights[68])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_69, SIMDClass.MultiplyLow(c_them_69, theirWeights[69])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_70, SIMDClass.MultiplyLow(c_them_70, theirWeights[70])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_71, SIMDClass.MultiplyLow(c_them_71, theirWeights[71])));

            var c_them_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[72]));
            var c_them_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[73]));
            var c_them_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[74]));
            var c_them_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[75]));
            var c_them_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[76]));
            var c_them_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[77]));
            var c_them_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[78]));
            var c_them_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[79]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_72, SIMDClass.MultiplyLow(c_them_72, theirWeights[72])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_73, SIMDClass.MultiplyLow(c_them_73, theirWeights[73])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_74, SIMDClass.MultiplyLow(c_them_74, theirWeights[74])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_75, SIMDClass.MultiplyLow(c_them_75, theirWeights[75])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_76, SIMDClass.MultiplyLow(c_them_76, theirWeights[76])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_77, SIMDClass.MultiplyLow(c_them_77, theirWeights[77])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_78, SIMDClass.MultiplyLow(c_them_78, theirWeights[78])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_79, SIMDClass.MultiplyLow(c_them_79, theirWeights[79])));

            if (StopBefore == AVX256_1280HL)
                goto END;

            var c_them_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[80]));
            var c_them_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[81]));
            var c_them_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[82]));
            var c_them_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[83]));
            var c_them_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[84]));
            var c_them_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[85]));
            var c_them_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[86]));
            var c_them_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[87]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_80, SIMDClass.MultiplyLow(c_them_80, theirWeights[80])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_81, SIMDClass.MultiplyLow(c_them_81, theirWeights[81])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_82, SIMDClass.MultiplyLow(c_them_82, theirWeights[82])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_83, SIMDClass.MultiplyLow(c_them_83, theirWeights[83])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_84, SIMDClass.MultiplyLow(c_them_84, theirWeights[84])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_85, SIMDClass.MultiplyLow(c_them_85, theirWeights[85])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_86, SIMDClass.MultiplyLow(c_them_86, theirWeights[86])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_87, SIMDClass.MultiplyLow(c_them_87, theirWeights[87])));

            var c_them_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[88]));
            var c_them_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[89]));
            var c_them_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[90]));
            var c_them_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[91]));
            var c_them_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[92]));
            var c_them_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[93]));
            var c_them_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[94]));
            var c_them_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[95]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_88, SIMDClass.MultiplyLow(c_them_88, theirWeights[88])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_89, SIMDClass.MultiplyLow(c_them_89, theirWeights[89])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_90, SIMDClass.MultiplyLow(c_them_90, theirWeights[90])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_91, SIMDClass.MultiplyLow(c_them_91, theirWeights[91])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_92, SIMDClass.MultiplyLow(c_them_92, theirWeights[92])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_93, SIMDClass.MultiplyLow(c_them_93, theirWeights[93])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_94, SIMDClass.MultiplyLow(c_them_94, theirWeights[94])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_95, SIMDClass.MultiplyLow(c_them_95, theirWeights[95])));

            if (StopBefore == AVX256_1536HL)
                goto END;

            var c_them_96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[96]));
            var c_them_97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[97]));
            var c_them_98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[98]));
            var c_them_99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[99]));
            var c_them_100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[100]));
            var c_them_101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[101]));
            var c_them_102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[102]));
            var c_them_103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[103]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_96, SIMDClass.MultiplyLow(c_them_96, theirWeights[96])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_97, SIMDClass.MultiplyLow(c_them_97, theirWeights[97])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_98, SIMDClass.MultiplyLow(c_them_98, theirWeights[98])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_99, SIMDClass.MultiplyLow(c_them_99, theirWeights[99])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_100, SIMDClass.MultiplyLow(c_them_100, theirWeights[100])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_101, SIMDClass.MultiplyLow(c_them_101, theirWeights[101])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_102, SIMDClass.MultiplyLow(c_them_102, theirWeights[102])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_103, SIMDClass.MultiplyLow(c_them_103, theirWeights[103])));

            var c_them_104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[104]));
            var c_them_105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[105]));
            var c_them_106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[106]));
            var c_them_107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[107]));
            var c_them_108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[108]));
            var c_them_109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[109]));
            var c_them_110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[110]));
            var c_them_111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[111]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_104, SIMDClass.MultiplyLow(c_them_104, theirWeights[104])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_105, SIMDClass.MultiplyLow(c_them_105, theirWeights[105])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_106, SIMDClass.MultiplyLow(c_them_106, theirWeights[106])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_107, SIMDClass.MultiplyLow(c_them_107, theirWeights[107])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_108, SIMDClass.MultiplyLow(c_them_108, theirWeights[108])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_109, SIMDClass.MultiplyLow(c_them_109, theirWeights[109])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_110, SIMDClass.MultiplyLow(c_them_110, theirWeights[110])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_111, SIMDClass.MultiplyLow(c_them_111, theirWeights[111])));

            if (StopBefore == AVX256_1792HL)
                goto END;

            var c_them_112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[112]));
            var c_them_113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[113]));
            var c_them_114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[114]));
            var c_them_115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[115]));
            var c_them_116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[116]));
            var c_them_117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[117]));
            var c_them_118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[118]));
            var c_them_119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[119]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_112, SIMDClass.MultiplyLow(c_them_112, theirWeights[112])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_113, SIMDClass.MultiplyLow(c_them_113, theirWeights[113])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_114, SIMDClass.MultiplyLow(c_them_114, theirWeights[114])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_115, SIMDClass.MultiplyLow(c_them_115, theirWeights[115])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_116, SIMDClass.MultiplyLow(c_them_116, theirWeights[116])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_117, SIMDClass.MultiplyLow(c_them_117, theirWeights[117])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_118, SIMDClass.MultiplyLow(c_them_118, theirWeights[118])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_119, SIMDClass.MultiplyLow(c_them_119, theirWeights[119])));

            var c_them_120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[120]));
            var c_them_121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[121]));
            var c_them_122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[122]));
            var c_them_123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[123]));
            var c_them_124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[124]));
            var c_them_125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[125]));
            var c_them_126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[126]));
            var c_them_127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, theirData[127]));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_120, SIMDClass.MultiplyLow(c_them_120, theirWeights[120])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_121, SIMDClass.MultiplyLow(c_them_121, theirWeights[121])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_122, SIMDClass.MultiplyLow(c_them_122, theirWeights[122])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_123, SIMDClass.MultiplyLow(c_them_123, theirWeights[123])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_124, SIMDClass.MultiplyLow(c_them_124, theirWeights[124])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_125, SIMDClass.MultiplyLow(c_them_125, theirWeights[125])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_126, SIMDClass.MultiplyLow(c_them_126, theirWeights[126])));
            sumVec += (SIMDClass.MultiplyAddAdjacent(c_them_127, SIMDClass.MultiplyLow(c_them_127, theirWeights[127])));

            #endregion



            END:

            int output = NNUE.SumVectorNoHadd(sumVec);

            return (output / QA + LayerBiases[outputBucket]) * OutputScale / (QA * QB);
        }

    }
}