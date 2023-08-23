

#define INLINE
//#define NOINLINING

#define OPTIMIZE

namespace LTChess.Logic.Data
{
    public static class RunOptions
    {

        //  PreserveSig shouldn't have any meaningful impact on performance... I hope.

#if (INLINE && !NOINLINING)
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#else
        public const MethodImplOptions Inline = MethodImplOptions.PreserveSig;
#endif

#if (OPTIMIZE)
        public const MethodImplOptions Optimize = MethodImplOptions.AggressiveOptimization;
#else
        public const MethodImplOptions Optimize = MethodImplOptions.PreserveSig;
#endif


    }
}
