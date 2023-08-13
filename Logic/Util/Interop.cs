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

using static LTChess.Logic.Util.Interop.Pinvoke;

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
    
        
        public static bool AllocateLargePage(nint allocSize, out nint allocAddress)
        {
            allocAddress = nint.Zero;

            if (!LookupPrivilegeValue(null, "SeLockMemoryPrivilege", out var luid))
            {
                Log($"AllocateLargePage: LookupPrivilegeValue failed: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
                return false;
            }

            Log(luid.HighPart + " " + luid.LowPart);

            if (!OpenProcessToken(Process.GetCurrentProcess().SafeHandle, TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query, out var tokenHandle))
            {
                Log($"OpenProcessToken failed: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
                return false;
            }

            var tokenPrivileges = new TOKEN_PRIVILEGES { 
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };


            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, nint.Zero, out _))
            {
                tokenHandle.Dispose();
                Log($"AdjustTokenPrivileges failed: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
                return false;
            }

            tokenHandle.Dispose();
            SetLastErrorEx(0, 0);
            nint largePageSize = GetLargePageMinimum();
            if (largePageSize == 0)
            {
                Log("GetLargePageMinimum returned 0, processor doesn't support large pages!");
                return false;
            }

            Log("Large Page Minimum is " + largePageSize);

            nint alignedSize = (allocSize + largePageSize - 1) & ~((nint)(largePageSize - 1));
            Log("Aligning allocSize " + allocSize + " -> " + alignedSize);
            SetLastErrorEx(0, 0);
            allocAddress = VirtualAlloc(nint.Zero, alignedSize, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);

            if (allocAddress == nint.Zero)
            {
                Log($"VirtualAlloc failed: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");

                allocAddress = VirtualAlloc(nint.Zero, allocSize, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
                if (allocAddress == nint.Zero)
                {
                    Log($"VirtualAlloc failed for non large page: {Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()).Message}");
                }
            }

            return true;
        }



        public static class Pinvoke
        {
            [DllImport("kernel32", SetLastError = true)]
            public static extern IntPtr VirtualAlloc(IntPtr lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);

            public const int MEM_RESERVE = 0x2000;
            public const int MEM_COMMIT = 0x1000;
            public const int MEM_LARGE_PAGES = 0x20000000;
            public const int PAGE_READWRITE = 0x04;
            public const int SE_PRIVILEGE_ENABLED = 0x02;
            public const int SE_PRIVILEGE_REMOVED = 0x04;


            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool OpenProcessToken(SafeProcessHandle ProcessHandle, TokenAccessLevels DesiredAccess, out SafeAccessTokenHandle TokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool AdjustTokenPrivileges(SafeAccessTokenHandle TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, nint PreviousState, out uint ReturnLength);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern nint GetLargePageMinimum();

            [DllImport("user32.dll", SetLastError = true)]
            public static extern void SetLastErrorEx(uint dwErrCode, uint dwType);

            public struct TOKEN_PRIVILEGES
            {
                public int PrivilegeCount;
                public LUID Luid;
                public UInt32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            public struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public UInt32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public uint LowPart;
                public uint HighPart;
            }

            [Flags]
            public enum AllocationType
            {
                Commit = 0x1000,
                Reserve = 0x2000,
                Decommit = 0x4000,
                Release = 0x8000,
                Reset = 0x80000,
                Physical = 0x400000,
                TopDown = 0x100000,
                WriteWatch = 0x200000,
                LargePages = 0x20000000
            }

            [Flags]
            public enum MemoryProtection
            {
                Execute = 0x10,
                ExecuteRead = 0x20,
                ExecuteReadWrite = 0x40,
                ExecuteWriteCopy = 0x80,
                NoAccess = 0x01,
                ReadOnly = 0x02,
                ReadWrite = 0x04,
                WriteCopy = 0x08,
                GuardModifierflag = 0x100,
                NoCacheModifierflag = 0x200,
                WriteCombineModifierflag = 0x400
            }
        }
    }
}
