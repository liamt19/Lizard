

using static LTChess.Logic.NN.HalfKP.NNCommon;
using static LTChess.Logic.NN.HalfKP.HalfKP;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Net;
using LEB128;

namespace LTChess.Logic.NN.HalfKP.Layers
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
        public int[] Bias;
        public sbyte[] Weights;

        /// <summary>
        /// The number of items within the Vector256<T> that this class uses, which is 32 / sizeof(sbyte) = 32.
        /// </summary>
        public static int VectorSize = Vector256<sbyte>.Count;

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

            Bias = new int[OutputDimensions];
            Weights = new sbyte[OutputDimensions * PaddedInputDimensions];
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

            InputDimensions = InputSlice.OutputDimensions;
            OutputDimensions = OutDims;

            SelfBufferSize = CeilToMultiple((short)(OutputDimensions * sizeof(int)), CacheLineSize);
            BufferSize = InputSlice.BufferSize + SelfBufferSize;

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);

            Bias = new int[OutputDimensions];
            Weights = new sbyte[OutputDimensions * PaddedInputDimensions];
        }


        [MethodImpl(Inline)]
        public int[] Propagate(sbyte[] transformedFeatures)
        {
            sbyte[] input;
            if (IsInputSliceLayer)
            {
                input = InputLayer.Propagate(transformedFeatures);
            }
            else
            {
                input = PreviousLayer.Propagate(transformedFeatures);
            }

            int[] output = new int[OutputDimensions];

            int NumChunks = PaddedInputDimensions / SimdWidth;
            Vector256<short> Ones = Vector256<short>.One;

            for (int i = 0; i < OutputDimensions; i++)
            {
                int offset = i * PaddedInputDimensions;
                Vector256<int> sum = Vector256<int>.Zero;

                for (int j = 0; j < NumChunks; j++)
                {
                    //  Unfortunately it doesn't look like I can use generic/template methods for any of the Avx intrinsics.
                    //  They are very particular about the vectors and addresses being of the same type,
                    //  (i.e. Vector256<int> LoadVector256(...) requires an int* address)
                    //  And there doesn't seem to be a convenient way of making that work with generics.

                    Vector256<byte> left = Avx.LoadVector256((byte*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (j * VectorSize)));
                    Vector256<sbyte> right = Avx.LoadVector256((sbyte*)UnsafeAddrOfPinnedArrayElementUnchecked(Weights, (j * VectorSize) + offset));

                    Vector256<short> product = Avx2.MultiplyAddAdjacent(left, right);
                    Vector256<int> product2 = Avx2.MultiplyAddAdjacent(product, Ones);

                    sum = Avx2.Add(sum, product2);
                }

                sum = Avx2.HorizontalAdd(sum, sum);
                sum = Avx2.HorizontalAdd(sum, sum);

                Vector128<int> lo = Avx2.ExtractVector128(sum, 0);
                Vector128<int> hi = Avx2.ExtractVector128(sum, 1);
                output[i] = Sse2.ConvertToInt32(lo) + Sse2.ConvertToInt32(hi) + Bias[i];
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

            try
            {
                for (int i = 0; i < OutputDimensions; i++)
                {
                    if (IsLEB128)
                    {
                        Bias[i] = (int)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                    }
                    else
                    {
                        Bias[i] = br.ReadInt32();
                    }
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i++)
                {
                    if (IsLEB128)
                    {
                        Weights[i] = (sbyte)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                    }
                    else
                    {
                        Weights[i] = br.ReadSByte();
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



        public uint GetHashValue()
        {
            uint hash_value = 0xCC03DAE4u;
            hash_value += (uint)OutputDimensions;

            if (IsInputSliceLayer)
            {
                hash_value ^= InputLayer.GetHashValue() >> 1;
                hash_value ^= InputLayer.GetHashValue() << 31;
            }
            else
            {
                hash_value ^= PreviousLayer.GetHashValue() >> 1;
                hash_value ^= PreviousLayer.GetHashValue() << 31;
            }

            return hash_value;
        }

    
    }
}
