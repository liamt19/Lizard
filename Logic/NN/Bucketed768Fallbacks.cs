
using System.Runtime.Intrinsics;

using static Lizard.Logic.NN.Aliases;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {
        public static int GetEvaluationSSE(Position pos, int outputBucket)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            Bucketed768.ProcessUpdates(pos);

            float* L1Outputs = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            var us   = (short*)(accumulator[pos.ToMove]);
            var them = (short*)(accumulator[Not(pos.ToMove)]);

            ActivateFTSparseSSE(us, them, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs);
            ActivateL2SSE(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL3SSE(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);

            return (int)(L3Output * OutputScale);
        }

        private static void ActivateFTSparseSSE(short* us, short* them, sbyte* weights, float* biases, float* output)
        {
            var ft_zero = _mm_setzero_epi16();
            var ft_one = _mm_set1_epi16(FT_QUANT);

            int nnzCount = 0;
            int offset = 0;

            sbyte* ft_outputs = stackalloc sbyte[L1_SIZE];
            ushort* nnzIndices = stackalloc ushort[L1_SIZE / L1_CHUNK_PER_32];

            //  4 here, not 8. SSE3 variant of vec_nnz_mask only handles 4 ints per chunk.
            Vector128<ushort> baseInc = Vector128.Create((ushort)4);
            Vector128<ushort> baseVec = Vector128<ushort>.Zero;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                short* acc = perspective == 0 ? us : them;

                for (int i = 0; i < L1_PAIR_COUNT; i += (I16_CHUNK_SIZE * 2))
                {
                    var input0a = _mm_load_si128(&acc[i + 0 * I16_CHUNK_SIZE + 0]);
                    var input0b = _mm_load_si128(&acc[i + 1 * I16_CHUNK_SIZE + 0]);

                    var input1a = _mm_load_si128(&acc[i + 0 * I16_CHUNK_SIZE + L1_PAIR_COUNT]);
                    var input1b = _mm_load_si128(&acc[i + 1 * I16_CHUNK_SIZE + L1_PAIR_COUNT]);

                    var clipped0a = _mm_min_epi16(_mm_max_epi16(input0a, ft_zero), ft_one);
                    var clipped0b = _mm_min_epi16(_mm_max_epi16(input0b, ft_zero), ft_one);

                    var clipped1a = _mm_min_epi16(input1a, ft_one);
                    var clipped1b = _mm_min_epi16(input1b, ft_one);

                    var producta = _mm_mulhi_epi16(_mm_slli_epi16(clipped0a, 16 - FT_SHIFT), clipped1a);
                    var productb = _mm_mulhi_epi16(_mm_slli_epi16(clipped0b, 16 - FT_SHIFT), clipped1b);

                    var product_one = _mm_packus_epi16(producta, productb).AsByte();
                    _mm_storeu_si128(&ft_outputs[offset + i], product_one.AsSByte());

                    var nnz_mask = vec_nnz_mask(product_one);

                    for (int j = 0; j < NNZ_OUTPUTS_PER_CHUNK; j++)
                    {
                        int lookup = (nnz_mask >> (j * 8)) & 0xFF;
                        var offsets = NNZLookup[lookup];
                        _mm_storeu_si128(&nnzIndices[nnzCount], _mm_add_epi16(baseVec, offsets));

                        nnzCount += int.PopCount(lookup);
                        baseVec += baseInc;
                    }

                }

                offset += L1_PAIR_COUNT;
            }

            ActivateL1SparseSSE(ft_outputs, weights, biases, output, new Span<ushort>(nnzIndices, nnzCount));
        }

        private static void ActivateL1SparseSSE(sbyte* inputs, sbyte* weights, float* biases, float* output, Span<ushort> nnzIndices)
        {
            var sums = stackalloc Vector128<int>[L2_SIZE / I32_CHUNK_SIZE];

            int nnzCount = nnzIndices.Length;
            int* inputs32 = (int*)(inputs);
            for (int i = 0; i < nnzCount; i++)
            {
                var index = nnzIndices[i];
                var input32 = _mm_set1_epi32(inputs32[index]);
                var weight = (Vector128<sbyte>*)(&weights[index * L1_CHUNK_PER_32 * L2_SIZE]);
                for (int k = 0; k < L2_SIZE / F32_CHUNK_SIZE; k++)
                {
                    sums[k] = vec_dpbusd_epi32(sums[k], input32.AsByte(), weight[k]);
                }
            }

            var zero = _mm_set1_ps(0.0f);
            var one = Vector128<float>.One;

            var sumMul = _mm_set1_ps((1 << FT_SHIFT) / (float)(FT_QUANT * FT_QUANT * L1_QUANT));
            for (int i = 0; i < L2_SIZE / F32_CHUNK_SIZE; ++i)
            {
                var biasVec = _mm_loadu_ps(&biases[i * F32_CHUNK_SIZE]);
                var sumPs = _mm_fmadd_ps(_mm_cvtepi32_ps(sums[i]), sumMul, biasVec);
                var clipped = _mm_min_ps(_mm_max_ps(sumPs, zero), one);
                var squared = _mm_mul_ps(clipped, clipped);
                _mm_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);
            }
        }

        private static void ActivateL2SSE(float* inputs, float* weights, float* biases, float* output)
        {
            var sumVecs = stackalloc Vector128<float>[L3_SIZE / F32_CHUNK_SIZE];

            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
                sumVecs[i] = _mm_loadu_ps(&biases[i * F32_CHUNK_SIZE]);

            for (int i = 0; i < L2_SIZE; ++i)
            {
                var inputVec = _mm_set1_ps(inputs[i]);
                var weight = (Vector128<float>*)(&weights[i * L3_SIZE]);
                for (int j = 0; j < L3_SIZE / F32_CHUNK_SIZE; ++j)
                {
                    sumVecs[j] = vec_mul_add_ps(inputVec, weight[j], sumVecs[j]);
                }
            }

            var zero = _mm_set1_ps(0.0f);
            var one = _mm_set1_ps(1.0f);
            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
            {
                var clipped = _mm_min_ps(_mm_max_ps(sumVecs[i], zero), one);
                var squared = _mm_mul_ps(clipped, clipped);
                _mm_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);
            }
        }

        private static void ActivateL3SSE(float* inputs, float* weights, float bias, ref float output)
        {
            var sumVec = _mm_set1_ps(0.0f);

            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; i++)
            {
                var weightVec = _mm_loadu_ps(&weights[i * F32_CHUNK_SIZE]);
                var inputsVec = _mm_loadu_ps(&inputs[i * F32_CHUNK_SIZE]);
                sumVec = vec_mul_add_ps(inputsVec, weightVec, sumVec);
            }

            output = bias + vec_reduce_add_ps(sumVec);
        }




        public static int GetEvaluationFallback(Position pos, int outputBucket)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            Bucketed768.ProcessUpdates(pos);

            float* L1Outputs = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            var us   = (short*)(accumulator[pos.ToMove]);
            var them = (short*)(accumulator[Not(pos.ToMove)]);

            FallbackActivateFT(us, them, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs);
            FallbackActivateL2(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            FallbackActivateL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);

            return (int)(L3Output * OutputScale);
        }

        private static void FallbackActivateFT(short* us, short* them, sbyte* weights, float* biases, float* output)
        {
            int offset = 0;
            sbyte* ft_outputs = stackalloc sbyte[L1_SIZE];

            for (int perspective = 0; perspective < 2; perspective++)
            {
                short* acc = perspective == 0 ? us : them;

                for (int i = 0; i < L1_PAIR_COUNT; i++)
                {
                    var cl = short.Clamp(acc[i], 0, FT_QUANT);
                    var cr = short.Clamp(acc[i + L1_PAIR_COUNT], 0, FT_QUANT);

                    ft_outputs[i + offset] = (sbyte)((cl * cr) >> FT_SHIFT);
                }

                offset += L1_PAIR_COUNT;
            }

            FallbackActivateL1(ft_outputs, weights, biases, output);
        }

        private static void FallbackActivateL1(sbyte* inputs, sbyte* weights, float* biases, float* output)
        {
            var sums = stackalloc int[L2_SIZE];

            for (int i = 0; i < L1_SIZE; i++)
            {
                var inp = inputs[i];
                if (inp == 0)
                    continue;

                for (int j = 0; j < L2_SIZE; j++)
                {
                    sums[j] += ((int)inp) * ((int)weights[j * L1_SIZE + i]);
                }
            }

            const float MUL = (1 << FT_SHIFT) / (float)(FT_QUANT * FT_QUANT * L1_QUANT);
            for (int j = 0; j < L2_SIZE; j++)
            {
                var c = float.Clamp((sums[j] * MUL) + biases[j], 0.0f, 1.0f);
                output[j] = (c * c);
            }
        }

        private static void FallbackActivateL2(float* inputs, float* weights, float* biases, float* output)
        {
            var sums = stackalloc float[L3_SIZE];

            for (int i = 0; i < L3_SIZE; ++i)
                sums[i] = biases[i];

            for (int i = 0; i < L2_SIZE; ++i)
            {
                for (int j = 0; j < L3_SIZE; ++j)
                    sums[j] += (inputs[i] * weights[i * L3_SIZE + j]);
            }

            for (int j = 0; j < L3_SIZE; j++)
            {
                var c = float.Clamp(sums[j], 0.0f, 1.0f);
                output[j] = (c * c);
            }
        }

        private static void FallbackActivateL3(float* inputs, float* weights, float bias, ref float output)
        {
            var sum = 0.0f;

            for (int i = 0; i < L3_SIZE; i++)
            {
                sum = (weights[i] * inputs[i]) + sum;
            }

            output = bias + sum;
        }

    }
}
