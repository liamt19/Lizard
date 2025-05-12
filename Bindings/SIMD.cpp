
#include <immintrin.h>
#include <xmmintrin.h>
#include <stdint.h>
#include <iostream>
#include <array>
#include <span>
#include <bit>

#include "simd.h"
#include "arch.h"
#include "defs.h"

#if defined(_MSC_VER)
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT extern "C"
#endif

DLL_EXPORT void SetupNNZ();

DLL_EXPORT void EvaluateBound(const i16* us, const i16* them,
                              const    i8* L1Weights, const float* L1Biases,
                              const float* L2Weights, const float* L2Biases,
                              const float* L3weights, const float  L3bias,
                              int& L3Output);

struct NNZTable {
    __m128i Entries[256];
};
NNZTable nnzTable;

DLL_EXPORT void SetupNNZ()  {
    for (u32 i = 0; i < 256; i++) {
        u16* ptr = reinterpret_cast<u16*>(&nnzTable.Entries[i]);

        u32 j = i;
        u32 k = 0;
        while (j != 0) {
            u32 lsbIndex = std::countr_zero(j);
            j &= j - 1;
            ptr[k++] = (u16)lsbIndex;
        }
    }
}

DLL_EXPORT void EvaluateBound(const i16* us, const i16* them,
                              const i8* L1Weights, const float* L1Biases,
                              const float* L2Weights, const float* L2Biases,
                              const float* L3Weights, const float L3Bias,
                              i32& L3Output) {

    i8 ft_outputs[L1_SIZE];
    float L1Outputs[L2_SIZE];
    float L2Outputs[L3_SIZE];

    i32 nnzCount = 0;
    u16 nnzIndices[L1_SIZE / L1_CHUNK_PER_32];

    {
        const auto zero = vec_setzero_epi16();
        const auto one = vec_set1_epi16(FT_QUANT);

        i32 offset = 0;

        const vec_128i baseInc = _mm_set1_epi16(u16(NNZ_INCREMENT));
        vec_128i baseVec = _mm_setzero_si128();

        for (const auto acc : { us, them }) {
            for (i32 i = 0; i < L1_PAIR_COUNT; i += (I16_CHUNK_SIZE * 2)) {
                const auto input0a = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 0 * I16_CHUNK_SIZE + 0]));
                const auto input0b = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 1 * I16_CHUNK_SIZE + 0]));

                const auto input1a = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 0 * I16_CHUNK_SIZE + L1_PAIR_COUNT]));
                const auto input1b = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 1 * I16_CHUNK_SIZE + L1_PAIR_COUNT]));

                const auto clipped0a = vec_min_epi16(vec_max_epi16(input0a, zero), one);
                const auto clipped0b = vec_min_epi16(vec_max_epi16(input0b, zero), one);

                const auto clipped1a = vec_min_epi16(input1a, one);
                const auto clipped1b = vec_min_epi16(input1b, one);

                const auto producta = vec_mulhrs_epi16(vec_slli_epi16(clipped0a, 16 - FT_SHIFT - 1), clipped1a);
                const auto productb = vec_mulhrs_epi16(vec_slli_epi16(clipped0b, 16 - FT_SHIFT - 1), clipped1b);

                const auto prod = vec_packus_epi16(producta, productb);
                vec_storeu_epi8(reinterpret_cast<vec_i8*>(&ft_outputs[offset + i]), prod);

                const auto nnz_mask = vec_nnz_mask(prod);

                for (i32 j = 0; j < NNZ_OUTPUTS_PER_CHUNK; j++) {
                    i32 lookup = (nnz_mask >> (j * 8)) & 0xFF;
                    auto offsets = nnzTable.Entries[lookup];
                    _mm_storeu_si128(reinterpret_cast<vec_128i*>(&nnzIndices[nnzCount]), _mm_add_epi16(baseVec, offsets));

                    nnzCount += std::popcount(static_cast<u32>(lookup));
                    baseVec = _mm_add_epi16(baseVec, baseInc);
                }
            }

            offset += L1_PAIR_COUNT;
        }
    }


    {
        const auto inputs = ft_outputs;
        const auto& weights = L1Weights;
        const auto& biases = L1Biases;
        const auto outputs = L1Outputs;

        vec_i32 sums[L2_SIZE / I32_CHUNK_SIZE]{};

        const auto inputs32 = (i32*)(inputs);
        for (i32 i = 0; i < nnzCount; i++) {
            const auto index = nnzIndices[i];
            const auto input32 = vec_set1_epi32(inputs32[index]);
            const auto weight = reinterpret_cast<const vec_i8*>(&weights[index * L1_CHUNK_PER_32 * L2_SIZE]);
            for (i32 k = 0; k < L2_SIZE / F32_CHUNK_SIZE; k++)
                sums[k] = vec_dpbusd_epi32(sums[k], input32, weight[k]);
        }

        const auto sumMul = vec_set1_ps(L1_MUL);

        const auto zero = vec_set1_ps(0.0f);
        const auto one = vec_set1_ps(1.0f);
        for (i32 i = 0; i < L2_SIZE / F32_CHUNK_SIZE; ++i) {
            const auto biasVec = vec_loadu_ps(&biases[i * F32_CHUNK_SIZE]);
            const auto sumPs = vec_fmadd_ps(vec_cvtepi32_ps(sums[i]), sumMul, biasVec);
            const auto clipped = vec_min_ps(vec_max_ps(sumPs, zero), one);
            const auto squared = vec_mul_ps(clipped, clipped);
            vec_storeu_ps(&outputs[i * F32_CHUNK_SIZE], squared);
        }
    }


    {
        const auto inputs = L1Outputs;
        const auto& weights = L2Weights;
        const auto& biases = L2Biases;
        const auto outputs = L2Outputs;

        vec_ps sumVecs[L3_SIZE / F32_CHUNK_SIZE];

        for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
            sumVecs[i] = vec_loadu_ps(&biases[i * F32_CHUNK_SIZE]);

        for (i32 i = 0; i < L2_SIZE; ++i) {
            const auto inputVec = vec_set1_ps(inputs[i]);
            const auto weight = reinterpret_cast<const vec_ps*>(&weights[i * L3_SIZE]);
            for (i32 j = 0; j < L3_SIZE / F32_CHUNK_SIZE; ++j)
                sumVecs[j] = vec_fmadd_ps(inputVec, weight[j], sumVecs[j]);
        }

        const auto zero = vec_set1_ps(0.0f);
        const auto one = vec_set1_ps(1.0f);
        for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i) {
            const auto clipped = vec_min_ps(vec_max_ps(sumVecs[i], zero), one);
            const auto squared = vec_mul_ps(clipped, clipped);
            vec_storeu_ps(&outputs[i * F32_CHUNK_SIZE], squared);
        }
    }


    {
        const auto& inputs = L2Outputs;
        const auto& weights = L3Weights;
        const auto bias = L3Bias;

        constexpr auto SUM_COUNT = 64 / sizeof(vec_ps);
        vec_ps sumVecs[SUM_COUNT]{};

        for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; i++) {
            const auto weightVec = vec_loadu_ps(&weights[i * F32_CHUNK_SIZE]);
            const auto inputsVec = vec_loadu_ps(&inputs[i * F32_CHUNK_SIZE]);
            sumVecs[i % SUM_COUNT] = vec_fmadd_ps(inputsVec, weightVec, sumVecs[i % SUM_COUNT]);
        }

        L3Output = static_cast<i32>((bias + vec_hsum_ps(sumVecs)) * OutputScale);
    }

}