



using static LTChess.Logic.NN.HalfKP.NNCommon;
using static LTChess.Logic.NN.HalfKP.HalfKP;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Numerics;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LTChess.Logic.NN.HalfKP
{
    /// <summary>
    /// Handles Accumulator updates and refreshes, and translates the position into inputs for the network to use.
    /// <para></para>
    /// 
    /// https://github.com/official-stockfish/Stockfish/blob/84f3e867903f62480c33243dd0ecbffd342796fc/src/nnue/nnue_feature_transformer.h
    /// </summary>
    [SkipStaticConstructor]
    public unsafe class FeatureTransformer
    {
        // Number of output dimensions for one side
        public const uint HalfDimensions = HalfKP.TransformedFeatureDimensions;

        private const uint InputDimensions = HalfKP.Dimensions;
        private const uint OutputDimensions = HalfDimensions * 2;

        public const long BufferSize = OutputDimensions;

        public static uint GetHashValue() => HalfKP.HashValue(Color.White) ^ OutputDimensions;
        public static readonly Vector256<short>* Weights;
        public static readonly Vector256<short>* Biases;


        /// <summary>
        /// The number of items within the Vector256<T> that this class uses, which is 32 / sizeof(short) = 16.
        /// </summary>
        public static int VectorSize = VSize.Short;


        static FeatureTransformer()
        {
            Weights = (Vector256<short>*) AlignedAllocZeroed((nuint) ((HalfDimensions * InputDimensions) * sizeof(short)), AllocAlignment);
            Biases  = (Vector256<short>*) AlignedAllocZeroed((nuint) ((HalfDimensions                  ) * sizeof(short)), AllocAlignment);
        }


        /// <summary>
        /// Takes the input from the <paramref name="accumulator"/> and places them into <paramref name="output"/>,
        /// refreshing the <paramref name="accumulator"/> if necessary.
        /// </summary>
        public void TransformFeatures(Position pos, Span<sbyte> output, ref AccumulatorPSQT accumulator)
        {
            if (accumulator.RefreshPerspective[Color.White])
            {
                RefreshAccumulatorPerspective(pos, ref accumulator, Color.White);
            }

            if (accumulator.RefreshPerspective[Color.Black])
            {
                RefreshAccumulatorPerspective(pos, ref accumulator, Color.Black);
            }

            const uint NumChunks = HalfDimensions / SimdWidth;
            const int Control = 0b11011000;

            sbyte* outputPtr = (sbyte*) Unsafe.AsPointer(ref output[0]);

            Span<int> perspectives = stackalloc int[2] { pos.ToMove, Not(pos.ToMove) };
            for (int p = 0; p < 2; p++)
            {
                uint offset = (uint)(HalfDimensions * p);
                var accumulation = accumulator[perspectives[p]];

                for (int j = 0; j < NumChunks; ++j)
                {
                    Vector256<short> sum0 = accumulation[(j * 2 + 0)];
                    Vector256<short> sum1 = accumulation[(j * 2 + 1)];

                    Vector256<sbyte> saturated = Avx2.PackSignedSaturate(sum0, sum1);
                    Vector256<sbyte> maxVec = Avx2.Max(saturated, Vector256<sbyte>.Zero);
                    Vector256<long> permuted = Avx2.Permute4x64(maxVec.AsInt64(), Control);

                    //  JIT seems to just use vmovups to place 'permuted' into the right offset in the output
                    //  instead of storing it with movdqu
                    int storeIdx = (int)((offset) + (j * VSize.SByte));
                    Avx.Store(outputPtr + storeIdx, permuted.AsSByte());
                }
            }
        }


        /// <summary>
        /// Finds the active features (existing pieces on the board) and updates the Accumulator to include those pieces.
        /// <br></br>
        /// This is comparatively very slow, so it should only be done when absolutely necessary, like when our king moves.
        /// </summary>
        public void RefreshAccumulatorPerspective(Position pos, ref AccumulatorPSQT accumulator, int perspective)
        {
            Span<int> active = stackalloc int[MaxActiveDimensions];
            int activeCount = HalfKP.AppendActiveIndices(pos, active, perspective);

            var accumulation = accumulator[perspective];

            Buffer.MemoryCopy(Biases, accumulation, (int)(HalfDimensions * sizeof(short)), (int)(HalfDimensions * sizeof(short)));

            int i = 0;
            while (i < activeCount)
            {
                int index = active[i++];
                if (index <= 0)
                {
                    break;
                }

                uint offset = (uint)(HalfDimensions * index) / VSize.Short;

                const uint NumChunks = HalfDimensions / (SimdWidth / 2);
                for (int j = 0; j < NumChunks; j++)
                {
                    Vector256<short> col = Weights[offset + j];
                    accumulation[j] = Avx2.Add(accumulation[j], col);
                }
            }

            accumulator.RefreshPerspective[perspective] = false;
        }



        /// <summary>
        /// Reads the weights and biases from the network file.
        /// </summary>
        public bool ReadParameters(BinaryReader br)
        {
            var stream = br.BaseStream;
            long toRead = (long) ((HalfDimensions * InputDimensions) + HalfDimensions) * sizeof(short);
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("HalfKP FeatureTransformer's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid HalfKP network, or has different layer sizes than the hardcoded ones.");
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

            return true;
        }

    }
}
