using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Net;
using System.Numerics;
using System.Reflection;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public unsafe class AffineTransform
    {
        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        public readonly int PaddedInputDimensions;

        public readonly Vector256<int>* Biases;
        public readonly Vector256<sbyte>* Weights;

        public const int OutputSimdWidth = SimdWidth / 4;

        private readonly int OutputNumChunks;
        private readonly int NormalNumChunks;
        private readonly int NumRegs;
        private readonly int WeightOffset;

        /// <summary>
        /// Creates a new Affine layer, which takes input from the <see cref="ClippedReLU"/> layer that came before it
        /// and outputs <paramref name="outDims"/> numbers.
        /// </summary>
        /// <param name="inDims">The length of the Span that this layer will use as input to propagate</param>
        /// <param name="outDims">The length of the Span that this layer returns as output</param>
        public AffineTransform(int inDims, int outDims)
        {
            InputDimensions = inDims;
            OutputDimensions = outDims;

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);
            BufferSize = CeilToMultiple((short)OutputDimensions, MaxSimdWidth);
            BufferSizeBytes = BufferSize * sizeof(int);

            OutputNumChunks = PaddedInputDimensions / SimdWidth;
            NormalNumChunks = CeilToMultiple((short)InputDimensions, 8) / 4;
            NumRegs = OutputDimensions / OutputSimdWidth;
            WeightOffset = (OutputDimensions * 4) / VSize.SByte;

            Weights = (Vector256<sbyte>*)  AlignedAllocZeroed((nuint)((OutputDimensions * PaddedInputDimensions) / VSize.SByte * 32), AllocAlignment);
            Biases  = (Vector256<int>*)    AlignedAllocZeroed((nuint)(Math.Max(1, (OutputDimensions) / VSize.Int) * 32),              AllocAlignment);

            if (OutputDimensions % OutputSimdWidth != 0 && OutputDimensions != 1)
            {
                throw new Exception("AffineTransform(" + inDims + ", " + outDims + ") has a bad size! " +
                    "The output dimensions must either be divisible by " + OutputSimdWidth + " or equal to 1.");
            }
        }


        public void PropagateNormal(Span<sbyte> input, Span<int> output)
        {
            int* inputPtr = (int*) Unsafe.AsPointer(ref input[0]);
            int* outputPtr = (int*) Unsafe.AsPointer(ref output[0]);

            Span<Vector256<int>> outs = stackalloc Vector256<int>[NumRegs];
            for (int k = 0; k < NumRegs; k++)
            {
                outs[k] = Biases[k];
            }

            const int vectorStride = VSize.Int;
            for (int i = 0; i < NormalNumChunks; i += 2)
            {
                //  input contains 32 sbyte's, which corresponds to 8 integers.
                //  We want to give m256_add_dpbusd_epi32x2 a vector of <inp[1], inp[2], inp[3], inp[4]>, repeated 8 times.
                //  Do this by casting the first 4 sbyte's into an int, broadcasting that int into a Vector256<int>,
                //  and converting that Vector256<int> into Vector256<byte>

                Vector256<byte> in0 = Avx2.BroadcastScalarToVector256(inputPtr + i + 0).AsByte();
                Vector256<byte> in1 = Avx2.BroadcastScalarToVector256(inputPtr + i + 1).AsByte();

                for (int k = 0; k < NumRegs; k++)
                {
                    var b0 = Weights[((i + 0) * WeightOffset) + k];
                    var b1 = Weights[((i + 1) * WeightOffset) + k];
                    m256_add_dpbusd_epi32x2(ref outs[k], in0, in1, b0, b1);
                }
            }
            
            for (int k = 0; k < NumRegs; k++)
            {
                Avx.Store(outputPtr + (k * vectorStride), outs[k]);
            }
        }

        public void PropagateOutput(Span<sbyte> input, Span<int> output)
        {
            const int vectorStride = VSize.SByte;
            byte* inputPtr = (byte*) Unsafe.AsPointer(ref input[0]);

            Vector256<int> sum0 = Vector256<int>.Zero;

            for (int j = 0; j < OutputNumChunks; j++)
            {
                Vector256<byte> inp = Avx.LoadDquVector256(inputPtr + (j * vectorStride));
                m256_add_dpbusd_epi32(ref sum0, inp, Weights[j]);
            }

            output[0] = m256_hadd(sum0, Biases[0][0]);
        }


        public bool ReadParameters(BinaryReader br)
        {

            int[] _Biases = new int[OutputDimensions];
            sbyte[] _Weights = new sbyte[OutputDimensions * PaddedInputDimensions];

            try
            {

                for (int i = 0; i < OutputDimensions; i++)
                {
                    _Biases[i] = br.ReadInt32();
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i++)
                {
                    uint cursedIndex = GetSmallWeightIndex(i);
                    _Weights[cursedIndex] = br.ReadSByte();
                }

                fixed (int* biasPtr = _Biases)
                {
                    for (int i = 0; i < OutputDimensions; i += VSize.Int)
                    {
                        if (OutputDimensions == 1)
                        {
                            Biases[i / VSize.Int] = Vector256.Create(biasPtr[0], 0, 0, 0, 0, 0, 0, 0);
                        }
                        else
                        {
                            Biases[i / VSize.Int] = Avx.LoadDquVector256(biasPtr + i);
                        }
                    }
                }

                fixed (sbyte* weightPtr = _Weights)
                {
                    for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i += VSize.SByte)
                    {
                        Weights[i / VSize.SByte] = Avx.LoadDquVector256(weightPtr + i);
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return false;
            }

            return true;
        }


        public uint GetHashValue(uint prevHash)
        {
            uint hashValue = 0xCC03DAE4u;
            hashValue += (uint)OutputDimensions;

            hashValue ^= prevHash >> 1;
            hashValue ^= prevHash << 31;

            return hashValue;
        }


        [MethodImpl(Inline)]
        private uint GetSmallWeightIndex(int i)
        {
            return (uint) ((i / 4) % (PaddedInputDimensions / 4) * OutputDimensions * 4 + i / PaddedInputDimensions * 4 + i % 4);
        }


        public static void m256_add_dpbusd_epi32(ref Vector256<int> acc, Vector256<byte> a, Vector256<sbyte> b)
        {
            Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a, b);
            acc = Avx2.Add(acc, Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One));
        }

        private static int m256_hadd(Vector256<int> sum, int bias)
        {
            const int _MM_PERM_BADC = 0x4E;
            const int _MM_PERM_CDAB = 0xB1;

            Vector128<int> lo = Avx2.ExtractVector128(sum, 0);
            
            var sum128 = Sse2.Add(lo, Avx2.ExtractVector128(sum, 1));
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_BADC));
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_CDAB));
            return Sse2.ConvertToInt32(sum128) + bias;
        }

        private static void m256_add_dpbusd_epi32x2(ref Vector256<int> acc, Vector256< byte> a0, Vector256< byte> a1, 
                                                                            Vector256<sbyte> b0, Vector256<sbyte> b1)
        {
            Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a0, b0);
            Vector256<short> product1 = Avx2.MultiplyAddAdjacent(a1, b1);

            product0 = Avx2.AddSaturate(product0, product1);
            Vector256<int> product0f = Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One);

            acc = Avx2.Add(acc, product0f);
        }

    }
}
