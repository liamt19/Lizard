
#define CONST

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

        //public float* Weights;
        //public float* Biases;

        public float[] Weights;
        public float[] Biases;

        public Layer(int inputDimensions, int outputDimensions)
        {
            this.InputDimensions = inputDimensions;
            this.OutputDimensions = outputDimensions;
        }

        public void ReadWeights(BinaryReader br)
        {
            //Weights = (float*)AlignedAllocZeroed((nuint) (sizeof(float) * WeightCount));
            //Biases = (float*)AlignedAllocZeroed((nuint) (sizeof(float) * BiasCount));

            Weights = new float[WeightCount];
            Biases = new float[BiasCount];

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
            Span<float> temp = stackalloc float[BiasCount];
            for (int i = 0; i < BiasCount; i++)
            {
                temp[i] = Biases[i];
            }

            for (int i = 0; i < InputDimensions; ++i)
            {
                for (int j = 0; j < OutputDimensions; ++j)
                {
                    int idx = (i * OutputDimensions) + j;
                    temp[j] += input[i] * Weights[idx];
                }
            }

            for (int i = 0; i < OutputDimensions; i++)
            {
                output[i] = float.Clamp(temp[i], 0, 1);
            }
        }
    }
}
