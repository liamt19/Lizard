

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

using LTChess.Properties;


namespace LTChess.Logic.NN.Simple768
{
    [SkipStaticConstructor]
    public unsafe static class Simple768
    {
        public const int InputSize = 768;
        public const int HiddenSize = 1024;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = (QA * QB);

        public const int OutputScale = 400;

        public const int SIMD_CHUNKS = (HiddenSize / VSize.Short);

        public const string NetworkName = "net-epoch10.bin";


        /*
        May be marginally better at 10+0.1
        Score of WDL_Net vs Baseline: 150 - 135 - 267  [0.514] 552
        ...      WDL_Net playing White: 131 - 31 - 114  [0.681] 276
        ...      WDL_Net playing Black: 19 - 104 - 153  [0.346] 276
        ...      White vs Black: 235 - 50 - 267  [0.668] 552
        Elo difference: 9.4 +/- 20.8, LOS: 81.3 %, DrawRatio: 48.4 %
        SPRT: llr 0.392 (13.6%), lbound -2.25, ubound 2.89
        */
        public const string TestNetworkName = "net-wdl3-epoch20";

        /// <summary>
        /// The values applied according to the active features and current bucket.
        /// </summary>
        public static Vector256<short>* FeatureWeights;

        /// <summary>
        /// The initial values that are placed into the accumulators.
        /// </summary>
        public static Vector256<short>* FeatureBiases;

        public static Vector256<short>* LayerWeights;
        public static Vector256<short>* LayerBiases;

        private const int FeatureWeightElements = (InputSize * HiddenSize);
        private const int FeatureBiasElements = (HiddenSize);

        private const int LayerWeightElements = (HiddenSize * 2);
        private const int LayerBiasElements = OutputBuckets;

        public static void Initialize()
        {
            if (!UseSimple768)
            {
                return;
            }

            FeatureWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);
            FeatureBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureBiasElements);

            LayerWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);
            LayerBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * (nuint)Math.Max(LayerBiasElements, VSize.Short));


            Stream kpFile;

            string networkToLoad = @"nn.nnue";
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
#if USE_TEST_NET
                networkToLoad = TestNetworkName;
                Log("Using NNUE with 768 network " + TestNetworkName);
#else
                networkToLoad = NetworkName;
                Log("Using NNUE with 768 network " + NetworkName);
