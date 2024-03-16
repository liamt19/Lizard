using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

//  Adapted from Berserk: https://github.com/jhonnold/berserk/blob/3e110d526bf41af2d2d21f90802248c1f6f599dd/src/nn/evaluate.c#L601

namespace Lizard.Logic.NN
{
    public static unsafe partial class Layered768
    {
        private const int SPARSE_CHUNK_SIZE = 4;

        private static void InputReLU(Position pos, sbyte* outputs)
        {
            Accumulator* acc = pos.State->Accumulator;
            if (acc->NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (acc->NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

            const int WIDTH = VSize.Short;
            const int CHUNKS = N_HIDDEN / WIDTH;
            int* views = stackalloc int[] { pos.ToMove, Not(pos.ToMove) };

            for (int v = 0; v < 2; v++)
            {
                var input = (Vector256<short>*)(*acc)[v];
                var output = (Vector256<sbyte>*)&outputs[N_HIDDEN * v];

                for (int i = 0; i < CHUNKS / 2; i += 2)
                {
                    var s0 = Avx2.ShiftRightArithmetic(input[2 * i + 0], 5);
                    var s1 = Avx2.ShiftRightArithmetic(input[2 * i + 1], 5);
                    var s2 = Avx2.ShiftRightArithmetic(input[2 * i + 2], 5);
                    var s3 = Avx2.ShiftRightArithmetic(input[2 * i + 3], 5);

                    output[i + 0] = Avx2.Max(Avx2.PackSignedSaturate(s0, s1), Vector256<sbyte>.Zero);
                    output[i + 1] = Avx2.Max(Avx2.PackSignedSaturate(s2, s3), Vector256<sbyte>.Zero);
                }
            }
        }

        private static void ForwardL1(float* src, float* dst)
        {
            const int Chunks = N_L1 / VSize.Float;
            Vector256<float>* temp = stackalloc Vector256<float>[Chunks];

            Vector256<float>* biases = (Vector256<float>*)L1_BIASES;
            Vector256<float>* input = (Vector256<float>*)src;
            Vector256<float>* output = (Vector256<float>*)dst;

            for (int i = 0; i < Chunks; i++)
                temp[i] = biases[i];

            for (int i = 0; i < N_HIDDEN; ++i)
            {
                var inV = Avx2.BroadcastScalarToVector256(src + i);
                int idx = (i * N_L1);
                for (int j = 0; j < Chunks; ++j)
                {
                    temp[j] = Avx.Add(temp[j], Avx.Multiply(inV, Avx.LoadVector256(L1_WEIGHTS + idx + (j * VSize.Float))));
                }
            }

            for (int i = 0; i < Chunks; i++)
                output[i] = Avx2.Max(temp[i], Vector256<float>.Zero);
        }

        private static void ForwardL2(float* src, float* dst)
        {
            const int Chunks = N_L2 / VSize.Float;
            Vector256<float>* temp = stackalloc Vector256<float>[Chunks];

            Vector256<float>* biases = (Vector256<float>*)L2_BIASES;
            Vector256<float>* input = (Vector256<float>*)src;
            Vector256<float>* output = (Vector256<float>*)dst;

            for (int i = 0; i < Chunks; i++)
                temp[i] = biases[i];

            for (int i = 0; i < N_L1; ++i)
            {
                var inV = Avx2.BroadcastScalarToVector256(src + i);
                int idx = (i * N_L2);
                for (int j = 0; j < Chunks; ++j)
                {
                    temp[j] = Avx.Add(temp[j], Avx.Multiply(inV, Avx.LoadVector256(L2_WEIGHTS + idx + (j * VSize.Float))));
                }
            }

            for (int i = 0; i < Chunks; i++)
                output[i] = Avx2.Max(temp[i], Vector256<float>.Zero);
        }

        private static float ForwardOutput(float* src)
        {
            const int Chunks = N_L2 / VSize.Float;
            Vector256<float>* input = (Vector256<float>*)src;

            Vector256<float> a0 = Vector256<float>.Zero;
            for (int i = 0; i < Chunks; i++)
                a0 = Fma.MultiplyAdd(input[i], OutputWeights[i], a0);

            //return OutputScale * (Vector256.Sum(a0) + OutputBias);

            Vector128<float> a4 = Sse.Add(Avx.ExtractVector128(a0, 0), Avx.ExtractVector128(a0, 1));
            Vector128<float> a2 = Sse.Add(a4, Sse.MoveHighToLow(a4, a4));
            Vector128<float> a1 = Sse.Add(a2, Sse.Shuffle(a2, a2, 0x1));

            //return OutputScale * ((a1[0] * 2) + OutputBias);
            return OutputScale * (a1[0] + OutputBias);
        }










        private static void SetupLookupTable()
        {
            ushort[] temp = new ushort[8];
            for (ulong i = 0; i < 256; i++)
            {
                ulong j = i;
                int k = 0;
                while (j != 0)
                    temp[k++] = (ushort)poplsb(&j);

                LookupIndices[i] = Vector128.Create(temp);
            }
        }

        public static void m256_add_dpbusd_epi32(Vector256<int>* acc, Vector256<byte> a, Vector256<sbyte> b)
        {
            if (AvxVnni.IsSupported)
            {
                *acc = AvxVnni.MultiplyWideningAndAdd(*acc, a, b);
            }
            else
            {
                Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a, b);
                *acc = Avx2.Add(*acc, Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One));
            }
        }

