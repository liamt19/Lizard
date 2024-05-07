
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace Lizard.Logic.NN
{
    public static unsafe class Aliases
    {

        public static Vector256<int> _mm256_add_epi32(Vector256<int> a, Vector256<int> b) => Avx2.Add(a, b);
        public static Vector256<int> _mm256_hadd_epi32(Vector256<int> a, Vector256<int> b) => Avx2.HorizontalAdd(a, b);
        public static Vector256<int> _mm256_madd_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyAddAdjacent(a, b);

        public static Vector256<short> _mm256_min_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Min(a, b);
        public static Vector256<short> _mm256_max_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Max(a, b);
        public static Vector256<short> _mm256_mullo_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyLow(a, b);
        public static Vector256<short> _mm256_srli_epi16(Vector256<short> a, [ConstantExpected] byte count) => Avx2.ShiftRightLogical(a, count);

        public static Vector256<float> _mm256_add_ps(Vector256<float> a, Vector256<float> b) => Avx.Add(a, b);
        public static Vector256<float> _mm256_hadd_ps(Vector256<float> a, Vector256<float> b) => Avx.HorizontalAdd(a, b);
        public static Vector256<float> _mm256_min_ps(Vector256<float> a, Vector256<float> b) => Avx.Min(a, b);
        public static Vector256<float> _mm256_max_ps(Vector256<float> a, Vector256<float> b) => Avx.Max(a, b);
        public static Vector256<float> _mm256_mul_ps(Vector256<float> a, Vector256<float> b) => Avx.Multiply(a, b);
        public static Vector256<float> _mm256_fmadd_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => Fma.MultiplyAdd(a, b, c);

        public static Vector256<float> _mm256_loadu_ps(float* a) => Avx.LoadVector256(a);
        public static Vector256<short> _mm256_loadu_si256(short* mem_addr) => Avx2.LoadVector256(mem_addr);
        public static void _mm256_storeu_si256(short* mem_addr, Vector256<short> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_si256(int* mem_addr, Vector256<int> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_ps(float* mem_addr, Vector256<float> a) => Avx.Store(mem_addr, a);

        public static Vector256<float> _mm256_castps128_ps256(Vector128<float> a) => a.ToVector256Unsafe();
        public static Vector256<int> _mm256_castsi128_si256(Vector128<int> a) => a.ToVector256Unsafe();
        public static Vector128<float> _mm256_castps256_ps128(Vector256<float> a) => a.GetLower();
        public static Vector128<int> _mm256_castsi256_si128(Vector256<int> a) => a.GetLower();
        public static Vector128<int> _mm256_extracti128_si256(Vector256<int> a, [ConstantExpected] byte index) => Avx.ExtractVector128(a, index);
        public static Vector128<float> _mm256_extractf128_ps(Vector256<float> a, [ConstantExpected] byte index) => Avx.ExtractVector128(a, index);
        public static Vector256<float> _mm256_insertf128_ps(Vector256<float> a, Vector128<float> b, [ConstantExpected] byte index) => Avx.InsertVector128(a, b, index);
        public static Vector256<int> _mm256_inserti128_si256(Vector256<int> a, Vector128<int> b, [ConstantExpected] byte index) => Avx2.InsertVector128(a, b, index);
        public static Vector256<float> _mm256_set1_ps(float a) => Vector256.Create(a);
        public static Vector256<short> _mm256_set1_epi16(short a) => Vector256.Create(a);
        public static Vector256<float> _mm256_setzero_ps() => Vector256<float>.Zero;

        public static Vector128<float> _mm_shuffle_ps(Vector128<float> a, Vector128<float> b, [ConstantExpected] byte control) => Sse.Shuffle(a, b, control);
        public static Vector128<float> _mm_movehl_ps(Vector128<float> a, Vector128<float> b) => Sse.MoveHighToLow(a, b);
        public static Vector128<float> _mm_add_ss(Vector128<float> a, Vector128<float> b) => Sse.AddScalar(a, b);
        public static Vector128<float> _mm_add_ps(Vector128<float> a, Vector128<float> b) => Sse.Add(a, b);
        public static Vector128<int> _mm_add_epi32(Vector128<int> a, Vector128<int> b) => Sse2.Add(a, b);

        public static float _mm_cvtss_f32(Vector128<float> a) => a.GetElement(0);
    }

}
