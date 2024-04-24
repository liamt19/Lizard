using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static Lizard.Logic.NN.FunUnrollThings;
using static Lizard.Logic.NN.NNUE;

namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe partial class Simple768
    {
        public const int InputSize = 768;
        public const int HiddenSize = 1536;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;
        private const bool SelectOutputBucket = (OutputBuckets != 1);

        private static readonly int SIMD_CHUNKS_512 = HiddenSize / Vector512<short>.Count;
        private static readonly int SIMD_CHUNKS_256 = HiddenSize / Vector256<short>.Count;

        public const string NetworkName = "iguana-wiggle-epoch11.bin";


        public static readonly short* FeatureWeights;
        public static readonly short* FeatureBiases;
        public static readonly short* LayerWeights;
        public static readonly short* LayerBiases;

        private const int FeatureWeightElements = InputSize * HiddenSize;
        private const int FeatureBiasElements = HiddenSize;

        private const int LayerWeightElements = HiddenSize * 2 * OutputBuckets;
        private const int LayerBiasElements = OutputBuckets;

        public static long ExpectedNetworkSize => (FeatureWeightElements + FeatureBiasElements + LayerWeightElements + LayerBiasElements) * sizeof(short);

        static Simple768()
        {
            FeatureWeights = (short*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);
            FeatureBiases = (short*)AlignedAllocZeroed(sizeof(short) * FeatureBiasElements);

            LayerWeights = (short*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);
            LayerBiases = (short*)AlignedAllocZeroed(sizeof(short) * (nuint)Math.Max(LayerBiasElements, VSize.Short));

            string networkToLoad = NetworkName;

            try
            {
                var evalFile = Assembly.GetEntryAssembly().GetCustomAttribute<EvalFileAttribute>().EvalFile;
                networkToLoad = evalFile;
            }
            catch { }

            Initialize(networkToLoad);
        }

        public static void Initialize(string networkToLoad, bool exitIfFail = true)
        {
            Stream netFile = NNUE.TryOpenFile(networkToLoad, exitIfFail);

            using BinaryReader br = new BinaryReader(netFile);
            var stream = br.BaseStream;
            long toRead = ExpectedNetworkSize;
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("Simple768's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid 768 network, or has different layer sizes than the hardcoded ones.");
                if (exitIfFail)
                {
                    Environment.Exit(-1);
                }
                else
                {
                    //  Don't overwrite the existing data, just abort.
                    return;
                }
            }

            for (int i = 0; i < FeatureWeightElements; i++)
            {
                FeatureWeights[i] = br.ReadInt16();
            }

            for (int i = 0; i < FeatureBiasElements; i++)
            {
                FeatureBiases[i] = br.ReadInt16();
            }

            for (int i = 0; i < LayerWeightElements; i++)
            {
                LayerWeights[i] = br.ReadInt16();
            }

            for (int i = 0; i < LayerBiasElements; i++)
            {
                LayerBiases[i] = br.ReadInt16();
            }

            if (OutputBuckets > 1)
            {
                //  These weights are stored in column major order, but they are easier to use in row major order.
                //  The first 8 weights in the binary file are actually the first weight for each of the 8 output buckets,
                //  so we will transpose them so that the all of the weights for each output bucket are contiguous.
                TransposeLayerWeights(LayerWeights, HiddenSize * 2, OutputBuckets);
            }

#if DEBUG
            NetStats("ft weight", FeatureWeights, FeatureWeightElements);
            NetStats("ft bias\t", FeatureBiases, FeatureBiasElements);

            NetStats("fc weight", LayerWeights, LayerWeightElements);
            NetStats("fc bias", LayerBiases, 1);

            Log("Init Simple768 done");
#endif
        }

        public static void RefreshAccumulator(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var w = (Vector512<short>*)accumulator.White;
            var b = (Vector512<short>*)accumulator.Black;

            Unsafe.CopyBlock(accumulator.White, FeatureBiases, sizeof(short) * HiddenSize);
            Unsafe.CopyBlock(accumulator.Black, FeatureBiases, sizeof(short) * HiddenSize);

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                (int wIdx, int bIdx) = FeatureIndex(pc, pt, pieceIdx);

                var whiteWeights = (Vector512<short>*)(FeatureWeights + wIdx);
                var blackWeights = (Vector512<short>*)(FeatureWeights + bIdx);

                for (int i = 0; i < SIMD_CHUNKS_512; i++)
                {
                    w[i] = Vector512.Add(w[i], whiteWeights[i]);
                    b[i] = Vector512.Add(b[i], blackWeights[i]);
                }
            }
        }

        public static int GetEvaluationFallback(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            Vector256<short> maxVec = Vector256.Create((short)QA);
            Vector256<short> zeroVec = Vector256<short>.Zero;
            Vector256<int> sum = Vector256<int>.Zero;

            int outputBucket = SelectOutputBucket ? (int)((popcount(pos.bb.Occupancy) - 2) / 4) : 0;
            var ourData =   (accumulator[pos.ToMove]);
            var theirData = (accumulator[Not(pos.ToMove)]);
            var ourWeights =   (Vector256<short>*)(LayerWeights + (outputBucket * (HiddenSize * 2)));
            var theirWeights = (Vector256<short>*)(LayerWeights + (outputBucket * (HiddenSize * 2)) + HiddenSize);

            for (int i = 0; i < SIMD_CHUNKS_256; i++)
            {
                Vector256<short> clamp = Vector256.Min(maxVec, Vector256.Max(zeroVec, ourData[i]));
                Vector256<short> mult = clamp * ourWeights[i];

                (var loMult, var hiMult) = Vector256.Widen(mult);
                (var loClamp, var hiClamp) = Vector256.Widen(clamp);

                sum = Vector256.Add(sum, Vector256.Add(loMult * loClamp, hiMult * hiClamp));
            }

            for (int i = 0; i < SIMD_CHUNKS_256; i++)
            {
                Vector256<short> clamp = Vector256.Min(maxVec, Vector256.Max(zeroVec, theirData[i]));
                Vector256<short> mult = clamp * theirWeights[i];

                (var loMult, var hiMult) = Vector256.Widen(mult);
                (var loClamp, var hiClamp) = Vector256.Widen(clamp);

                sum = Vector256.Add(sum, Vector256.Add(loMult * loClamp, hiMult * hiClamp));
            }

            int output = Vector256.Sum(sum);

            return (output / QA + LayerBiases[outputBucket]) * OutputScale / QAB;
        }


        private static int FeatureIndex(int pc, int pt, int sq, int perspective)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            return ((pc ^ perspective) * ColorStride + pt * PieceStride + (sq ^ perspective * 56)) * HiddenSize;
        }



        private static (int, int) FeatureIndex(int pc, int pt, int sq)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int whiteIndex = pc * ColorStride + pt * PieceStride + sq;
            int blackIndex = Not(pc) * ColorStride + pt * PieceStride + (sq ^ 56);

            return (whiteIndex * HiddenSize, blackIndex * HiddenSize);
        }


        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* src = pos.State->Accumulator;
            Accumulator* dst = pos.NextState->Accumulator;

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = pos.ToMove;
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            var srcWhite = (*src)[White];
            var srcBlack = (*src)[Black];

            var dstWhite = (*dst)[White];
            var dstBlack = (*dst)[Black];

            (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom);
            (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo);

            if (m.Castle)
            {
                int rookFrom = moveTo;
                int rookTo = m.CastlingRookSquare;

                (wTo, bTo) = FeatureIndex(us, ourPiece, m.CastlingKingSquare);

                (int wRookFrom, int bRookFrom) = FeatureIndex(us, Rook, rookFrom);
                (int wRookTo, int bRookTo) = FeatureIndex(us, Rook, rookTo);

                SubSubAddAdd((short*)srcWhite, (short*)dstWhite,
                             (FeatureWeights + wFrom),
                             (FeatureWeights + wRookFrom),
                             (FeatureWeights + wTo),
                             (FeatureWeights + wRookTo));

                SubSubAddAdd((short*)srcBlack, (short*)dstBlack,
                             (FeatureWeights + bFrom),
                             (FeatureWeights + bRookFrom),
                             (FeatureWeights + bTo),
                             (FeatureWeights + bRookTo));
            }
            else if (theirPiece != None)
            {
                (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo);

                SubSubAdd((short*)srcWhite, (short*)dstWhite,
                          (FeatureWeights + wFrom),
                          (FeatureWeights + wCap),
                          (FeatureWeights + wTo));

                SubSubAdd((short*)srcBlack, (short*)dstBlack,
                          (FeatureWeights + bFrom),
                          (FeatureWeights + bCap),
                          (FeatureWeights + bTo));
            }
            else if (m.EnPassant)
            {
                int idxPawn = moveTo - ShiftUpDir(us);

                (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn);

                SubSubAdd((short*)srcWhite, (short*)dstWhite,
                          (FeatureWeights + wFrom),
                          (FeatureWeights + wCap),
                          (FeatureWeights + wTo));

                SubSubAdd((short*)srcBlack, (short*)dstBlack,
                          (FeatureWeights + bFrom),
                          (FeatureWeights + bCap),
                          (FeatureWeights + bTo));
            }
            else
            {
                SubAdd((short*)srcWhite, (short*)dstWhite,
                       (FeatureWeights + wFrom),
                       (FeatureWeights + wTo));

                SubAdd((short*)srcBlack, (short*)dstBlack,
                       (FeatureWeights + bFrom),
                       (FeatureWeights + bTo));
            }
        }
    }
}
