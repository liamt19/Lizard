
#define UNROLL
#undef UNROLL

#define SPARSE
#undef SPARSE

using FTType = float;

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using Lizard.Properties;

using static Lizard.Logic.NN.FunUnrollThings;
using System.Reflection;
using System;

namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe partial class Layered768
    {
        private const int InputBuckets = 4;
        public const int InputSize = 768;
        public const int HiddenSize = 768;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;

        public const int SIMD_CHUNKS = HiddenSize / VSize.Float;

        public const string NetworkName = "lizard-768x4x16x32x1-320-params.bin";
        private const string DefaultNetwork = "nn.nnue";

#if FT_QUANTIZE
        private static readonly Vector256<FTType>* FeatureWeights;
        private static readonly Vector256<FTType>* FeatureBiases;

        private static readonly float* L1_WEIGHTS;
        private static readonly float* L1_BIASES;
#else
        private static readonly Vector256<float>* FeatureWeights;
        private static readonly Vector256<float>* FeatureBiases;

        private static readonly float* L1_WEIGHTS;
        private static readonly float* L1_BIASES;
#endif

        private static readonly float* L2_WEIGHTS;
        private static readonly float* L2_BIASES;

        private static readonly Vector256<float>* OutputWeights;
        private static float OutputBias;

        private static readonly Vector128<ushort>* LookupIndices;

        private const int FeatureWeightElements = InputSize * HiddenSize * InputBuckets;
        private const int FeatureBiasElements = HiddenSize;
        private const int N_HIDDEN = HiddenSize * 2;
        private const int N_L1 = 16;
        private const int N_L2 = 32;

        private static readonly int[] KingBuckets =
        [
            0, 0, 1, 1, 5, 5, 4, 4,
            0, 0, 1, 1, 5, 5, 4, 4,
            2, 2, 3, 3, 7, 7, 6, 6,
            2, 2, 3, 3, 7, 7, 6, 6,
            2, 2, 3, 3, 7, 7, 6, 6,
            2, 2, 3, 3, 7, 7, 6, 6,
            2, 2, 3, 3, 7, 7, 6, 6,
            2, 2, 3, 3, 7, 7, 6, 6,
        ];

        static Layered768()
        {
            FeatureWeights = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * FeatureWeightElements);
            FeatureBiases = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * FeatureBiasElements);

            L1_WEIGHTS = (float*)AlignedAllocZeroed(sizeof(float) * N_HIDDEN * N_L1);
            L1_BIASES = (float*)AlignedAllocZeroed(sizeof(float) * N_L1);

            L2_WEIGHTS = (float*)AlignedAllocZeroed(sizeof(float) * N_L1 * N_L2);
            L2_BIASES = (float*)AlignedAllocZeroed(sizeof(float) * N_L2);

            OutputWeights = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * N_L2);

            LookupIndices = (Vector128<ushort>*)AlignedAllocZeroed((nuint)(sizeof(Vector128<ushort>) * 256), AllocAlignment);
            SetupLookupTable();


            string networkToLoad = DefaultNetwork;

            try
            {
                var evalFile = Assembly.GetEntryAssembly().GetCustomAttribute<EvalFileAttribute>().EvalFile;
                networkToLoad = evalFile;
            }
            catch { }

            Initialize(networkToLoad);
        }

        public static void Initialize(string networkToLoad)
        {
            Stream kpFile;

            if (File.Exists(networkToLoad))
            {
                kpFile = File.OpenRead(networkToLoad);
                Log("Using NNUE with 768 network " + networkToLoad);
            }
            else if (File.Exists(NetworkName))
            {
                kpFile = File.OpenRead(NetworkName);
                Log("Using NNUE with 768 network " + NetworkName);
            }
            else
            {
                //  Just load the default network
                networkToLoad = NetworkName;
                Log("Using NNUE with 768 network " + NetworkName);

                string resourceName = networkToLoad.Replace(".nnue", string.Empty).Replace(".bin", string.Empty);

                object? o = Resources.ResourceManager.GetObject(resourceName);
                if (o == null)
                {
                    Console.WriteLine("The 768 NNRunOption was set to true, but there isn't a valid 768 network to load!");
                    Console.WriteLine("This program looks for a 768 network named " + "'nn.nnue' or '" + NetworkName + "' within the current directory.");
                    Console.WriteLine("If neither can be found, then '" + NetworkName + "' needs to be a compiled as a resource as a fallback!");
                    Console.ReadLine();
                    Environment.Exit(-1);
                }

                byte[] data = (byte[])o;
                kpFile = new MemoryStream(data);
            }


            using BinaryReader br = new BinaryReader(kpFile);
            var stream = br.BaseStream;

            float* ftw = (float*)FeatureWeights;
            for (int i = 0; i < FeatureWeightElements; i++)
            {
                ftw[i] = br.ReadSingle();
            }

            var ftb = (float*)FeatureBiases;
            for (int i = 0; i < FeatureBiasElements; i++)
            {
                ftb[i] = br.ReadSingle();
            }

            for (int i = 0; i < N_HIDDEN * N_L1; i++)
                L1_WEIGHTS[i] = br.ReadSingle();
            for (int i = 0; i < N_L1; i++)
                L1_BIASES[i] = br.ReadSingle();




            for (int i = 0; i < N_L1 * N_L2; i++)
                L2_WEIGHTS[i] = br.ReadSingle();
            for (int i = 0; i < N_L2; i++)
                L2_BIASES[i] = br.ReadSingle();

#if SPARSE

            float* temp = (float*)AlignedAllocZeroed(sizeof(float) * N_HIDDEN * N_L1);
            Unsafe.CopyBlock(temp, L1_WEIGHTS, sizeof(float) * N_HIDDEN * N_L1);
            for (int i = 0; i < N_L1 * N_L2; i++)
                L1_WEIGHTS[GetWeightIndex(i)] = temp[i];
            NativeMemory.AlignedFree(temp);

            const int WIDTH = VSize.Short;
            const int WEIGHT_CHUNKS = ((InputBuckets * 12 * 64) * N_HIDDEN) / WIDTH;
            const int BIAS_CHUNKS = N_HIDDEN / WIDTH;

            var weights = FeatureWeights;
            var biases = FeatureBiases;

            for (int i = 0; i < WEIGHT_CHUNKS; i += 2)
            {
                var a1 = Avx2.ExtractVector128(weights[i + 0], 1);
                var b0 = Avx2.ExtractVector128(weights[i + 1], 0);

                weights[i + 0] = Avx2.InsertVector128(weights[i + 0], b0, 1);
                weights[i + 1] = Avx2.InsertVector128(weights[i + 1], a1, 0);
            }

            for (int i = 0; i < BIAS_CHUNKS; i += 2)
            {
                var a1 = Avx2.ExtractVector128(biases[i + 0], 1);
                var b0 = Avx2.ExtractVector128(biases[i + 1], 0);

                biases[i + 0] = Avx2.InsertVector128(biases[i + 0], b0, 1);
                biases[i + 1] = Avx2.InsertVector128(biases[i + 1], a1, 0);
            }
#endif

            var outw = (float*)OutputWeights;
            for (int i = 0; i < N_L2; i++)
            {
                outw[i] = br.ReadSingle();
            }
            OutputBias = br.ReadSingle();

            if (br.BaseStream.Position != br.BaseStream.Length)
            {
                Console.WriteLine($"Too few weights read!! {br.BaseStream.Position / 4} of {br.BaseStream.Length / 4}");
            }

        }

        public static void RefreshAccumulator(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var w = (Vector256<FTType>*)accumulator.White;
            var b = (Vector256<FTType>*)accumulator.Black;

            Unsafe.CopyBlock(w, FeatureBiases, sizeof(FTType) * FeatureBiasElements);
            Unsafe.CopyBlock(b, FeatureBiases, sizeof(FTType) * FeatureBiasElements);

            int wk = pos.State->KingSquares[White];
            int bk = pos.State->KingSquares[Black];

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                (int wIdx, int bIdx) = FeatureIndex(pc, pt, pieceIdx, wk, bk);
                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    w[i] = Avx2.Add(w[i], FeatureWeights[wIdx + i]);
                    b[i] = Avx2.Add(b[i], FeatureWeights[bIdx + i]);
                }
            }
            accumulator.NeedsRefresh[White] = accumulator.NeedsRefresh[Black] = false;
        }

        public static void RefreshAccumulatorPerspective(Position pos, int perspective)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var ourAccumulation = (Vector256<FTType>*) (accumulator[perspective]);
            Unsafe.CopyBlock(ourAccumulation, FeatureBiases, sizeof(FTType) * FeatureBiasElements);

            int ourKing = pos.State->KingSquares[perspective];

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                int idx = FeatureIndexSingle(pc, pt, pieceIdx, ourKing, perspective);
                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    ourAccumulation[i] = Avx2.Add(ourAccumulation[i], FeatureWeights[idx + i]);
                }
            }

            accumulator.NeedsRefresh[perspective] = false;
        }


        public static int GetEvaluation(Position pos)
        {

#if SPARSE
            sbyte* x0 = stackalloc sbyte[2 * N_HIDDEN];
            float* x1 = stackalloc float[16];
            float* x2 = stackalloc float[32];

            InputReLU(pos, x0);
            L1AffineReLU(x0, x1);
            ForwardL2(x1, x2);

            return (int)ForwardOutput(x2);
#else
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

            var us = (Vector256<float>*)accumulator[pos.ToMove];
            var them = (Vector256<float>*)accumulator[Not(pos.ToMove)];


            float* x0 = stackalloc float[N_HIDDEN];
            float* x1 = stackalloc float[16];
            float* x2 = stackalloc float[32];

            Vector256<float>* bufferVec = (Vector256<float>*)x0;

            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                bufferVec[i              ] = Avx2.Max(us  [i], Vector256<float>.Zero);
                bufferVec[i + SIMD_CHUNKS] = Avx2.Max(them[i], Vector256<float>.Zero);
            }

            ForwardL1(x0, x1);
            ForwardL2(x1, x2);
            return (int)ForwardOutput(x2);