        private static void m256_add_dpbusd_epi32x2(Vector256<int>* acc, Vector256<byte> a0, Vector256<byte> a1,
                                                                         Vector256<sbyte> b0, Vector256<sbyte> b1)
        {
            if (AvxVnni.IsSupported)
            {
                *acc = AvxVnni.MultiplyWideningAndAdd(*acc, a0, b0);
                *acc = AvxVnni.MultiplyWideningAndAdd(*acc, a1, b1);
            }
            else
            {
                Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a0, b0);
                Vector256<short> product1 = Avx2.MultiplyAddAdjacent(a1, b1);

                product0 = Avx2.AddSaturate(product0, product1);
                Vector256<int> product0f = Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One);

                *acc = Avx2.Add(*acc, product0f);
            }
        }


        //private static void m256_add_dpbusd_epi32(__m256i* acc, __m256i a, __m256i b)
        //{
        //    __m256i p0 = _mm256_maddubs_epi16(a, b);
        //    p0 = _mm256_madd_epi16(p0, _mm256_set1_epi16(1));
        //    *acc = _mm256_add_epi32(*acc, p0);
        //}

        //private static void m256_add_dpbusd_epi32x2(__m256i* acc, __m256i a0, __m256i b0, __m256i a1, __m256i b1)
        //{
        //    __m256i p0 = _mm256_maddubs_epi16(a0, b0);
        //    __m256i p1 = _mm256_maddubs_epi16(a1, b1);

        //    p0 = _mm256_madd_epi16(_mm256_add_epi16(p0, p1), _mm256_set1_epi16(1));
        //    *acc = _mm256_add_epi32(*acc, p0);
        //}

        private static uint NNZ(Vector256<int> chunk)
        {
            return (uint)Avx.MoveMask(Avx2.CompareGreaterThan(chunk, Vector256<int>.Zero).AsSingle());
        }

        private static int FindNNZ(ushort* dst, int* inputs, int chunks)
        {
            const int IN_WIDTH = VSize.Int;
            const int CHUNK_SIZE = IN_WIDTH;
            int NUM_CHUNKS = chunks / CHUNK_SIZE;
            const int IN_PER_CHUNK = CHUNK_SIZE / IN_WIDTH;
            const int OUT_PER_CHUNK = CHUNK_SIZE / 8;

            Vector256<int>* input = (Vector256<int>*)inputs;

            int count = 0;

            Vector128<ushort> Eight = Vector128.Create((ushort)8);
            Vector128<ushort> baseVal = Vector128<ushort>.Zero;

            for (int i = 0; i < NUM_CHUNKS; i++)
            {
                uint nnz = 0;

                for (int j = 0; j < IN_PER_CHUNK; j++)
                {
                    Vector256<int> chunk = Avx.LoadDquVector256(inputs + (((i * IN_PER_CHUNK) + j) * VSize.Int));
                    Vector256<int> cmpgt = Avx2.CompareGreaterThan(chunk, Vector256<int>.Zero);
                    int mask = Avx.MoveMask(cmpgt.AsSingle());
                    nnz |= (uint)mask << (j * IN_WIDTH);
                }

                for (int j = 0; j < OUT_PER_CHUNK; j++)
                {
                    var lookup = (nnz >> (j * 8)) & 0xFF;
                    Vector128<ushort> offsets = LookupIndices[lookup];
                    var toStore = Sse2.Add(baseVal, offsets);
                    Sse2.Store(dst + count, toStore);
                    count += (int)popcount(lookup);
                    baseVal = Sse2.Add(baseVal, Eight);
                }
            }

            return count;
        }

        private static void L1AffineReLU(sbyte* src, float* dst)
        {
            const int OUT_WIDTH = VSize.Int;
            const int NUM_CHUNKS = N_L1 / SPARSE_CHUNK_SIZE;
            const int OUT_CC = N_L2 / OUT_WIDTH;

            int* in32 = (int*)src;
            Vector256<int>* biases = (Vector256<int>*)L1_BIASES;
            Vector256<short>* output = (Vector256<short>*)dst;

            ushort* nnz = stackalloc ushort[NUM_CHUNKS];
            int count = FindNNZ(nnz, in32, NUM_CHUNKS);


            Vector256<int>* regs = stackalloc Vector256<int>[OUT_CC];
            for (int k = 0; k < OUT_CC; k++)
                regs[k] = biases[k];

            int i = 0;
            for (; i + 1 < count; i += 2)
            {
                ushort i0 = nnz[i + 0];
                ushort i1 = nnz[i + 1];

                Vector256<byte> f0 = Avx2.BroadcastScalarToVector256(in32 + i0).AsByte();
                Vector256<sbyte> f1 = Avx2.BroadcastScalarToVector256(in32 + i1).AsSByte();

                Vector256<byte>* c0 = (Vector256<byte>*) &L1_WEIGHTS[i0 * N_L2 * SPARSE_CHUNK_SIZE];
                Vector256<sbyte>* c1 = (Vector256<sbyte>*) &L1_WEIGHTS[i1 * N_L2 * SPARSE_CHUNK_SIZE];

                for (int j = 0; j < OUT_CC; j++)
                    m256_add_dpbusd_epi32x2(regs + j, f0, c0[j], f1, c1[j]);
            }

            if (i < count)
            {
                ushort i0 = nnz[i];
                Vector256<byte> f0 = Avx2.BroadcastScalarToVector256(in32 + i0).AsByte();
                Vector256<sbyte>* c0 = (Vector256<sbyte>*)&L1_WEIGHTS[i0 * N_L2 * SPARSE_CHUNK_SIZE];

                for (int j = 0; j < OUT_CC; j++)
                    m256_add_dpbusd_epi32(regs + j, f0, c0[j]);
            }
            
            for (i = 0; i < OUT_CC; i++)
                output[i] = Avx2.ConvertToVector256Single(Avx2.Max(regs[i], Vector256<int>.Zero)).AsInt16();
        }

        private static uint GetWeightIndex(int idx)
        {
            return (uint)(((idx / SPARSE_CHUNK_SIZE) % (N_L1 / SPARSE_CHUNK_SIZE) * N_L2 * SPARSE_CHUNK_SIZE) 
                    + (idx / N_L1 * SPARSE_CHUNK_SIZE) 
                    + (idx % SPARSE_CHUNK_SIZE));
        }
    }
}