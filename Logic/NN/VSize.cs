using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.NN
{
    public static class VSize
    {
        public const int Vector256Size = 32;
        public const int Long = Vector256Size / sizeof(long);
        public const int Int = Vector256Size / sizeof(uint);
        public const int Short = Vector256Size / sizeof(short);
        public const int SByte = Vector256Size / sizeof(sbyte);


        public const int UInt = Vector256Size / sizeof(uint);
        public const int UShort = Vector256Size / sizeof(ushort);
        public const int Byte = Vector256Size / sizeof(byte);
    }
}
