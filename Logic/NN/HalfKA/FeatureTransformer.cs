
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static Lizard.Logic.NN.HKA.HalfKA_HM;
using static Lizard.Logic.NN.HKA.NNCommon;
using static Lizard.Logic.NN.HKA.LEB128;

namespace Lizard.Logic.NN.HKA
{
    /// <summary>
    /// Handles Accumulator updates and refreshes, and translates the position into inputs for the network to use.
    /// <para></para>
    /// 
    /// https://github.com/official-stockfish/Stockfish/blob/84f3e867903f62480c33243dd0ecbffd342796fc/src/nnue/nnue_feature_transformer.h
    /// </summary>
    [SkipStaticConstructor]
    public static unsafe class FeatureTransformer
    {

        // Number of output dimensions for one side
        public const uint HalfDimensions = HalfKA_HM.TransformedFeatureDimensions;

        public const uint InputDimensions = HalfKA_HM.Dimensions;
        public const uint OutputDimensions = HalfDimensions;
        public static uint HashValue => HalfKA_HM.HashValue ^ (OutputDimensions * 2);
        private static readonly Vector256<short> One = Vector256.Create((short)127).AsInt16();
        private static readonly Vector256<short> Zero = Vector256<short>.Zero;


        public const int BufferSize = (int)(OutputDimensions * sizeof(ushort));

        private static readonly Vector256<short>* Biases;
        public static readonly Vector256<short>* Weights;
        public static readonly Vector256<int>* PSQTWeights;


        public const int NumRegs = 16;
        public const int NumPsqtRegs = 1;

        public const int TileHeight = NumRegs * VSize.Byte / 2;
        public const int PsqtTileHeight = NumPsqtRegs * VSize.Byte / 4;

        static FeatureTransformer()
        {
            Biases = (Vector256<short>*)AlignedAllocZeroed(HalfDimensions / VSize.Short * 32, AllocAlignment);
            Weights = (Vector256<short>*)AlignedAllocZeroed(HalfDimensions * InputDimensions / VSize.Short * 32, AllocAlignment);
            PSQTWeights = (Vector256<int>*)AlignedAllocZeroed(InputDimensions * PSQTBuckets / VSize.Int * 32, AllocAlignment);

            One = Vector256.Create((short)127).AsInt16();
        }

        /// <summary>
        /// Takes the input from the <paramref name="accumulator"/> and places them into <paramref name="output"/>,
        /// refreshing the <paramref name="accumulator"/> if necessary.
        /// </summary>
        public static int TransformFeatures(Position pos, Span<sbyte> output, ref Accumulator accumulator, int bucket)
        {
            if (accumulator.NeedsRefresh[White])
            {
                RefreshAccumulatorPerspective(pos, ref accumulator, White);
            }

            if (accumulator.NeedsRefresh[Black])
            {
                RefreshAccumulatorPerspective(pos, ref accumulator, Black);
            }

            const int Control = 0b11011000;

            const uint OutputChunkSize = 256 / 8;
            const uint NumOutputChunks = HalfDimensions / 2 / OutputChunkSize;
            const uint StrideOffset = HalfKA_HM.TransformedFeatureDimensions / VSize.Short / 2;

            Span<int> perspectives = stackalloc int[2] { pos.ToMove, Not(pos.ToMove) };

            var psqt = (accumulator.PSQ(perspectives[0])[0][bucket] -
                        accumulator.PSQ(perspectives[1])[0][bucket]) / 2;

            sbyte* outputPtr = (sbyte*)Unsafe.AsPointer(ref output[0]);

            for (int p = 0; p < 2; p++)
            {
                var accumulation = accumulator[perspectives[p]];
                uint offset = (uint)(HalfDimensions / 2 * p);
                for (int j = 0; j < NumOutputChunks; j++)
                {
                    var sum0a = Avx2.Max(Avx2.Min(accumulation[(j * 2) + 0], One), Zero);
                    var sum0b = Avx2.Max(Avx2.Min(accumulation[(j * 2) + 1], One), Zero);
                    var sum1a = Avx2.Max(Avx2.Min(accumulation[(j * 2) + 0 + StrideOffset], One), Zero);
                    var sum1b = Avx2.Max(Avx2.Min(accumulation[(j * 2) + 1 + StrideOffset], One), Zero);

                    var pa = Avx2.ShiftRightLogical(Avx2.MultiplyLow(sum0a, sum1a), 7);
                    var pb = Avx2.ShiftRightLogical(Avx2.MultiplyLow(sum0b, sum1b), 7);

                    Vector256<sbyte> saturated = Avx2.PackSignedSaturate(pa, pb);
                    Vector256<long> permuted = Avx2.Permute4x64(saturated.AsInt64(), Control);

                    int storeIdx = (int)(offset + (j * VSize.SByte));
                    Avx.Store((long*)(outputPtr + storeIdx), permuted);
                }
            }

            return psqt;
        }


