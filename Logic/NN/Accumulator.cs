using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Lizard.Logic.NN
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public unsafe struct Accumulator
    {
        public const int ByteSize = Bucketed768.HiddenSize * sizeof(short);

        public readonly Vector256<short>* White;
        public readonly Vector256<short>* Black;

        public fixed bool NeedsRefresh[2];

        public Accumulator()
        {
            White = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);
            Black = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            NeedsRefresh[Color.White] = NeedsRefresh[Color.Black] = true;
        }

        public Vector256<short>* this[int perspective] => (perspective == Color.White) ? White : Black;

        public void CopyTo(Accumulator* target)
        {
            Unsafe.CopyBlock(target->White, White, ByteSize);
            Unsafe.CopyBlock(target->Black, Black, ByteSize);

            target->NeedsRefresh[0] = NeedsRefresh[0];
            target->NeedsRefresh[1] = NeedsRefresh[1];
        }

        public void CopyTo(Accumulator* target, int perspective)
        {
            Unsafe.CopyBlock((*target)[perspective], this[perspective], ByteSize);
            target->NeedsRefresh[perspective] = NeedsRefresh[perspective];
        }

        public void CopyTo(ref Accumulator target, int perspective)
        {
            Unsafe.CopyBlock(target[perspective], this[perspective], ByteSize);
            target.NeedsRefresh[perspective] = NeedsRefresh[perspective];
        }

        public void ResetWithBiases(short* biases, uint byteCount)
        {
            Unsafe.CopyBlock(White, biases, byteCount);
            Unsafe.CopyBlock(Black, biases, byteCount);
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);
            NativeMemory.AlignedFree(Black);
        }
    }
}