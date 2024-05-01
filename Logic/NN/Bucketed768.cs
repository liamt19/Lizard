using System.Reflection;
using System.Runtime.Intrinsics;

using Lizard.Logic.Threads;

using static Lizard.Logic.NN.FunUnrollThings;
using static Lizard.Logic.NN.NNUE;

namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe partial class Bucketed768
    {
        public const int InputBuckets = 4;
        public const int InputSize = 768;
        public const int HiddenSize = 1024;
        public const int OutputBuckets = 8;

        public const int QA = 255;
        public const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;
        private const bool SelectOutputBucket = (OutputBuckets != 1);

        public static readonly int SIMD_CHUNKS_512 = HiddenSize / Vector512<short>.Count;
        public static readonly int SIMD_CHUNKS_256 = HiddenSize / Vector256<short>.Count;

        /// <summary>
        /// 
        /// (768x4 -> 1024)x2 -> 8
        /// 
        /// </summary>
        public const string NetworkName = "lizard-1024_4_8_gauss-600.bin";


        public static readonly short* FeatureWeights;
        public static readonly short* FeatureBiases;
        public static readonly short* LayerWeights;
        public static readonly short* LayerBiases;

        private const int FeatureWeightElements = InputSize * HiddenSize * InputBuckets;
        private const int FeatureBiasElements = HiddenSize;

        private const int LayerWeightElements = HiddenSize * 2 * OutputBuckets;
        private const int LayerBiasElements = OutputBuckets;

        public static long ExpectedNetworkSize => (FeatureWeightElements + FeatureBiasElements + LayerWeightElements + LayerBiasElements) * sizeof(short);

        private static ReadOnlySpan<int> KingBuckets =>
        [
            0, 0, 1, 1, 5, 5, 4, 4,
            2, 2, 2, 2, 6, 6, 6, 6,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
        ];

        public static int BucketForPerspective(int ksq, int perspective) => (KingBuckets[perspective == Black ? (ksq ^ 56) : ksq]);

        static Bucketed768()
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

            //  These weights are stored in column major order, but they are easier to use in row major order.
            //  The first 8 weights in the binary file are actually the first weight for each of the 8 output buckets,
            //  so we will transpose them so that the all of the weights for each output bucket are contiguous.
            TransposeLayerWeights((short*)LayerWeights, HiddenSize * 2, OutputBuckets);

#if DEBUG
            NetStats("ft weight", FeatureWeights, FeatureWeightElements);
            NetStats("ft bias\t", FeatureBiases, FeatureBiasElements);

            NetStats("fc weight", LayerWeights, LayerWeightElements);
            NetStats("fc bias", LayerBiases, LayerBiasElements);

            Log("Init Simple768 done");
#endif
        }

        public static void RefreshAccumulator(Position pos)
        {
            RefreshAccumulatorPerspectiveFull(pos, White);
            RefreshAccumulatorPerspectiveFull(pos, Black);
        }

        public static void RefreshAccumulatorPerspectiveFull(Position pos, int perspective)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var ourAccumulation = (short*)accumulator[perspective];
            Unsafe.CopyBlock(ourAccumulation, FeatureBiases, sizeof(short) * HiddenSize);
            accumulator.NeedsRefresh[perspective] = false;

            int ourKing = pos.State->KingSquares[perspective];
            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                int idx = FeatureIndexSingle(pc, pt, pieceIdx, ourKing, perspective);
                var ourWeights = (Vector512<short>*)(FeatureWeights + idx);
                UnrollAdd(ourAccumulation, ourAccumulation, FeatureWeights + idx);
            }

            if (pos.Owner.CachedBuckets == null)
            {
                //  TODO: Upon SearchThread init, this isn't created yet :(
                return;
            }

            ref BucketCache cache = ref pos.Owner.CachedBuckets[BucketForPerspective(ourKing, perspective)];
            ref Bitboard entryBB = ref cache.Boards[perspective];
            ref Accumulator entryAcc = ref cache.Accumulator;

            accumulator.CopyTo(ref entryAcc, perspective);
            bb.CopyTo(ref entryBB);
        }


        public static void RefreshAccumulatorPerspective(Position pos, int perspective)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            int ourKing = pos.State->KingSquares[perspective];
            int thisBucket = KingBuckets[ourKing];

            ref BucketCache rtEntry = ref pos.Owner.CachedBuckets[BucketForPerspective(ourKing, perspective)];
            ref Bitboard entryBB = ref rtEntry.Boards[perspective];
            ref Accumulator entryAcc = ref rtEntry.Accumulator;

            var ourAccumulation = (short*)entryAcc[perspective];
            accumulator.NeedsRefresh[perspective] = false;

            for (int pc = 0; pc < ColorNB; pc++)
            {
                for (int pt = 0; pt < PieceNB; pt++)
                {
                    ulong prev = entryBB.Pieces[pt] & entryBB.Colors[pc];
                    ulong curr =      bb.Pieces[pt] &      bb.Colors[pc];

                    ulong added   = curr & ~prev;
                    ulong removed = prev & ~curr;

                    while (added != 0)
                    {
                        int sq = poplsb(&added);
                        int idx = FeatureIndexSingle(pc, pt, sq, ourKing, perspective);
                        UnrollAdd(ourAccumulation, ourAccumulation, FeatureWeights + idx);
                    }

                    while (removed != 0)
                    {
                        int sq = poplsb(&removed);
                        int idx = FeatureIndexSingle(pc, pt, sq, ourKing, perspective);
                        UnrollSubtract(ourAccumulation, ourAccumulation, FeatureWeights + idx);
                    }
                }
            }

            entryAcc.CopyTo(ref accumulator, perspective);
            bb.CopyTo(ref entryBB);
        }

        public static int GetEvaluation(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

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

            return ((768 * KingBuckets[kingSq]) + ((pc ^ perspective) * ColorStride) + (pt * PieceStride) + (sq)) * HiddenSize;
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

            return (whiteIndex * HiddenSize, blackIndex * HiddenSize);
        }

        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* src = pos.State->Accumulator;
            Accumulator* dst = pos.NextState->Accumulator;

            dst->NeedsRefresh[0] = src->NeedsRefresh[0];
            dst->NeedsRefresh[1] = src->NeedsRefresh[1];

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

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                dst->NeedsRefresh[us] = true;

                var theirSrc = (*src)[them];
                var theirDst = (*dst)[them];
                int theirKing = pos.State->KingSquares[them];

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None && !m.Castle)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);

                    SubSubAdd((short*)theirSrc, (short*)theirDst,
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

                    SubSubAddAdd((short*)theirSrc, (short*)theirDst,
                                 (FeatureWeights + from),
                                 (FeatureWeights + rookFrom),
                                 (FeatureWeights + to),
                                 (FeatureWeights + rookTo));
                }
                else
                {
                    SubAdd((short*)theirSrc, (short*)theirDst,
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

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

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

        public static void ResetCaches(SearchThread td)
        {
            for (int bIdx = 0; bIdx < td.CachedBuckets.Length; bIdx++)
            {
                ref BucketCache bc = ref td.CachedBuckets[bIdx];
                bc.Accumulator.ResetWithBiases(FeatureBiases, sizeof(short) * HiddenSize);
                bc.Boards[White].Reset();
                bc.Boards[Black].Reset();
            }
        }
    }
}