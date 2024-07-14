
#if AVX512
using SIMDClass = System.Runtime.Intrinsics.X86.Avx512BW;
using VectorT = System.Runtime.Intrinsics.Vector512;
using VShort = System.Runtime.Intrinsics.Vector512<short>;
using VInt = System.Runtime.Intrinsics.Vector512<int>;
using VFloat = System.Runtime.Intrinsics.Vector512<float>;
#else
using System.Runtime.Intrinsics;

using SIMDClass = System.Runtime.Intrinsics.X86.Avx2;
using VectorT = System.Runtime.Intrinsics.Vector256;
using VInt = System.Runtime.Intrinsics.Vector256<int>;
using VShort = System.Runtime.Intrinsics.Vector256<short>;
using VFloat = System.Runtime.Intrinsics.Vector256<float>;
#endif

#pragma warning disable CS0162 // Unreachable code detected


namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {

#if AVX512
        private const int N = 16;
#else
        private const int N = 8;
#endif

        private const int StopBefore = HiddenSize / N;

        private const int AVX512_1024HL = 1024 / 16;
        private const int AVX512_1536HL = 1536 / 16;

        private const int AVX256_1024HL = 1024 / 8;
        private const int AVX256_1536HL = 1536 / 8;

        public static int GetEvaluationUnrolled512(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var maxVec = VFloat.One;
            var zeroVec = VFloat.Zero;
            var sumVec = VFloat.Zero;

            RefreshAccumulator(pos);

            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = Math.Min((63 - occ) * (32 - occ) / 225, 7);

            var ourData =   (float*)(accumulator[pos.ToMove]);
            var theirData = (float*)(accumulator[Not(pos.ToMove)]);
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
            sumVec += (c_us_0 * c_us_0 * VectorT.LoadAligned(ourWeights + 0 * N));
            sumVec += (c_us_1 * c_us_1 * VectorT.LoadAligned(ourWeights + 1 * N));
            sumVec += (c_us_2 * c_us_2 * VectorT.LoadAligned(ourWeights + 2 * N));
            sumVec += (c_us_3 * c_us_3 * VectorT.LoadAligned(ourWeights + 3 * N));
            sumVec += (c_us_4 * c_us_4 * VectorT.LoadAligned(ourWeights + 4 * N));
            sumVec += (c_us_5 * c_us_5 * VectorT.LoadAligned(ourWeights + 5 * N));
            sumVec += (c_us_6 * c_us_6 * VectorT.LoadAligned(ourWeights + 6 * N));
            sumVec += (c_us_7 * c_us_7 * VectorT.LoadAligned(ourWeights + 7 * N));

            var c_us_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 8 * N)));
            var c_us_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 9 * N)));
            var c_us_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 10 * N)));
            var c_us_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 11 * N)));
            var c_us_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 12 * N)));
            var c_us_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 13 * N)));
            var c_us_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 14 * N)));
            var c_us_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 15 * N)));
            sumVec += (c_us_8 * c_us_8 * VectorT.LoadAligned(ourWeights + 8 * N));
            sumVec += (c_us_9 * c_us_9 * VectorT.LoadAligned(ourWeights + 9 * N));
            sumVec += (c_us_10 * c_us_10 * VectorT.LoadAligned(ourWeights + 10 * N));
            sumVec += (c_us_11 * c_us_11 * VectorT.LoadAligned(ourWeights + 11 * N));
            sumVec += (c_us_12 * c_us_12 * VectorT.LoadAligned(ourWeights + 12 * N));
            sumVec += (c_us_13 * c_us_13 * VectorT.LoadAligned(ourWeights + 13 * N));
            sumVec += (c_us_14 * c_us_14 * VectorT.LoadAligned(ourWeights + 14 * N));
            sumVec += (c_us_15 * c_us_15 * VectorT.LoadAligned(ourWeights + 15 * N));

            var c_us_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 16 * N)));
            var c_us_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 17 * N)));
            var c_us_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 18 * N)));
            var c_us_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 19 * N)));
            var c_us_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 20 * N)));
            var c_us_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 21 * N)));
            var c_us_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 22 * N)));
            var c_us_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 23 * N)));
            sumVec += (c_us_16 * c_us_16 * VectorT.LoadAligned(ourWeights + 16 * N));
            sumVec += (c_us_17 * c_us_17 * VectorT.LoadAligned(ourWeights + 17 * N));
            sumVec += (c_us_18 * c_us_18 * VectorT.LoadAligned(ourWeights + 18 * N));
            sumVec += (c_us_19 * c_us_19 * VectorT.LoadAligned(ourWeights + 19 * N));
            sumVec += (c_us_20 * c_us_20 * VectorT.LoadAligned(ourWeights + 20 * N));
            sumVec += (c_us_21 * c_us_21 * VectorT.LoadAligned(ourWeights + 21 * N));
            sumVec += (c_us_22 * c_us_22 * VectorT.LoadAligned(ourWeights + 22 * N));
            sumVec += (c_us_23 * c_us_23 * VectorT.LoadAligned(ourWeights + 23 * N));

            var c_us_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 24 * N)));
            var c_us_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 25 * N)));
            var c_us_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 26 * N)));
            var c_us_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 27 * N)));
            var c_us_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 28 * N)));
            var c_us_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 29 * N)));
            var c_us_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 30 * N)));
            var c_us_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 31 * N)));
            sumVec += (c_us_24 * c_us_24 * VectorT.LoadAligned(ourWeights + 24 * N));
            sumVec += (c_us_25 * c_us_25 * VectorT.LoadAligned(ourWeights + 25 * N));
            sumVec += (c_us_26 * c_us_26 * VectorT.LoadAligned(ourWeights + 26 * N));
            sumVec += (c_us_27 * c_us_27 * VectorT.LoadAligned(ourWeights + 27 * N));
            sumVec += (c_us_28 * c_us_28 * VectorT.LoadAligned(ourWeights + 28 * N));
            sumVec += (c_us_29 * c_us_29 * VectorT.LoadAligned(ourWeights + 29 * N));
            sumVec += (c_us_30 * c_us_30 * VectorT.LoadAligned(ourWeights + 30 * N));
            sumVec += (c_us_31 * c_us_31 * VectorT.LoadAligned(ourWeights + 31 * N));

            var c_us_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 32 * N)));
            var c_us_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 33 * N)));
            var c_us_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 34 * N)));
            var c_us_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 35 * N)));
            var c_us_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 36 * N)));
            var c_us_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 37 * N)));
            var c_us_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 38 * N)));
            var c_us_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 39 * N)));
            sumVec += (c_us_32 * c_us_32 * VectorT.LoadAligned(ourWeights + 32 * N));
            sumVec += (c_us_33 * c_us_33 * VectorT.LoadAligned(ourWeights + 33 * N));
            sumVec += (c_us_34 * c_us_34 * VectorT.LoadAligned(ourWeights + 34 * N));
            sumVec += (c_us_35 * c_us_35 * VectorT.LoadAligned(ourWeights + 35 * N));
            sumVec += (c_us_36 * c_us_36 * VectorT.LoadAligned(ourWeights + 36 * N));
            sumVec += (c_us_37 * c_us_37 * VectorT.LoadAligned(ourWeights + 37 * N));
            sumVec += (c_us_38 * c_us_38 * VectorT.LoadAligned(ourWeights + 38 * N));
            sumVec += (c_us_39 * c_us_39 * VectorT.LoadAligned(ourWeights + 39 * N));

            var c_us_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 40 * N)));
            var c_us_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 41 * N)));
            var c_us_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 42 * N)));
            var c_us_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 43 * N)));
            var c_us_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 44 * N)));
            var c_us_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 45 * N)));
            var c_us_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 46 * N)));
            var c_us_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 47 * N)));
            sumVec += (c_us_40 * c_us_40 * VectorT.LoadAligned(ourWeights + 40 * N));
            sumVec += (c_us_41 * c_us_41 * VectorT.LoadAligned(ourWeights + 41 * N));
            sumVec += (c_us_42 * c_us_42 * VectorT.LoadAligned(ourWeights + 42 * N));
            sumVec += (c_us_43 * c_us_43 * VectorT.LoadAligned(ourWeights + 43 * N));
            sumVec += (c_us_44 * c_us_44 * VectorT.LoadAligned(ourWeights + 44 * N));
            sumVec += (c_us_45 * c_us_45 * VectorT.LoadAligned(ourWeights + 45 * N));
            sumVec += (c_us_46 * c_us_46 * VectorT.LoadAligned(ourWeights + 46 * N));
            sumVec += (c_us_47 * c_us_47 * VectorT.LoadAligned(ourWeights + 47 * N));

            var c_us_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 48 * N)));
            var c_us_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 49 * N)));
            var c_us_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 50 * N)));
            var c_us_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 51 * N)));
            var c_us_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 52 * N)));
            var c_us_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 53 * N)));
            var c_us_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 54 * N)));
            var c_us_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 55 * N)));
            sumVec += (c_us_48 * c_us_48 * VectorT.LoadAligned(ourWeights + 48 * N));
            sumVec += (c_us_49 * c_us_49 * VectorT.LoadAligned(ourWeights + 49 * N));
            sumVec += (c_us_50 * c_us_50 * VectorT.LoadAligned(ourWeights + 50 * N));
            sumVec += (c_us_51 * c_us_51 * VectorT.LoadAligned(ourWeights + 51 * N));
            sumVec += (c_us_52 * c_us_52 * VectorT.LoadAligned(ourWeights + 52 * N));
            sumVec += (c_us_53 * c_us_53 * VectorT.LoadAligned(ourWeights + 53 * N));
            sumVec += (c_us_54 * c_us_54 * VectorT.LoadAligned(ourWeights + 54 * N));
            sumVec += (c_us_55 * c_us_55 * VectorT.LoadAligned(ourWeights + 55 * N));

            var c_us_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 56 * N)));
            var c_us_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 57 * N)));
            var c_us_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 58 * N)));
            var c_us_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 59 * N)));
            var c_us_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 60 * N)));
            var c_us_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 61 * N)));
            var c_us_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 62 * N)));
            var c_us_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 63 * N)));
            sumVec += (c_us_56 * c_us_56 * VectorT.LoadAligned(ourWeights + 56 * N));
            sumVec += (c_us_57 * c_us_57 * VectorT.LoadAligned(ourWeights + 57 * N));
            sumVec += (c_us_58 * c_us_58 * VectorT.LoadAligned(ourWeights + 58 * N));
            sumVec += (c_us_59 * c_us_59 * VectorT.LoadAligned(ourWeights + 59 * N));
            sumVec += (c_us_60 * c_us_60 * VectorT.LoadAligned(ourWeights + 60 * N));
            sumVec += (c_us_61 * c_us_61 * VectorT.LoadAligned(ourWeights + 61 * N));
            sumVec += (c_us_62 * c_us_62 * VectorT.LoadAligned(ourWeights + 62 * N));
            sumVec += (c_us_63 * c_us_63 * VectorT.LoadAligned(ourWeights + 63 * N));

            var c_us_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 64 * N)));
            var c_us_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 65 * N)));
            var c_us_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 66 * N)));
            var c_us_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 67 * N)));
            var c_us_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 68 * N)));
            var c_us_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 69 * N)));
            var c_us_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 70 * N)));
            var c_us_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 71 * N)));
            sumVec += (c_us_64 * c_us_64 * VectorT.LoadAligned(ourWeights + 64 * N));
            sumVec += (c_us_65 * c_us_65 * VectorT.LoadAligned(ourWeights + 65 * N));
            sumVec += (c_us_66 * c_us_66 * VectorT.LoadAligned(ourWeights + 66 * N));
            sumVec += (c_us_67 * c_us_67 * VectorT.LoadAligned(ourWeights + 67 * N));
            sumVec += (c_us_68 * c_us_68 * VectorT.LoadAligned(ourWeights + 68 * N));
            sumVec += (c_us_69 * c_us_69 * VectorT.LoadAligned(ourWeights + 69 * N));
            sumVec += (c_us_70 * c_us_70 * VectorT.LoadAligned(ourWeights + 70 * N));
            sumVec += (c_us_71 * c_us_71 * VectorT.LoadAligned(ourWeights + 71 * N));

            var c_us_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 72 * N)));
            var c_us_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 73 * N)));
            var c_us_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 74 * N)));
            var c_us_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 75 * N)));
            var c_us_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 76 * N)));
            var c_us_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 77 * N)));
            var c_us_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 78 * N)));
            var c_us_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 79 * N)));
            sumVec += (c_us_72 * c_us_72 * VectorT.LoadAligned(ourWeights + 72 * N));
            sumVec += (c_us_73 * c_us_73 * VectorT.LoadAligned(ourWeights + 73 * N));
            sumVec += (c_us_74 * c_us_74 * VectorT.LoadAligned(ourWeights + 74 * N));
            sumVec += (c_us_75 * c_us_75 * VectorT.LoadAligned(ourWeights + 75 * N));
            sumVec += (c_us_76 * c_us_76 * VectorT.LoadAligned(ourWeights + 76 * N));
            sumVec += (c_us_77 * c_us_77 * VectorT.LoadAligned(ourWeights + 77 * N));
            sumVec += (c_us_78 * c_us_78 * VectorT.LoadAligned(ourWeights + 78 * N));
            sumVec += (c_us_79 * c_us_79 * VectorT.LoadAligned(ourWeights + 79 * N));

            var c_us_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 80 * N)));
            var c_us_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 81 * N)));
            var c_us_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 82 * N)));
            var c_us_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 83 * N)));
            var c_us_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 84 * N)));
            var c_us_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 85 * N)));
            var c_us_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 86 * N)));
            var c_us_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 87 * N)));
            sumVec += (c_us_80 * c_us_80 * VectorT.LoadAligned(ourWeights + 80 * N));
            sumVec += (c_us_81 * c_us_81 * VectorT.LoadAligned(ourWeights + 81 * N));
            sumVec += (c_us_82 * c_us_82 * VectorT.LoadAligned(ourWeights + 82 * N));
            sumVec += (c_us_83 * c_us_83 * VectorT.LoadAligned(ourWeights + 83 * N));
            sumVec += (c_us_84 * c_us_84 * VectorT.LoadAligned(ourWeights + 84 * N));
            sumVec += (c_us_85 * c_us_85 * VectorT.LoadAligned(ourWeights + 85 * N));
            sumVec += (c_us_86 * c_us_86 * VectorT.LoadAligned(ourWeights + 86 * N));
            sumVec += (c_us_87 * c_us_87 * VectorT.LoadAligned(ourWeights + 87 * N));

            var c_us_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 88 * N)));
            var c_us_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 89 * N)));
            var c_us_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 90 * N)));
            var c_us_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 91 * N)));
            var c_us_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 92 * N)));
            var c_us_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 93 * N)));
            var c_us_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 94 * N)));
            var c_us_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 95 * N)));
            sumVec += (c_us_88 * c_us_88 * VectorT.LoadAligned(ourWeights + 88 * N));
            sumVec += (c_us_89 * c_us_89 * VectorT.LoadAligned(ourWeights + 89 * N));
            sumVec += (c_us_90 * c_us_90 * VectorT.LoadAligned(ourWeights + 90 * N));
            sumVec += (c_us_91 * c_us_91 * VectorT.LoadAligned(ourWeights + 91 * N));
            sumVec += (c_us_92 * c_us_92 * VectorT.LoadAligned(ourWeights + 92 * N));
            sumVec += (c_us_93 * c_us_93 * VectorT.LoadAligned(ourWeights + 93 * N));
            sumVec += (c_us_94 * c_us_94 * VectorT.LoadAligned(ourWeights + 94 * N));
            sumVec += (c_us_95 * c_us_95 * VectorT.LoadAligned(ourWeights + 95 * N));

            var c_us_96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 96 * N)));
            var c_us_97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 97 * N)));
            var c_us_98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 98 * N)));
            var c_us_99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 99 * N)));
            var c_us_100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 100 * N)));
            var c_us_101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 101 * N)));
            var c_us_102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 102 * N)));
            var c_us_103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 103 * N)));
            sumVec += (c_us_96 * c_us_96 * VectorT.LoadAligned(ourWeights + 96 * N));
            sumVec += (c_us_97 * c_us_97 * VectorT.LoadAligned(ourWeights + 97 * N));
            sumVec += (c_us_98 * c_us_98 * VectorT.LoadAligned(ourWeights + 98 * N));
            sumVec += (c_us_99 * c_us_99 * VectorT.LoadAligned(ourWeights + 99 * N));
            sumVec += (c_us_100 * c_us_100 * VectorT.LoadAligned(ourWeights + 100 * N));
            sumVec += (c_us_101 * c_us_101 * VectorT.LoadAligned(ourWeights + 101 * N));
            sumVec += (c_us_102 * c_us_102 * VectorT.LoadAligned(ourWeights + 102 * N));
            sumVec += (c_us_103 * c_us_103 * VectorT.LoadAligned(ourWeights + 103 * N));

            var c_us_104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 104 * N)));
            var c_us_105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 105 * N)));
            var c_us_106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 106 * N)));
            var c_us_107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 107 * N)));
            var c_us_108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 108 * N)));
            var c_us_109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 109 * N)));
            var c_us_110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 110 * N)));
            var c_us_111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 111 * N)));
            sumVec += (c_us_104 * c_us_104 * VectorT.LoadAligned(ourWeights + 104 * N));
            sumVec += (c_us_105 * c_us_105 * VectorT.LoadAligned(ourWeights + 105 * N));
            sumVec += (c_us_106 * c_us_106 * VectorT.LoadAligned(ourWeights + 106 * N));
            sumVec += (c_us_107 * c_us_107 * VectorT.LoadAligned(ourWeights + 107 * N));
            sumVec += (c_us_108 * c_us_108 * VectorT.LoadAligned(ourWeights + 108 * N));
            sumVec += (c_us_109 * c_us_109 * VectorT.LoadAligned(ourWeights + 109 * N));
            sumVec += (c_us_110 * c_us_110 * VectorT.LoadAligned(ourWeights + 110 * N));
            sumVec += (c_us_111 * c_us_111 * VectorT.LoadAligned(ourWeights + 111 * N));

            var c_us_112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 112 * N)));
            var c_us_113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 113 * N)));
            var c_us_114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 114 * N)));
            var c_us_115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 115 * N)));
            var c_us_116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 116 * N)));
            var c_us_117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 117 * N)));
            var c_us_118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 118 * N)));
            var c_us_119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 119 * N)));
            sumVec += (c_us_112 * c_us_112 * VectorT.LoadAligned(ourWeights + 112 * N));
            sumVec += (c_us_113 * c_us_113 * VectorT.LoadAligned(ourWeights + 113 * N));
            sumVec += (c_us_114 * c_us_114 * VectorT.LoadAligned(ourWeights + 114 * N));
            sumVec += (c_us_115 * c_us_115 * VectorT.LoadAligned(ourWeights + 115 * N));
            sumVec += (c_us_116 * c_us_116 * VectorT.LoadAligned(ourWeights + 116 * N));
            sumVec += (c_us_117 * c_us_117 * VectorT.LoadAligned(ourWeights + 117 * N));
            sumVec += (c_us_118 * c_us_118 * VectorT.LoadAligned(ourWeights + 118 * N));
            sumVec += (c_us_119 * c_us_119 * VectorT.LoadAligned(ourWeights + 119 * N));

            var c_us_120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 120 * N)));
            var c_us_121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 121 * N)));
            var c_us_122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 122 * N)));
            var c_us_123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 123 * N)));
            var c_us_124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 124 * N)));
            var c_us_125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 125 * N)));
            var c_us_126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 126 * N)));
            var c_us_127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 127 * N)));
            sumVec += (c_us_120 * c_us_120 * VectorT.LoadAligned(ourWeights + 120 * N));
            sumVec += (c_us_121 * c_us_121 * VectorT.LoadAligned(ourWeights + 121 * N));
            sumVec += (c_us_122 * c_us_122 * VectorT.LoadAligned(ourWeights + 122 * N));
            sumVec += (c_us_123 * c_us_123 * VectorT.LoadAligned(ourWeights + 123 * N));
            sumVec += (c_us_124 * c_us_124 * VectorT.LoadAligned(ourWeights + 124 * N));
            sumVec += (c_us_125 * c_us_125 * VectorT.LoadAligned(ourWeights + 125 * N));
            sumVec += (c_us_126 * c_us_126 * VectorT.LoadAligned(ourWeights + 126 * N));
            sumVec += (c_us_127 * c_us_127 * VectorT.LoadAligned(ourWeights + 127 * N));

            var c_us_128 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 128 * N)));
            var c_us_129 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 129 * N)));
            var c_us_130 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 130 * N)));
            var c_us_131 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 131 * N)));
            var c_us_132 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 132 * N)));
            var c_us_133 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 133 * N)));
            var c_us_134 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 134 * N)));
            var c_us_135 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 135 * N)));
            sumVec += (c_us_128 * c_us_128 * VectorT.LoadAligned(ourWeights + 128 * N));
            sumVec += (c_us_129 * c_us_129 * VectorT.LoadAligned(ourWeights + 129 * N));
            sumVec += (c_us_130 * c_us_130 * VectorT.LoadAligned(ourWeights + 130 * N));
            sumVec += (c_us_131 * c_us_131 * VectorT.LoadAligned(ourWeights + 131 * N));
            sumVec += (c_us_132 * c_us_132 * VectorT.LoadAligned(ourWeights + 132 * N));
            sumVec += (c_us_133 * c_us_133 * VectorT.LoadAligned(ourWeights + 133 * N));
            sumVec += (c_us_134 * c_us_134 * VectorT.LoadAligned(ourWeights + 134 * N));
            sumVec += (c_us_135 * c_us_135 * VectorT.LoadAligned(ourWeights + 135 * N));

            var c_us_136 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 136 * N)));
            var c_us_137 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 137 * N)));
            var c_us_138 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 138 * N)));
            var c_us_139 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 139 * N)));
            var c_us_140 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 140 * N)));
            var c_us_141 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 141 * N)));
            var c_us_142 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 142 * N)));
            var c_us_143 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 143 * N)));
            sumVec += (c_us_136 * c_us_136 * VectorT.LoadAligned(ourWeights + 136 * N));
            sumVec += (c_us_137 * c_us_137 * VectorT.LoadAligned(ourWeights + 137 * N));
            sumVec += (c_us_138 * c_us_138 * VectorT.LoadAligned(ourWeights + 138 * N));
            sumVec += (c_us_139 * c_us_139 * VectorT.LoadAligned(ourWeights + 139 * N));
            sumVec += (c_us_140 * c_us_140 * VectorT.LoadAligned(ourWeights + 140 * N));
            sumVec += (c_us_141 * c_us_141 * VectorT.LoadAligned(ourWeights + 141 * N));
            sumVec += (c_us_142 * c_us_142 * VectorT.LoadAligned(ourWeights + 142 * N));
            sumVec += (c_us_143 * c_us_143 * VectorT.LoadAligned(ourWeights + 143 * N));

            var c_us_144 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 144 * N)));
            var c_us_145 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 145 * N)));
            var c_us_146 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 146 * N)));
            var c_us_147 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 147 * N)));
            var c_us_148 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 148 * N)));
            var c_us_149 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 149 * N)));
            var c_us_150 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 150 * N)));
            var c_us_151 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 151 * N)));
            sumVec += (c_us_144 * c_us_144 * VectorT.LoadAligned(ourWeights + 144 * N));
            sumVec += (c_us_145 * c_us_145 * VectorT.LoadAligned(ourWeights + 145 * N));
            sumVec += (c_us_146 * c_us_146 * VectorT.LoadAligned(ourWeights + 146 * N));
            sumVec += (c_us_147 * c_us_147 * VectorT.LoadAligned(ourWeights + 147 * N));
            sumVec += (c_us_148 * c_us_148 * VectorT.LoadAligned(ourWeights + 148 * N));
            sumVec += (c_us_149 * c_us_149 * VectorT.LoadAligned(ourWeights + 149 * N));
            sumVec += (c_us_150 * c_us_150 * VectorT.LoadAligned(ourWeights + 150 * N));
            sumVec += (c_us_151 * c_us_151 * VectorT.LoadAligned(ourWeights + 151 * N));

            var c_us_152 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 152 * N)));
            var c_us_153 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 153 * N)));
            var c_us_154 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 154 * N)));
            var c_us_155 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 155 * N)));
            var c_us_156 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 156 * N)));
            var c_us_157 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 157 * N)));
            var c_us_158 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 158 * N)));
            var c_us_159 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 159 * N)));
            sumVec += (c_us_152 * c_us_152 * VectorT.LoadAligned(ourWeights + 152 * N));
            sumVec += (c_us_153 * c_us_153 * VectorT.LoadAligned(ourWeights + 153 * N));
            sumVec += (c_us_154 * c_us_154 * VectorT.LoadAligned(ourWeights + 154 * N));
            sumVec += (c_us_155 * c_us_155 * VectorT.LoadAligned(ourWeights + 155 * N));
            sumVec += (c_us_156 * c_us_156 * VectorT.LoadAligned(ourWeights + 156 * N));
            sumVec += (c_us_157 * c_us_157 * VectorT.LoadAligned(ourWeights + 157 * N));
            sumVec += (c_us_158 * c_us_158 * VectorT.LoadAligned(ourWeights + 158 * N));
            sumVec += (c_us_159 * c_us_159 * VectorT.LoadAligned(ourWeights + 159 * N));

            var c_us_160 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 160 * N)));
            var c_us_161 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 161 * N)));
            var c_us_162 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 162 * N)));
            var c_us_163 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 163 * N)));
            var c_us_164 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 164 * N)));
            var c_us_165 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 165 * N)));
            var c_us_166 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 166 * N)));
            var c_us_167 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 167 * N)));
            sumVec += (c_us_160 * c_us_160 * VectorT.LoadAligned(ourWeights + 160 * N));
            sumVec += (c_us_161 * c_us_161 * VectorT.LoadAligned(ourWeights + 161 * N));
            sumVec += (c_us_162 * c_us_162 * VectorT.LoadAligned(ourWeights + 162 * N));
            sumVec += (c_us_163 * c_us_163 * VectorT.LoadAligned(ourWeights + 163 * N));
            sumVec += (c_us_164 * c_us_164 * VectorT.LoadAligned(ourWeights + 164 * N));
            sumVec += (c_us_165 * c_us_165 * VectorT.LoadAligned(ourWeights + 165 * N));
            sumVec += (c_us_166 * c_us_166 * VectorT.LoadAligned(ourWeights + 166 * N));
            sumVec += (c_us_167 * c_us_167 * VectorT.LoadAligned(ourWeights + 167 * N));

            var c_us_168 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 168 * N)));
            var c_us_169 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 169 * N)));
            var c_us_170 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 170 * N)));
            var c_us_171 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 171 * N)));
            var c_us_172 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 172 * N)));
            var c_us_173 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 173 * N)));
            var c_us_174 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 174 * N)));
            var c_us_175 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 175 * N)));
            sumVec += (c_us_168 * c_us_168 * VectorT.LoadAligned(ourWeights + 168 * N));
            sumVec += (c_us_169 * c_us_169 * VectorT.LoadAligned(ourWeights + 169 * N));
            sumVec += (c_us_170 * c_us_170 * VectorT.LoadAligned(ourWeights + 170 * N));
            sumVec += (c_us_171 * c_us_171 * VectorT.LoadAligned(ourWeights + 171 * N));
            sumVec += (c_us_172 * c_us_172 * VectorT.LoadAligned(ourWeights + 172 * N));
            sumVec += (c_us_173 * c_us_173 * VectorT.LoadAligned(ourWeights + 173 * N));
            sumVec += (c_us_174 * c_us_174 * VectorT.LoadAligned(ourWeights + 174 * N));
            sumVec += (c_us_175 * c_us_175 * VectorT.LoadAligned(ourWeights + 175 * N));

            var c_us_176 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 176 * N)));
            var c_us_177 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 177 * N)));
            var c_us_178 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 178 * N)));
            var c_us_179 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 179 * N)));
            var c_us_180 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 180 * N)));
            var c_us_181 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 181 * N)));
            var c_us_182 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 182 * N)));
            var c_us_183 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 183 * N)));
            sumVec += (c_us_176 * c_us_176 * VectorT.LoadAligned(ourWeights + 176 * N));
            sumVec += (c_us_177 * c_us_177 * VectorT.LoadAligned(ourWeights + 177 * N));
            sumVec += (c_us_178 * c_us_178 * VectorT.LoadAligned(ourWeights + 178 * N));
            sumVec += (c_us_179 * c_us_179 * VectorT.LoadAligned(ourWeights + 179 * N));
            sumVec += (c_us_180 * c_us_180 * VectorT.LoadAligned(ourWeights + 180 * N));
            sumVec += (c_us_181 * c_us_181 * VectorT.LoadAligned(ourWeights + 181 * N));
            sumVec += (c_us_182 * c_us_182 * VectorT.LoadAligned(ourWeights + 182 * N));
            sumVec += (c_us_183 * c_us_183 * VectorT.LoadAligned(ourWeights + 183 * N));

            var c_us_184 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 184 * N)));
            var c_us_185 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 185 * N)));
            var c_us_186 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 186 * N)));
            var c_us_187 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 187 * N)));
            var c_us_188 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 188 * N)));
            var c_us_189 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 189 * N)));
            var c_us_190 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 190 * N)));
            var c_us_191 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(ourData + 191 * N)));
            sumVec += (c_us_184 * c_us_184 * VectorT.LoadAligned(ourWeights + 184 * N));
            sumVec += (c_us_185 * c_us_185 * VectorT.LoadAligned(ourWeights + 185 * N));
            sumVec += (c_us_186 * c_us_186 * VectorT.LoadAligned(ourWeights + 186 * N));
            sumVec += (c_us_187 * c_us_187 * VectorT.LoadAligned(ourWeights + 187 * N));
            sumVec += (c_us_188 * c_us_188 * VectorT.LoadAligned(ourWeights + 188 * N));
            sumVec += (c_us_189 * c_us_189 * VectorT.LoadAligned(ourWeights + 189 * N));
            sumVec += (c_us_190 * c_us_190 * VectorT.LoadAligned(ourWeights + 190 * N));
            sumVec += (c_us_191 * c_us_191 * VectorT.LoadAligned(ourWeights + 191 * N));

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
            sumVec += (c_them_0 * c_them_0 * VectorT.LoadAligned(theirWeights + 0 * N));
            sumVec += (c_them_1 * c_them_1 * VectorT.LoadAligned(theirWeights + 1 * N));
            sumVec += (c_them_2 * c_them_2 * VectorT.LoadAligned(theirWeights + 2 * N));
            sumVec += (c_them_3 * c_them_3 * VectorT.LoadAligned(theirWeights + 3 * N));
            sumVec += (c_them_4 * c_them_4 * VectorT.LoadAligned(theirWeights + 4 * N));
            sumVec += (c_them_5 * c_them_5 * VectorT.LoadAligned(theirWeights + 5 * N));
            sumVec += (c_them_6 * c_them_6 * VectorT.LoadAligned(theirWeights + 6 * N));
            sumVec += (c_them_7 * c_them_7 * VectorT.LoadAligned(theirWeights + 7 * N));

            var c_them_8 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 8 * N)));
            var c_them_9 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 9 * N)));
            var c_them_10 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 10 * N)));
            var c_them_11 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 11 * N)));
            var c_them_12 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 12 * N)));
            var c_them_13 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 13 * N)));
            var c_them_14 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 14 * N)));
            var c_them_15 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 15 * N)));
            sumVec += (c_them_8 * c_them_8 * VectorT.LoadAligned(theirWeights + 8 * N));
            sumVec += (c_them_9 * c_them_9 * VectorT.LoadAligned(theirWeights + 9 * N));
            sumVec += (c_them_10 * c_them_10 * VectorT.LoadAligned(theirWeights + 10 * N));
            sumVec += (c_them_11 * c_them_11 * VectorT.LoadAligned(theirWeights + 11 * N));
            sumVec += (c_them_12 * c_them_12 * VectorT.LoadAligned(theirWeights + 12 * N));
            sumVec += (c_them_13 * c_them_13 * VectorT.LoadAligned(theirWeights + 13 * N));
            sumVec += (c_them_14 * c_them_14 * VectorT.LoadAligned(theirWeights + 14 * N));
            sumVec += (c_them_15 * c_them_15 * VectorT.LoadAligned(theirWeights + 15 * N));

            var c_them_16 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 16 * N)));
            var c_them_17 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 17 * N)));
            var c_them_18 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 18 * N)));
            var c_them_19 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 19 * N)));
            var c_them_20 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 20 * N)));
            var c_them_21 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 21 * N)));
            var c_them_22 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 22 * N)));
            var c_them_23 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 23 * N)));
            sumVec += (c_them_16 * c_them_16 * VectorT.LoadAligned(theirWeights + 16 * N));
            sumVec += (c_them_17 * c_them_17 * VectorT.LoadAligned(theirWeights + 17 * N));
            sumVec += (c_them_18 * c_them_18 * VectorT.LoadAligned(theirWeights + 18 * N));
            sumVec += (c_them_19 * c_them_19 * VectorT.LoadAligned(theirWeights + 19 * N));
            sumVec += (c_them_20 * c_them_20 * VectorT.LoadAligned(theirWeights + 20 * N));
            sumVec += (c_them_21 * c_them_21 * VectorT.LoadAligned(theirWeights + 21 * N));
            sumVec += (c_them_22 * c_them_22 * VectorT.LoadAligned(theirWeights + 22 * N));
            sumVec += (c_them_23 * c_them_23 * VectorT.LoadAligned(theirWeights + 23 * N));

            var c_them_24 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 24 * N)));
            var c_them_25 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 25 * N)));
            var c_them_26 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 26 * N)));
            var c_them_27 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 27 * N)));
            var c_them_28 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 28 * N)));
            var c_them_29 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 29 * N)));
            var c_them_30 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 30 * N)));
            var c_them_31 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 31 * N)));
            sumVec += (c_them_24 * c_them_24 * VectorT.LoadAligned(theirWeights + 24 * N));
            sumVec += (c_them_25 * c_them_25 * VectorT.LoadAligned(theirWeights + 25 * N));
            sumVec += (c_them_26 * c_them_26 * VectorT.LoadAligned(theirWeights + 26 * N));
            sumVec += (c_them_27 * c_them_27 * VectorT.LoadAligned(theirWeights + 27 * N));
            sumVec += (c_them_28 * c_them_28 * VectorT.LoadAligned(theirWeights + 28 * N));
            sumVec += (c_them_29 * c_them_29 * VectorT.LoadAligned(theirWeights + 29 * N));
            sumVec += (c_them_30 * c_them_30 * VectorT.LoadAligned(theirWeights + 30 * N));
            sumVec += (c_them_31 * c_them_31 * VectorT.LoadAligned(theirWeights + 31 * N));

            var c_them_32 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 32 * N)));
            var c_them_33 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 33 * N)));
            var c_them_34 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 34 * N)));
            var c_them_35 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 35 * N)));
            var c_them_36 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 36 * N)));
            var c_them_37 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 37 * N)));
            var c_them_38 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 38 * N)));
            var c_them_39 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 39 * N)));
            sumVec += (c_them_32 * c_them_32 * VectorT.LoadAligned(theirWeights + 32 * N));
            sumVec += (c_them_33 * c_them_33 * VectorT.LoadAligned(theirWeights + 33 * N));
            sumVec += (c_them_34 * c_them_34 * VectorT.LoadAligned(theirWeights + 34 * N));
            sumVec += (c_them_35 * c_them_35 * VectorT.LoadAligned(theirWeights + 35 * N));
            sumVec += (c_them_36 * c_them_36 * VectorT.LoadAligned(theirWeights + 36 * N));
            sumVec += (c_them_37 * c_them_37 * VectorT.LoadAligned(theirWeights + 37 * N));
            sumVec += (c_them_38 * c_them_38 * VectorT.LoadAligned(theirWeights + 38 * N));
            sumVec += (c_them_39 * c_them_39 * VectorT.LoadAligned(theirWeights + 39 * N));

            var c_them_40 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 40 * N)));
            var c_them_41 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 41 * N)));
            var c_them_42 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 42 * N)));
            var c_them_43 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 43 * N)));
            var c_them_44 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 44 * N)));
            var c_them_45 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 45 * N)));
            var c_them_46 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 46 * N)));
            var c_them_47 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 47 * N)));
            sumVec += (c_them_40 * c_them_40 * VectorT.LoadAligned(theirWeights + 40 * N));
            sumVec += (c_them_41 * c_them_41 * VectorT.LoadAligned(theirWeights + 41 * N));
            sumVec += (c_them_42 * c_them_42 * VectorT.LoadAligned(theirWeights + 42 * N));
            sumVec += (c_them_43 * c_them_43 * VectorT.LoadAligned(theirWeights + 43 * N));
            sumVec += (c_them_44 * c_them_44 * VectorT.LoadAligned(theirWeights + 44 * N));
            sumVec += (c_them_45 * c_them_45 * VectorT.LoadAligned(theirWeights + 45 * N));
            sumVec += (c_them_46 * c_them_46 * VectorT.LoadAligned(theirWeights + 46 * N));
            sumVec += (c_them_47 * c_them_47 * VectorT.LoadAligned(theirWeights + 47 * N));

            var c_them_48 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 48 * N)));
            var c_them_49 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 49 * N)));
            var c_them_50 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 50 * N)));
            var c_them_51 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 51 * N)));
            var c_them_52 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 52 * N)));
            var c_them_53 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 53 * N)));
            var c_them_54 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 54 * N)));
            var c_them_55 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 55 * N)));
            sumVec += (c_them_48 * c_them_48 * VectorT.LoadAligned(theirWeights + 48 * N));
            sumVec += (c_them_49 * c_them_49 * VectorT.LoadAligned(theirWeights + 49 * N));
            sumVec += (c_them_50 * c_them_50 * VectorT.LoadAligned(theirWeights + 50 * N));
            sumVec += (c_them_51 * c_them_51 * VectorT.LoadAligned(theirWeights + 51 * N));
            sumVec += (c_them_52 * c_them_52 * VectorT.LoadAligned(theirWeights + 52 * N));
            sumVec += (c_them_53 * c_them_53 * VectorT.LoadAligned(theirWeights + 53 * N));
            sumVec += (c_them_54 * c_them_54 * VectorT.LoadAligned(theirWeights + 54 * N));
            sumVec += (c_them_55 * c_them_55 * VectorT.LoadAligned(theirWeights + 55 * N));

            var c_them_56 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 56 * N)));
            var c_them_57 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 57 * N)));
            var c_them_58 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 58 * N)));
            var c_them_59 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 59 * N)));
            var c_them_60 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 60 * N)));
            var c_them_61 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 61 * N)));
            var c_them_62 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 62 * N)));
            var c_them_63 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 63 * N)));
            sumVec += (c_them_56 * c_them_56 * VectorT.LoadAligned(theirWeights + 56 * N));
            sumVec += (c_them_57 * c_them_57 * VectorT.LoadAligned(theirWeights + 57 * N));
            sumVec += (c_them_58 * c_them_58 * VectorT.LoadAligned(theirWeights + 58 * N));
            sumVec += (c_them_59 * c_them_59 * VectorT.LoadAligned(theirWeights + 59 * N));
            sumVec += (c_them_60 * c_them_60 * VectorT.LoadAligned(theirWeights + 60 * N));
            sumVec += (c_them_61 * c_them_61 * VectorT.LoadAligned(theirWeights + 61 * N));
            sumVec += (c_them_62 * c_them_62 * VectorT.LoadAligned(theirWeights + 62 * N));
            sumVec += (c_them_63 * c_them_63 * VectorT.LoadAligned(theirWeights + 63 * N));

            var c_them_64 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 64 * N)));
            var c_them_65 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 65 * N)));
            var c_them_66 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 66 * N)));
            var c_them_67 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 67 * N)));
            var c_them_68 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 68 * N)));
            var c_them_69 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 69 * N)));
            var c_them_70 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 70 * N)));
            var c_them_71 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 71 * N)));
            sumVec += (c_them_64 * c_them_64 * VectorT.LoadAligned(theirWeights + 64 * N));
            sumVec += (c_them_65 * c_them_65 * VectorT.LoadAligned(theirWeights + 65 * N));
            sumVec += (c_them_66 * c_them_66 * VectorT.LoadAligned(theirWeights + 66 * N));
            sumVec += (c_them_67 * c_them_67 * VectorT.LoadAligned(theirWeights + 67 * N));
            sumVec += (c_them_68 * c_them_68 * VectorT.LoadAligned(theirWeights + 68 * N));
            sumVec += (c_them_69 * c_them_69 * VectorT.LoadAligned(theirWeights + 69 * N));
            sumVec += (c_them_70 * c_them_70 * VectorT.LoadAligned(theirWeights + 70 * N));
            sumVec += (c_them_71 * c_them_71 * VectorT.LoadAligned(theirWeights + 71 * N));

            var c_them_72 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 72 * N)));
            var c_them_73 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 73 * N)));
            var c_them_74 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 74 * N)));
            var c_them_75 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 75 * N)));
            var c_them_76 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 76 * N)));
            var c_them_77 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 77 * N)));
            var c_them_78 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 78 * N)));
            var c_them_79 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 79 * N)));
            sumVec += (c_them_72 * c_them_72 * VectorT.LoadAligned(theirWeights + 72 * N));
            sumVec += (c_them_73 * c_them_73 * VectorT.LoadAligned(theirWeights + 73 * N));
            sumVec += (c_them_74 * c_them_74 * VectorT.LoadAligned(theirWeights + 74 * N));
            sumVec += (c_them_75 * c_them_75 * VectorT.LoadAligned(theirWeights + 75 * N));
            sumVec += (c_them_76 * c_them_76 * VectorT.LoadAligned(theirWeights + 76 * N));
            sumVec += (c_them_77 * c_them_77 * VectorT.LoadAligned(theirWeights + 77 * N));
            sumVec += (c_them_78 * c_them_78 * VectorT.LoadAligned(theirWeights + 78 * N));
            sumVec += (c_them_79 * c_them_79 * VectorT.LoadAligned(theirWeights + 79 * N));

            var c_them_80 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 80 * N)));
            var c_them_81 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 81 * N)));
            var c_them_82 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 82 * N)));
            var c_them_83 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 83 * N)));
            var c_them_84 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 84 * N)));
            var c_them_85 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 85 * N)));
            var c_them_86 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 86 * N)));
            var c_them_87 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 87 * N)));
            sumVec += (c_them_80 * c_them_80 * VectorT.LoadAligned(theirWeights + 80 * N));
            sumVec += (c_them_81 * c_them_81 * VectorT.LoadAligned(theirWeights + 81 * N));
            sumVec += (c_them_82 * c_them_82 * VectorT.LoadAligned(theirWeights + 82 * N));
            sumVec += (c_them_83 * c_them_83 * VectorT.LoadAligned(theirWeights + 83 * N));
            sumVec += (c_them_84 * c_them_84 * VectorT.LoadAligned(theirWeights + 84 * N));
            sumVec += (c_them_85 * c_them_85 * VectorT.LoadAligned(theirWeights + 85 * N));
            sumVec += (c_them_86 * c_them_86 * VectorT.LoadAligned(theirWeights + 86 * N));
            sumVec += (c_them_87 * c_them_87 * VectorT.LoadAligned(theirWeights + 87 * N));

            var c_them_88 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 88 * N)));
            var c_them_89 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 89 * N)));
            var c_them_90 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 90 * N)));
            var c_them_91 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 91 * N)));
            var c_them_92 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 92 * N)));
            var c_them_93 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 93 * N)));
            var c_them_94 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 94 * N)));
            var c_them_95 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 95 * N)));
            sumVec += (c_them_88 * c_them_88 * VectorT.LoadAligned(theirWeights + 88 * N));
            sumVec += (c_them_89 * c_them_89 * VectorT.LoadAligned(theirWeights + 89 * N));
            sumVec += (c_them_90 * c_them_90 * VectorT.LoadAligned(theirWeights + 90 * N));
            sumVec += (c_them_91 * c_them_91 * VectorT.LoadAligned(theirWeights + 91 * N));
            sumVec += (c_them_92 * c_them_92 * VectorT.LoadAligned(theirWeights + 92 * N));
            sumVec += (c_them_93 * c_them_93 * VectorT.LoadAligned(theirWeights + 93 * N));
            sumVec += (c_them_94 * c_them_94 * VectorT.LoadAligned(theirWeights + 94 * N));
            sumVec += (c_them_95 * c_them_95 * VectorT.LoadAligned(theirWeights + 95 * N));

            var c_them_96 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 96 * N)));
            var c_them_97 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 97 * N)));
            var c_them_98 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 98 * N)));
            var c_them_99 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 99 * N)));
            var c_them_100 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 100 * N)));
            var c_them_101 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 101 * N)));
            var c_them_102 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 102 * N)));
            var c_them_103 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 103 * N)));
            sumVec += (c_them_96 * c_them_96 * VectorT.LoadAligned(theirWeights + 96 * N));
            sumVec += (c_them_97 * c_them_97 * VectorT.LoadAligned(theirWeights + 97 * N));
            sumVec += (c_them_98 * c_them_98 * VectorT.LoadAligned(theirWeights + 98 * N));
            sumVec += (c_them_99 * c_them_99 * VectorT.LoadAligned(theirWeights + 99 * N));
            sumVec += (c_them_100 * c_them_100 * VectorT.LoadAligned(theirWeights + 100 * N));
            sumVec += (c_them_101 * c_them_101 * VectorT.LoadAligned(theirWeights + 101 * N));
            sumVec += (c_them_102 * c_them_102 * VectorT.LoadAligned(theirWeights + 102 * N));
            sumVec += (c_them_103 * c_them_103 * VectorT.LoadAligned(theirWeights + 103 * N));

            var c_them_104 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 104 * N)));
            var c_them_105 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 105 * N)));
            var c_them_106 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 106 * N)));
            var c_them_107 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 107 * N)));
            var c_them_108 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 108 * N)));
            var c_them_109 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 109 * N)));
            var c_them_110 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 110 * N)));
            var c_them_111 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 111 * N)));
            sumVec += (c_them_104 * c_them_104 * VectorT.LoadAligned(theirWeights + 104 * N));
            sumVec += (c_them_105 * c_them_105 * VectorT.LoadAligned(theirWeights + 105 * N));
            sumVec += (c_them_106 * c_them_106 * VectorT.LoadAligned(theirWeights + 106 * N));
            sumVec += (c_them_107 * c_them_107 * VectorT.LoadAligned(theirWeights + 107 * N));
            sumVec += (c_them_108 * c_them_108 * VectorT.LoadAligned(theirWeights + 108 * N));
            sumVec += (c_them_109 * c_them_109 * VectorT.LoadAligned(theirWeights + 109 * N));
            sumVec += (c_them_110 * c_them_110 * VectorT.LoadAligned(theirWeights + 110 * N));
            sumVec += (c_them_111 * c_them_111 * VectorT.LoadAligned(theirWeights + 111 * N));

            var c_them_112 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 112 * N)));
            var c_them_113 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 113 * N)));
            var c_them_114 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 114 * N)));
            var c_them_115 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 115 * N)));
            var c_them_116 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 116 * N)));
            var c_them_117 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 117 * N)));
            var c_them_118 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 118 * N)));
            var c_them_119 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 119 * N)));
            sumVec += (c_them_112 * c_them_112 * VectorT.LoadAligned(theirWeights + 112 * N));
            sumVec += (c_them_113 * c_them_113 * VectorT.LoadAligned(theirWeights + 113 * N));
            sumVec += (c_them_114 * c_them_114 * VectorT.LoadAligned(theirWeights + 114 * N));
            sumVec += (c_them_115 * c_them_115 * VectorT.LoadAligned(theirWeights + 115 * N));
            sumVec += (c_them_116 * c_them_116 * VectorT.LoadAligned(theirWeights + 116 * N));
            sumVec += (c_them_117 * c_them_117 * VectorT.LoadAligned(theirWeights + 117 * N));
            sumVec += (c_them_118 * c_them_118 * VectorT.LoadAligned(theirWeights + 118 * N));
            sumVec += (c_them_119 * c_them_119 * VectorT.LoadAligned(theirWeights + 119 * N));

            var c_them_120 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 120 * N)));
            var c_them_121 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 121 * N)));
            var c_them_122 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 122 * N)));
            var c_them_123 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 123 * N)));
            var c_them_124 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 124 * N)));
            var c_them_125 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 125 * N)));
            var c_them_126 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 126 * N)));
            var c_them_127 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 127 * N)));
            sumVec += (c_them_120 * c_them_120 * VectorT.LoadAligned(theirWeights + 120 * N));
            sumVec += (c_them_121 * c_them_121 * VectorT.LoadAligned(theirWeights + 121 * N));
            sumVec += (c_them_122 * c_them_122 * VectorT.LoadAligned(theirWeights + 122 * N));
            sumVec += (c_them_123 * c_them_123 * VectorT.LoadAligned(theirWeights + 123 * N));
            sumVec += (c_them_124 * c_them_124 * VectorT.LoadAligned(theirWeights + 124 * N));
            sumVec += (c_them_125 * c_them_125 * VectorT.LoadAligned(theirWeights + 125 * N));
            sumVec += (c_them_126 * c_them_126 * VectorT.LoadAligned(theirWeights + 126 * N));
            sumVec += (c_them_127 * c_them_127 * VectorT.LoadAligned(theirWeights + 127 * N));

            var c_them_128 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 128 * N)));
            var c_them_129 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 129 * N)));
            var c_them_130 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 130 * N)));
            var c_them_131 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 131 * N)));
            var c_them_132 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 132 * N)));
            var c_them_133 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 133 * N)));
            var c_them_134 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 134 * N)));
            var c_them_135 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 135 * N)));
            sumVec += (c_them_128 * c_them_128 * VectorT.LoadAligned(theirWeights + 128 * N));
            sumVec += (c_them_129 * c_them_129 * VectorT.LoadAligned(theirWeights + 129 * N));
            sumVec += (c_them_130 * c_them_130 * VectorT.LoadAligned(theirWeights + 130 * N));
            sumVec += (c_them_131 * c_them_131 * VectorT.LoadAligned(theirWeights + 131 * N));
            sumVec += (c_them_132 * c_them_132 * VectorT.LoadAligned(theirWeights + 132 * N));
            sumVec += (c_them_133 * c_them_133 * VectorT.LoadAligned(theirWeights + 133 * N));
            sumVec += (c_them_134 * c_them_134 * VectorT.LoadAligned(theirWeights + 134 * N));
            sumVec += (c_them_135 * c_them_135 * VectorT.LoadAligned(theirWeights + 135 * N));

            var c_them_136 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 136 * N)));
            var c_them_137 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 137 * N)));
            var c_them_138 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 138 * N)));
            var c_them_139 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 139 * N)));
            var c_them_140 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 140 * N)));
            var c_them_141 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 141 * N)));
            var c_them_142 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 142 * N)));
            var c_them_143 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 143 * N)));
            sumVec += (c_them_136 * c_them_136 * VectorT.LoadAligned(theirWeights + 136 * N));
            sumVec += (c_them_137 * c_them_137 * VectorT.LoadAligned(theirWeights + 137 * N));
            sumVec += (c_them_138 * c_them_138 * VectorT.LoadAligned(theirWeights + 138 * N));
            sumVec += (c_them_139 * c_them_139 * VectorT.LoadAligned(theirWeights + 139 * N));
            sumVec += (c_them_140 * c_them_140 * VectorT.LoadAligned(theirWeights + 140 * N));
            sumVec += (c_them_141 * c_them_141 * VectorT.LoadAligned(theirWeights + 141 * N));
            sumVec += (c_them_142 * c_them_142 * VectorT.LoadAligned(theirWeights + 142 * N));
            sumVec += (c_them_143 * c_them_143 * VectorT.LoadAligned(theirWeights + 143 * N));

            var c_them_144 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 144 * N)));
            var c_them_145 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 145 * N)));
            var c_them_146 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 146 * N)));
            var c_them_147 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 147 * N)));
            var c_them_148 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 148 * N)));
            var c_them_149 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 149 * N)));
            var c_them_150 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 150 * N)));
            var c_them_151 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 151 * N)));
            sumVec += (c_them_144 * c_them_144 * VectorT.LoadAligned(theirWeights + 144 * N));
            sumVec += (c_them_145 * c_them_145 * VectorT.LoadAligned(theirWeights + 145 * N));
            sumVec += (c_them_146 * c_them_146 * VectorT.LoadAligned(theirWeights + 146 * N));
            sumVec += (c_them_147 * c_them_147 * VectorT.LoadAligned(theirWeights + 147 * N));
            sumVec += (c_them_148 * c_them_148 * VectorT.LoadAligned(theirWeights + 148 * N));
            sumVec += (c_them_149 * c_them_149 * VectorT.LoadAligned(theirWeights + 149 * N));
            sumVec += (c_them_150 * c_them_150 * VectorT.LoadAligned(theirWeights + 150 * N));
            sumVec += (c_them_151 * c_them_151 * VectorT.LoadAligned(theirWeights + 151 * N));

            var c_them_152 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 152 * N)));
            var c_them_153 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 153 * N)));
            var c_them_154 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 154 * N)));
            var c_them_155 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 155 * N)));
            var c_them_156 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 156 * N)));
            var c_them_157 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 157 * N)));
            var c_them_158 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 158 * N)));
            var c_them_159 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 159 * N)));
            sumVec += (c_them_152 * c_them_152 * VectorT.LoadAligned(theirWeights + 152 * N));
            sumVec += (c_them_153 * c_them_153 * VectorT.LoadAligned(theirWeights + 153 * N));
            sumVec += (c_them_154 * c_them_154 * VectorT.LoadAligned(theirWeights + 154 * N));
            sumVec += (c_them_155 * c_them_155 * VectorT.LoadAligned(theirWeights + 155 * N));
            sumVec += (c_them_156 * c_them_156 * VectorT.LoadAligned(theirWeights + 156 * N));
            sumVec += (c_them_157 * c_them_157 * VectorT.LoadAligned(theirWeights + 157 * N));
            sumVec += (c_them_158 * c_them_158 * VectorT.LoadAligned(theirWeights + 158 * N));
            sumVec += (c_them_159 * c_them_159 * VectorT.LoadAligned(theirWeights + 159 * N));

            var c_them_160 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 160 * N)));
            var c_them_161 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 161 * N)));
            var c_them_162 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 162 * N)));
            var c_them_163 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 163 * N)));
            var c_them_164 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 164 * N)));
            var c_them_165 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 165 * N)));
            var c_them_166 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 166 * N)));
            var c_them_167 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 167 * N)));
            sumVec += (c_them_160 * c_them_160 * VectorT.LoadAligned(theirWeights + 160 * N));
            sumVec += (c_them_161 * c_them_161 * VectorT.LoadAligned(theirWeights + 161 * N));
            sumVec += (c_them_162 * c_them_162 * VectorT.LoadAligned(theirWeights + 162 * N));
            sumVec += (c_them_163 * c_them_163 * VectorT.LoadAligned(theirWeights + 163 * N));
            sumVec += (c_them_164 * c_them_164 * VectorT.LoadAligned(theirWeights + 164 * N));
            sumVec += (c_them_165 * c_them_165 * VectorT.LoadAligned(theirWeights + 165 * N));
            sumVec += (c_them_166 * c_them_166 * VectorT.LoadAligned(theirWeights + 166 * N));
            sumVec += (c_them_167 * c_them_167 * VectorT.LoadAligned(theirWeights + 167 * N));

            var c_them_168 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 168 * N)));
            var c_them_169 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 169 * N)));
            var c_them_170 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 170 * N)));
            var c_them_171 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 171 * N)));
            var c_them_172 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 172 * N)));
            var c_them_173 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 173 * N)));
            var c_them_174 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 174 * N)));
            var c_them_175 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 175 * N)));
            sumVec += (c_them_168 * c_them_168 * VectorT.LoadAligned(theirWeights + 168 * N));
            sumVec += (c_them_169 * c_them_169 * VectorT.LoadAligned(theirWeights + 169 * N));
            sumVec += (c_them_170 * c_them_170 * VectorT.LoadAligned(theirWeights + 170 * N));
            sumVec += (c_them_171 * c_them_171 * VectorT.LoadAligned(theirWeights + 171 * N));
            sumVec += (c_them_172 * c_them_172 * VectorT.LoadAligned(theirWeights + 172 * N));
            sumVec += (c_them_173 * c_them_173 * VectorT.LoadAligned(theirWeights + 173 * N));
            sumVec += (c_them_174 * c_them_174 * VectorT.LoadAligned(theirWeights + 174 * N));
            sumVec += (c_them_175 * c_them_175 * VectorT.LoadAligned(theirWeights + 175 * N));

            var c_them_176 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 176 * N)));
            var c_them_177 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 177 * N)));
            var c_them_178 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 178 * N)));
            var c_them_179 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 179 * N)));
            var c_them_180 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 180 * N)));
            var c_them_181 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 181 * N)));
            var c_them_182 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 182 * N)));
            var c_them_183 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 183 * N)));
            sumVec += (c_them_176 * c_them_176 * VectorT.LoadAligned(theirWeights + 176 * N));
            sumVec += (c_them_177 * c_them_177 * VectorT.LoadAligned(theirWeights + 177 * N));
            sumVec += (c_them_178 * c_them_178 * VectorT.LoadAligned(theirWeights + 178 * N));
            sumVec += (c_them_179 * c_them_179 * VectorT.LoadAligned(theirWeights + 179 * N));
            sumVec += (c_them_180 * c_them_180 * VectorT.LoadAligned(theirWeights + 180 * N));
            sumVec += (c_them_181 * c_them_181 * VectorT.LoadAligned(theirWeights + 181 * N));
            sumVec += (c_them_182 * c_them_182 * VectorT.LoadAligned(theirWeights + 182 * N));
            sumVec += (c_them_183 * c_them_183 * VectorT.LoadAligned(theirWeights + 183 * N));

            var c_them_184 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 184 * N)));
            var c_them_185 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 185 * N)));
            var c_them_186 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 186 * N)));
            var c_them_187 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 187 * N)));
            var c_them_188 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 188 * N)));
            var c_them_189 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 189 * N)));
            var c_them_190 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 190 * N)));
            var c_them_191 = VectorT.Min(maxVec, VectorT.Max(zeroVec, VectorT.LoadAligned(theirData + 191 * N)));
            sumVec += (c_them_184 * c_them_184 * VectorT.LoadAligned(theirWeights + 184 * N));
            sumVec += (c_them_185 * c_them_185 * VectorT.LoadAligned(theirWeights + 185 * N));
            sumVec += (c_them_186 * c_them_186 * VectorT.LoadAligned(theirWeights + 186 * N));
            sumVec += (c_them_187 * c_them_187 * VectorT.LoadAligned(theirWeights + 187 * N));
            sumVec += (c_them_188 * c_them_188 * VectorT.LoadAligned(theirWeights + 188 * N));
            sumVec += (c_them_189 * c_them_189 * VectorT.LoadAligned(theirWeights + 189 * N));
            sumVec += (c_them_190 * c_them_190 * VectorT.LoadAligned(theirWeights + 190 * N));
            sumVec += (c_them_191 * c_them_191 * VectorT.LoadAligned(theirWeights + 191 * N));

            #endregion



            END:

            float output = VectorT.Sum(sumVec);
            return (int)((output + LayerBiases[outputBucket]) * OutputScale);
        }

    }
}

