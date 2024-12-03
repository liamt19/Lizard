
#define NO_PERM
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
        public const int INPUT_BUCKETS = 16;
        public const int INPUT_SIZE = 768;
        public const int L1_SIZE = 1536;
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
             0,  1,  2,  3, 19, 18, 17, 16,
             4,  5,  6,  7, 23, 22, 21, 20,
             8,  9, 10, 11, 27, 26, 25, 24,
             8,  9, 10, 11, 27, 26, 25, 24,
            12, 12, 13, 13, 29, 29, 28, 28,
            12, 12, 13, 13, 29, 29, 28, 28,
            14, 14, 15, 15, 31, 31, 30, 30,
            14, 14, 15, 15, 31, 31, 30, 30,
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
            const int numChunks = (32 / 2) / sizeof(short);
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
            223, 612, 641, 227, 29, 634, 827, 250, 25, 105, 1004, 900, 103, 614, 580, 78,
            415, 896, 967, 219, 947, 886, 1016, 494, 839, 154, 910, 695, 615, 1017, 661, 212,
            139, 935, 517, 920, 209, 221, 995, 420, 258, 864, 991, 752, 136, 233, 126, 932,
            369, 186, 697, 555, 49, 945, 406, 197, 208, 521, 980, 380, 691, 878, 632, 682,
            280, 812, 267, 511, 879, 922, 428, 90, 885, 344, 394, 431, 525, 829, 358, 629,
            143, 464, 138, 798, 71, 895, 842, 529, 965, 647, 538, 469, 627, 401, 488, 678,
            403, 854, 411, 375, 807, 461, 1013, 1000, 429, 822, 845, 359, 242, 283, 124, 243,
            142, 423, 308, 847, 112, 271, 533, 4, 405, 639, 719, 257, 30, 579, 36, 905,
            44, 247, 541, 865, 442, 797, 425, 153, 717, 337, 1009, 861, 588, 701, 204, 59,
            149, 94, 462, 520, 32, 61, 786, 88, 404, 179, 201, 735, 569, 925, 985, 365,
            777, 315, 578, 381, 715, 959, 249, 999, 57, 162, 976, 888, 346, 600, 558, 631,
            613, 713, 448, 610, 775, 453, 894, 788, 942, 220, 729, 178, 815, 594, 23, 157,
            13, 207, 562, 378, 703, 82, 733, 761, 721, 745, 567, 949, 134, 982, 685, 789,
            1001, 502, 395, 106, 776, 819, 115, 161, 64, 325, 274, 941, 828, 474, 791, 916,
            70, 421, 214, 509, 355, 544, 1005, 784, 473, 557, 117, 549, 817, 898, 859, 779,
            408, 680, 637, 41, 48, 951, 724, 387, 589, 1, 286, 585, 688, 787, 890, 76,
            224, 628, 137, 457, 328, 234, 964, 597, 539, 856, 205, 102, 20, 236, 950, 349,
            390, 960, 598, 410, 332, 440, 288, 150, 497, 482, 171, 34, 526, 1012, 330, 669,
            1015, 309, 810, 994, 338, 853, 796, 56, 726, 222, 446, 449, 152, 322, 466, 858,
            217, 595, 50, 618, 239, 28, 489, 303, 601, 7, 368, 477, 523, 438, 302, 336,
            536, 342, 245, 75, 738, 259, 272, 492, 270, 970, 675, 383, 97, 686, 314, 841,
            229, 444, 870, 141, 133, 127, 546, 296, 548, 284, 282, 483, 652, 689, 805, 195,
            820, 40, 364, 938, 327, 331, 304, 86, 923, 705, 439, 599, 33, 379, 676, 650,
            630, 642, 54, 293, 1023, 487, 24, 833, 802, 447, 955, 192, 693, 414, 93, 748,
            747, 605, 867, 645, 790, 285, 60, 659, 266, 373, 654, 253, 260, 866, 832, 671,
            990, 113, 924, 109, 21, 663, 434, 479, 430, 609, 180, 730, 918, 824, 750, 992,
            26, 363, 592, 291, 893, 354, 936, 699, 432, 128, 91, 241, 651, 808, 931, 668,
            1019, 933, 69, 382, 396, 501, 329, 164, 63, 591, 165, 412, 215, 907, 794, 860,
            129, 889, 460, 871, 753, 9, 413, 188, 766, 175, 107, 81, 707, 804, 1008, 1022,
            913, 146, 53, 966, 734, 45, 1003, 755, 2, 626, 813, 17, 92, 636, 590, 467,
            445, 534, 348, 427, 584, 587, 203, 269, 566, 564, 213, 163, 491, 764, 510, 300,
            15, 620, 986, 624, 643, 535, 800, 42, 299, 522, 731, 782, 711, 292, 720, 170,
            515, 471, 673, 39, 623, 298, 66, 6, 846, 503, 561, 158, 313, 74, 816, 948,
            725, 768, 277, 848, 801, 194, 914, 683, 679, 275, 319, 684, 868, 51, 774, 903,
            496, 276, 263, 1014, 422, 927, 617, 551, 55, 852, 87, 287, 692, 486, 100, 937,
            409, 758, 451, 73, 781, 398, 565, 351, 708, 770, 10, 8, 324, 830, 290, 963,
            850, 340, 316, 12, 16, 869, 780, 765, 374, 362, 43, 518, 83, 863, 919, 400,
            528, 507, 712, 46, 855, 210, 971, 940, 915, 320, 912, 166, 524, 505, 67, 727,
            977, 785, 993, 490, 519, 884, 516, 450, 930, 200, 323, 297, 989, 821, 248, 570,
            72, 98, 114, 881, 481, 876, 22, 823, 37, 756, 5, 987, 155, 80, 335, 216,
            926, 147, 604, 929, 702, 917, 540, 456, 235, 531, 763, 593, 135, 376, 397, 622,
            182, 262, 760, 228, 849, 700, 238, 65, 11, 370, 961, 704, 843, 655, 646, 426,
            350, 576, 560, 498, 495, 709, 806, 62, 640, 582, 399, 279, 366, 1002, 877, 657,
            616, 969, 746, 268, 388, 104, 131, 225, 887, 710, 696, 665, 506, 681, 144, 837,
            385, 872, 754, 187, 972, 339, 834, 559, 455, 110, 736, 825, 892, 246, 402, 52,
            122, 943, 305, 769, 289, 874, 811, 698, 552, 952, 84, 979, 672, 857, 443, 1010,
            356, 656, 391, 132, 174, 211, 803, 608, 38, 353, 452, 603, 372, 988, 568, 168,
            984, 493, 778, 120, 441, 424, 749, 983, 1020, 169, 968, 202, 85, 563, 306, 667,
            621, 19, 809, 962, 470, 255, 757, 206, 773, 690, 767, 475, 311, 814, 468, 619,
            547, 625, 47, 177, 116, 294, 184, 674, 454, 333, 244, 416, 513, 465, 573, 586,
            472, 873, 954, 58, 818, 611, 648, 419, 418, 602, 882, 928, 772, 742, 1006, 792,
            321, 307, 909, 571, 191, 649, 946, 739, 537, 875, 232, 341, 545, 660, 997, 694,
            741, 251, 463, 793, 574, 237, 240, 123, 334, 956, 145, 981, 173, 27, 939, 795,
            167, 14, 744, 273, 934, 499, 281, 433, 718, 189, 706, 664, 31, 921, 480, 435,
            417, 101, 151, 392, 530, 973, 3, 393, 514, 1007, 230, 361, 543, 638, 607, 108,
            111, 181, 978, 732, 737, 550, 18, 160, 722, 140, 862, 79, 908, 343, 512, 677,
            96, 911, 716, 407, 185, 653, 771, 459, 957, 542, 318, 975, 527, 799, 556, 389,
            906, 183, 199, 436, 553, 264, 897, 844, 583, 840, 508, 743, 666, 347, 1021, 762,
            265, 740, 532, 687, 148, 317, 851, 902, 256, 606, 218, 644, 130, 190, 377, 714,
            826, 835, 458, 278, 198, 301, 99, 254, 783, 944, 345, 119, 728, 176, 836, 996,
            581, 386, 1011, 998, 831, 500, 572, 880, 899, 360, 891, 68, 974, 635, 575, 261,
            118, 751, 121, 476, 89, 295, 196, 596, 670, 658, 35, 901, 156, 554, 904, 883,
            352, 723, 172, 384, 226, 437, 633, 958, 0, 1018, 231, 662, 77, 484, 759, 367,
            485, 953, 312, 504, 95, 125, 193, 357, 159, 310, 326, 252, 371, 478, 577, 838,
        ];

    }
}