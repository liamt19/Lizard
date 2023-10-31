

#define INLINE
//#undef INLINE




#define ENABLE_ASSERTIONS
#undef ENABLE_ASSERTIONS




#define USE_SKIP_INIT
//#undef USE_SKIP_INIT

#if (USE_SKIP_INIT)

//  Using this will cause methods to be generated without a ".locals init" flag,
//  meaning that local variables aren't initialized to 0 when they are created.

//  Using SkipInit does seem to make things run 2-3% faster, but this can actually be dangerous
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

        public const MethodImplOptions NoInline = MethodImplOptions.NoInlining;

#if (PEXT)
        public const bool HasPext = true;
#else
        public const bool HasPext = false;
#endif

        /// <summary>
        /// Whether or not to enable various sanity checks throughout the program. 
        /// These are explicit calls to Debug.Assert in Debug mode, and a reimplementation of it while in Release mode.
        /// <para></para>
        /// This should be off if you aren't actively looking for a bug, because it makes the program run about 3x slower.
        /// </summary>
#if ENABLE_ASSERTIONS
        public const bool EnableAssertions = true;
#else
        public const bool EnableAssertions = false;
#endif

    }
}
