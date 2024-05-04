
#if AVX512
using VectorT = System.Runtime.Intrinsics.Vector512;
#else
using VectorT = System.Runtime.Intrinsics.Vector256;
#endif

namespace Lizard.Logic.NN
{
    public static unsafe class FunUnrollThings
    {

#if AVX512
        private const int N = 32;
#else
        private const int N = 16;
#endif

        private const int HL = Bucketed768.HiddenSize;
        private const int StopBefore = HL / N;

        private const int AVX512_1024HL = 1024 / 32;
        private const int AVX512_1536HL = 1536 / 32;

        private const int AVX256_1024HL = 1024 / 16;
        private const int AVX256_1536HL = 1536 / 16;


        public static void SubAdd(short* src, short* dst, short* sub1, short* add1)
        {
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 0 * N), VectorT.Load(add1 + 0 * N)), VectorT.Load(sub1 + 0 * N)), dst + 0 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 1 * N), VectorT.Load(add1 + 1 * N)), VectorT.Load(sub1 + 1 * N)), dst + 1 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 2 * N), VectorT.Load(add1 + 2 * N)), VectorT.Load(sub1 + 2 * N)), dst + 2 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 3 * N), VectorT.Load(add1 + 3 * N)), VectorT.Load(sub1 + 3 * N)), dst + 3 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 4 * N), VectorT.Load(add1 + 4 * N)), VectorT.Load(sub1 + 4 * N)), dst + 4 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 5 * N), VectorT.Load(add1 + 5 * N)), VectorT.Load(sub1 + 5 * N)), dst + 5 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 6 * N), VectorT.Load(add1 + 6 * N)), VectorT.Load(sub1 + 6 * N)), dst + 6 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 7 * N), VectorT.Load(add1 + 7 * N)), VectorT.Load(sub1 + 7 * N)), dst + 7 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 8 * N), VectorT.Load(add1 + 8 * N)), VectorT.Load(sub1 + 8 * N)), dst + 8 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 9 * N), VectorT.Load(add1 + 9 * N)), VectorT.Load(sub1 + 9 * N)), dst + 9 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 10 * N), VectorT.Load(add1 + 10 * N)), VectorT.Load(sub1 + 10 * N)), dst + 10 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 11 * N), VectorT.Load(add1 + 11 * N)), VectorT.Load(sub1 + 11 * N)), dst + 11 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 12 * N), VectorT.Load(add1 + 12 * N)), VectorT.Load(sub1 + 12 * N)), dst + 12 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 13 * N), VectorT.Load(add1 + 13 * N)), VectorT.Load(sub1 + 13 * N)), dst + 13 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 14 * N), VectorT.Load(add1 + 14 * N)), VectorT.Load(sub1 + 14 * N)), dst + 14 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 15 * N), VectorT.Load(add1 + 15 * N)), VectorT.Load(sub1 + 15 * N)), dst + 15 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 16 * N), VectorT.Load(add1 + 16 * N)), VectorT.Load(sub1 + 16 * N)), dst + 16 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 17 * N), VectorT.Load(add1 + 17 * N)), VectorT.Load(sub1 + 17 * N)), dst + 17 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 18 * N), VectorT.Load(add1 + 18 * N)), VectorT.Load(sub1 + 18 * N)), dst + 18 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 19 * N), VectorT.Load(add1 + 19 * N)), VectorT.Load(sub1 + 19 * N)), dst + 19 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 20 * N), VectorT.Load(add1 + 20 * N)), VectorT.Load(sub1 + 20 * N)), dst + 20 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 21 * N), VectorT.Load(add1 + 21 * N)), VectorT.Load(sub1 + 21 * N)), dst + 21 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 22 * N), VectorT.Load(add1 + 22 * N)), VectorT.Load(sub1 + 22 * N)), dst + 22 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 23 * N), VectorT.Load(add1 + 23 * N)), VectorT.Load(sub1 + 23 * N)), dst + 23 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 24 * N), VectorT.Load(add1 + 24 * N)), VectorT.Load(sub1 + 24 * N)), dst + 24 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 25 * N), VectorT.Load(add1 + 25 * N)), VectorT.Load(sub1 + 25 * N)), dst + 25 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 26 * N), VectorT.Load(add1 + 26 * N)), VectorT.Load(sub1 + 26 * N)), dst + 26 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 27 * N), VectorT.Load(add1 + 27 * N)), VectorT.Load(sub1 + 27 * N)), dst + 27 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 28 * N), VectorT.Load(add1 + 28 * N)), VectorT.Load(sub1 + 28 * N)), dst + 28 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 29 * N), VectorT.Load(add1 + 29 * N)), VectorT.Load(sub1 + 29 * N)), dst + 29 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 30 * N), VectorT.Load(add1 + 30 * N)), VectorT.Load(sub1 + 30 * N)), dst + 30 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 31 * N), VectorT.Load(add1 + 31 * N)), VectorT.Load(sub1 + 31 * N)), dst + 31 * N);

            if (StopBefore == AVX512_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 32 * N), VectorT.Load(add1 + 32 * N)), VectorT.Load(sub1 + 32 * N)), dst + 32 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 33 * N), VectorT.Load(add1 + 33 * N)), VectorT.Load(sub1 + 33 * N)), dst + 33 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 34 * N), VectorT.Load(add1 + 34 * N)), VectorT.Load(sub1 + 34 * N)), dst + 34 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 35 * N), VectorT.Load(add1 + 35 * N)), VectorT.Load(sub1 + 35 * N)), dst + 35 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 36 * N), VectorT.Load(add1 + 36 * N)), VectorT.Load(sub1 + 36 * N)), dst + 36 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 37 * N), VectorT.Load(add1 + 37 * N)), VectorT.Load(sub1 + 37 * N)), dst + 37 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 38 * N), VectorT.Load(add1 + 38 * N)), VectorT.Load(sub1 + 38 * N)), dst + 38 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 39 * N), VectorT.Load(add1 + 39 * N)), VectorT.Load(sub1 + 39 * N)), dst + 39 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 40 * N), VectorT.Load(add1 + 40 * N)), VectorT.Load(sub1 + 40 * N)), dst + 40 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 41 * N), VectorT.Load(add1 + 41 * N)), VectorT.Load(sub1 + 41 * N)), dst + 41 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 42 * N), VectorT.Load(add1 + 42 * N)), VectorT.Load(sub1 + 42 * N)), dst + 42 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 43 * N), VectorT.Load(add1 + 43 * N)), VectorT.Load(sub1 + 43 * N)), dst + 43 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 44 * N), VectorT.Load(add1 + 44 * N)), VectorT.Load(sub1 + 44 * N)), dst + 44 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 45 * N), VectorT.Load(add1 + 45 * N)), VectorT.Load(sub1 + 45 * N)), dst + 45 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 46 * N), VectorT.Load(add1 + 46 * N)), VectorT.Load(sub1 + 46 * N)), dst + 46 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 47 * N), VectorT.Load(add1 + 47 * N)), VectorT.Load(sub1 + 47 * N)), dst + 47 * N);

            if (StopBefore == AVX512_1536HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 48 * N), VectorT.Load(add1 + 48 * N)), VectorT.Load(sub1 + 48 * N)), dst + 48 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 49 * N), VectorT.Load(add1 + 49 * N)), VectorT.Load(sub1 + 49 * N)), dst + 49 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 50 * N), VectorT.Load(add1 + 50 * N)), VectorT.Load(sub1 + 50 * N)), dst + 50 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 51 * N), VectorT.Load(add1 + 51 * N)), VectorT.Load(sub1 + 51 * N)), dst + 51 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 52 * N), VectorT.Load(add1 + 52 * N)), VectorT.Load(sub1 + 52 * N)), dst + 52 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 53 * N), VectorT.Load(add1 + 53 * N)), VectorT.Load(sub1 + 53 * N)), dst + 53 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 54 * N), VectorT.Load(add1 + 54 * N)), VectorT.Load(sub1 + 54 * N)), dst + 54 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 55 * N), VectorT.Load(add1 + 55 * N)), VectorT.Load(sub1 + 55 * N)), dst + 55 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 56 * N), VectorT.Load(add1 + 56 * N)), VectorT.Load(sub1 + 56 * N)), dst + 56 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 57 * N), VectorT.Load(add1 + 57 * N)), VectorT.Load(sub1 + 57 * N)), dst + 57 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 58 * N), VectorT.Load(add1 + 58 * N)), VectorT.Load(sub1 + 58 * N)), dst + 58 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 59 * N), VectorT.Load(add1 + 59 * N)), VectorT.Load(sub1 + 59 * N)), dst + 59 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 60 * N), VectorT.Load(add1 + 60 * N)), VectorT.Load(sub1 + 60 * N)), dst + 60 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 61 * N), VectorT.Load(add1 + 61 * N)), VectorT.Load(sub1 + 61 * N)), dst + 61 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 62 * N), VectorT.Load(add1 + 62 * N)), VectorT.Load(sub1 + 62 * N)), dst + 62 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 63 * N), VectorT.Load(add1 + 63 * N)), VectorT.Load(sub1 + 63 * N)), dst + 63 * N);

            if (StopBefore == AVX256_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 64 * N), VectorT.Load(add1 + 64 * N)), VectorT.Load(sub1 + 64 * N)), dst + 64 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 65 * N), VectorT.Load(add1 + 65 * N)), VectorT.Load(sub1 + 65 * N)), dst + 65 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 66 * N), VectorT.Load(add1 + 66 * N)), VectorT.Load(sub1 + 66 * N)), dst + 66 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 67 * N), VectorT.Load(add1 + 67 * N)), VectorT.Load(sub1 + 67 * N)), dst + 67 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 68 * N), VectorT.Load(add1 + 68 * N)), VectorT.Load(sub1 + 68 * N)), dst + 68 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 69 * N), VectorT.Load(add1 + 69 * N)), VectorT.Load(sub1 + 69 * N)), dst + 69 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 70 * N), VectorT.Load(add1 + 70 * N)), VectorT.Load(sub1 + 70 * N)), dst + 70 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 71 * N), VectorT.Load(add1 + 71 * N)), VectorT.Load(sub1 + 71 * N)), dst + 71 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 72 * N), VectorT.Load(add1 + 72 * N)), VectorT.Load(sub1 + 72 * N)), dst + 72 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 73 * N), VectorT.Load(add1 + 73 * N)), VectorT.Load(sub1 + 73 * N)), dst + 73 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 74 * N), VectorT.Load(add1 + 74 * N)), VectorT.Load(sub1 + 74 * N)), dst + 74 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 75 * N), VectorT.Load(add1 + 75 * N)), VectorT.Load(sub1 + 75 * N)), dst + 75 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 76 * N), VectorT.Load(add1 + 76 * N)), VectorT.Load(sub1 + 76 * N)), dst + 76 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 77 * N), VectorT.Load(add1 + 77 * N)), VectorT.Load(sub1 + 77 * N)), dst + 77 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 78 * N), VectorT.Load(add1 + 78 * N)), VectorT.Load(sub1 + 78 * N)), dst + 78 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 79 * N), VectorT.Load(add1 + 79 * N)), VectorT.Load(sub1 + 79 * N)), dst + 79 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 80 * N), VectorT.Load(add1 + 80 * N)), VectorT.Load(sub1 + 80 * N)), dst + 80 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 81 * N), VectorT.Load(add1 + 81 * N)), VectorT.Load(sub1 + 81 * N)), dst + 81 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 82 * N), VectorT.Load(add1 + 82 * N)), VectorT.Load(sub1 + 82 * N)), dst + 82 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 83 * N), VectorT.Load(add1 + 83 * N)), VectorT.Load(sub1 + 83 * N)), dst + 83 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 84 * N), VectorT.Load(add1 + 84 * N)), VectorT.Load(sub1 + 84 * N)), dst + 84 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 85 * N), VectorT.Load(add1 + 85 * N)), VectorT.Load(sub1 + 85 * N)), dst + 85 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 86 * N), VectorT.Load(add1 + 86 * N)), VectorT.Load(sub1 + 86 * N)), dst + 86 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 87 * N), VectorT.Load(add1 + 87 * N)), VectorT.Load(sub1 + 87 * N)), dst + 87 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 88 * N), VectorT.Load(add1 + 88 * N)), VectorT.Load(sub1 + 88 * N)), dst + 88 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 89 * N), VectorT.Load(add1 + 89 * N)), VectorT.Load(sub1 + 89 * N)), dst + 89 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 90 * N), VectorT.Load(add1 + 90 * N)), VectorT.Load(sub1 + 90 * N)), dst + 90 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 91 * N), VectorT.Load(add1 + 91 * N)), VectorT.Load(sub1 + 91 * N)), dst + 91 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 92 * N), VectorT.Load(add1 + 92 * N)), VectorT.Load(sub1 + 92 * N)), dst + 92 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 93 * N), VectorT.Load(add1 + 93 * N)), VectorT.Load(sub1 + 93 * N)), dst + 93 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 94 * N), VectorT.Load(add1 + 94 * N)), VectorT.Load(sub1 + 94 * N)), dst + 94 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 95 * N), VectorT.Load(add1 + 95 * N)), VectorT.Load(sub1 + 95 * N)), dst + 95 * N);

        }


        public static void SubSubAdd(short* src, short* dst, short* sub1, short* sub2, short* add1)
        {
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 0 * N), VectorT.Load(add1 + 0 * N)), VectorT.Load(sub1 + 0 * N)), VectorT.Load(sub2 + 0 * N)), dst + 0 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 1 * N), VectorT.Load(add1 + 1 * N)), VectorT.Load(sub1 + 1 * N)), VectorT.Load(sub2 + 1 * N)), dst + 1 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 2 * N), VectorT.Load(add1 + 2 * N)), VectorT.Load(sub1 + 2 * N)), VectorT.Load(sub2 + 2 * N)), dst + 2 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 3 * N), VectorT.Load(add1 + 3 * N)), VectorT.Load(sub1 + 3 * N)), VectorT.Load(sub2 + 3 * N)), dst + 3 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 4 * N), VectorT.Load(add1 + 4 * N)), VectorT.Load(sub1 + 4 * N)), VectorT.Load(sub2 + 4 * N)), dst + 4 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 5 * N), VectorT.Load(add1 + 5 * N)), VectorT.Load(sub1 + 5 * N)), VectorT.Load(sub2 + 5 * N)), dst + 5 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 6 * N), VectorT.Load(add1 + 6 * N)), VectorT.Load(sub1 + 6 * N)), VectorT.Load(sub2 + 6 * N)), dst + 6 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 7 * N), VectorT.Load(add1 + 7 * N)), VectorT.Load(sub1 + 7 * N)), VectorT.Load(sub2 + 7 * N)), dst + 7 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 8 * N), VectorT.Load(add1 + 8 * N)), VectorT.Load(sub1 + 8 * N)), VectorT.Load(sub2 + 8 * N)), dst + 8 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 9 * N), VectorT.Load(add1 + 9 * N)), VectorT.Load(sub1 + 9 * N)), VectorT.Load(sub2 + 9 * N)), dst + 9 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 10 * N), VectorT.Load(add1 + 10 * N)), VectorT.Load(sub1 + 10 * N)), VectorT.Load(sub2 + 10 * N)), dst + 10 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 11 * N), VectorT.Load(add1 + 11 * N)), VectorT.Load(sub1 + 11 * N)), VectorT.Load(sub2 + 11 * N)), dst + 11 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 12 * N), VectorT.Load(add1 + 12 * N)), VectorT.Load(sub1 + 12 * N)), VectorT.Load(sub2 + 12 * N)), dst + 12 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 13 * N), VectorT.Load(add1 + 13 * N)), VectorT.Load(sub1 + 13 * N)), VectorT.Load(sub2 + 13 * N)), dst + 13 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 14 * N), VectorT.Load(add1 + 14 * N)), VectorT.Load(sub1 + 14 * N)), VectorT.Load(sub2 + 14 * N)), dst + 14 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 15 * N), VectorT.Load(add1 + 15 * N)), VectorT.Load(sub1 + 15 * N)), VectorT.Load(sub2 + 15 * N)), dst + 15 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 16 * N), VectorT.Load(add1 + 16 * N)), VectorT.Load(sub1 + 16 * N)), VectorT.Load(sub2 + 16 * N)), dst + 16 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 17 * N), VectorT.Load(add1 + 17 * N)), VectorT.Load(sub1 + 17 * N)), VectorT.Load(sub2 + 17 * N)), dst + 17 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 18 * N), VectorT.Load(add1 + 18 * N)), VectorT.Load(sub1 + 18 * N)), VectorT.Load(sub2 + 18 * N)), dst + 18 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 19 * N), VectorT.Load(add1 + 19 * N)), VectorT.Load(sub1 + 19 * N)), VectorT.Load(sub2 + 19 * N)), dst + 19 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 20 * N), VectorT.Load(add1 + 20 * N)), VectorT.Load(sub1 + 20 * N)), VectorT.Load(sub2 + 20 * N)), dst + 20 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 21 * N), VectorT.Load(add1 + 21 * N)), VectorT.Load(sub1 + 21 * N)), VectorT.Load(sub2 + 21 * N)), dst + 21 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 22 * N), VectorT.Load(add1 + 22 * N)), VectorT.Load(sub1 + 22 * N)), VectorT.Load(sub2 + 22 * N)), dst + 22 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 23 * N), VectorT.Load(add1 + 23 * N)), VectorT.Load(sub1 + 23 * N)), VectorT.Load(sub2 + 23 * N)), dst + 23 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 24 * N), VectorT.Load(add1 + 24 * N)), VectorT.Load(sub1 + 24 * N)), VectorT.Load(sub2 + 24 * N)), dst + 24 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 25 * N), VectorT.Load(add1 + 25 * N)), VectorT.Load(sub1 + 25 * N)), VectorT.Load(sub2 + 25 * N)), dst + 25 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 26 * N), VectorT.Load(add1 + 26 * N)), VectorT.Load(sub1 + 26 * N)), VectorT.Load(sub2 + 26 * N)), dst + 26 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 27 * N), VectorT.Load(add1 + 27 * N)), VectorT.Load(sub1 + 27 * N)), VectorT.Load(sub2 + 27 * N)), dst + 27 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 28 * N), VectorT.Load(add1 + 28 * N)), VectorT.Load(sub1 + 28 * N)), VectorT.Load(sub2 + 28 * N)), dst + 28 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 29 * N), VectorT.Load(add1 + 29 * N)), VectorT.Load(sub1 + 29 * N)), VectorT.Load(sub2 + 29 * N)), dst + 29 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 30 * N), VectorT.Load(add1 + 30 * N)), VectorT.Load(sub1 + 30 * N)), VectorT.Load(sub2 + 30 * N)), dst + 30 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 31 * N), VectorT.Load(add1 + 31 * N)), VectorT.Load(sub1 + 31 * N)), VectorT.Load(sub2 + 31 * N)), dst + 31 * N);

            if (StopBefore == AVX512_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 32 * N), VectorT.Load(add1 + 32 * N)), VectorT.Load(sub1 + 32 * N)), VectorT.Load(sub2 + 32 * N)), dst + 32 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 33 * N), VectorT.Load(add1 + 33 * N)), VectorT.Load(sub1 + 33 * N)), VectorT.Load(sub2 + 33 * N)), dst + 33 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 34 * N), VectorT.Load(add1 + 34 * N)), VectorT.Load(sub1 + 34 * N)), VectorT.Load(sub2 + 34 * N)), dst + 34 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 35 * N), VectorT.Load(add1 + 35 * N)), VectorT.Load(sub1 + 35 * N)), VectorT.Load(sub2 + 35 * N)), dst + 35 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 36 * N), VectorT.Load(add1 + 36 * N)), VectorT.Load(sub1 + 36 * N)), VectorT.Load(sub2 + 36 * N)), dst + 36 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 37 * N), VectorT.Load(add1 + 37 * N)), VectorT.Load(sub1 + 37 * N)), VectorT.Load(sub2 + 37 * N)), dst + 37 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 38 * N), VectorT.Load(add1 + 38 * N)), VectorT.Load(sub1 + 38 * N)), VectorT.Load(sub2 + 38 * N)), dst + 38 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 39 * N), VectorT.Load(add1 + 39 * N)), VectorT.Load(sub1 + 39 * N)), VectorT.Load(sub2 + 39 * N)), dst + 39 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 40 * N), VectorT.Load(add1 + 40 * N)), VectorT.Load(sub1 + 40 * N)), VectorT.Load(sub2 + 40 * N)), dst + 40 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 41 * N), VectorT.Load(add1 + 41 * N)), VectorT.Load(sub1 + 41 * N)), VectorT.Load(sub2 + 41 * N)), dst + 41 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 42 * N), VectorT.Load(add1 + 42 * N)), VectorT.Load(sub1 + 42 * N)), VectorT.Load(sub2 + 42 * N)), dst + 42 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 43 * N), VectorT.Load(add1 + 43 * N)), VectorT.Load(sub1 + 43 * N)), VectorT.Load(sub2 + 43 * N)), dst + 43 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 44 * N), VectorT.Load(add1 + 44 * N)), VectorT.Load(sub1 + 44 * N)), VectorT.Load(sub2 + 44 * N)), dst + 44 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 45 * N), VectorT.Load(add1 + 45 * N)), VectorT.Load(sub1 + 45 * N)), VectorT.Load(sub2 + 45 * N)), dst + 45 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 46 * N), VectorT.Load(add1 + 46 * N)), VectorT.Load(sub1 + 46 * N)), VectorT.Load(sub2 + 46 * N)), dst + 46 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 47 * N), VectorT.Load(add1 + 47 * N)), VectorT.Load(sub1 + 47 * N)), VectorT.Load(sub2 + 47 * N)), dst + 47 * N);

            if (StopBefore == AVX512_1536HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 48 * N), VectorT.Load(add1 + 48 * N)), VectorT.Load(sub1 + 48 * N)), VectorT.Load(sub2 + 48 * N)), dst + 48 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 49 * N), VectorT.Load(add1 + 49 * N)), VectorT.Load(sub1 + 49 * N)), VectorT.Load(sub2 + 49 * N)), dst + 49 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 50 * N), VectorT.Load(add1 + 50 * N)), VectorT.Load(sub1 + 50 * N)), VectorT.Load(sub2 + 50 * N)), dst + 50 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 51 * N), VectorT.Load(add1 + 51 * N)), VectorT.Load(sub1 + 51 * N)), VectorT.Load(sub2 + 51 * N)), dst + 51 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 52 * N), VectorT.Load(add1 + 52 * N)), VectorT.Load(sub1 + 52 * N)), VectorT.Load(sub2 + 52 * N)), dst + 52 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 53 * N), VectorT.Load(add1 + 53 * N)), VectorT.Load(sub1 + 53 * N)), VectorT.Load(sub2 + 53 * N)), dst + 53 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 54 * N), VectorT.Load(add1 + 54 * N)), VectorT.Load(sub1 + 54 * N)), VectorT.Load(sub2 + 54 * N)), dst + 54 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 55 * N), VectorT.Load(add1 + 55 * N)), VectorT.Load(sub1 + 55 * N)), VectorT.Load(sub2 + 55 * N)), dst + 55 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 56 * N), VectorT.Load(add1 + 56 * N)), VectorT.Load(sub1 + 56 * N)), VectorT.Load(sub2 + 56 * N)), dst + 56 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 57 * N), VectorT.Load(add1 + 57 * N)), VectorT.Load(sub1 + 57 * N)), VectorT.Load(sub2 + 57 * N)), dst + 57 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 58 * N), VectorT.Load(add1 + 58 * N)), VectorT.Load(sub1 + 58 * N)), VectorT.Load(sub2 + 58 * N)), dst + 58 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 59 * N), VectorT.Load(add1 + 59 * N)), VectorT.Load(sub1 + 59 * N)), VectorT.Load(sub2 + 59 * N)), dst + 59 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 60 * N), VectorT.Load(add1 + 60 * N)), VectorT.Load(sub1 + 60 * N)), VectorT.Load(sub2 + 60 * N)), dst + 60 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 61 * N), VectorT.Load(add1 + 61 * N)), VectorT.Load(sub1 + 61 * N)), VectorT.Load(sub2 + 61 * N)), dst + 61 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 62 * N), VectorT.Load(add1 + 62 * N)), VectorT.Load(sub1 + 62 * N)), VectorT.Load(sub2 + 62 * N)), dst + 62 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 63 * N), VectorT.Load(add1 + 63 * N)), VectorT.Load(sub1 + 63 * N)), VectorT.Load(sub2 + 63 * N)), dst + 63 * N);

            if (StopBefore == AVX256_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 64 * N), VectorT.Load(add1 + 64 * N)), VectorT.Load(sub1 + 64 * N)), VectorT.Load(sub2 + 64 * N)), dst + 64 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 65 * N), VectorT.Load(add1 + 65 * N)), VectorT.Load(sub1 + 65 * N)), VectorT.Load(sub2 + 65 * N)), dst + 65 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 66 * N), VectorT.Load(add1 + 66 * N)), VectorT.Load(sub1 + 66 * N)), VectorT.Load(sub2 + 66 * N)), dst + 66 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 67 * N), VectorT.Load(add1 + 67 * N)), VectorT.Load(sub1 + 67 * N)), VectorT.Load(sub2 + 67 * N)), dst + 67 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 68 * N), VectorT.Load(add1 + 68 * N)), VectorT.Load(sub1 + 68 * N)), VectorT.Load(sub2 + 68 * N)), dst + 68 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 69 * N), VectorT.Load(add1 + 69 * N)), VectorT.Load(sub1 + 69 * N)), VectorT.Load(sub2 + 69 * N)), dst + 69 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 70 * N), VectorT.Load(add1 + 70 * N)), VectorT.Load(sub1 + 70 * N)), VectorT.Load(sub2 + 70 * N)), dst + 70 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 71 * N), VectorT.Load(add1 + 71 * N)), VectorT.Load(sub1 + 71 * N)), VectorT.Load(sub2 + 71 * N)), dst + 71 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 72 * N), VectorT.Load(add1 + 72 * N)), VectorT.Load(sub1 + 72 * N)), VectorT.Load(sub2 + 72 * N)), dst + 72 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 73 * N), VectorT.Load(add1 + 73 * N)), VectorT.Load(sub1 + 73 * N)), VectorT.Load(sub2 + 73 * N)), dst + 73 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 74 * N), VectorT.Load(add1 + 74 * N)), VectorT.Load(sub1 + 74 * N)), VectorT.Load(sub2 + 74 * N)), dst + 74 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 75 * N), VectorT.Load(add1 + 75 * N)), VectorT.Load(sub1 + 75 * N)), VectorT.Load(sub2 + 75 * N)), dst + 75 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 76 * N), VectorT.Load(add1 + 76 * N)), VectorT.Load(sub1 + 76 * N)), VectorT.Load(sub2 + 76 * N)), dst + 76 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 77 * N), VectorT.Load(add1 + 77 * N)), VectorT.Load(sub1 + 77 * N)), VectorT.Load(sub2 + 77 * N)), dst + 77 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 78 * N), VectorT.Load(add1 + 78 * N)), VectorT.Load(sub1 + 78 * N)), VectorT.Load(sub2 + 78 * N)), dst + 78 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 79 * N), VectorT.Load(add1 + 79 * N)), VectorT.Load(sub1 + 79 * N)), VectorT.Load(sub2 + 79 * N)), dst + 79 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 80 * N), VectorT.Load(add1 + 80 * N)), VectorT.Load(sub1 + 80 * N)), VectorT.Load(sub2 + 80 * N)), dst + 80 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 81 * N), VectorT.Load(add1 + 81 * N)), VectorT.Load(sub1 + 81 * N)), VectorT.Load(sub2 + 81 * N)), dst + 81 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 82 * N), VectorT.Load(add1 + 82 * N)), VectorT.Load(sub1 + 82 * N)), VectorT.Load(sub2 + 82 * N)), dst + 82 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 83 * N), VectorT.Load(add1 + 83 * N)), VectorT.Load(sub1 + 83 * N)), VectorT.Load(sub2 + 83 * N)), dst + 83 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 84 * N), VectorT.Load(add1 + 84 * N)), VectorT.Load(sub1 + 84 * N)), VectorT.Load(sub2 + 84 * N)), dst + 84 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 85 * N), VectorT.Load(add1 + 85 * N)), VectorT.Load(sub1 + 85 * N)), VectorT.Load(sub2 + 85 * N)), dst + 85 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 86 * N), VectorT.Load(add1 + 86 * N)), VectorT.Load(sub1 + 86 * N)), VectorT.Load(sub2 + 86 * N)), dst + 86 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 87 * N), VectorT.Load(add1 + 87 * N)), VectorT.Load(sub1 + 87 * N)), VectorT.Load(sub2 + 87 * N)), dst + 87 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 88 * N), VectorT.Load(add1 + 88 * N)), VectorT.Load(sub1 + 88 * N)), VectorT.Load(sub2 + 88 * N)), dst + 88 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 89 * N), VectorT.Load(add1 + 89 * N)), VectorT.Load(sub1 + 89 * N)), VectorT.Load(sub2 + 89 * N)), dst + 89 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 90 * N), VectorT.Load(add1 + 90 * N)), VectorT.Load(sub1 + 90 * N)), VectorT.Load(sub2 + 90 * N)), dst + 90 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 91 * N), VectorT.Load(add1 + 91 * N)), VectorT.Load(sub1 + 91 * N)), VectorT.Load(sub2 + 91 * N)), dst + 91 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 92 * N), VectorT.Load(add1 + 92 * N)), VectorT.Load(sub1 + 92 * N)), VectorT.Load(sub2 + 92 * N)), dst + 92 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 93 * N), VectorT.Load(add1 + 93 * N)), VectorT.Load(sub1 + 93 * N)), VectorT.Load(sub2 + 93 * N)), dst + 93 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 94 * N), VectorT.Load(add1 + 94 * N)), VectorT.Load(sub1 + 94 * N)), VectorT.Load(sub2 + 94 * N)), dst + 94 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Load(src + 95 * N), VectorT.Load(add1 + 95 * N)), VectorT.Load(sub1 + 95 * N)), VectorT.Load(sub2 + 95 * N)), dst + 95 * N);

        }


        public static void SubSubAddAdd(short* src, short* dst, short* sub1, short* sub2, short* add1, short* add2)
        {
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 0 * N), VectorT.Load(add1 + 0 * N)), VectorT.Load(add2 + 0 * N)), VectorT.Load(sub1 + 0 * N)), VectorT.Load(sub2 + 0 * N)), dst + 0 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 1 * N), VectorT.Load(add1 + 1 * N)), VectorT.Load(add2 + 1 * N)), VectorT.Load(sub1 + 1 * N)), VectorT.Load(sub2 + 1 * N)), dst + 1 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 2 * N), VectorT.Load(add1 + 2 * N)), VectorT.Load(add2 + 2 * N)), VectorT.Load(sub1 + 2 * N)), VectorT.Load(sub2 + 2 * N)), dst + 2 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 3 * N), VectorT.Load(add1 + 3 * N)), VectorT.Load(add2 + 3 * N)), VectorT.Load(sub1 + 3 * N)), VectorT.Load(sub2 + 3 * N)), dst + 3 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 4 * N), VectorT.Load(add1 + 4 * N)), VectorT.Load(add2 + 4 * N)), VectorT.Load(sub1 + 4 * N)), VectorT.Load(sub2 + 4 * N)), dst + 4 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 5 * N), VectorT.Load(add1 + 5 * N)), VectorT.Load(add2 + 5 * N)), VectorT.Load(sub1 + 5 * N)), VectorT.Load(sub2 + 5 * N)), dst + 5 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 6 * N), VectorT.Load(add1 + 6 * N)), VectorT.Load(add2 + 6 * N)), VectorT.Load(sub1 + 6 * N)), VectorT.Load(sub2 + 6 * N)), dst + 6 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 7 * N), VectorT.Load(add1 + 7 * N)), VectorT.Load(add2 + 7 * N)), VectorT.Load(sub1 + 7 * N)), VectorT.Load(sub2 + 7 * N)), dst + 7 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 8 * N), VectorT.Load(add1 + 8 * N)), VectorT.Load(add2 + 8 * N)), VectorT.Load(sub1 + 8 * N)), VectorT.Load(sub2 + 8 * N)), dst + 8 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 9 * N), VectorT.Load(add1 + 9 * N)), VectorT.Load(add2 + 9 * N)), VectorT.Load(sub1 + 9 * N)), VectorT.Load(sub2 + 9 * N)), dst + 9 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 10 * N), VectorT.Load(add1 + 10 * N)), VectorT.Load(add2 + 10 * N)), VectorT.Load(sub1 + 10 * N)), VectorT.Load(sub2 + 10 * N)), dst + 10 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 11 * N), VectorT.Load(add1 + 11 * N)), VectorT.Load(add2 + 11 * N)), VectorT.Load(sub1 + 11 * N)), VectorT.Load(sub2 + 11 * N)), dst + 11 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 12 * N), VectorT.Load(add1 + 12 * N)), VectorT.Load(add2 + 12 * N)), VectorT.Load(sub1 + 12 * N)), VectorT.Load(sub2 + 12 * N)), dst + 12 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 13 * N), VectorT.Load(add1 + 13 * N)), VectorT.Load(add2 + 13 * N)), VectorT.Load(sub1 + 13 * N)), VectorT.Load(sub2 + 13 * N)), dst + 13 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 14 * N), VectorT.Load(add1 + 14 * N)), VectorT.Load(add2 + 14 * N)), VectorT.Load(sub1 + 14 * N)), VectorT.Load(sub2 + 14 * N)), dst + 14 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 15 * N), VectorT.Load(add1 + 15 * N)), VectorT.Load(add2 + 15 * N)), VectorT.Load(sub1 + 15 * N)), VectorT.Load(sub2 + 15 * N)), dst + 15 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 16 * N), VectorT.Load(add1 + 16 * N)), VectorT.Load(add2 + 16 * N)), VectorT.Load(sub1 + 16 * N)), VectorT.Load(sub2 + 16 * N)), dst + 16 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 17 * N), VectorT.Load(add1 + 17 * N)), VectorT.Load(add2 + 17 * N)), VectorT.Load(sub1 + 17 * N)), VectorT.Load(sub2 + 17 * N)), dst + 17 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 18 * N), VectorT.Load(add1 + 18 * N)), VectorT.Load(add2 + 18 * N)), VectorT.Load(sub1 + 18 * N)), VectorT.Load(sub2 + 18 * N)), dst + 18 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 19 * N), VectorT.Load(add1 + 19 * N)), VectorT.Load(add2 + 19 * N)), VectorT.Load(sub1 + 19 * N)), VectorT.Load(sub2 + 19 * N)), dst + 19 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 20 * N), VectorT.Load(add1 + 20 * N)), VectorT.Load(add2 + 20 * N)), VectorT.Load(sub1 + 20 * N)), VectorT.Load(sub2 + 20 * N)), dst + 20 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 21 * N), VectorT.Load(add1 + 21 * N)), VectorT.Load(add2 + 21 * N)), VectorT.Load(sub1 + 21 * N)), VectorT.Load(sub2 + 21 * N)), dst + 21 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 22 * N), VectorT.Load(add1 + 22 * N)), VectorT.Load(add2 + 22 * N)), VectorT.Load(sub1 + 22 * N)), VectorT.Load(sub2 + 22 * N)), dst + 22 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 23 * N), VectorT.Load(add1 + 23 * N)), VectorT.Load(add2 + 23 * N)), VectorT.Load(sub1 + 23 * N)), VectorT.Load(sub2 + 23 * N)), dst + 23 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 24 * N), VectorT.Load(add1 + 24 * N)), VectorT.Load(add2 + 24 * N)), VectorT.Load(sub1 + 24 * N)), VectorT.Load(sub2 + 24 * N)), dst + 24 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 25 * N), VectorT.Load(add1 + 25 * N)), VectorT.Load(add2 + 25 * N)), VectorT.Load(sub1 + 25 * N)), VectorT.Load(sub2 + 25 * N)), dst + 25 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 26 * N), VectorT.Load(add1 + 26 * N)), VectorT.Load(add2 + 26 * N)), VectorT.Load(sub1 + 26 * N)), VectorT.Load(sub2 + 26 * N)), dst + 26 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 27 * N), VectorT.Load(add1 + 27 * N)), VectorT.Load(add2 + 27 * N)), VectorT.Load(sub1 + 27 * N)), VectorT.Load(sub2 + 27 * N)), dst + 27 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 28 * N), VectorT.Load(add1 + 28 * N)), VectorT.Load(add2 + 28 * N)), VectorT.Load(sub1 + 28 * N)), VectorT.Load(sub2 + 28 * N)), dst + 28 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 29 * N), VectorT.Load(add1 + 29 * N)), VectorT.Load(add2 + 29 * N)), VectorT.Load(sub1 + 29 * N)), VectorT.Load(sub2 + 29 * N)), dst + 29 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 30 * N), VectorT.Load(add1 + 30 * N)), VectorT.Load(add2 + 30 * N)), VectorT.Load(sub1 + 30 * N)), VectorT.Load(sub2 + 30 * N)), dst + 30 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 31 * N), VectorT.Load(add1 + 31 * N)), VectorT.Load(add2 + 31 * N)), VectorT.Load(sub1 + 31 * N)), VectorT.Load(sub2 + 31 * N)), dst + 31 * N);

            if (StopBefore == AVX512_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 32 * N), VectorT.Load(add1 + 32 * N)), VectorT.Load(add2 + 32 * N)), VectorT.Load(sub1 + 32 * N)), VectorT.Load(sub2 + 32 * N)), dst + 32 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 33 * N), VectorT.Load(add1 + 33 * N)), VectorT.Load(add2 + 33 * N)), VectorT.Load(sub1 + 33 * N)), VectorT.Load(sub2 + 33 * N)), dst + 33 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 34 * N), VectorT.Load(add1 + 34 * N)), VectorT.Load(add2 + 34 * N)), VectorT.Load(sub1 + 34 * N)), VectorT.Load(sub2 + 34 * N)), dst + 34 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 35 * N), VectorT.Load(add1 + 35 * N)), VectorT.Load(add2 + 35 * N)), VectorT.Load(sub1 + 35 * N)), VectorT.Load(sub2 + 35 * N)), dst + 35 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 36 * N), VectorT.Load(add1 + 36 * N)), VectorT.Load(add2 + 36 * N)), VectorT.Load(sub1 + 36 * N)), VectorT.Load(sub2 + 36 * N)), dst + 36 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 37 * N), VectorT.Load(add1 + 37 * N)), VectorT.Load(add2 + 37 * N)), VectorT.Load(sub1 + 37 * N)), VectorT.Load(sub2 + 37 * N)), dst + 37 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 38 * N), VectorT.Load(add1 + 38 * N)), VectorT.Load(add2 + 38 * N)), VectorT.Load(sub1 + 38 * N)), VectorT.Load(sub2 + 38 * N)), dst + 38 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 39 * N), VectorT.Load(add1 + 39 * N)), VectorT.Load(add2 + 39 * N)), VectorT.Load(sub1 + 39 * N)), VectorT.Load(sub2 + 39 * N)), dst + 39 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 40 * N), VectorT.Load(add1 + 40 * N)), VectorT.Load(add2 + 40 * N)), VectorT.Load(sub1 + 40 * N)), VectorT.Load(sub2 + 40 * N)), dst + 40 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 41 * N), VectorT.Load(add1 + 41 * N)), VectorT.Load(add2 + 41 * N)), VectorT.Load(sub1 + 41 * N)), VectorT.Load(sub2 + 41 * N)), dst + 41 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 42 * N), VectorT.Load(add1 + 42 * N)), VectorT.Load(add2 + 42 * N)), VectorT.Load(sub1 + 42 * N)), VectorT.Load(sub2 + 42 * N)), dst + 42 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 43 * N), VectorT.Load(add1 + 43 * N)), VectorT.Load(add2 + 43 * N)), VectorT.Load(sub1 + 43 * N)), VectorT.Load(sub2 + 43 * N)), dst + 43 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 44 * N), VectorT.Load(add1 + 44 * N)), VectorT.Load(add2 + 44 * N)), VectorT.Load(sub1 + 44 * N)), VectorT.Load(sub2 + 44 * N)), dst + 44 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 45 * N), VectorT.Load(add1 + 45 * N)), VectorT.Load(add2 + 45 * N)), VectorT.Load(sub1 + 45 * N)), VectorT.Load(sub2 + 45 * N)), dst + 45 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 46 * N), VectorT.Load(add1 + 46 * N)), VectorT.Load(add2 + 46 * N)), VectorT.Load(sub1 + 46 * N)), VectorT.Load(sub2 + 46 * N)), dst + 46 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 47 * N), VectorT.Load(add1 + 47 * N)), VectorT.Load(add2 + 47 * N)), VectorT.Load(sub1 + 47 * N)), VectorT.Load(sub2 + 47 * N)), dst + 47 * N);

            if (StopBefore == AVX512_1536HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 48 * N), VectorT.Load(add1 + 48 * N)), VectorT.Load(add2 + 48 * N)), VectorT.Load(sub1 + 48 * N)), VectorT.Load(sub2 + 48 * N)), dst + 48 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 49 * N), VectorT.Load(add1 + 49 * N)), VectorT.Load(add2 + 49 * N)), VectorT.Load(sub1 + 49 * N)), VectorT.Load(sub2 + 49 * N)), dst + 49 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 50 * N), VectorT.Load(add1 + 50 * N)), VectorT.Load(add2 + 50 * N)), VectorT.Load(sub1 + 50 * N)), VectorT.Load(sub2 + 50 * N)), dst + 50 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 51 * N), VectorT.Load(add1 + 51 * N)), VectorT.Load(add2 + 51 * N)), VectorT.Load(sub1 + 51 * N)), VectorT.Load(sub2 + 51 * N)), dst + 51 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 52 * N), VectorT.Load(add1 + 52 * N)), VectorT.Load(add2 + 52 * N)), VectorT.Load(sub1 + 52 * N)), VectorT.Load(sub2 + 52 * N)), dst + 52 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 53 * N), VectorT.Load(add1 + 53 * N)), VectorT.Load(add2 + 53 * N)), VectorT.Load(sub1 + 53 * N)), VectorT.Load(sub2 + 53 * N)), dst + 53 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 54 * N), VectorT.Load(add1 + 54 * N)), VectorT.Load(add2 + 54 * N)), VectorT.Load(sub1 + 54 * N)), VectorT.Load(sub2 + 54 * N)), dst + 54 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 55 * N), VectorT.Load(add1 + 55 * N)), VectorT.Load(add2 + 55 * N)), VectorT.Load(sub1 + 55 * N)), VectorT.Load(sub2 + 55 * N)), dst + 55 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 56 * N), VectorT.Load(add1 + 56 * N)), VectorT.Load(add2 + 56 * N)), VectorT.Load(sub1 + 56 * N)), VectorT.Load(sub2 + 56 * N)), dst + 56 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 57 * N), VectorT.Load(add1 + 57 * N)), VectorT.Load(add2 + 57 * N)), VectorT.Load(sub1 + 57 * N)), VectorT.Load(sub2 + 57 * N)), dst + 57 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 58 * N), VectorT.Load(add1 + 58 * N)), VectorT.Load(add2 + 58 * N)), VectorT.Load(sub1 + 58 * N)), VectorT.Load(sub2 + 58 * N)), dst + 58 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 59 * N), VectorT.Load(add1 + 59 * N)), VectorT.Load(add2 + 59 * N)), VectorT.Load(sub1 + 59 * N)), VectorT.Load(sub2 + 59 * N)), dst + 59 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 60 * N), VectorT.Load(add1 + 60 * N)), VectorT.Load(add2 + 60 * N)), VectorT.Load(sub1 + 60 * N)), VectorT.Load(sub2 + 60 * N)), dst + 60 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 61 * N), VectorT.Load(add1 + 61 * N)), VectorT.Load(add2 + 61 * N)), VectorT.Load(sub1 + 61 * N)), VectorT.Load(sub2 + 61 * N)), dst + 61 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 62 * N), VectorT.Load(add1 + 62 * N)), VectorT.Load(add2 + 62 * N)), VectorT.Load(sub1 + 62 * N)), VectorT.Load(sub2 + 62 * N)), dst + 62 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 63 * N), VectorT.Load(add1 + 63 * N)), VectorT.Load(add2 + 63 * N)), VectorT.Load(sub1 + 63 * N)), VectorT.Load(sub2 + 63 * N)), dst + 63 * N);

            if (StopBefore == AVX256_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 64 * N), VectorT.Load(add1 + 64 * N)), VectorT.Load(add2 + 64 * N)), VectorT.Load(sub1 + 64 * N)), VectorT.Load(sub2 + 64 * N)), dst + 64 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 65 * N), VectorT.Load(add1 + 65 * N)), VectorT.Load(add2 + 65 * N)), VectorT.Load(sub1 + 65 * N)), VectorT.Load(sub2 + 65 * N)), dst + 65 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 66 * N), VectorT.Load(add1 + 66 * N)), VectorT.Load(add2 + 66 * N)), VectorT.Load(sub1 + 66 * N)), VectorT.Load(sub2 + 66 * N)), dst + 66 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 67 * N), VectorT.Load(add1 + 67 * N)), VectorT.Load(add2 + 67 * N)), VectorT.Load(sub1 + 67 * N)), VectorT.Load(sub2 + 67 * N)), dst + 67 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 68 * N), VectorT.Load(add1 + 68 * N)), VectorT.Load(add2 + 68 * N)), VectorT.Load(sub1 + 68 * N)), VectorT.Load(sub2 + 68 * N)), dst + 68 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 69 * N), VectorT.Load(add1 + 69 * N)), VectorT.Load(add2 + 69 * N)), VectorT.Load(sub1 + 69 * N)), VectorT.Load(sub2 + 69 * N)), dst + 69 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 70 * N), VectorT.Load(add1 + 70 * N)), VectorT.Load(add2 + 70 * N)), VectorT.Load(sub1 + 70 * N)), VectorT.Load(sub2 + 70 * N)), dst + 70 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 71 * N), VectorT.Load(add1 + 71 * N)), VectorT.Load(add2 + 71 * N)), VectorT.Load(sub1 + 71 * N)), VectorT.Load(sub2 + 71 * N)), dst + 71 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 72 * N), VectorT.Load(add1 + 72 * N)), VectorT.Load(add2 + 72 * N)), VectorT.Load(sub1 + 72 * N)), VectorT.Load(sub2 + 72 * N)), dst + 72 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 73 * N), VectorT.Load(add1 + 73 * N)), VectorT.Load(add2 + 73 * N)), VectorT.Load(sub1 + 73 * N)), VectorT.Load(sub2 + 73 * N)), dst + 73 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 74 * N), VectorT.Load(add1 + 74 * N)), VectorT.Load(add2 + 74 * N)), VectorT.Load(sub1 + 74 * N)), VectorT.Load(sub2 + 74 * N)), dst + 74 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 75 * N), VectorT.Load(add1 + 75 * N)), VectorT.Load(add2 + 75 * N)), VectorT.Load(sub1 + 75 * N)), VectorT.Load(sub2 + 75 * N)), dst + 75 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 76 * N), VectorT.Load(add1 + 76 * N)), VectorT.Load(add2 + 76 * N)), VectorT.Load(sub1 + 76 * N)), VectorT.Load(sub2 + 76 * N)), dst + 76 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 77 * N), VectorT.Load(add1 + 77 * N)), VectorT.Load(add2 + 77 * N)), VectorT.Load(sub1 + 77 * N)), VectorT.Load(sub2 + 77 * N)), dst + 77 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 78 * N), VectorT.Load(add1 + 78 * N)), VectorT.Load(add2 + 78 * N)), VectorT.Load(sub1 + 78 * N)), VectorT.Load(sub2 + 78 * N)), dst + 78 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 79 * N), VectorT.Load(add1 + 79 * N)), VectorT.Load(add2 + 79 * N)), VectorT.Load(sub1 + 79 * N)), VectorT.Load(sub2 + 79 * N)), dst + 79 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 80 * N), VectorT.Load(add1 + 80 * N)), VectorT.Load(add2 + 80 * N)), VectorT.Load(sub1 + 80 * N)), VectorT.Load(sub2 + 80 * N)), dst + 80 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 81 * N), VectorT.Load(add1 + 81 * N)), VectorT.Load(add2 + 81 * N)), VectorT.Load(sub1 + 81 * N)), VectorT.Load(sub2 + 81 * N)), dst + 81 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 82 * N), VectorT.Load(add1 + 82 * N)), VectorT.Load(add2 + 82 * N)), VectorT.Load(sub1 + 82 * N)), VectorT.Load(sub2 + 82 * N)), dst + 82 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 83 * N), VectorT.Load(add1 + 83 * N)), VectorT.Load(add2 + 83 * N)), VectorT.Load(sub1 + 83 * N)), VectorT.Load(sub2 + 83 * N)), dst + 83 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 84 * N), VectorT.Load(add1 + 84 * N)), VectorT.Load(add2 + 84 * N)), VectorT.Load(sub1 + 84 * N)), VectorT.Load(sub2 + 84 * N)), dst + 84 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 85 * N), VectorT.Load(add1 + 85 * N)), VectorT.Load(add2 + 85 * N)), VectorT.Load(sub1 + 85 * N)), VectorT.Load(sub2 + 85 * N)), dst + 85 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 86 * N), VectorT.Load(add1 + 86 * N)), VectorT.Load(add2 + 86 * N)), VectorT.Load(sub1 + 86 * N)), VectorT.Load(sub2 + 86 * N)), dst + 86 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 87 * N), VectorT.Load(add1 + 87 * N)), VectorT.Load(add2 + 87 * N)), VectorT.Load(sub1 + 87 * N)), VectorT.Load(sub2 + 87 * N)), dst + 87 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 88 * N), VectorT.Load(add1 + 88 * N)), VectorT.Load(add2 + 88 * N)), VectorT.Load(sub1 + 88 * N)), VectorT.Load(sub2 + 88 * N)), dst + 88 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 89 * N), VectorT.Load(add1 + 89 * N)), VectorT.Load(add2 + 89 * N)), VectorT.Load(sub1 + 89 * N)), VectorT.Load(sub2 + 89 * N)), dst + 89 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 90 * N), VectorT.Load(add1 + 90 * N)), VectorT.Load(add2 + 90 * N)), VectorT.Load(sub1 + 90 * N)), VectorT.Load(sub2 + 90 * N)), dst + 90 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 91 * N), VectorT.Load(add1 + 91 * N)), VectorT.Load(add2 + 91 * N)), VectorT.Load(sub1 + 91 * N)), VectorT.Load(sub2 + 91 * N)), dst + 91 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 92 * N), VectorT.Load(add1 + 92 * N)), VectorT.Load(add2 + 92 * N)), VectorT.Load(sub1 + 92 * N)), VectorT.Load(sub2 + 92 * N)), dst + 92 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 93 * N), VectorT.Load(add1 + 93 * N)), VectorT.Load(add2 + 93 * N)), VectorT.Load(sub1 + 93 * N)), VectorT.Load(sub2 + 93 * N)), dst + 93 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 94 * N), VectorT.Load(add1 + 94 * N)), VectorT.Load(add2 + 94 * N)), VectorT.Load(sub1 + 94 * N)), VectorT.Load(sub2 + 94 * N)), dst + 94 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Subtract(VectorT.Add(VectorT.Add(VectorT.Load(src + 95 * N), VectorT.Load(add1 + 95 * N)), VectorT.Load(add2 + 95 * N)), VectorT.Load(sub1 + 95 * N)), VectorT.Load(sub2 + 95 * N)), dst + 95 * N);

        }


        public static void UnrollAdd(short* src, short* dst, short* add1)
        {
            VectorT.Store(VectorT.Add(VectorT.Load(src + 0 * N), VectorT.Load(add1 + 0 * N)), dst + 0 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 1 * N), VectorT.Load(add1 + 1 * N)), dst + 1 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 2 * N), VectorT.Load(add1 + 2 * N)), dst + 2 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 3 * N), VectorT.Load(add1 + 3 * N)), dst + 3 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 4 * N), VectorT.Load(add1 + 4 * N)), dst + 4 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 5 * N), VectorT.Load(add1 + 5 * N)), dst + 5 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 6 * N), VectorT.Load(add1 + 6 * N)), dst + 6 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 7 * N), VectorT.Load(add1 + 7 * N)), dst + 7 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 8 * N), VectorT.Load(add1 + 8 * N)), dst + 8 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 9 * N), VectorT.Load(add1 + 9 * N)), dst + 9 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 10 * N), VectorT.Load(add1 + 10 * N)), dst + 10 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 11 * N), VectorT.Load(add1 + 11 * N)), dst + 11 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 12 * N), VectorT.Load(add1 + 12 * N)), dst + 12 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 13 * N), VectorT.Load(add1 + 13 * N)), dst + 13 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 14 * N), VectorT.Load(add1 + 14 * N)), dst + 14 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 15 * N), VectorT.Load(add1 + 15 * N)), dst + 15 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 16 * N), VectorT.Load(add1 + 16 * N)), dst + 16 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 17 * N), VectorT.Load(add1 + 17 * N)), dst + 17 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 18 * N), VectorT.Load(add1 + 18 * N)), dst + 18 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 19 * N), VectorT.Load(add1 + 19 * N)), dst + 19 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 20 * N), VectorT.Load(add1 + 20 * N)), dst + 20 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 21 * N), VectorT.Load(add1 + 21 * N)), dst + 21 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 22 * N), VectorT.Load(add1 + 22 * N)), dst + 22 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 23 * N), VectorT.Load(add1 + 23 * N)), dst + 23 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 24 * N), VectorT.Load(add1 + 24 * N)), dst + 24 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 25 * N), VectorT.Load(add1 + 25 * N)), dst + 25 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 26 * N), VectorT.Load(add1 + 26 * N)), dst + 26 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 27 * N), VectorT.Load(add1 + 27 * N)), dst + 27 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 28 * N), VectorT.Load(add1 + 28 * N)), dst + 28 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 29 * N), VectorT.Load(add1 + 29 * N)), dst + 29 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 30 * N), VectorT.Load(add1 + 30 * N)), dst + 30 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 31 * N), VectorT.Load(add1 + 31 * N)), dst + 31 * N);

            if (StopBefore == AVX512_1024HL)
                return;

            VectorT.Store(VectorT.Add(VectorT.Load(src + 32 * N), VectorT.Load(add1 + 32 * N)), dst + 32 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 33 * N), VectorT.Load(add1 + 33 * N)), dst + 33 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 34 * N), VectorT.Load(add1 + 34 * N)), dst + 34 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 35 * N), VectorT.Load(add1 + 35 * N)), dst + 35 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 36 * N), VectorT.Load(add1 + 36 * N)), dst + 36 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 37 * N), VectorT.Load(add1 + 37 * N)), dst + 37 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 38 * N), VectorT.Load(add1 + 38 * N)), dst + 38 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 39 * N), VectorT.Load(add1 + 39 * N)), dst + 39 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 40 * N), VectorT.Load(add1 + 40 * N)), dst + 40 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 41 * N), VectorT.Load(add1 + 41 * N)), dst + 41 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 42 * N), VectorT.Load(add1 + 42 * N)), dst + 42 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 43 * N), VectorT.Load(add1 + 43 * N)), dst + 43 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 44 * N), VectorT.Load(add1 + 44 * N)), dst + 44 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 45 * N), VectorT.Load(add1 + 45 * N)), dst + 45 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 46 * N), VectorT.Load(add1 + 46 * N)), dst + 46 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 47 * N), VectorT.Load(add1 + 47 * N)), dst + 47 * N);

            if (StopBefore == AVX512_1536HL)
                return;

            VectorT.Store(VectorT.Add(VectorT.Load(src + 48 * N), VectorT.Load(add1 + 48 * N)), dst + 48 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 49 * N), VectorT.Load(add1 + 49 * N)), dst + 49 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 50 * N), VectorT.Load(add1 + 50 * N)), dst + 50 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 51 * N), VectorT.Load(add1 + 51 * N)), dst + 51 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 52 * N), VectorT.Load(add1 + 52 * N)), dst + 52 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 53 * N), VectorT.Load(add1 + 53 * N)), dst + 53 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 54 * N), VectorT.Load(add1 + 54 * N)), dst + 54 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 55 * N), VectorT.Load(add1 + 55 * N)), dst + 55 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 56 * N), VectorT.Load(add1 + 56 * N)), dst + 56 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 57 * N), VectorT.Load(add1 + 57 * N)), dst + 57 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 58 * N), VectorT.Load(add1 + 58 * N)), dst + 58 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 59 * N), VectorT.Load(add1 + 59 * N)), dst + 59 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 60 * N), VectorT.Load(add1 + 60 * N)), dst + 60 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 61 * N), VectorT.Load(add1 + 61 * N)), dst + 61 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 62 * N), VectorT.Load(add1 + 62 * N)), dst + 62 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 63 * N), VectorT.Load(add1 + 63 * N)), dst + 63 * N);

            if (StopBefore == AVX256_1024HL)
                return;

            VectorT.Store(VectorT.Add(VectorT.Load(src + 64 * N), VectorT.Load(add1 + 64 * N)), dst + 64 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 65 * N), VectorT.Load(add1 + 65 * N)), dst + 65 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 66 * N), VectorT.Load(add1 + 66 * N)), dst + 66 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 67 * N), VectorT.Load(add1 + 67 * N)), dst + 67 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 68 * N), VectorT.Load(add1 + 68 * N)), dst + 68 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 69 * N), VectorT.Load(add1 + 69 * N)), dst + 69 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 70 * N), VectorT.Load(add1 + 70 * N)), dst + 70 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 71 * N), VectorT.Load(add1 + 71 * N)), dst + 71 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 72 * N), VectorT.Load(add1 + 72 * N)), dst + 72 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 73 * N), VectorT.Load(add1 + 73 * N)), dst + 73 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 74 * N), VectorT.Load(add1 + 74 * N)), dst + 74 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 75 * N), VectorT.Load(add1 + 75 * N)), dst + 75 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 76 * N), VectorT.Load(add1 + 76 * N)), dst + 76 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 77 * N), VectorT.Load(add1 + 77 * N)), dst + 77 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 78 * N), VectorT.Load(add1 + 78 * N)), dst + 78 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 79 * N), VectorT.Load(add1 + 79 * N)), dst + 79 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 80 * N), VectorT.Load(add1 + 80 * N)), dst + 80 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 81 * N), VectorT.Load(add1 + 81 * N)), dst + 81 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 82 * N), VectorT.Load(add1 + 82 * N)), dst + 82 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 83 * N), VectorT.Load(add1 + 83 * N)), dst + 83 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 84 * N), VectorT.Load(add1 + 84 * N)), dst + 84 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 85 * N), VectorT.Load(add1 + 85 * N)), dst + 85 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 86 * N), VectorT.Load(add1 + 86 * N)), dst + 86 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 87 * N), VectorT.Load(add1 + 87 * N)), dst + 87 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 88 * N), VectorT.Load(add1 + 88 * N)), dst + 88 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 89 * N), VectorT.Load(add1 + 89 * N)), dst + 89 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 90 * N), VectorT.Load(add1 + 90 * N)), dst + 90 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 91 * N), VectorT.Load(add1 + 91 * N)), dst + 91 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 92 * N), VectorT.Load(add1 + 92 * N)), dst + 92 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 93 * N), VectorT.Load(add1 + 93 * N)), dst + 93 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 94 * N), VectorT.Load(add1 + 94 * N)), dst + 94 * N);
            VectorT.Store(VectorT.Add(VectorT.Load(src + 95 * N), VectorT.Load(add1 + 95 * N)), dst + 95 * N);
        }

        public static void UnrollSubtract(short* src, short* dst, short* sub1)
        {
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 0 * N), VectorT.Load(sub1 + 0 * N)), dst + 0 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 1 * N), VectorT.Load(sub1 + 1 * N)), dst + 1 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 2 * N), VectorT.Load(sub1 + 2 * N)), dst + 2 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 3 * N), VectorT.Load(sub1 + 3 * N)), dst + 3 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 4 * N), VectorT.Load(sub1 + 4 * N)), dst + 4 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 5 * N), VectorT.Load(sub1 + 5 * N)), dst + 5 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 6 * N), VectorT.Load(sub1 + 6 * N)), dst + 6 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 7 * N), VectorT.Load(sub1 + 7 * N)), dst + 7 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 8 * N), VectorT.Load(sub1 + 8 * N)), dst + 8 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 9 * N), VectorT.Load(sub1 + 9 * N)), dst + 9 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 10 * N), VectorT.Load(sub1 + 10 * N)), dst + 10 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 11 * N), VectorT.Load(sub1 + 11 * N)), dst + 11 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 12 * N), VectorT.Load(sub1 + 12 * N)), dst + 12 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 13 * N), VectorT.Load(sub1 + 13 * N)), dst + 13 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 14 * N), VectorT.Load(sub1 + 14 * N)), dst + 14 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 15 * N), VectorT.Load(sub1 + 15 * N)), dst + 15 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 16 * N), VectorT.Load(sub1 + 16 * N)), dst + 16 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 17 * N), VectorT.Load(sub1 + 17 * N)), dst + 17 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 18 * N), VectorT.Load(sub1 + 18 * N)), dst + 18 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 19 * N), VectorT.Load(sub1 + 19 * N)), dst + 19 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 20 * N), VectorT.Load(sub1 + 20 * N)), dst + 20 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 21 * N), VectorT.Load(sub1 + 21 * N)), dst + 21 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 22 * N), VectorT.Load(sub1 + 22 * N)), dst + 22 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 23 * N), VectorT.Load(sub1 + 23 * N)), dst + 23 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 24 * N), VectorT.Load(sub1 + 24 * N)), dst + 24 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 25 * N), VectorT.Load(sub1 + 25 * N)), dst + 25 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 26 * N), VectorT.Load(sub1 + 26 * N)), dst + 26 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 27 * N), VectorT.Load(sub1 + 27 * N)), dst + 27 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 28 * N), VectorT.Load(sub1 + 28 * N)), dst + 28 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 29 * N), VectorT.Load(sub1 + 29 * N)), dst + 29 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 30 * N), VectorT.Load(sub1 + 30 * N)), dst + 30 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 31 * N), VectorT.Load(sub1 + 31 * N)), dst + 31 * N);

            if (StopBefore == AVX512_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 32 * N), VectorT.Load(sub1 + 32 * N)), dst + 32 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 33 * N), VectorT.Load(sub1 + 33 * N)), dst + 33 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 34 * N), VectorT.Load(sub1 + 34 * N)), dst + 34 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 35 * N), VectorT.Load(sub1 + 35 * N)), dst + 35 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 36 * N), VectorT.Load(sub1 + 36 * N)), dst + 36 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 37 * N), VectorT.Load(sub1 + 37 * N)), dst + 37 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 38 * N), VectorT.Load(sub1 + 38 * N)), dst + 38 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 39 * N), VectorT.Load(sub1 + 39 * N)), dst + 39 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 40 * N), VectorT.Load(sub1 + 40 * N)), dst + 40 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 41 * N), VectorT.Load(sub1 + 41 * N)), dst + 41 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 42 * N), VectorT.Load(sub1 + 42 * N)), dst + 42 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 43 * N), VectorT.Load(sub1 + 43 * N)), dst + 43 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 44 * N), VectorT.Load(sub1 + 44 * N)), dst + 44 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 45 * N), VectorT.Load(sub1 + 45 * N)), dst + 45 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 46 * N), VectorT.Load(sub1 + 46 * N)), dst + 46 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 47 * N), VectorT.Load(sub1 + 47 * N)), dst + 47 * N);

            if (StopBefore == AVX512_1536HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 48 * N), VectorT.Load(sub1 + 48 * N)), dst + 48 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 49 * N), VectorT.Load(sub1 + 49 * N)), dst + 49 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 50 * N), VectorT.Load(sub1 + 50 * N)), dst + 50 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 51 * N), VectorT.Load(sub1 + 51 * N)), dst + 51 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 52 * N), VectorT.Load(sub1 + 52 * N)), dst + 52 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 53 * N), VectorT.Load(sub1 + 53 * N)), dst + 53 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 54 * N), VectorT.Load(sub1 + 54 * N)), dst + 54 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 55 * N), VectorT.Load(sub1 + 55 * N)), dst + 55 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 56 * N), VectorT.Load(sub1 + 56 * N)), dst + 56 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 57 * N), VectorT.Load(sub1 + 57 * N)), dst + 57 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 58 * N), VectorT.Load(sub1 + 58 * N)), dst + 58 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 59 * N), VectorT.Load(sub1 + 59 * N)), dst + 59 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 60 * N), VectorT.Load(sub1 + 60 * N)), dst + 60 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 61 * N), VectorT.Load(sub1 + 61 * N)), dst + 61 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 62 * N), VectorT.Load(sub1 + 62 * N)), dst + 62 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 63 * N), VectorT.Load(sub1 + 63 * N)), dst + 63 * N);

            if (StopBefore == AVX256_1024HL)
                return;

            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 64 * N), VectorT.Load(sub1 + 64 * N)), dst + 64 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 65 * N), VectorT.Load(sub1 + 65 * N)), dst + 65 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 66 * N), VectorT.Load(sub1 + 66 * N)), dst + 66 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 67 * N), VectorT.Load(sub1 + 67 * N)), dst + 67 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 68 * N), VectorT.Load(sub1 + 68 * N)), dst + 68 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 69 * N), VectorT.Load(sub1 + 69 * N)), dst + 69 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 70 * N), VectorT.Load(sub1 + 70 * N)), dst + 70 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 71 * N), VectorT.Load(sub1 + 71 * N)), dst + 71 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 72 * N), VectorT.Load(sub1 + 72 * N)), dst + 72 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 73 * N), VectorT.Load(sub1 + 73 * N)), dst + 73 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 74 * N), VectorT.Load(sub1 + 74 * N)), dst + 74 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 75 * N), VectorT.Load(sub1 + 75 * N)), dst + 75 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 76 * N), VectorT.Load(sub1 + 76 * N)), dst + 76 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 77 * N), VectorT.Load(sub1 + 77 * N)), dst + 77 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 78 * N), VectorT.Load(sub1 + 78 * N)), dst + 78 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 79 * N), VectorT.Load(sub1 + 79 * N)), dst + 79 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 80 * N), VectorT.Load(sub1 + 80 * N)), dst + 80 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 81 * N), VectorT.Load(sub1 + 81 * N)), dst + 81 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 82 * N), VectorT.Load(sub1 + 82 * N)), dst + 82 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 83 * N), VectorT.Load(sub1 + 83 * N)), dst + 83 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 84 * N), VectorT.Load(sub1 + 84 * N)), dst + 84 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 85 * N), VectorT.Load(sub1 + 85 * N)), dst + 85 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 86 * N), VectorT.Load(sub1 + 86 * N)), dst + 86 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 87 * N), VectorT.Load(sub1 + 87 * N)), dst + 87 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 88 * N), VectorT.Load(sub1 + 88 * N)), dst + 88 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 89 * N), VectorT.Load(sub1 + 89 * N)), dst + 89 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 90 * N), VectorT.Load(sub1 + 90 * N)), dst + 90 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 91 * N), VectorT.Load(sub1 + 91 * N)), dst + 91 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 92 * N), VectorT.Load(sub1 + 92 * N)), dst + 92 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 93 * N), VectorT.Load(sub1 + 93 * N)), dst + 93 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 94 * N), VectorT.Load(sub1 + 94 * N)), dst + 94 * N);
            VectorT.Store(VectorT.Subtract(VectorT.Load(src + 95 * N), VectorT.Load(sub1 + 95 * N)), dst + 95 * N);
        }

    }
}
