


using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Numerics;
using System.Reflection;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public unsafe class ClippedReLU
    {

        public AffineTransform PreviousLayer;

        public int InputDimensions;
        public int OutputDimensions;

        public int SelfBufferSize;
        public int BufferSize;

        //  Stockfish uses reintepret_cast extensively, which can be recreated in C# but seemed kind of finicky in my testing
        //  They used it to create a Vector256<int>[] from the int[] input, and would then just index that Vector array when loading / storing.
        //  That is functionally the same as just loading the vector from the same index, but I have to multiply their index by 
        //  VSize.Int (which is 8 for me) since Vector256<int>[0] should contain the first VSize.Int items (0-7),
        //  Vector256<int>[1] should have the next 8 (8-15), etc.
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

            if (InputDimensions % SimdWidth == 0)
            {
                int NumChunks = InputDimensions / SimdWidth;
                Vector256<sbyte> Zero = Vector256<sbyte>.Zero;
                Vector256<int> Offsets = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);
                //var _in = MemoryMarshal.Cast<int, Vector256<int>>(input);
                //var _out = MemoryMarshal.Cast<sbyte, Vector256<sbyte>>(output);
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
            }
            else
            {
                int NumChunks = InputDimensions / (SimdWidth / 2);
                Vector128<sbyte> Zero = Vector128<sbyte>.Zero;

                const int VectorSize = 4;
                for (int i = 0; i < NumChunks; i++)
                {
                    Vector128<short> words0 = Sse2.ShiftRightArithmetic(Sse2.PackSignedSaturate(
                        Sse2.LoadVector128((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 0) * VectorSize)),
                        Sse2.LoadVector128((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 1) * VectorSize))), WeightScaleBits);

                    Vector128<short> words1 = Sse2.ShiftRightArithmetic(Sse2.PackSignedSaturate(
                        Sse2.LoadVector128((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 2) * VectorSize)),
                        Sse2.LoadVector128((int*)UnsafeAddrOfPinnedArrayElementUnchecked(input, (i * 4 + 3) * VectorSize))), WeightScaleBits);

                    Vector128<sbyte> packed = Sse2.PackSignedSaturate(words0, words1);
                    Vector128<sbyte> max = Sse41.Max(packed, Zero);
                    Avx2.Store((sbyte*)UnsafeAddrOfPinnedArrayElementUnchecked(output, i * VectorSize), max);
                }
            }


            int kStart = InputDimensions % SimdWidth == 0
                ? InputDimensions / SimdWidth * SimdWidth
                : InputDimensions / (SimdWidth / 2) * (SimdWidth / 2);

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

