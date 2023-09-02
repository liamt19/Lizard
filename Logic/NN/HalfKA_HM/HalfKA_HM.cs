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
using LTChess.Logic.Core;
using System.Text;
using System.Runtime.InteropServices;

namespace LTChess.Logic.NN.HalfKA_HM
{
    /// <summary>
    /// Uses SFNNv3 architecture, 
    /// https://raw.githubusercontent.com/glinscott/nnue-pytorch/master/docs/img/SFNNv3_architecture_detailed_v2.svg
    /// 
    /// 
    /// </summary>
    public static unsafe class HalfKA_HM
    {
        /// <summary>
        /// NNUE-PyTorch (https://github.com/glinscott/nnue-pytorch/tree/master) compresses networks using LEB128,
        /// but older Stockfish networks didn't do this.
        /// </summary>
        public const bool IsLEB128 = false;

        public const string BestNet = @"nn-735bba95dec0.nnue";

        public const string Name = "HalfKAv2_hm(Friend)";

        public const uint VersionValue = 0x7AF32F20u;
        public const uint HashValue = 0x7F234CB8u;
        public const uint Dimensions = SquareNB * PS_NB / 2;
        public const int TransformedFeatureDimensions = 1024;

        public const int MaxActiveDimensions = 32;

        /// <summary>
        /// King squares * Piece squares * Piece types * Colors == 40960
        /// </summary>
        private const int FeatureCount = SquareNB * SquareNB * PieceNB * ColorNB;

        private const int OutputDimensions = 512 * 2;

        private static AccumulatorPSQT[] AccumulatorStack;
        private static int CurrentAccumulator;

        private static Network[] LayerStack;

        private static FeatureTransformer Transformer;


        private static nint _FeatureBuffer;
        private const int _TransformedFeaturesBufferLength = FeatureTransformer.BufferSize;

        private static nint _LayerBuffer;
        private const int _LayerBufferLength = 384;

        private static bool Initialized = false;

        static HalfKA_HM()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            if (Initialized || !UseHalfKA)
            {
                return;
            }

            Initialized = true;


            AccumulatorStack = new AccumulatorPSQT[MaxListCapacity];
            for (int i = 0; i < MaxListCapacity; i++)
            {
                AccumulatorStack[i] = new AccumulatorPSQT();
            }
            CurrentAccumulator = 0;


            //  Set up the network architecture layers
            LayerStack = new Network[LayerStacks];
            for (int i = 0; i < LayerStacks; i++)
            {
                LayerStack[i] = new Network(inputSize: Network.SliceSize_HM, layerSize1: Network.Layer1Size_HM);
            }


            //  Set up the feature transformer
            Transformer = new FeatureTransformer();


            _FeatureBuffer = (nint) NativeMemory.AlignedAlloc((sizeof(sbyte) * _TransformedFeaturesBufferLength), AllocAlignment);
            _LayerBuffer   = (nint) NativeMemory.AlignedAlloc((sizeof(byte ) * _LayerBufferLength),               AllocAlignment);


            Stream kpFile;
            byte[] buff;

            string networkToLoad = @"nn.nnue";

            if (File.Exists(networkToLoad))
            {
                buff = File.ReadAllBytes(networkToLoad);
                Log("Using NNUE with HalfKA_v2_hm network " + networkToLoad);
            }
            else
            {
                //  Just load the default network
                networkToLoad = BestNet;
                Log("Using embedded NNUE with HalfKA_v2_hm network " + networkToLoad);

                string resourceName = (networkToLoad.Replace(".nnue", string.Empty));
                try
                {
                    buff = (byte[])Resources.ResourceManager.GetObject(resourceName);
                }
                catch (Exception e)
                {
                    //  TODO: have a fallback here for classical eval
                    Log("Tried to load '" + resourceName + "' from embedded resources failed!");
                    throw;
                }
                
            }

            kpFile = new MemoryStream(buff);

            using BinaryReader br = new BinaryReader(kpFile);
            ReadHeader(br);
            ReadTransformParameters(br);
            ReadNetworkParameters(br);

            kpFile.Dispose();
        }

        /// <summary>
        /// Resets <see cref="CurrentAccumulator"/> to 0
        /// </summary>
        [MethodImpl(Inline)]
        private static void ResetAccumulator() => CurrentAccumulator = 0;

