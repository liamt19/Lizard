
#if AVX512
using SIMDClass = System.Runtime.Intrinsics.X86.Avx512BW;
using VectorT = System.Runtime.Intrinsics.Vector512;
using VShort = System.Runtime.Intrinsics.Vector512<short>;
using VInt = System.Runtime.Intrinsics.Vector512<int>;
#else
using SIMDClass = System.Runtime.Intrinsics.X86.Avx2;
using VectorT = System.Runtime.Intrinsics.Vector256;
using VInt = System.Runtime.Intrinsics.Vector256<int>;
using VShort = System.Runtime.Intrinsics.Vector256<short>;
#endif

#pragma warning disable CS0162 // Unreachable code detected

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {

#if AVX512
        private const int N = 32;
#else
        private const int N = 16;
#endif

        private const int StopBefore = L1_SIZE / N;

        private const int AVX512_1024HL = 1024 / 32;
        private const int AVX512_1536HL = 1536 / 32;

        private const int AVX256_1024HL = 1024 / 16;
        private const int AVX256_1536HL = 1536 / 16;

        public static int GetEvaluationUnrolled512(Position pos)
        {
            return 1;
        }

    }
}
