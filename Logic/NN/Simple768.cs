using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using Lizard.Properties;


namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe class Simple768
    {
        public const int InputSize = 768;
        public const int HiddenSize = 1536;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;

        public const int SIMD_CHUNKS = HiddenSize / VSize.Short;

        public const string NetworkName = "iguana-epoch10.bin";

        /// <summary>
        /// The values applied according to the active features and current bucket.
        /// <para></para>
        /// This is the 768 -> 1536 part of the architecture.
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
        /// This is the (1536)x2 -> 1 part.
        /// </summary>
        public static readonly Vector256<short>* LayerWeights;

        /// <summary>
        /// The value(s) applied to the final output.
        /// <para></para>
        /// There is exactly 1 bias for each output bucket, so this currently contains only 1 number (followed by 15 zeroes).
        /// </summary>
        public static readonly Vector256<short>* LayerBiases;

        private const int FeatureWeightElements = InputSize * HiddenSize;
        private const int FeatureBiasElements = HiddenSize;

        private const int LayerWeightElements = HiddenSize * 2;
        private const int LayerBiasElements = OutputBuckets;

        static Simple768()
        {
            FeatureWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureWeightElements);
            FeatureBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * FeatureBiasElements);

            LayerWeights = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * LayerWeightElements);
            LayerBiases = (Vector256<short>*)AlignedAllocZeroed(sizeof(short) * (nuint)Math.Max(LayerBiasElements, VSize.Short));

            Initialize();
        }

        public static void Initialize()
        {
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
            NetStats("fc bias", LayerBiases, 1);

            Log("Init Simple768 done");
#endif
        }

        public static void RefreshAccumulator(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            Unsafe.CopyBlock(accumulator.White, FeatureBiases, sizeof(short) * HiddenSize);
            Unsafe.CopyBlock(accumulator.Black, FeatureBiases, sizeof(short) * HiddenSize);

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                (int wIdx, int bIdx) = FeatureIndex(pc, pt, pieceIdx);
                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    accumulator.White[i] = Avx2.Add(accumulator.White[i], FeatureWeights[wIdx + i]);
                    accumulator.Black[i] = Avx2.Add(accumulator.Black[i], FeatureWeights[bIdx + i]);
                }
            }
        }

        public static int GetEvaluation(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            Vector256<short> ClampMax = Vector256.Create((short)QA);
            Vector256<int> normalSum = Vector256<int>.Zero;

            var ourData = (short*)(accumulator[pos.ToMove]);
            var ourWeights = (short*)(LayerWeights);
            var theirData = (short*)(accumulator[Not(pos.ToMove)]);
            var theirWeights = (short*)(LayerWeights + SIMD_CHUNKS);


            Vector256<short> clamp_us_0 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 0)));
            Vector256<short> clamp_us_16 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 16)));
            Vector256<short> clamp_us_32 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 32)));
            Vector256<short> clamp_us_48 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 48)));
            Vector256<short> clamp_us_64 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 64)));
            Vector256<short> clamp_us_80 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 80)));
            Vector256<short> clamp_us_96 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 96)));
            Vector256<short> clamp_us_112 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 112)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_0, Avx2.MultiplyLow(clamp_us_0, Avx2.LoadAlignedVector256(ourWeights + 0))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_16, Avx2.MultiplyLow(clamp_us_16, Avx2.LoadAlignedVector256(ourWeights + 16))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_32, Avx2.MultiplyLow(clamp_us_32, Avx2.LoadAlignedVector256(ourWeights + 32))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_48, Avx2.MultiplyLow(clamp_us_48, Avx2.LoadAlignedVector256(ourWeights + 48))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_64, Avx2.MultiplyLow(clamp_us_64, Avx2.LoadAlignedVector256(ourWeights + 64))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_80, Avx2.MultiplyLow(clamp_us_80, Avx2.LoadAlignedVector256(ourWeights + 80))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_96, Avx2.MultiplyLow(clamp_us_96, Avx2.LoadAlignedVector256(ourWeights + 96))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_112, Avx2.MultiplyLow(clamp_us_112, Avx2.LoadAlignedVector256(ourWeights + 112))));

            Vector256<short> clamp_us_128 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 128)));
            Vector256<short> clamp_us_144 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 144)));
            Vector256<short> clamp_us_160 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 160)));
            Vector256<short> clamp_us_176 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 176)));
            Vector256<short> clamp_us_192 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 192)));
            Vector256<short> clamp_us_208 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 208)));
            Vector256<short> clamp_us_224 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 224)));
            Vector256<short> clamp_us_240 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 240)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_128, Avx2.MultiplyLow(clamp_us_128, Avx2.LoadAlignedVector256(ourWeights + 128))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_144, Avx2.MultiplyLow(clamp_us_144, Avx2.LoadAlignedVector256(ourWeights + 144))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_160, Avx2.MultiplyLow(clamp_us_160, Avx2.LoadAlignedVector256(ourWeights + 160))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_176, Avx2.MultiplyLow(clamp_us_176, Avx2.LoadAlignedVector256(ourWeights + 176))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_192, Avx2.MultiplyLow(clamp_us_192, Avx2.LoadAlignedVector256(ourWeights + 192))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_208, Avx2.MultiplyLow(clamp_us_208, Avx2.LoadAlignedVector256(ourWeights + 208))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_224, Avx2.MultiplyLow(clamp_us_224, Avx2.LoadAlignedVector256(ourWeights + 224))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_240, Avx2.MultiplyLow(clamp_us_240, Avx2.LoadAlignedVector256(ourWeights + 240))));

            Vector256<short> clamp_us_256 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 256)));
            Vector256<short> clamp_us_272 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 272)));
            Vector256<short> clamp_us_288 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 288)));
            Vector256<short> clamp_us_304 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 304)));
            Vector256<short> clamp_us_320 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 320)));
            Vector256<short> clamp_us_336 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 336)));
            Vector256<short> clamp_us_352 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 352)));
            Vector256<short> clamp_us_368 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 368)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_256, Avx2.MultiplyLow(clamp_us_256, Avx2.LoadAlignedVector256(ourWeights + 256))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_272, Avx2.MultiplyLow(clamp_us_272, Avx2.LoadAlignedVector256(ourWeights + 272))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_288, Avx2.MultiplyLow(clamp_us_288, Avx2.LoadAlignedVector256(ourWeights + 288))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_304, Avx2.MultiplyLow(clamp_us_304, Avx2.LoadAlignedVector256(ourWeights + 304))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_320, Avx2.MultiplyLow(clamp_us_320, Avx2.LoadAlignedVector256(ourWeights + 320))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_336, Avx2.MultiplyLow(clamp_us_336, Avx2.LoadAlignedVector256(ourWeights + 336))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_352, Avx2.MultiplyLow(clamp_us_352, Avx2.LoadAlignedVector256(ourWeights + 352))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_368, Avx2.MultiplyLow(clamp_us_368, Avx2.LoadAlignedVector256(ourWeights + 368))));

            Vector256<short> clamp_us_384 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 384)));
            Vector256<short> clamp_us_400 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 400)));
            Vector256<short> clamp_us_416 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 416)));
            Vector256<short> clamp_us_432 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 432)));
            Vector256<short> clamp_us_448 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 448)));
            Vector256<short> clamp_us_464 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 464)));
            Vector256<short> clamp_us_480 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 480)));
            Vector256<short> clamp_us_496 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 496)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_384, Avx2.MultiplyLow(clamp_us_384, Avx2.LoadAlignedVector256(ourWeights + 384))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_400, Avx2.MultiplyLow(clamp_us_400, Avx2.LoadAlignedVector256(ourWeights + 400))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_416, Avx2.MultiplyLow(clamp_us_416, Avx2.LoadAlignedVector256(ourWeights + 416))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_432, Avx2.MultiplyLow(clamp_us_432, Avx2.LoadAlignedVector256(ourWeights + 432))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_448, Avx2.MultiplyLow(clamp_us_448, Avx2.LoadAlignedVector256(ourWeights + 448))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_464, Avx2.MultiplyLow(clamp_us_464, Avx2.LoadAlignedVector256(ourWeights + 464))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_480, Avx2.MultiplyLow(clamp_us_480, Avx2.LoadAlignedVector256(ourWeights + 480))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_496, Avx2.MultiplyLow(clamp_us_496, Avx2.LoadAlignedVector256(ourWeights + 496))));

            Vector256<short> clamp_us_512 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 512)));
            Vector256<short> clamp_us_528 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 528)));
            Vector256<short> clamp_us_544 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 544)));
            Vector256<short> clamp_us_560 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 560)));
            Vector256<short> clamp_us_576 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 576)));
            Vector256<short> clamp_us_592 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 592)));
            Vector256<short> clamp_us_608 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 608)));
            Vector256<short> clamp_us_624 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 624)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_512, Avx2.MultiplyLow(clamp_us_512, Avx2.LoadAlignedVector256(ourWeights + 512))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_528, Avx2.MultiplyLow(clamp_us_528, Avx2.LoadAlignedVector256(ourWeights + 528))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_544, Avx2.MultiplyLow(clamp_us_544, Avx2.LoadAlignedVector256(ourWeights + 544))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_560, Avx2.MultiplyLow(clamp_us_560, Avx2.LoadAlignedVector256(ourWeights + 560))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_576, Avx2.MultiplyLow(clamp_us_576, Avx2.LoadAlignedVector256(ourWeights + 576))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_592, Avx2.MultiplyLow(clamp_us_592, Avx2.LoadAlignedVector256(ourWeights + 592))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_608, Avx2.MultiplyLow(clamp_us_608, Avx2.LoadAlignedVector256(ourWeights + 608))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_624, Avx2.MultiplyLow(clamp_us_624, Avx2.LoadAlignedVector256(ourWeights + 624))));

            Vector256<short> clamp_us_640 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 640)));
            Vector256<short> clamp_us_656 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 656)));
            Vector256<short> clamp_us_672 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 672)));
            Vector256<short> clamp_us_688 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 688)));
            Vector256<short> clamp_us_704 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 704)));
            Vector256<short> clamp_us_720 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 720)));
            Vector256<short> clamp_us_736 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 736)));
            Vector256<short> clamp_us_752 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 752)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_640, Avx2.MultiplyLow(clamp_us_640, Avx2.LoadAlignedVector256(ourWeights + 640))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_656, Avx2.MultiplyLow(clamp_us_656, Avx2.LoadAlignedVector256(ourWeights + 656))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_672, Avx2.MultiplyLow(clamp_us_672, Avx2.LoadAlignedVector256(ourWeights + 672))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_688, Avx2.MultiplyLow(clamp_us_688, Avx2.LoadAlignedVector256(ourWeights + 688))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_704, Avx2.MultiplyLow(clamp_us_704, Avx2.LoadAlignedVector256(ourWeights + 704))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_720, Avx2.MultiplyLow(clamp_us_720, Avx2.LoadAlignedVector256(ourWeights + 720))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_736, Avx2.MultiplyLow(clamp_us_736, Avx2.LoadAlignedVector256(ourWeights + 736))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_752, Avx2.MultiplyLow(clamp_us_752, Avx2.LoadAlignedVector256(ourWeights + 752))));

            Vector256<short> clamp_us_768 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 768)));
            Vector256<short> clamp_us_784 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 784)));
            Vector256<short> clamp_us_800 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 800)));
            Vector256<short> clamp_us_816 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 816)));
            Vector256<short> clamp_us_832 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 832)));
            Vector256<short> clamp_us_848 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 848)));
            Vector256<short> clamp_us_864 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 864)));
            Vector256<short> clamp_us_880 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 880)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_768, Avx2.MultiplyLow(clamp_us_768, Avx2.LoadAlignedVector256(ourWeights + 768))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_784, Avx2.MultiplyLow(clamp_us_784, Avx2.LoadAlignedVector256(ourWeights + 784))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_800, Avx2.MultiplyLow(clamp_us_800, Avx2.LoadAlignedVector256(ourWeights + 800))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_816, Avx2.MultiplyLow(clamp_us_816, Avx2.LoadAlignedVector256(ourWeights + 816))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_832, Avx2.MultiplyLow(clamp_us_832, Avx2.LoadAlignedVector256(ourWeights + 832))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_848, Avx2.MultiplyLow(clamp_us_848, Avx2.LoadAlignedVector256(ourWeights + 848))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_864, Avx2.MultiplyLow(clamp_us_864, Avx2.LoadAlignedVector256(ourWeights + 864))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_880, Avx2.MultiplyLow(clamp_us_880, Avx2.LoadAlignedVector256(ourWeights + 880))));

            Vector256<short> clamp_us_896 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 896)));
            Vector256<short> clamp_us_912 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 912)));
            Vector256<short> clamp_us_928 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 928)));
            Vector256<short> clamp_us_944 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 944)));
            Vector256<short> clamp_us_960 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 960)));
            Vector256<short> clamp_us_976 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 976)));
            Vector256<short> clamp_us_992 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 992)));
            Vector256<short> clamp_us_1008 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1008)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_896, Avx2.MultiplyLow(clamp_us_896, Avx2.LoadAlignedVector256(ourWeights + 896))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_912, Avx2.MultiplyLow(clamp_us_912, Avx2.LoadAlignedVector256(ourWeights + 912))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_928, Avx2.MultiplyLow(clamp_us_928, Avx2.LoadAlignedVector256(ourWeights + 928))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_944, Avx2.MultiplyLow(clamp_us_944, Avx2.LoadAlignedVector256(ourWeights + 944))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_960, Avx2.MultiplyLow(clamp_us_960, Avx2.LoadAlignedVector256(ourWeights + 960))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_976, Avx2.MultiplyLow(clamp_us_976, Avx2.LoadAlignedVector256(ourWeights + 976))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_992, Avx2.MultiplyLow(clamp_us_992, Avx2.LoadAlignedVector256(ourWeights + 992))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1008, Avx2.MultiplyLow(clamp_us_1008, Avx2.LoadAlignedVector256(ourWeights + 1008))));

            Vector256<short> clamp_us_1024 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1024)));
            Vector256<short> clamp_us_1040 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1040)));
            Vector256<short> clamp_us_1056 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1056)));
            Vector256<short> clamp_us_1072 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1072)));
            Vector256<short> clamp_us_1088 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1088)));
            Vector256<short> clamp_us_1104 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1104)));
            Vector256<short> clamp_us_1120 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1120)));
            Vector256<short> clamp_us_1136 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1136)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1024, Avx2.MultiplyLow(clamp_us_1024, Avx2.LoadAlignedVector256(ourWeights + 1024))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1040, Avx2.MultiplyLow(clamp_us_1040, Avx2.LoadAlignedVector256(ourWeights + 1040))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1056, Avx2.MultiplyLow(clamp_us_1056, Avx2.LoadAlignedVector256(ourWeights + 1056))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1072, Avx2.MultiplyLow(clamp_us_1072, Avx2.LoadAlignedVector256(ourWeights + 1072))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1088, Avx2.MultiplyLow(clamp_us_1088, Avx2.LoadAlignedVector256(ourWeights + 1088))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1104, Avx2.MultiplyLow(clamp_us_1104, Avx2.LoadAlignedVector256(ourWeights + 1104))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1120, Avx2.MultiplyLow(clamp_us_1120, Avx2.LoadAlignedVector256(ourWeights + 1120))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1136, Avx2.MultiplyLow(clamp_us_1136, Avx2.LoadAlignedVector256(ourWeights + 1136))));

            Vector256<short> clamp_us_1152 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1152)));
            Vector256<short> clamp_us_1168 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1168)));
            Vector256<short> clamp_us_1184 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1184)));
            Vector256<short> clamp_us_1200 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1200)));
            Vector256<short> clamp_us_1216 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1216)));
            Vector256<short> clamp_us_1232 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1232)));
            Vector256<short> clamp_us_1248 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1248)));
            Vector256<short> clamp_us_1264 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1264)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1152, Avx2.MultiplyLow(clamp_us_1152, Avx2.LoadAlignedVector256(ourWeights + 1152))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1168, Avx2.MultiplyLow(clamp_us_1168, Avx2.LoadAlignedVector256(ourWeights + 1168))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1184, Avx2.MultiplyLow(clamp_us_1184, Avx2.LoadAlignedVector256(ourWeights + 1184))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1200, Avx2.MultiplyLow(clamp_us_1200, Avx2.LoadAlignedVector256(ourWeights + 1200))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1216, Avx2.MultiplyLow(clamp_us_1216, Avx2.LoadAlignedVector256(ourWeights + 1216))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1232, Avx2.MultiplyLow(clamp_us_1232, Avx2.LoadAlignedVector256(ourWeights + 1232))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1248, Avx2.MultiplyLow(clamp_us_1248, Avx2.LoadAlignedVector256(ourWeights + 1248))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1264, Avx2.MultiplyLow(clamp_us_1264, Avx2.LoadAlignedVector256(ourWeights + 1264))));

            Vector256<short> clamp_us_1280 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1280)));
            Vector256<short> clamp_us_1296 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1296)));
            Vector256<short> clamp_us_1312 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1312)));
            Vector256<short> clamp_us_1328 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1328)));
            Vector256<short> clamp_us_1344 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1344)));
            Vector256<short> clamp_us_1360 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1360)));
            Vector256<short> clamp_us_1376 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1376)));
            Vector256<short> clamp_us_1392 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1392)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1280, Avx2.MultiplyLow(clamp_us_1280, Avx2.LoadAlignedVector256(ourWeights + 1280))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1296, Avx2.MultiplyLow(clamp_us_1296, Avx2.LoadAlignedVector256(ourWeights + 1296))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1312, Avx2.MultiplyLow(clamp_us_1312, Avx2.LoadAlignedVector256(ourWeights + 1312))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1328, Avx2.MultiplyLow(clamp_us_1328, Avx2.LoadAlignedVector256(ourWeights + 1328))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1344, Avx2.MultiplyLow(clamp_us_1344, Avx2.LoadAlignedVector256(ourWeights + 1344))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1360, Avx2.MultiplyLow(clamp_us_1360, Avx2.LoadAlignedVector256(ourWeights + 1360))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1376, Avx2.MultiplyLow(clamp_us_1376, Avx2.LoadAlignedVector256(ourWeights + 1376))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1392, Avx2.MultiplyLow(clamp_us_1392, Avx2.LoadAlignedVector256(ourWeights + 1392))));

            Vector256<short> clamp_us_1408 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1408)));
            Vector256<short> clamp_us_1424 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1424)));
            Vector256<short> clamp_us_1440 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1440)));
            Vector256<short> clamp_us_1456 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1456)));
            Vector256<short> clamp_us_1472 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1472)));
            Vector256<short> clamp_us_1488 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1488)));
            Vector256<short> clamp_us_1504 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1504)));
            Vector256<short> clamp_us_1520 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(ourData + 1520)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1408, Avx2.MultiplyLow(clamp_us_1408, Avx2.LoadAlignedVector256(ourWeights + 1408))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1424, Avx2.MultiplyLow(clamp_us_1424, Avx2.LoadAlignedVector256(ourWeights + 1424))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1440, Avx2.MultiplyLow(clamp_us_1440, Avx2.LoadAlignedVector256(ourWeights + 1440))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1456, Avx2.MultiplyLow(clamp_us_1456, Avx2.LoadAlignedVector256(ourWeights + 1456))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1472, Avx2.MultiplyLow(clamp_us_1472, Avx2.LoadAlignedVector256(ourWeights + 1472))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1488, Avx2.MultiplyLow(clamp_us_1488, Avx2.LoadAlignedVector256(ourWeights + 1488))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1504, Avx2.MultiplyLow(clamp_us_1504, Avx2.LoadAlignedVector256(ourWeights + 1504))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_us_1520, Avx2.MultiplyLow(clamp_us_1520, Avx2.LoadAlignedVector256(ourWeights + 1520))));






            Vector256<short> clamp_them_0 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 0)));
            Vector256<short> clamp_them_16 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 16)));
            Vector256<short> clamp_them_32 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 32)));
            Vector256<short> clamp_them_48 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 48)));
            Vector256<short> clamp_them_64 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 64)));
            Vector256<short> clamp_them_80 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 80)));
            Vector256<short> clamp_them_96 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 96)));
            Vector256<short> clamp_them_112 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 112)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_0, Avx2.MultiplyLow(clamp_them_0, Avx2.LoadAlignedVector256(theirWeights + 0))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_16, Avx2.MultiplyLow(clamp_them_16, Avx2.LoadAlignedVector256(theirWeights + 16))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_32, Avx2.MultiplyLow(clamp_them_32, Avx2.LoadAlignedVector256(theirWeights + 32))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_48, Avx2.MultiplyLow(clamp_them_48, Avx2.LoadAlignedVector256(theirWeights + 48))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_64, Avx2.MultiplyLow(clamp_them_64, Avx2.LoadAlignedVector256(theirWeights + 64))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_80, Avx2.MultiplyLow(clamp_them_80, Avx2.LoadAlignedVector256(theirWeights + 80))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_96, Avx2.MultiplyLow(clamp_them_96, Avx2.LoadAlignedVector256(theirWeights + 96))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_112, Avx2.MultiplyLow(clamp_them_112, Avx2.LoadAlignedVector256(theirWeights + 112))));

            Vector256<short> clamp_them_128 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 128)));
            Vector256<short> clamp_them_144 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 144)));
            Vector256<short> clamp_them_160 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 160)));
            Vector256<short> clamp_them_176 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 176)));
            Vector256<short> clamp_them_192 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 192)));
            Vector256<short> clamp_them_208 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 208)));
            Vector256<short> clamp_them_224 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 224)));
            Vector256<short> clamp_them_240 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 240)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_128, Avx2.MultiplyLow(clamp_them_128, Avx2.LoadAlignedVector256(theirWeights + 128))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_144, Avx2.MultiplyLow(clamp_them_144, Avx2.LoadAlignedVector256(theirWeights + 144))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_160, Avx2.MultiplyLow(clamp_them_160, Avx2.LoadAlignedVector256(theirWeights + 160))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_176, Avx2.MultiplyLow(clamp_them_176, Avx2.LoadAlignedVector256(theirWeights + 176))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_192, Avx2.MultiplyLow(clamp_them_192, Avx2.LoadAlignedVector256(theirWeights + 192))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_208, Avx2.MultiplyLow(clamp_them_208, Avx2.LoadAlignedVector256(theirWeights + 208))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_224, Avx2.MultiplyLow(clamp_them_224, Avx2.LoadAlignedVector256(theirWeights + 224))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_240, Avx2.MultiplyLow(clamp_them_240, Avx2.LoadAlignedVector256(theirWeights + 240))));

            Vector256<short> clamp_them_256 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 256)));
            Vector256<short> clamp_them_272 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 272)));
            Vector256<short> clamp_them_288 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 288)));
            Vector256<short> clamp_them_304 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 304)));
            Vector256<short> clamp_them_320 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 320)));
            Vector256<short> clamp_them_336 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 336)));
            Vector256<short> clamp_them_352 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 352)));
            Vector256<short> clamp_them_368 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 368)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_256, Avx2.MultiplyLow(clamp_them_256, Avx2.LoadAlignedVector256(theirWeights + 256))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_272, Avx2.MultiplyLow(clamp_them_272, Avx2.LoadAlignedVector256(theirWeights + 272))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_288, Avx2.MultiplyLow(clamp_them_288, Avx2.LoadAlignedVector256(theirWeights + 288))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_304, Avx2.MultiplyLow(clamp_them_304, Avx2.LoadAlignedVector256(theirWeights + 304))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_320, Avx2.MultiplyLow(clamp_them_320, Avx2.LoadAlignedVector256(theirWeights + 320))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_336, Avx2.MultiplyLow(clamp_them_336, Avx2.LoadAlignedVector256(theirWeights + 336))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_352, Avx2.MultiplyLow(clamp_them_352, Avx2.LoadAlignedVector256(theirWeights + 352))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_368, Avx2.MultiplyLow(clamp_them_368, Avx2.LoadAlignedVector256(theirWeights + 368))));

            Vector256<short> clamp_them_384 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 384)));
            Vector256<short> clamp_them_400 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 400)));
            Vector256<short> clamp_them_416 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 416)));
            Vector256<short> clamp_them_432 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 432)));
            Vector256<short> clamp_them_448 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 448)));
            Vector256<short> clamp_them_464 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 464)));
            Vector256<short> clamp_them_480 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 480)));
            Vector256<short> clamp_them_496 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 496)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_384, Avx2.MultiplyLow(clamp_them_384, Avx2.LoadAlignedVector256(theirWeights + 384))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_400, Avx2.MultiplyLow(clamp_them_400, Avx2.LoadAlignedVector256(theirWeights + 400))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_416, Avx2.MultiplyLow(clamp_them_416, Avx2.LoadAlignedVector256(theirWeights + 416))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_432, Avx2.MultiplyLow(clamp_them_432, Avx2.LoadAlignedVector256(theirWeights + 432))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_448, Avx2.MultiplyLow(clamp_them_448, Avx2.LoadAlignedVector256(theirWeights + 448))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_464, Avx2.MultiplyLow(clamp_them_464, Avx2.LoadAlignedVector256(theirWeights + 464))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_480, Avx2.MultiplyLow(clamp_them_480, Avx2.LoadAlignedVector256(theirWeights + 480))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_496, Avx2.MultiplyLow(clamp_them_496, Avx2.LoadAlignedVector256(theirWeights + 496))));

            Vector256<short> clamp_them_512 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 512)));
            Vector256<short> clamp_them_528 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 528)));
            Vector256<short> clamp_them_544 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 544)));
            Vector256<short> clamp_them_560 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 560)));
            Vector256<short> clamp_them_576 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 576)));
            Vector256<short> clamp_them_592 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 592)));
            Vector256<short> clamp_them_608 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 608)));
            Vector256<short> clamp_them_624 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 624)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_512, Avx2.MultiplyLow(clamp_them_512, Avx2.LoadAlignedVector256(theirWeights + 512))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_528, Avx2.MultiplyLow(clamp_them_528, Avx2.LoadAlignedVector256(theirWeights + 528))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_544, Avx2.MultiplyLow(clamp_them_544, Avx2.LoadAlignedVector256(theirWeights + 544))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_560, Avx2.MultiplyLow(clamp_them_560, Avx2.LoadAlignedVector256(theirWeights + 560))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_576, Avx2.MultiplyLow(clamp_them_576, Avx2.LoadAlignedVector256(theirWeights + 576))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_592, Avx2.MultiplyLow(clamp_them_592, Avx2.LoadAlignedVector256(theirWeights + 592))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_608, Avx2.MultiplyLow(clamp_them_608, Avx2.LoadAlignedVector256(theirWeights + 608))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_624, Avx2.MultiplyLow(clamp_them_624, Avx2.LoadAlignedVector256(theirWeights + 624))));

            Vector256<short> clamp_them_640 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 640)));
            Vector256<short> clamp_them_656 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 656)));
            Vector256<short> clamp_them_672 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 672)));
            Vector256<short> clamp_them_688 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 688)));
            Vector256<short> clamp_them_704 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 704)));
            Vector256<short> clamp_them_720 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 720)));
            Vector256<short> clamp_them_736 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 736)));
            Vector256<short> clamp_them_752 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 752)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_640, Avx2.MultiplyLow(clamp_them_640, Avx2.LoadAlignedVector256(theirWeights + 640))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_656, Avx2.MultiplyLow(clamp_them_656, Avx2.LoadAlignedVector256(theirWeights + 656))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_672, Avx2.MultiplyLow(clamp_them_672, Avx2.LoadAlignedVector256(theirWeights + 672))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_688, Avx2.MultiplyLow(clamp_them_688, Avx2.LoadAlignedVector256(theirWeights + 688))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_704, Avx2.MultiplyLow(clamp_them_704, Avx2.LoadAlignedVector256(theirWeights + 704))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_720, Avx2.MultiplyLow(clamp_them_720, Avx2.LoadAlignedVector256(theirWeights + 720))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_736, Avx2.MultiplyLow(clamp_them_736, Avx2.LoadAlignedVector256(theirWeights + 736))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_752, Avx2.MultiplyLow(clamp_them_752, Avx2.LoadAlignedVector256(theirWeights + 752))));

            Vector256<short> clamp_them_768 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 768)));
            Vector256<short> clamp_them_784 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 784)));
            Vector256<short> clamp_them_800 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 800)));
            Vector256<short> clamp_them_816 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 816)));
            Vector256<short> clamp_them_832 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 832)));
            Vector256<short> clamp_them_848 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 848)));
            Vector256<short> clamp_them_864 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 864)));
            Vector256<short> clamp_them_880 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 880)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_768, Avx2.MultiplyLow(clamp_them_768, Avx2.LoadAlignedVector256(theirWeights + 768))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_784, Avx2.MultiplyLow(clamp_them_784, Avx2.LoadAlignedVector256(theirWeights + 784))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_800, Avx2.MultiplyLow(clamp_them_800, Avx2.LoadAlignedVector256(theirWeights + 800))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_816, Avx2.MultiplyLow(clamp_them_816, Avx2.LoadAlignedVector256(theirWeights + 816))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_832, Avx2.MultiplyLow(clamp_them_832, Avx2.LoadAlignedVector256(theirWeights + 832))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_848, Avx2.MultiplyLow(clamp_them_848, Avx2.LoadAlignedVector256(theirWeights + 848))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_864, Avx2.MultiplyLow(clamp_them_864, Avx2.LoadAlignedVector256(theirWeights + 864))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_880, Avx2.MultiplyLow(clamp_them_880, Avx2.LoadAlignedVector256(theirWeights + 880))));

            Vector256<short> clamp_them_896 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 896)));
            Vector256<short> clamp_them_912 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 912)));
            Vector256<short> clamp_them_928 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 928)));
            Vector256<short> clamp_them_944 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 944)));
            Vector256<short> clamp_them_960 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 960)));
            Vector256<short> clamp_them_976 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 976)));
            Vector256<short> clamp_them_992 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 992)));
            Vector256<short> clamp_them_1008 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1008)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_896, Avx2.MultiplyLow(clamp_them_896, Avx2.LoadAlignedVector256(theirWeights + 896))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_912, Avx2.MultiplyLow(clamp_them_912, Avx2.LoadAlignedVector256(theirWeights + 912))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_928, Avx2.MultiplyLow(clamp_them_928, Avx2.LoadAlignedVector256(theirWeights + 928))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_944, Avx2.MultiplyLow(clamp_them_944, Avx2.LoadAlignedVector256(theirWeights + 944))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_960, Avx2.MultiplyLow(clamp_them_960, Avx2.LoadAlignedVector256(theirWeights + 960))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_976, Avx2.MultiplyLow(clamp_them_976, Avx2.LoadAlignedVector256(theirWeights + 976))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_992, Avx2.MultiplyLow(clamp_them_992, Avx2.LoadAlignedVector256(theirWeights + 992))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1008, Avx2.MultiplyLow(clamp_them_1008, Avx2.LoadAlignedVector256(theirWeights + 1008))));

            Vector256<short> clamp_them_1024 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1024)));
            Vector256<short> clamp_them_1040 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1040)));
            Vector256<short> clamp_them_1056 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1056)));
            Vector256<short> clamp_them_1072 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1072)));
            Vector256<short> clamp_them_1088 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1088)));
            Vector256<short> clamp_them_1104 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1104)));
            Vector256<short> clamp_them_1120 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1120)));
            Vector256<short> clamp_them_1136 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1136)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1024, Avx2.MultiplyLow(clamp_them_1024, Avx2.LoadAlignedVector256(theirWeights + 1024))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1040, Avx2.MultiplyLow(clamp_them_1040, Avx2.LoadAlignedVector256(theirWeights + 1040))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1056, Avx2.MultiplyLow(clamp_them_1056, Avx2.LoadAlignedVector256(theirWeights + 1056))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1072, Avx2.MultiplyLow(clamp_them_1072, Avx2.LoadAlignedVector256(theirWeights + 1072))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1088, Avx2.MultiplyLow(clamp_them_1088, Avx2.LoadAlignedVector256(theirWeights + 1088))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1104, Avx2.MultiplyLow(clamp_them_1104, Avx2.LoadAlignedVector256(theirWeights + 1104))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1120, Avx2.MultiplyLow(clamp_them_1120, Avx2.LoadAlignedVector256(theirWeights + 1120))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1136, Avx2.MultiplyLow(clamp_them_1136, Avx2.LoadAlignedVector256(theirWeights + 1136))));

            Vector256<short> clamp_them_1152 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1152)));
            Vector256<short> clamp_them_1168 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1168)));
            Vector256<short> clamp_them_1184 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1184)));
            Vector256<short> clamp_them_1200 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1200)));
            Vector256<short> clamp_them_1216 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1216)));
            Vector256<short> clamp_them_1232 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1232)));
            Vector256<short> clamp_them_1248 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1248)));
            Vector256<short> clamp_them_1264 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1264)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1152, Avx2.MultiplyLow(clamp_them_1152, Avx2.LoadAlignedVector256(theirWeights + 1152))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1168, Avx2.MultiplyLow(clamp_them_1168, Avx2.LoadAlignedVector256(theirWeights + 1168))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1184, Avx2.MultiplyLow(clamp_them_1184, Avx2.LoadAlignedVector256(theirWeights + 1184))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1200, Avx2.MultiplyLow(clamp_them_1200, Avx2.LoadAlignedVector256(theirWeights + 1200))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1216, Avx2.MultiplyLow(clamp_them_1216, Avx2.LoadAlignedVector256(theirWeights + 1216))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1232, Avx2.MultiplyLow(clamp_them_1232, Avx2.LoadAlignedVector256(theirWeights + 1232))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1248, Avx2.MultiplyLow(clamp_them_1248, Avx2.LoadAlignedVector256(theirWeights + 1248))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1264, Avx2.MultiplyLow(clamp_them_1264, Avx2.LoadAlignedVector256(theirWeights + 1264))));

            Vector256<short> clamp_them_1280 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1280)));
            Vector256<short> clamp_them_1296 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1296)));
            Vector256<short> clamp_them_1312 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1312)));
            Vector256<short> clamp_them_1328 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1328)));
            Vector256<short> clamp_them_1344 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1344)));
            Vector256<short> clamp_them_1360 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1360)));
            Vector256<short> clamp_them_1376 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1376)));
            Vector256<short> clamp_them_1392 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1392)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1280, Avx2.MultiplyLow(clamp_them_1280, Avx2.LoadAlignedVector256(theirWeights + 1280))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1296, Avx2.MultiplyLow(clamp_them_1296, Avx2.LoadAlignedVector256(theirWeights + 1296))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1312, Avx2.MultiplyLow(clamp_them_1312, Avx2.LoadAlignedVector256(theirWeights + 1312))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1328, Avx2.MultiplyLow(clamp_them_1328, Avx2.LoadAlignedVector256(theirWeights + 1328))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1344, Avx2.MultiplyLow(clamp_them_1344, Avx2.LoadAlignedVector256(theirWeights + 1344))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1360, Avx2.MultiplyLow(clamp_them_1360, Avx2.LoadAlignedVector256(theirWeights + 1360))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1376, Avx2.MultiplyLow(clamp_them_1376, Avx2.LoadAlignedVector256(theirWeights + 1376))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1392, Avx2.MultiplyLow(clamp_them_1392, Avx2.LoadAlignedVector256(theirWeights + 1392))));

            Vector256<short> clamp_them_1408 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1408)));
            Vector256<short> clamp_them_1424 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1424)));
            Vector256<short> clamp_them_1440 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1440)));
            Vector256<short> clamp_them_1456 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1456)));
            Vector256<short> clamp_them_1472 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1472)));
            Vector256<short> clamp_them_1488 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1488)));
            Vector256<short> clamp_them_1504 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1504)));
            Vector256<short> clamp_them_1520 = Avx2.Min(ClampMax, Avx2.Max(Vector256<short>.Zero, Avx2.LoadAlignedVector256(theirData + 1520)));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1408, Avx2.MultiplyLow(clamp_them_1408, Avx2.LoadAlignedVector256(theirWeights + 1408))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1424, Avx2.MultiplyLow(clamp_them_1424, Avx2.LoadAlignedVector256(theirWeights + 1424))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1440, Avx2.MultiplyLow(clamp_them_1440, Avx2.LoadAlignedVector256(theirWeights + 1440))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1456, Avx2.MultiplyLow(clamp_them_1456, Avx2.LoadAlignedVector256(theirWeights + 1456))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1472, Avx2.MultiplyLow(clamp_them_1472, Avx2.LoadAlignedVector256(theirWeights + 1472))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1488, Avx2.MultiplyLow(clamp_them_1488, Avx2.LoadAlignedVector256(theirWeights + 1488))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1504, Avx2.MultiplyLow(clamp_them_1504, Avx2.LoadAlignedVector256(theirWeights + 1504))));
            normalSum = Avx2.Add(normalSum, Avx2.MultiplyAddAdjacent(clamp_them_1520, Avx2.MultiplyLow(clamp_them_1520, Avx2.LoadAlignedVector256(theirWeights + 1520))));

            int output = SumVector256NoHadd(normalSum);

            return (output / QA + LayerBiases[0][0]) * OutputScale / QAB;
        }


        private static int FeatureIndex(int pc, int pt, int sq, int perspective)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            return ((pc ^ perspective) * ColorStride + pt * PieceStride + (sq ^ perspective * 56)) * SIMD_CHUNKS;
        }



        private static (int, int) FeatureIndex(int pc, int pt, int sq)
        {
            const int ColorStride = 64 * 6;
            const int PieceStride = 64;

            int whiteIndex = pc * ColorStride + pt * PieceStride + sq;
            int blackIndex = Not(pc) * ColorStride + pt * PieceStride + (sq ^ 56);

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

            (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom);
            (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo);

            if (theirPiece != None)
            {
                (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo);

                SubSubAdd(whiteAccumulation,
                    (FeatureWeights + wFrom),
                    (FeatureWeights + wCap),
                    (FeatureWeights + wTo));

                SubSubAdd(blackAccumulation,
                    (FeatureWeights + bFrom),
                    (FeatureWeights + bCap),
                    (FeatureWeights + bTo));
            }
            else if (m.EnPassant)
            {
                int idxPawn = moveTo - ShiftUpDir(us);

                (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn);

                SubSubAdd(whiteAccumulation,
                    (FeatureWeights + wFrom),
                    (FeatureWeights + wCap),
                    (FeatureWeights + wTo));

                SubSubAdd(blackAccumulation,
                    (FeatureWeights + bFrom),
                    (FeatureWeights + bCap),
                    (FeatureWeights + bTo));
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

                SubSubAddAdd(whiteAccumulation,
                    (FeatureWeights + wFrom),
                    (FeatureWeights + wRookFrom),
                    (FeatureWeights + wTo),
                    (FeatureWeights + wRookTo));

                SubSubAddAdd(blackAccumulation,
                    (FeatureWeights + bFrom),
                    (FeatureWeights + bRookFrom),
                    (FeatureWeights + bTo),
                    (FeatureWeights + bRookTo));
            }
            else
            {
                SubAdd(whiteAccumulation,
                    (FeatureWeights + wFrom),
                    (FeatureWeights + wTo));

                SubAdd(blackAccumulation,
                    (FeatureWeights + bFrom),
                    (FeatureWeights + bTo));
            }
        }


        private static void SubAdd(Vector256<short>* src, Vector256<short>* sub1, Vector256<short>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]);
            }
        }

        private static void SubSubAdd(Vector256<short>* src, Vector256<short>* sub1, Vector256<short>* sub2, Vector256<short>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]), sub2[i]);
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
                for (int pc = 0; pc < 2; pc++)
                {
                    for (int pt = 0; pt < PieceNB; pt++)
                    {
                        int[] sums = new int[64];
                        for (int sq = 0; sq < SquareNB; sq++)
                        {
                            int sum = 0;

                            int idx = FeatureIndex(pc, pt, sq, perspective);
                            short* weights = (FTWeights + (idx * HiddenSize));

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
            }
            else
            {
                //  Use the current network's weights
                Unsafe.CopyBlock(tempLayerWeights, LayerWeights, sizeof(short) * LayerWeightElements);
            }

            System.Drawing.Bitmap pic = new System.Drawing.Bitmap(xSize, ySize);

            for (int perspective = 0; perspective < 2; perspective++)
            {
                var FCWeights = (short*)tempLayerWeights;
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
