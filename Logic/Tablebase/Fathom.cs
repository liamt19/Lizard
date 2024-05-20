
/*

Translated from C to C# based on https://github.com/jdart1/Fathom, which uses the MIT license:

The MIT License (MIT)

Copyright (c) 2013-2018 Ronald de Man
Copyright (c) 2015 basil00
Copyright (c) 2016-2023 by Jon Dart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using static Lizard.Logic.Tablebase.Fathom;
using static Lizard.Logic.Tablebase.TBProbeHeader;
using static Lizard.Logic.Tablebase.TBProbe;
using static Lizard.Logic.Tablebase.TBProbeCore;
using static Lizard.Logic.Tablebase.TBConfig;

using TbMove = ushort;
using size_t = ulong;
using map_t = ulong;

using int8_t = sbyte;
using uint8_t = byte;
using int16_t = short;
using uint16_t = ushort;
using int32_t = int;
using uint32_t = uint;
using int64_t = long;
using uint64_t = ulong;

using unsigned = uint;
using Value = int;

using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace Lizard.Logic.Tablebase
{
    public static unsafe class Fathom
    {
        public static void LOCK(object o)
        {
            Monitor.Enter(o);
        }

        public static void UNLOCK(object o)
        {
            Monitor.Exit(o);
        }

        public static void LOCK_DESTROY(ref object o)
        {
            o = null;
        }

        public static void LOCK_INIT(ref object o)
        {
            o = new object();
        }


        public static void exit(int code)
        {
            Environment.Exit(code);
        }

        public static void* malloc(int size)
        {
            return AlignedAllocZeroed((nuint)size, 64);
        }

        public static void* malloc(ulong size)
        {
            return AlignedAllocZeroed((nuint)size, 64);
        }

        public static void* malloc(long size)
        {
            return AlignedAllocZeroed((nuint)size, 64);
        }

        public static void free(void* ptr)
        {
            NativeMemory.AlignedFree(ptr);
        }

        public static void memcpy(void* dst, void* src, int size)
        {
            Buffer.MemoryCopy(src, dst, size, size);
        }

        public static void unmap_file(void* data, MemoryMappedFile mmf)
        {
            data = null;
            mmf.Dispose();
        }

        public static bool IntBool(int i) => i != 0 ? true : false;
        public static int BoolInt(bool b) => b ? 1 : 0;
    }
}
