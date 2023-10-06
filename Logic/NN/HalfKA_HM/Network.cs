
using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using LTChess.Logic.NN.HalfKA_HM.Layers;
using System.Runtime.InteropServices;

namespace LTChess.Logic.NN.HalfKA_HM
{
    /// <summary>
    /// Represents the different feature layers for one bucket within the HalfKA architecture.
    /// <para></para>
    /// Based on https://github.com/official-stockfish/Stockfish/blob/c079acc26f93acc2eda08c7218c60559854f52f0/src/nnue/nnue_architecture.h
    /// </summary>
    public unsafe class Network
    {

        private const int FC_0_OUTPUTS = 15;
        private const int FC_1_OUTPUTS = 32;

        /// <summary>
        /// The input layer
        /// </summary>
        private AffineTransformSparse fc_0;
        private SqrClippedReLU ac_sqr_0;
        private ClippedReLU ac_0;

        private AffineTransform fc_1;
        private ClippedReLU ac_1;

        /// <summary>
        /// The output layer
        /// </summary>
        private AffineTransform fc_2;

        private readonly nuint fc_0_idx;
        private readonly nuint ac_sqr_0_idx;
        private readonly nuint ac_0_idx;
        private readonly nuint fc_1_idx;
        private readonly nuint ac_1_idx;
        private readonly nuint fc_2_idx;

        private readonly int _bytesToAlloc;

        private const int ClippedReLU_Padding = 32;

        public Network()
        {
            fc_0 = new AffineTransformSparse(TransformedFeatureDimensions, FC_0_OUTPUTS + 1);
            ac_sqr_0 = new SqrClippedReLU(FC_0_OUTPUTS + 1);
            ac_0 = new ClippedReLU(FC_0_OUTPUTS);

            fc_1 = new AffineTransform(FC_0_OUTPUTS * 2, FC_1_OUTPUTS);
            ac_1 = new ClippedReLU(FC_1_OUTPUTS);

            fc_2 = new AffineTransform(FC_1_OUTPUTS, 1);


            //  This allocates the buffers for each (AffineTransform/ClippedReLU) Layer in the network.
            //  Each AffineTransform uses a buffer of 32 ints, which are 128 bytes in size.
            //  The ClippedReLU layers use buffers of 32 sbytes, which are 32 bytes in size.
            //  The ClippedReLU buffers are padded so that they are aligned on a 64 byte boundary.
            fc_0_idx = (nuint) 0;
            ac_sqr_0_idx = (nuint)(fc_0_idx + (nuint)fc_0.BufferSizeBytes);
            ac_0_idx = (nuint)(ac_sqr_0_idx + (nuint)ac_sqr_0.BufferSizeBytes + ClippedReLU_Padding);
            fc_1_idx = (nuint) (ac_0_idx + (nuint) ac_0.BufferSizeBytes + ClippedReLU_Padding);
            ac_1_idx = (nuint) (fc_1_idx + (nuint) fc_1.BufferSizeBytes);
            fc_2_idx = (nuint) (ac_1_idx + (nuint) ac_1.BufferSizeBytes + ClippedReLU_Padding);

            _bytesToAlloc  = (fc_0.BufferSize + fc_1.BufferSize + fc_2.BufferSize) * sizeof(int);
            _bytesToAlloc += ((ac_0.BufferSize + ClippedReLU_Padding) + (ac_1.BufferSize + ClippedReLU_Padding) * sizeof(sbyte));
            _bytesToAlloc += ((ac_sqr_0.BufferSize + ClippedReLU_Padding) * sizeof(sbyte));
        
        }

        public uint GetHashValue()
        {
            uint hashValue = 0xEC42E90Du;

            hashValue ^= TransformedFeatureDimensions * 2;

            hashValue = fc_0.GetHashValue(hashValue);
            hashValue = ac_0.GetHashValue(hashValue);
            hashValue = fc_1.GetHashValue(hashValue);
            hashValue = ac_1.GetHashValue(hashValue);
            hashValue = fc_2.GetHashValue(hashValue);

            return hashValue;
        }

        public bool ReadParameters(BinaryReader br)
        {
            if (!fc_0.ReadParameters(br)) return false;
            if (!ac_0.ReadParameters(br)) return false;
            if (!fc_1.ReadParameters(br)) return false;
            if (!ac_1.ReadParameters(br)) return false;
            if (!fc_2.ReadParameters(br)) return false;
            return true;
        }

        public int Propagate(Span<sbyte> transformedFeatures)
        {
            var _buffer = AlignedAllocZeroed((nuint)_bytesToAlloc, AllocAlignment);

            Span<int>   fc_0_out     = new Span<int>   ((void*) ((nuint)_buffer + fc_0_idx    ), fc_0.BufferSize);
            Span<sbyte> ac_sqr_0_out = new Span<sbyte> ((void*) ((nuint)_buffer + ac_sqr_0_idx), ac_sqr_0.BufferSize);
            Span<sbyte> ac_0_out     = new Span<sbyte> ((void*) ((nuint)_buffer + ac_0_idx    ), ac_0.BufferSize);

            Span<int>   fc_1_out     = new Span<int>   ((void*) ((nuint)_buffer + fc_1_idx    ), fc_1.BufferSize);
            Span<sbyte> ac_1_out     = new Span<sbyte> ((void*) ((nuint)_buffer + ac_1_idx    ), ac_1.BufferSize);

            Span<int>   fc_2_out     = new Span<int>   ((void*) ((nuint)_buffer + fc_2_idx    ), fc_2.BufferSize);

            fc_0.Propagate(transformedFeatures, fc_0_out);
            ac_sqr_0.Propagate(fc_0_out, ac_sqr_0_out);
            ac_0.Propagate(fc_0_out, ac_0_out);

            void* src = (void*)((nuint)_buffer + ac_0_idx);
            void* dst = (void*)((nuint)_buffer + ac_sqr_0_idx + FC_0_OUTPUTS);
            Buffer.MemoryCopy(src, dst, FC_0_OUTPUTS * sizeof(sbyte), FC_0_OUTPUTS * sizeof(sbyte));

            fc_1.PropagateNormal(ac_sqr_0_out, fc_1_out);
            ac_1.Propagate(fc_1_out, ac_1_out);

            fc_2.PropagateOutput(ac_1_out, fc_2_out);

            NativeMemory.AlignedFree(_buffer);

            int fwdOut = (int) (fc_0_out[FC_0_OUTPUTS]) * (600 * OutputScale) / (127 * (1 << WeightScaleBits));
            int outputValue = fc_2_out[0] + fwdOut;

            return outputValue;
        }
    }
}
