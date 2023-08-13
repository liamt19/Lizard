


namespace LTChess.Logic.NN.HalfKP
{
    public static class NNCommon
    {
        public const uint Version = 0x7AF32F20u;

        public const int OutputScale = 16;
        public const int WeightScaleBits = 6;

        public const int CacheLineSize = 64;

        public const int SimdWidth = 32;
        public const int MaxSimdWidth = 32;


        /// <summary>
        /// Rounds <paramref name="n"/> up to be a multiple of <paramref name="numBase"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToMultiple(short n, short numBase)
        {
            return (n + numBase - 1) / numBase * numBase;
        }

        //  https://stackoverflow.com/questions/19497765/equivalent-of-cs-reinterpret-cast-in-c-sharp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TDest reinterpret_cast<TSource, TDest>(TSource source)
        {
            var sourceRef = __makeref(source);
            var dest = default(TDest);
            var destRef = __makeref(dest);
            *(IntPtr*)&destRef = *(IntPtr*)&sourceRef;
            return __refvalue(destRef, TDest);
        }
    }

    public struct ExtPieceSquare
    {
        public uint[] from = new uint[2];

        public ExtPieceSquare(uint a, uint b)
        {
            from[0] = a; 
            from[1] = b;
        }

        public uint this[int i]
        {
            get
            {
                return from[i];
            }
        }
    }
}
