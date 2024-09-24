
#define SPARSE

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Lizard.Logic.Threads;

using static Lizard.Logic.NN.Aliases;
using static Lizard.Logic.NN.FunUnrollThings;

namespace Lizard.Logic.NN
{
    public static unsafe partial class Bucketed768
    {
        public const int INPUT_BUCKETS = 4;
        public const int INPUT_SIZE = 768;
        public const int L1_SIZE = 1024;
        public const int L2_SIZE = 32;
        public const int L3_SIZE = 32;
        public const int OUTPUT_BUCKETS = 8;

        private const int FT_QUANT = 362;
        private const int FT_SHIFT = 10;
        private const int L1_QUANT = 32;

        public const int OutputScale = 400;

        public static readonly int SIMD_CHUNKS_512 = L1_SIZE / Vector512<short>.Count;
        public static readonly int SIMD_CHUNKS_256 = L1_SIZE / Vector256<short>.Count;
        public const int L1_CHUNK_PER_32 = sizeof(int) / sizeof(sbyte);

        public const int FT_CHUNK_SIZE = 32 / sizeof(short);
        public const int L1_CHUNK_SIZE = 32 / sizeof(sbyte);
        public const int L2_CHUNK_SIZE = 32 / sizeof(float);
        public const int L3_CHUNK_SIZE = 32 / sizeof(float);

        /// <summary>
        /// (768 -> 1024)x2 -> (32 -> 32)x8 -> 1? I don't know anymore
        /// </summary>
        public const string NetworkName = "net-010-layers-4m32-250.bin";

        private static readonly UQNetContainer UQNet;
        public static readonly NetContainer<short, sbyte, float> Net;
        private static readonly Vector128<ushort>* NNZLookup;

        public const int N_FTW = INPUT_SIZE * L1_SIZE * INPUT_BUCKETS;
        public const int N_FTB = L1_SIZE;

        public const int N_L1W = OUTPUT_BUCKETS * L1_SIZE * L2_SIZE;
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
            0, 0, 1, 1, 5, 5, 4, 4,
            0, 0, 1, 1, 5, 5, 4, 4,
            2, 2, 2, 2, 6, 6, 6, 6,
            2, 2, 2, 2, 6, 6, 6, 6,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
            3, 3, 3, 3, 7, 7, 7, 7,
        ];

        public static int BucketForPerspective(int ksq, int perspective) => (KingBuckets[perspective == Black ? (ksq ^ 56) : ksq]);

        static Bucketed768()
        {
            UQNet = new UQNetContainer();
            Net = new NetContainer<short, sbyte, float>();
            NNZLookup = AlignedAllocZeroed<Vector128<ushort>>(256);
            SetupNNZ();

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

                for (int i = 0; i < L1_SIZE / L1_CHUNK_PER_32; ++i)
                    for (int j = 0; j < L2_SIZE; ++j)
                        for (int k = 0; k < L1_CHUNK_PER_32; ++k)
                            Net.L1Weights[bucket][i * L1_CHUNK_PER_32 * L2_SIZE
                                                + j * L1_CHUNK_PER_32
                                                + k] = (sbyte)(MathF.Round(UQNet.L1Weights[i * L1_CHUNK_PER_32 + k, bucket, j] * L1_QUANT));

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



        public static int GetEvaluation(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;

            Bucketed768.ProcessUpdates(pos);

            byte*  FTOutputs = stackalloc byte [L1_SIZE];
            float* L1Outputs = stackalloc float[L2_SIZE];
            float* L2Outputs = stackalloc float[L3_SIZE];
            float L3Output = 0;

            //  Formula from BlackMarlin
            int occ = (int)popcount(pos.bb.Occupancy);
            int outputBucket = Math.Min((63 - occ) * (32 - occ) / 225, 7);

            var us   = (short*)(accumulator[pos.ToMove]);
            var them = (short*)(accumulator[Not(pos.ToMove)]);

            ushort* nnzIndices = stackalloc ushort[L1_SIZE / L1_CHUNK_PER_32];
            int nnzCount = 0;
            ActivateFTSparse(us, them, FTOutputs, nnzIndices, ref nnzCount);
            ActivateL1Sparse(FTOutputs, Net.L1Weights[outputBucket], Net.L1Biases[outputBucket], L1Outputs, nnzIndices, nnzCount);
            ActivateL2(L1Outputs, Net.L2Weights[outputBucket], Net.L2Biases[outputBucket], L2Outputs);
            ActivateL3(L2Outputs, Net.L3Weights[outputBucket], Net.L3Biases[outputBucket], ref L3Output);

            return (int)(L3Output * OutputScale);
        }



        public static void ActivateFTSparse(short* us, short* them, byte* output, ushort* nnzIndices, ref int nnzCount)
        {
            var zero = _mm256_setzero_epi16();
            var one = _mm256_set1_epi16(FT_QUANT);

            nnzCount = 0;
            int offset = 0;

            const int STRIDE = L1_SIZE / 2;
            const int STEP = 2 * FT_CHUNK_SIZE;

            Vector128<ushort> baseInc = Vector128.Create((ushort)8);
            Vector128<ushort> baseVec = Vector128<ushort>.Zero;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                short* acc = perspective == 0 ? us : them;

                for (int i = 0; i < STRIDE; i += STEP)
                {
                    var input0a = _mm256_load_si256(&acc[i + 0             + 0]);
                    var input0b = _mm256_load_si256(&acc[i + FT_CHUNK_SIZE + 0]);
                    var input1a = _mm256_load_si256(&acc[i + 0             + STRIDE]);
                    var input1b = _mm256_load_si256(&acc[i + FT_CHUNK_SIZE + STRIDE]);

                    var clipped0a = _mm256_min_epi16(_mm256_max_epi16(input0a, zero), one);
                    var clipped0b = _mm256_min_epi16(_mm256_max_epi16(input0b, zero), one);
                    var clipped1a = _mm256_min_epi16(input1a, one);
                    var clipped1b = _mm256_min_epi16(input1b, one);

                    var s0 = _mm256_mulhi_epi16(_mm256_slli_epi16(clipped0a, 16 - FT_SHIFT), clipped1a);
                    var s1 = _mm256_mulhi_epi16(_mm256_slli_epi16(clipped0b, 16 - FT_SHIFT), clipped1b);

                    var packed = _mm256_packus_epi16(s0, s1).AsByte();
                    _mm256_storeu_si256(&output[offset + i], packed);

                    var nnzMask = vec_nnz_mask(packed);

                    {
                        int lookup = nnzMask & 0xFF;
                        var offsets = NNZLookup[lookup];
                        _mm128_storeu_si128(&nnzIndices[nnzCount], _mm_add_epi16(baseVec, offsets));

                        nnzCount += int.PopCount(lookup);
                        baseVec += baseInc;
                    }
                }

                offset += STRIDE;
            }
        }

