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
            if (Sse.IsSupported)
            {
                Sse.Prefetch0(address);
            }
        }


        /// <summary>
        /// Returns the number of bits set in <paramref name="value"/> using <c>_mm_popcnt_u64</c>
        /// </summary>
        public static ulong popcount(ulong value)
        {
            if (Popcnt.X64.IsSupported)
            {
                return Popcnt.X64.PopCount(value);
            }
            else
            {
                return ulong.PopCount(value);
            }
        }

        /// <summary>
        /// Returns true if <paramref name="value"/> has more than one bit set.
        /// </summary>
        public static bool MoreThanOne(ulong value)
        {
            return poplsb(value) != 0;
        }

        /// <summary>
        /// Returns the number of trailing least significant zero bits in <paramref name="value"/> using <c>Bmi1.X64.TrailingZeroCount</c>. 
        /// So lsb(100_2) returns 2.
        /// </summary>
        public static int lsb(ulong value)
        {
            if (Bmi1.X64.IsSupported)
            {
                return (int)Bmi1.X64.TrailingZeroCount(value);
            }
            else
            {
                return (int)ulong.TrailingZeroCount(value);
            }
        }

        /// <summary>
        /// Sets the least significant bit to 0 using <c>Bmi1.X64.ResetLowestSetBit</c>. 
        /// So PopLsb(10110_2) returns 10100_2.
        /// </summary>
        public static ulong poplsb(ulong value)
        {
            if (Bmi1.X64.IsSupported)
            {
                return Bmi1.X64.ResetLowestSetBit(value);
            }
            else
            {
                return value & (value - 1);
            }
        }

        /// <summary>
        /// Returns the number of trailing least significant zero bits in <paramref name="value"/> using <c>_mm_tzcnt_64</c>,
        /// and clears the lowest set bit with <c>_blsr_u64</c>.
        /// </summary>
        public static unsafe int poplsb(ulong* value)
        {
            if (Bmi1.X64.IsSupported)
            {
                int sq = (int)Bmi1.X64.TrailingZeroCount(*value);
                *value = Bmi1.X64.ResetLowestSetBit(*value);
                return sq;
            }
            else
            {
                int sq = (int)ulong.TrailingZeroCount(*value);
                *value = *value & (*value - 1);
                return sq;
            }
        }

        /// <summary>
        /// Returns the index of the most significant bit (highest, toward the square H8) 
        /// set in the mask <paramref name="value"/> using <c>Lzcnt.X64.LeadingZeroCount</c>
        /// </summary>
        public static int msb(ulong value)
        {
            if (Lzcnt.X64.IsSupported)
            {
                return (int)(63 - Lzcnt.X64.LeadingZeroCount(value));
            }
            else
            {
                return BitOperations.Log2(value - 1) + 1;
            }
        }

        /// <summary>
        /// Returns <paramref name="value"/> with the most significant bit set to 0.
        /// </summary>
        public static ulong popmsb(ulong value)
        {
            return value ^ (1UL << msb(value));
        }


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


        [DllImport("libc", SetLastError = true)]
        private static extern int madvise(IntPtr addr, UIntPtr length, int advice);
        private const int MADV_HUGEPAGE = 14;
        public static unsafe void AdviseHugePage(void* addr, nuint length)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            try
            {
                int result = madvise(new IntPtr(addr), length, MADV_HUGEPAGE);
                if (result != 0)
                {
                    Console.WriteLine($"info string madvise failed with result {result} and error {Marshal.GetLastSystemError()}");
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine($"info string madvise threw {exc.GetType()}");
            }
        }
    }
}