#endif


        }



        private static int FeatureIndexSingle(int pc, int pt, int sq, int kingSq, int perspective)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            if (perspective == Black)
            {
                sq ^= 56;
                kingSq ^= 56;
            }

            if (kingSq % 8 > 3)
            {
                sq ^= 7;
                kingSq ^= 7;
            }

            return ((768 * KingBuckets[kingSq]) + ((pc ^ perspective) * ColorStride) + (pt * PieceStride) + (sq)) * SIMD_CHUNKS;
        }

        private static (int, int) FeatureIndex(int pc, int pt, int sq, int wk, int bk)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int wSq = sq;
            int bSq = sq ^ 56;

            if (wk % 8 > 3)
            {
                wk ^= 7;
                wSq ^= 7;
            }

            bk ^= 56;
            if (bk % 8 > 3)
            {
                bk ^= 7;
                bSq ^= 7;
            }

            int whiteIndex = (768 * KingBuckets[wk]) + (pc * ColorStride) + (pt * PieceStride) + (wSq);
            int blackIndex = (768 * KingBuckets[bk]) + (Not(pc) * ColorStride) + (pt * PieceStride) + (bSq);

            return (whiteIndex * SIMD_CHUNKS, blackIndex * SIMD_CHUNKS);
        }


#if UNROLL

        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* accumulator = pos.NextState->Accumulator;
            pos.State->Accumulator->CopyTo(accumulator);

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = pos.ToMove;
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            var whiteAccumulation = (Vector256<FTType>*) ((*accumulator)[White]);
            var blackAccumulation = (Vector256<FTType>*) ((*accumulator)[Black]);

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                accumulator->NeedsRefresh[us] = true;

                var theirAccumulation = (Vector256<FTType>*) ((*accumulator)[them]);
                int theirKing = pos.State->KingSquares[them];

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);

                    FunUnrollThings.SubSubAdd((FTType*)theirAccumulation,
                        (FTType*)(FeatureWeights + from),
                        (FTType*)(FeatureWeights + cap),
                        (FTType*)(FeatureWeights + to));
                }
                else if (m.Castle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare;

                    to = FeatureIndexSingle(us, ourPiece, m.CastlingKingSquare, theirKing, them);

                    int rookFrom = FeatureIndexSingle(us, Rook, rookFromSq, theirKing, them);
                    int rookTo = FeatureIndexSingle(us, Rook, rookToSq, theirKing, them);

                    SubSubAddAdd(theirAccumulation,
                        (FeatureWeights + from),
                        (FeatureWeights + rookFrom),
                        (FeatureWeights + to),
                        (FeatureWeights + rookTo));
                }
                else
                {
                    FunUnrollThings.SubAdd((FTType*)theirAccumulation,
                        (FTType*)(FeatureWeights + from),
                        (FTType*)(FeatureWeights + to));
                }
            }
            else
            {
                int wKing = pos.State->KingSquares[White];
                int bKing = pos.State->KingSquares[Black];

                (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom, wKing, bKing);
                (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo, wKing, bKing);

                if (theirPiece != None)
                {
                    (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo, wKing, bKing);

                    FunUnrollThings.SubSubAdd((FTType*)whiteAccumulation,
                        (FTType*)(FeatureWeights + wFrom),
                        (FTType*)(FeatureWeights + wCap),
                        (FTType*)(FeatureWeights + wTo));

                    FunUnrollThings.SubSubAdd((FTType*)blackAccumulation,
                        (FTType*)(FeatureWeights + bFrom),
                        (FTType*)(FeatureWeights + bCap),
                        (FTType*)(FeatureWeights + bTo));
                }
                else if (m.EnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    FunUnrollThings.SubSubAdd((FTType*)whiteAccumulation,
                        (FTType*)(FeatureWeights + wFrom),
                        (FTType*)(FeatureWeights + wCap),
                        (FTType*)(FeatureWeights + wTo));

                    FunUnrollThings.SubSubAdd((FTType*)blackAccumulation,
                        (FTType*)(FeatureWeights + bFrom),
                        (FTType*)(FeatureWeights + bCap),
                        (FTType*)(FeatureWeights + bTo));
                }
                else
                {
                    FunUnrollThings.SubAdd((FTType*)whiteAccumulation,
                        (FTType*)(FeatureWeights + wFrom),
                        (FTType*)(FeatureWeights + wTo));

                    FunUnrollThings.SubAdd((FTType*)blackAccumulation,
                        (FTType*)(FeatureWeights + bFrom),
                        (FTType*)(FeatureWeights + bTo));
                }
            }
        }

