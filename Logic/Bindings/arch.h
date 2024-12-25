#pragma once

#include "defs.h"
#include "simd.h"

constexpr auto INPUT_BUCKETS = 14;
constexpr auto INPUT_SIZE = 768;
constexpr auto L1_SIZE = 2048;
constexpr auto L2_SIZE = 16;
constexpr auto L3_SIZE = 32;
constexpr auto OUTPUT_BUCKETS = 8;

constexpr auto FT_QUANT = 255;
constexpr auto FT_SHIFT = 10;
constexpr auto L1_QUANT = 64;
constexpr auto OutputScale = 400;

constexpr auto U8_CHUNK_SIZE = sizeof(vec_i8) / sizeof(u8);
constexpr auto I16_CHUNK_SIZE = sizeof(vec_i16) / sizeof(i16);
constexpr auto I32_CHUNK_SIZE = sizeof(vec_i32) / sizeof(i32);
constexpr auto F32_CHUNK_SIZE = sizeof(vec_ps) / sizeof(float);

constexpr auto NNZ_INPUT_SIMD_WIDTH = sizeof(vec_i32) / sizeof(i32);
constexpr auto NNZ_CHUNK_SIZE = (NNZ_INPUT_SIMD_WIDTH > 8) ? NNZ_INPUT_SIMD_WIDTH : 8;
constexpr auto NNZ_OUTPUTS_PER_CHUNK = NNZ_CHUNK_SIZE / 8;

constexpr auto L1_CHUNK_PER_32 = sizeof(i32) / sizeof(i8);
constexpr auto L1_PAIR_COUNT = L1_SIZE / 2;

constexpr auto SIMD_CHUNKS = L1_SIZE / (sizeof(vec_i16) / sizeof(i16));

constexpr float L1_MUL = (1 << FT_SHIFT) / static_cast<float>(FT_QUANT * FT_QUANT * L1_QUANT);

constexpr auto N_FTW = INPUT_SIZE * L1_SIZE * INPUT_BUCKETS;
constexpr auto N_FTB = L1_SIZE;
constexpr auto N_L1W = OUTPUT_BUCKETS * L1_SIZE * L2_SIZE;
constexpr auto N_L1B = OUTPUT_BUCKETS * L2_SIZE;
constexpr auto N_L2W = OUTPUT_BUCKETS * L2_SIZE * L3_SIZE;
constexpr auto N_L2B = OUTPUT_BUCKETS * L3_SIZE;
constexpr auto N_L3W = OUTPUT_BUCKETS * L3_SIZE;
constexpr auto N_L3B = OUTPUT_BUCKETS;