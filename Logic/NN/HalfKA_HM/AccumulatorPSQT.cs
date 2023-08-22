using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Logic.NN.HalfKA_HM.NNCommon;

namespace LTChess.Logic.NN.HalfKA_HM
{
    /// <summary>
    /// Keeps track of the weights of the active features for both sides.
    /// </summary>
    public struct AccumulatorPSQT
    {
        public short[] White;
        public short[] Black;

        public int[] PSQTWhite;
        public int[] PSQTBlack;

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh;

        public AccumulatorPSQT(int size = HalfKP.HalfKP.TransformedFeatureDimensions)
        {
            White = new short[size];
            Black = new short[size];

            PSQTWhite = new int[PSQTBuckets];
            PSQTBlack = new int[PSQTBuckets];
        }

        public short[] this[int perspective]
        {
            get
            {
                return (perspective == Color.White) ? White : Black;
            }
        }

        [MethodImpl(Inline)]
        public int[] PSQ(int perspective)
        {
            return (perspective == Color.White) ? PSQTWhite : PSQTBlack;
        }

        [MethodImpl(Inline)]
        public void CopyTo(AccumulatorPSQT target)
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

            ref int c = ref MemoryMarshal.GetArrayDataReference(PSQTWhite);
            ref int d = ref MemoryMarshal.GetArrayDataReference(PSQTBlack);
            ref int targetC = ref MemoryMarshal.GetArrayDataReference(target.PSQTWhite);
            ref int targetD = ref MemoryMarshal.GetArrayDataReference(target.PSQTBlack);

            size = PSQTWhite.Length * Unsafe.SizeOf<int>();

            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<int, byte>(ref targetC),
                ref Unsafe.As<int, byte>(ref c),
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<int, byte>(ref targetD),
                ref Unsafe.As<int, byte>(ref d),
                (uint)size
            );
        }


        [MethodImpl(Inline)]
        public void Zero()
        {
            Array.Clear(White);
            Array.Clear(Black);
            Array.Clear(PSQTWhite);
            Array.Clear(PSQTBlack);
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

