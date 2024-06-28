
#define USE_AVX2
//#undef USE_AVX2

using System.Reflection;
using System.Runtime.Intrinsics;

using Lizard.Logic.Threads;

using static Lizard.Logic.NN.NNUE;
using static Lizard.Logic.NN.Aliases;
using static Lizard.Logic.NN.FunUnrollThings;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {
        public const int INPUT_BUCKETS = 5;
        public const int INPUT_SIZE = 768;
        public const int L1_SIZE = 1280;
        public const int L2_SIZE = 16;
        public const int L3_SIZE = 32;
        public const int OUTPUT_BUCKETS = 8;

        private const int FT_QUANT = 512;
        private const int L1_QUANT = 256;

        public const int OutputScale = 400;

        public static readonly int SIMD_CHUNKS_512 = L1_SIZE / Vector512<short>.Count;
        public static readonly int SIMD_CHUNKS_256 = L1_SIZE / Vector256<short>.Count;

#if USE_AVX2
        public static readonly int L1_CHUNK_SIZE = Vector256<short>.Count;
        public static readonly int L2_CHUNK_SIZE = Vector256<float>.Count;
        public static readonly int L3_CHUNK_SIZE = Vector256<float>.Count;
#else
        public static readonly int L1_CHUNK_SIZE = 1;
        public static readonly int L2_CHUNK_SIZE = 1;
        public static readonly int L3_CHUNK_SIZE = 1;
#endif

        public const string NetworkName = "morelayers_1280x5_16_32_8-475-params.bin";

        private static readonly UQNetContainer UQNet;
        public static readonly NetContainer<short, float> Net;

        public const int N_FTW = INPUT_SIZE * L1_SIZE * INPUT_BUCKETS;
        public const int N_FTB = L1_SIZE;

        public const int N_L1W = OUTPUT_BUCKETS * L1_SIZE * L2_SIZE * 2;
        public const int N_L1B = OUTPUT_BUCKETS * L2_SIZE;

        public const int N_L2W = OUTPUT_BUCKETS * L2_SIZE * L3_SIZE;
        public const int N_L2B = OUTPUT_BUCKETS * L3_SIZE;

        public const int N_L3W = OUTPUT_BUCKETS * L3_SIZE;
        public const int N_L3B = OUTPUT_BUCKETS;

        public static long ExpectedNetworkSize => (N_FTW + N_FTB + N_L1W) * sizeof(short) +
                                                  (N_L1B + N_L3W + N_L3B) * sizeof(float) +
                                                          (N_L3W + N_L3B) * sizeof(float);

        private static ReadOnlySpan<int> KingBuckets =>
        [
            0, 0, 1, 1, 6, 6, 5, 5,
            2, 2, 3, 3, 8, 8, 7, 7,
            4, 4, 4, 4, 9, 9, 9, 9,
            4, 4, 4, 4, 9, 9, 9, 9,
            4, 4, 4, 4, 9, 9, 9, 9,
            4, 4, 4, 4, 9, 9, 9, 9,
            4, 4, 4, 4, 9, 9, 9, 9,
            4, 4, 4, 4, 9, 9, 9, 9,
        ];

        public static int BucketForPerspective(int ksq, int perspective) => (KingBuckets[perspective == Black ? (ksq ^ 56) : ksq]);

        static Bucketed768()
        {
            UQNet = new UQNetContainer();
            Net = new NetContainer<short, float>();

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
                Console.WriteLine("Bucketed768's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine($"It expects to read {toRead} bytes, but the stream's position is {stream.Position} / {stream.Length}");
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

            //UQNetContainer UQNet = new UQNetContainer();

            for (int i = 0; i < N_FTW; i++)
            {
                UQNet.FTWeights[i] = br.ReadSingle();
                Net.FTWeights[i] = (short)MathF.Round(UQNet.FTWeights[i] * FT_QUANT);
            }

            for (int i = 0; i < N_FTB; i++)
            {
                UQNet.FTBiases[i] = br.ReadSingle();
                Net.FTBiases[i] = (short)MathF.Round(UQNet.FTBiases[i] * FT_QUANT);
            }


            fixed (float* ptr = UQNet.L1Weights)
                for (int i = 0; i < N_L1W; i++)
                    ptr[i] = br.ReadSingle();

            fixed (float* ptr = UQNet.L1Biases)
                for (int i = 0; i < N_L1B; i++)
                    ptr[i] = br.ReadSingle();

            fixed (float* ptr = UQNet.L2Weights)
                for (int i = 0; i < N_L2W; i++)
                    ptr[i] = br.ReadSingle();

            fixed (float* ptr = UQNet.L2Biases)
                for (int i = 0; i < N_L2B; i++)
                    ptr[i] = br.ReadSingle();

            fixed (float* ptr = UQNet.L3Weights)
                for (int i = 0; i < N_L3W; i++)
                    ptr[i] = br.ReadSingle();

            fixed (float* ptr = UQNet.L3Biases)
                for (int i = 0; i < N_L3B; i++)
                    ptr[i] = br.ReadSingle();


            for (int bucket = 0; bucket < OUTPUT_BUCKETS; bucket++)
            {
                for (int i = 0; i < 2 * L1_SIZE / L1_CHUNK_SIZE; ++i)
                    for (int j = 0; j < L2_SIZE; ++j)
                        for (int k = 0; k < L1_CHUNK_SIZE; ++k)
                            Net.L1Weights[bucket][i * L1_CHUNK_SIZE * L2_SIZE
                                                + j * L1_CHUNK_SIZE
                                                + k] = (short)(MathF.Round(UQNet.L1Weights[i * L1_CHUNK_SIZE + k, bucket, j] * L1_QUANT));

                for (int i = 0; i < L2_SIZE; ++i)
                    Net.L1Biases[bucket][i] = UQNet.L1Biases[bucket, i];

                for (int i = 0; i < L2_SIZE / L2_CHUNK_SIZE; ++i)
                    for (int j = 0; j < L3_SIZE; ++j)
                        for (int k = 0; k < L2_CHUNK_SIZE; ++k)
                            Net.L2Weights[bucket][i * L2_CHUNK_SIZE * L3_SIZE
                                                + j * L2_CHUNK_SIZE
                                                + k] = UQNet.L2Weights[i * L2_CHUNK_SIZE + k, bucket, j];

                for (int i = 0; i < L3_SIZE; ++i)
                    Net.L2Biases[bucket][i] = UQNet.L2Biases[bucket, i];

                for (int i = 0; i < L3_SIZE; ++i)
                    Net.L3Weights[bucket][i] = UQNet.L3Weights[i, bucket];

                Net.L3Biases[bucket] = UQNet.L3Biases[bucket];
            }



#if DEBUG
            NetStats("ft weight", Net.FTWeights, N_FTW);
            NetStats("ft bias\t", Net.FTBiases, N_FTB);

            NetStats("L1 weight", Net.L1Weights[0], N_L1W / OUTPUT_BUCKETS);
            NetStats("L1 bias\t", Net.L1Biases[0], N_L1B / OUTPUT_BUCKETS);

            NetStats("L2 weight", Net.L2Weights[0], N_L2W / OUTPUT_BUCKETS);
            NetStats("L2 bias\t", Net.L2Biases[0], N_L2B / OUTPUT_BUCKETS);

            NetStats("L3 weight", Net.L3Weights[0], N_L3W / OUTPUT_BUCKETS);
            NetStats("L3 bias\t", Net.L3Biases, N_L3B / OUTPUT_BUCKETS);
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
            Unsafe.CopyBlock(ourAccumulation, Net.FTBiases, sizeof(short) * L1_SIZE);
            accumulator.NeedsRefresh[perspective] = false;

            int ourKing = pos.State->KingSquares[perspective];
            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                int idx = FeatureIndexSingle(pc, pt, pieceIdx, ourKing, perspective);
                var ourWeights = (Vector512<short>*)(Net.FTWeights + idx);
                UnrollAdd(ourAccumulation, ourAccumulation, Net.FTWeights + idx);
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
                        UnrollAdd(ourAccumulation, ourAccumulation, Net.FTWeights + idx);
                    }

                    while (removed != 0)
                    {
                        int sq = poplsb(&removed);
                        int idx = FeatureIndexSingle(pc, pt, sq, ourKing, perspective);
                        UnrollSubtract(ourAccumulation, ourAccumulation, Net.FTWeights + idx);
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
                RefreshAccumulatorPerspective(pos, White);

            if (accumulator.NeedsRefresh[Black])
                RefreshAccumulatorPerspective(pos, Black);

            int* L1OutputsUs   = stackalloc int[L2_SIZE];
            int* L1OutputsThem = stackalloc int[L2_SIZE];
            float* L2Inputs  = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = Math.Min((63 - occ) * (32 - occ) / 225, 7);

            var ourData   = (short*)(accumulator[pos.ToMove]);
            var theirData = (short*)(accumulator[Not(pos.ToMove)]);

#if USE_AVX2
            ActivateFTAndAffineL1Sparse(ourData,   Net.L1Weights[outputBucket], L1OutputsUs);
            ActivateFTAndAffineL1Sparse(theirData, Net.L1Weights[outputBucket] + L1_SIZE * L2_SIZE, L1OutputsThem);
#else
            ActivateFTAndAffineL1Fallback(ourData,   Net.L1Weights[outputBucket], L1OutputsUs);
            ActivateFTAndAffineL1Fallback(theirData, Net.L1Weights[outputBucket] + L1_SIZE * L2_SIZE, L1OutputsThem);
#endif

            for (int i = 0; i < L2_SIZE; ++i)
            {
                L2Inputs[i] = ((float)L1OutputsUs[i] + L1OutputsThem[i]) / ((float)FT_QUANT * L1_QUANT) + Net.L1Biases[outputBucket][i];
            }

#if USE_AVX2
            ActivateL1AndAffineL2(L2Inputs,  Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL2AndAffineL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);
#else
            ActivateL1AndAffineL2Fallback(L2Inputs,  Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL2AndAffineL3Fallback(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);
#endif

            return (int)(L3Output * OutputScale);
        }


        public static void ActivateFTAndAffineL1Sparse(short* inputs, short* weights, int* output)
        {
            var sums = stackalloc Vector256<int>[L2_SIZE];
            var wVecs = (Vector256<short>*)(weights);
            var inputsVecs = (Vector256<short>*)(inputs);

            var zero = _mm256_setzero_epi16();
            var one = _mm256_set1_epi16(FT_QUANT);

            int* nnz_indices = stackalloc int[L1_SIZE / L1_CHUNK_SIZE];
            int total_nnz = 0;

            for (int i = 0; i < L1_SIZE / L1_CHUNK_SIZE; i++)
            {
                var chunk = inputsVecs[i];
                var cmpgt = _mm256_cmpgt_epi16(chunk, zero);
                if (_mm256_movemask_epi8(cmpgt.AsByte()) != 0)
                {
                    nnz_indices[total_nnz++] = i;
                }
            }

            for (int nnz = 0; nnz < total_nnz; ++nnz)
            {
                int i = nnz_indices[nnz];
                var row = &wVecs[i * L2_SIZE];
                var activated = _mm256_min_epi16(one, _mm256_max_epi16(inputsVecs[i], zero));

                for (int j = 0; j < L2_SIZE; j++)
                {
                    sums[j] = _mm256_add_epi32(sums[j], _mm256_madd_epi16(activated, row[j]));
                }
            }

            for (int i = 0; i < L2_SIZE / L2_CHUNK_SIZE; ++i)
            {
                var sum0123 = hadd_epi32x4(&sums[i * L2_CHUNK_SIZE]);
                var sum4567 = hadd_epi32x4(&sums[i * L2_CHUNK_SIZE + 4]);
                var sum = combine_m256i(sum0123, sum4567);
                _mm256_storeu_si256(output + i * L2_CHUNK_SIZE, sum);
            }
        }

        public static void ActivateL1AndAffineL2(float* inputs, float* weights, float* biases, float* output)
        {
            var sums = stackalloc Vector256<float>[L3_SIZE];
            var inputVecs = (Vector256<float>*)(inputs);
            var biasVecs = (Vector256<float>*)biases;
            var outputVecs = (Vector256<float>*)output;

            var zero = _mm256_setzero_ps();

            for (int i = 0; i < L2_SIZE / L2_CHUNK_SIZE; i++)
            {
                Vector256<float>* wVecs = (Vector256<float>*)(weights + i * L3_SIZE * L2_CHUNK_SIZE);
                var activated = _mm256_max_ps(inputVecs[i], zero);

                for (int j = 0; j < L3_SIZE; j++)
                {
                    sums[j] = _mm256_fmadd_ps(activated, wVecs[j], sums[j]);
                }
            }

            for (int i = 0; i < L3_SIZE / L3_CHUNK_SIZE; i++)
            {
                var sum0123 = hadd_psx4(&sums[i * L3_CHUNK_SIZE]);
                var sum4567 = hadd_psx4(&sums[i * L3_CHUNK_SIZE + 4]);
                outputVecs[i] = _mm256_add_ps(combine_m256(sum0123, sum4567), biasVecs[i]);
            }
        }

        public static void ActivateL2AndAffineL3(float* inputs, float* weights, float bias, ref float output)
        {
            var inputVecs = (Vector256<float>*)(inputs);
            var wVecs = (Vector256<float>*)(weights);

            var sumVec = _mm256_setzero_ps();
            var zero = _mm256_setzero_ps();

            for (int i = 0; i < L3_SIZE / L3_CHUNK_SIZE; ++i)
            {
                sumVec = _mm256_fmadd_ps(_mm256_max_ps(inputVecs[i], zero), wVecs[i], sumVec);
            }

            output = _mm256_reduce_add_ps(sumVec) + bias;
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

            return ((768 * KingBuckets[kingSq]) + ((pc ^ perspective) * ColorStride) + (pt * PieceStride) + (sq)) * L1_SIZE;
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

            return (whiteIndex * L1_SIZE, blackIndex * L1_SIZE);
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

            var FTWeights = Net.FTWeights;

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
                              (FTWeights + from),
                              (FTWeights + cap),
                              (FTWeights + to));
                }
                else if (m.Castle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare;

                    to = FeatureIndexSingle(us, ourPiece, m.CastlingKingSquare, theirKing, them);

                    int rookFrom = FeatureIndexSingle(us, Rook, rookFromSq, theirKing, them);
                    int rookTo = FeatureIndexSingle(us, Rook, rookToSq, theirKing, them);

                    SubSubAddAdd((short*)theirSrc, (short*)theirDst,
                                 (FTWeights + from),
                                 (FTWeights + rookFrom),
                                 (FTWeights + to),
                                 (FTWeights + rookTo));
                }
                else
                {
                    SubAdd((short*)theirSrc, (short*)theirDst,
                           (FTWeights + from),
                           (FTWeights + to));
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
                              (FTWeights + wFrom),
                              (FTWeights + wCap),
                              (FTWeights + wTo));

                    SubSubAdd((short*)srcBlack, (short*)dstBlack,
                              (FTWeights + bFrom),
                              (FTWeights + bCap),
                              (FTWeights + bTo));
                }
                else if (m.EnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    SubSubAdd((short*)srcWhite, (short*)dstWhite,
                              (FTWeights + wFrom),
                              (FTWeights + wCap),
                              (FTWeights + wTo));

                    SubSubAdd((short*)srcBlack, (short*)dstBlack,
                              (FTWeights + bFrom),
                              (FTWeights + bCap),
                              (FTWeights + bTo));
                }
                else
                {
                    SubAdd((short*)srcWhite, (short*)dstWhite,
                           (FTWeights + wFrom),
                           (FTWeights + wTo));

                    SubAdd((short*)srcBlack, (short*)dstBlack,
                           (FTWeights + bFrom),
                           (FTWeights + bTo));
                }
            }
        }

        public static void ResetCaches(SearchThread td)
        {
            for (int bIdx = 0; bIdx < td.CachedBuckets.Length; bIdx++)
            {
                ref BucketCache bc = ref td.CachedBuckets[bIdx];
                bc.Accumulator.ResetWithBiases(Net.FTBiases, sizeof(short) * L1_SIZE);
                bc.Boards[White].Reset();
                bc.Boards[Black].Reset();
            }
        }


        public static void ActivateFTAndAffineL1Fallback(short* inputs, short* weights, int* output)
        {
            int* sums = stackalloc int[L2_SIZE];

            for (int i = 0; i < L1_SIZE; ++i)
            {
                for (int j = 0; j < L2_SIZE; ++j)
                {
                    sums[j] += Math.Max((int)inputs[i], 0) * weights[i * L2_SIZE + j];
                }
            }

            for (int i = 0; i < L2_SIZE; ++i)
            {
                output[i] = sums[i];
            }
        }

        public static void ActivateL1AndAffineL2Fallback(float* inputs, float* weights, float* biases, float* output)
        {
            float* sums = stackalloc float[L3_SIZE];

            for (int i = 0; i < L2_SIZE; ++i)
            {
                for (int j = 0; j < L3_SIZE; ++j)
                {
                    sums[j] += Math.Max(inputs[i], 0.0f) * weights[i * L3_SIZE + j];
                }
            }

            for (int i = 0; i < L3_SIZE; ++i)
            {
                output[i] = sums[i] + biases[i];
            }
        }

        public static void ActivateL2AndAffineL3Fallback(float* inputs, float* weights, float bias, ref float output)
        {
            float sum = 0.0f;

            for (int i = 0; i < L3_SIZE; ++i)
            {
                sum += Math.Max(inputs[i], 0.0f) * weights[i];
            }

            output = sum + bias;
        }


        public static float _mm256_reduce_add_ps(Vector256<float> sum)
        {
            var upper_128 = _mm256_extractf128_ps(sum, 1);
            var lower_128 = _mm256_castps256_ps128(sum);
            var sum_128 = _mm_add_ps(upper_128, lower_128);

            var upper_64 = _mm_movehl_ps(sum_128, sum_128);
            var sum_64 = _mm_add_ps(upper_64, sum_128);

            var upper_32 = _mm_shuffle_ps(sum_64, sum_64, 1);
            var sum_32 = _mm_add_ss(upper_32, sum_64);

            return _mm_cvtss_f32(sum_32);
        }

        public static Vector256<int> combine_m256i(Vector256<int> in0, Vector256<int> in1)
        {
            var in0_low = _mm256_castsi256_si128(in0);
            var in0_hi = _mm256_extracti128_si256(in0, 1);
            var in0_m128 = _mm_add_epi32(in0_low, in0_hi);

            var in1_low = _mm256_castsi256_si128(in1);
            var in1_hi = _mm256_extracti128_si256(in1, 1);
            var in1_m128 = _mm_add_epi32(in1_low, in1_hi);

            return _mm256_inserti128_si256(_mm256_castsi128_si256(in0_m128), in1_m128, 1);
        }

        public static Vector256<int> hadd_epi32x4(Vector256<int>* inp)
        {
            var sum01 = _mm256_hadd_epi32(inp[0], inp[1]);
            var sum23 = _mm256_hadd_epi32(inp[2], inp[3]);
            return _mm256_hadd_epi32(sum01, sum23);
        }

        public static Vector256<float> combine_m256(Vector256<float> in0, Vector256<float> in1)
        {
            var in0_low = _mm256_castps256_ps128(in0);
            var in0_hi = _mm256_extractf128_ps(in0, 1);
            var in0_m128 = _mm_add_ps(in0_low, in0_hi);

            var in1_low = _mm256_castps256_ps128(in1);
            var in1_hi = _mm256_extractf128_ps(in1, 1);
            var in1_m128 = _mm_add_ps(in1_low, in1_hi);

            return _mm256_insertf128_ps(_mm256_castps128_ps256(in0_m128), in1_m128, 1);
        }

        public static Vector256<float> hadd_psx4(Vector256<float>* inp)
        {
            var sum01 = _mm256_hadd_ps(inp[0], inp[1]);
            var sum23 = _mm256_hadd_ps(inp[2], inp[3]);
            return _mm256_hadd_ps(sum01, sum23);
        }

        //  https://github.com/official-stockfish/nnue-pytorch/blob/master/docs/nnue.md#m256_process_chunk
        public static void m256_process_chunk(ref Vector256<int> sum0, ref Vector256<int> sum1, Vector256<short> col0, Vector256<short> col1, Vector256<short> factor)
        {
            // We interleave the two columns, because madd adds adjacent values.
            // This way we effectively add the results from both columns.
            sum0 = _mm256_add_epi32(
                sum0, _mm256_madd_epi16(factor, _mm256_unpacklo_epi16(col0, col1))
            );
            sum1 = _mm256_add_epi32(
                sum1, _mm256_madd_epi16(factor, _mm256_unpackhi_epi16(col0, col1))
            );
        }
    }
}