using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using static Lizard.Logic.NN.HKA.NNCommon;

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

        public const uint ByteSize = HKA.HalfKA_HM.TransformedFeatureDimensions * sizeof(short);

        public static int VectorCount => Simple768.HiddenSize / VSize.Short;

        public readonly Vector256<short>* White;
        public readonly Vector256<short>* Black;

        public Vector256<int>* PSQTWhite;
        public Vector256<int>* PSQTBlack;

        public fixed bool NeedsRefresh[2];

        public Accumulator()
        {
            White = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);
            Black = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            PSQTWhite = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);
            PSQTBlack = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);

            NeedsRefresh[Color.White] = NeedsRefresh[Color.Black] = true;
        }

        public Vector256<short>* this[int perspective] => (perspective == Color.White) ? White : Black;
        public Vector256<int>* PSQ(int perspective) => (perspective == Color.White) ? PSQTWhite : PSQTBlack;

        public void CopyTo(Accumulator* target)
        {
            Unsafe.CopyBlock(target->White, White, ByteSize);
            Unsafe.CopyBlock(target->Black, Black, ByteSize);


            const uint psqSize = PSQTBuckets * sizeof(int);
            Unsafe.CopyBlock(target->PSQTWhite, PSQTWhite, psqSize);
            Unsafe.CopyBlock(target->PSQTBlack, PSQTBlack, psqSize);

            target->NeedsRefresh[0] = NeedsRefresh[0];
            target->NeedsRefresh[1] = NeedsRefresh[1];
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);
            NativeMemory.AlignedFree(Black);

            NativeMemory.AlignedFree(PSQTWhite);
            NativeMemory.AlignedFree(PSQTBlack);
        }
    }
}