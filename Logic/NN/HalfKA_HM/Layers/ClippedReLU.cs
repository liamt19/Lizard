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
    public unsafe class ClippedReLU
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

        private readonly int kStart;

        public ClippedReLU(int inputDims)
        {   
            InputDimensions = inputDims;
            OutputDimensions = InputDimensions;

            BufferSize = CeilToMultiple((short)OutputDimensions, 32);
            BufferSizeBytes = BufferSize * sizeof(sbyte);

            kStart = InputDimensions % SimdWidth == 0
                ? InputDimensions / SimdWidth * SimdWidth
                : InputDimensions / (SimdWidth / 2) * (SimdWidth / 2);
        }


        /// <summary>
        /// Clamps the output from the previous <see cref="AffineTransform"/> layer between [0, 127]
        /// </summary>
        public void Propagate(Span<int> input, Span<sbyte> output)
        {
            int* inputPtr = (int*) Unsafe.AsPointer(ref input[0]);
            int* outputPtr = (int*) Unsafe.AsPointer(ref output[0]);

            int NumChunks = InputDimensions / SimdWidth;
            Vector256<sbyte> Zero = Vector256<sbyte>.Zero;
            Vector256<int> Offsets = Vector256.Create(0, 4, 1, 5, 2, 6, 3, 7);

            for (int i = 0; i < NumChunks; i++)
            {
                Vector256<short> words0 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                    Avx.LoadDquVector256(inputPtr + ((i * 4 + 0) * VectorSize)),
                    Avx.LoadDquVector256(inputPtr + ((i * 4 + 1) * VectorSize))), WeightScaleBits);

                Vector256<short> words1 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(
                    Avx.LoadDquVector256(inputPtr + ((i * 4 + 2) * VectorSize)),
                    Avx.LoadDquVector256(inputPtr + ((i * 4 + 3) * VectorSize))), WeightScaleBits);

                Vector256<sbyte> packed = Avx2.PackSignedSaturate(words0, words1);
                Vector256<sbyte> max = Avx2.Max(packed, Zero);
                Vector256<int> permuted = Avx2.PermuteVar8x32(max.AsInt32(), Offsets);

                Avx.Store(outputPtr + (i * VectorSize), permuted);
            }

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

