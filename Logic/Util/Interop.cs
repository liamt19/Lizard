using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Lizard.Logic.Util
{
    public static class Interop
    {
        /// <summary>
        /// Hints the CPU that we are going to be using the data located at <paramref name="address"/> soon,
        /// so it should fetch a cache line from that address and place it in a high locality cache.
        /// <para></para>
        /// This isn't a guarantee, and the time it takes for <see cref="Unsafe.AsPointer"/> to compute the address does hurt, 
        /// but regardless this seems to help.
        /// </summary>
        public static unsafe void prefetch(void* address)
        {
            if (Sse.IsSupported) Sse.Prefetch0(address);
        }

        public static ulong popcount(ulong value) => ulong.PopCount(value);

        public static int lsb(ulong value) => ((int)ulong.TrailingZeroCount(value));

        public static unsafe int poplsb(ulong* value)
        {
            int sq = lsb(*value);
            *value = *value & (*value - 1);
            return sq;
        }

        public static bool MoreThanOne(ulong value) => (value & (value - 1)) != 0;


        /// <summary>
        /// Extracts the bits from <paramref name="value"/> that are set in <paramref name="mask"/>, 
        /// and places them in the least significant bits of the result.
        /// <br></br>
        /// The output will be somewhat similar to a bitwise AND operation, just shifted and condensed to the right.
        /// <para></para>
        /// So <c>pext("ABCD EFGH", 1011 0001)</c> would return <c>"0000 ACDH"</c>,
        /// where ACDH could each be 0 or 1 depending on if they were set in <paramref name="value"/>
        /// </summary>
        public static ulong pext(ulong value, ulong mask)
        {
            if (Bmi2.X64.IsSupported)
            {
                return Bmi2.X64.ParallelBitExtract(value, mask);
            }
            else
            {
                ulong res = 0;
                for (ulong bb = 1; mask != 0; bb += bb)
                {
                    if ((value & mask & (0UL - mask)) != 0)
                    {
                        res |= bb;
                    }
                    mask &= mask - 1;
                }
                return res;
            }
        }


        /// <summary>
        /// Allocates a block of memory of size <paramref name="byteCount"/>, aligned on the boundary <paramref name="alignment"/>,
        /// and clears the block before returning its address.
        /// <para></para>
        /// The <see cref="NativeMemory"/> class provides <see cref="NativeMemory.AlignedAlloc"/> to make sure that the block is aligned,
        /// and <see cref="NativeMemory.AllocZeroed"/>, to ensure that the memory in that block is set to 0 before it is used,
        /// but doesn't have a method to do these both.
        /// </summary>
        public static unsafe void* AlignedAllocZeroed(nuint byteCount, nuint alignment = AllocAlignment)
        {
            void* block = NativeMemory.AlignedAlloc(byteCount, alignment);
            NativeMemory.Clear(block, byteCount);

            return block;
        }

    }
}
