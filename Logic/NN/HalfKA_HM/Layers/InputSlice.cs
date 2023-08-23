

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    public class InputSlice
    {
        public readonly int Offset = 0;

        public readonly int OutputDimensions;
        public static readonly int BufferSize = 0;

        public InputSlice(int outDims, int offset = 0)
        {
            this.OutputDimensions = outDims;
            this.Offset = offset;
        }

        [MethodImpl(Inline)]
        public Span<sbyte> Propagate(Span<sbyte> transformedFeatures, Span<byte> buffer)
        {
            return transformedFeatures;
        }

        public bool ReadParameters(BinaryReader br)
        {
            return true;
        }


        public uint GetHashValue()
        {
            uint hashValue = 0xEC42E90Du;
            hashValue ^= (uint)(OutputDimensions ^ (Offset << 10));
            return hashValue;
        }
    }
}
