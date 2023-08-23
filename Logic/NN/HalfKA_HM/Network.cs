

using static LTChess.Logic.NN.HalfKA_HM.FeatureTransformer;
using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System;

using LTChess.Logic.Data;
using LTChess.Logic.NN.HalfKA_HM.Layers;
using LTChess.Properties;

using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM.UniquePiece;

namespace LTChess.Logic.NN.HalfKA_HM
{
    public class Network
    {
        public const int SliceSize_HM = 1024 * 2;
        public const int Layer1Size_HM = 8;

        public const int SliceSize = 512 * 2;
        public const int Layer1Size = 16;
        public const int Layer2Size = 32;
        public const int OutputSize = 1;


        public InputSlice InputLayer;
        public AffineTransform TransformLayer1;
        public ClippedReLU HiddenLayer1;

        public AffineTransform TransformLayer2;
        public ClippedReLU HiddenLayer2;

        /// <summary>
        /// The final layer of the network, which outputs 1 integer representing the final evaluation
        /// </summary>
        public AffineTransform OutputLayer;

        public int NetworkSize => OutputLayer.BufferSize;

        public Network(int inputSize = SliceSize, int layerSize1 = Layer1Size, int layerSize2 = Layer2Size, int outputSize = OutputSize)
        {
            InputLayer = new InputSlice(inputSize);

            TransformLayer1 = new AffineTransform(InputLayer, layerSize1);
            HiddenLayer1 = new ClippedReLU(TransformLayer1);

            TransformLayer2 = new AffineTransform(HiddenLayer1, layerSize2);
            HiddenLayer2 = new ClippedReLU(TransformLayer2);

            OutputLayer = new AffineTransform(HiddenLayer2, outputSize);
        }
    }
}
