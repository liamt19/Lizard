using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System;
using System.Net;
using System.Numerics;
using System.Reflection;

namespace LTChess.Logic.NN.HalfKA_HM.Layers
{
    /// <summary>
    /// This takes the place of AffineTransform's "large specialization".
    /// <para></para> 
    /// Adapted from https://github.com/official-stockfish/Stockfish/blob/38e61663d836e062af0bc002814ad5149c4b7729/src/nnue/layers/affine_transform_sparse_input.h
    /// </summary>
    public unsafe class AffineTransformSparse
    {
        private static readonly ushort[][] lookup_indices;
        private static readonly int[] lookup_count;
        static AffineTransformSparse()
        {
            Zero = Vector256.Create(0);
            Eight = Vector128.Create((ushort) 8);

            lookup_indices = new ushort[256][];
            for (int i = 0; i < 256; i++)
            {
                lookup_indices[i] = new ushort[8];
                int j = i;
                int k = 0;
                while (j != 0)
                {
                    uint lsbIndex = uint.TrailingZeroCount((uint)j);
                    j &= j - 1;
                    lookup_indices[i][k] = (ushort)lsbIndex;
                    ++k;
                }
            }

            lookup_count = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int j = i;
                int k = 0;
                while (j != 0)
                {
                    j &= j - 1;
                    ++k;
                }
                lookup_count[i] = (int) k;
            }
        }



        public const int ChunkSize = 4;

        public readonly int InputDimensions;
        public readonly int OutputDimensions;

        public readonly int BufferSize;
        public readonly int BufferSizeBytes;

        public readonly int PaddedInputDimensions;

        public readonly Vector256<int>* Biases;
        public readonly Vector256<sbyte>* Weights;

        public readonly int NNZ_Size;
        private static readonly Vector256<int> Zero;
        private static readonly Vector128<ushort> Eight;

        public AffineTransformSparse(int inDims, int outDims)
        {
            InputDimensions = inDims;
            OutputDimensions = outDims;

            PaddedInputDimensions = CeilToMultiple((short)InputDimensions, MaxSimdWidth);

            BufferSize = CeilToMultiple((short)OutputDimensions, MaxSimdWidth);
            BufferSizeBytes = BufferSize * sizeof(int);

            Weights = (Vector256<sbyte>*)  AlignedAllocZeroed((nuint)((OutputDimensions * PaddedInputDimensions) / VSize.SByte * 32), AllocAlignment);
            Biases  = (Vector256<int>*)    AlignedAllocZeroed((nuint)(Math.Max(1, (OutputDimensions) / VSize.Int) * 32),              AllocAlignment);

            NNZ_Size = CeilToMultiple((short)InputDimensions, 8) / 4;
        }

        public int FindNNZ(Span<int> input, Span<ushort> output)
        {
            const int InputSimdWidth = VSize.Int;
            const int ChunkSize = 8;
            const int InputsPerChunk = ChunkSize / InputSimdWidth;
            const int OutputsPerChunk = ChunkSize / 8;

            int count = 0;
            Vector128<ushort> baseVal = Vector128.Create((ushort)0);

            int NumChunks = NNZ_Size / ChunkSize;
            for (int i = 0; i < NumChunks; i++)
            {
                uint nnz = 0;
                for (int j = 0; j < InputsPerChunk; j++)
                {
                    Vector256<int> chunk = LoadSpan256(input, (i * InputsPerChunk + j) * VSize.Int);
                    Vector256<int> cmpgt = Avx2.CompareGreaterThan(chunk, Zero);
                    int mask = Avx.MoveMask(cmpgt.AsSingle());
                    nnz |= (uint)(mask) << (j * InputSimdWidth);
                }

                for (int j = 0; j < OutputsPerChunk; j++)
                {
                    var lookup = (nnz >> (j * 8)) & 0xFF;
                    Vector128<ushort> offsets;
                    fixed (ushort* ptr = &(lookup_indices[lookup][0]))
                    {
                        offsets = Sse2.LoadVector128(ptr);
                    }

                    var toStore = Sse2.Add(baseVal, offsets);
                    Sse2.Store((ushort*) Unsafe.AsPointer(ref output[count]), toStore);
                    count += lookup_count[lookup];
                    baseVal = Sse2.Add(baseVal, Eight);
                }
            }

            return count;
        }

        public void Propagate(Span<sbyte> input, Span<int> output)
        {
            const int OutputSimdWidth = VSize.UInt;
            var input32 = MemoryMarshal.Cast<sbyte, int>(input);

            int NumRegs = OutputDimensions / OutputSimdWidth;
            Span<ushort> nnz = stackalloc ushort[NNZ_Size];

            // Find indices of nonzero 32bit blocks
            int count = FindNNZ(input32, nnz);

            Span<Vector256<int>> outs = stackalloc Vector256<int>[NumRegs];
            for (int k = 0; k < NumRegs; k++)
            {
                outs[k] = Biases[k];
            }

            for (int j = 0; j < count; ++j)
            {
                var i = nnz[j];
                Vector256<byte> in0 = Vector256.Create(input32[i]).AsByte();

                for (int k = 0; k < NumRegs; ++k)
                {
                    AffineTransform.m256_add_dpbusd_epi32(ref outs[k], in0, Weights[((i * OutputDimensions * ChunkSize) / VSize.SByte) + k]);
                }
            }

            for (int k = 0; k < NumRegs; k++)
            {
                StoreSpan256(ref outs[k], output, k * VSize.Int);
            }
        }








        public bool ReadParameters(BinaryReader br)
        {

            int[] _Biases = new int[OutputDimensions];
            sbyte[] _Weights = new sbyte[OutputDimensions * PaddedInputDimensions];

            try
            {

                for (int i = 0; i < OutputDimensions; i++)
                {
                    _Biases[i] = br.ReadInt32();
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i++)
                {
                    uint cursedIndex = GetWeightIndex(i);
                    _Weights[cursedIndex] = br.ReadSByte();
                }

                for (int i = 0; i < OutputDimensions; i += VSize.Int)
                {
                    Biases[i / VSize.Int] = Load256(_Biases, i);
                }

                for (int i = 0; i < OutputDimensions * PaddedInputDimensions; i += VSize.SByte)
                {
                    Weights[i / VSize.SByte] = Load256(_Weights, i);
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return false;
            }

            return true;
        }

        public uint GetHashValue(uint prevHash)
        {
            uint hashValue = 0xCC03DAE4u;
            hashValue += (uint)OutputDimensions;

            hashValue ^= prevHash >> 1;
            hashValue ^= prevHash << 31;

            return hashValue;
        }



        [MethodImpl(Inline)]
        private uint GetWeightIndex(int i)
        {
            return (uint) ((i / 4) % (PaddedInputDimensions / 4) * OutputDimensions * 4 + i / PaddedInputDimensions * 4 + i % 4);
        }
    }
}
