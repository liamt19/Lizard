

#define INTRIN


using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Data;
using LTChess.Logic.NN;


using static LTChess.Logic.NN.SIMD;

namespace LTChess.Logic.NN.Simple768
{

    /// <summary>
    /// (Almost) everything in this namespace was adapted from https://github.com/TheBlackPlague/StockNemo/tree/master/Backend/Engine/NNUE.
    /// <br></br>
    /// 
    /// The only modifications were removing SSE-2 code, using AVX2 intrinsics rather than the slower <see cref="Vector256"/> class
    /// when possible, and condensing some of the methods so they are visually easier to follow.
    /// 
    /// <para></para>
    /// 
    /// This is/was being used as a starting point for fine tuning search parameters.
    /// The entire point of NNUE is that you take the evaluation that the network gives you as gospel
    /// and that way I don't have to spend time/resources on tweaking the ~50 evaluation parameters
    /// while also changing the ~25 search options.
    /// 
    /// </summary>
    public class NNUE768
    {

        private const int VectorSize = 32 / sizeof(short);

        //  2 * 6 * 64 = 768  
        private const int INPUT = Color.ColorNB * Piece.PieceNB * SquareNB;
        private const int HIDDEN = 256;
        private const int OUTPUT = 1;
        private const int CR_MIN = 0;
        private const int CR_MAX = 1 * QA;
        private const int SCALE = 400;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        private const int ACCUMULATOR_STACK_SIZE = 512;

        private readonly short[] FeatureWeight = new short[INPUT * HIDDEN];
        private readonly short[] FeatureBias = new short[HIDDEN];
        private readonly short[] OutWeight = new short[HIDDEN * 2 * OUTPUT];
        private readonly short[] OutBias = new short[OUTPUT];

        private Accumulator[] Accumulators;

        private readonly int[] Output = new int[OUTPUT];

        private int CurrentAccumulator;

        public NNUE768()
        {
            Accumulators = new Accumulator[ACCUMULATOR_STACK_SIZE];

            for (int i = 0; i < Accumulators.Length; i++)
            {
                Accumulators[i] = new Accumulator(HIDDEN);
            }

        }


        [MethodImpl(Inline)]
        public void ResetAccumulator() => CurrentAccumulator = 0;

        [MethodImpl(Inline)]
        public void PushAccumulator()
        {
            Accumulators[CurrentAccumulator].CopyTo(Accumulators[++CurrentAccumulator]);
        }

        [MethodImpl(Inline)]
        public void PullAccumulator() => CurrentAccumulator--;

