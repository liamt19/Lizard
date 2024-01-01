

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static LTChess.Logic.NN.HalfKP.NNCommon;

namespace LTChess.Logic.NN.HalfKP.Layers
{
    public unsafe class AffineTransform
    {
        private readonly int InputDimensions;
        private readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        private readonly int PaddedInputDimensions;

        private readonly Vector256<int>* Biases;
        private readonly Vector256<sbyte>* Weights;

        /// <summary>
        /// The number of items within the Vector256<T> that this class uses, which is 32 / sizeof(sbyte) = 32.
        /// </summary>
        private static int VectorSize = VSize.SByte;

        /// <summary>
        /// Creates a new Affine layer, which takes input from the <see cref="ClippedReLU"/> layer that came before it
        /// and outputs <paramref name="outputDims"/> numbers.
        /// </summary>
        /// <param name="prev">The layer that came before this one in the network</param>
        /// <param name="outputDims">The length of the array that this layer returns as output</param>
        public AffineTransform(int inputDims, int outputDims)
        {
            InputDimensions = inputDims;
            OutputDimensions = outputDims;

            BufferSize = CeilToMultiple((short)OutputDimensions, CacheLineSize);
            BufferSizeBytes = CeilToMultiple((short)(OutputDimensions * sizeof(int)), CacheLineSize);

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);

            Biases = (Vector256<int>*)AlignedAllocZeroed((nuint)(Math.Max(1, OutputDimensions / VSize.Int) * 32), AllocAlignment);
            Weights = (Vector256<sbyte>*)AlignedAllocZeroed((nuint)(OutputDimensions * PaddedInputDimensions / VSize.SByte * 32), AllocAlignment);
        }


        public void Propagate(Span<sbyte> input, Span<int> output)
        {
            int* inputPtr = (int*)Unsafe.AsPointer(ref input[0]);
            int* outputPtr = (int*)Unsafe.AsPointer(ref output[0]);

            int NumChunks = PaddedInputDimensions / SimdWidth;
            Vector256<short> Ones = Vector256<short>.One;

            for (int i = 0; i < OutputDimensions; i++)
            {
                int offset = i * PaddedInputDimensions;
                Vector256<int> sum = Vector256<int>.Zero;

                for (int j = 0; j < NumChunks; j++)
                {
                    Vector256<byte> left = Avx.LoadVector256((byte*)inputPtr + (j * VSize.SByte));
                    Vector256<sbyte> right = Weights[j + (i * PaddedInputDimensions / VSize.SByte)];
                    _mm256_dpbusd_epi32(ref sum, left, right);
                }

                sum = Avx2.HorizontalAdd(sum, sum);
                sum = Avx2.HorizontalAdd(sum, sum);

                Vector128<int> lo = Avx2.ExtractVector128(sum, 0);
                Vector128<int> hi = Avx2.ExtractVector128(sum, 1);
                output[i] = Sse2.ConvertToInt32(lo) + Sse2.ConvertToInt32(hi) + ((int*)Biases)[i];
            }
        }

        public bool ReadParameters(BinaryReader br)
        {

            var stream = br.BaseStream;
            long toRead = (long)((OutputDimensions * sizeof(int)) + (OutputDimensions * PaddedInputDimensions * sizeof(sbyte)));
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("HalfKP AffineTransform's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid HalfKP network, or has different layer sizes than the hardcoded ones.");
                Console.ReadLine();
                Environment.Exit(-1);
            }

            try
            {
                //  The output layer only has 1 bias
                if (OutputDimensions == 1)
                {
                    Biases[0] = Vector256.Create(br.ReadInt32(), 0, 0, 0, 0, 0, 0, 0);
                }
                else
                {
                    for (int i = 0; i < OutputDimensions; i += VSize.Int)
                    {
                        Biases[i / VSize.Int] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsInt32();
                    }
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i += VSize.SByte)
                {
                    Weights[i / VSize.SByte] = Vector256.Create(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64()).AsSByte();
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return false;
            }

            return true;
        }


        [MethodImpl(Inline)]
        public static void _mm256_dpbusd_epi32(ref Vector256<int> acc, Vector256<byte> a, Vector256<sbyte> b)
        {
            if (AvxVnni.IsSupported)
            {
                acc = AvxVnni.MultiplyWideningAndAdd(acc, a, b);
            }
            else
            {
                Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a, b);
                acc = Avx2.Add(acc, Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One));
            }
        }

        private static void m256_add_dpbusd_epi32x2(ref Vector256<int> acc, Vector256<byte> a0, Vector256<byte> a1,
                                                                            Vector256<sbyte> b0, Vector256<sbyte> b1)
        {
            if (AvxVnni.IsSupported)
            {
                acc = AvxVnni.MultiplyWideningAndAdd(acc, a0, b0);
                acc = AvxVnni.MultiplyWideningAndAdd(acc, a1, b1);
            }
            else
            {
                Vector256<short> product0 = Avx2.MultiplyAddAdjacent(a0, b0);
                Vector256<short> product1 = Avx2.MultiplyAddAdjacent(a1, b1);

                product0 = Avx2.AddSaturate(product0, product1);
                Vector256<int> product0f = Avx2.MultiplyAddAdjacent(product0, Vector256<short>.One);

                acc = Avx2.Add(acc, product0f);
            }
        }
    }
}
