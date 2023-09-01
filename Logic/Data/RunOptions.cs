

#define INLINE
//#undef INLINE

#define OPTIMIZE


#define USE_SKIP_INIT
#undef USE_SKIP_INIT

#if (USE_SKIP_INIT)

//  Using this will cause methods to be generated without a ".locals init" flag,
//  meaning that local variables aren't initialized to 0 when they are created.

//  This does seem to make things run 2-3% faster, but this is actually dangerous
//  since stackalloc'd arrays will have junk data in them in the indices that haven't been written to.
[module: System.Runtime.CompilerServices.SkipLocalsInit]
#endif

namespace LTChess.Logic.Data
{
    public static class RunOptions
    {

        //  PreserveSig shouldn't have any meaningful impact on performance... I hope.

#if (INLINE)
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#else
        public const MethodImplOptions Inline = MethodImplOptions.PreserveSig;
#endif

#if (OPTIMIZE)
        public const MethodImplOptions Optimize = MethodImplOptions.AggressiveOptimization;
#else
        public const MethodImplOptions Optimize = MethodImplOptions.PreserveSig;
#endif

#if (PEXT)
        public const bool HasPext = true;
#else
        public const bool HasPext = false;
#endif

#if (USE_SKIP_INIT)
        public const bool SkipInitEnabled = true;
#else
        public const bool SkipInitEnabled = false;
#endif

    }
}
