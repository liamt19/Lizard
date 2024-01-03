
#define ONE_BLOCK
#undef ONE_BLOCK

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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

        private static readonly int NormalByteSize = Simple768.HiddenSize;

        public static readonly int ByteSize = NormalByteSize;

        public int VectorCount => ByteSize / VSize.Short;

        public Vector256<short>* White;
        public Vector256<short>* Black;

        public AccumulatorPSQT()
        {
#if ONE_BLOCK
            //  This allocates each block together, so instead of allocating 12289 blocks of 2048 bytes and 12289 blocks of 32 bytes,
            //  we will only allocate 6144 blocks of size 4160.
            //  This uses the same amount of memory, but hopefully makes it easier on the memory allocator to not have
            //  to deal with tens of thousands of tiny allocations.
            
            nuint allocationSize = (nuint) (2 * VSize.Vector256Size * (ByteSize / VSize.Short));

            nuint block = (nuint) AlignedAllocZeroed(allocationSize, AllocAlignment);

            White = (Vector256<short>*) block;
            Black = (Vector256<short>*) (White + (nuint)(ByteSize / VSize.Short));

#else
            White = (Vector256<short>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (ByteSize / VSize.Short)), AllocAlignment);
            Black = (Vector256<short>*)AlignedAllocZeroed((nuint)(VSize.Vector256Size * (ByteSize / VSize.Short)), AllocAlignment);
#endif
        }



        public Vector256<short>* this[int perspective] => (perspective == Color.White) ? White : Black;

        public void CopyTo(AccumulatorPSQT* target)
        {
#if ONE_BLOCK
            //uint vecSize = (uint) ((ByteSize * sizeof(short) * 2) + (PSQTBuckets * sizeof(int) * 2));
            uint vecSize = (uint) (2 * VSize.Vector256Size * (ByteSize / VSize.Short));

            Unsafe.CopyBlock(target->White, White, vecSize);
#else
            uint vecSize = (uint)(ByteSize * sizeof(short));

            Unsafe.CopyBlock(target->White, White, vecSize);
            Unsafe.CopyBlock(target->Black, Black, vecSize);
#endif
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(White);

#if !ONE_BLOCK
            NativeMemory.AlignedFree(Black);
#endif
        }
    }
}