        public static void ActivateL1Sparse(byte* inputs, sbyte* weights, float* biases, float* output, ushort* nnzIndices, int nnzCount)
        {
            var sums = stackalloc Vector256<int>[L2_SIZE / L2_CHUNK_SIZE];
            int* inputs32 = (int*)(inputs);
            for (int i = 0; i < nnzCount; ++i)
            {
                var index = nnzIndices[i];
                var input32 = _mm256_set1_epi32(inputs32[index]);
                var weight = (Vector256<sbyte>*)(&weights[index * L1_CHUNK_PER_32 * L2_SIZE]);
                for (int j = 0; j < L2_SIZE / L2_CHUNK_SIZE; ++j)
                    sums[j] = vec_dpbusd_epi32(sums[j], input32.AsByte(), weight[j]);
            }

            var zero = _mm256_set1_ps(0.0f);
            var one = Vector256<float>.One;
            for (int i = 0; i < L2_SIZE / L2_CHUNK_SIZE; ++i)
            {
                var biasVec = _mm256_loadu_ps(&biases[i * L2_CHUNK_SIZE]);
                var sumDiv = _mm256_set1_ps((1 << FT_SHIFT) / (float)(FT_QUANT * FT_QUANT * L1_QUANT));
                var sumPs = _mm256_fmadd_ps(_mm256_cvtepi32_ps(sums[i]), sumDiv, biasVec);
                var clipped = _mm256_min_ps(_mm256_max_ps(sumPs, zero), one);
                var squared = _mm256_mul_ps(clipped, clipped);
                _mm256_storeu_ps(&output[i * L2_CHUNK_SIZE], squared);

            }
        }


        public static void ActivateL2(float* inputs, float* weights, float* biases, float* output)
        {
            var sumVecs = stackalloc Vector256<float>[L3_SIZE / L3_CHUNK_SIZE];

            for (int i = 0; i < L3_SIZE / L3_CHUNK_SIZE; ++i)
                sumVecs[i] = _mm256_loadu_ps(&biases[i * L3_CHUNK_SIZE]);

            for (int i = 0; i < L2_SIZE; ++i)
            {
                var inputVec = _mm256_set1_ps(inputs[i]);
                var weight = (Vector256<float>*)(&weights[i * L3_SIZE]);
                for (int j = 0; j < L3_SIZE / L3_CHUNK_SIZE; ++j)
                    sumVecs[j] = vec_mul_add_ps(inputVec, weight[j], sumVecs[j]);
            }

            var zero = _mm256_set1_ps(0.0f);
            var one = _mm256_set1_ps(1.0f);

            for (int i = 0; i < L3_SIZE / L3_CHUNK_SIZE; ++i)
            {
                var clipped = _mm256_min_ps(_mm256_max_ps(sumVecs[i], zero), one);
                var squared = _mm256_mul_ps(clipped, clipped);
                _mm256_storeu_ps(&output[i * L3_CHUNK_SIZE], squared);
            }
        }

        public static void ActivateL3(float* inputs, float* weights, float bias, ref float output)
        {
            var sumVec = _mm256_set1_ps(0.0f);

            for (int i = 0; i < L3_SIZE; i += L3_CHUNK_SIZE)
            {
                var weightVec = _mm256_loadu_ps(&weights[i]);
                var inputsVec = _mm256_loadu_ps(&inputs[i]);
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

    }
}