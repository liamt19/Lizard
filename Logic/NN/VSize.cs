namespace LTChess.Logic.NN
{
    public static class VSize
    {
        public const int Vector256Size = 32;

        ///<summary> == 4 </summary>
        public const int Long = Vector256Size / sizeof(long);

        ///<summary> == 8 </summary>
        public const int Int = Vector256Size / sizeof(int);

        ///<summary> == 16 </summary>
        public const int Short = Vector256Size / sizeof(short);

        ///<summary> == 32 </summary>
        public const int SByte = Vector256Size / sizeof(sbyte);


        ///<summary> == 8 </summary>
        public const int UInt = Vector256Size / sizeof(uint);

        ///<summary> == 16 </summary>
        public const int UShort = Vector256Size / sizeof(ushort);

        ///<summary> == 32 </summary>
        public const int Byte = Vector256Size / sizeof(byte);
    }
}