        /// <summary>
        /// Copies the current accumulator to the next accumulator in the stack, and increments <see cref="CurrentAccumulator"/>
        /// </summary>
        [MethodImpl(Inline)]
        private static void PushAccumulator() => AccumulatorStack[CurrentAccumulator].CopyTo(ref AccumulatorStack[++CurrentAccumulator]);

        /// <summary>
        /// Decrements <see cref="CurrentAccumulator"/>
        /// </summary>
        [MethodImpl(Inline)]
        private static void PullAccumulator() => CurrentAccumulator--;



        public static void ReadHeader(BinaryReader br)
        {
            

            uint version = br.ReadUInt32();
            if (version != VersionValue)
            {
                Debug.WriteLine("Expected header version " + VersionValue.ToString("X") + " but got " + version.ToString("X"));
            }

            uint hashValue = br.ReadUInt32();
            uint netHash = LayerStack[0].OutputLayer.GetHashValue();
            uint ftHash = FeatureTransformer.GetHashValue();
            uint finalHash = (netHash ^ ftHash);

            if (hashValue != finalHash)
            {
                Debug.WriteLine("Expected header hash " + hashValue.ToString("X") + " but got " + finalHash.ToString("X"));
            }

            uint size = br.ReadUInt32();

            byte[] archBuffer = new byte[size];
            br.Read(archBuffer);

            string arch = System.Text.Encoding.UTF8.GetString(archBuffer);
            Debug.WriteLine("Network architecture:");
            Debug.WriteLine(arch);
        }

        /// <summary>
        /// Reads the feature transformer weights and biases from the network.
        /// </summary>
        [MethodImpl(Inline)]
        public static void ReadTransformParameters(BinaryReader br)
        {
            Transformer.ReadParameters(br);
        }

        /// <summary>
        /// Reads the weights and biases for each of the layers in the network.
        /// 
        /// Layers closer to the input (first) layer get their weights first.
        /// </summary>
        /// <param name="br"></param>
        public static void ReadNetworkParameters(BinaryReader br)
        {
            for (int i = 0; i < LayerStacks; i++)
            {
                uint header = br.ReadUInt32();
                LayerStack[i].OutputLayer.ReadParameters(br);
            }
        }



        /// <summary>
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetEvaluation(Position pos, bool adjusted = true)
        {
            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];
            int bucket = (int) (popcount(pos.bb.Occupancy) - 1) / 4;

            Span<sbyte> features = new Span<sbyte>((void*) _FeatureBuffer, _TransformedFeaturesBufferLength);
            Span<byte> layerBuff = new Span< byte>((void*) _LayerBuffer,               _LayerBufferLength);

            int psqt = Transformer.TransformFeatures(pos, features, ref Accumulator, bucket);
            var output = LayerStack[bucket].OutputLayer.Propagate(features, layerBuff);

            /**
            
            if (adjusted)
            {
                //  Not worrying about this for now.
                int delta_npm = Math.Abs((pos.MaterialCount[White] - ((int)popcount(pos.bb.Pieces[Pawn] & pos.bb.Colors[White]) * GetPieceValue(Pawn)))
                                         - (pos.MaterialCount[Black] - ((int)popcount(pos.bb.Pieces[Pawn] & pos.bb.Colors[Black]) * GetPieceValue(Pawn))));
                int entertainment = (adjusted && delta_npm <= GetPieceValue(Bishop) - GetPieceValue(Knight) ? 7 : 0);

                int A = 128 - entertainment;
                int B = 128 + entertainment;

                int sum = (A * psqt + B * output[0]) / 128;
                return sum / NNCommon.OutputScale;
            }

             */

