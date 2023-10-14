using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using static LTChess.Logic.NN.HalfKA_HM.NNCommon;

namespace LTChess.Logic.NN.HalfKA_HM
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public unsafe struct AccumulatorPSQT
    {
        //  Intellisense incorrectly marks "var accumulation = accumulator[perspectives[p]]" in FeatureTransformer.TransformFeatures
        //  as an error when this uses a primary constructor with "size" as a parameter.
        //  https://github.com/dotnet/roslyn/issues/69663

        public const int ByteSize = HalfKA_HM.TransformedFeatureDimensions;
        public const int VectorCount = (ByteSize / VSize.Short);
        public const int PSQTVectorCount = (PSQTBuckets / VSize.Int);

        public Vector256<short>* White;
        public Vector256<short>* Black;

        public Vector256<int>* PSQTWhite;
        public Vector256<int>* PSQTBlack;

        public Vector256<short>** Perspectives;
        public Vector256<int>** PerspectivesPSQ;

        /// <summary>
        /// Set to true when a king move is made by each perspective, in which case every feature 
        /// in that side's accumulator needs to be recalculated.
        /// </summary>
        public fixed bool RefreshPerspective[2];

        public AccumulatorPSQT() 
        {
            White       = (Vector256<short>*) AlignedAllocZeroed((nuint)(sizeof(Vector256<short>) * (ByteSize / VSize.Short)),  AllocAlignment);
            Black       = (Vector256<short>*) AlignedAllocZeroed((nuint)(sizeof(Vector256<short>) * (ByteSize / VSize.Short)),  AllocAlignment);
            PSQTWhite   = (Vector256<int>*)   AlignedAllocZeroed((nuint)(sizeof(Vector256<int>)   * (PSQTBuckets / VSize.Int)), AllocAlignment);
            PSQTBlack   = (Vector256<int>*)   AlignedAllocZeroed((nuint)(sizeof(Vector256<int>)   * (PSQTBuckets / VSize.Int)), AllocAlignment);

            RefreshPerspective[0] = RefreshPerspective[1] = true;

            Perspectives = (Vector256<short>**) AlignedAllocZeroed((nuint)(sizeof(nuint) * 2), AllocAlignment);
            Perspectives[Color.White] = White;
            Perspectives[Color.Black] = Black;

            PerspectivesPSQ = (Vector256<int>**) AlignedAllocZeroed((nuint)(sizeof(nuint) * 2), AllocAlignment);
            PerspectivesPSQ[Color.White] = PSQTWhite;
            PerspectivesPSQ[Color.Black] = PSQTBlack;
        }

        public Vector256<short>* this[int perspective] => Perspectives[perspective];

        public Vector256<int>* PSQ(int perspective) => PerspectivesPSQ[perspective];



        public void CopyTo(AccumulatorPSQT* target)
        {
            const uint vecSize = ByteSize * sizeof(short);

            Unsafe.CopyBlockUnaligned(
                target->White,
                White,
                vecSize
            );
            Unsafe.CopyBlockUnaligned(
                target->Black,
                Black,
                vecSize
            );

            const uint psqSize = PSQTBuckets * sizeof(int);

            Unsafe.CopyBlockUnaligned(
                target->PSQTWhite,
                PSQTWhite,
                psqSize
            );
            Unsafe.CopyBlockUnaligned(
                target->PSQTBlack,
                PSQTBlack,
                psqSize
            );

            target->RefreshPerspective[0] = RefreshPerspective[0];
            target->RefreshPerspective[1] = RefreshPerspective[1];
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);
            NativeMemory.AlignedFree(Black);

            NativeMemory.AlignedFree(PSQTWhite);
            NativeMemory.AlignedFree(PSQTBlack);

            NativeMemory.AlignedFree(Perspectives);
            NativeMemory.AlignedFree(PerspectivesPSQ);
        }
    }
}