#define INLINE
//#define NOINLINING

namespace LTChess.Data
{
    public static class RunOptions
    {

        //  PreserveSig shouldn't have any meaningful impact on performance... I hope.

#if (INLINE && !NOINLINING)
        public const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;
#else
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
