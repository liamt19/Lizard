using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Lizard.Logic.NN.Bucketed768;

namespace Lizard.Logic.NN
{

    public unsafe struct NetContainer<T, U>
    {
        public readonly T* FTWeights;
        public readonly T* FTBiases;
        public readonly T** L1Weights;
        public readonly U** L1Biases;
        public readonly U** L2Weights;
        public readonly U** L2Biases;
        public readonly U** L3Weights;
        public readonly U* L3Biases;

        public NetContainer()
        {
            FTWeights = (T*)AlignedAllocZeroed((nuint)sizeof(T) * N_FTW);
            FTBiases  = (T*)AlignedAllocZeroed((nuint)sizeof(T) * N_FTB);


            L1Weights = (T**)AlignedAllocZeroed((nuint)sizeof(T*) * OUTPUT_BUCKETS);
            L1Biases  = (U**)AlignedAllocZeroed((nuint)sizeof(U*) * OUTPUT_BUCKETS);
            L2Weights = (U**)AlignedAllocZeroed((nuint)sizeof(U*) * OUTPUT_BUCKETS);
            L2Biases  = (U**)AlignedAllocZeroed((nuint)sizeof(U*) * OUTPUT_BUCKETS);
            L3Weights = (U**)AlignedAllocZeroed((nuint)sizeof(U*) * OUTPUT_BUCKETS);
            L3Biases  = (U*) AlignedAllocZeroed((nuint)sizeof(U)  * OUTPUT_BUCKETS);

            for (int i = 0; i < OUTPUT_BUCKETS; i++)
            {
                L1Weights[i] = (T*)AlignedAllocZeroed((nuint)sizeof(T) * (L1_SIZE * L2_SIZE * 2));
                L1Biases[i]  = (U*)AlignedAllocZeroed((nuint)sizeof(U) * (L2_SIZE));
                L2Weights[i] = (U*)AlignedAllocZeroed((nuint)sizeof(U) * (L2_SIZE * L3_SIZE ));
                L2Biases[i]  = (U*)AlignedAllocZeroed((nuint)sizeof(U) * (L3_SIZE));
                L3Weights[i] = (U*)AlignedAllocZeroed((nuint)sizeof(U) * (L3_SIZE));
            }
        }
    }

    public unsafe struct UQNetContainer
    {
        public readonly float[] FTWeights;
        public readonly float[] FTBiases;
        public readonly float[,,] L1Weights;
        public readonly float[,]  L1Biases;
        public readonly float[,,] L2Weights;
        public readonly float[,]  L2Biases;
        public readonly float[,] L3Weights;
        public readonly float[] L3Biases;

        public UQNetContainer()
        {
            FTWeights = new float[INPUT_SIZE * L1_SIZE * INPUT_BUCKETS];
            FTBiases  = new float[L1_SIZE];

            L1Weights = new float[2 * L1_SIZE, OUTPUT_BUCKETS, L2_SIZE];
            L1Biases  = new float[OUTPUT_BUCKETS, L2_SIZE];

            L2Weights = new float[L2_SIZE, OUTPUT_BUCKETS, L3_SIZE];
            L2Biases  = new float[OUTPUT_BUCKETS, L3_SIZE];

            L3Weights = new float[L3_SIZE, OUTPUT_BUCKETS];
            L3Biases  = new float[OUTPUT_BUCKETS];
        }
    }
}
