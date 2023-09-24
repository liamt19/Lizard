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
    public unsafe class SqrClippedReLU
    {
        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        private const int VectorSize = (VSize.Int / 2);

        public SqrClippedReLU(int inDims)
        {
            InputDimensions = inDims;
            OutputDimensions = InputDimensions;

            BufferSize = CeilToMultiple((short)OutputDimensions, 32);
            BufferSizeBytes = BufferSize * sizeof(sbyte);
        }

        public void Propagate(Span<int> input, Span<sbyte> output)
        {
            var NumChunks = InputDimensions / 16;

            for (int i = 0; i < NumChunks; i++)
            {
                Vector128<short> words0 = Sse2.PackSignedSaturate(
                    Sse3.LoadDquVector128((int*)Unsafe.AsPointer(ref input[(i * 4 + 0) * (VectorSize)])),
                    Sse3.LoadDquVector128((int*)Unsafe.AsPointer(ref input[(i * 4 + 1) * (VectorSize)])));

                Vector128<short> words1 = Sse2.PackSignedSaturate(
                    Sse3.LoadDquVector128((int*)Unsafe.AsPointer(ref input[(i * 4 + 2) * (VectorSize)])),
                    Sse3.LoadDquVector128((int*)Unsafe.AsPointer(ref input[(i * 4 + 3) * (VectorSize)])));

                words0 = Sse2.ShiftRightLogical(Sse2.MultiplyHigh(words0, words0), 3);
                words1 = Sse2.ShiftRightLogical(Sse2.MultiplyHigh(words1, words1), 3);

                Vector128<sbyte> packed = Sse2.PackSignedSaturate(words0, words1);

                Sse2.StoreAligned((sbyte*)Unsafe.AsPointer(ref output[i * (VectorSize)]), packed);
            }

            var start = NumChunks * 16;
            for (int i = start; i < InputDimensions; ++i)
            {   
                output[i] = (sbyte)Math.Max(0, Math.Min(127L, ((input[i] * input[i]) >> (2 * WeightScaleBits)) / 128));
            }

        }
    }
}
