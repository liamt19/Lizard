



using static LTChess.Logic.NN.HalfKP.NNCommon;
using static LTChess.Logic.NN.HalfKP.HalfKP;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Numerics;
using System.Reflection;

namespace LTChess.Logic.NN.HalfKP
{
    /// <summary>
    /// Handles Accumulator updates and refreshes, and translates the position into inputs for the network to use.
    /// <para></para>
    /// 
    /// https://github.com/official-stockfish/Stockfish/blob/84f3e867903f62480c33243dd0ecbffd342796fc/src/nnue/nnue_feature_transformer.h
    /// </summary>
    public unsafe class FeatureTransformer
    {
        // Number of output dimensions for one side
        public const uint HalfDimensions = HalfKP.TransformedFeatureDimensions;

        public const uint InputDimensions = HalfKP.Dimensions;
        public const uint OutputDimensions = HalfDimensions * 2;

        public const long BufferSize = OutputDimensions * sizeof(ushort);

        public static uint GetHashValue() => HalfKP.HashValue(Color.White) ^ OutputDimensions;

        public short[] Biases = new short[HalfDimensions];
        public short[] Weights = new short[HalfDimensions * InputDimensions];


        /// <summary>
        /// The number of items within the Vector256<T> that this class uses, which is 32 / sizeof(short) = 16.
        /// </summary>
        public static int VectorSize = VSize.Short;


        /// <summary>
        /// Takes the input from the <paramref name="accumulator"/> and places them into <paramref name="output"/>,
        /// refreshing the <paramref name="accumulator"/> if necessary.
        /// </summary>
        [MethodImpl(Optimize)]
        public void TransformFeatures(Position pos, sbyte[] output, ref Accumulator accumulator)
        {
            if (accumulator.NeedsRefresh)
            {
                RefreshAccumulator(pos, ref accumulator);
            }


            uint NumChunks = HalfDimensions / SimdWidth;
            const int Control = 0b11011000;
            Vector256<sbyte> Zero = Vector256<sbyte>.Zero;

            int[] perspectives = { pos.ToMove, Not(pos.ToMove) };
            for (int p = 0; p < 2; p++)
            {
                uint offset = (uint)(HalfDimensions * p);

                var accumulation = accumulator[perspectives[p]];

                for (int j = 0; j < NumChunks; ++j)
                {
                    int vectIndex = (int) (offset + (j * VSize.SByte));

                    Vector256<short> sum0 = Load256(accumulation, (j * 2 + 0) * VSize.Short);
                    Vector256<short> sum1 = Load256(accumulation, (j * 2 + 1) * VSize.Short);

                    Vector256<sbyte> saturated = Avx2.PackSignedSaturate(sum0, sum1);
                    Vector256<sbyte> maxVec = Avx2.Max(saturated, Zero);
                    Vector256<long> permuted = Avx2.Permute4x64(maxVec.AsInt64(), Control);

                    Vector256<sbyte> toStore = permuted.AsSByte();
                    Avx2.Store((sbyte*)UnsafeAddrOfPinnedArrayElementUnchecked(output, vectIndex), toStore);
                }
            }
        }


        /// <summary>
        /// Finds the active features (existing pieces on the board) and updates the Accumulator to include those pieces.
        /// <br></br>
        /// This is comparatively very slow, so it should only be done when absolutely necessary, like when our king moves.
        /// </summary>
        [MethodImpl(Inline)]
        public void RefreshAccumulator(Position pos, ref Accumulator accumulator)
        {
            Span<int> active = stackalloc int[MaxActiveDimensions * 2];
            HalfKP.AppendActiveIndices(pos, active);

            for (int perspective = 0; perspective < ColorNB; perspective++)
            {
                var accumulation = accumulator[perspective];

                //  memcpy(accumulator.accumulation[perspective], biases, HalfDimensions * sizeof(BiasType));
                Buffer.BlockCopy(Biases, 0, accumulation, 0, (int)(HalfDimensions * sizeof(short)));

                int i = 0;
                while (i < MaxActiveDimensions)
                {
                    int index = active[(i++) + (perspective * MaxActiveDimensions)];
                    if (index <= 0)
                    {
                        break;
                    }

                    uint offset = (uint)(HalfDimensions * index);

                    uint NumChunks = HalfDimensions / (SimdWidth / 2);

                    for (int j = 0; j < NumChunks; j++)
                    {
                        int vectIndex = j * VSize.Short;
                        Vector256<short> inV = Load256(accumulation, vectIndex);

                        int columnIndex = (int)(offset + (j * VSize.Short));
                        Vector256<short> row = Load256(Weights, columnIndex);

                        inV = Avx2.Add(inV, row);

                        Store256(ref inV, accumulation, vectIndex);

                    }
                }
            }

            accumulator.NeedsRefresh = false;
        }



        /// <summary>
        /// Reads the weights and biases from the network file.
        /// </summary>
        public bool ReadParameters(BinaryReader br)
        {

            uint header = br.ReadUInt32();
            Debug.WriteLine("FeatureTransformer header: " + header.ToString("X"));

            for (int i = 0; i < HalfDimensions; i++)
            {
                if (IsLEB128)
                {
                    //  TODO: this obviously won't work
                    Biases[i] = (short)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                }
                else
                {
                    Biases[i] = br.ReadInt16();
                }
            }

            for (int i = 0; i < HalfDimensions * InputDimensions; i++)
            {
                if (IsLEB128)
                {
                    Weights[i] = (short)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                }
                else
                {
                    Weights[i] = br.ReadInt16();
                }
            }

            return true;
        }

    }
}
