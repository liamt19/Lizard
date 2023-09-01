using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.NN.Simple768;

namespace LTChess.Logic.NN
{

    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public struct Accumulator
    {
        public const int ByteSize = NNUE768.HIDDEN;

        public Vector256<short>[] White = new Vector256<short>[ByteSize / VSize.Short];
        public Vector256<short>[] Black = new Vector256<short>[ByteSize / VSize.Short];

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh;

        public Accumulator()
        {

        }

        public Vector256<short>[] this[int perspective]
        {
            get 
            {
                return (perspective == Color.White) ? White : Black;
            }
        }

        [MethodImpl(Inline)]
        public void CopyTo(in Accumulator target)
        {
            ref var a = ref MemoryMarshal.GetArrayDataReference(White);
            ref var b = ref MemoryMarshal.GetArrayDataReference(Black);
            ref var targetA = ref MemoryMarshal.GetArrayDataReference(target.White);
            ref var targetB = ref MemoryMarshal.GetArrayDataReference(target.Black);

            int size = ByteSize * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<Vector256<short>, byte>(ref targetA),
                ref Unsafe.As<Vector256<short>, byte>(ref a),
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<Vector256<short>, byte>(ref targetB),
                ref Unsafe.As<Vector256<short>, byte>(ref b),
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
