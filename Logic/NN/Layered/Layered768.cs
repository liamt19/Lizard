using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using Lizard.Properties;

using static Lizard.Logic.NN.FunUnrollThings;
using Lizard.Logic.NN.Layered;

namespace Lizard.Logic.NN
{
    [SkipStaticConstructor]
    public static unsafe partial class Layered768
    {
        public const int InputSize = 768;
        public const int HiddenSize = 256;
        public const int OutputBuckets = 1;

        private const int QA = 255;
        private const int QB = 64;
        private const int QAB = QA * QB;

        public const int OutputScale = 400;

        public const int SIMD_CHUNKS = HiddenSize / VSize.Float;

        public const string NetworkName = "params.bin";

        /// <summary>
        /// The values applied according to the active features and current bucket.
        /// <para></para>
        /// This is the 768 -> 1536 part of the architecture.
        /// </summary>
        public static readonly Vector256<float>* FeatureWeights;

        /// <summary>
        /// The initial values that are placed into the accumulators.
        /// <para></para>
        /// When doing a full refresh, both accumulators are filled with these.
        /// </summary>
        public static readonly Vector256<float>* FeatureBiases;


        public static readonly Vector256<float>* OutputWeights;
        public static float OutputBias;
        private const int OutputWeightElements = L2_OUT;

        private const int FeatureWeightElements = InputSize * HiddenSize;
        private const int FeatureBiasElements = HiddenSize;

        private const int L1_IN = HiddenSize * 2;
        private const int L1_OUT = 8;
        private const int L2_IN = L1_OUT;
        private const int L2_OUT = 16;

        private static Layer Layer1;
        private static Layer Layer2;
        public static readonly Layer[] Layers;

        static Layered768()
        {
            FeatureWeights = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * FeatureWeightElements);
            FeatureBiases = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * FeatureBiasElements);
            OutputWeights = (Vector256<float>*)AlignedAllocZeroed(sizeof(float) * OutputWeightElements);

            Layer1 = new Layer(L1_IN, L1_OUT);
            Layer2 = new Layer(L2_IN, L2_OUT);
            Layers = [ Layer1, Layer2 ];

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

            float* ftw = (float*)FeatureWeights;
            for (int i = 0; i < FeatureWeightElements; i++)
            {
                ftw[i] = br.ReadSingle();
            }

            var ftb = (float*)FeatureBiases;
            for (int i = 0; i < FeatureBiasElements; i++)
            {
                ftb[i] = br.ReadSingle();
            }

            foreach (Layer layer in Layers)
            {
                layer.ReadWeights(br);
            }

            var outw = (float*)OutputWeights;
            for (int i = 0; i < OutputWeightElements; i++)
            {
                outw[i] = br.ReadSingle();
            }
            OutputBias = br.ReadSingle();

            if (br.BaseStream.Position != br.BaseStream.Length)
            {
                Console.WriteLine($"Too few weights read!! {br.BaseStream.Position / 4} of {br.BaseStream.Length / 4}");
            }

        }

        public static void RefreshAccumulator(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            ref Bitboard bb = ref pos.bb;

            var w = (Vector256<float>*)accumulator.White;
            var b = (Vector256<float>*)accumulator.Black;

            Unsafe.CopyBlock(w, FeatureBiases, sizeof(float) * HiddenSize);
            Unsafe.CopyBlock(b, FeatureBiases, sizeof(float) * HiddenSize);

            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                (int wIdx, int bIdx) = FeatureIndex(pc, pt, pieceIdx);
                for (int i = 0; i < SIMD_CHUNKS; i++)
                {
                    w[i] = Avx2.Add(w[i], FeatureWeights[wIdx + i]);
                    b[i] = Avx2.Add(b[i], FeatureWeights[bIdx + i]);
                }
            }
        }

        public static int GetEvaluation(Position pos)
        {
            ref Accumulator accumulator = ref *pos.State->Accumulator;
            var us = (float*)accumulator[pos.ToMove];
            var them = (float*)accumulator[Not(pos.ToMove)];

            int bufferSize = Layer1.InputDimensions + Layer1.OutputDimensions + Layer2.InputDimensions + Layer2.OutputDimensions;
            float* buffer = stackalloc float[bufferSize];

            for (int i = 0; i < HiddenSize; i++)
            {
                var activated = Math.Min(Math.Max(us[i], 0), 1);
                activated *= activated;
                buffer[i] += activated;
            }

            for (int i = 0; i < HiddenSize; i++)
            {
                var activated = Math.Min(Math.Max(them[i], 0), 1);
                activated *= activated;
                buffer[i + HiddenSize] += activated;
            }

            float* L1_out = buffer + ((HiddenSize * 2));
            float* L2_out = L1_out + Layer1.OutputDimensions;
            float* finalOut = L2_out;
            Layer1.Forward(buffer, L1_out);
            Layer2.Forward(L1_out, L2_out);


            float* outWeights = (float*)OutputWeights;
            float sum = 0;
            for (int i = 0; i < Layer2.OutputDimensions; i++)
            {
                sum += (finalOut[i] * outWeights[i]);
            }

            int retVal = (int)(OutputScale * (sum + OutputBias));
            return retVal;
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

            var whiteAccumulation = (Vector256<float>*) ((*accumulator)[White]);
            var blackAccumulation = (Vector256<float>*) ((*accumulator)[Black]);

            (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom);
            (int wTo, int bTo) = FeatureIndex(us, m.Promotion ? m.PromotionTo : ourPiece, moveTo);

            if (m.Castle)
            {
                int rookFrom = moveTo;
                int rookTo = m.CastlingRookSquare;

                (wTo, bTo) = FeatureIndex(us, ourPiece, m.CastlingKingSquare);

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
            else if (theirPiece != None)
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


        private static void SubAdd(Vector256<float>* src, Vector256<float>* sub1, Vector256<float>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]);
            }
        }

        private static void SubSubAdd(Vector256<float>* src, Vector256<float>* sub1, Vector256<float>* sub2, Vector256<float>* add1)
        {
            for (int i = 0; i < SIMD_CHUNKS; i++)
            {
                src[i] = Avx2.Subtract(Avx2.Subtract(Avx2.Add(src[i], add1[i]), sub1[i]), sub2[i]);
            }
        }

        private static void SubSubAddAdd(Vector256<float>* src, Vector256<float>* sub1, Vector256<float>* sub2, Vector256<float>* add1, Vector256<float>* add2)
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
