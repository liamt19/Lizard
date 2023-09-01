

using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Net;
using LEB128;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public unsafe class AffineTransform
    {
        public ClippedReLU PreviousLayer;

        public InputSlice InputLayer;

        /// <summary>
        /// If this is true, then this layer is the second layer in the network (Hidden layer 1)
        /// and the previous layer is stored in <see cref="InputLayer"/> rather than <see cref="PreviousLayer"/>.
        /// </summary>
        public bool IsInputSliceLayer = false;

        public int InputDimensions;
        public int OutputDimensions;

        public int SelfBufferSize;
        public int BufferSize;

        public readonly int PaddedInputDimensions;

        public Vector256<int>[] Biases;
        public Vector256<sbyte>[] Weights;

        public const int VectorSize = VSize.SByte;

        public const int OutputSimdWidth = SimdWidth / 4;
        public const int InputSimdWidth = SimdWidth;
        public const int OutputTypeSize = sizeof(int);

        /// <summary>
        /// Creates a new Affine layer, which takes input from the <see cref="ClippedReLU"/> layer that came before it
        /// and outputs <paramref name="OutDims"/> numbers.
        /// </summary>
        /// <param name="prev">The layer that came before this one in the network</param>
        /// <param name="OutDims">The length of the array that this layer returns as output</param>
        public AffineTransform(ClippedReLU prev, int OutDims)
        {
            PreviousLayer = prev;

            InputDimensions = prev.OutputDimensions;
            OutputDimensions = OutDims;

            SelfBufferSize = CeilToMultiple((short)(OutputDimensions * sizeof(int)), CacheLineSize);
            BufferSize = prev.BufferSize + SelfBufferSize;

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);

            Weights = new Vector256<sbyte>[(OutputDimensions * PaddedInputDimensions) / VSize.SByte];
            Biases = new Vector256<int>[Math.Max(1, (OutputDimensions) / VSize.Int)];
        }

        /// <summary>
        /// Creates a new Affine layer, which takes input from the <see cref="InputSlice"/> layer that came before it
        /// and outputs <paramref name="OutDims"/> numbers.
        /// </summary>
        /// <param name="prev">The first layer in the network</param>
        /// <param name="OutDims">The length of the array that this layer returns as output</param>
        public AffineTransform(InputSlice prev, int OutDims)
        {
            InputLayer = prev;
            IsInputSliceLayer = true;

            InputDimensions = prev.OutputDimensions;
            OutputDimensions = OutDims;

            SelfBufferSize = CeilToMultiple((short)(OutputDimensions * sizeof(int)), CacheLineSize);
            BufferSize = InputSlice.BufferSize + SelfBufferSize;

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);

            Weights = new Vector256<sbyte>[(OutputDimensions * PaddedInputDimensions) / VSize.SByte];
            Biases = new Vector256<int>[Math.Max(1, (OutputDimensions) / VSize.Int)];
        }

        [MethodImpl(Inline)]
        public Span<int> Propagate(Span<sbyte> transformedFeatures, Span<byte> buffer) {
            
            Span<sbyte> input = IsInputSliceLayer ? InputLayer.Propagate(transformedFeatures, buffer.Slice(SelfBufferSize)) : PreviousLayer.Propagate(transformedFeatures, buffer.Slice(SelfBufferSize));
            Span<int> output = MemoryMarshal.Cast<byte, int>(buffer);
            

            if (OutputDimensions % OutputSimdWidth == 0 && InputDimensions == 8)
            {
                Buffer.MemoryCopy((void*)Marshal.UnsafeAddrOfPinnedArrayElement(Biases, 0), Unsafe.AsPointer(ref output[0]), OutputDimensions * OutputTypeSize, OutputDimensions * OutputTypeSize);

                var input32 = MemoryMarshal.Cast<sbyte, int>(input);

                const int vectorStride = VSize.Int;

                Vector256<int> in0 = Vector256.Create(input32[0]);
                Vector256<int> in1 = Vector256.Create(input32[1]);

                for (int j = 0; j * OutputSimdWidth < OutputDimensions; ++j)
                {
                    Vector256<int> outVec = LoadSpan256(output, j * vectorStride);

                    Vector256<sbyte> col0 = Weights[j];
                    Vector256<sbyte> col1 = Weights[j + 4];

                    m256_add_dpbusd_epi32x2(ref outVec, in0.AsByte(), in1.AsByte(), col0, col1);

                    StoreSpan256(ref outVec, output, j * vectorStride);

                }
            }
            else if (OutputDimensions % OutputSimdWidth == 0)
            {
                var input32 = MemoryMarshal.Cast<sbyte, int>(input);

                int NumRegs = OutputDimensions / OutputSimdWidth;
                Span<Vector256<int>> outs = stackalloc Vector256<int>[NumRegs];
                for (int k = 0; k < NumRegs; k++)
                {
                   Vector256<int> biasVec = Biases[k];
                    outs[k] = biasVec;
                }

                const int vectorStride = VSize.Int;
                int NumChunks = InputDimensions / 4;
                for (int i = 0; i < NumChunks; i += 4)
                {
                    Vector256<int> in0 = Vector256.Create(input32[i + 0]);
                    Vector256<int> in1 = Vector256.Create(input32[i + 1]);
                    Vector256<int> in2 = Vector256.Create(input32[i + 2]);
                    Vector256<int> in3 = Vector256.Create(input32[i + 3]);

                    for (int k = 0; k < NumRegs; k++)
                    {
                        Vector256<sbyte> col0 = Weights[((i + 0)) + k];
                        Vector256<sbyte> col1 = Weights[((i + 1)) + k];
                        Vector256<sbyte> col2 = Weights[((i + 2)) + k];
                        Vector256<sbyte> col3 = Weights[((i + 3)) + k];

                        m256_add_dpbusd_epi32x4(ref outs[k], in0.AsByte(), in1.AsByte(), in2.AsByte(), in3.AsByte(), col0, col1, col2, col3);
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
                    Vector256<sbyte> weightVec = Weights[j];
                    m256_add_dpbusd_epi32(ref sum0, inp, weightVec);
                }

                output[0] = m256_hadd(sum0, Biases[0][0]);

            }
            else
            {
                throw new Exception();
            }

            return output;
        }



        public bool ReadParameters(BinaryReader br)
        {
            if ((IsInputSliceLayer && !InputLayer.ReadParameters(br)) ||
               (!IsInputSliceLayer && !PreviousLayer.ReadParameters(br)))
            {
                return false;
            }

            int[] _Biases = new int[OutputDimensions];
            sbyte[] _Weights = new sbyte[OutputDimensions * PaddedInputDimensions];

            try
            {

                for (int i = 0; i < OutputDimensions; i++)
                {
                    if (IsLEB128)
                    {
                        _Biases[i] = (int)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                    }
                    else
                    {
                        _Biases[i] = br.ReadInt32();
                    }
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i++)
                {
                    if (IsLEB128)
                    {
                        _Weights[i] = (sbyte)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                    }
                    else
                    {
                        int cursedIndex = (i / 4) % (PaddedInputDimensions / 4) * OutputDimensions * 4 + i / PaddedInputDimensions * 4 + i % 4;
                        _Weights[cursedIndex] = br.ReadSByte();
                    }

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



        public uint GetHashValue()
        {
            uint hashValue = 0xCC03DAE4u;
            hashValue += (uint)OutputDimensions;

            if (IsInputSliceLayer)
            {
                hashValue ^= InputLayer.GetHashValue() >> 1;
                hashValue ^= InputLayer.GetHashValue() << 31;
            }
            else
            {
                hashValue ^= PreviousLayer.GetHashValue() >> 1;
                hashValue ^= PreviousLayer.GetHashValue() << 31;
            }

            return hashValue;
        }


        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void m256_add_dpbusd_epi32x4(ref Vector256<int> acc, 
            Vector256< byte> a0, Vector256< byte> a1, Vector256< byte> a2, Vector256< byte> a3, 
            Vector256<sbyte> b0, Vector256<sbyte> b1, Vector256<sbyte> b2, Vector256<sbyte> b3)
        {
            Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a0, b0);
            Vector256<short> product1 = Avx2.MultiplyAddAdjacent(a1, b1);
            Vector256<short> product2 = Avx2.MultiplyAddAdjacent(a2, b2);
            Vector256<short> product3 = Avx2.MultiplyAddAdjacent(a3, b3);

            product0 = Avx2.AddSaturate(product0, product1);
            Vector256<int> product0f = Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One);

            product2 = Avx2.AddSaturate(product2, product3);
            Vector256<int> product2f = Avx2.MultiplyAddAdjacent(product2, Vector256<short>.One);

            acc = Avx2.Add(acc, Avx2.Add(product0f, product2f));
        }

        [MethodImpl(Inline)]
        private static void m256_add_dpbusd_epi32(ref Vector256<int> acc, Vector256<byte> a, Vector256<sbyte> b)
        {
            Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a, b);
            acc = Avx2.Add(acc, Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static int m256_hadd(Vector256<int> sum, int bias)
        {
            const int _MM_PERM_BADC = 0x4E;
            const int _MM_PERM_CDAB = 0xB1;

            Vector128<int> lo = Avx2.ExtractVector128(sum, 0);
            var sum128 = Ssse3.Add(lo, Avx2.ExtractVector128(sum, 1));
            sum128 = Ssse3.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_BADC));
            sum128 = Ssse3.Add(sum128, Sse2.Shuffle(sum128, _MM_PERM_CDAB));
            return Ssse3.ConvertToInt32(sum128) + bias;
        }



        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
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
