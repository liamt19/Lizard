using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace LTChess.Logic.Util
{

    public static class Interop
    {

        //http://graphics.stanford.edu/%7Eseander/bithacks.html
        private static int[] BitScanValues = {
            0,  1,  48,  2, 57, 49, 28,  3,
            61, 58, 50, 42, 38, 29, 17,  4,
            62, 55, 59, 36, 53, 51, 43, 22,
            45, 39, 33, 30, 24, 18, 12,  5,
            63, 47, 56, 27, 60, 41, 37, 16,
            54, 35, 52, 21, 44, 32, 23, 11,
            46, 26, 40, 15, 34, 20, 31, 10,
            25, 14, 19,  9, 13,  8,  7,  6
        };


        /// <summary>
        /// Returns the number of bits set in <paramref name="value"/> using <c>Popcnt.X64.PopCount(<paramref name="value"/>)</c>
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong popcount(ulong value)
        {
            if (Popcnt.X64.IsSupported)
            {
                return Popcnt.X64.PopCount(value);
            }
            else
            {
                var count = 0ul;
                while (value > 0)
                {
                    value = poplsb(value);
                    count++;
                }

                return count;
            }
        }

        [MethodImpl(Inline)]
        public static bool MoreThanOne(ulong u)
        {
            return (poplsb(u) != 0);
        }

        /// <summary>
        /// Returns the number of trailing least significant zero bits in <paramref name="value"/> using <c>Bmi1.X64.TrailingZeroCount</c>. 
        /// So lsb(100_2) returns 2.
        /// </summary>
        [MethodImpl(Inline)]
        public static int lsb(ulong value)
        {
            if (Bmi1.X64.IsSupported)
            {
                return (int)Bmi1.X64.TrailingZeroCount(value);
            }
            else
            {
                return BitScanValues[((ulong)((long)value & -(long)value) * 0x03F79D71B4CB0A89) >> 58];
            }
        }

        /// <summary>
        /// Sets the least significant bit to 0 using <c>Bmi1.X64.ResetLowestSetBit</c>. 
        /// So PopLsb(10110_2) returns 10100_2.
        /// </summary>
        [MethodImpl(Inline)]
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
        /// Returns the index of the most significant bit (highest, toward the square H8) 
        /// set in the mask <paramref name="value"/> using <c>Lzcnt.X64.LeadingZeroCount</c>
        /// </summary>
        [MethodImpl(Inline)]
        public static int msb(ulong value)
        {
            if (Lzcnt.X64.IsSupported)
            {
                return (int)(63 - Lzcnt.X64.LeadingZeroCount(value));
            }
            else
            {
                return (BitOperations.Log2(value - 1) + 1);
            }
        }

        /// <summary>
        /// Sets the most significant bit to 0. 
        /// So popmsb(10110_2) returns 00110_2.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong popmsb(ulong value)
        {
            return value ^ (1UL << msb(value));
        }



        [MethodImpl(Inline)]
        public static ulong pext(ulong b, ulong mask)
        {
            if (Bmi2.X64.IsSupported)
            {
                return Bmi2.X64.ParallelBitExtract(b, mask);
            }
            else
            {
                ulong res = 0;
                for (ulong bb = 1; mask != 0; bb += bb)
                {
                    if ((b & mask & (0UL - mask)) != 0)
                    {
                        res |= bb;
                    }
                    mask &= mask - 1;
                }
                return res;
            }
        }
    

    }
}
