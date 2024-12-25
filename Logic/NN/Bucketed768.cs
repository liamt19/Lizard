
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
        public const int INPUT_BUCKETS = 14;
        public const int INPUT_SIZE = 768;
        public const int L1_SIZE = 2048;
        public const int L2_SIZE = 16;
        public const int L3_SIZE = 32;
        public const int OUTPUT_BUCKETS = 8;

        private const int FT_QUANT = 255;
        private const int FT_SHIFT = 10;
        private const int L1_QUANT = 64;
        private const int OutputScale = 400;

        private static readonly int U8_CHUNK_SIZE = (NNUE.UseAvx ? sizeof(Vector256<byte>) : sizeof(Vector128<byte>)) / sizeof(byte);
        private static readonly int I16_CHUNK_SIZE = (NNUE.UseAvx ? sizeof(Vector256<short>) : sizeof(Vector128<short>)) / sizeof(short);
        private static readonly int I32_CHUNK_SIZE = (NNUE.UseAvx ? sizeof(Vector256<int>) : sizeof(Vector128<int>)) / sizeof(int);
        private static readonly int F32_CHUNK_SIZE = (NNUE.UseAvx ? sizeof(Vector256<float>) : sizeof(Vector128<float>)) / sizeof(float);

        private static readonly int NNZ_INPUT_SIMD_WIDTH = I32_CHUNK_SIZE;
        private static readonly int NNZ_OUTPUTS_PER_CHUNK = Math.Max(NNZ_INPUT_SIMD_WIDTH, 8) / 8;

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
             0,  1,  2,  3, 17, 16, 15, 14,
             4,  5,  6,  7, 21, 20, 19, 18,
             8,  9, 10, 11, 25, 24, 23, 22,
             8,  9, 10, 11, 25, 24, 23, 22,
            12, 12, 13, 13, 27, 27, 26, 26,
            12, 12, 13, 13, 27, 27, 26, 26,
            12, 12, 13, 13, 27, 27, 26, 26,
            12, 12, 13, 13, 27, 27, 26, 26,
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

#if BINDINGS
            HorsieSetupNNZ();
            Log("Bindings found " + (NNUE.UseAvx ? "and in use" : "but (intentionally) unused"));
#else
            NNZLookup = AlignedAllocZeroed<Vector128<ushort>>(256);
            SetupNNZ();
            Log("Bindings are not in use");
#endif


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
                if (!NNUE.UseFallback)
                {
                    for (int i = 0; i < L1_SIZE / L1_CHUNK_PER_32; ++i)
                        for (int j = 0; j < L2_SIZE; ++j)
                            for (int k = 0; k < L1_CHUNK_PER_32; ++k)
                                Net.L1Weights[bucket][i * L1_CHUNK_PER_32 * L2_SIZE
                                                    + j * L1_CHUNK_PER_32
                                                    + k] = UQNet.L1Weights[i * L1_CHUNK_PER_32 + k, bucket, j];
                }
                else
                {
                    for (int i = 0; i < L1_SIZE; ++i)
                        for (int j = 0; j < L2_SIZE; ++j)
                            Net.L1Weights[bucket][j * L1_SIZE + i] = UQNet.L1Weights[i, bucket, j];
                }

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

            if (NNUE.UseFallback)
                return;

            int numRegi = NNUE.UseAvx ? 4 : 2;
            int numChunks = 16 / sizeof(short);
            Span<int> order = NNUE.UseAvx ? [0, 2, 1, 3] : [0, 1];

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


        public static short GetEvaluation(Position pos)
        {
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = (occ - 2) / ((32 + OUTPUT_BUCKETS - 1) / OUTPUT_BUCKETS);

            var v = NNUE.UseAvx ? GetEvaluation(pos, outputBucket) :
                    NNUE.UseSSE ? GetEvaluationSSE(pos, outputBucket) :
                    NNUE.UseARM ? GetEvaluationARM(pos, outputBucket) :
                                  GetEvaluationFallback(pos, outputBucket);

            return (short)v;
        }


        public static short GetEvaluation(Position pos, int outputBucket)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            Bucketed768.ProcessUpdates(pos);

            float* L1Outputs = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            var us   = (short*)(accumulator[pos.ToMove]);
            var them = (short*)(accumulator[Not(pos.ToMove)]);

#if BINDINGS
            HorsieActivateFTSparse(us, them, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs);
            HorsieActivateL2(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            HorsieActivateL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);
#else
            ActivateFTSparse(us, them, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs);
            ActivateL2(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);