        [MethodImpl(Inline)]
        public void RefreshAccumulator(Position pos)
        {

            Accumulator accumulator = Accumulators[CurrentAccumulator];
            //accumulator.Zero();
            accumulator.PreLoadBias(FeatureBias);

            for (int i = 0; i < 64; i++)
            {
                int pt = pos.bb.PieceTypes[i];
                if (pt == Piece.None)
                {
                    continue;
                }

                int pc = pos.bb.GetColorAtIndex(i);

                ActivateAccumulator(pt, pc, i, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EfficientlyUpdateAccumulator(int pt, int pc, int iFrom, int iTo)
        {
            const int colorStride = 64 * Piece.PieceNB;
            const int pieceStride = 64;

            int opPieceStride = pt * pieceStride;

            int whiteIndexFrom = pc * colorStride + opPieceStride + iFrom;
            int blackIndexFrom = Not(pc) * colorStride + opPieceStride + (iFrom ^ 56);
            int whiteIndexTo = pc * colorStride + opPieceStride + iTo;
            int blackIndexTo = Not(pc) * colorStride + opPieceStride + (iTo ^ 56);

            Accumulator accumulator = Accumulators[CurrentAccumulator];

            SubtractAndAddToAll(accumulator.White, FeatureWeight, whiteIndexFrom * HIDDEN, whiteIndexTo * HIDDEN);
            SubtractAndAddToAll(accumulator.Black, FeatureWeight, blackIndexFrom * HIDDEN, blackIndexTo * HIDDEN);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ActivateAccumulator(int pt, int pc, int i, bool Activate)
        {
            const int colorStride = 64 * Piece.PieceNB;
            const int pieceStride = 64;

            int opPieceStride = pt * pieceStride;

            int whiteIndex = pc * colorStride + opPieceStride + i;
            int blackIndex = Not(pc) * colorStride + opPieceStride + (i ^ 56);

            Accumulator accumulator = Accumulators[CurrentAccumulator];

            if (Activate)
            {
                AddToAll(accumulator.White, accumulator.Black, FeatureWeight, whiteIndex * HIDDEN, blackIndex * HIDDEN);
            }
            else
            {
                SubtractFromAll(accumulator.White, accumulator.Black, FeatureWeight, whiteIndex * HIDDEN, blackIndex * HIDDEN);
            }
        }


        [MethodImpl(Inline)]
        public int Evaluate(int ToMove)
        {
            Accumulator accumulator = Accumulators[CurrentAccumulator];

            if (ToMove == Color.White)
            {
                ClippedReLUFlattenAndForward(accumulator.White, accumulator.Black, OutWeight, Output);
            }
            else
            {
                ClippedReLUFlattenAndForward(accumulator.Black, accumulator.White, OutWeight, Output);
            }

            return (Output[0] + OutBias[0]) * SCALE / QAB;
        }




        [MethodImpl(Optimize)]
        public void ClippedReLUFlattenAndForward(short[] inputA, short[] inputB, short[] weight, int[] output)
        {
            const int step = HIDDEN / 4;
            const int loopMax = HIDDEN;

            Vector256<short> minVec256 = Vector256.Create<short>(CR_MIN);
            Vector256<short> maxVec256 = Vector256.Create<short>(CR_MAX);

            Vector256<int> sum = Vector256<int>.Zero;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                int idx1 = idxV + VectorSize * 1;
                int idx2 = idxV + VectorSize * 2;
                int idx3 = idxV + VectorSize * 3;

                sum += MultiplyAddAdjacent256(Load256(inputA, idxV).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idxV));
                sum += MultiplyAddAdjacent256(Load256(inputA, idx1).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx1));
                sum += MultiplyAddAdjacent256(Load256(inputA, idx2).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx2));
                sum += MultiplyAddAdjacent256(Load256(inputA, idx3).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx3));

                sum += MultiplyAddAdjacent256(Load256(inputB, idxV).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idxV + loopMax));
                sum += MultiplyAddAdjacent256(Load256(inputB, idx1).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx1 + loopMax));
                sum += MultiplyAddAdjacent256(Load256(inputB, idx2).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx2 + loopMax));
                sum += MultiplyAddAdjacent256(Load256(inputB, idx3).Clamp256(ref minVec256, ref maxVec256), Load256(weight, idx3 + loopMax));
            }

            output[0] = Vector256.Sum(sum);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static void AddToAll(short[] inputA, short[] inputB, short[] delta, int offA, int offB)
        {
            const int step = HIDDEN / 4;
            const int loopMax = HIDDEN;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                int idx1 = idxV + VectorSize * 1;
                int idx2 = idxV + VectorSize * 2;
                int idx3 = idxV + VectorSize * 3;

                Vector256<short> rAVec1 = Add256(Load256(inputA, idxV), Load256(delta, idxV + offA));
                Store256(ref rAVec1, inputA, idxV);

                Vector256<short> rAVec2 = Add256(Load256(inputA, idx1), Load256(delta, idx1 + offA));
                Store256(ref rAVec2, inputA, idx1);

                Vector256<short> rAVec3 = Add256(Load256(inputA, idx2), Load256(delta, idx2 + offA));
                Store256(ref rAVec3, inputA, idx2);

                Vector256<short> rAVec4 = Add256(Load256(inputA, idx3), Load256(delta, idx3 + offA));
                Store256(ref rAVec4, inputA, idx3);



                Vector256<short> rBVec1 = Add256(Load256(inputB, idxV), Load256(delta, idxV + offB));
                Store256(ref rBVec1, inputB, idxV);

                Vector256<short> rBVec2 = Add256(Load256(inputB, idx1), Load256(delta, idx1 + offB));
                Store256(ref rBVec2, inputB, idx1);

                Vector256<short> rBVec3 = Add256(Load256(inputB, idx2), Load256(delta, idx2 + offB));
                Store256(ref rBVec3, inputB, idx2);

                Vector256<short> rBVec4 = Add256(Load256(inputB, idx3), Load256(delta, idx3 + offB));
                Store256(ref rBVec4, inputB, idx3);

            }
        }


        [MethodImpl(Optimize)]
        public static void SubtractFromAll(short[] inputA, short[] inputB, short[] delta, int offA, int offB)
        {
            const int step = HIDDEN / 4;
            const int loopMax = HIDDEN;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                int idx1 = idxV + VectorSize * 1;
                int idx2 = idxV + VectorSize * 2;
                int idx3 = idxV + VectorSize * 3;

                Vector256<short> rAVec1 = Sub256(Load256(inputA, idxV), Load256(delta, idxV + offA));
                Store256(ref rAVec1, inputA, idxV);

                Vector256<short> rAVec2 = Sub256(Load256(inputA, idx1), Load256(delta, idx1 + offA));
                Store256(ref rAVec2, inputA, idx1);

                Vector256<short> rAVec3 = Sub256(Load256(inputA, idx2), Load256(delta, idx2 + offA));
                Store256(ref rAVec3, inputA, idx2);

                Vector256<short> rAVec4 = Sub256(Load256(inputA, idx3), Load256(delta, idx3 + offA));
                Store256(ref rAVec4, inputA, idx3);



                Vector256<short> rBVec1 = Sub256(Load256(inputB, idxV), Load256(delta, idxV + offB));
                Store256(ref rBVec1, inputB, idxV);

                Vector256<short> rBVec2 = Sub256(Load256(inputB, idx1), Load256(delta, idx1 + offB));
                Store256(ref rBVec2, inputB, idx1);

                Vector256<short> rBVec3 = Sub256(Load256(inputB, idx2), Load256(delta, idx2 + offB));
                Store256(ref rBVec3, inputB, idx2);

                Vector256<short> rBVec4 = Sub256(Load256(inputB, idx3), Load256(delta, idx3 + offB));
                Store256(ref rBVec4, inputB, idx3);
            }
        }



        [MethodImpl(Optimize)]
        public static unsafe void SubtractAndAddToAll(short[] input, short[] delta, int offA, int offB)
        {
            const int loopMax = HIDDEN;
            const int step = HIDDEN / 4;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                int idx1 = idxV + VectorSize * 1;
                int idx2 = idxV + VectorSize * 2;
                int idx3 = idxV + VectorSize * 3;


                Vector256<short> rVec1 = Add256(Load256(delta, idxV + offB), Sub256(Load256(input, idxV), Load256(delta, idxV + offA)));
                Store256(ref rVec1, input, idxV);

                Vector256<short> rVec2 = Add256(Load256(delta, idx1 + offB), Sub256(Load256(input, idx1), Load256(delta, idx1 + offA)));
                Store256(ref rVec2, input, idx1);

                Vector256<short> rVec3 = Add256(Load256(delta, idx2 + offB), Sub256(Load256(input, idx2), Load256(delta, idx2 + offA)));
                Store256(ref rVec3, input, idx2);

                Vector256<short> rVec4 = Add256(Load256(delta, idx3 + offB), Sub256(Load256(input, idx3), Load256(delta, idx3 + offA)));
                Store256(ref rVec4, input, idx3);

            }
        }



        public void Randomize()
        {
            //int seed = 1234;
            Random r = new Random();

            for (int i = 0; i < FeatureWeight.Length; i++)
            {
                FeatureWeight[i] = (short)(r.Next(1000) - 500);
            }

            for (int i = 0; i < FeatureBias.Length; i++)
            {
                FeatureBias[i] = (short)(r.Next(1000) - 500);
            }

            for (int i = 0; i < OutWeight.Length; i++)
            {
                OutWeight[i] = (short)(r.Next(400) - 200);
            }

            OutBias[0] = 1225;

        }


        public void FromTXT(Stream stream)
        {
            using StreamReader sr = new StreamReader(stream);

            string line;
            string[] splits;

            line = sr.ReadLine().TrimEnd(']').Substring(new string("FeatureWeight:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < FeatureWeight.Length; i++)
            {
                FeatureWeight[i] = short.Parse(splits[i]);
            }

            line = sr.ReadLine().TrimEnd(']').Substring(new string("FeatureBias:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < FeatureBias.Length; i++)
            {
                FeatureBias[i] = short.Parse(splits[i]);
            }

            line = sr.ReadLine().TrimEnd(']').Substring(new string("OutWeight:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < OutWeight.Length; i++)
            {
                OutWeight[i] = short.Parse(splits[i]);
            }

            OutBias[0] = short.Parse(sr.ReadLine().TrimEnd(']').Substring(new string("OutBias:[").Length));
            OutBias[0] = 1230;
        }
    }
}
