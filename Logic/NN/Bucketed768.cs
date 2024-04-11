using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static Lizard.Logic.NN.FunUnrollThings;

namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe partial class Bucketed768
    {
        private const int InputBuckets = 4;
        public const int InputSize = 768;
        public const int HiddenSize = 1024;
        private const int OutputBuckets = 8;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;
        private const bool SelectOutputBucket = (OutputBuckets != 1);

        public const int SIMD_CHUNKS = HiddenSize / VSize.Short;

        /// <summary>
        /// 
        /// (768x4 -> 1024)x2 -> 8
        /// 
        /// </summary>
        public const string NetworkName = "lizard-1024_4_8_gauss-600.bin";

        /// <summary>
        /// The values applied according to the active features and current bucket.
        /// <para></para>
        /// This is the 768 -> 1024 part of the architecture.
        /// </summary>
        public static readonly Vector256<short>* FeatureWeights;

        /// <summary>
        /// The initial values that are placed into the accumulators.
        /// <para></para>
        /// When doing a full refresh, both accumulators are filled with these.
        /// </summary>
        public static readonly Vector256<short>* FeatureBiases;

        /// <summary>
        /// The values that are multiplied with the SCRelu-activated output from the feature transformer 
        /// to produce the final sum.
        /// <para></para>
        /// This is the (1024)x2 -> 1 part.
        /// </summary>
        public static readonly Vector256<short>* LayerWeights;

        /// <summary>
        /// The value(s) applied to the final output.
        /// <para></para>
        /// There is exactly 1 bias for each output bucket, so this currently contains only 1 number (followed by 15 zeroes).
        /// </summary>
        public static readonly Vector256<short>* LayerBiases;

        private const int FeatureWeightElements = InputSize * HiddenSize * InputBuckets;
        private const int FeatureBiasElements = HiddenSize;

        private const int LayerWeightElements = HiddenSize * 2 * OutputBuckets;
        private const int LayerBiasElements = OutputBuckets;

        public static long ExpectedNetworkSize => (FeatureWeightElements + FeatureBiasElements + LayerWeightElements + LayerBiasElements) * sizeof(short);

        private static readonly int[] KingBuckets =
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

        static Bucketed768()
        {
            FeatureWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);
            FeatureBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureBiasElements);

            LayerWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);
            LayerBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * (nuint)Math.Max(LayerBiasElements, VSize.Short));

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

            for (int i = 0; i < FeatureWeightElements / VSize.Short; i++)
            {
                FeatureWeights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            for (int i = 0; i < FeatureBiasElements / VSize.Short; i++)
            {
                FeatureBiases[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            for (int i = 0; i < LayerWeightElements / VSize.Short; i++)
            {
                LayerWeights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            //  These weights are stored in column major order, but they are easier to use in row major order.
            //  The first 8 weights in the binary file are actually the first weight for each of the 8 output buckets,
            //  so we will transpose them so that the all of the weights for each output bucket are contiguous.
            TransposeLayerWeights((short*)LayerWeights, HiddenSize * 2, OutputBuckets);

            //  Round LayerBiasElements to the next highest multiple of VSize.Short
            //  i.e. if LayerBiasElements is <= 15, totalBiases = 16.
            int totalBiases = ((LayerBiasElements + VSize.Short - 1) / VSize.Short) * VSize.Short;

            short[] _Biases = new short[totalBiases];
            Array.Fill(_Biases, (short)0);

            for (int i = 0; i < LayerBiasElements; i++)
            {
                _Biases[i] = br.ReadInt16();
            }

            for (int i = 0; i < totalBiases / VSize.Short; i++)
            {
                LayerBiases[i] = Vector256.Create(_Biases, (i * VSize.Short));
            }

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
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            Unsafe.CopyBlock(accumulator.White, FeatureBiases, sizeof(short) * HiddenSize);
            Unsafe.CopyBlock(accumulator.Black, FeatureBiases, sizeof(short) * HiddenSize);

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
                    accumulator.White[i] = Avx2.Add(accumulator.White[i], FeatureWeights[wIdx + i]);
                    accumulator.Black[i] = Avx2.Add(accumulator.Black[i], FeatureWeights[bIdx + i]);
                }
            }

            accumulator.NeedsRefresh[White] = accumulator.NeedsRefresh[Black] = false;
        }

        public static void RefreshAccumulatorPerspective(Position pos, int perspective)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var ourAccumulation = accumulator[perspective];
            Unsafe.CopyBlock(ourAccumulation, FeatureBiases, sizeof(short) * HiddenSize);

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
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, Black);
            }

            Vector256<short> ClampMax = Vector256.Create((short)QA);
            Vector256<int> normalSum = Vector256<int>.Zero;

            int outputBucket = (int)((popcount(pos.bb.Occupancy) - 2) / 4);
            Vector256<short>* bucketWeights = LayerWeights + (outputBucket * (SIMD_CHUNKS * 2));

            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                //  Clamp each feature between [0, QA]
                Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[pos.ToMove][i]));

                //  Multiply the clamped feature by its corresponding weight.
                //  We can do this with short values since the weights are always between [-127, 127]
                //  (and the product will always be < short.MaxValue) so this will never overflow.
                Vector256<short> mult = clamp * bucketWeights[i];
                normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(mult, clamp));
            }

            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[Not(pos.ToMove)][i]));
                Vector256<short> mult = clamp * bucketWeights[i + SIMD_CHUNKS];
                normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(mult, clamp));
            }

            //  Now sum the summation vector, preferably without vphaddd (which Vector256.Sum appears to use)
            //  because it can be quite a bit slower on some architectures.
            int output = SumVector256NoHadd(normalSum);

            return (output / QA + LayerBiases[0][outputBucket]) * OutputScale / QAB;
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

            var whiteAccumulation = (*accumulator)[White];
            var blackAccumulation = (*accumulator)[Black];

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                accumulator->NeedsRefresh[us] = true;

                var theirAccumulation = (*accumulator)[them];
                int theirKing = pos.State->KingSquares[them];

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None && !m.Castle)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);

                    SubSubAdd((short*)theirAccumulation,
                        (short*)(FeatureWeights + from),
                        (short*)(FeatureWeights + cap),
                        (short*)(FeatureWeights + to));
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
                    SubAdd((short*)theirAccumulation,
                        (short*)(FeatureWeights + from),
                        (short*)(FeatureWeights + to));
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

                    SubSubAdd((short*)whiteAccumulation,
                        (short*)(FeatureWeights + wFrom),
                        (short*)(FeatureWeights + wCap),
                        (short*)(FeatureWeights + wTo));

                    SubSubAdd((short*)blackAccumulation,
                        (short*)(FeatureWeights + bFrom),
                        (short*)(FeatureWeights + bCap),
                        (short*)(FeatureWeights + bTo));
                }
                else if (m.EnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    SubSubAdd((short*)whiteAccumulation,
                        (short*)(FeatureWeights + wFrom),
                        (short*)(FeatureWeights + wCap),
                        (short*)(FeatureWeights + wTo));

                    SubSubAdd((short*)blackAccumulation,
                        (short*)(FeatureWeights + bFrom),
                        (short*)(FeatureWeights + bCap),
                        (short*)(FeatureWeights + bTo));
                }
                else
                {
                    SubAdd((short*)whiteAccumulation,
                        (short*)(FeatureWeights + wFrom),
                        (short*)(FeatureWeights + wTo));

                    SubAdd((short*)blackAccumulation,
                        (short*)(FeatureWeights + bFrom),
                        (short*)(FeatureWeights + bTo));
                }
            }
        }


        private static void SubSubAddAdd(Vector256<short>* src, Vector256<short>* sub1, Vector256<short>* sub2, Vector256<short>* add1, Vector256<short>* add2)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Subtract(Avx2.Add(Avx2.Add(src[i], add1[i]), add2[i]), sub1[i]), sub2[i]);
            }
        }



        private static int SumVector256NoHadd(Vector256<int> vect)
        {
            Vector128<int> lo = vect.GetLower();
            Vector128<int> hi = Avx.ExtractVector128(vect, 1);
            Vector128<int> sum128 = Sse2.Add(lo, hi);

            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, 0b_10_11_00_01));
            sum128 = Sse2.Add(sum128, Sse2.Shuffle(sum128, 0b_01_00_11_10));

            //  Something along the lines of Add(sum128, UnpackHigh(sum128, sum128))
            //  would also work here but it is occasionally off by +- 1.
            //  The JIT also seems to replace the unpack with a shuffle anyways depending on the instruction order,
            //  and who am I to not trust the JIT? :)

            return Sse2.ConvertToInt32(sum128);
        }


        /// <summary>
        /// Transposes the weights stored in <paramref name="block"/>
        /// </summary>
        private static void TransposeLayerWeights(short* block, int columnLength, int rowLength)
        {
            short* temp = stackalloc short[columnLength * rowLength];
            Unsafe.CopyBlock(temp, block, (uint)(sizeof(short) * columnLength * rowLength));

            for (int bucket = 0; bucket < rowLength; bucket++)
            {
                short* thisBucket = block + (bucket * columnLength);

                for (int i = 0; i < columnLength; i++)
                {
                    thisBucket[i] = temp[(rowLength * i) + bucket];
                }
            }
        }





        public static void DrawFeatureWeightPic(bool doAll = false)
        {
#if DEBUG
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //  This uses System.Drawing, which can't be used on non-Windows machines.
                //  Non-Windows people, just use your imaginations I guess
                return;
            }

            const int SquaresPerRow = 8;

            const int PictureScale = 8;
            const int BorderSize = 1;

            const int PerspNB = 1;

            const int PaddingBotRight = (BorderSize * PictureScale);

            //  Each of the 'PieceNB' boards are given '8 + BorderSize' pixels, multiplied by 'PictureScale'.
            const int xSize = (SquaresPerRow + BorderSize) * PictureScale * PieceNB + PaddingBotRight;

            //  There are 2 rows of pieces, one for white and one for black.
            //  There can be an additional 2 rows for the same white and black pieces but from black's perspective (if PerspNB == 2).
            const int ySize = (SquaresPerRow + BorderSize) * PictureScale * ColorNB * PerspNB + PaddingBotRight;

            string fDir = Environment.CurrentDirectory;
            int epoch = 0;
            string[] nets = Directory.GetFiles(fDir, "*.bin");
            doAll = doAll && nets.Length > 0;

            Vector256<short>* tempFeatureWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);

        AllLoop:

            if (doAll)
            {
                if (epoch >= nets.Length)
                {
                    return;
                }

                using var fs = File.OpenRead(nets[epoch]);
                using BinaryReader br = new BinaryReader(fs);

                for (int j = 0; j < (FeatureWeightElements / VSize.Short); j++)
                {
                    tempFeatureWeights[j] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }
            }
            else
            {
                //  Use the current network's weights
                Unsafe.CopyBlock(tempFeatureWeights, FeatureWeights, sizeof(short) * FeatureWeightElements);
            }

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(xSize, ySize);

            var FTWeights = (short*)tempFeatureWeights;

            for (int perspective = 0; perspective < PerspNB; perspective++)
            {
                int ksq = perspective == White ? Squares.E1 : Squares.E8;
                for (int pc = 0; pc < 2; pc++)
                {
                    for (int pt = 0; pt < PieceNB; pt++)
                    {
                        int[] sums = new int[64];
                        for (int sq = 0; sq < SquareNB; sq++)
                        {
                            int sum = 0;

                            int idx = FeatureIndexSingle(pc, pt, sq, ksq, perspective);
                            short* weights = (FTWeights + idx);

                            for (int i = 0; i < HiddenSize; i++)
                            {
                                sum += weights[i];
                            }
                            sums[sq] = sum;
                        }

                        int min = sums.Min();
                        int max = sums.Max();

                        for (int sq = 0; sq < 64; sq++)
                        {
                            int x = (7 - GetIndexFile(sq)) + (pt * SquaresPerRow) + ((pt + 1) * BorderSize);
                            int y = (7 - GetIndexRank(sq)) + (pc * SquaresPerRow) + ((pc + 1) * BorderSize) + (perspective * (2 * (SquaresPerRow + BorderSize)));

                            x *= PictureScale;
                            y *= PictureScale;

                            int rVal = 127;
                            int gVal = 127;
                            int bVal = 127;

                            if (sums[sq] < 0)
                            {
                                rVal += ConvertRange(min, 0, 128, 0, sums[sq]);
                            }

                            if (sums[sq] > 0)
                            {
                                gVal += ConvertRange(0, max, 0, 128, sums[sq]);
                            }

                            for (int ix = 0; ix < PictureScale; ix++)
                            {
                                for (int iy = 0; iy < PictureScale; iy++)
                                {
                                    pic.SetPixel(x + ix, y + iy, System.Drawing.Color.FromArgb(255, rVal, gVal, bVal));
                                }
                            }


                        }

                    }
                }
            }

            if (doAll)
            {
                pic.Save(fDir + "\\feature_transformer_weights_" + (epoch + 1) + ".png");
                epoch++;
                goto AllLoop;
            }
            else
            {
                pic.Save("feature_transformer_weights_pic.png");
            }

            NativeMemory.AlignedFree(tempFeatureWeights);