#else

        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* accumulator = pos.NextState->Accumulator;
            pos.State->Accumulator->CopyTo(accumulator);

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = pos.ToMove;
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            var whiteAccumulation = (Vector256<FTType>*) ((*accumulator)[White]);
            var blackAccumulation = (Vector256<FTType>*) ((*accumulator)[Black]);

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                accumulator->NeedsRefresh[us] = true;

                var theirAccumulation = (Vector256<FTType>*) ((*accumulator)[them]);
                int theirKing = pos.State->KingSquares[them];

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);

                    SubSubAdd(theirAccumulation,
                        (FeatureWeights + from),
                        (FeatureWeights + cap),
                        (FeatureWeights + to));
                }
                else if (m.Castle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare;

                    to = FeatureIndexSingle(us, ourPiece, m.CastlingKingSquare, theirKing, them);

                    int rookFrom = FeatureIndexSingle(us, Rook, rookFromSq, theirKing, them);
                    int rookTo = FeatureIndexSingle(us, Rook, rookToSq, theirKing, them);

                    SubSubAddAdd(theirAccumulation,
                        (FeatureWeights + from),
                        (FeatureWeights + rookFrom),
                        (FeatureWeights + to),
                        (FeatureWeights + rookTo));
                }
                else
                {
                    SubAdd(theirAccumulation,
                        (FeatureWeights + from),
                        (FeatureWeights + to));
                }
            }
            else
            {
                int wKing = pos.State->KingSquares[White];
                int bKing = pos.State->KingSquares[Black];

                (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom, wKing, bKing);
                (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo, wKing, bKing);

                if (theirPiece != None)
                {
                    (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo, wKing, bKing);

                    SubSubAdd(whiteAccumulation,
                        (FeatureWeights + wFrom),
                        (FeatureWeights + wCap),
                        (FeatureWeights + wTo));

                    SubSubAdd(blackAccumulation,
                        (FeatureWeights + bFrom),
                        (FeatureWeights + bCap),
                        (FeatureWeights + bTo));
                }
                else if (m.EnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    SubSubAdd(whiteAccumulation,
                        (FeatureWeights + wFrom),
                        (FeatureWeights + wCap),
                        (FeatureWeights + wTo));

                    SubSubAdd(blackAccumulation,
                        (FeatureWeights + bFrom),
                        (FeatureWeights + bCap),
                        (FeatureWeights + bTo));
                }
                else
                {
                    SubAdd(whiteAccumulation,
                        (FeatureWeights + wFrom),
                        (FeatureWeights + wTo));

                    SubAdd(blackAccumulation,
                        (FeatureWeights + bFrom),
                        (FeatureWeights + bTo));
                }
            }
        }
#endif



        private static void SubAdd(Vector256<FTType>* src, Vector256<FTType>* sub1, Vector256<FTType>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]);
            }
        }

        private static void SubSubAdd(Vector256<FTType>* src, Vector256<FTType>* sub1, Vector256<FTType>* sub2, Vector256<FTType>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]), sub2[i]);
            }
        }

        private static void SubSubAddAdd(Vector256<FTType>* src, Vector256<FTType>* sub1, Vector256<FTType>* sub2, Vector256<FTType>* add1, Vector256<FTType>* add2)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.Add(src[i], add1[i]), add2[i]), sub1[i]), sub2[i]);
            }
        }

        private static void NetStats(string layerName, void* layer, int n)
        {
            long avg = 0;
            int max = int.MinValue;
            int min = int.MaxValue;
            short* ptr = (short*)layer;
            for (int i = 0; i < n; i++)
            {
                if (ptr[i] > max)
                {
                    max = ptr[i];
                }
                if (ptr[i] < min)
                {
                    min = ptr[i];
                }
                avg += ptr[i];
            }

            Log(layerName + "\tmin: " + min + ", max: " + max + ", avg: " + (double)avg / n);
        }



        
    }
}
