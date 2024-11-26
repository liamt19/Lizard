
//#define NO_PERM
//#define PERM_COUNT

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Lizard.Logic.Threads;

using static Lizard.Logic.NN.Aliases;
using static Lizard.Logic.NN.FunUnrollThings;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {
        public const int INPUT_BUCKETS = 13;
        public const int INPUT_SIZE = 768;
        public const int L1_SIZE = 1280;
        public const int L2_SIZE = 16;
        public const int L3_SIZE = 32;
        public const int OUTPUT_BUCKETS = 8;

        private const int FT_QUANT = 255;
        private const int FT_SHIFT = 10;
        private const int L1_QUANT = 64;
        private const int OutputScale = 400;

        private static readonly int U8_CHUNK_SIZE = sizeof(Vector256<byte>) / sizeof(byte);
        private static readonly int I16_CHUNK_SIZE = sizeof(Vector256<short>) / sizeof(short);
        private static readonly int I32_CHUNK_SIZE = sizeof(Vector256<int>) / sizeof(int);
        private static readonly int F32_CHUNK_SIZE = sizeof(Vector256<float>) / sizeof(float);

        private static readonly int NNZ_INPUT_SIMD_WIDTH = sizeof(Vector256<int>) / sizeof(int);
        private static readonly int NNZ_CHUNK_SIZE = Math.Max(NNZ_INPUT_SIMD_WIDTH, 8);
        private static readonly int NNZ_OUTPUTS_PER_CHUNK = NNZ_CHUNK_SIZE / 8;

        private const int L1_CHUNK_PER_32 = sizeof(int) / sizeof(sbyte);
        private const int L1_PAIR_COUNT = L1_SIZE / 2;


        public static string NetworkName
        {
            get
            {
                try
                {
                    return Assembly.GetEntryAssembly().GetCustomAttribute<EvalFileAttribute>().EvalFile.Trim();
                }
                catch { return ""; }
            }
        }

        private static readonly NetContainer<short, sbyte, float> Net;
        private static readonly Vector128<ushort>* NNZLookup;

        private const int N_FTW = INPUT_SIZE * L1_SIZE * INPUT_BUCKETS;
        private const int N_FTB = L1_SIZE;

        private const int N_L1W = OUTPUT_BUCKETS * L1_SIZE * L2_SIZE;
        private const int N_L1B = OUTPUT_BUCKETS * L2_SIZE;

        private const int N_L2W = OUTPUT_BUCKETS * L2_SIZE * L3_SIZE;
        private const int N_L2B = OUTPUT_BUCKETS * L3_SIZE;

        private const int N_L3W = OUTPUT_BUCKETS * L3_SIZE;
        private const int N_L3B = OUTPUT_BUCKETS;

        private static long ExpectedNetworkSize => (N_FTW + N_FTB) * sizeof(short) +
                                                           (N_L1W) * sizeof(byte)  + 
                                           (N_L1B + N_L2W + N_L2B) * sizeof(float) +
                                                   (N_L3W + N_L3B) * sizeof(float);

        private static ReadOnlySpan<int> KingBuckets =>
        [
             0,  1,  2,  3, 16, 15, 14, 13,
             4,  5,  6,  7, 20, 19, 18, 17,
             8,  9, 10, 11, 24, 23, 22, 21,
             8,  9, 10, 11, 24, 23, 22, 21,
            12, 12, 12, 12, 25, 25, 25, 25,
            12, 12, 12, 12, 25, 25, 25, 25,
            12, 12, 12, 12, 25, 25, 25, 25,
            12, 12, 12, 12, 25, 25, 25, 25,
        ];

        public static int BucketForPerspective(int ksq, int perspective) => (KingBuckets[perspective == Black ? (ksq ^ 56) : ksq]);


#if PERM_COUNT
        public static ulong ActivationCount = 0;
        public static ulong EvalCalls = 0;
        public static readonly ulong[] NNZCounts = new ulong[L1_SIZE];
#endif


        static Bucketed768()
        {
            Net = new NetContainer<short, sbyte, float>();
            NNZLookup = AlignedAllocZeroed<Vector128<ushort>>(256);
            SetupNNZ();

#if NO_PERM
            PermuteIndices = Enumerable.Range(0, L1_PAIR_COUNT).ToArray();
#else
            PermuteIndices = BestIndices.ToArray();
#endif

            Initialize(NetworkName);
        }

        public static void Initialize(string networkToLoad, bool exitIfFail = true)
        {
            using Stream netStream = NNUE.TryOpenFile(networkToLoad, exitIfFail);

            BinaryReader br;

            if (Zstd.IsCompressed(netStream))
            {
                byte[] buff = new byte[ExpectedNetworkSize + 64];
                MemoryStream memStream = Zstd.Decompress(netStream, buff);
                br = new BinaryReader(memStream);
            }
            else
            {
                br = new BinaryReader(netStream);
            }

            long toRead = ExpectedNetworkSize;
            if (br.BaseStream.Position + toRead > br.BaseStream.Length)
            {
                Console.WriteLine("Bucketed768's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine($"It expects to read {toRead} bytes, but the stream's position is {br.BaseStream.Position} / {br.BaseStream.Length}");
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

            UQNetContainer UQNet = new UQNetContainer();

            for (int i = 0; i < N_FTW; i++)
            {
                UQNet.FTWeights[i] = br.ReadInt16();
                Net.FTWeights[i] = UQNet.FTWeights[i];
            }

            for (int i = 0; i < N_FTB; i++)
            {
                UQNet.FTBiases[i] = br.ReadInt16();
                Net.FTBiases[i] = UQNet.FTBiases[i];
            }

            PermuteFT(new Span<short>(Net.FTWeights, N_FTW), new Span<short>(Net.FTBiases, N_FTB));

            fixed (sbyte* ptr = UQNet.L1Weights)
                for (int i = 0; i < N_L1W; i++)
                    ptr[i] = br.ReadSByte();

            PermuteL1(UQNet.L1Weights);

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
                for (int i = 0; i < L1_SIZE / L1_CHUNK_PER_32; ++i)
                    for (int j = 0; j < L2_SIZE; ++j)
                        for (int k = 0; k < L1_CHUNK_PER_32; ++k)
                            Net.L1Weights[bucket][i * L1_CHUNK_PER_32 * L2_SIZE
                                                + j * L1_CHUNK_PER_32
                                                + k] = UQNet.L1Weights[i * L1_CHUNK_PER_32 + k, bucket, j];

                for (int i = 0; i < L2_SIZE; ++i)
                    Net.L1Biases[bucket][i] = UQNet.L1Biases[bucket, i];

                for (int i = 0; i < L2_SIZE; ++i)
                    for (int j = 0; j < L3_SIZE; ++j)
                        Net.L2Weights[bucket][i * L3_SIZE + j] = UQNet.L2Weights[i, bucket, j];

                for (int i = 0; i < L3_SIZE; ++i)
                    Net.L2Biases[bucket][i] = UQNet.L2Biases[bucket, i];

                for (int i = 0; i < L3_SIZE; ++i)
                    Net.L3Weights[bucket][i] = UQNet.L3Weights[i, bucket];

                Net.L3Biases[bucket] = UQNet.L3Biases[bucket];
            }

            const int numRegi = 4;
            int numChunks = sizeof(Vector128<short>) / sizeof(short);
            Span<int> order = [0, 2, 1, 3];
            Vector128<short>[] regi = new Vector128<short>[numRegi];
            var ws = (Vector128<short>*)Net.FTWeights;
            var bs = (Vector128<short>*)Net.FTBiases;

            for (int i = 0; i < INPUT_SIZE * L1_SIZE * INPUT_BUCKETS / numChunks; i += numRegi)
            {
                for (int j = 0; j < numRegi; j++)
                    regi[j] = ws[i + j];

                for (int j = 0; j < numRegi; j++)
                    ws[i + j] = regi[order[j]];
            }

            for (int i = 0; i < L1_SIZE / numChunks; i += numRegi)
            {
                for (int j = 0; j < numRegi; j++)
                    regi[j] = bs[i + j];

                for (int j = 0; j < numRegi; j++)
                    bs[i + j] = regi[order[j]];
            }
        }

        private static void SetupNNZ()
        {
            ushort[] temp = new ushort[8];
            for (int i = 0; i < 256; i++)
            {
                Array.Clear(temp);
                int j = i;
                int k = 0;
                while (j != 0)
                {
                    uint lsbIndex = uint.TrailingZeroCount((uint)j);
                    j &= j - 1;
                    temp[k] = (ushort)lsbIndex;
                    k++;
                }

                NNZLookup[i] = Vector128.Create(temp);
            }
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
            accumulator.Computed[perspective] = true;

            int ourKing = pos.State->KingSquares[perspective];
            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                int idx = FeatureIndexSingle(pc, pt, pieceIdx, ourKing, perspective);
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

            accumulator.Computed[perspective] = true;
        }


        public static int GetEvaluation(Position pos, int outputBucket)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            Bucketed768.ProcessUpdates(pos);

            float* L1Outputs = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            var us   = (short*)(accumulator[pos.ToMove]);
            var them = (short*)(accumulator[Not(pos.ToMove)]);

            ActivateFTSparse(us, them, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs);
            ActivateL2(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);

            return (int)(L3Output * OutputScale);
        }

        public static int GetEvaluation(Position pos)
        {
            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = (occ - 2) / ((32 + OUTPUT_BUCKETS - 1) / OUTPUT_BUCKETS);

            return GetEvaluation(pos, outputBucket);
        }


        private static void ActivateFTSparse(short* us, short* them, sbyte* weights, float* biases, float* output)
        {
            var ft_zero = _mm256_setzero_epi16();
            var ft_one = _mm256_set1_epi16(FT_QUANT);

            int nnzCount = 0;
            int offset = 0;

            sbyte* ft_outputs = stackalloc sbyte[L1_SIZE];
            ushort* nnzIndices = stackalloc ushort[L1_SIZE / L1_CHUNK_PER_32];

            Vector128<ushort> baseInc = Vector128.Create((ushort)8);
            Vector128<ushort> baseVec = Vector128<ushort>.Zero;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                short* acc = perspective == 0 ? us : them;

                for (int i = 0; i < L1_PAIR_COUNT; i += (I16_CHUNK_SIZE * 2))
                {
                    var input0a = _mm256_load_si256(&acc[i + 0 * I16_CHUNK_SIZE + 0]);
                    var input0b = _mm256_load_si256(&acc[i + 1 * I16_CHUNK_SIZE + 0]);

                    var input1a = _mm256_load_si256(&acc[i + 0 * I16_CHUNK_SIZE + L1_PAIR_COUNT]);
                    var input1b = _mm256_load_si256(&acc[i + 1 * I16_CHUNK_SIZE + L1_PAIR_COUNT]);

                    var clipped0a = _mm256_min_epi16(_mm256_max_epi16(input0a, ft_zero), ft_one);
                    var clipped0b = _mm256_min_epi16(_mm256_max_epi16(input0b, ft_zero), ft_one);

                    var clipped1a = _mm256_min_epi16(input1a, ft_one);
                    var clipped1b = _mm256_min_epi16(input1b, ft_one);

                    var producta = _mm256_mulhi_epi16(_mm256_slli_epi16(clipped0a, 16 - FT_SHIFT), clipped1a);
                    var productb = _mm256_mulhi_epi16(_mm256_slli_epi16(clipped0b, 16 - FT_SHIFT), clipped1b);

                    var product_one = _mm256_packus_epi16(producta, productb).AsByte();
                    _mm256_storeu_si256(&ft_outputs[offset + i], product_one.AsSByte());

                    var nnz_mask = vec_nnz_mask(product_one);

                    for (int j = 0; j < NNZ_OUTPUTS_PER_CHUNK; j++)
                    {
                        int lookup = (nnz_mask >> (j * 8)) & 0xFF;
                        var offsets = NNZLookup[lookup];
                        _mm128_storeu_si128(&nnzIndices[nnzCount], _mm_add_epi16(baseVec, offsets));

                        nnzCount += int.PopCount(lookup);
                        baseVec += baseInc;
                    }

                }

                offset += L1_PAIR_COUNT;
            }

#if PERM_COUNT
            EvalCalls++;
            ActivationCount += (ulong)nnzCount;
            lock (NNZCounts)
            {
                for (int i = 0; i < L1_SIZE; i++)
                    NNZCounts[i] += (ft_outputs[i] != 0) ? 1UL : 0;
            }
#endif

            ActivateL1Sparse(ft_outputs, weights, biases, output, new Span<ushort>(nnzIndices, nnzCount));
        }


        private static void ActivateL1Sparse(sbyte* inputs, sbyte* weights, float* biases, float* output, Span<ushort> nnzIndices)
        {
            var sums = stackalloc Vector256<int>[L2_SIZE / I32_CHUNK_SIZE];

            int nnzCount = nnzIndices.Length;
            int* inputs32 = (int*)(inputs);
            for (int i = 0; i < nnzCount; i++)
            {
                var index = nnzIndices[i];
                var input32 = _mm256_set1_epi32(inputs32[index]);
                var weight = (Vector256<sbyte>*)(&weights[index * L1_CHUNK_PER_32 * L2_SIZE]);
                for (int k = 0; k < L2_SIZE / F32_CHUNK_SIZE; k++)
                {
                    sums[k] = vec_dpbusd_epi32(sums[k], input32.AsByte(), weight[k]);
                }
            }

            var zero = _mm256_set1_ps(0.0f);
            var one = Vector256<float>.One;

            var sumMul = _mm256_set1_ps((1 << FT_SHIFT) / (float)(FT_QUANT * FT_QUANT * L1_QUANT));
            for (int i = 0; i < L2_SIZE / F32_CHUNK_SIZE; ++i)
            {
                var biasVec = _mm256_loadu_ps(&biases[i * F32_CHUNK_SIZE]);
                var sumPs = _mm256_fmadd_ps(_mm256_cvtepi32_ps(sums[i]), sumMul, biasVec);
                var clipped = _mm256_min_ps(_mm256_max_ps(sumPs, zero), one);
                var squared = _mm256_mul_ps(clipped, clipped);
                _mm256_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);

            }
        }


        private static void ActivateL2(float* inputs, float* weights, float* biases, float* output)
        {
            var sumVecs = stackalloc Vector256<float>[L3_SIZE / F32_CHUNK_SIZE];

            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
                sumVecs[i] = _mm256_loadu_ps(&biases[i * F32_CHUNK_SIZE]);

            for (int i = 0; i < L2_SIZE; ++i)
            {
                var inputVec = _mm256_set1_ps(inputs[i]);
                var weight = (Vector256<float>*)(&weights[i * L3_SIZE]);
                for (int j = 0; j < L3_SIZE / F32_CHUNK_SIZE; ++j)
                {
                    sumVecs[j] = vec_mul_add_ps(inputVec, weight[j], sumVecs[j]);
                }
            }

            var zero = _mm256_set1_ps(0.0f);
            var one = _mm256_set1_ps(1.0f);
            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; ++i)
            {
                var clipped = _mm256_min_ps(_mm256_max_ps(sumVecs[i], zero), one);
                var squared = _mm256_mul_ps(clipped, clipped);
                _mm256_storeu_ps(&output[i * F32_CHUNK_SIZE], squared);
            }
        }


        private static void ActivateL3(float* inputs, float* weights, float bias, ref float output)
        {
            var sumVec = _mm256_set1_ps(0.0f);

            for (int i = 0; i < L3_SIZE / F32_CHUNK_SIZE; i++)
            {
                var weightVec = _mm256_loadu_ps(&weights[i * F32_CHUNK_SIZE]);
                var inputsVec = _mm256_loadu_ps(&inputs[i * F32_CHUNK_SIZE]);
                sumVec = vec_mul_add_ps(inputsVec, weightVec, sumVec);
            }

            output = bias + Vector256.Sum(sumVec);
        }


        [MethodImpl(Inline)]
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

        [MethodImpl(Inline)]
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

            int whiteIndex = (768 * KingBuckets[wk]) + (    pc  * ColorStride) + (pt * PieceStride) + wSq;
            int blackIndex = (768 * KingBuckets[bk]) + (Not(pc) * ColorStride) + (pt * PieceStride) + bSq;

            return (whiteIndex * L1_SIZE, blackIndex * L1_SIZE);
        }

        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* src = pos.State->Accumulator;
            Accumulator* dst = pos.NextState->Accumulator;

            dst->NeedsRefresh[0] = src->NeedsRefresh[0];
            dst->NeedsRefresh[1] = src->NeedsRefresh[1];

            dst->Computed[0] = dst->Computed[1] = false;

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = pos.ToMove;
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            ref PerspectiveUpdate wUpdate = ref dst->Update[White];
            ref PerspectiveUpdate bUpdate = ref dst->Update[Black];

            //  Remove any updates that are present
            wUpdate.Clear();
            bUpdate.Clear();

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                dst->NeedsRefresh[us] = true;

                ref PerspectiveUpdate theirUpdate = ref dst->Update[them];

                int theirKing = pos.State->KingSquares[them];

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None && !m.IsCastle)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);
                    theirUpdate.PushSubSubAdd(from, cap, to);
                }
                else if (m.IsCastle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare();

                    to = FeatureIndexSingle(us, ourPiece, m.CastlingKingSquare(), theirKing, them);

                    int rookFrom = FeatureIndexSingle(us, Rook, rookFromSq, theirKing, them);
                    int rookTo = FeatureIndexSingle(us, Rook, rookToSq, theirKing, them);

                    theirUpdate.PushSubSubAddAdd(from, rookFrom, to, rookTo);
                }
                else
                {
                    theirUpdate.PushSubAdd(from, to);
                }
            }
            else
            {
                int wKing = pos.State->KingSquares[White];
                int bKing = pos.State->KingSquares[Black];

                (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom, wKing, bKing);
                (int wTo, int bTo) = FeatureIndex(us, m.IsPromotion ? m.PromotionTo : ourPiece, moveTo, wKing, bKing);

                if (m.IsCastle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare();

                    (wTo, bTo) = FeatureIndex(us, ourPiece, m.CastlingKingSquare(), wKing, bKing);

                    (int wRookFrom, int bRookFrom) = FeatureIndex(us, Rook, rookFromSq, wKing, bKing);
                    (int wRookTo, int bRookTo) = FeatureIndex(us, Rook, rookToSq, wKing, bKing);

                    wUpdate.PushSubSubAddAdd(wFrom, wRookFrom, wTo, wRookTo);
                    bUpdate.PushSubSubAddAdd(bFrom, bRookFrom, bTo, bRookTo);
                }
                else if (theirPiece != None)
                {
                    (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo, wKing, bKing);

                    wUpdate.PushSubSubAdd(wFrom, wCap, wTo);
                    bUpdate.PushSubSubAdd(bFrom, bCap, bTo);
                }
                else if (m.IsEnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    wUpdate.PushSubSubAdd(wFrom, wCap, wTo);
                    bUpdate.PushSubSubAdd(bFrom, bCap, bTo);
                }
                else
                {
                    wUpdate.PushSubAdd(wFrom, wTo);
                    bUpdate.PushSubAdd(bFrom, bTo);
                }
            }
        }

        [MethodImpl(Inline)]
        public static void MakeNullMove(Position pos)
        {
            var currAcc = pos.State->Accumulator;
            var nextAcc = pos.NextState->Accumulator;

            currAcc->CopyTo(nextAcc);

            nextAcc->Computed[White] = currAcc->Computed[White];
            nextAcc->Computed[Black] = currAcc->Computed[Black];
            nextAcc->Update[White].Clear();
            nextAcc->Update[Black].Clear();
        }


        //  The general concept here is based off of Stormphrax's implementation:
        //  https://github.com/Ciekce/Stormphrax/commit/9b76f2a35531513239ed7078acc21294a11e75c6
        [MethodImpl(Inline)]
        public static void ProcessUpdates(Position pos)
        {
            StateInfo* st = pos.State;
            for (int perspective = 0; perspective < 2; perspective++)
            {
                //  If the current state is correct for our perspective, no work is needed
                if (st->Accumulator->Computed[perspective])
                    continue;

                //  If the current state needs a refresh, don't bother with previous states
                if (st->Accumulator->NeedsRefresh[perspective])
                {
                    RefreshAccumulatorPerspective(pos, perspective);
                    continue;
                }

                //  Find the most recent computed or refresh-needed accumulator
                StateInfo* curr = st - 1;
                while (!curr->Accumulator->Computed[perspective] && !curr->Accumulator->NeedsRefresh[perspective])
                    curr--;

                if (curr->Accumulator->NeedsRefresh[perspective])
                {
                    //  The most recent accumulator would need to be refreshed,
                    //  so don't bother and refresh the current one instead
                    RefreshAccumulatorPerspective(pos, perspective);
                }
                else
                {
                    //  Update incrementally till the current accumulator is correct
                    while (curr != st)
                    {
                        StateInfo* prev = curr;
                        curr++;
                        UpdateSingle(prev->Accumulator, curr->Accumulator, perspective);
                    }
                }

            }
        }

        [MethodImpl(Inline)]
        public static void UpdateSingle(Accumulator* prev, Accumulator* curr, int perspective)
        {
            ref var updates = ref curr->Update[perspective];

            if (updates.AddCnt == 0 && updates.SubCnt == 0)
            {
                //  For null moves, we still need to carry forward the correct accumulator state
                prev->CopyTo(ref *curr, perspective);
                return;
            }

            var src = (short*)((*prev)[perspective]);
            var dst = (short*)((*curr)[perspective]);

            var FeatureWeights = Net.FTWeights;

            if (updates.AddCnt == 1 && updates.SubCnt == 1)
            {
                SubAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Adds[0]]);
            }
            else if (updates.AddCnt == 1 && updates.SubCnt == 2)
            {
                SubSubAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Subs[1]],
                    &FeatureWeights[updates.Adds[0]]);
            }
            else if (updates.AddCnt == 2 && updates.SubCnt == 2)
            {
                SubSubAddAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Subs[1]],
                    &FeatureWeights[updates.Adds[0]],
                    &FeatureWeights[updates.Adds[1]]);
            }

            curr->Computed[perspective] = true;
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



        private static void PermuteFT(Span<short> ftWeights, Span<short> ftBiases)
        {
            const int OneBucket = (INPUT_SIZE * L1_SIZE);
            short* temp = AlignedAllocZeroed<short>(OneBucket);

            for (int bucket = 0; bucket < INPUT_BUCKETS; bucket++)
            {
                Span<short> ftBucket = ftWeights[(bucket * OneBucket)..((bucket + 1) * OneBucket)];
                ftBucket.CopyTo(new Span<short>(temp, OneBucket));
                for (int i = 0; i < INPUT_SIZE; i++)
                {
                    for (int dst = 0; dst < PermuteIndices.Length; dst++)
                    {
                        int src = PermuteIndices[dst];
                        var f = i * L1_SIZE;

                        ftBucket[f + dst] = temp[f + src];
                        ftBucket[f + dst + L1_PAIR_COUNT] = temp[f + src + L1_PAIR_COUNT];
                    }
                }
            }

            ftBiases.CopyTo(new Span<short>(temp, L1_SIZE));
            for (int dst = 0; dst < PermuteIndices.Length; dst++)
            {
                int src = PermuteIndices[dst];

                ftBiases[dst] = temp[src];
                ftBiases[dst + L1_PAIR_COUNT] = temp[src + L1_PAIR_COUNT];
            }

            NativeMemory.AlignedFree(temp);
        }

        private static void PermuteL1(sbyte[,,] l1Weights)
        {
            sbyte[,,] temp = new sbyte[L1_SIZE, OUTPUT_BUCKETS, L2_SIZE];

            Array.Copy(l1Weights, temp, N_L1W);
            for (int dst = 0; dst < PermuteIndices.Length; dst++)
            {
                int src = PermuteIndices[dst];

                for (int b = 0; b < OUTPUT_BUCKETS; b++)
                {
                    for (int l2 = 0; l2 < L2_SIZE; l2++)
                    {
                        l1Weights[dst, b, l2] = temp[src, b, l2];
                        l1Weights[dst + L1_PAIR_COUNT, b, l2] = temp[src + L1_PAIR_COUNT, b, l2];
                    }
                }
            }
        }

        public static void PrintActivationStats()
        {
#if PERM_COUNT
            using var f = File.Open("perm.txt", FileMode.Create);
            using StreamWriter tw = new StreamWriter(f);
            for (int i = 0; i < NNZCounts.Length; i++)
            {
                tw.WriteLine($"{i} {NNZCounts[i]}");
            }
            Log($"{ActivationCount} / {EvalCalls} = {(double)ActivationCount / EvalCalls}");

            NNZCounts
                .Select((v, i) => (i, v))
                .Where(pair => pair.i < (L1_SIZE / 2))
                .OrderByDescending(pair => pair.v)
                .Select(pair => pair.i)
                .Chunk(16)
                .ToList()
                .ForEach(chunk =>
                {
                    Console.WriteLine($"{string.Join(", ", chunk)},");
                });
#endif
        }


        private static readonly int[] PermuteIndices = new int[L1_PAIR_COUNT];
        //  ((?:\d+, ){16})
        //  $1\n
        private static ReadOnlySpan<int> BestIndices =>
        [
            87, 429, 321, 372, 43, 395, 102, 62, 464, 230, 610, 495, 360, 314, 275, 196,
            35, 636, 375, 118, 263, 75, 586, 528, 513, 543, 36, 455, 412, 10, 61, 109,
            542, 447, 387, 274, 191, 288, 508, 188, 201, 91, 480, 129, 121, 588, 14, 615,
            256, 511, 313, 486, 431, 470, 3, 162, 133, 348, 418, 270, 45, 318, 504, 286,
            562, 541, 126, 389, 428, 625, 614, 189, 335, 563, 114, 238, 155, 550, 422, 385,
            267, 315, 217, 252, 535, 554, 273, 92, 227, 390, 294, 154, 521, 628, 249, 441,
            494, 423, 127, 130, 290, 421, 222, 116, 25, 398, 305, 324, 476, 237, 627, 496,
            567, 371, 103, 532, 231, 601, 105, 475, 514, 293, 382, 575, 621, 17, 223, 332,
            137, 276, 527, 158, 471, 454, 569, 21, 519, 425, 363, 216, 81, 119, 317, 101,
            22, 620, 501, 462, 134, 107, 611, 453, 71, 245, 343, 529, 299, 386, 120, 240,
            407, 328, 362, 151, 487, 199, 561, 64, 479, 63, 97, 467, 44, 493, 558, 459,
            139, 80, 47, 525, 325, 483, 584, 165, 306, 272, 426, 90, 320, 94, 19, 600,
            557, 634, 203, 570, 359, 536, 576, 549, 410, 432, 112, 392, 96, 618, 79, 361,
            153, 424, 311, 482, 148, 607, 582, 579, 291, 490, 74, 589, 605, 132, 277, 11,
            190, 174, 244, 307, 485, 115, 571, 271, 243, 323, 283, 350, 458, 345, 225, 77,
            555, 173, 1, 42, 491, 585, 30, 330, 590, 489, 630, 617, 606, 298, 152, 241,
            347, 259, 253, 457, 598, 619, 444, 28, 0, 170, 402, 282, 339, 568, 68, 254,
            176, 175, 6, 420, 366, 39, 515, 33, 443, 149, 287, 73, 181, 220, 612, 384,
            123, 219, 602, 469, 99, 434, 76, 518, 394, 168, 301, 41, 179, 451, 413, 327,
            509, 531, 353, 5, 333, 185, 616, 110, 247, 67, 209, 26, 58, 48, 336, 404,
            84, 445, 405, 8, 202, 18, 221, 269, 466, 473, 210, 53, 506, 66, 334, 180,
            492, 329, 572, 593, 408, 226, 379, 309, 484, 161, 595, 93, 208, 524, 409, 448,
            78, 192, 279, 346, 128, 51, 229, 285, 368, 266, 150, 388, 452, 520, 122, 474,
            596, 456, 65, 70, 206, 164, 439, 599, 573, 354, 592, 503, 530, 141, 104, 553,
            551, 264, 340, 450, 609, 232, 138, 316, 638, 397, 370, 552, 83, 594, 37, 559,
            356, 446, 639, 400, 265, 167, 352, 117, 624, 629, 393, 437, 516, 512, 632, 163,
            57, 34, 312, 461, 357, 381, 262, 477, 403, 32, 546, 55, 435, 548, 544, 194,
            533, 166, 187, 564, 178, 95, 507, 566, 228, 427, 547, 72, 207, 113, 396, 218,
            182, 442, 308, 537, 304, 23, 319, 160, 251, 608, 100, 523, 89, 604, 146, 29,
            449, 142, 488, 517, 499, 438, 478, 378, 297, 46, 234, 295, 341, 377, 24, 9,
            296, 255, 248, 60, 246, 156, 171, 603, 85, 613, 436, 213, 31, 580, 534, 505,
            591, 399, 4, 460, 300, 369, 391, 355, 284, 502, 373, 280, 239, 88, 205, 211,
            463, 416, 86, 635, 183, 260, 540, 20, 135, 204, 195, 7, 401, 626, 383, 581,
            140, 303, 242, 16, 526, 214, 193, 278, 125, 268, 49, 82, 481, 545, 250, 98,
            498, 433, 143, 147, 417, 351, 522, 131, 556, 631, 52, 472, 578, 40, 302, 577,
            322, 338, 349, 468, 292, 406, 637, 111, 415, 184, 411, 54, 342, 414, 310, 136,
            212, 367, 261, 380, 344, 69, 12, 56, 172, 560, 633, 538, 106, 13, 565, 281,
            144, 2, 169, 236, 583, 108, 497, 157, 500, 622, 59, 331, 326, 597, 27, 419,
            374, 623, 38, 145, 358, 258, 365, 235, 574, 289, 430, 233, 224, 200, 159, 257,
            337, 15, 198, 177, 124, 539, 465, 376, 197, 186, 440, 215, 364, 587, 50, 510,
        ];

    }
}