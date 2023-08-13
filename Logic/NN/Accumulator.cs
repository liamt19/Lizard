using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.NN
{

    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public struct Accumulator
    {
        public short[] White;
        public short[] Black;

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh;

        public Accumulator(int size = HalfKP.HalfKP.TransformedFeatureDimensions)
        {
            White = new short[size];
            Black = new short[size];
        }

        public short[] this[int perspective]
        {
            get 
            { 
                return (perspective == Color.White) ? White : Black;
            }
        }

        [MethodImpl(Inline)]
        public void CopyTo(Accumulator target)
        {
            ref short a = ref MemoryMarshal.GetArrayDataReference(White);
            ref short b = ref MemoryMarshal.GetArrayDataReference(Black);
            ref short targetA = ref MemoryMarshal.GetArrayDataReference(target.White);
            ref short targetB = ref MemoryMarshal.GetArrayDataReference(target.Black);

            int size = White.Length * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<short, byte>(ref targetA),
                ref Unsafe.As<short, byte>(ref a),
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<short, byte>(ref targetB),
                ref Unsafe.As<short, byte>(ref b),
                (uint)size
            );
        }

        [MethodImpl(Inline)]
        public void PreLoadBias(short[] bias)
        {
            ref short white = ref MemoryMarshal.GetArrayDataReference(White);
            ref short black = ref MemoryMarshal.GetArrayDataReference(Black);
            ref short biasRef = ref MemoryMarshal.GetArrayDataReference(bias);

            int size = White.Length * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<short, byte>(ref white),
                ref Unsafe.As<short, byte>(ref biasRef),
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<short, byte>(ref black),
                ref Unsafe.As<short, byte>(ref biasRef),
                (uint)size
            );
        }

        [MethodImpl(Inline)]
        public void Zero()
        {
            Array.Clear(White);
            Array.Clear(Black);
        }

        public void DebugPrint()
        {
            Console.Write("White:");
            for (int i = 0; i < White.Length; i++)
            {
                Console.Write(" " + White[i]);
            }
            Console.WriteLine();

            Console.Write("Black:");
            for (int i = 0; i < Black.Length; i++)
            {
                Console.Write(" " + Black[i]);
            }
            Console.WriteLine();
        }
    }
}