            return (psqt + output[0]) / 16;
        }

        public static void Trace(Position pos)
        {
            char[][] board = new char[3 * 8 + 1][];
            for (int i = 0; i < 3 * 8 + 1; i++)
            {
                board[i] = new char[8 * 8 + 2];
                Array.Fill(board[i], ' ');
            }

            

            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                board[row][8 * 8 + 1] = '\0';
            }

            void writeSquare(int file, int rank, int pc, int value)
            {
                const string PieceToChar = " PNBRQK  pnbrqk";

                int x = (file) * 8;
                int y = (7 - rank) * 3;

                for (int i = 1; i < 8; i++)
                {
                    board[y][x + i] = board[y + 3][x + i] = '-';
                }

                for (int i = 1; i < 3; i++)
                {
                    board[y + i][x] = board[y + i][x + 8] = '|';
                }

                board[y][x] = board[y][x + 8] = board[y + 3][x + 8] = board[y + 3][x] = '+';

                if (pc != PS_NONE && !(pc == 15 && value == ScoreMate))
                {
                    board[y + 1][x + 4] = PieceToChar[pc];
                }

                if (value != ScoreMate)
                {
                    unsafe
                    {
                        fixed (char* ptr = &board[y + 2][x + 2])
                        {
                            format_cp_ptr(value, ptr);
                        }
                    }
                }
                    
            }

            int baseEval = GetEvaluation(pos, false);

            Log("\nNNUE evaluation: " +  baseEval + "\n");

            Bitboard bb = pos.bb;
            for (int f = Files.A; f <= Files.H; f++)
            {
                for (int r = 0; r <= 7; r++)
                {
                    int idx = CoordToIndex(f, r);
                    int pt = bb.GetPieceAtIndex(idx);
                    int pc = bb.GetColorAtIndex(idx);
                    int fishPc = FishPiece(pt, pc);
                    int v = ScoreMate;

                    if (pt != None && bb.GetPieceAtIndex(idx) != King)
                    {
                        bb.Pieces[pt] ^= SquareBB[idx];
                        bb.Colors[pc] ^= SquareBB[idx];
                        bb.PieceTypes[idx] = None;

                        AccumulatorStack[CurrentAccumulator].NeedsRefresh = true;
                        int eval = GetEvaluation(pos, false);
                        v = baseEval - eval;

                        bb.Pieces[pt] ^= SquareBB[idx];
                        bb.Colors[pc] ^= SquareBB[idx];
                        bb.PieceTypes[idx] = pt;
                    }

                    writeSquare(f, r, fishPc, v);
                }
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }

            Log("\n");

            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];
            int correctBucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;
            int networkSize = LayerStack[0].NetworkSize;

            Span<sbyte> transformed_features = stackalloc sbyte[FeatureTransformer.BufferSize];
            Span<byte> buffer = stackalloc byte[networkSize];

            Log("Bucket\t\tPSQT\t\tPositional\tTotal");
            for (int bucket = 0; bucket < LayerStacks; bucket++)
            {

                int psqt = Transformer.TransformFeatures(pos, transformed_features, ref Accumulator, bucket);
                var output = LayerStack[bucket].OutputLayer.Propagate(transformed_features, buffer);

                psqt /= 16;
                output[0] /= 16;

                Log(bucket + "\t\t" + psqt + "\t\t" + output[0] + "\t\t" + (psqt + output[0]) +
                    (bucket == correctBucket ? "\t<-- this bucket is used" : string.Empty));

                buffer.Clear();
            }

        }


        /// <summary>
        /// Undoes a move that was previously made by reverting to the previous accumulator.
        /// </summary>
        [MethodImpl(Inline)]
        public static void UnmakeMoveNN()
        {
            PullAccumulator();
        }

        /// <summary>
        /// Resets the current accumulator to the first accumulator in the stack.
        /// </summary>
        [MethodImpl(Inline)]
        public static void ResetNN()
        {
            ResetAccumulator();
        }

        /// <summary>
        /// Refreshes the current accumulator using the active features in the position <paramref name="pos"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static void RefreshNN(Position pos)
        {
            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];
            Transformer.RefreshAccumulator(pos, ref Accumulator);
        }


        /// <summary>
        /// Updates the features in the next accumulator by copying the current features and adding/removing
        /// the features that will be changing. 
        /// <br></br>
        /// If <paramref name="m"/> is a king move, the next accumulator
        /// will only be marked as needing a refresh.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool MakeMove(Position pos, Move m)
        {
            Bitboard bb = pos.bb;

            int moveFrom = m.From;
            int moveTo = m.To;

            PushAccumulator();
            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];

            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            if (ourPiece == Piece.King)
            {
                Accumulator.NeedsRefresh = true;
                return false;
            }
            else
            {
                //  Otherwise, we only need to remove the features that are no longer there (move.From) and the piece that was on
                //  move.To before it was captured, and add the new features (move.To).

                int us = bb.GetColorAtIndex(moveFrom);
                int them = Not(us);
                int theirPiece = bb.GetPieceAtIndex(moveTo);

                int ourKing = bb.KingIndex(us);
                int theirKing = bb.KingIndex(them);

                var ourAccumulation = Accumulator[us];
                var theirAccumulation = Accumulator[them];

                var ourPsq = Accumulator.PSQ(us);
                var theirPsq = Accumulator.PSQ(them);

                int ourPieceOldIndex_US =   HalfKAIndex(us, moveFrom, FishPiece(ourPiece, us), ourKing);
                int ourPieceOldIndex_THEM = HalfKAIndex(them, moveFrom, FishPiece(ourPiece, us), theirKing);

                RemoveFeature(ourAccumulation, ourPsq, ourPieceOldIndex_US);
                RemoveFeature(theirAccumulation, theirPsq, ourPieceOldIndex_THEM);

                if (!m.Promotion)
                {
                    int ourPieceNewIndex_US =   HalfKAIndex(us, moveTo, FishPiece(ourPiece, us), ourKing);
                    int ourPieceNewIndex_THEM = HalfKAIndex(them, moveTo, FishPiece(ourPiece, us), theirKing);

                    AddFeature(ourAccumulation, ourPsq, ourPieceNewIndex_US);
                    AddFeature(theirAccumulation, theirPsq, ourPieceNewIndex_THEM);
                }
                else
                {
                    //  Add the promotion piece instead.
                    int ourPieceNewIndex_US =   HalfKAIndex(us, moveTo, FishPiece(m.PromotionTo, us), ourKing);
                    int ourPieceNewIndex_THEM = HalfKAIndex(them, moveTo, FishPiece(m.PromotionTo, us), theirKing);

                    AddFeature(ourAccumulation, ourPsq, ourPieceNewIndex_US);
                    AddFeature(theirAccumulation, theirPsq, ourPieceNewIndex_THEM);
                }

                if (m.Capture)
                {
                    //  A captured piece needs to be removed from both perspectives as well.
                    int theirCapturedPieceIndex_US =    HalfKAIndex(us, moveTo, FishPiece(theirPiece, Not(us)), ourKing);
                    int theirCapturedPieceIndex_THEM =  HalfKAIndex(them, moveTo, FishPiece(theirPiece, Not(us)), theirKing);

                    RemoveFeature(ourAccumulation, ourPsq, theirCapturedPieceIndex_US);
                    RemoveFeature(theirAccumulation, theirPsq, theirCapturedPieceIndex_THEM);

                }

                if (m.EnPassant)
                {
                    int idxPawn = (bb.Pieces[Piece.Pawn] & SquareBB[pos.EnPassantTarget - 8]) != 0 ? pos.EnPassantTarget - 8 : pos.EnPassantTarget + 8;

                    int theirCapturedPieceIndex_US =    HalfKAIndex(us, idxPawn, FishPiece(Piece.Pawn, Not(us)), ourKing);
                    int theirCapturedPieceIndex_THEM =  HalfKAIndex(them, idxPawn, FishPiece(Piece.Pawn, Not(us)), theirKing);

                    RemoveFeature(ourAccumulation, ourPsq, theirCapturedPieceIndex_US);
                    RemoveFeature(theirAccumulation, theirPsq, theirCapturedPieceIndex_THEM);
                }

                if (m.Castle)
                {

                    int rookFrom = moveTo switch
                    {
                        C1 => A1,
                        G1 => H1,
                        C8 => A8,
                        G8 => H8,
                    };

                    int rookTo = moveTo switch
                    {
                        C1 => D1,
                        G1 => F1,
                        C8 => D8,
                        G8 => F8,
                    };

                    int ourRookOldIndex_US =    HalfKAIndex(us, rookFrom, FishPiece(Piece.Rook, us), ourKing);
                    int ourRookOldIndex_THEM =  HalfKAIndex(them, rookFrom, FishPiece(Piece.Rook, us), theirKing);

                    RemoveFeature(ourAccumulation, ourPsq, ourRookOldIndex_US);
                    RemoveFeature(theirAccumulation, theirPsq, ourRookOldIndex_THEM);

                    int ourRookNewIndex_US =    HalfKAIndex(us, rookTo, FishPiece(Piece.Rook, us), ourKing);
                    int ourRookNewIndex_THEM =  HalfKAIndex(them, rookTo, FishPiece(Piece.Rook, us), theirKing);

                    AddFeature(ourAccumulation, ourPsq, ourRookNewIndex_US);
                    AddFeature(theirAccumulation, theirPsq, ourRookNewIndex_THEM);
                }

            }

            return true;
        }




        /// <summary>
        /// Removes the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        [MethodImpl(Inline)]
        public static void RemoveFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)HalfDimensions / 16;
            const int RelativeTileHeight = TileHeight / 16;

            for (int j = 0; j < NumChunks; j++)
            {
                Vector256<short> column = FeatureTransformer.Weights[(RelativeDimensions * index) + j];

                accumulation[j] = Avx2.Subtract(accumulation[j], column);
            }



            for (int j = 0; j < PSQTBuckets / PsqtTileHeight; j++)
            {
                Vector256<int> column = FeatureTransformer.PSQTWeights[index + j * RelativeTileHeight];

                psqtAccumulation[j] = Sub256(psqtAccumulation[j], column);
            }
        }


        /// <summary>
        /// Adds the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        [MethodImpl(Inline)]
        public static void AddFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)HalfDimensions / 16;
            const int RelativeTileHeight = TileHeight / 16;

            for (int j = 0; j < NumChunks; j++)
            {
                Vector256<short> column = FeatureTransformer.Weights[(RelativeDimensions * index) + j];

                accumulation[j] = Avx2.Add(accumulation[j], column);
            }

            for (int j = 0; j < PSQTBuckets / PsqtTileHeight; j++)
            {
                Vector256<int> column = FeatureTransformer.PSQTWeights[index + j * RelativeTileHeight];

                psqtAccumulation[j] = Add256(psqtAccumulation[j], column);
            }

        }


        /// <summary>
        /// Returns a list of active indices for each side. 
        /// Every piece on the board has a unique index based on their king's square, their color, and the
        /// perspective of the player looking at them.
        /// </summary>
        [MethodImpl(Optimize)]
        public static void AppendActiveIndices(Position pos, Span<int> active)
        {
            int spanIndex = 0;

            Bitboard bb = pos.bb;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                ulong us = bb.Colors[perspective];
                ulong them = bb.Colors[Not(perspective)];
                int ourKing = bb.KingIndex(perspective);

                while (us != 0)
                {
                    int idx = lsb(us);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(pt, perspective);
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    active[spanIndex++] = kpIdx;

                    us = poplsb(us);
                }

                while (them != 0)
                {
                    int idx = lsb(them);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(pt, Not(perspective));
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    active[spanIndex++] = kpIdx;

                    them = poplsb(them);
                }

                spanIndex = MaxActiveDimensions;
            }

        }


        /// <summary>
        /// Returns the index of the square <paramref name="s"/>, rotated 180 degrees from white's perspective if <paramref name="perspective"/> is false.
        /// This is then mirrored if <paramref name="ksq"/> is on the A/B/C/D files, which would change the index of a piece on the E file to the D file
        /// and vice versa.
        /// </summary>
        [MethodImpl(Inline)]
        public static int Orient(int perspective, int s, int ksq = 0)
        {
            return (s ^ (perspective * Squares.A8) ^ ((GetIndexFile(ksq) < Files.E ? 1 : 0) * Squares.H1));
        }

        /// <summary>
        /// Returns the feature index for a piece of type <paramref name="fishPT"/> on the square <paramref name="s"/>,
        /// seen by the player with the color <paramref name="perspective"/> and whose king is on <paramref name="ksq"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static int HalfKAIndex(int perspective, int s, int fishPT, int ksq)
        {
            int o_ksq = Orient(perspective, ksq, ksq);

            int orientSq = Orient(perspective, s, ksq);
            uint psqI = PieceSquareIndex[perspective][fishPT];
            int fishIndex = (int)(orientSq + psqI + PS_NB * KingBuckets[o_ksq]);
            return fishIndex;
        }


        /// <summary>
        /// Returns an integer representing the piece of color <paramref name="pc"/> and type <paramref name="pt"/>
        /// in Stockfish 12's format, which has all of white's pieces before black's. It looks like:
        /// PS_NONE = 0, W_PAWN = 1, W_KNIGHT = 2, ... B_PAWN = 9, ... PIECE_NB = 16
        /// </summary>
        [MethodImpl(Inline)]
        public static int FishPiece(int pt, int pc)
        {
            return ((pt + 1) + (pc * 8));
        }




        /// <summary>
        /// Shows the feature indices for each piece on the board and for both perspectives.
        /// </summary>
        public static void Debug_ShowActiveIndices(Position pos)
        {
            Bitboard bb = pos.bb;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                Log(ColorToString(perspective) + ": ");
                ulong us = bb.Colors[perspective];
                ulong them = bb.Colors[Not(perspective)];
                int ourKing = bb.KingIndex(perspective);

                while (us != 0)
                {
                    int idx = lsb(us);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(pt, perspective);
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));

                    us = poplsb(us);
                }

                while (them != 0)
                {
                    int idx = lsb(them);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(pt, Not(perspective));
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));

                    them = poplsb(them);
                }

                Log("\n");
            }
        }


        public static int[] KingBuckets = {
            -1, -1, -1, -1, 31, 30, 29, 28,
            -1, -1, -1, -1, 27, 26, 25, 24,
            -1, -1, -1, -1, 23, 22, 21, 20,
            -1, -1, -1, -1, 19, 18, 17, 16,
            -1, -1, -1, -1, 15, 14, 13, 12,
            -1, -1, -1, -1, 11, 10,  9,  8,
            -1, -1, -1, -1,  7,  6,  5,  4,
            -1, -1, -1, -1,  3,  2,  1,  0
        };

        public static uint[][] PieceSquareIndex = {
            // convention: W - us, B - them
            // viewed from other side, W and B are reversed
            new uint[] { PS_NONE, PS_W_PAWN, PS_W_KNIGHT, PS_W_BISHOP, PS_W_ROOK, PS_W_QUEEN, PS_KING, PS_NONE,
                         PS_NONE, PS_B_PAWN, PS_B_KNIGHT, PS_B_BISHOP, PS_B_ROOK, PS_B_QUEEN, PS_KING, PS_NONE},
            new uint[] { PS_NONE, PS_B_PAWN, PS_B_KNIGHT, PS_B_BISHOP, PS_B_ROOK, PS_B_QUEEN, PS_KING, PS_NONE,
                         PS_NONE, PS_W_PAWN, PS_W_KNIGHT, PS_W_BISHOP, PS_W_ROOK, PS_W_QUEEN, PS_KING, PS_NONE}
        };