#endif

            return (short)(L3Output * OutputScale);
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
                        _mm_storeu_si128(&nnzIndices[nnzCount], _mm_add_epi16(baseVec, offsets));

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

            output = bias + vec_reduce_add_ps(sumVec);
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
            238, 406, 493, 921, 296, 700, 1003, 855, 663, 450, 996, 511, 337, 400, 82, 424,
            841, 984, 769, 554, 653, 181, 678, 286, 266, 667, 109, 604, 78, 303, 713, 798,
            213, 889, 644, 79, 685, 507, 850, 838, 999, 909, 184, 503, 591, 557, 770, 501,
            620, 260, 654, 779, 97, 595, 420, 724, 514, 944, 263, 868, 297, 500, 316, 359,
            797, 879, 386, 9, 699, 453, 570, 73, 568, 69, 596, 194, 198, 330, 916, 807,
            126, 910, 1007, 475, 494, 734, 427, 414, 936, 822, 542, 81, 349, 197, 71, 818,
            360, 170, 4, 437, 703, 193, 645, 125, 447, 312, 95, 588, 10, 264, 27, 505,
            433, 982, 116, 233, 72, 992, 504, 270, 241, 827, 436, 458, 358, 550, 561, 832,
            275, 295, 1023, 342, 611, 183, 928, 851, 751, 899, 247, 549, 446, 753, 771, 167,
            612, 963, 136, 849, 635, 328, 134, 617, 768, 621, 431, 735, 294, 422, 639, 958,
            355, 980, 630, 1020, 661, 392, 357, 174, 391, 931, 664, 372, 469, 1, 171, 623,
            428, 633, 553, 787, 237, 60, 572, 569, 796, 208, 593, 122, 540, 745, 92, 94,
            421, 236, 551, 8, 301, 246, 643, 29, 884, 668, 544, 726, 576, 837, 728, 464,
            288, 259, 249, 811, 867, 350, 13, 468, 616, 763, 520, 628, 302, 196, 16, 0,
            175, 325, 466, 809, 215, 859, 142, 656, 58, 209, 960, 473, 410, 179, 30, 893,
            488, 815, 340, 416, 875, 525, 904, 387, 823, 1006, 96, 23, 439, 31, 606, 895,
            38, 702, 245, 813, 537, 597, 388, 979, 229, 881, 347, 188, 590, 587, 836, 380,
            506, 204, 335, 327, 619, 1001, 252, 605, 988, 897, 412, 489, 106, 418, 324, 154,
            17, 498, 32, 714, 833, 528, 636, 367, 15, 51, 14, 45, 160, 332, 935, 559,
            20, 711, 648, 968, 477, 541, 321, 556, 518, 448, 57, 555, 434, 202, 19, 934,
            749, 120, 788, 222, 579, 480, 933, 84, 223, 258, 946, 274, 649, 182, 62, 290,
            729, 894, 153, 101, 989, 853, 864, 137, 976, 954, 940, 821, 124, 415, 522, 66,
            780, 394, 858, 331, 322, 707, 826, 180, 508, 805, 825, 115, 696, 339, 584, 977,
            562, 792, 647, 677, 1008, 756, 578, 12, 269, 941, 983, 778, 516, 143, 43, 907,
            425, 846, 471, 665, 117, 63, 478, 28, 722, 900, 786, 277, 882, 144, 842, 284,
            21, 594, 905, 190, 232, 772, 262, 465, 659, 351, 938, 444, 834, 795, 219, 496,
            227, 794, 48, 426, 162, 548, 242, 145, 451, 155, 354, 955, 251, 698, 381, 538,
            64, 86, 248, 640, 470, 845, 804, 691, 374, 404, 876, 280, 912, 147, 641, 903,
            211, 1000, 913, 739, 34, 283, 1011, 123, 957, 1010, 341, 87, 375, 371, 948, 377,
            1015, 441, 326, 552, 627, 607, 586, 800, 923, 345, 759, 111, 732, 276, 363, 467,
            692, 1021, 362, 364, 300, 24, 91, 440, 212, 560, 885, 113, 129, 871, 886, 88,
            35, 228, 766, 927, 1009, 527, 799, 687, 378, 793, 975, 987, 271, 950, 765, 138,
            775, 463, 592, 706, 1013, 483, 178, 748, 539, 566, 133, 361, 998, 99, 738, 376,
            721, 599, 961, 883, 243, 932, 26, 41, 1014, 291, 495, 93, 310, 365, 760, 482,
            783, 589, 42, 172, 1017, 828, 773, 166, 803, 959, 148, 812, 230, 672, 622, 1005,
            930, 781, 334, 531, 383, 750, 671, 411, 573, 727, 571, 613, 774, 176, 785, 547,
            937, 390, 725, 158, 880, 887, 187, 972, 385, 314, 59, 533, 157, 860, 65, 261,
            642, 318, 695, 764, 1004, 658, 974, 389, 717, 396, 85, 472, 486, 461, 449, 265,
            121, 861, 319, 313, 502, 943, 407, 716, 994, 490, 839, 462, 455, 565, 743, 151,
            929, 862, 484, 986, 107, 683, 281, 681, 37, 693, 53, 676, 323, 789, 874, 46,
            603, 863, 510, 949, 366, 289, 235, 491, 408, 819, 852, 962, 405, 546, 816, 723,
            758, 848, 535, 720, 790, 108, 40, 401, 817, 524, 513, 791, 285, 981, 189, 200,
            487, 682, 1022, 499, 307, 744, 103, 762, 625, 517, 847, 119, 156, 1018, 767, 255,
            304, 104, 3, 459, 877, 292, 161, 709, 519, 890, 997, 1019, 128, 873, 239, 680,
            747, 413, 481, 305, 399, 856, 914, 951, 397, 149, 492, 49, 869, 964, 336, 990,
            741, 608, 100, 150, 824, 1012, 670, 368, 947, 140, 240, 718, 409, 127, 840, 430,
            343, 908, 139, 89, 173, 317, 135, 598, 67, 70, 534, 130, 11, 708, 114, 293,
            704, 56, 705, 74, 892, 452, 536, 402, 694, 131, 808, 956, 370, 68, 257, 191,
            479, 684, 610, 829, 432, 601, 993, 250, 679, 309, 192, 512, 754, 971, 857, 742,
            564, 55, 878, 1016, 906, 5, 7, 782, 98, 650, 267, 712, 740, 50, 1002, 141,
            655, 186, 164, 673, 736, 2, 382, 801, 344, 697, 872, 820, 206, 563, 195, 830,
            776, 395, 843, 308, 44, 710, 615, 419, 320, 945, 457, 583, 854, 61, 90, 626,
            199, 529, 523, 132, 201, 614, 76, 268, 47, 634, 582, 279, 970, 152, 254, 731,
            675, 969, 217, 618, 398, 22, 311, 515, 526, 926, 602, 225, 185, 666, 844, 746,
            315, 637, 902, 646, 600, 918, 965, 953, 733, 384, 662, 393, 761, 352, 686, 474,
            752, 530, 165, 220, 558, 224, 715, 985, 922, 253, 629, 379, 810, 737, 203, 346,
            973, 609, 105, 925, 306, 216, 445, 898, 911, 244, 967, 891, 177, 163, 356, 580,
            901, 831, 102, 581, 6, 896, 33, 835, 460, 39, 25, 435, 36, 77, 52, 205,
            110, 521, 757, 802, 168, 915, 688, 118, 210, 755, 545, 329, 282, 214, 784, 333,
            632, 660, 80, 256, 777, 652, 942, 443, 567, 83, 624, 54, 75, 651, 509, 730,
            669, 417, 169, 952, 438, 442, 476, 991, 369, 456, 429, 146, 719, 348, 966, 273,
            978, 353, 221, 888, 575, 454, 159, 631, 870, 689, 920, 234, 287, 543, 532, 814,
            577, 574, 338, 806, 299, 231, 585, 112, 373, 701, 218, 272, 919, 638, 485, 657,
            207, 690, 403, 917, 866, 939, 278, 423, 674, 497, 298, 226, 18, 865, 995, 924,
        ];

#if BINDINGS
        [DllImport("HorsieBindings.dll", EntryPoint = "SetupNNZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HorsieSetupNNZ();

        [DllImport("HorsieBindings.dll", EntryPoint = "ActivateFTSparse", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HorsieActivateFTSparse(short* us, short* them, sbyte* weights, float* biases, float* output);

        [DllImport("HorsieBindings.dll", EntryPoint = "ActivateL2", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HorsieActivateL2(float* inputs, float* weights, float* biases, float* output);

        [DllImport("HorsieBindings.dll", EntryPoint = "ActivateL3", CallingConvention = CallingConvention.Cdecl)]
        public static extern void HorsieActivateL3(float* inputs, float* weights, float bias, ref float output);
#endif
    }
}