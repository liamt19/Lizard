using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Lizard.Properties;


namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe class Simple768
    {
        public const int InputSize = 768;
        public const int HiddenSize = 1024;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;

        public const int SIMD_CHUNKS = HiddenSize / VSize.Short;

#if DEV_NET
        public const string NetworkName = "net-wdl3-epoch20.bin";
#else
        public const string NetworkName = "net-epoch10.bin";
#endif

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

        private const int FeatureWeightElements = InputSize * HiddenSize;
        private const int FeatureBiasElements = HiddenSize;

        private const int LayerWeightElements = HiddenSize * 2;
        private const int LayerBiasElements = OutputBuckets;

        static Simple768()
        {
            Initialize();
        }

        public static void Initialize()
        {
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
            long toRead = sizeof(short) * (FeatureWeightElements + FeatureBiasElements + LayerWeightElements * OutputBuckets + LayerBiasElements);
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("Simple768's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid 768 network, or has different layer sizes than the hardcoded ones.");
                Console.ReadLine();
                Environment.Exit(-1);
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
                    LayerBiases[i / VSize.Short] = Vector256.Load(biasPtr + i * VSize.Short);
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

        public static void RefreshAccumulator(Position pos) => RefreshAccumulator(pos, ref *pos.State->Accumulator);
        public static void RefreshAccumulator(Position pos, ref Accumulator accumulator)
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
        }

        public static int GetEvaluation(Position pos) => GetEvaluation(pos, ref *pos.State->Accumulator);

        public static int GetEvaluation(Position pos, ref Accumulator accumulator)
        {
            Vector256<short> ClampMax = Vector256.Create((short)QA);
            int output = 0;

            if (AvxVnni.IsSupported)
            {
                Vector256<int> vnniSum = Vector256<int>.Zero;

                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    //  Clamp each feature between [0, QA]
                    Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[pos.ToMove][i]));

                    //  Multiply the clamped feature by its corresponding weight.
                    //  We can do this with short values since the weights are always between [-127, 127]
                    //  (and the product will always be < short.MaxValue) so this will never overflow.
                    Vector256<short> mult = clamp * LayerWeights[i];


                    //  We can use VPDPWSSD to do the multiplication of mult and clamp,
                    //  as well as the horizontal accumulation of it into the sum in a single instruction.

                    //  Since the accumulation is happening via 32-bit integers,
                    //  we would only need to worry about overflowing if we were summing short.MaxValue 2^16 times.
                    vnniSum = AvxVnni.MultiplyWideningAndAdd(vnniSum, mult, clamp);
                }

                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[Not(pos.ToMove)][i]));
                    Vector256<short> mult = clamp * LayerWeights[i + SIMD_CHUNKS];
                    vnniSum = AvxVnni.MultiplyWideningAndAdd(vnniSum, mult, clamp);
                }

                output = SumVector256NoHadd(vnniSum);
            }
            else
            {
                Vector256<int> normalSum = Vector256<int>.Zero;

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

                    //  Add the sum of loMult and hiMult to the summation vector.
                    normalSum = Avx2.Add(normalSum, Avx2.Add(loMult, hiMult));
                }

                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    Vector256<short> clamp = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, accumulator[Not(pos.ToMove)][i]));
                    Vector256<short> mult = clamp * LayerWeights[i + SIMD_CHUNKS];

                    Vector256<int> loMult = Avx2.MultiplyLow(
                        Avx2.ConvertToVector256Int32(mult.GetLower().AsInt16()),
                        Avx2.ConvertToVector256Int32(clamp.GetLower().AsInt16()));

                    Vector256<int> hiMult = Avx2.MultiplyLow(
                        Avx2.ConvertToVector256Int32(mult.GetUpper().AsInt16()),
                        Avx2.ConvertToVector256Int32(clamp.GetUpper().AsInt16()));

                    normalSum = Avx2.Add(normalSum, Avx2.Add(loMult, hiMult));
                }

                //  Now sum the summation vector, preferably without vphaddd (which Vector256.Sum appears to use)
                //  because it can be quite a bit slower on some architectures.
                output = SumVector256NoHadd(normalSum);
            }

            return (output / QA + LayerBiases[0][0]) * OutputScale / QAB;
        }


        [MethodImpl(Inline)]
        private static int FeatureIndex(int pc, int pt, int sq, int perspective)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            return (pc ^ perspective) * ColorStride + pt * PieceStride + (sq ^ perspective * 56);
        }



        [MethodImpl(Inline)]
        private static (int, int) FeatureIndex(int pc, int pt, int sq)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int whiteIndex = pc * ColorStride + pt * PieceStride + sq;
            int blackIndex = Not(pc) * ColorStride + pt * PieceStride + (sq ^ 56);

            return (whiteIndex, blackIndex);
        }



        [MethodImpl(Inline)]
        private static void AddToAll(Vector256<short>* input, Vector256<short>* delta, int offset)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                input[i] = Avx2.Add(input[i], delta[offset + i]);
            }
        }



        [MethodImpl(Inline)]
        private static void SubtractFromAll(Vector256<short>* input, Vector256<short>* delta, int offset)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                input[i] = Avx2.Subtract(input[i], delta[offset + i]);
            }
        }

        public static void MakeMoveNN(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            Accumulator* accumulator = pos.NextState->Accumulator;
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
        private static void MoveFeature(Vector256<short>* accumulation, int indexFrom, int indexTo)
        {
            SubtractFromAll(accumulation, FeatureWeights, indexFrom * SIMD_CHUNKS);
            AddToAll(accumulation, FeatureWeights, indexTo * SIMD_CHUNKS);
        }


        [MethodImpl(Inline)]
        private static void AddFeature(Vector256<short>* accumulation, int index)
        {
            AddToAll(accumulation, FeatureWeights, index * SIMD_CHUNKS);
        }

        [MethodImpl(Inline)]
        private static void RemoveFeature(Vector256<short>* accumulation, int index)
        {
            SubtractFromAll(accumulation, FeatureWeights, index * SIMD_CHUNKS);
        }



        [MethodImpl(Inline)]
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




        public static void Trace(Position pos)
        {
            char[][] board = new char[3 * 8 + 1][];
            for (int i = 0; i < 3 * 8 + 1; i++)
            {
                board[i] = new char[8 * 8 + 2];
                Array.Fill(board[i], ' ');
            }

            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                board[row][8 * 8 + 1] = '\0';
            }

            int baseEval = GetEvaluation(pos);

            Log("\nNNUE evaluation: " + baseEval + "\n");

            ref Accumulator Accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;
            for (int f = Files.A; f <= Files.H; f++)
            {
                for (int r = 0; r <= 7; r++)
                {
                    int idx = CoordToIndex(f, r);
                    int pt = bb.GetPieceAtIndex(idx);
                    int pc = bb.GetColorAtIndex(idx);
                    int fishPc = pt + 1 + pc * 8;
                    int v = ScoreMate;

                    if (pt != None && bb.GetPieceAtIndex(idx) != King)
                    {
                        bb.RemovePiece(idx, pc, pt);

                        RefreshAccumulator(pos);
                        int eval = GetEvaluation(pos);
                        v = baseEval - eval;

                        bb.AddPiece(idx, pc, pt);
                    }

                    writeSquare(board, f, r, fishPc, v);
                }
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }
        }

        public static void TracePieceValues(int pieceType, int pieceColor)
        {
            char[][] board = new char[3 * 8 + 1][];
            for (int i = 0; i < 3 * 8 + 1; i++)
            {
                board[i] = new char[8 * 8 + 2];
                Array.Fill(board[i], ' ');
            }

            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                board[row][8 * 8 + 1] = '\0';
            }

            //  White king on A1, black king on H8
            Position pos = new Position("7k/8/8/8/8/8/8/K7 w - - 0 1", true, owner: SearchPool.MainThread);
            int baseEval = GetEvaluation(pos);

            Log("\nNNUE evaluation: " + baseEval + "\n");

            ref Bitboard bb = ref pos.bb;

            for (int i = 0; i < SquareNB; i++)
            {
                if (bb.GetPieceAtIndex(i) != None)
                {

                    int fp = bb.GetPieceAtIndex(i) + 1 + bb.GetColorAtIndex(i) * 8;
                    writeSquare(board, GetIndexFile(i), GetIndexRank(i), fp, ScoreMate);
                    continue;
                }

                bb.AddPiece(i, pieceColor, pieceType);
                RefreshAccumulator(pos);
                int eval = GetEvaluation(pos);
                bb.RemovePiece(i, pieceColor, pieceType);

                writeSquare(board, GetIndexFile(i), GetIndexRank(i), pieceType + 1 + pieceColor * 8, eval);
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }

            Log("\n");
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

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(xSize, ySize);

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

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(xSize, ySize);

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

            Log(layerName + "\tmin: " + min + ", max: " + max + ", avg: " + (double)avg / n);
        }



        private static void writeSquare(char[][] board, int file, int rank, int pc, int value)
        {
            const string PieceToChar = " PNBRQK  pnbrqk";

            int x = file * 8;
            int y = (7 - rank) * 3;

            for (int i = 1; i < 8; i++)
            {
                board[y][x + i] = board[y + 3][x + i] = '-';
            }

            for (int i = 1; i < 3; i++)
            {
                board[y + i][x] = board[y + i][x + 8] = '|';
            }

            board[y][x] = board[y][x + 8] = board[y + 3][x + 8] = board[y + 3][x] = '+';

            if (pc != 0 && !(pc == 15 && value == ScoreMate))
            {
                board[y + 1][x + 4] = PieceToChar[pc];
            }

            if (value != ScoreMate)
            {
                fixed (char* ptr = &board[y + 2][x + 2])
                {
                    format_cp_ptr(value, ptr);
                }
            }

        }


        private static void format_cp_ptr(int v, char* buffer)
        {
            buffer[0] = v < 0 ? '-' : v > 0 ? '+' : ' ';

            //  This reduces the displayed value of each piece so that it is more in line with
            //  conventional piece values, i.e. pawn = ~100, bishop/knight = ~300, rook = ~500
            const int Normalization = 200;
            int cp = Math.Abs(100 * v / Normalization);

            if (cp >= 10000)
            {
                buffer[1] = (char)('0' + cp / 10000); cp %= 10000;
                buffer[2] = (char)('0' + cp / 1000); cp %= 1000;
                buffer[3] = (char)('0' + cp / 100); cp %= 100;
                buffer[4] = ' ';
            }
            else if (cp >= 1000)
            {
                buffer[1] = (char)('0' + cp / 1000); cp %= 1000;
                buffer[2] = (char)('0' + cp / 100); cp %= 100;
                buffer[3] = '.';
                buffer[4] = (char)('0' + cp / 10);
            }
            else
            {
                buffer[1] = (char)('0' + cp / 100); cp %= 100;
                buffer[2] = '.';
                buffer[3] = (char)('0' + cp / 10); cp %= 10;
                buffer[4] = (char)('0' + cp / 1);
            }
        }


        private static int ConvertRange(int originalStart, int originalEnd, int newStart, int newEnd, int value)
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (int)(newStart + (value - originalStart) * scale);
        }
    }
}
