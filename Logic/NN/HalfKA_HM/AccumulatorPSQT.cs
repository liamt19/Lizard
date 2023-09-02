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

        public Vector256<short>* White;
        public Vector256<short>* Black;

        public Vector256<int>* PSQTWhite;
        public Vector256<int>* PSQTBlack;

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh = true;

        public AccumulatorPSQT() 
        {
            White       = (Vector256<short>*) NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<short>) * (ByteSize / VSize.Short)), 32);
            Black       = (Vector256<short>*) NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<short>) * (ByteSize / VSize.Short)), 32);
            PSQTWhite   = (Vector256<int>*)   NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>)   * (PSQTBuckets / VSize.Int)), 32);
            PSQTBlack   = (Vector256<int>*)   NativeMemory.AlignedAlloc((nuint)(sizeof(Vector256<int>)   * (PSQTBuckets / VSize.Int)), 32);
        }

        public Vector256<short>* this[int perspective]
        {
            get
            {
                return (perspective == Color.White) ? White : Black;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<int>* PSQ(int perspective)
        {
            return (perspective == Color.White) ? PSQTWhite : PSQTBlack;
        }

        [MethodImpl(Inline)]
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
        }
    }
}