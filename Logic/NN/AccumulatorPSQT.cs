
#define ONE_BLOCK
#undef ONE_BLOCK

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using LTChess.Logic.NN.HalfKP;
using LTChess.Logic.NN.Simple768;


using static LTChess.Logic.NN.HalfKA_HM.NNCommon;

namespace LTChess.Logic.NN
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public unsafe struct AccumulatorPSQT
    {
        //  Intellisense incorrectly marks "var accumulation = accumulator[perspectives[p]]" in FeatureTransformer.TransformFeatures
        //  as an error when this uses a primary constructor with "size" as a parameter.
        //  https://github.com/dotnet/roslyn/issues/69663

        private static readonly int NormalByteSize = UseHalfKA ? HalfKA_HM.HalfKA_HM.TransformedFeatureDimensions :
                                                      UseHalfKP ? HalfKP.HalfKP.TransformedFeatureDimensions :
                                                                  Simple768.Simple768.HiddenSize;

        public static readonly int ByteSize = NormalByteSize;

        public int VectorCount => ByteSize / VSize.Short;

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
#if ONE_BLOCK
            //  This allocates each block together, so instead of allocating 12289 blocks of 2048 bytes and 12289 blocks of 32 bytes,
            //  we will only allocate 6144 blocks of size 4160.
            //  This uses the same amount of memory, but hopefully makes it easier on the memory allocator to not have
            //  to deal with tens of thousands of tiny allocations.
            
            nuint allocationSize = (nuint) (2 * VSize.Vector256Size * (ByteSize / VSize.Short)) 
                            + (UseHalfKA ? (2 * VSize.Vector256Size * (PSQTBuckets / VSize.Int)) : 0);

            nuint block = (nuint) AlignedAllocZeroed(allocationSize, AllocAlignment);

            White = (Vector256<short>*) block;
            Black = (Vector256<short>*) (White + (nuint)(ByteSize / VSize.Short));

            if (UseHalfKA) 
            {
                PSQTWhite = (Vector256<int>*)(Black + (nuint)(ByteSize / VSize.Short));
                PSQTBlack = (Vector256<int>*)(PSQTWhite + (nuint)(PSQTBuckets / VSize.Int));
            }
#else
            White = (Vector256<short>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (ByteSize / VSize.Short)), AllocAlignment);
            Black = (Vector256<short>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (ByteSize / VSize.Short)), AllocAlignment);
            
            if (UseHalfKA)
            {
                PSQTWhite = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);
                PSQTBlack = (Vector256<int>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (PSQTBuckets / VSize.Int)), AllocAlignment);
            }
#endif

            


            RefreshPerspective[0] = RefreshPerspective[1] = true;
        }



        public Vector256<short>* this[int perspective] => (perspective == Color.White) ? White : Black;

        public Vector256<int>* PSQ(int perspective) => (perspective == Color.White) ? PSQTWhite : PSQTBlack;



        public void CopyTo(AccumulatorPSQT* target)
        {
#if ONE_BLOCK
            //uint vecSize = (uint) ((ByteSize * sizeof(short) * 2) + (PSQTBuckets * sizeof(int) * 2));
            uint vecSize = (uint) (2 * VSize.Vector256Size * (ByteSize / VSize.Short))
                   + (UseHalfKA ? (2 * VSize.Vector256Size * (PSQTBuckets / VSize.Int)) : 0);

            Unsafe.CopyBlock(target->White, White, vecSize);
#else
            uint vecSize = (uint)(ByteSize * sizeof(short));

            Unsafe.CopyBlock(target->White, White, vecSize);
            Unsafe.CopyBlock(target->Black, Black, vecSize);

            if (UseHalfKA)
            {
                const uint psqSize = PSQTBuckets * sizeof(int);
                Unsafe.CopyBlock(target->PSQTWhite, PSQTWhite, psqSize);
                Unsafe.CopyBlock(target->PSQTBlack, PSQTBlack, psqSize);
            }
#endif

            if (UseHalfKA || UseHalfKP)
            {
                target->RefreshPerspective[0] = RefreshPerspective[0];
                target->RefreshPerspective[1] = RefreshPerspective[1];
            }
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);

#if !ONE_BLOCK
            NativeMemory.AlignedFree(Black);

            if (UseHalfKA)
            {
                NativeMemory.AlignedFree(PSQTWhite);
                NativeMemory.AlignedFree(PSQTBlack);
            }
#endif
        }
    }
}