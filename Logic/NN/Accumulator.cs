
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using static Lizard.Logic.NN.HalfKA_HM.NNCommon;

namespace Lizard.Logic.NN
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public unsafe struct Accumulator
    {
        public static readonly nuint ByteSize = HalfKA_HM.HalfKA_HM.TransformedFeatureDimensions * sizeof(short);

        public Vector256<short>* White;
        public Vector256<short>* Black;

        public Vector256<int>* PSQTWhite;
        public Vector256<int>* PSQTBlack;

        /// <summary>
        /// Set to true when a king move is made by each perspective, in which case every feature 
        /// in that side's accumulator needs to be recalculated.
        /// </summary>
        public fixed bool RefreshPerspective[2];

        public Accumulator()
        {
            White = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);
            Black = (Vector256<short>*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            PSQTWhite = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);
            PSQTBlack = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);

            RefreshPerspective[0] = RefreshPerspective[1] = true;
        }

        public Vector256<short>* this[int perspective] => (perspective == Color.White) ? White : Black;
        public Vector256<int>* PSQ(int perspective) => (perspective == Color.White) ? PSQTWhite : PSQTBlack;


        public void CopyTo(Accumulator* target)
        {
            Unsafe.CopyBlock(target->White, White, (uint)ByteSize);
            Unsafe.CopyBlock(target->Black, Black, (uint)ByteSize);

            const uint psqSize = PSQTBuckets * sizeof(int);
            Unsafe.CopyBlock(target->PSQTWhite, PSQTWhite, psqSize);
            Unsafe.CopyBlock(target->PSQTBlack, PSQTBlack, psqSize);

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