        /// <summary>
        /// Finds the active features (existing pieces on the board) and updates the Accumulator to include those pieces.
        /// This is comparatively very slow, so it should only be done when absolutely necessary, like when our king moves.
        /// </summary>
        public static void RefreshAccumulatorPerspective(Position pos, ref Accumulator accumulator, int perspective)
        {
            const int RelativeWeightIndex = (int)HalfDimensions / 16;
            const int RelativeTileHeight = TileHeight / 16;

            Span<int> active = stackalloc int[MaxActiveDimensions];

            int activeCount = HalfKA_HM.AppendActiveIndices(pos, active, perspective);

            if (EnableAssertions)
            {
                Assert(activeCount <= MaxActiveDimensions, "AppendActiveIndices returned " + activeCount + " features! (should be <= 32)");
            }

            var accumulation = accumulator[perspective];
            var PSQTaccumulation = accumulator.PSQ(perspective);

            Vector256<short>* acc = stackalloc Vector256<short>[NumRegs];

            for (int j = 0; j < HalfDimensions / TileHeight; j++)
            {
                for (int k = 0; k < NumRegs; k++)
                {
                    acc[k] = Biases[(j * RelativeTileHeight) + k];
                }

                int i = 0;
                while (i < activeCount)
                {
                    int index = active[i++];
                    if (index <= 0)
                    {
                        break;
                    }

                    var offset = (RelativeWeightIndex * index) + (j * RelativeTileHeight);
                    Vector256<short>* column = &Weights[offset];
                    for (int k = 0; k < NumRegs; k++)
                    {
                        acc[k] = Avx2.Add(acc[k], column[k]);
                    }
                }

                for (int k = 0; k < NumRegs; k++)
                {
                    accumulation[(j * NumRegs) + k] = acc[k];

                }
            }

            Vector256<int>* psq = stackalloc Vector256<int>[NumPsqtRegs];
            for (int j = 0; j < PSQTBuckets / PsqtTileHeight; j++)
            {
                for (int k = 0; k < NumPsqtRegs; k++)
                {
                    psq[k] = Vector256<int>.Zero;
                }

                int i = 0;
                while (i < activeCount)
                {
                    int index = active[i++];
                    if (index <= 0)
                    {
                        break;
                    }

                    for (int k = 0; k < NumPsqtRegs; k++)
                    {
                        psq[k] = Avx2.Add(psq[k], PSQTWeights[index + (j * PsqtTileHeight)]);
                    }
                }

                for (int k = 0; k < NumPsqtRegs; k++)
                {
                    PSQTaccumulation[k] = psq[k];
                }
            }

            accumulator.NeedsRefresh[perspective] = false;
        }



        /// <summary>
        /// Reads the weights and biases from the network file.
        /// </summary>
        public static bool ReadParameters(BinaryReader br)
        {
            uint header = br.ReadUInt32();
            Debug.WriteLine("FeatureTransformer header: " + header.ToString("X"));

            if (LEB128.IsCompressed(br))
            {
                Debug.WriteLine("FeatureTransformer reading LEB compressed biases/weights");


                LEB128.ReadLEBInt16(br, (short*)Biases, (int)HalfDimensions);
                LEB128.ReadLEBInt16(br, (short*)Weights, (int)(HalfDimensions * InputDimensions));
                LEB128.ReadLEBInt32(br, (int*)PSQTWeights, (int)(PSQTBuckets * InputDimensions));
            }
            else
            {
                var stream = br.BaseStream;
                long toRead = (sizeof(short) * (HalfDimensions + (HalfDimensions * InputDimensions))) + (sizeof(int) * (PSQTBuckets * InputDimensions));
                if (stream.Position + toRead > stream.Length)
                {
                    Console.WriteLine("HalfKA FeatureTransformer's BinaryReader doesn't have enough data for all weights and biases to be read!");
                    Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                    Console.WriteLine("The file being loaded is either not a valid HalfKA network, or has different layer sizes than the hardcoded ones.");
                    Console.ReadLine();
                    Environment.Exit(-1);
                }

                for (int i = 0; i < HalfDimensions; i += VSize.Short)
                {
                    Biases[i / VSize.Short] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }

                for (int i = 0; i < HalfDimensions * InputDimensions; i += VSize.Short)
                {
                    Weights[i / VSize.Short] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt16();
                }

                for (int i = 0; i < PSQTBuckets * InputDimensions; i += VSize.Int)
                {
                    PSQTWeights[i / VSize.Int] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt32();
                }
            }

            return true;
        }

    }
}
