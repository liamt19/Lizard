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
        }

        public Vector256<short>* this[int perspective]
        {
            get
            {
                return (perspective == Color.White) ? White : Black;
            }
        }


        public Vector256<int>* PSQ(int perspective)
        {
            return (perspective == Color.White) ? PSQTWhite : PSQTBlack;
        }

        public void CopyTo(ref AccumulatorPSQT target)
        {
            int size = ByteSize * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                target.White,
                White,
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                target.Black,
                Black,
                (uint)size
            );

            size = PSQTBuckets * sizeof(int);

            Unsafe.CopyBlockUnaligned(
                target.PSQTWhite,
                PSQTWhite,
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                target.PSQTBlack,
                PSQTBlack,
                (uint)size
            );

            target.RefreshPerspective[0] = RefreshPerspective[0];
            target.RefreshPerspective[1] = RefreshPerspective[1];
        }

        public void CopyTo(AccumulatorPSQT* target)
        {
            int size = ByteSize * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                target->White,
                White,
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                target->Black,
                Black,
                (uint)size
            );

            size = PSQTBuckets * sizeof(int);

            Unsafe.CopyBlockUnaligned(
                target->PSQTWhite,
                PSQTWhite,
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                target->PSQTBlack,
                PSQTBlack,
                (uint)size
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
        }
    }
}