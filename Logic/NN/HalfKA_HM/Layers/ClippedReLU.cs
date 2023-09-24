


using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System;
using System.Numerics;
using System.Reflection;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public class ClippedReLU
    {
        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        //  Stockfish uses reintepret_cast extensively, which can be recreated in C# but seemed kind of finicky in my testing
        //  They used it to create a Vector256<int>[] from the int[] input, and would then just index that Vector array when loading / storing.
        //  That is functionally the same as just loading the vector from the same index, but I have to multiply their index by 
        //  VSize.Int (which is 8 for me) since Vector256<int>[0] should contain the first VSize.Int items (0-7),
        //  Vector256<int>[1] should have the next 8 (8-15), etc.
        private const int VectorSize = VSize.Int;

        public ClippedReLU(int inputDims)
        {   
            InputDimensions = inputDims;
            OutputDimensions = InputDimensions;

            BufferSize = CeilToMultiple((short)OutputDimensions, 32);
            BufferSizeBytes = BufferSize * sizeof(sbyte);
        }


        /// <summary>
        /// Clamps the output from the previous <see cref="AffineTransform"/> layer between [0, 127]
        /// </summary>
        public void Propagate(Span<int> input, Span<sbyte> output)
        {
            if (InputDimensions % SimdWidth == 0)
            {
                int NumChunks = InputDimensions / SimdWidth;
                Vector256<sbyte> Zero = Vector256<sbyte>.Zero;
                Vector256<int> Offsets = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);
                
                for (int i = 0; i < NumChunks; i++)
                {
                    Vector256<short> words0 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                        LoadSpan256(input, (i * 4 + 0) * VectorSize),
                        LoadSpan256(input, (i * 4 + 1) * VectorSize)), WeightScaleBits);

                    Vector256<short> words1 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                        LoadSpan256(input, (i * 4 + 2) * VectorSize),
                        LoadSpan256(input, (i * 4 + 3) * VectorSize)), WeightScaleBits);

                    Vector256<sbyte> packed = Avx2.PackSignedSaturate(words0, words1);
                    Vector256<sbyte> max = Avx2.Max(packed, Zero);
                    Vector256<int> permuted = Avx2.PermuteVar8x32(max.AsInt32(), Offsets);

                    StoreSpan256(ref permuted, output, i * VectorSize);
                }
            }
            else
            {
                int NumChunks = InputDimensions / (SimdWidth / 2);
                Vector128<sbyte> Zero = Vector128<sbyte>.Zero;

                //  Vector128<int> contains 4 integers
                const int VectorSize = 4;
                for (int i = 0; i < NumChunks; i++)
                {
                    Vector128<short> words0 = Sse2.ShiftRightArithmetic(Sse2.PackSignedSaturate(
                        LoadSpan128(input, (i * 4 + 0) * VectorSize),
                        LoadSpan128(input, (i * 4 + 1) * VectorSize)), WeightScaleBits);

                    Vector128<short> words1 = Sse2.ShiftRightArithmetic(Sse2.PackSignedSaturate(
                        LoadSpan128(input, (i * 4 + 2) * VectorSize),
                        LoadSpan128(input, (i * 4 + 3) * VectorSize)), WeightScaleBits);
                    Vector128<sbyte> packed = Sse2.PackSignedSaturate(words0, words1);
                    Vector128<sbyte> max = Sse41.Max(packed, Zero);

                    StoreSpan128(ref max, output, i * VectorSize);
                }
            }


            int kStart = InputDimensions % SimdWidth == 0
                ? InputDimensions / SimdWidth * SimdWidth
                : InputDimensions / (SimdWidth / 2) * (SimdWidth / 2);

            for (int i = kStart; i < InputDimensions; ++i)
            {
                output[i] = (sbyte) Math.Max(0, Math.Min(127, input[i] >> WeightScaleBits));
            }


        }


        public bool ReadParameters(BinaryReader br)
        {
            return true;
        }

        public uint GetHashValue(uint prevHash)
        {
            uint hashValue = 0x538D24C7u;
            hashValue += prevHash;
            return hashValue;
        }

    }
}

