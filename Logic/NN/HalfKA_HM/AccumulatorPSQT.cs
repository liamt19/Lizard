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
        //  Intellisense incorrectly marks "var accumulation = accumulator[perspectives[p]]" in FeatureTransformer.TransformFeatures
        //  as an error when this uses a primary constructor with "size" as a parameter.
        //  https://github.com/dotnet/roslyn/issues/69663

        public const int size = HalfKA_HM.TransformedFeatureDimensions;
        public short[] White = new short[size];
        public short[] Black = new short[size];

        public int[] PSQTWhite = new int[PSQTBuckets];
        public int[] PSQTBlack = new int[PSQTBuckets];

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh = true;

        public AccumulatorPSQT() { }

        public short[] this[int perspective]
        {
            get
            {
                return (perspective == Color.White) ? White : Black;
            }
        }
    

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] PSQ(int perspective)
        {
            return (perspective == Color.White) ? PSQTWhite : PSQTBlack;
        }

        [MethodImpl(Inline)]
        public void CopyTo(ref AccumulatorPSQT target)
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

