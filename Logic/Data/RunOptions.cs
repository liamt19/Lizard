
#define OPTIMIZE
#define INLINE

using System.Runtime.CompilerServices;

namespace LTChess.Data
{
    public static class RunOptions
    {

#if (OPTIMIZE && INLINE)
        public const MethodImplOptions Optimize = MethodImplOptions.AggressiveOptimization;
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#elif (OPTIMIZE)
        public const MethodImplOptions Optimize = MethodImplOptions.AggressiveOptimization;
        public const MethodImplOptions Inline = MethodImplOptions.PreserveSig;
#elif (INLINE)
        public const MethodImplOptions Optimize = MethodImplOptions.PreserveSig;
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#else
        public const MethodImplOptions Optimize = MethodImplOptions.PreserveSig;
        public const MethodImplOptions Inline = MethodImplOptions.PreserveSig;
#endif


#if IS64BIT
        public const bool Is64Bit = true;
#else
        public const bool Is64Bit = false;
#endif

#if PEXT
        public const bool Pext = true;
#else
        public const bool Pext = false;
#endif

#if BMI
        public const bool BMI = true;
#else
        public const bool BMI = false;
#endif

    }
}
