using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Numerics;
using System.Reflection;
using System.IO;
using LTChess.Logic.Search;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LTChess.Logic.NN.HalfKA_HM
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
        public const uint HalfDimensions = HalfKA_HM.TransformedFeatureDimensions;

        public const uint InputDimensions = HalfKA_HM.Dimensions;
        public const uint OutputDimensions = HalfDimensions * 2;

        public const int BufferSize = (int) (OutputDimensions * sizeof(ushort));

        public static uint GetHashValue() => HalfKA_HM.HashValue ^ OutputDimensions;

        public static Vector256<short>* Biases;
        public static Vector256<short>* Weights;
        public static Vector256<int>* PSQTWeights;

        private static readonly Vector256<sbyte> Zero = Vector256<sbyte>.Zero;

        public const int NumRegs = 16;
        public const int NumPsqtRegs = 1;

        public const int TileHeight = NumRegs * VSize.Byte / 2;
        public const int PsqtTileHeight = NumPsqtRegs * VSize.Byte / 4;

        static FeatureTransformer()
        {
            Biases = (Vector256<short>*) NativeMemory.AlignedAlloc((nuint)((HalfDimensions) / VSize.Short * 32), 32);
            Weights = (Vector256<short>*) NativeMemory.AlignedAlloc((nuint)((HalfDimensions * InputDimensions) / VSize.Short * 32), 32);
            PSQTWeights = (Vector256<int>*) NativeMemory.AlignedAlloc((nuint)((InputDimensions * PSQTBuckets) / VSize.Int * 32), 32);
        }

        /// <summary>
        /// Takes the input from the <paramref name="accumulator"/> and places them into <paramref name="output"/>,
        /// refreshing the <paramref name="accumulator"/> if necessary.
        /// </summary>
        [MethodImpl(Optimize)]
        public int TransformFeatures(Position pos, Span<sbyte> output, ref AccumulatorPSQT accumulator, int bucket)
        {
            if (accumulator.NeedsRefresh)
            {
                RefreshAccumulator(pos, ref accumulator);
            }
            
            const uint NumChunks = HalfDimensions / SimdWidth;
            const int Control = 0b11011000;

            Span<int> perspectives = stackalloc int[2] { pos.ToMove, Not(pos.ToMove) };

            var psqt = (accumulator.PSQ(perspectives[0])[0][bucket] -
                        accumulator.PSQ(perspectives[1])[0][bucket]) / 2;

            for (int p = 0; p < 2; p++)
            {
                var accumulation = accumulator[perspectives[p]];

                for (int j = 0; j < NumChunks; ++j)
                {

                    Vector256<short> sum0 = accumulation[j * 2 + 0];
                    Vector256<short> sum1 = accumulation[j * 2 + 1];

                    Vector256<sbyte> saturated = Avx2.PackSignedSaturate(sum0, sum1);
                    Vector256<sbyte> maxVec = Avx2.Max(saturated, Zero);
                    Vector256<long> permuted = Avx2.Permute4x64(maxVec.AsInt64(), Control);

                    Vector256<sbyte> toStore = permuted.AsSByte();
                    StoreSpan256(ref toStore, output, (int)((uint)(HalfDimensions * p) + (j * VSize.SByte)));
                }
            }

            return psqt;
        }

        /// <summary>
        /// Finds the active features (existing pieces on the board) and updates the Accumulator to include those pieces.
        /// This is comparatively very slow, so it should only be done when absolutely necessary, like when our king moves.
        /// </summary>
        [MethodImpl(Inline)]
        public void RefreshAccumulator(Position pos, ref AccumulatorPSQT accumulator)
        {
            const int RelativeWeightIndex = (int)HalfDimensions / 16;
            const int RelativeTileHeight = TileHeight / 16;

            Span<int> active = stackalloc int[MaxActiveDimensions * 2];
            
            if (SkipInitEnabled)
            {
                active.Clear();
            }

            HalfKA_HM.AppendActiveIndices(pos, active);

            for (int perspective = 0; perspective < ColorNB; perspective++)
            {
                var accumulation = accumulator[perspective];
                var PSQTaccumulation = accumulator.PSQ(perspective);

                Span<Vector256<short>> acc = stackalloc Vector256<short>[NumRegs];

                for (int j = 0; j < HalfDimensions / TileHeight; j++)
                {
                    for (int k = 0; k < NumRegs; k++)
                    {
                        acc[k] = Biases[((j * RelativeTileHeight) + k)];
                    }

                    int i = 0;
                    while (i < MaxActiveDimensions)
                    {
                        int index = active[(i++) + (perspective * MaxActiveDimensions)];
                        if (index <= 0)
                        {
                            break;
                        }

                        for (int k = 0; k < NumRegs; k++)
                        {
                            Vector256<short> column = Weights[((RelativeWeightIndex * index + j * RelativeTileHeight) + k)];
                            acc[k] = Add256(acc[k], column);
                        }
                    }

                    for (int k = 0; k < NumRegs; k++)
                    {
                        accumulation[(j * NumRegs) + k] = acc[k];

                    }
                }

                Span<Vector256<int>> psq = stackalloc Vector256<int>[NumPsqtRegs];
                for (int j = 0; j < PSQTBuckets / PsqtTileHeight; j++)
                {
                    for (int k = 0; k < NumPsqtRegs; k++)
                    {
                        psq[k] = Vector256<int>.Zero;
                    }

                    int i = 0;
                    while (i < MaxActiveDimensions)
                    {
                        int index = active[(i++) + (perspective * MaxActiveDimensions)];
                        if (index <= 0)
                        {
                            break;
                        }

                        for (int k = 0; k < NumPsqtRegs; k++)
                        {
                            Vector256<int> column = PSQTWeights[(index + j * PsqtTileHeight)];

                            psq[k] = Add256(psq[k], column);
                        }
                    }

                    for (int k = 0; k < NumPsqtRegs; k++)
                    {
                        PSQTaccumulation[k] = psq[k];
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

            short[] _Biases = new short[HalfDimensions];
            short[] _Weights = new short[HalfDimensions * InputDimensions];
            int[] _PSQTWeights = new int[InputDimensions * PSQTBuckets];

            uint header = br.ReadUInt32();
            Debug.WriteLine("FeatureTransformer header: " + header.ToString("X"));

            for (int i = 0; i < HalfDimensions; i++)
            {
                if (IsLEB128)
                {
                    //  TODO: this obviously won't work
                    _Biases[i] = (short)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                }
                else
                {
                    _Biases[i] = br.ReadInt16();
                }
            }

            for (int i = 0; i < HalfDimensions * InputDimensions; i++)
            {
                if (IsLEB128)
                {
                    _Weights[i] = (short)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                }
                else
                {
                    _Weights[i] = br.ReadInt16();
                }
            }

            for (int i = 0; i < PSQTBuckets * InputDimensions; i++)
            {
                if (IsLEB128)
                {
                    _PSQTWeights[i] = (int)LEB128.LEB128.ReadLEB128Signed(br.BaseStream);
                }
                else
                {
                    _PSQTWeights[i] = br.ReadInt32();
                }
            }


            for (int i = 0; i < HalfDimensions; i += VSize.Short)
            {
                Biases[i / VSize.Short] = Load256(_Biases, i);
            }

            for (int i = 0; i < HalfDimensions * InputDimensions; i += VSize.Short)
            {
                Weights[i / VSize.Short] = Load256(_Weights, i);
            }

            for (int i = 0; i < PSQTBuckets * InputDimensions; i += VSize.Int)
            {
                PSQTWeights[i / VSize.Int] = Load256(_PSQTWeights, i);
            }

            return true;
        }

    }
}
