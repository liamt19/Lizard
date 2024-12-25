
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

#define DLL_EXPORT extern "C" __declspec(dllexport)

DLL_EXPORT void SetupNNZ();

DLL_EXPORT void ActivateFTSparse(i16* _us, i16* _them, i8* _weights, float* _biases, float* _output);
void ActivateL1Sparse(Span<i8> inputs, Span<i8> weights, Span<float> biases, Span<float> output, Span<u16> nnzIndices, const i32 nnzCount);
DLL_EXPORT void ActivateL2(float* _inputs, float* _weights, float* _biases, float* _output);
DLL_EXPORT void ActivateL3(float* _inputs, float* _weights, const float bias, float& output);
DLL_EXPORT void ActivateL2_AndL3(float* _inputs, float* _weights, float* _biases, float* _L3weights, const float L3bias, float& output);


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

DLL_EXPORT void ActivateFTSparse(i16* _us, i16* _them, i8* _weights, float* _biases, float* _output) {
    Span<i16> us(_us, L1_SIZE);
    Span<i16> them(_them, L1_SIZE);
    Span<i8> weights(_weights, L1_SIZE * L2_SIZE);
    Span<float> biases(_biases, L2_SIZE);
    Span<float> output(_output, L2_SIZE);
    
    const auto ft_zero = vec_setzero_epi16();
    const auto ft_one = vec_set1_epi16(FT_QUANT);

    i32 nnzCount = 0;
    i32 offset = 0;

    i8 ft_outputs[L1_SIZE] alignas(32);
    u16 nnzIndices[L1_SIZE / L1_CHUNK_PER_32] alignas(32);

    const vec_128i baseInc = _mm_set1_epi16(u16(8));
    vec_128i baseVec = _mm_setzero_si128();


    for (const auto acc : { us, them }) {
        for (i32 i = 0; i < L1_PAIR_COUNT; i += (I16_CHUNK_SIZE * 2)) {
            const auto input0a = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 0 * I16_CHUNK_SIZE + 0]));
            const auto input0b = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 1 * I16_CHUNK_SIZE + 0]));

            const auto input1a = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 0 * I16_CHUNK_SIZE + L1_PAIR_COUNT]));
            const auto input1b = vec_load_epi16(reinterpret_cast<const vec_i16*>(&acc[i + 1 * I16_CHUNK_SIZE + L1_PAIR_COUNT]));

            const auto clipped0a = vec_min_epi16(vec_max_epi16(input0a, ft_zero), ft_one);
            const auto clipped0b = vec_min_epi16(vec_max_epi16(input0b, ft_zero), ft_one);

            const auto clipped1a = vec_min_epi16(input1a, ft_one);
            const auto clipped1b = vec_min_epi16(input1b, ft_one);

            const auto producta = vec_mulhi_epi16(vec_slli_epi16(clipped0a, 16 - FT_SHIFT), clipped1a);
            const auto productb = vec_mulhi_epi16(vec_slli_epi16(clipped0b, 16 - FT_SHIFT), clipped1b);

            const auto product_one = vec_packus_epi16(producta, productb);
            vec_storeu_epi8(reinterpret_cast<vec_i8*>(&ft_outputs[offset + i]), product_one);

            const auto nnz_mask = vec_nnz_mask(product_one);

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

    ActivateL1Sparse(Span<i8>(ft_outputs), weights, biases, output, Span<u16>(nnzIndices), nnzCount);
}


