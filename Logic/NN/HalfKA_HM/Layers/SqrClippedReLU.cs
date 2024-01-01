using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static LTChess.Logic.NN.HalfKA_HM.NNCommon;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public unsafe class SqrClippedReLU
    {
        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        private const int VectorSize = VSize.Int / 2;

        public readonly int NumChunks;
        private readonly int OutputStart;

        public SqrClippedReLU(int inDims)
        {
            InputDimensions = inDims;
            OutputDimensions = InputDimensions;

            BufferSize = CeilToMultiple((short)OutputDimensions, 32);
            BufferSizeBytes = BufferSize * sizeof(sbyte);

            NumChunks = InputDimensions / 16;
            OutputStart = NumChunks * 16;
        }

        public void Propagate(Span<int> input, Span<sbyte> output)
        {
            int* inputPtr = (int*)Unsafe.AsPointer(ref input[0]);
            sbyte* outputPtr = (sbyte*)Unsafe.AsPointer(ref output[0]);

            for (int i = 0; i < NumChunks; i++)
            {
                Vector128<short> words0 = Sse2.PackSignedSaturate(
                    Sse3.LoadDquVector128(inputPtr + (((i * 4) + 0) * VectorSize)),
                    Sse3.LoadDquVector128(inputPtr + (((i * 4) + 1) * VectorSize)));

                Vector128<short> words1 = Sse2.PackSignedSaturate(
                    Sse3.LoadDquVector128(inputPtr + (((i * 4) + 2) * VectorSize)),
                    Sse3.LoadDquVector128(inputPtr + (((i * 4) + 3) * VectorSize)));

                words0 = Sse2.ShiftRightLogical(Sse2.MultiplyHigh(words0, words0), 3);
                words1 = Sse2.ShiftRightLogical(Sse2.MultiplyHigh(words1, words1), 3);

                Vector128<sbyte> packed = Sse2.PackSignedSaturate(words0, words1);

                Sse2.StoreAligned(outputPtr + (i * VectorSize), packed);
            }


            for (int i = OutputStart; i < InputDimensions; ++i)
            {
                output[i] = (sbyte)Math.Min(127L, ((input[i] * input[i]) >> (2 * WeightScaleBits)) / 128);
            }

        }
    }
}