#endif
        }

        public static void DrawLayerWeightPic(bool doAll = false)
        {
#if DEBUG
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //  This uses System.Drawing, which can't be used on non-Windows machines.
                //  Non-Windows people, just use your imaginations I guess
                return;
            }

            const int PictureScale = 8;
            const int RowLength = 16;
            const int BorderSize = 1;
            const int DividerSize = 2;

            const int RowSizePixels = (RowLength * 2 + (BorderSize * 4));

            //  Each of the 'PieceNB' boards are given '8 + BorderSize' pixels, multiplied by 'PictureScale'.
            const int xSize = ((OutputBuckets) * (RowLength + BorderSize + RowLength + (BorderSize * 3))) * PictureScale;

            //  This depends on HiddenSize.
            const int ySize = (HiddenSize / RowLength) * PictureScale;

            string fDir = Environment.CurrentDirectory;
            int epoch = 0;
            string[] nets = Directory.GetFiles(fDir, "*.bin");
            doAll = doAll && nets.Length > 0;

            Vector256<short>* tempLayerWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);

        AllLoop:
        
            if (doAll)
            {
                if (epoch >= nets.Length)
                {
                    return;
                }

                using var fs = File.OpenRead(nets[epoch]);
                using BinaryReader br = new BinaryReader(fs);

                for (int j = 0; j < (FeatureWeightElements / VSize.Short); j++)
                {
                    br.ReadInt64(); br.ReadInt64(); br.ReadInt64(); br.ReadInt64();
                }

                for (int i = 0; i < (FeatureBiasElements / VSize.Short); i++)
                {
                    br.ReadInt64(); br.ReadInt64(); br.ReadInt64(); br.ReadInt64();
                }

                for (int i = 0; i < (LayerWeightElements / VSize.Short); i++)
                {
                    tempLayerWeights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }

                TransposeLayerWeights((short*)tempLayerWeights, HiddenSize * 2, OutputBuckets);
            }
            else
            {
                //  Use the current network's weights
                Unsafe.CopyBlock(tempLayerWeights, LayerWeights, sizeof(short) * LayerWeightElements);
            }

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(xSize, ySize);

            for (int outputBucket = 0; outputBucket < OutputBuckets; outputBucket++)
            {
                for (int perspective = 0; perspective < 2; perspective++)
                {
                    var FCWeights = (short*)tempLayerWeights;
                    int[] sums = new int[HiddenSize];
                    for (int i = 0; i < HiddenSize; i++)
                    {
                        sums[i] = FCWeights[i + (HiddenSize * perspective) + (outputBucket * HiddenSize * 2)];
                    }

                    int min = sums.Min();
                    int max = sums.Max();

                    for (int i = 0; i < HiddenSize; i++)
                    {
                        //  i % RowLength maps to the "file" file.
                        //  (perspective * RowLength) maps to the perspective.
                        //  
                        int x = (outputBucket * (RowLength + BorderSize + RowLength + (BorderSize * 3))) + (i % RowLength) + (perspective * RowLength) + (perspective * BorderSize);
                        int y = i / RowLength;

                        x *= PictureScale;
                        y *= PictureScale;

                        int rVal = 127;
                        int gVal = 127;
                        int bVal = 127;

                        if (sums[i] < 0)
                        {
                            rVal += ConvertRange(min, 0, 128, 0, sums[i]);
                        }

                        if (sums[i] > 0)
                        {
                            gVal += ConvertRange(0, max, 0, 128, sums[i]);
                        }

                        for (int ix = 0; ix < PictureScale; ix++)
                        {
                            for (int iy = 0; iy < PictureScale; iy++)
                            {
                                pic.SetPixel(x + ix, y + iy, System.Drawing.Color.FromArgb(255, rVal, gVal, bVal));
                            }
                        }
                    }
                }
            }

            if (doAll)
            {
                pic.Save(fDir + "\\hidden_layer_weights_" + (epoch + 1) + ".png");
                epoch++;
                goto AllLoop;
            }
            else
            {
                pic.Save("hidden_layer_weights_pic.png");
            }

            NativeMemory.AlignedFree(tempLayerWeights);
#endif
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