#endif

                string resourceName = (networkToLoad.Replace(".nnue", string.Empty).Replace(".bin", string.Empty));

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
            long toRead = (sizeof(short) * (FeatureWeightElements + FeatureBiasElements + (LayerWeightElements * OutputBuckets) + LayerBiasElements));
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("Simple768's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid 768 network, or has different layer sizes than the hardcoded ones.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            for (int i = 0; i < (FeatureWeightElements / VSize.Short); i++)
            {
                FeatureWeights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            for (int i = 0; i < (FeatureBiasElements / VSize.Short); i++)
            {
                FeatureBiases[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            for (int i = 0; i < (LayerWeightElements / VSize.Short); i++)
            {
                LayerWeights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
            }

            int totalBiases = Math.Max(LayerBiasElements, VSize.Short);

            short[] _Biases = new short[totalBiases];
            Array.Fill(_Biases, (short)0);

            for (int i = 0; i < LayerBiasElements; i++)
            {
                _Biases[i] = br.ReadInt16();
            }

            fixed (short* biasPtr = &_Biases[0])
            {
                for (int i = 0; i < totalBiases; i += VSize.Short)
                {
                    LayerBiases[i / VSize.Short] = Vector256.Load(biasPtr + (i * VSize.Short));
                }
            }

#if DEBUG
            NetStats("ft weight", FeatureWeights, FeatureWeightElements);
            NetStats("ft bias\t", FeatureBiases, FeatureBiasElements);

            NetStats("fc weight", LayerWeights, LayerWeightElements);
            NetStats("fc bias", LayerBiases, 1);

            Log("Init Simple768 done");
#endif
        }

        public static void RefreshAccumulator(Position pos) => RefreshAccumulator(pos, ref *(pos.State->Accumulator));
        public static void RefreshAccumulator(Position pos, ref AccumulatorPSQT accumulator)
        {
            ref Bitboard bb = ref pos.bb;

            Unsafe.CopyBlock(accumulator.White, FeatureBiases, sizeof(short) * HiddenSize);
            Unsafe.CopyBlock(accumulator.Black, FeatureBiases, sizeof(short) * HiddenSize);

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                AddFeature(accumulator.White, FeatureIndex(pc, pt, pieceIdx, White));
                AddFeature(accumulator.Black, FeatureIndex(pc, pt, pieceIdx, Black));
            }

            accumulator.RefreshPerspective[White] = false;
            accumulator.RefreshPerspective[Black] = false;
        }

        public static int GetEvaluation(Position pos) => GetEvaluation(pos, ref (*pos.State->Accumulator));

        public static int GetEvaluation(Position pos, ref AccumulatorPSQT accumulator)
        {
            int output = 0;

            Vector256<short> ClampMax = Vector256.Create((short)QA);

            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                //  Clamp each feature between [0, QA]
                Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[pos.ToMove][i]));

                //  Multiply the clamped feature by its corresponding weight.
                //  We can do this with short values since the weights are always between [-127, 127]
                //  (and the product will always be < short.MaxValue) so this will never overflow.
                Vector256<short> mult = clamp * LayerWeights[i];


                //  We want _mm256_mullo_epi32(_mm256_cvtepi16_epi32(_mm256_castsi256_si128(mult)), ...) here
                //  Vector256.Widen(mult) generates almost exactly the same code but I'd rather write it out
                //  so I can be disappointed when the JIT decides to use other intrinsics.

                //  With this approach we will need to widen both vectors before doing the squared part of the activation
                //  so that this multiplication step is done with integers and not shorts.
                Vector256<int> loMult = Avx2.MultiplyLow(
                    Avx2.ConvertToVector256Int32(mult.GetLower().AsInt16()),
                    Avx2.ConvertToVector256Int32(clamp.GetLower().AsInt16()));

                Vector256<int> hiMult = Avx2.MultiplyLow(
                    Avx2.ConvertToVector256Int32(mult.GetUpper().AsInt16()),
                    Avx2.ConvertToVector256Int32(clamp.GetUpper().AsInt16()));

                //  Now sum the low and high vectors horizontally, preferably without vphaddd (which Vector256.Sum appears to use)
                //  because it can be quite a bit slower on some architectures.
                output += SumVector256NoHadd(loMult);
                output += SumVector256NoHadd(hiMult);
            }

            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[Not(pos.ToMove)][i]));
                Vector256<short> mult = clamp * LayerWeights[i + (SIMD_CHUNKS)];

                Vector256<int> loMult = Avx2.MultiplyLow(
                    Avx2.ConvertToVector256Int32(mult.GetLower().AsInt16()),
                    Avx2.ConvertToVector256Int32(clamp.GetLower().AsInt16()));

                Vector256<int> hiMult = Avx2.MultiplyLow(
                    Avx2.ConvertToVector256Int32(mult.GetUpper().AsInt16()),
                    Avx2.ConvertToVector256Int32(clamp.GetUpper().AsInt16()));

                output += SumVector256NoHadd(loMult);
                output += SumVector256NoHadd(hiMult);
            }

            return (output / QA + LayerBiases[0][0]) * OutputScale / (QAB);
        }


        [MethodImpl(Inline)]
        public static int FeatureIndex(int pc, int pt, int sq, int perspective)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            return ((pc ^ perspective) * ColorStride) + (pt * PieceStride) + (sq ^ (perspective * 56));
        }



        [MethodImpl(Inline)]
        public static (int, int) FeatureIndex(int pc, int pt, int sq)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int whiteIndex = (pc * ColorStride) + (pt * PieceStride) + sq;
            int blackIndex = (Not(pc) * ColorStride) + (pt * PieceStride) + (sq ^ 56);

            return (whiteIndex, blackIndex);
        }



        [MethodImpl(Inline)]
        public static void AddToAll(Vector256<short>* input, Vector256<short>* delta, int offset)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                input[i] = Avx2.Add(input[i], delta[offset + i]);
            }
        }



        [MethodImpl(Inline)]
        public static void SubtractFromAll(Vector256<short>* input, Vector256<short>* delta, int offset)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                input[i] = Avx2.Subtract(input[i], delta[offset + i]);
            }
        }

        public static void MakeMoveNN(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            AccumulatorPSQT* accumulator = pos.NextState->Accumulator;
            pos.State->Accumulator->CopyTo(accumulator);

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = bb.GetColorAtIndex(moveFrom);
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            var whiteAccumulation = (*accumulator)[White];
            var blackAccumulation = (*accumulator)[Black];


            (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom);
            (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo);

            MoveFeature(whiteAccumulation, wFrom, wTo);
            MoveFeature(blackAccumulation, bFrom, bTo);

            if (theirPiece != None)
            {
                (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo);
                RemoveFeature(whiteAccumulation, wCap);
                RemoveFeature(blackAccumulation, bCap);
            }
            else if (m.EnPassant)
            {
                int idxPawn = moveTo - ShiftUpDir(us);

                (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn);

                RemoveFeature(whiteAccumulation, wCap);
                RemoveFeature(blackAccumulation, bCap);
            }
            else if (m.Castle)
            {
                int rookFrom = moveTo switch
                {
                    C1 => A1,
                    G1 => H1,
                    C8 => A8,
                    _ => H8,    //  G8 => H8
                };

                int rookTo = moveTo switch
                {
                    C1 => D1,
                    G1 => F1,
                    C8 => D8,
                    _ => F8,    //  G8 => F8
                };

                (int wRookFrom, int bRookFrom) = FeatureIndex(us, Rook, rookFrom);
                (int wRookTo, int bRookTo) = FeatureIndex(us, Rook, rookTo);

                MoveFeature(whiteAccumulation, wRookFrom, wRookTo);
                MoveFeature(blackAccumulation, bRookFrom, bRookTo);

            }

        }


        [MethodImpl(Inline)]
        public static void MoveFeature(Vector256<short>* accumulation, int indexFrom, int indexTo)
        {
            SubtractFromAll(accumulation, FeatureWeights, indexFrom * SIMD_CHUNKS);
            AddToAll(accumulation, FeatureWeights, indexTo * SIMD_CHUNKS);
        }


        [MethodImpl(Inline)]
        public static void AddFeature(Vector256<short>* accumulation, int index)
        {
            AddToAll(accumulation, FeatureWeights, index * SIMD_CHUNKS);
        }

        [MethodImpl(Inline)]
        public static void RemoveFeature(Vector256<short>* accumulation, int index)
        {
            SubtractFromAll(accumulation, FeatureWeights, index * SIMD_CHUNKS);
        }



        [MethodImpl(Inline)]
        public static int SumVector256NoHadd(Vector256<int> vect)
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

            Vector256<short>* featuresWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);

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
                    featuresWeights[j] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }
            }

            Bitmap pic = new Bitmap(xSize, ySize);

            var FTWeights = (short*)featuresWeights;

            for (int perspective = 0; perspective < PerspNB; perspective++)
            {
                for (int pc = 0; pc < 2; pc++)
                {
                    for (int pt = 0; pt < PieceNB; pt++)
                    {
                        int[] sums = new int[64];
                        for (int sq = 0; sq < SquareNB; sq++)
                        {
                            int sum = 0;

                            (int wIdx, int bIdx) = FeatureIndex(pc, pt, sq);
                            short* weights = (FTWeights + (wIdx * HiddenSize));
                            if (pc == Black)
                            {
                                weights = (FTWeights + (bIdx * HiddenSize));
                            }

                            for (int i = 0; i < HiddenSize; i++)
                            {
                                sum += weights[i];
                            }
                            sums[sq] = sum;
                        }

                        int min = sums.Min();

                        if (min < 0)
                        {
                            //  If there are negative weights, then move each weight up by the absolute value of the minimum.
                            for (int sq = 0; sq < SquareNB; sq++)
                            {
                                //sums[sq] = sums[sq] + Math.Abs(min);
                            }
                        }

                        min = sums.Min();
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
                pic.Save(fDir + "\\weights_" + (epoch + 1) + ".png");
                epoch++;
                goto AllLoop;
            }
            else
            {
                pic.Save("ft_weights_pic.png");
            }

            NativeMemory.AlignedFree(featuresWeights);
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
            Vector256<short>* weights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);

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
                    weights[i] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }
            }

            Bitmap pic = new Bitmap(xSize, ySize);

            for (int perspective = 0; perspective < 2; perspective++)
            {
                var FCWeights = (short*)weights;
                int[] sums = new int[HiddenSize];
                for (int i = 0; i < HiddenSize; i++)
                {
                    sums[i] = FCWeights[i + (HiddenSize * perspective)];
                }

                int min = sums.Min();
                int max = sums.Max();

                for (int i = 0; i < HiddenSize; i++)
                {
                    //  i % RowLength maps to the "file" file.
                    //  (perspective * RowLength) maps to the perspective.
                    //  
                    int x = (i % RowLength) + (perspective * RowLength) + (perspective * BorderSize);
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

            if (doAll)
            {
                pic.Save(fDir + "\\layers_" + (epoch + 1) + ".png");
                epoch++;
                goto AllLoop;
            }
            else
            {
                pic.Save("fc_weights_pic.png");
            }

            NativeMemory.AlignedFree(weights);
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

            Log(layerName + "\tmin: " + min + ", max: " + max + ", avg: " + ((double)avg / n));
        }

        private static int ConvertRange(int originalStart, int originalEnd, int newStart, int newEnd, int value)
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (int)(newStart + ((value - originalStart) * scale));
        }
    }
}
