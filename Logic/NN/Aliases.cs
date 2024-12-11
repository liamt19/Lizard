
using System.Diagnostics.CodeAnalysis;
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


        public static void _mm_storeu_si128(ushort* mem_addr, Vector128<ushort> a) => Sse2.Store(mem_addr, a);
        public static Vector128<float> _mm_shuffle_ps(Vector128<float> a, Vector128<float> b, [ConstantExpected] byte control) => Sse.Shuffle(a, b, control);
        public static Vector128<float> _mm_movehl_ps(Vector128<float> a, Vector128<float> b) => Sse.MoveHighToLow(a, b);
        public static Vector128<float> _mm_add_ss(Vector128<float> a, Vector128<float> b) => Sse.AddScalar(a, b);
        public static Vector128<float> _mm_add_ps(Vector128<float> a, Vector128<float> b) => Sse.Add(a, b);
        public static Vector128<ushort> _mm_add_epi16(Vector128<ushort> a, Vector128<ushort> b) => Sse2.Add(a, b);
        public static float _mm_cvtss_f32(Vector128<float> a) => a.GetElement(0);


        public static Vector256<float> vec_mul_add_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => _mm256_fmadd_ps(a, b, c);
        public static Vector128<float> vec_mul_add_ps(Vector128<float> a, Vector128<float> b, Vector128<float> c) => _mm_fmadd_ps(a, b, c);

        public static int vec_nnz_mask(Vector256<byte> a) => _mm256_movemask_ps(_mm256_castsi256_ps(_mm256_cmpgt_epi32(a.AsInt32(), _mm256_setzero_epi32())));
        public static int vec_nnz_mask(Vector128<byte> a) => _mm_movemask_ps(_mm_castsi128_ps(_mm_cmpgt_epi32(a.AsInt32(), _mm_setzero_epi32())));


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
    }

}
