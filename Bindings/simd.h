#pragma once

#include <immintrin.h>
#include "defs.h"

#if defined(AVX512) && defined(TODO_SOME_DAY)

using vec_i8 = __m512i;
using vec_i16 = __m512i;
using vec_i32 = __m512i;
using vec_ps = __m512;
using vec_128i = __m128i;

inline vec_i8 vec_packus_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_packus_epi16(a, b); }
inline void vec_storeu_epi8(vec_i8* a, const vec_i8 b) { _mm512_storeu_si512(a, b); }

inline vec_ps vec_set1_ps(const float a) { return _mm512_set1_ps(a); }
inline vec_ps vec_fmadd_ps(const vec_ps a, const vec_ps b, const vec_ps c) { return _mm512_fmadd_ps(a, b, c); }
inline vec_ps vec_min_ps(const vec_ps a, const vec_ps b) { return _mm512_min_ps(a, b); }
inline vec_ps vec_max_ps(const vec_ps a, const vec_ps b) { return _mm512_max_ps(a, b); }
inline vec_ps vec_mul_ps(const vec_ps a, const vec_ps b) { return _mm512_mul_ps(a, b); }
inline vec_ps vec_cvtepi32_ps(const vec_i32 a) { return _mm512_cvtepi32_ps(a); }
inline vec_ps vec_loadu_ps(const float* a) { return _mm512_loadu_ps(a); }
inline void vec_storeu_ps(float* a, const vec_ps b) { _mm512_storeu_ps(a, b); }

inline vec_i16 vec_set1_epi16(const i16 a) { return _mm512_set1_epi16(a); }
inline vec_i16 vec_setzero_epi16() { return _mm512_setzero_si512(); }
inline vec_i16 vec_add_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_add_epi16(a, b); }
inline vec_i16 vec_sub_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_sub_epi16(a, b); }
inline vec_i16 vec_maddubs_epi16(const vec_i8 a, const vec_i8 b) { return _mm512_maddubs_epi16(a, b); }
inline vec_i16 vec_mulhi_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_mulhi_epi16(a, b); }
inline vec_i16 vec_slli_epi16(const vec_i16 a, const i16 i) { return _mm512_slli_epi16(a, i); }
inline vec_i16 vec_min_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_min_epi16(a, b); }
inline vec_i16 vec_max_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_max_epi16(a, b); }
inline vec_i16 vec_load_epi16(const vec_i16* a) { return _mm512_load_si512(a); }
inline void vec_storeu_i16(vec_i16* a, const vec_i16 b) { _mm512_storeu_si512(a, b); }

inline vec_i32 vec_set1_epi32(const i32 a) { return _mm512_set1_epi32(a); }
inline vec_i32 vec_add_epi32(const vec_i32 a, const vec_i32 b) { return _mm512_add_epi32(a, b); }
inline vec_i32 vec_madd_epi16(const vec_i16 a, const vec_i16 b) { return _mm512_madd_epi16(a, b); }

inline uint16_t vec_nnz_mask(const vec_i32 vec) { return _mm512_cmpgt_epi32_mask(vec, _mm512_setzero_si512()); }

inline float vec_hsum_ps(const vec_ps v) {
    return _mm512_reduce_add_ps(v);
}

inline vec_i32 vec_dpbusd_epi32(const vec_i32 sum, const vec_i8 vec0, const vec_i8 vec1) {
    const vec_i16 product16 = vec_maddubs_epi16(vec0, vec1);
    const vec_i32 product32 = vec_madd_epi16(product16, vec_set1_epi16(1));
    return vec_add_epi32(sum, product32);
}


#else

using vec_i8 = __m256i;
using vec_i16 = __m256i;
using vec_i32 = __m256i;
using vec_ps = __m256;
using vec_128i = __m128i;

inline vec_i8 vec_packus_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_packus_epi16(a, b); }
inline void vec_storeu_epi8(vec_i8* a, const vec_i8 b) { _mm256_storeu_si256(a, b); }

inline vec_ps vec_set1_ps(const float a) { return _mm256_set1_ps(a); }
inline vec_ps vec_fmadd_ps(const vec_ps a, const vec_ps b, const vec_ps c) { return _mm256_fmadd_ps(a, b, c); }
inline vec_ps vec_min_ps(const vec_ps a, const vec_ps b) { return _mm256_min_ps(a, b); }
inline vec_ps vec_max_ps(const vec_ps a, const vec_ps b) { return _mm256_max_ps(a, b); }
inline vec_ps vec_mul_ps(const vec_ps a, const vec_ps b) { return _mm256_mul_ps(a, b); }
inline vec_ps vec_cvtepi32_ps(const vec_i32 a) { return _mm256_cvtepi32_ps(a); }
inline vec_ps vec_loadu_ps(const float* a) { return _mm256_loadu_ps(a); }
inline void vec_storeu_ps(float* a, const vec_ps b) { _mm256_storeu_ps(a, b); }

inline vec_i16 vec_set1_epi16(const i16 a) { return _mm256_set1_epi16(a); }
inline vec_i16 vec_setzero_epi16() { return _mm256_setzero_si256(); }
inline vec_i16 vec_maddubs_epi16(const vec_i8 a, const vec_i8 b) { return _mm256_maddubs_epi16(a, b); }
inline vec_i16 vec_add_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_add_epi16(a, b); }
inline vec_i16 vec_sub_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_sub_epi16(a, b); }
inline vec_i16 vec_mulhi_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_mulhi_epi16(a, b); }
inline vec_i16 vec_slli_epi16(const vec_i16 a, const i16 i) { return _mm256_slli_epi16(a, i); }
inline vec_i16 vec_min_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_min_epi16(a, b); }
inline vec_i16 vec_max_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_max_epi16(a, b); }
inline vec_i16 vec_load_epi16(const vec_i16* a) { return _mm256_load_si256(a); }
inline void vec_storeu_i16(vec_i16* a, const vec_i16 b) { _mm256_storeu_si256(a, b); }

inline vec_i32 vec_set1_epi32(const i32 a) { return _mm256_set1_epi32(a); }
inline vec_i32 vec_add_epi32(const vec_i32 a, const vec_i32 b) { return _mm256_add_epi32(a, b); }
inline vec_i32 vec_madd_epi16(const vec_i16 a, const vec_i16 b) { return _mm256_madd_epi16(a, b); }

inline uint16_t vec_nnz_mask(const vec_i32 vec) { return _mm256_movemask_ps(_mm256_castsi256_ps(_mm256_cmpgt_epi32(vec, _mm256_setzero_si256()))); }

inline float vec_hsum_ps(const vec_ps v) {
    __m128 sum128 = _mm_add_ps(_mm256_castps256_ps128(v), _mm256_extractf128_ps(v, 1));
    sum128 = _mm_hadd_ps(sum128, sum128);
    sum128 = _mm_hadd_ps(sum128, sum128);
    return _mm_cvtss_f32(sum128);
}

inline vec_i32 vec_dpbusd_epi32(const vec_i32 sum, const vec_i8 vec0, const vec_i8 vec1) {
    const vec_i16 product16 = vec_maddubs_epi16(vec0, vec1);
    const vec_i32 product32 = vec_madd_epi16(product16, vec_set1_epi16(1));
    return vec_add_epi32(sum, product32);
}

#endif