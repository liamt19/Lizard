
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace Lizard.Logic.NN
{
    public static unsafe class Aliases
    {

        public static Vector256<short> _mm256_add_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Add(a, b);
        public static Vector256<int> _mm256_add_epi32(Vector256<int> a, Vector256<int> b) => Avx2.Add(a, b);
        public static Vector256<int> _mm256_hadd_epi32(Vector256<int> a, Vector256<int> b) => Avx2.HorizontalAdd(a, b);
        public static Vector256<int> _mm256_madd_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyAddAdjacent(a, b);

        public static Vector256<short> _mm256_min_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Min(a, b);
        public static Vector256<short> _mm256_max_epi16(Vector256<short> a, Vector256<short> b) => Avx2.Max(a, b);
        public static Vector256<short> _mm256_mullo_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyLow(a, b);
        public static Vector256<short> _mm256_mulhi_epi16(Vector256<short> a, Vector256<short> b) => Avx2.MultiplyHigh(a, b);
        public static Vector256<short> _mm256_srli_epi16(Vector256<short> a, [ConstantExpected] byte count) => Avx2.ShiftRightLogical(a, count);
        public static Vector256<short> _mm256_slli_epi16(Vector256<short> a, [ConstantExpected] byte count) => Avx2.ShiftLeftLogical(a, count);
        public static Vector256<short> _mm256_unpacklo_epi16(Vector256<short> a, Vector256<short> b) => Avx2.UnpackLow(a, b);
        public static Vector256<short> _mm256_unpackhi_epi16(Vector256<short> a, Vector256<short> b) => Avx2.UnpackHigh(a, b);
        public static Vector256<short> _mm256_cmpgt_epi16(Vector256<short> a, Vector256<short> b) => Avx2.CompareGreaterThan(a, b);
        public static Vector256<int> _mm256_cmpgt_epi32(Vector256<int> a, Vector256<int> b) => Avx2.CompareGreaterThan(a, b);
        public static int _mm256_movemask_epi8(Vector256<byte> a) => Avx2.MoveMask(a);

        public static uint _mm256_movemask_epi16_at_home(Vector256<short> a) => (uint) Avx2.MoveMask(_mm256_srli_epi16(a, 8).AsByte());

        public static Vector256<float> _mm256_add_ps(Vector256<float> a, Vector256<float> b) => Avx.Add(a, b);
        public static Vector256<float> _mm256_hadd_ps(Vector256<float> a, Vector256<float> b) => Avx.HorizontalAdd(a, b);
        public static Vector256<float> _mm256_min_ps(Vector256<float> a, Vector256<float> b) => Avx.Min(a, b);
        public static Vector256<float> _mm256_max_ps(Vector256<float> a, Vector256<float> b) => Avx.Max(a, b);
        public static Vector256<float> _mm256_mul_ps(Vector256<float> a, Vector256<float> b) => Avx.Multiply(a, b);
        public static Vector256<float> _mm256_fmadd_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => Fma.MultiplyAdd(a, b, c);
        public static Vector256<float> _mm256_castsi256_ps(Vector256<short> a) => a.AsSingle();
        public static Vector256<float> _mm256_castsi256_ps(Vector256<int> a) => a.AsSingle();
        public static Vector256<float> _mm256_div_ps(Vector256<float> a, Vector256<float> b) => Avx.Divide(a, b); 
        public static Vector256<float> _mm256_cvtepi32_ps(Vector256<int> a) => Avx.ConvertToVector256Single(a);
        public static Vector256<float> _mm256_permute2f128_ps(Vector256<float> a, Vector256<float> b, [ConstantExpected] byte control) => Avx.Permute2x128(a, b, control);
        public static int _mm256_movemask_ps(Vector256<float> a) => Avx.MoveMask(a);

        public static Vector256<float> _mm256_loadu_ps(float* a) => Avx.LoadVector256(a);
        public static Vector256<float> _mm256_load_ps(float* a) => Avx.LoadAlignedVector256(a);
        public static Vector256<short> _mm256_loadu_si256(short* mem_addr) => Avx.LoadVector256(mem_addr);
        public static Vector256<short> _mm256_load_si256(short* mem_addr) => Avx.LoadAlignedVector256(mem_addr);
        public static Vector256<sbyte> _mm256_load_si256(sbyte* mem_addr) => Avx.LoadAlignedVector256(mem_addr);
        public static Vector256<int> _mm256_load_si256(int* mem_addr) => Avx.LoadAlignedVector256(mem_addr);
        public static void _mm256_storeu_si256(short* mem_addr, Vector256<short> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_si256(int* mem_addr, Vector256<int> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_si256(byte* mem_addr, Vector256<byte> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_si256(sbyte* mem_addr, Vector256<sbyte> a) => Avx.Store(mem_addr, a);
        public static void _mm256_storeu_ps(float* mem_addr, Vector256<float> a) => Avx.Store(mem_addr, a);
        public static void _mm256_store_si256(int* mem_addr, Vector256<int> a) => Avx.StoreAligned(mem_addr, a);

        public static Vector256<float> _mm256_castps128_ps256(Vector128<float> a) => a.ToVector256Unsafe();
        public static Vector256<int> _mm256_castsi128_si256(Vector128<int> a) => a.ToVector256Unsafe();
        public static Vector128<float> _mm256_castps256_ps128(Vector256<float> a) => a.GetLower();
        public static Vector128<int> _mm256_castsi256_si128(Vector256<int> a) => a.GetLower();
        public static Vector128<int> _mm256_extracti128_si256(Vector256<int> a, [ConstantExpected] byte index) => Avx.ExtractVector128(a, index);
        public static Vector128<float> _mm256_extractf128_ps(Vector256<float> a, [ConstantExpected] byte index) => Avx.ExtractVector128(a, index);
        public static Vector256<float> _mm256_insertf128_ps(Vector256<float> a, Vector128<float> b, [ConstantExpected] byte index) => Avx.InsertVector128(a, b, index);
        public static Vector256<int> _mm256_inserti128_si256(Vector256<int> a, Vector128<int> b, [ConstantExpected] byte index) => Avx2.InsertVector128(a, b, index);
        public static Vector256<short> _mm256_inserti128_si256(Vector256<short> a, Vector128<short> b, [ConstantExpected] byte index) => Avx2.InsertVector128(a, b, index);
        public static Vector256<int> _mm256_set1_epi32(int a) => Vector256.Create(a);
        public static Vector256<float> _mm256_set1_ps(float a) => Vector256.Create(a);
        public static Vector256<short> _mm256_set1_epi16(short a) => Vector256.Create(a);
        public static Vector256<int> _mm256_setzero_epi32() => Vector256<int>.Zero;
        public static Vector256<short> _mm256_setzero_epi16() => Vector256<short>.Zero;
        public static Vector256<float> _mm256_setzero_ps() => Vector256<float>.Zero;


        public static void _mm128_storeu_si128(ushort* mem_addr, Vector128<ushort> a) => Sse2.Store(mem_addr, a);
        public static Vector128<int> _mm_madd_epi16(Vector128<short> a, Vector128<short> b) => Sse2.MultiplyAddAdjacent(a, b);
        public static Vector128<short> _mm_min_epi16(Vector128<short> a, Vector128<short> b) => Sse2.Min(a, b);
        public static Vector128<short> _mm_max_epi16(Vector128<short> a, Vector128<short> b) => Sse2.Max(a, b);
        public static Vector128<short> _mm_loadu_si128(short* mem_addr) => Sse2.LoadVector128(mem_addr);
        public static Vector128<short> _mm_cmpgt_epi16(Vector128<short> a, Vector128<short> b) => Sse2.CompareGreaterThan(a, b); 
        public static Vector128<float> _mm_shuffle_ps(Vector128<float> a, Vector128<float> b, [ConstantExpected] byte control) => Sse.Shuffle(a, b, control);
        public static Vector128<float> _mm_movehl_ps(Vector128<float> a, Vector128<float> b) => Sse.MoveHighToLow(a, b);
        public static Vector128<float> _mm_add_ss(Vector128<float> a, Vector128<float> b) => Sse.AddScalar(a, b);
        public static Vector128<float> _mm_add_ps(Vector128<float> a, Vector128<float> b) => Sse.Add(a, b);
        public static Vector128<ushort> _mm_add_epi16(Vector128<ushort> a, Vector128<ushort> b) => Sse2.Add(a, b);

        public static Vector128<int> _mm_add_epi32(Vector128<int> a, Vector128<int> b) => Sse2.Add(a, b);
        public static Vector128<short> _mm_setzero_epi16() => Vector128<short>.Zero;
        public static Vector128<short> _mm_set1_epi16(short a) => Vector128.Create(a); 
        public static int _mm_movemask_epi8(Vector128<byte> a) => Sse2.MoveMask(a);

        public static float _mm_cvtss_f32(Vector128<float> a) => a.GetElement(0);


        public static Vector256<short> _mm256_maddubs_epi16(Vector256<byte> a, Vector256<sbyte> b) => Avx2.MultiplyAddAdjacent(a, b);
        public static Vector256<byte> _mm256_packus_epi16(Vector256<short> a, Vector256<short> b) => Avx2.PackUnsignedSaturate(a, b);
        public static Vector256<long> _mm256_permute4x64_epi64(Vector256<long> a, [ConstantExpected] byte control) => Avx2.Permute4x64(a, control);


        public static Vector256<sbyte> vec_packus_permute_epi16(Vector256<short> vec0, Vector256<short> vec1) {
            var packed = _mm256_packus_epi16(vec0, vec1);

            //  _MM_SHUFFLE(3, 1, 2, 0) == 0b11011000
            return _mm256_permute4x64_epi64(packed.AsInt64(), 0b11_01_10_00).AsSByte();
        }

        public static Vector256<float> vec_mul_add_ps(Vector256<float> a, Vector256<float> b, Vector256<float> c) => _mm256_fmadd_ps(a, b, c);

        public static Vector256<int> vec_dpwssd_epi32(Vector256<int> sum, Vector256<short> a, Vector256<short> b) => _mm256_add_epi32(sum, _mm256_madd_epi16(a, b));

        public static int vec_nnz_mask(Vector256<byte> a) => _mm256_movemask_ps(_mm256_castsi256_ps(_mm256_cmpgt_epi32(a.AsInt32(), _mm256_setzero_epi32())));

        public static Vector256<int> vec_dpbusd_epi32(Vector256<int> sum, Vector256<byte> a, Vector256<sbyte> b)
        {
            var product16 = _mm256_maddubs_epi16(a, b);
            var product32 = _mm256_madd_epi16(product16, _mm256_set1_epi16(1));
            return _mm256_add_epi32(sum, product32);
        }

        public static Vector256<int> mul_add_2xu8_to_i32(Vector256<int> sum, Vector256<byte> a, Vector256<sbyte> b, Vector256<byte> c, Vector256<sbyte> d)
        {
            var product16a = _mm256_maddubs_epi16(a, b);
            var product16b = _mm256_maddubs_epi16(c, d);
            var product32 = _mm256_madd_epi16(_mm256_add_epi16(product16a, product16b), _mm256_set1_epi16(1));
            return _mm256_add_epi32(sum, product32);
        }

        public static Vector256<float> vec_haddx8_cvtepi32_ps(Vector256<int>* vecs) {
            var sum01 = _mm256_hadd_epi32(vecs[0], vecs[1]);
            var sum23 = _mm256_hadd_epi32(vecs[2], vecs[3]);
            var sum45 = _mm256_hadd_epi32(vecs[4], vecs[5]);
            var sum67 = _mm256_hadd_epi32(vecs[6], vecs[7]);

            var sum0123 = _mm256_hadd_epi32(sum01, sum23);
            var sum4567 = _mm256_hadd_epi32(sum45, sum67);

            var sumALow = _mm256_castsi256_si128(sum0123);
            var sumAHi = _mm256_extracti128_si256(sum0123, 1);
            var sumA = _mm_add_epi32(sumALow, sumAHi);

            var sumBLow = _mm256_castsi256_si128(sum4567);
            var sumBHi = _mm256_extracti128_si256(sum4567, 1);
            var sumB = _mm_add_epi32(sumBLow, sumBHi);

            var sumAB = _mm256_inserti128_si256(_mm256_castsi128_si256(sumA), sumB, 1);
            return _mm256_cvtepi32_ps(sumAB);
        }

        public static Vector256<float> vec_hadd_psx8(Vector256<float>* vecs)
        {
            var sum01 = _mm256_hadd_ps(vecs[0], vecs[1]);
            var sum23 = _mm256_hadd_ps(vecs[2], vecs[3]);
            var sum45 = _mm256_hadd_ps(vecs[4], vecs[5]);
            var sum67 = _mm256_hadd_ps(vecs[6], vecs[7]);

            var sum0123 = _mm256_hadd_ps(sum01, sum23);
            var sum4567 = _mm256_hadd_ps(sum45, sum67);

            var sumA = _mm256_permute2f128_ps(sum0123, sum4567, 0x20);
            var sumB = _mm256_permute2f128_ps(sum0123, sum4567, 0x31);
            return _mm256_add_ps(sumA, sumB);
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
    }

}