void ActivateL1Sparse(Span<i8> inputs, Span<i8> weights, Span<float> biases, Span<float> output, Span<u16> nnzIndices, const i32 nnzCount) {
    vec_i32 sums[L2_SIZE / I32_CHUNK_SIZE] alignas(32) = {};

    const auto inputs32 = (i32*)(inputs.data());
    for (i32 i = 0; i < nnzCount; i++) {
        const auto index = nnzIndices[i];
        const auto input32 = vec_set1_epi32(inputs32[index]);
        const auto weight = reinterpret_cast<const vec_i8*>(&weights[index * L1_CHUNK_PER_32 * L2_SIZE]);
        for (i32 k = 0; k < L2_SIZE / F32_CHUNK_SIZE; k++)
            sums[k] = vec_dpbusd_epi32(sums[k], input32, weight[k]);
    }

    const auto zero = vec_set1_ps(0.0f);
    const auto one = vec_set1_ps(1.0f);

    const auto sumMul = vec_set1_ps(L1_MUL);
    for (i32 i = 0; i < L2_SIZE / F32_CHUNK_SIZE; ++i) {
        const auto biasVec = vec_loadu_ps(&biases[i * F32_CHUNK_SIZE]);
        const auto sumPs = vec_fmadd_ps(vec_cvtepi32_ps(sums[i]), sumMul, biasVec);
        const auto clipped = vec_min_ps(vec_max_ps(sumPs, zero), one);
        const auto squared = vec_mul_ps(clipped, clipped);
        vec_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);
    }
}


DLL_EXPORT void ActivateL2(float* _inputs, float* _weights, float* _biases, float* _output) {
    Span<float> inputs(_inputs, L2_SIZE);
    Span<float> weights(_weights, L2_SIZE * L3_SIZE);
    Span<float> biases(_biases, L2_SIZE);
    Span<float> output(_output, L3_SIZE);
    
    vec_ps sumVecs[L3_SIZE / F32_CHUNK_SIZE] alignas(32);

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
        vec_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);
    }
}


DLL_EXPORT void ActivateL3(float* _inputs, float* _weights, const float bias, float& output) {
    Span<float> inputs(_inputs, L3_SIZE);
    Span<float> weights(_weights, L3_SIZE);

    auto sumVec = vec_set1_ps(0.0f);

    for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; i++) {
        const auto weightVec = vec_loadu_ps(&weights[i * F32_CHUNK_SIZE]);
        const auto inputsVec = vec_loadu_ps(&inputs[i * F32_CHUNK_SIZE]);
        sumVec = vec_fmadd_ps(inputsVec, weightVec, sumVec);
    }

    output = bias + vec_hsum_ps(sumVec);
}


DLL_EXPORT void ActivateL2_AndL3(float* _inputs, float* _weights, float* _biases, float* _L3weights, const float L3bias, float& output) {
    Span<float> inputs(_inputs, L2_SIZE);
    Span<float> weights(_weights, L2_SIZE * L3_SIZE);
    Span<float> biases(_biases, L2_SIZE);
    Span<float> L3weights(_L3weights, L3_SIZE);

    vec_ps sumVecs[L3_SIZE / F32_CHUNK_SIZE] alignas(32);

    for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
        sumVecs[i] = vec_loadu_ps(&biases[i * F32_CHUNK_SIZE]);

    for (i32 i = 0; i < L2_SIZE; ++i) {
        const auto inputVec = vec_set1_ps(inputs[i]);
        const auto weight = reinterpret_cast<const vec_ps*>(&weights[i * L3_SIZE]);
        for (i32 j = 0; j < L3_SIZE / F32_CHUNK_SIZE; ++j)
            sumVecs[j] = vec_fmadd_ps(inputVec, weight[j], sumVecs[j]);
    }

    auto l3Sum = vec_set1_ps(0.0f);
    const auto zero = vec_set1_ps(0.0f);
    const auto one = vec_set1_ps(1.0f);
    for (i32 i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i) {
        const auto clipped = vec_min_ps(vec_max_ps(sumVecs[i], zero), one);
        const auto squared = vec_mul_ps(clipped, clipped);

        const auto weightVec = vec_loadu_ps(&L3weights[i * F32_CHUNK_SIZE]);
        l3Sum = vec_fmadd_ps(squared, weightVec, l3Sum);
    }

    output = L3bias + vec_hsum_ps(l3Sum);
}

