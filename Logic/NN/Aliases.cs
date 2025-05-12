﻿
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace Lizard.Logic.NN
{
    public static unsafe class Aliases
    {

        public static Vector256<int> _mm256_add_epi32(Vector256<int> a, Vector256<int> b) => Avx2.Add(a, b);
        public static Vector256<int> _mm256_madd_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyAddAdjacent(a, b);
        public static Vector256<int> _mm256_cmpgt_epi32(Vector256<int> a, Vector256<int> b) => Avx2.CompareGreaterThan(a, b);
        public static Vector256<int> _mm256_setzero_epi32() => Vector256<int>.Zero;
        public static Vector256<int> _mm256_set1_epi32(int a) => Vector256.Create(a);

        public static Vector256<short> _mm256_load_si256(short* mem_addr) => Avx.LoadAlignedVector256(mem_addr);
        public static Vector256<short> _mm256_min_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Min(a, b);
        public static Vector256<short> _mm256_max_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Max(a, b);
        public static Vector256<short> _mm256_mulhi_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyHigh(a, b);
        public static Vector256<short> _mm256_mulhrs_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyHighRoundScale(a, b);
        public static Vector256<short> _mm256_slli_epi16(Vector256<short> a, [ConstantExpected] byte count) => Avx2.ShiftLeftLogical(a, count);
        public static Vector256<short> _mm256_maddubs_epi16(Vector256<byte> a, Vector256<sbyte> b) => Avx2.MultiplyAddAdjacent(a, b); 
        public static Vector256<short> _mm256_setzero_epi16() => Vector256<short>.Zero;
        public static Vector256<short> _mm256_set1_epi16(short a) => Vector256.Create(a);

        public static Vector256<float> _mm256_loadu_ps(float* a) => Avx.LoadVector256(a);
        public static Vector256<float> _mm256_min_ps(Vector256<float> a, Vector256<float> b) => Avx.Min(a, b);
        public static Vector256<float> _mm256_max_ps(Vector256<float> a, Vector256<float> b) => Avx.Max(a, b);
        public static Vector256<float> _mm256_mul_ps(Vector256<float> a, Vector256<float> b) => Avx.Multiply(a, b);
        public static Vector256<float> _mm256_fmadd_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => Fma.MultiplyAdd(a, b, c);
        public static Vector256<float> _mm256_castsi256_ps(Vector256<int> a) => a.AsSingle();
        public static Vector256<float> _mm256_cvtepi32_ps(Vector256<int> a) => Avx.ConvertToVector256Single(a);
        public static Vector256<float> _mm256_set1_ps(float a) => Vector256.Create(a);
        public static int _mm256_movemask_ps(Vector256<float> a) => Avx.MoveMask(a);
        public static void _mm256_storeu_ps(float* mem_addr, Vector256<float> a) => Avx.Store(mem_addr, a);

        public static void _mm256_storeu_si256(sbyte* mem_addr, Vector256<sbyte> a) => Avx.Store(mem_addr, a);
        public static Vector256<byte> _mm256_packus_epi16(Vector256<short> a, Vector256<short> b) => Avx2.PackUnsignedSaturate(a, b);


        public static Vector128<float> _mm256_castps256_ps128(Vector256<float> a) => a.GetLower();
        public static Vector128<float> _mm256_extractf128_ps(Vector256<float> a, [ConstantExpected] byte index) => Avx.ExtractVector128(a, index);



        public static Vector128<int> _mm_add_epi32(Vector128<int> a, Vector128<int> b) => Avx.Add(a, b);
        public static Vector128<int> _mm_madd_epi16(Vector128<short> a, Vector128<short> b) => Avx.MultiplyAddAdjacent(a, b);
        public static Vector128<int> _mm_cmpgt_epi32(Vector128<int> a, Vector128<int> b) => Avx.CompareGreaterThan(a, b);
        public static Vector128<int> _mm_setzero_epi32() => Vector128<int>.Zero;
        public static Vector128<int> _mm_set1_epi32(int a) => Vector128.Create(a);

        public static Vector128<short> _mm_load_si128(short* mem_addr) => Avx.LoadAlignedVector128(mem_addr);
        public static Vector128<short> _mm_min_epi16(Vector128<short> a, Vector128<short> b) => Avx.Min(a, b);
        public static Vector128<short> _mm_max_epi16(Vector128<short> a, Vector128<short> b) => Avx.Max(a, b);
        public static Vector128<short> _mm_mulhi_epi16(Vector128<short> a, Vector128<short> b) => Avx.MultiplyHigh(a, b);
        public static Vector128<short> _mm_mulhrs_epi16(Vector128<short> a, Vector128<short> b) => Avx.MultiplyHighRoundScale(a, b);
        public static Vector128<short> _mm_slli_epi16(Vector128<short> a, [ConstantExpected] byte count) => Avx.ShiftLeftLogical(a, count);
        public static Vector128<short> _mm_maddubs_epi16(Vector128<byte> a, Vector128<sbyte> b) => Avx.MultiplyAddAdjacent(a, b);
        public static Vector128<short> _mm_setzero_epi16() => Vector128<short>.Zero;
        public static Vector128<short> _mm_set1_epi16(short a) => Vector128.Create(a);

        public static Vector128<float> _mm_loadu_ps(float* a) => Avx.LoadVector128(a);
        public static Vector128<float> _mm_min_ps(Vector128<float> a, Vector128<float> b) => Avx.Min(a, b);
        public static Vector128<float> _mm_max_ps(Vector128<float> a, Vector128<float> b) => Avx.Max(a, b);
        public static Vector128<float> _mm_mul_ps(Vector128<float> a, Vector128<float> b) => Avx.Multiply(a, b);
        public static Vector128<float> _mm_fmadd_ps(Vector128<float> a, Vector128<float> b, Vector128<float> c) => Fma.MultiplyAdd(a, b, c);
        public static Vector128<float> _mm_castsi128_ps(Vector128<int> a) => a.AsSingle();
        public static Vector128<float> _mm_cvtepi32_ps(Vector128<int> a) => Avx.ConvertToVector128Single(a);
        public static Vector128<float> _mm_set1_ps(float a) => Vector128.Create(a);
        public static int _mm_movemask_ps(Vector128<float> a) => Avx.MoveMask(a);
        public static void _mm_storeu_ps(float* mem_addr, Vector128<float> a) => Avx.Store(mem_addr, a);

        public static void _mm_storeu_si128(sbyte* mem_addr, Vector128<sbyte> a) => Avx.Store(mem_addr, a);
        public static Vector128<byte> _mm_packus_epi16(Vector128<short> a, Vector128<short> b) => Avx.PackUnsignedSaturate(a, b);



        public static Vector128<int> arm_madd_epi16(Vector128<short> a, Vector128<short> b)
        {
            var low = vmull_s16(vget_low_s16(a), vget_low_s16(b));
            var high = vmull_high_s16(a, b);
            return vpaddq_s32(low, high);
        }
        public static Vector128<int> arm_cmpgt_epi32(Vector128<int> a, Vector128<int> b) => AdvSimd.CompareGreaterThan(a, b);
        public static Vector128<int> arm_setzero_epi32() => Vector128<int>.Zero;
        public static Vector128<int> arm_set1_epi32(int a) => Vector128.Create(a);

        public static Vector128<short> arm_load_si128(short* mem_addr) => AdvSimd.LoadVector128(mem_addr);
        public static Vector128<short> arm_min_epi16(Vector128<short> a, Vector128<short> b) => AdvSimd.Min(a, b);
        public static Vector128<short> arm_max_epi16(Vector128<short> a, Vector128<short> b) => AdvSimd.Max(a, b);
        public static Vector128<short> arm_mulhi_epi16(Vector128<short> a, Vector128<short> b)
        {
            var lo = vmull_s16(vget_low_s16(a), vget_low_s16(b));
            var hi = vmull_s16(vget_high_s16(a), vget_high_s16(b));
            return vcombine_s16(vshrn_n_s32(lo, 16), vshrn_n_s32(hi, 16));
        }

        public static Vector128<short> arm_mulhrs_epi16(Vector128<short> a, Vector128<short> b)
        {
            var lo = vmull_s16(vget_low_s16(a), vget_low_s16(b));
            var hi = vmull_s16(vget_high_s16(a), vget_high_s16(b));
            var narrowLo = vshrn_n_s32(lo, 15);
            var narrowHi = vshrn_n_s32(hi, 15);
            return vcombine_s16(narrowLo, narrowHi);
        }

        public static Vector128<short> arm_slli_epi16(Vector128<short> a, [ConstantExpected(Min = 0, Max = 63)] byte count) => AdvSimd.ShiftLeftLogical(a, count);
        public static Vector128<short> arm_maddubs_epi16(Vector128<byte> a, Vector128<sbyte> b)
        {
            var tl = vmulq_s16(vreinterpretq_s16_u16(vmovl_u8(vget_low_u8(a))), vmovl_s8(vget_low_s8(b)));
            var th = vmulq_s16(vreinterpretq_s16_u16(vmovl_u8(vget_high_u8(a))), vmovl_s8(vget_high_s8(b)));
            return vqaddq_s16(vuzp1q_s16(tl, th), vuzp2q_s16(tl, th));
        }
        public static Vector128<short> arm_setzero_epi16() => Vector128<short>.Zero;
        public static Vector128<short> arm_set1_epi16(short a) => Vector128.Create(a);

        public static Vector128<float> arm_loadu_ps(float* a) => AdvSimd.LoadVector128(a);
        public static Vector128<float> arm_min_ps(Vector128<float> a, Vector128<float> b) => AdvSimd.Min(a, b);
        public static Vector128<float> arm_max_ps(Vector128<float> a, Vector128<float> b) => AdvSimd.Max(a, b);
        public static Vector128<float> arm_mul_ps(Vector128<float> a, Vector128<float> b) => AdvSimd.Multiply(a, b);
        public static Vector128<float> arm_fmadd_ps(Vector128<float> a, Vector128<float> b, Vector128<float> c) => AdvSimd.FusedMultiplyAdd(c, a, b);
        public static Vector128<float> arm_castsi128_ps(Vector128<int> a) => a.AsSingle();
        public static Vector128<float> arm_cvtepi32_ps(Vector128<int> a) => AdvSimd.ConvertToSingle(a);
        public static Vector128<float> arm_set1_ps(float a) => Vector128.Create(a);
        public static void arm_storeu_ps(float* mem_addr, Vector128<float> a) => AdvSimd.Store(mem_addr, a);

        public static void arm_storeu_si128(sbyte* mem_addr, Vector128<sbyte> a) => AdvSimd.Store(mem_addr, a);
        public static Vector128<byte> arm_packus_epi16(Vector128<short> a, Vector128<short> b)
        {
            return vcombine_u8(vqmovun_s16(a), vqmovun_s16(b));
        }


        private static Vector128<float> vmulq_f32(Vector128<float> a, Vector128<float> b) => AdvSimd.Multiply(a, b);
        private static Vector128<float> vaddq_f32(Vector128<float> a, Vector128<float> b) => AdvSimd.Add(a, b);
        private static Vector128<int> vpaddq_s32(Vector128<int> a, Vector128<int> b) => AdvSimd.Arm64.AddPairwise(a, b);
        private static Vector128<int> vaddq_s32(Vector128<int> a, Vector128<int> b) => AdvSimd.Add(a, b);
        private static Vector128<byte> vcombine_u8(Vector64<byte> a, Vector64<byte> b) => Vector128.Create(a, b);
        private static Vector64<byte> vqmovun_s16(Vector128<short> a) => AdvSimd.ExtractNarrowingSaturateUnsignedLower(a);
        private static Vector128<short> vuzp2q_s16(Vector128<short> a, Vector128<short> b) => AdvSimd.Arm64.UnzipOdd(a, b);
        private static Vector128<short> vuzp1q_s16(Vector128<short> a, Vector128<short> b) => AdvSimd.Arm64.UnzipEven(a, b);
        private static Vector128<short> vqaddq_s16(Vector128<short> a, Vector128<short> b) => AdvSimd.AddSaturate(a, b);
        private static Vector64<byte> vget_low_u8(Vector128<byte> a) => a.GetLower();
        private static Vector64<sbyte> vget_low_s8(Vector128<sbyte> a) => a.GetLower();
        private static Vector64<byte> vget_high_u8(Vector128<byte> a) => a.GetUpper();
        private static Vector64<sbyte> vget_high_s8(Vector128<sbyte> a) => a.GetUpper();
        private static Vector128<short> vmulq_s16(Vector128<short> a, Vector128<short> b) => AdvSimd.Multiply(a, b);
        private static Vector128<int> vmull_high_s16(Vector128<short> a, Vector128<short> b) => AdvSimd.MultiplyWideningUpper(a, b);
        private static Vector64<short> vshrn_n_s32(Vector128<int> a, [ConstantExpected(Min = 1, Max = 32)] byte count) => AdvSimd.ShiftRightLogicalNarrowingLower(a, count);
        private static Vector128<short> vcombine_s16(Vector64<short> a, Vector64<short> b) => Vector128.Create(a, b);
        private static Vector64<short> vget_low_s16(Vector128<short> a) => a.GetLower();
        private static Vector64<short> vget_high_s16(Vector128<short> a) => a.GetUpper();
        private static Vector128<int> vmull_s16(Vector64<short> a, Vector64<short> b) => AdvSimd.MultiplyWideningLower(a, b);
        private static Vector128<int> vdupq_n_s32(int a) => AdvSimd.DuplicateToVector128(a);
        private static Vector128<short> vdupq_n_s16(short a) => AdvSimd.DuplicateToVector128(a);
        private static Vector128<int> vcgtq_s32(Vector128<int> a, Vector128<int> b) => AdvSimd.CompareGreaterThan(a, b);
        private static Vector64<ushort> vmovn_u32(Vector128<uint> a) => AdvSimd.ExtractNarrowingLower(a);
        private static Vector128<ushort> vmovl_u8(Vector64<byte> a) => AdvSimd.ZeroExtendWideningLower(a);
        private static Vector128<short> vmovl_s8(Vector64<sbyte> a) => AdvSimd.SignExtendWideningLower(a);

        private static ulong vget_lane_u64(Vector64<ulong> a, int b) => a.GetElement(b);
        private static Vector128<short> vreinterpretq_s16_u16(Vector128<ushort> a) => a.AsInt16();
        private static Vector64<ulong> vreinterpret_u64_u16(Vector64<ushort> a) => a.AsUInt64();

        public static void arm_storeu_si128(ushort* mem_addr, Vector128<ushort> a) => AdvSimd.Store(mem_addr, a); 
        public static Vector128<ushort> arm_add_epi16(Vector128<ushort> a, Vector128<ushort> b) => AdvSimd.Add(a, b);

        public static Vector256<float> vec_mul_add_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => _mm256_fmadd_ps(a, b, c);
        public static Vector128<float> vec_mul_add_ps(Vector128<float> a, Vector128<float> b, Vector128<float> c) => _mm_fmadd_ps(a, b, c);
        public static Vector128<float> arm_vec_mul_add_ps(Vector128<float> a, Vector128<float> b, Vector128<float> c) => AdvSimd.FusedMultiplyAdd(c, a, b);

        public static int vec_nnz_mask(Vector256<byte> a) => _mm256_movemask_ps(_mm256_castsi256_ps(_mm256_cmpgt_epi32(a.AsInt32(), _mm256_setzero_epi32())));
        public static int vec_nnz_mask(Vector128<byte> a) => _mm_movemask_ps(_mm_castsi128_ps(_mm_cmpgt_epi32(a.AsInt32(), _mm_setzero_epi32())));
        public static int arm_vec_nnz_mask(Vector128<byte> a)
        {
            var mask = vcgtq_s32(a.AsInt32(), vdupq_n_s32(0));
            Vector64<ushort> narrowed_mask = vmovn_u32(mask.AsUInt32());
            var packed_mask = vget_lane_u64(vreinterpret_u64_u16(narrowed_mask), 0);
            var retVal = ((packed_mask & (1UL <<  0)) >>  0) |
                         ((packed_mask & (1UL << 16)) >> 15) |
                         ((packed_mask & (1UL << 32)) >> 30) |
                         ((packed_mask & (1UL << 48)) >> 45);

            return (int)retVal;
        }


        public static Vector256<int> vec_dpbusd_epi32(Vector256<int> sum, Vector256<byte> a, Vector256<sbyte> b)
        {
            var product16 = _mm256_maddubs_epi16(a, b);
            var product32 = _mm256_madd_epi16(product16, _mm256_set1_epi16(1));
            return _mm256_add_epi32(sum, product32);
        }

        public static Vector128<int> vec_dpbusd_epi32(Vector128<int> sum, Vector128<byte> a, Vector128<sbyte> b)
        {
            var product16 = _mm_maddubs_epi16(a, b);
            var product32 = _mm_madd_epi16(product16, _mm_set1_epi16(1));
            return _mm_add_epi32(sum, product32);
        }

        public static Vector128<int> arm_vec_dpbusd_epi32(Vector128<int> sum, Vector128<byte> a, Vector128<sbyte> b)
        {
            var ubs = arm_maddubs_epi16(a, b);
            var sum32 = arm_madd_epi16(ubs, vdupq_n_s16(1));
            return vaddq_s32(sum32, sum);
        }




        public static void _mm_storeu_si128(ushort* mem_addr, Vector128<ushort> a) => Sse2.Store(mem_addr, a);
        public static Vector128<float> _mm_shuffle_ps(Vector128<float> a, Vector128<float> b, [ConstantExpected] byte control) => Sse.Shuffle(a, b, control);
        public static Vector128<float> _mm_movehl_ps(Vector128<float> a, Vector128<float> b) => Sse.MoveHighToLow(a, b);
        public static Vector128<float> _mm_add_ss(Vector128<float> a, Vector128<float> b) => Sse.AddScalar(a, b);
        public static Vector128<float> _mm_add_ps(Vector128<float> a, Vector128<float> b) => Sse.Add(a, b);
        public static Vector128<ushort> _mm_add_epi16(Vector128<ushort> a, Vector128<ushort> b) => Sse2.Add(a, b);


        public static float _mm_cvtss_f32(Vector128<float> a) => a.GetElement(0);


        public static float vec_reduce_add_ps(Vector256<float> sum)
        {
            var upper_128 = _mm256_extractf128_ps(sum, 1);
            var lower_128 = _mm256_castps256_ps128(sum);
            var sum_128 = _mm_add_ps(upper_128, lower_128);

            var upper_64 = _mm_movehl_ps(sum_128, sum_128);
            var sum_64 = _mm_add_ps(upper_64, sum_128);

            var upper_32 = _mm_shuffle_ps(sum_64, sum_64, 1);
            var sum_32 = _mm_add_ss(upper_32, sum_64);

            return _mm_cvtss_f32(sum_32);
        }

        public static float vec_reduce_add_ps(Vector128<float> sum)
        {
            var sum_128 = sum;

            var upper_64 = _mm_movehl_ps(sum_128, sum_128);
            var sum_64 = _mm_add_ps(upper_64, sum_128);

            var upper_32 = _mm_shuffle_ps(sum_64, sum_64, 1);
            var sum_32 = _mm_add_ss(upper_32, sum_64);

            return _mm_cvtss_f32(sum_32);
        }

        public static float arm_vec_reduce_add_ps(Vector128<float> sum)
        {
            return Vector128.Sum(sum);
        }
    }

}
