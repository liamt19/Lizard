namespace Lizard.Logic.NN.HalfKA_HM
{
    public static class NNCommon
    {
        public const int OutputScale = 16;
        public const int WeightScaleBits = 6;

        public const int CacheLineSize = 64;

        public const int PSQTBuckets = 8;
        public const int LayerStacks = 8;

        public const int SimdWidth = 32;
        public const int MaxSimdWidth = 32;


        /// <summary>
        /// Rounds <paramref name="n"/> up to be a multiple of <paramref name="numBase"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static int CeilToMultiple(short n, short numBase)
        {
            return (n + numBase - 1) / numBase * numBase;
        }

        //  https://stackoverflow.com/questions/19497765/equivalent-of-cs-reinterpret-cast-in-c-sharp
        public static unsafe TDest reinterpret_cast<TSource, TDest>(TSource source)
        {
            var sourceRef = __makeref(source);
            var dest = default(TDest);
            var destRef = __makeref(dest);
            *(IntPtr*)&destRef = *(IntPtr*)&sourceRef;
            return __refvalue(destRef, TDest);
        }


        public static unsafe void format_cp_ptr(int v, char* buffer)
        {
            buffer[0] = v < 0 ? '-' : v > 0 ? '+' : ' ';

            //  This reduces the displayed value of each piece so that it is more in line with
            //  conventional piece values, i.e. pawn = ~100, bishop/knight = ~300, rook = ~500
            const int Normalization = 200;
            int cp = Math.Abs(100 * v / Normalization);

            if (cp >= 10000)
            {
                buffer[1] = (char)('0' + (cp / 10000)); cp %= 10000;
                buffer[2] = (char)('0' + (cp / 1000)); cp %= 1000;
                buffer[3] = (char)('0' + (cp / 100)); cp %= 100;
                buffer[4] = (char)' ';
            }
            else if (cp >= 1000)
            {
                buffer[1] = (char)('0' + (cp / 1000)); cp %= 1000;
                buffer[2] = (char)('0' + (cp / 100)); cp %= 100;
                buffer[3] = (char)'.';
                buffer[4] = (char)('0' + (cp / 10));
            }
            else
            {
                buffer[1] = (char)('0' + (cp / 100)); cp %= 100;
                buffer[2] = (char)'.';
                buffer[3] = (char)('0' + (cp / 10)); cp %= 10;
                buffer[4] = (char)('0' + (cp / 1));
            }
        }
    }

}
