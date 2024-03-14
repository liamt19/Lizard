using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.InteropServices;
using Lizard.Properties;
using System.Reflection.Emit;


namespace Lizard.Logic.NN.Layered
{
    public unsafe class Layer
    {
        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public int WeightCount => InputDimensions * OutputDimensions;
        public int BiasCount => OutputDimensions;

        public float* Weights;
        public float* Biases;

        public Layer(int inputDimensions, int outputDimensions)
        {
            this.InputDimensions = inputDimensions;
            this.OutputDimensions = outputDimensions;
        }

        public void ReadWeights(BinaryReader br)
        {
            Weights = (float*)AlignedAllocZeroed((nuint) (sizeof(float) * WeightCount));
            Biases = (float*)AlignedAllocZeroed((nuint) (sizeof(float) * BiasCount));

            for (int i = 0; i < WeightCount; i++)
            {
                Weights[i] = br.ReadSingle();
            }

            for (int i = 0; i < BiasCount; i++)
            {
                Biases[i] = br.ReadSingle();
            }
        }

        public void Forward(float* input, float* output)
        {
            float* temp = stackalloc float[BiasCount];
            for (int i = 0; i < BiasCount; i += VSize.Float)
            {
                Avx2.Store(temp + i, Avx2.LoadVector256(Biases + i));
            }

            for (int i = 0; i < InputDimensions; ++i)
            {
                int idx = (i * OutputDimensions);
                for (int j = 0; j < OutputDimensions; ++j)
                {
                    temp[j] += input[i] * Weights[idx + j];
                }
            }

            for (int i = 0; i < OutputDimensions; i += VSize.Float)
            {
                Avx2.Store(output + i, Avx2.Min(Avx2.Max(Avx2.LoadVector256(temp + i), Vector256<float>.Zero), Vector256<float>.One));
            }
        }
    }
}
