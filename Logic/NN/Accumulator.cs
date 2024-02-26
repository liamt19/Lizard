using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Lizard.Logic.NN
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public unsafe struct Accumulator
    {
        //  Intellisense incorrectly marks "var accumulation = accumulator[perspectives[p]]" in FeatureTransformer.TransformFeatures
        //  as an error when this uses a primary constructor with "size" as a parameter.
        //  https://github.com/dotnet/roslyn/issues/69663

        public const int ByteSize = Simple768.HiddenSize * sizeof(short);

        public static int VectorCount => Simple768.HiddenSize / VSize.Short;

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

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);
            NativeMemory.AlignedFree(Black);
        }
    }
}