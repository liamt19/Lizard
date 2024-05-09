

#define USE_AGGRESSIVE_INLINING

//#define USE_SKIP_INIT

//#define SKIP_INIT_IN_DEBUG


#if USE_SKIP_INIT

//  Using SkipInit will cause methods to be generated without a ".locals init" flag,
//  meaning that local variables aren't automatically initialized to 0 when they are created.

//  This does seem to make things run 2-3% faster, but it can be dangerous
//  since stackalloc'd arrays will have junk data in them in the indices that haven't been written to.

//  So long as the code is written properly (which is the kicker...), there won't be any runtime differences
//  besides the slight performance improvement.


//  I prefer to have SkipInit off while debugging since the values that you mouse over can have confusing values
#if RELEASE || DEV || SKIP_INIT_IN_DEBUG
[module: System.Runtime.CompilerServices.SkipLocalsInit]
#endif

#endif


namespace Lizard.Logic.Data
{
    public static class RunOptions
    {

        //  PreserveSig shouldn't have any meaningful impact on performance... I hope.

#if USE_AGGRESSIVE_INLINING
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#else
        public const MethodImplOptions Inline = MethodImplOptions.PreserveSig;
#endif

        public const MethodImplOptions NoInline = MethodImplOptions.NoInlining;

#if PEXT
        public const bool HasPext = true;
#else
        public const bool HasPext = false;
#endif

#if USE_SKIP_INIT && (RELEASE || DEV || SKIP_INIT_IN_DEBUG)
        public const bool HasSkipInit = true;
#else
        public const bool HasSkipInit = false;
#endif

    }
}
