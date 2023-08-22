

//using __m256i = System.Runtime.Intrinsics.Vector256<short>;
//using __m256isb = System.Runtime.Intrinsics.Vector256<sbyte>;

using static LTChess.Logic.NN.HalfKP.NNCommon;
using static LTChess.Logic.NN.HalfKP.HalfKP;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System;

namespace LTChess.Logic.NN.HalfKP.Layers
{
    public unsafe class ClippedReLU
    {

        public AffineTransform PreviousLayer;

        public int InputDimensions;
        public int OutputDimensions;

        public int SelfBufferSize;
        public int BufferSize;

        /// <summary>
        /// The number of items within the Vector256<T> that this class uses, which is 32 / sizeof(int) = 8.
        /// </summary>
        public static int VectorSize = VSize.Int;

        public ClippedReLU(AffineTransform? prev)
        {
            PreviousLayer = prev;

            InputDimensions = prev.OutputDimensions;
            OutputDimensions = InputDimensions;

            SelfBufferSize = CeilToMultiple((short) (OutputDimensions * sizeof(ushort)), CacheLineSize);
            BufferSize = prev.BufferSize + SelfBufferSize;
        }


        /// <summary>
        /// Clamps the output from the previous <see cref="AffineTransform"/> layer between [0, 127]
        /// </summary>
        [MethodImpl(Optimize)]
        public sbyte[] Propagate(sbyte[] transformedFeatures)
        {
            int[] input = PreviousLayer.Propagate(transformedFeatures);
            sbyte[] output = new sbyte[input.Length];

            //  Stockfish had _mm256_set_epi32(7, 3, 6, 2, 5, 1, 4, 0) here, which creates the vector <0, 4, 1, 5, 2, 6, 3, 7>
            //  Vector256.Create actually uses _mm256_setr_epi32 which is _mm256_set_epi32 in reverse order,
            //  so this vector needed to be reversed
            Vector256<int> Offsets = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);

            int NumChunks = InputDimensions / SimdWidth;
            Vector256<sbyte> Zero = Vector256<sbyte>.Zero;

            for (int i = 0; i < NumChunks; i++)
            {
                Vector256<short> words0 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                    Avx.LoadVector256((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 0) * VectorSize)),
                    Avx.LoadVector256((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 1) * VectorSize))), WeightScaleBits);

                Vector256<short> words1 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                    Avx.LoadVector256((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 2) * VectorSize)),
                    Avx.LoadVector256((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 3) * VectorSize))), WeightScaleBits);

                Vector256<sbyte> packed = Avx2.PackSignedSaturate(words0, words1);
                Vector256<sbyte> max = Avx2.Max(packed, Zero);
                Vector256<int> permuted = Avx2.PermuteVar8x32(max.AsInt32(), Offsets);

                Avx2.Store((int*)UnsafeAddrOfPinnedArrayElementUnchecked(output, i * VectorSize), permuted);
            }

            int kStart = NumChunks * SimdWidth;

            for (int i = kStart; i < InputDimensions; ++i)
            {
                output[i] = (sbyte) Math.Max(0, Math.Min(127, input[i] >> WeightScaleBits));
            }

            return output;
        }


        public bool ReadParameters(BinaryReader br)
        {
            return PreviousLayer.ReadParameters(br);
        }


        public uint GetHashValue()
        {
            uint hashValue = 0x538D24C7u;
            hashValue += PreviousLayer.GetHashValue();
            return hashValue;
        }

    }
}

