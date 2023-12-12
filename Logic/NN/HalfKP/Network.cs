using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.NN.HalfKP.Layers;

namespace LTChess.Logic.NN.HalfKP
{
    public unsafe class Network
    {
        private readonly AffineTransform fc_0;
        private readonly ClippedReLU ac_0;

        private readonly AffineTransform fc_1;
        private readonly ClippedReLU ac_1;

        private readonly AffineTransform fc_2;

        private readonly nuint fc_0_idx;
        private readonly nuint ac_0_idx;

        private readonly nuint fc_1_idx;
        private readonly nuint ac_1_idx;

        private readonly nuint fc_2_idx;

        private readonly nuint _bytesToAlloc;

        private const int FC_0_INPUTS = HalfKP.TransformedFeatureDimensions * 2;
        private const int INPUTS = 32;
        private const int OUTPUTS = 32;
        private const int FC_2_OUTPUTS = 1;

        /// <summary>
        /// Contains a per-thread pointer to a pre-allocated block of buffer memory.
        /// </summary>
        private static ThreadLocal<nuint> _ThreadBuffer;

        public Network()
        {
            fc_0 = new AffineTransform(FC_0_INPUTS, OUTPUTS);
            ac_0 = new ClippedReLU(INPUTS);

            fc_1 = new AffineTransform(INPUTS, OUTPUTS);
            ac_1 = new ClippedReLU(INPUTS);

            fc_2 = new AffineTransform(INPUTS, FC_2_OUTPUTS);

            fc_0_idx = (nuint)0;
            ac_0_idx = (nuint)(fc_0_idx + (nuint)fc_0.BufferSizeBytes);
            fc_1_idx = (nuint)(ac_0_idx + (nuint)ac_0.BufferSizeBytes);
            ac_1_idx = (nuint)(fc_1_idx + (nuint)fc_1.BufferSizeBytes);
            fc_2_idx = (nuint)(ac_1_idx + (nuint)ac_1.BufferSizeBytes);

            int bytes;
            bytes = (fc_0.BufferSize + fc_1.BufferSize + fc_2.BufferSize) * sizeof(int);
            bytes += ((ac_0.BufferSize) + (ac_1.BufferSize) * sizeof(sbyte));
            _bytesToAlloc = (nuint)bytes;

            _ThreadBuffer = new ThreadLocal<nuint>(() => (nuint)AlignedAllocZeroed(_bytesToAlloc, AllocAlignment));
        }

        public uint GetHashValue()
        {
            uint hashValue = 0xEC42E90Du;

            //hashValue ^= HalfKP.TransformedFeatureDimensions * 2;
            //hashValue ^= InputLayer.GetHashValue();
            //hashValue ^= fc_0.GetHashValue();
            //hashValue ^= ac_0.GetHashValue();
            //hashValue ^= fc_1.GetHashValue();
            //hashValue ^= ac_1.GetHashValue();
            //hashValue ^= fc_2.GetHashValue();

            return hashValue;
        }

        public bool ReadParameters(BinaryReader br)
        {
            fc_0.ReadParameters(br);
            fc_1.ReadParameters(br);
            fc_2.ReadParameters(br);
            return true;
        }

        public int Propagate(Span<sbyte> transformedFeatures)
        {
            var _buffer = _ThreadBuffer.Value;
            NativeMemory.Clear((void*)_buffer, _bytesToAlloc);

            Span<int>   fc_0_out     = new Span<int>   ((void*) (_buffer + fc_0_idx    ), fc_0.BufferSize);
            Span<sbyte> ac_0_out     = new Span<sbyte> ((void*) (_buffer + ac_0_idx    ), ac_0.BufferSize);

            Span<int>   fc_1_out     = new Span<int>   ((void*) (_buffer + fc_1_idx    ), fc_1.BufferSize);
            Span<sbyte> ac_1_out     = new Span<sbyte> ((void*) (_buffer + ac_1_idx    ), ac_1.BufferSize);

            Span<int>   fc_2_out     = new Span<int>   ((void*) (_buffer + fc_2_idx    ), fc_2.BufferSize);

            fc_0.Propagate(transformedFeatures, fc_0_out);
            ac_0.Propagate(fc_0_out, ac_0_out);

            fc_1.Propagate(ac_0_out, fc_1_out);
            ac_1.Propagate(fc_1_out, ac_1_out);

            fc_2.Propagate(ac_1_out, fc_2_out);

            var output = fc_2_out[0];
            return output / 16;
        }
    }
}
