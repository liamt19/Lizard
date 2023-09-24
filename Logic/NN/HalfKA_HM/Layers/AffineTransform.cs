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

        public readonly bool IsLarge;

        public const int OutputSimdWidth = SimdWidth / 4;

        private const uint MaxNumOutputRegs = 8;
        private const uint SmallBlockSize = SimdWidth;

        private readonly uint NumOutputRegs;
        private readonly uint BigBlockSize;
        private readonly uint NumSmallBlocksInBigBlock;
        private readonly uint NumSmallBlocksPerOutput;
        private readonly uint NumBigBlocks;


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

            Weights = (Vector256<sbyte>*)  AlignedAllocZeroed((nuint)((OutputDimensions * PaddedInputDimensions) / VSize.SByte * 32), AllocAlignment);
            Biases  = (Vector256<int>*)    AlignedAllocZeroed((nuint)(Math.Max(1, (OutputDimensions) / VSize.Int) * 32),              AllocAlignment);

            NumOutputRegs = (uint)Math.Min(MaxNumOutputRegs, OutputDimensions);

            BigBlockSize                = (uint) (NumOutputRegs * PaddedInputDimensions);
            NumSmallBlocksInBigBlock    = (uint) (BigBlockSize / SmallBlockSize);
            NumSmallBlocksPerOutput     = (uint) (PaddedInputDimensions / SmallBlockSize);
            NumBigBlocks                = (uint) (OutputDimensions / NumOutputRegs);

            IsLarge = (CeilToMultiple((short)inDims, MaxSimdWidth) >= 128);
        }


        public void Propagate(Span<sbyte> input, Span<int> output)
        {
            if (!IsLarge)
            {
                PropagateSmall(input, output);
            }
            else
            {
                PropagateLarge(input, output);
            }
        }

        public void PropagateLarge(Span<sbyte> input, Span<int> output)
        {
            for (int bigBlock = 0; bigBlock < NumBigBlocks; bigBlock++)
            {
                Span<Vector256<int>> acc = stackalloc Vector256<int>[(int)NumOutputRegs];
                var bigIndex = bigBlock * BigBlockSize;

                for (int smallBlock = 0; smallBlock < NumSmallBlocksPerOutput; smallBlock += 2)
                {
                    int smallIndex = (int) (bigIndex + (smallBlock * SmallBlockSize * NumOutputRegs)) / VSize.SByte;

                    Vector256<byte> in0 = LoadSpan256(input, (smallBlock + 0) * VSize.Byte);
                    Vector256<byte> in1 = LoadSpan256(input, (smallBlock + 1) * VSize.Byte);

                    for (int k = 0; k < NumOutputRegs; k++)
                    {
                        m256_add_dpbusd_epi32x2(ref acc[k], in0, in1, Weights[smallIndex + k], Weights[smallIndex + k + NumOutputRegs]);
                    }
                }

                if (NumOutputRegs % 4 == 0)
                {
                    for (int k = 0; k < NumOutputRegs; k += 4)
                    {
                        int idx = (int) (bigBlock * NumOutputRegs + k) / 4;

                        //  The Biases array is stored as a Vector256[], but m256_haddx4 requires a Vector128...
                        //  This extracts the low Vector128, then the high Vector128, then moves to the next Vector256 and repeats.

                        //  TODO: CA1857
                        Vector128<int> biasVec = Avx2.ExtractVector128(Biases[idx / 2], (byte)(idx % 2));

                        var summed = m256_haddx4(acc[k + 0], acc[k + 1], acc[k + 2], acc[k + 3], biasVec);
                        Avx.Store((int*)Unsafe.AsPointer(ref output[idx * 4]), summed);
                    }
                }
                else
                {
                    for (int k = 0; k < NumOutputRegs; k++)
                    {
                        int idx = (int)(bigBlock * NumOutputRegs + k);
                        int b = Biases[idx / VSize.Int][0];

                        output[idx] = m256_hadd(acc[k], b);
                    }
                }
            }
        }

        public void PropagateSmall(Span<sbyte> input, Span<int> output)
        {
            if (OutputDimensions % OutputSimdWidth == 0)
            {
                var input32 = MemoryMarshal.Cast<sbyte, int>(input);

                int NumChunks = CeilToMultiple((short)InputDimensions, 8) / 4;
                int NumRegs = OutputDimensions / OutputSimdWidth;

                Span<Vector256<int>> outs = stackalloc Vector256<int>[NumRegs];
                for (int k = 0; k < NumRegs; k++)
                {
                    outs[k] = Biases[k];
                }

                const int vectorStride = VSize.Int;
                for (int i = 0; i < NumChunks; i += 2)
                {
                    Vector256<byte> in0 = Vector256.Create(input32[i + 0]).AsByte();
                    Vector256<byte> in1 = Vector256.Create(input32[i + 1]).AsByte();

                    for (int k = 0; k < NumRegs; k++)
                    {
                        var b0 = Weights[((i + 0) * (OutputDimensions * 4) / VSize.SByte) + k];
                        var b1 = Weights[((i + 1) * (OutputDimensions * 4) / VSize.SByte) + k];
                        m256_add_dpbusd_epi32x2(ref outs[k], in0, in1, b0, b1);
                    }
                }

                for (int k = 0; k < NumRegs; k++)
                {
                    StoreSpan256(ref outs[k], output, k * vectorStride);
                }
            }
            else if (OutputDimensions == 1)
            {
                const int vectorStride = VSize.SByte;
                int NumChunks = PaddedInputDimensions / SimdWidth;
                Vector256<int> sum0 = Vector256<int>.Zero;

                for (int j = 0; j < NumChunks; j++)
                {
                    Vector256<byte> inp = LoadSpan256(input, j * vectorStride);
                    m256_add_dpbusd_epi32(ref sum0, inp, Weights[j]);
                }

                output[0] = m256_hadd(sum0, Biases[0][0]);

            }
            else
            {
                throw new Exception();
            }

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
                    uint cursedIndex = (IsLarge ? GetLargeWeightIndex(i) : GetSmallWeightIndex(i));
                    _Weights[cursedIndex] = br.ReadSByte();
                }

                for (int i = 0; i < OutputDimensions; i += VSize.Int)
                {
                    if (OutputDimensions == 1)
                    {
                        Biases[i / VSize.Int] = Vector256.Create(_Biases[i], 0, 0, 0, 0, 0, 0, 0);
                    }
                    else
                    {
                        Biases[i / VSize.Int] = Load256(_Biases, i);
                    }
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i += VSize.SByte)
                {
                    Weights[i / VSize.SByte] = Load256(_Weights, i);
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

        private uint GetLargeWeightIndex(int i)
        {
            uint smallBlock     = (uint) ((i / SmallBlockSize) % NumSmallBlocksInBigBlock);
            uint smallBlockCol  = (uint) (smallBlock / NumSmallBlocksPerOutput);
            uint smallBlockRow  = (uint) (smallBlock % NumSmallBlocksPerOutput);
            uint bigBlock       = (uint) (i / BigBlockSize);
            uint rest           = (uint) (i % SmallBlockSize);

            return bigBlock * BigBlockSize
                    + smallBlockRow * SmallBlockSize * NumOutputRegs
                    + smallBlockCol * SmallBlockSize
                    + rest;
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

        private static int m256_hadd(Vector128<int> lo, Vector128<int> hi, int bias)
        {
            const int _MM_PERM_BADC = 0x4E;
            const int _MM_PERM_CDAB = 0xB1;

            var sum128 = Sse2.Add(lo, hi);
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_BADC));
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_CDAB));
            return Sse2.ConvertToInt32(sum128) + bias;
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

        public static void m256_add_dpbusd_epi32x2(ref Vector256<int> acc, Vector256< byte> a0, Vector256< byte> a1, 
                                                                            Vector256<sbyte> b0, Vector256<sbyte> b1)
        {
            Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a0, b0);
            Vector256<short> product1 = Avx2.MultiplyAddAdjacent(a1, b1);

            product0 = Avx2.AddSaturate(product0, product1);
            Vector256<int> product0f = Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One);

            acc = Avx2.Add(acc, product0f);
        }


        public static Vector128<int> m256_haddx4(Vector256<int> sum0, Vector256<int> sum1, Vector256<int> sum2, Vector256<int> sum3, Vector128<int> bias)
        {
            sum0 = Avx2.HorizontalAdd(sum0, sum1);
            sum2 = Avx2.HorizontalAdd(sum2, sum3);

            sum0 = Avx2.HorizontalAdd(sum0, sum2);

            var sum128lo = Avx2.ExtractVector128(sum0, 0);
            var sum128hi = Avx2.ExtractVector128(sum0, 1);

            return Ssse3.Add(Ssse3.Add(sum128lo, sum128hi), bias);
        }

    }
}
