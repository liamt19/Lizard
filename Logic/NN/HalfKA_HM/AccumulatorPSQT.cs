using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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

        public const int ByteSize = HalfKA_HM.TransformedFeatureDimensions;
        public Vector256<short>[] White = new Vector256<short>[ByteSize / VSize.Short];
        public Vector256<short>[] Black = new Vector256<short>[ByteSize / VSize.Short];

        public Vector256<int>[] PSQTWhite = new Vector256<int>[PSQTBuckets / VSize.Int];
        public Vector256<int>[] PSQTBlack = new Vector256<int>[PSQTBuckets / VSize.Int];

        /// <summary>
        /// Set to true when a king move is made, in which case every feature in that side's accumulator
        /// needs to be recalculated.
        /// </summary>
        public bool NeedsRefresh = true;

        public AccumulatorPSQT() { }


        public Vector256<short>[] this[int perspective]
        {
            get
            {
                return (perspective == Color.White) ? White : Black;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector256<int>[] PSQ(int perspective)
        {
            return (perspective == Color.White) ? PSQTWhite : PSQTBlack;
        }

        [MethodImpl(Inline)]
        public void CopyTo(ref AccumulatorPSQT target)
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

            ref var c = ref MemoryMarshal.GetArrayDataReference(PSQTWhite);
            ref var d = ref MemoryMarshal.GetArrayDataReference(PSQTBlack);
            ref var targetC = ref MemoryMarshal.GetArrayDataReference(target.PSQTWhite);
            ref var targetD = ref MemoryMarshal.GetArrayDataReference(target.PSQTBlack);

            size = PSQTBuckets * sizeof(int);

            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<Vector256<int>, byte>(ref targetC),
                ref Unsafe.As<Vector256<int>, byte>(ref c),
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                ref Unsafe.As<Vector256<int>, byte>(ref targetD),
                ref Unsafe.As<Vector256<int>, byte>(ref d),
                (uint)size
            );
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