/// <summary>
/// A unique number for each piece type on any square.
/// <br></br>
/// For example, the ID's between 1 and 65 are reserved for white pawns on A1-H8,
/// and 513 through 577 are for white queens on A1-H8.
/// </summary>
public static class UniquePiece
        {
            public const uint PS_NONE       =  0;
            public const uint PS_W_PAWN     =  0;
            public const uint PS_B_PAWN     =  1 * SquareNB;
            public const uint PS_W_KNIGHT   =  2 * SquareNB;
            public const uint PS_B_KNIGHT   =  3 * SquareNB;
            public const uint PS_W_BISHOP   =  4 * SquareNB;
            public const uint PS_B_BISHOP   =  5 * SquareNB;
            public const uint PS_W_ROOK     =  6 * SquareNB;
            public const uint PS_B_ROOK     =  7 * SquareNB;
            public const uint PS_W_QUEEN    =  8 * SquareNB;
            public const uint PS_B_QUEEN    =  9 * SquareNB;
            public const uint PS_KING       = 10 * SquareNB;
            public const uint PS_NB         = 11 * SquareNB;

            public static readonly ExtPieceSquare[] kpp_board_index = {
                 // convention: W - us, B - them
                 // viewed from other side, W and B are reversed
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_W_PAWN,   PS_B_PAWN   ),
                new ExtPieceSquare( PS_W_KNIGHT, PS_B_KNIGHT ),
                new ExtPieceSquare( PS_W_BISHOP, PS_B_BISHOP ),
                new ExtPieceSquare( PS_W_ROOK,   PS_B_ROOK   ),
                new ExtPieceSquare( PS_W_QUEEN,  PS_B_QUEEN  ),
                new ExtPieceSquare( PS_KING,     PS_KING     ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_B_PAWN,   PS_W_PAWN   ),
                new ExtPieceSquare( PS_B_KNIGHT, PS_W_KNIGHT ),
                new ExtPieceSquare( PS_B_BISHOP, PS_W_BISHOP ),
                new ExtPieceSquare( PS_B_ROOK,   PS_W_ROOK   ),
                new ExtPieceSquare( PS_B_QUEEN,  PS_W_QUEEN  ),
                new ExtPieceSquare( PS_KING,     PS_KING     ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     )
            };
        }
    }
}
