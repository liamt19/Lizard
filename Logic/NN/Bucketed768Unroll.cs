
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
        private const int AVX512_1536HL = 1536 / 32;

        private const int AVX256_1024HL = 1024 / 16;
        private const int AVX256_1536HL = 1536 / 16;

        public static int GetEvaluationUnrolled512(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var maxVec = VectorT.Create((short)QA);
            var zeroVec = VShort.Zero;
            var sumVec = VInt.Zero;

            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

            int outputBucket = (SelectOutputBucket) ? (int)((popcount(pos.bb.Occupancy) - 2) / 4) : 0;

            var ourData =   (short*)(accumulator[pos.ToMove]);
            var theirData = (short*)(accumulator[Not(pos.ToMove)]);
            var ourWeights =   (LayerWeights + (outputBucket * (HiddenSize * 2)));
            var theirWeights = (LayerWeights + (outputBucket * (HiddenSize * 2)) + HiddenSize);

            #region R_STM

            var c_us_0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 0 * N)));
            var c_us_1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 1 * N)));
            var c_us_2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 2 * N)));
            var c_us_3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 3 * N)));
            var c_us_4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 4 * N)));
            var c_us_5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 5 * N)));
            var c_us_6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 6 * N)));
            var c_us_7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 7 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_0, SIMDClass.MultiplyLow(c_us_0, VectorT.LoadAligned(ourWeights + 0 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_1, SIMDClass.MultiplyLow(c_us_1, VectorT.LoadAligned(ourWeights + 1 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_2, SIMDClass.MultiplyLow(c_us_2, VectorT.LoadAligned(ourWeights + 2 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_3, SIMDClass.MultiplyLow(c_us_3, VectorT.LoadAligned(ourWeights + 3 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_4, SIMDClass.MultiplyLow(c_us_4, VectorT.LoadAligned(ourWeights + 4 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_5, SIMDClass.MultiplyLow(c_us_5, VectorT.LoadAligned(ourWeights + 5 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_6, SIMDClass.MultiplyLow(c_us_6, VectorT.LoadAligned(ourWeights + 6 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_7, SIMDClass.MultiplyLow(c_us_7, VectorT.LoadAligned(ourWeights + 7 * N))));

            var c_us_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 8 * N)));
            var c_us_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 9 * N)));
            var c_us_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 10 * N)));
            var c_us_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 11 * N)));
            var c_us_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 12 * N)));
            var c_us_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 13 * N)));
            var c_us_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 14 * N)));
            var c_us_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 15 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_8, SIMDClass.MultiplyLow(c_us_8, VectorT.LoadAligned(ourWeights + 8 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_9, SIMDClass.MultiplyLow(c_us_9, VectorT.LoadAligned(ourWeights + 9 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_10, SIMDClass.MultiplyLow(c_us_10, VectorT.LoadAligned(ourWeights + 10 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_11, SIMDClass.MultiplyLow(c_us_11, VectorT.LoadAligned(ourWeights + 11 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_12, SIMDClass.MultiplyLow(c_us_12, VectorT.LoadAligned(ourWeights + 12 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_13, SIMDClass.MultiplyLow(c_us_13, VectorT.LoadAligned(ourWeights + 13 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_14, SIMDClass.MultiplyLow(c_us_14, VectorT.LoadAligned(ourWeights + 14 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_15, SIMDClass.MultiplyLow(c_us_15, VectorT.LoadAligned(ourWeights + 15 * N))));

            var c_us_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 16 * N)));
            var c_us_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 17 * N)));
            var c_us_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 18 * N)));
            var c_us_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 19 * N)));
            var c_us_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 20 * N)));
            var c_us_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 21 * N)));
            var c_us_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 22 * N)));
            var c_us_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 23 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_16, SIMDClass.MultiplyLow(c_us_16, VectorT.LoadAligned(ourWeights + 16 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_17, SIMDClass.MultiplyLow(c_us_17, VectorT.LoadAligned(ourWeights + 17 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_18, SIMDClass.MultiplyLow(c_us_18, VectorT.LoadAligned(ourWeights + 18 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_19, SIMDClass.MultiplyLow(c_us_19, VectorT.LoadAligned(ourWeights + 19 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_20, SIMDClass.MultiplyLow(c_us_20, VectorT.LoadAligned(ourWeights + 20 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_21, SIMDClass.MultiplyLow(c_us_21, VectorT.LoadAligned(ourWeights + 21 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_22, SIMDClass.MultiplyLow(c_us_22, VectorT.LoadAligned(ourWeights + 22 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_23, SIMDClass.MultiplyLow(c_us_23, VectorT.LoadAligned(ourWeights + 23 * N))));

            var c_us_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 24 * N)));
            var c_us_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 25 * N)));
            var c_us_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 26 * N)));
            var c_us_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 27 * N)));
            var c_us_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 28 * N)));
            var c_us_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 29 * N)));
            var c_us_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 30 * N)));
            var c_us_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 31 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_24, SIMDClass.MultiplyLow(c_us_24, VectorT.LoadAligned(ourWeights + 24 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_25, SIMDClass.MultiplyLow(c_us_25, VectorT.LoadAligned(ourWeights + 25 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_26, SIMDClass.MultiplyLow(c_us_26, VectorT.LoadAligned(ourWeights + 26 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_27, SIMDClass.MultiplyLow(c_us_27, VectorT.LoadAligned(ourWeights + 27 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_28, SIMDClass.MultiplyLow(c_us_28, VectorT.LoadAligned(ourWeights + 28 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_29, SIMDClass.MultiplyLow(c_us_29, VectorT.LoadAligned(ourWeights + 29 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_30, SIMDClass.MultiplyLow(c_us_30, VectorT.LoadAligned(ourWeights + 30 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_31, SIMDClass.MultiplyLow(c_us_31, VectorT.LoadAligned(ourWeights + 31 * N))));

            if (StopBefore == AVX512_1024HL)
                goto NSTM;

            var c_us_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 32 * N)));
            var c_us_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 33 * N)));
            var c_us_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 34 * N)));
            var c_us_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 35 * N)));
            var c_us_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 36 * N)));
            var c_us_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 37 * N)));
            var c_us_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 38 * N)));
            var c_us_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 39 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_32, SIMDClass.MultiplyLow(c_us_32, VectorT.LoadAligned(ourWeights + 32 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_33, SIMDClass.MultiplyLow(c_us_33, VectorT.LoadAligned(ourWeights + 33 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_34, SIMDClass.MultiplyLow(c_us_34, VectorT.LoadAligned(ourWeights + 34 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_35, SIMDClass.MultiplyLow(c_us_35, VectorT.LoadAligned(ourWeights + 35 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_36, SIMDClass.MultiplyLow(c_us_36, VectorT.LoadAligned(ourWeights + 36 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_37, SIMDClass.MultiplyLow(c_us_37, VectorT.LoadAligned(ourWeights + 37 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_38, SIMDClass.MultiplyLow(c_us_38, VectorT.LoadAligned(ourWeights + 38 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_39, SIMDClass.MultiplyLow(c_us_39, VectorT.LoadAligned(ourWeights + 39 * N))));

            var c_us_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 40 * N)));
            var c_us_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 41 * N)));
            var c_us_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 42 * N)));
            var c_us_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 43 * N)));
            var c_us_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 44 * N)));
            var c_us_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 45 * N)));
            var c_us_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 46 * N)));
            var c_us_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 47 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_40, SIMDClass.MultiplyLow(c_us_40, VectorT.LoadAligned(ourWeights + 40 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_41, SIMDClass.MultiplyLow(c_us_41, VectorT.LoadAligned(ourWeights + 41 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_42, SIMDClass.MultiplyLow(c_us_42, VectorT.LoadAligned(ourWeights + 42 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_43, SIMDClass.MultiplyLow(c_us_43, VectorT.LoadAligned(ourWeights + 43 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_44, SIMDClass.MultiplyLow(c_us_44, VectorT.LoadAligned(ourWeights + 44 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_45, SIMDClass.MultiplyLow(c_us_45, VectorT.LoadAligned(ourWeights + 45 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_46, SIMDClass.MultiplyLow(c_us_46, VectorT.LoadAligned(ourWeights + 46 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_47, SIMDClass.MultiplyLow(c_us_47, VectorT.LoadAligned(ourWeights + 47 * N))));

            if (StopBefore == AVX512_1536HL)
                goto NSTM;

            var c_us_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 48 * N)));
            var c_us_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 49 * N)));
            var c_us_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 50 * N)));
            var c_us_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 51 * N)));
            var c_us_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 52 * N)));
            var c_us_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 53 * N)));
            var c_us_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 54 * N)));
            var c_us_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 55 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_48, SIMDClass.MultiplyLow(c_us_48, VectorT.LoadAligned(ourWeights + 48 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_49, SIMDClass.MultiplyLow(c_us_49, VectorT.LoadAligned(ourWeights + 49 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_50, SIMDClass.MultiplyLow(c_us_50, VectorT.LoadAligned(ourWeights + 50 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_51, SIMDClass.MultiplyLow(c_us_51, VectorT.LoadAligned(ourWeights + 51 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_52, SIMDClass.MultiplyLow(c_us_52, VectorT.LoadAligned(ourWeights + 52 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_53, SIMDClass.MultiplyLow(c_us_53, VectorT.LoadAligned(ourWeights + 53 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_54, SIMDClass.MultiplyLow(c_us_54, VectorT.LoadAligned(ourWeights + 54 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_55, SIMDClass.MultiplyLow(c_us_55, VectorT.LoadAligned(ourWeights + 55 * N))));

            var c_us_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 56 * N)));
            var c_us_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 57 * N)));
            var c_us_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 58 * N)));
            var c_us_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 59 * N)));
            var c_us_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 60 * N)));
            var c_us_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 61 * N)));
            var c_us_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 62 * N)));
            var c_us_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 63 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_56, SIMDClass.MultiplyLow(c_us_56, VectorT.LoadAligned(ourWeights + 56 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_57, SIMDClass.MultiplyLow(c_us_57, VectorT.LoadAligned(ourWeights + 57 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_58, SIMDClass.MultiplyLow(c_us_58, VectorT.LoadAligned(ourWeights + 58 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_59, SIMDClass.MultiplyLow(c_us_59, VectorT.LoadAligned(ourWeights + 59 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_60, SIMDClass.MultiplyLow(c_us_60, VectorT.LoadAligned(ourWeights + 60 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_61, SIMDClass.MultiplyLow(c_us_61, VectorT.LoadAligned(ourWeights + 61 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_62, SIMDClass.MultiplyLow(c_us_62, VectorT.LoadAligned(ourWeights + 62 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_63, SIMDClass.MultiplyLow(c_us_63, VectorT.LoadAligned(ourWeights + 63 * N))));

            if (StopBefore == AVX256_1024HL)
                goto NSTM;

            var c_us_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 64 * N)));
            var c_us_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 65 * N)));
            var c_us_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 66 * N)));
            var c_us_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 67 * N)));
            var c_us_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 68 * N)));
            var c_us_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 69 * N)));
            var c_us_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 70 * N)));
            var c_us_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 71 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_64, SIMDClass.MultiplyLow(c_us_64, VectorT.LoadAligned(ourWeights + 64 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_65, SIMDClass.MultiplyLow(c_us_65, VectorT.LoadAligned(ourWeights + 65 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_66, SIMDClass.MultiplyLow(c_us_66, VectorT.LoadAligned(ourWeights + 66 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_67, SIMDClass.MultiplyLow(c_us_67, VectorT.LoadAligned(ourWeights + 67 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_68, SIMDClass.MultiplyLow(c_us_68, VectorT.LoadAligned(ourWeights + 68 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_69, SIMDClass.MultiplyLow(c_us_69, VectorT.LoadAligned(ourWeights + 69 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_70, SIMDClass.MultiplyLow(c_us_70, VectorT.LoadAligned(ourWeights + 70 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_71, SIMDClass.MultiplyLow(c_us_71, VectorT.LoadAligned(ourWeights + 71 * N))));

            var c_us_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 72 * N)));
            var c_us_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 73 * N)));
            var c_us_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 74 * N)));
            var c_us_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 75 * N)));
            var c_us_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 76 * N)));
            var c_us_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 77 * N)));
            var c_us_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 78 * N)));
            var c_us_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 79 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_72, SIMDClass.MultiplyLow(c_us_72, VectorT.LoadAligned(ourWeights + 72 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_73, SIMDClass.MultiplyLow(c_us_73, VectorT.LoadAligned(ourWeights + 73 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_74, SIMDClass.MultiplyLow(c_us_74, VectorT.LoadAligned(ourWeights + 74 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_75, SIMDClass.MultiplyLow(c_us_75, VectorT.LoadAligned(ourWeights + 75 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_76, SIMDClass.MultiplyLow(c_us_76, VectorT.LoadAligned(ourWeights + 76 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_77, SIMDClass.MultiplyLow(c_us_77, VectorT.LoadAligned(ourWeights + 77 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_78, SIMDClass.MultiplyLow(c_us_78, VectorT.LoadAligned(ourWeights + 78 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_79, SIMDClass.MultiplyLow(c_us_79, VectorT.LoadAligned(ourWeights + 79 * N))));

            var c_us_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 80 * N)));
            var c_us_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 81 * N)));
            var c_us_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 82 * N)));
            var c_us_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 83 * N)));
            var c_us_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 84 * N)));
            var c_us_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 85 * N)));
            var c_us_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 86 * N)));
            var c_us_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 87 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_80, SIMDClass.MultiplyLow(c_us_80, VectorT.LoadAligned(ourWeights + 80 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_81, SIMDClass.MultiplyLow(c_us_81, VectorT.LoadAligned(ourWeights + 81 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_82, SIMDClass.MultiplyLow(c_us_82, VectorT.LoadAligned(ourWeights + 82 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_83, SIMDClass.MultiplyLow(c_us_83, VectorT.LoadAligned(ourWeights + 83 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_84, SIMDClass.MultiplyLow(c_us_84, VectorT.LoadAligned(ourWeights + 84 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_85, SIMDClass.MultiplyLow(c_us_85, VectorT.LoadAligned(ourWeights + 85 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_86, SIMDClass.MultiplyLow(c_us_86, VectorT.LoadAligned(ourWeights + 86 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_87, SIMDClass.MultiplyLow(c_us_87, VectorT.LoadAligned(ourWeights + 87 * N))));

            var c_us_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 88 * N)));
            var c_us_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 89 * N)));
            var c_us_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 90 * N)));
            var c_us_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 91 * N)));
            var c_us_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 92 * N)));
            var c_us_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 93 * N)));
            var c_us_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 94 * N)));
            var c_us_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 95 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_88, SIMDClass.MultiplyLow(c_us_88, VectorT.LoadAligned(ourWeights + 88 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_89, SIMDClass.MultiplyLow(c_us_89, VectorT.LoadAligned(ourWeights + 89 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_90, SIMDClass.MultiplyLow(c_us_90, VectorT.LoadAligned(ourWeights + 90 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_91, SIMDClass.MultiplyLow(c_us_91, VectorT.LoadAligned(ourWeights + 91 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_92, SIMDClass.MultiplyLow(c_us_92, VectorT.LoadAligned(ourWeights + 92 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_93, SIMDClass.MultiplyLow(c_us_93, VectorT.LoadAligned(ourWeights + 93 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_94, SIMDClass.MultiplyLow(c_us_94, VectorT.LoadAligned(ourWeights + 94 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_us_95, SIMDClass.MultiplyLow(c_us_95, VectorT.LoadAligned(ourWeights + 95 * N))));

            #endregion



            NSTM:

            #region R_NSTM

            var c_them_0 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 0 * N)));
            var c_them_1 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 1 * N)));
            var c_them_2 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 2 * N)));
            var c_them_3 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 3 * N)));
            var c_them_4 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 4 * N)));
            var c_them_5 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 5 * N)));
            var c_them_6 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 6 * N)));
            var c_them_7 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 7 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_0, SIMDClass.MultiplyLow(c_them_0, VectorT.LoadAligned(theirWeights + 0 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_1, SIMDClass.MultiplyLow(c_them_1, VectorT.LoadAligned(theirWeights + 1 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_2, SIMDClass.MultiplyLow(c_them_2, VectorT.LoadAligned(theirWeights + 2 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_3, SIMDClass.MultiplyLow(c_them_3, VectorT.LoadAligned(theirWeights + 3 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_4, SIMDClass.MultiplyLow(c_them_4, VectorT.LoadAligned(theirWeights + 4 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_5, SIMDClass.MultiplyLow(c_them_5, VectorT.LoadAligned(theirWeights + 5 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_6, SIMDClass.MultiplyLow(c_them_6, VectorT.LoadAligned(theirWeights + 6 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_7, SIMDClass.MultiplyLow(c_them_7, VectorT.LoadAligned(theirWeights + 7 * N))));

            var c_them_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 8 * N)));
            var c_them_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 9 * N)));
            var c_them_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 10 * N)));
            var c_them_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 11 * N)));
            var c_them_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 12 * N)));
            var c_them_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 13 * N)));
            var c_them_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 14 * N)));
            var c_them_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 15 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_8, SIMDClass.MultiplyLow(c_them_8, VectorT.LoadAligned(theirWeights + 8 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_9, SIMDClass.MultiplyLow(c_them_9, VectorT.LoadAligned(theirWeights + 9 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_10, SIMDClass.MultiplyLow(c_them_10, VectorT.LoadAligned(theirWeights + 10 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_11, SIMDClass.MultiplyLow(c_them_11, VectorT.LoadAligned(theirWeights + 11 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_12, SIMDClass.MultiplyLow(c_them_12, VectorT.LoadAligned(theirWeights + 12 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_13, SIMDClass.MultiplyLow(c_them_13, VectorT.LoadAligned(theirWeights + 13 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_14, SIMDClass.MultiplyLow(c_them_14, VectorT.LoadAligned(theirWeights + 14 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_15, SIMDClass.MultiplyLow(c_them_15, VectorT.LoadAligned(theirWeights + 15 * N))));

            var c_them_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 16 * N)));
            var c_them_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 17 * N)));
            var c_them_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 18 * N)));
            var c_them_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 19 * N)));
            var c_them_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 20 * N)));
            var c_them_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 21 * N)));
            var c_them_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 22 * N)));
            var c_them_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 23 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_16, SIMDClass.MultiplyLow(c_them_16, VectorT.LoadAligned(theirWeights + 16 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_17, SIMDClass.MultiplyLow(c_them_17, VectorT.LoadAligned(theirWeights + 17 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_18, SIMDClass.MultiplyLow(c_them_18, VectorT.LoadAligned(theirWeights + 18 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_19, SIMDClass.MultiplyLow(c_them_19, VectorT.LoadAligned(theirWeights + 19 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_20, SIMDClass.MultiplyLow(c_them_20, VectorT.LoadAligned(theirWeights + 20 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_21, SIMDClass.MultiplyLow(c_them_21, VectorT.LoadAligned(theirWeights + 21 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_22, SIMDClass.MultiplyLow(c_them_22, VectorT.LoadAligned(theirWeights + 22 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_23, SIMDClass.MultiplyLow(c_them_23, VectorT.LoadAligned(theirWeights + 23 * N))));

            var c_them_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 24 * N)));
            var c_them_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 25 * N)));
            var c_them_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 26 * N)));
            var c_them_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 27 * N)));
            var c_them_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 28 * N)));
            var c_them_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 29 * N)));
            var c_them_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 30 * N)));
            var c_them_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 31 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_24, SIMDClass.MultiplyLow(c_them_24, VectorT.LoadAligned(theirWeights + 24 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_25, SIMDClass.MultiplyLow(c_them_25, VectorT.LoadAligned(theirWeights + 25 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_26, SIMDClass.MultiplyLow(c_them_26, VectorT.LoadAligned(theirWeights + 26 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_27, SIMDClass.MultiplyLow(c_them_27, VectorT.LoadAligned(theirWeights + 27 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_28, SIMDClass.MultiplyLow(c_them_28, VectorT.LoadAligned(theirWeights + 28 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_29, SIMDClass.MultiplyLow(c_them_29, VectorT.LoadAligned(theirWeights + 29 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_30, SIMDClass.MultiplyLow(c_them_30, VectorT.LoadAligned(theirWeights + 30 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_31, SIMDClass.MultiplyLow(c_them_31, VectorT.LoadAligned(theirWeights + 31 * N))));

            if (StopBefore == AVX512_1024HL)
                goto END;

            var c_them_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 32 * N)));
            var c_them_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 33 * N)));
            var c_them_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 34 * N)));
            var c_them_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 35 * N)));
            var c_them_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 36 * N)));
            var c_them_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 37 * N)));
            var c_them_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 38 * N)));
            var c_them_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 39 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_32, SIMDClass.MultiplyLow(c_them_32, VectorT.LoadAligned(theirWeights + 32 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_33, SIMDClass.MultiplyLow(c_them_33, VectorT.LoadAligned(theirWeights + 33 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_34, SIMDClass.MultiplyLow(c_them_34, VectorT.LoadAligned(theirWeights + 34 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_35, SIMDClass.MultiplyLow(c_them_35, VectorT.LoadAligned(theirWeights + 35 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_36, SIMDClass.MultiplyLow(c_them_36, VectorT.LoadAligned(theirWeights + 36 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_37, SIMDClass.MultiplyLow(c_them_37, VectorT.LoadAligned(theirWeights + 37 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_38, SIMDClass.MultiplyLow(c_them_38, VectorT.LoadAligned(theirWeights + 38 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_39, SIMDClass.MultiplyLow(c_them_39, VectorT.LoadAligned(theirWeights + 39 * N))));

            var c_them_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 40 * N)));
            var c_them_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 41 * N)));
            var c_them_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 42 * N)));
            var c_them_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 43 * N)));
            var c_them_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 44 * N)));
            var c_them_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 45 * N)));
            var c_them_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 46 * N)));
            var c_them_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 47 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_40, SIMDClass.MultiplyLow(c_them_40, VectorT.LoadAligned(theirWeights + 40 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_41, SIMDClass.MultiplyLow(c_them_41, VectorT.LoadAligned(theirWeights + 41 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_42, SIMDClass.MultiplyLow(c_them_42, VectorT.LoadAligned(theirWeights + 42 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_43, SIMDClass.MultiplyLow(c_them_43, VectorT.LoadAligned(theirWeights + 43 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_44, SIMDClass.MultiplyLow(c_them_44, VectorT.LoadAligned(theirWeights + 44 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_45, SIMDClass.MultiplyLow(c_them_45, VectorT.LoadAligned(theirWeights + 45 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_46, SIMDClass.MultiplyLow(c_them_46, VectorT.LoadAligned(theirWeights + 46 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_47, SIMDClass.MultiplyLow(c_them_47, VectorT.LoadAligned(theirWeights + 47 * N))));

            if (StopBefore == AVX512_1536HL)
                goto END;

            var c_them_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 48 * N)));
            var c_them_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 49 * N)));
            var c_them_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 50 * N)));
            var c_them_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 51 * N)));
            var c_them_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 52 * N)));
            var c_them_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 53 * N)));
            var c_them_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 54 * N)));
            var c_them_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 55 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_48, SIMDClass.MultiplyLow(c_them_48, VectorT.LoadAligned(theirWeights + 48 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_49, SIMDClass.MultiplyLow(c_them_49, VectorT.LoadAligned(theirWeights + 49 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_50, SIMDClass.MultiplyLow(c_them_50, VectorT.LoadAligned(theirWeights + 50 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_51, SIMDClass.MultiplyLow(c_them_51, VectorT.LoadAligned(theirWeights + 51 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_52, SIMDClass.MultiplyLow(c_them_52, VectorT.LoadAligned(theirWeights + 52 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_53, SIMDClass.MultiplyLow(c_them_53, VectorT.LoadAligned(theirWeights + 53 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_54, SIMDClass.MultiplyLow(c_them_54, VectorT.LoadAligned(theirWeights + 54 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_55, SIMDClass.MultiplyLow(c_them_55, VectorT.LoadAligned(theirWeights + 55 * N))));

            var c_them_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 56 * N)));
            var c_them_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 57 * N)));
            var c_them_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 58 * N)));
            var c_them_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 59 * N)));
            var c_them_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 60 * N)));
            var c_them_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 61 * N)));
            var c_them_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 62 * N)));
            var c_them_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 63 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_56, SIMDClass.MultiplyLow(c_them_56, VectorT.LoadAligned(theirWeights + 56 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_57, SIMDClass.MultiplyLow(c_them_57, VectorT.LoadAligned(theirWeights + 57 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_58, SIMDClass.MultiplyLow(c_them_58, VectorT.LoadAligned(theirWeights + 58 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_59, SIMDClass.MultiplyLow(c_them_59, VectorT.LoadAligned(theirWeights + 59 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_60, SIMDClass.MultiplyLow(c_them_60, VectorT.LoadAligned(theirWeights + 60 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_61, SIMDClass.MultiplyLow(c_them_61, VectorT.LoadAligned(theirWeights + 61 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_62, SIMDClass.MultiplyLow(c_them_62, VectorT.LoadAligned(theirWeights + 62 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_63, SIMDClass.MultiplyLow(c_them_63, VectorT.LoadAligned(theirWeights + 63 * N))));

            if (StopBefore == AVX256_1024HL)
                goto END;

            var c_them_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 64 * N)));
            var c_them_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 65 * N)));
            var c_them_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 66 * N)));
            var c_them_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 67 * N)));
            var c_them_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 68 * N)));
            var c_them_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 69 * N)));
            var c_them_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 70 * N)));
            var c_them_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 71 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_64, SIMDClass.MultiplyLow(c_them_64, VectorT.LoadAligned(theirWeights + 64 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_65, SIMDClass.MultiplyLow(c_them_65, VectorT.LoadAligned(theirWeights + 65 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_66, SIMDClass.MultiplyLow(c_them_66, VectorT.LoadAligned(theirWeights + 66 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_67, SIMDClass.MultiplyLow(c_them_67, VectorT.LoadAligned(theirWeights + 67 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_68, SIMDClass.MultiplyLow(c_them_68, VectorT.LoadAligned(theirWeights + 68 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_69, SIMDClass.MultiplyLow(c_them_69, VectorT.LoadAligned(theirWeights + 69 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_70, SIMDClass.MultiplyLow(c_them_70, VectorT.LoadAligned(theirWeights + 70 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_71, SIMDClass.MultiplyLow(c_them_71, VectorT.LoadAligned(theirWeights + 71 * N))));

            var c_them_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 72 * N)));
            var c_them_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 73 * N)));
            var c_them_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 74 * N)));
            var c_them_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 75 * N)));
            var c_them_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 76 * N)));
            var c_them_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 77 * N)));
            var c_them_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 78 * N)));
            var c_them_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 79 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_72, SIMDClass.MultiplyLow(c_them_72, VectorT.LoadAligned(theirWeights + 72 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_73, SIMDClass.MultiplyLow(c_them_73, VectorT.LoadAligned(theirWeights + 73 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_74, SIMDClass.MultiplyLow(c_them_74, VectorT.LoadAligned(theirWeights + 74 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_75, SIMDClass.MultiplyLow(c_them_75, VectorT.LoadAligned(theirWeights + 75 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_76, SIMDClass.MultiplyLow(c_them_76, VectorT.LoadAligned(theirWeights + 76 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_77, SIMDClass.MultiplyLow(c_them_77, VectorT.LoadAligned(theirWeights + 77 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_78, SIMDClass.MultiplyLow(c_them_78, VectorT.LoadAligned(theirWeights + 78 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_79, SIMDClass.MultiplyLow(c_them_79, VectorT.LoadAligned(theirWeights + 79 * N))));

            var c_them_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 80 * N)));
            var c_them_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 81 * N)));
            var c_them_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 82 * N)));
            var c_them_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 83 * N)));
            var c_them_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 84 * N)));
            var c_them_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 85 * N)));
            var c_them_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 86 * N)));
            var c_them_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 87 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_80, SIMDClass.MultiplyLow(c_them_80, VectorT.LoadAligned(theirWeights + 80 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_81, SIMDClass.MultiplyLow(c_them_81, VectorT.LoadAligned(theirWeights + 81 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_82, SIMDClass.MultiplyLow(c_them_82, VectorT.LoadAligned(theirWeights + 82 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_83, SIMDClass.MultiplyLow(c_them_83, VectorT.LoadAligned(theirWeights + 83 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_84, SIMDClass.MultiplyLow(c_them_84, VectorT.LoadAligned(theirWeights + 84 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_85, SIMDClass.MultiplyLow(c_them_85, VectorT.LoadAligned(theirWeights + 85 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_86, SIMDClass.MultiplyLow(c_them_86, VectorT.LoadAligned(theirWeights + 86 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_87, SIMDClass.MultiplyLow(c_them_87, VectorT.LoadAligned(theirWeights + 87 * N))));

            var c_them_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 88 * N)));
            var c_them_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 89 * N)));
            var c_them_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 90 * N)));
            var c_them_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 91 * N)));
            var c_them_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 92 * N)));
            var c_them_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 93 * N)));
            var c_them_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 94 * N)));
            var c_them_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 95 * N)));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_88, SIMDClass.MultiplyLow(c_them_88, VectorT.LoadAligned(theirWeights + 88 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_89, SIMDClass.MultiplyLow(c_them_89, VectorT.LoadAligned(theirWeights + 89 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_90, SIMDClass.MultiplyLow(c_them_90, VectorT.LoadAligned(theirWeights + 90 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_91, SIMDClass.MultiplyLow(c_them_91, VectorT.LoadAligned(theirWeights + 91 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_92, SIMDClass.MultiplyLow(c_them_92, VectorT.LoadAligned(theirWeights + 92 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_93, SIMDClass.MultiplyLow(c_them_93, VectorT.LoadAligned(theirWeights + 93 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_94, SIMDClass.MultiplyLow(c_them_94, VectorT.LoadAligned(theirWeights + 94 * N))));
            sumVec = VectorT.Add(sumVec, SIMDClass.MultiplyAddAdjacent(c_them_95, SIMDClass.MultiplyLow(c_them_95, VectorT.LoadAligned(theirWeights + 95 * N))));

            #endregion



            END:

            int output = NNUE.SumVectorNoHadd(sumVec);

            return (output / QA + LayerBiases[outputBucket]) * 400 / (QA * QB);
        }

    }
}
