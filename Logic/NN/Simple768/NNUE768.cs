


using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Data;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;

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
    public unsafe class NNUE768
    {

        public const int VectorSize = 32 / sizeof(short);

        //  2 * 6 * 64 = 768  
        public const int INPUT = Color.ColorNB * Piece.PieceNB * SquareNB;
        public const int HIDDEN = 256;
        public const int OUTPUT = 1;
        public const int CR_MIN = 0;
        public const int CR_MAX = 1 * QA;
        public const int SCALE = 400;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        private const int ACCUMULATOR_STACK_SIZE = 512;

        private readonly short[] OutBias = new short[OUTPUT];

        private readonly Vector256<short>* FeatureWeight;
        
        private readonly Vector256<short>* FeatureBias;
        
        private readonly Vector256<short>* OutWeight;

        [FixedAddressValueType]
        private readonly AccumulatorPSQT[] Accumulators;

        private readonly int[] Output = new int[OUTPUT];

        private int CurrentAccumulator;

        public NNUE768()
        {
            if (SearchConstants.Threads != 1)
            {
                Log("WARN This network architecture currently only works for single-thread access!");
                Log("The output will almost certainly be incorrect.\n");
            }

            Accumulators = new AccumulatorPSQT[ACCUMULATOR_STACK_SIZE];

            for (int i = 0; i < Accumulators.Length; i++)
            {
                Accumulators[i] = new AccumulatorPSQT();
            }

            FeatureWeight   = (Vector256<short>*) AlignedAllocZeroed(sizeof(short) * (INPUT * HIDDEN),      AllocAlignment);
            FeatureBias     = (Vector256<short>*) AlignedAllocZeroed(sizeof(short) * (HIDDEN),              AllocAlignment);
            OutWeight       = (Vector256<short>*) AlignedAllocZeroed(sizeof(short) * (HIDDEN * 2 * OUTPUT), AllocAlignment);
        }


        [MethodImpl(Inline)]
        public void ResetAccumulator() => CurrentAccumulator = 0;

        [MethodImpl(Inline)]
        public void PushAccumulator()
        {
            int size = HIDDEN * Unsafe.SizeOf<short>();

            Unsafe.CopyBlockUnaligned(
                Accumulators[CurrentAccumulator + 1].White,
                Accumulators[CurrentAccumulator].White,
                (uint)size
            );
            Unsafe.CopyBlockUnaligned(
                Accumulators[CurrentAccumulator + 1].Black,
                Accumulators[CurrentAccumulator].Black,
                (uint)size
            );

            CurrentAccumulator++;
        }

        [MethodImpl(Inline)]
        public void PullAccumulator() => CurrentAccumulator--;


        public void RefreshAccumulator(Position pos)
        {
            ref AccumulatorPSQT accumulator = ref Accumulators[CurrentAccumulator];
            Buffer.MemoryCopy((void*)FeatureBias, (void*)(accumulator.White), HIDDEN * 2, HIDDEN * 2);
            Buffer.MemoryCopy((void*)FeatureBias, (void*)(accumulator.Black), HIDDEN * 2, HIDDEN * 2);

            ulong occ = pos.bb.Occupancy;
            while (occ != 0)
            {
                int i = lsb(occ);

                int pt = pos.bb.GetPieceAtIndex(i);
                int pc = pos.bb.GetColorAtIndex(i);
                ActivateAccumulator(pt, pc, i, true);

                occ = poplsb(occ);
            }
        }


        public void EfficientlyUpdateAccumulator(int pt, int pc, int iFrom, int iTo)
        {
            const int colorStride = 64 * Piece.PieceNB;
            const int pieceStride = 64;

            int opPieceStride = pt * pieceStride;

            int whiteIndexFrom = pc * colorStride + opPieceStride + iFrom;
            int blackIndexFrom = Not(pc) * colorStride + opPieceStride + (iFrom ^ 56);
            int whiteIndexTo = pc * colorStride + opPieceStride + iTo;
            int blackIndexTo = Not(pc) * colorStride + opPieceStride + (iTo ^ 56);

            ref AccumulatorPSQT accumulator = ref Accumulators[CurrentAccumulator];
            SubtractAndAddToAll(accumulator.White, FeatureWeight, whiteIndexFrom * VectorSize, whiteIndexTo * VectorSize);
            SubtractAndAddToAll(accumulator.Black, FeatureWeight, blackIndexFrom * VectorSize, blackIndexTo * VectorSize);
        }


        public void ActivateAccumulator(int pt, int pc, int i, bool Activate)
        {
            const int colorStride = 64 * Piece.PieceNB;
            const int pieceStride = 64;

            int opPieceStride = pt * pieceStride;

            int whiteIndex = pc * colorStride + opPieceStride + i;
            int blackIndex = Not(pc) * colorStride + opPieceStride + (i ^ 56);

            ref AccumulatorPSQT accumulator = ref Accumulators[CurrentAccumulator];

            if (Activate)
            {
                AddToAll(accumulator.White, accumulator.Black, FeatureWeight, whiteIndex * VectorSize, blackIndex * VectorSize);
            }
            else
            {
                SubtractFromAll(accumulator.White, accumulator.Black, FeatureWeight, whiteIndex * VectorSize, blackIndex * VectorSize);
            }
        }



        public int Evaluate(int ToMove)
        {
            ref AccumulatorPSQT accumulator = ref Accumulators[CurrentAccumulator];

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





        public void ClippedReLUFlattenAndForward(in Vector256<short>* inputA, in Vector256<short>* inputB, in Vector256<short>* weights, in int[] output)
        {
            const int loopMax = (HIDDEN) / 16;
            const int step = (HIDDEN / 4) / 16;

            Vector256<short> minVec256 = Vector256.Create<short>(CR_MIN);
            Vector256<short> maxVec256 = Vector256.Create<short>(CR_MAX);

            Vector256<int> sum = Vector256<int>.Zero;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                sum += MultiplyAddAdjacent256(inputA[(idxV    )].Clamp256(ref minVec256, ref maxVec256), weights[(idxV    )]);
                sum += MultiplyAddAdjacent256(inputA[(idxV + 1)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 1)]);
                sum += MultiplyAddAdjacent256(inputA[(idxV + 2)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 2)]);
                sum += MultiplyAddAdjacent256(inputA[(idxV + 3)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 3)]);

                sum += MultiplyAddAdjacent256(inputB[(idxV    )].Clamp256(ref minVec256, ref maxVec256), weights[(idxV    ) + 16]);
                sum += MultiplyAddAdjacent256(inputB[(idxV + 1)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 1) + 16]);
                sum += MultiplyAddAdjacent256(inputB[(idxV + 2)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 2) + 16]);
                sum += MultiplyAddAdjacent256(inputB[(idxV + 3)].Clamp256(ref minVec256, ref maxVec256), weights[(idxV + 3) + 16]);
            }

            output[0] = Vector256.Sum(sum);
        }


        public static void AddToAll(in Vector256<short>* inputA, in Vector256<short>* inputB, in Vector256<short>* delta, int offA, int offB)
        {
            const int loopMax = (HIDDEN) / 16;
            const int step = (HIDDEN / 4) / 16;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                inputA[(idxV    )] = Add256(inputA[(idxV    )], delta[(idxV    ) + offA]);
                inputA[(idxV + 1)] = Add256(inputA[(idxV + 1)], delta[(idxV + 1) + offA]);
                inputA[(idxV + 2)] = Add256(inputA[(idxV + 2)], delta[(idxV + 2) + offA]);
                inputA[(idxV + 3)] = Add256(inputA[(idxV + 3)], delta[(idxV + 3) + offA]);

                inputB[(idxV    )] = Add256(inputB[(idxV    )], delta[(idxV    ) + offB]);
                inputB[(idxV + 1)] = Add256(inputB[(idxV + 1)], delta[(idxV + 1) + offB]);
                inputB[(idxV + 2)] = Add256(inputB[(idxV + 2)], delta[(idxV + 2) + offB]);
                inputB[(idxV + 3)] = Add256(inputB[(idxV + 3)], delta[(idxV + 3) + offB]);
            }
        }



        public static void SubtractFromAll(in Vector256<short>* inputA, in Vector256<short>* inputB, in Vector256<short>* delta, int offA, int offB)
        {
            const int loopMax = (HIDDEN) / 16;
            const int step = (HIDDEN / 4) / 16;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                inputA[(idxV    )] = Sub256(inputA[(idxV    )], delta[(idxV    ) + offA]);
                inputA[(idxV + 1)] = Sub256(inputA[(idxV + 1)], delta[(idxV + 1) + offA]);
                inputA[(idxV + 2)] = Sub256(inputA[(idxV + 2)], delta[(idxV + 2) + offA]);
                inputA[(idxV + 3)] = Sub256(inputA[(idxV + 3)], delta[(idxV + 3) + offA]);

                inputB[(idxV    )] = Sub256(inputB[(idxV    )], delta[(idxV    ) + offB]);
                inputB[(idxV + 1)] = Sub256(inputB[(idxV + 1)], delta[(idxV + 1) + offB]);
                inputB[(idxV + 2)] = Sub256(inputB[(idxV + 2)], delta[(idxV + 2) + offB]);
                inputB[(idxV + 3)] = Sub256(inputB[(idxV + 3)], delta[(idxV + 3) + offB]);
            }
        }



        public static void SubtractAndAddToAll(in Vector256<short>* input, in Vector256<short>* delta, int offA, int offB)
        {
            const int loopMax = (HIDDEN) / 16;
            const int step = (HIDDEN / 4) / 16;

            for (int idxV = 0; idxV < loopMax; idxV += step)
            {
                input[(idxV    )] = Add256(delta[(idxV    ) + offB], Sub256(input[(idxV    )], delta[(idxV    ) + offA]));
                input[(idxV + 1)] = Add256(delta[(idxV + 1) + offB], Sub256(input[(idxV + 1)], delta[(idxV + 1) + offA]));
                input[(idxV + 2)] = Add256(delta[(idxV + 2) + offB], Sub256(input[(idxV + 2)], delta[(idxV + 2) + offA]));
                input[(idxV + 3)] = Add256(delta[(idxV + 3) + offB], Sub256(input[(idxV + 3)], delta[(idxV + 3) + offA]));
            }
        }



        public void FromTXT(Stream stream)
        {

            short[] _FeatureWeight = new short[INPUT * HIDDEN];
            short[] _FeatureBias = new short[HIDDEN];
            short[] _OutWeight = new short[HIDDEN * 2 * OUTPUT];
            short[] OutBias = new short[OUTPUT];

            using StreamReader sr = new StreamReader(stream);

            string line;
            string[] splits;

            line = sr.ReadLine().TrimEnd(']').Substring(new string("FeatureWeight:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < (INPUT * HIDDEN); i++)
            {
                _FeatureWeight[i] = short.Parse(splits[i]);
            }

            line = sr.ReadLine().TrimEnd(']').Substring(new string("FeatureBias:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < (HIDDEN); i++)
            {
                _FeatureBias[i] = short.Parse(splits[i]);
            }

            line = sr.ReadLine().TrimEnd(']').Substring(new string("OutWeight:[").Length);
            splits = line.Split(',');
            for (int i = 0; i < (HIDDEN * 2 * OUTPUT); i++)
            {
                _OutWeight[i] = short.Parse(splits[i]);
            }

            OutBias[0] = short.Parse(sr.ReadLine().TrimEnd(']').Substring(new string("OutBias:[").Length));
            OutBias[0] = 1230;



            for (int i = 0; i < _FeatureWeight.Length; i += VSize.Short)
            {
                FeatureWeight[i / VSize.Short] = Avx.LoadDquVector256((short*)UnsafeAddrOfPinnedArrayElementUnchecked(_FeatureWeight, i));
            }

            for (int i = 0; i < _FeatureBias.Length; i += VSize.Short)
            {
                FeatureBias[i / VSize.Short] = Load256(_FeatureBias, i);
            }

            for (int i = 0; i < _OutWeight.Length; i += VSize.Short)
            {
                OutWeight[i / VSize.Short] = Load256(_OutWeight, i);
            }

        }
    }
}
