

using static LTChess.Logic.NN.HalfKP.FeatureTransformer;
using static LTChess.Logic.NN.HalfKP.NNCommon;
using static LTChess.Logic.NN.HalfKP.HalfKP;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System;

using LTChess.Logic.Data;
using LTChess.Logic.NN.HalfKP.Layers;
using LTChess.Properties;

using static LTChess.Logic.NN.HalfKP.HalfKP.UniquePiece;
using System.Resources;

namespace LTChess.Logic.NN.HalfKP
{
    /// <summary>
    /// HalfKP is the first network architecture that Stockfish 12 used, and is 40960-256x2-32-32-1 .
    /// <para></para>
    /// 
    /// The following SVG illustrates this: https://raw.githubusercontent.com/glinscott/nnue-pytorch/master/docs/img/HalfKP-40960-256x2-32-32-1.svg
    /// <para></para>
    /// 
    /// This is adapted from the initial release of Stockfish 12, but also combines the use of an "Accumulator stack"
    /// from https://github.com/TheBlackPlague/StockNemo/tree/master/Backend/Engine/NNUE.
    /// Stockfish 12 used a "StateInfo" struct which contained an Accumulator for each position,
    /// but to keep things simpler and use less pointers, this stack concept works just as well.
    /// 
    /// </summary>
    public static class HalfKP
    {
        /// <summary>
        /// NNUE-PyTorch (https://github.com/glinscott/nnue-pytorch/tree/master) compresses networks using LEB128,
        /// but older Stockfish networks didn't do this.
        /// </summary>
        public static bool IsLEB128 = false;

        public const string Stockfish12DefaultNet = @"nn-97f742aaefcd.nnue";
        public const string Stockfish12FinalNet =   @"nn-62ef826d1a6d.nnue";

        public const string Name = "HalfKP(Friend)";
        public const int Dimensions = SquareNB * (10 * SquareNB + 1);

        /// <summary>
        /// There are a max of 32 pieces on the board, and kings are excluded here
        /// </summary>
        public const int MaxActiveDimensions = 30;

        /// <summary>
        /// King squares * Piece squares * Piece types * Colors == 40960
        /// </summary>
        public const int FeatureCount = SquareNB * SquareNB * PieceNB * ColorNB;

        public const int OutputDimensions = 512 * 2;
        public const int TransformedFeatureDimensions = 256;

        public static Accumulator[] AccumulatorStack;
        public static int CurrentAccumulator;

        public static FeatureTransformer Transformer;

        public static InputSlice InputLayer;

        public static AffineTransform TransformLayer1;
        public static ClippedReLU HiddenLayer1;

        public static AffineTransform TransformLayer2;
        public static ClippedReLU HiddenLayer2;

        public static AffineTransform OutputLayer;

        private static bool Initialized = false;

        static HalfKP()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            if (Initialized || !Position.UseHalfKP)
            {
                return;
            }

            Initialized = true;

            AccumulatorStack = new Accumulator[MaxListCapacity];
            for (int i = 0; i < MaxListCapacity; i++)
            {
                AccumulatorStack[i] = new Accumulator(TransformedFeatureDimensions);
            }
            CurrentAccumulator = 0;


            //  Set up the network architecture layers
            InputLayer = new InputSlice(0);

            TransformLayer1 = new AffineTransform(InputLayer, 32);
            HiddenLayer1 = new ClippedReLU(TransformLayer1);

            TransformLayer2 = new AffineTransform(HiddenLayer1, 32);
            HiddenLayer2 = new ClippedReLU(TransformLayer2);

            OutputLayer = new AffineTransform(HiddenLayer2, 1);


            //  Set up the feature transformer
            Transformer = new FeatureTransformer();


            //  Try loading 'nn.nnue' from the working directory, or just use the default network otherwise.
            Stream kpFile;

            string networkToLoad = @"nn.nnue";
            if (File.Exists(networkToLoad))
            {
                kpFile = File.OpenRead(networkToLoad);
            }
            else
            {
                //  Just load the default network

                networkToLoad = Stockfish12FinalNet;

                string resourceName = (Stockfish12FinalNet.Replace(".nnue", string.Empty));
                byte[] data = (byte[])Resources.ResourceManager.GetObject(resourceName);
                kpFile = new MemoryStream(data);
            }

            using BinaryReader br = new BinaryReader(kpFile);
            ReadHeader(br);
            ReadTransformParameters(br);
            ReadNetworkParameters(br);

            Log("Using NNUE with HalfKP network " + networkToLoad);

            kpFile.Dispose();
        }

        [MethodImpl(Inline)]
        private static void ResetAccumulator() => CurrentAccumulator = 0;

        [MethodImpl(Inline)]
        private static void PushAccumulator() => AccumulatorStack[CurrentAccumulator].CopyTo(AccumulatorStack[++CurrentAccumulator]);

        [MethodImpl(Inline)]
        private static void PullAccumulator() => CurrentAccumulator--;



        /// <summary>
        /// HalfKP networks have a header containing a version, hash value, and architecture descriptor.
        /// </summary>
        public static void ReadHeader(BinaryReader br)
        {
            const uint CorrectVersion = 0x7AF32F16u;
            const uint CorrectHashValue = 0x3E5AA6EE;

            uint version = br.ReadUInt32();
            if (version != CorrectVersion)
            {
                Debug.WriteLine("Expected header version " + CorrectVersion.ToString("X") + " but got " + version.ToString("X"));
            }

            uint hashValue = br.ReadUInt32();
            uint netHash = OutputLayer.GetHashValue();
            uint finalHash = (hashValue ^ netHash);

            if (finalHash != CorrectHashValue)
            {
                Debug.WriteLine("Expected header hash " + CorrectHashValue.ToString("X") + " but got " + finalHash.ToString("X"));
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
            uint header = br.ReadUInt32();
            Debug.WriteLine("NetworkParameter header: " + header.ToString("X"));

            OutputLayer.ReadParameters(br);
        }




        /// <summary>
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetEvaluation(Position pos)
        {
            sbyte[] transformed_features = new sbyte[FeatureTransformer.BufferSize];

            Accumulator acc = AccumulatorStack[CurrentAccumulator];

            Transformer.TransformFeatures(pos, transformed_features, ref acc);
            var output = OutputLayer.Propagate(transformed_features);

            int score = (int)((double)output[0] / NNCommon.OutputScale);

            return score;
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
            Accumulator Accumulator = AccumulatorStack[CurrentAccumulator];
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

            PushAccumulator();
            Accumulator Accumulator = AccumulatorStack[CurrentAccumulator];

            int ourPiece = bb.GetPieceAtIndex(m.From);

            if (ourPiece == Piece.King)
            {
                //  We will need to completely refresh this side's accumulator since every feature in HalfKP
                //  is dependent upon where their king is/was.

                Accumulator.NeedsRefresh = true;
                return false;
            }
            else
            {
                //  Otherwise, we only need to remove the features that are no longer there (move.From) and the piece that was on
                //  move.To before it was captured, and add the new features (move.To).

                int us = bb.GetColorAtIndex(m.From);
                int them = Not(us);
                int theirPiece = bb.GetPieceAtIndex(m.To);

                int ourKing = Orient(us == Color.White, bb.KingIndex(us));
                int theirKing = Orient(them == Color.White, bb.KingIndex(them));

                short[] ourAccumulation = ((us == Color.White) ? Accumulator.White : Accumulator.Black);
                short[] theirAccumulation = ((us == Color.Black) ? Accumulator.White : Accumulator.Black);

                int ourPieceOldIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, m.From), FishPiece(ourPiece, us), us);
                int ourPieceOldIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, m.From), FishPiece(ourPiece, us), them);

                RemoveFeature(ref ourAccumulation, ourPieceOldIndex_US);
                RemoveFeature(ref theirAccumulation, ourPieceOldIndex_THEM);

                if (!m.Promotion)
                {
                    int ourPieceNewIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, m.To), FishPiece(ourPiece, us), us);
                    int ourPieceNewIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, m.To), FishPiece(ourPiece, us), them);

                    AddFeature(ref ourAccumulation, ourPieceNewIndex_US);
                    AddFeature(ref theirAccumulation, ourPieceNewIndex_THEM);
                }
                else
                {
                    //  Add the promotion piece instead.
                    int ourPieceNewIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, m.To), FishPiece(m.PromotionTo, us), us);
                    int ourPieceNewIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, m.To), FishPiece(m.PromotionTo, us), them);

                    AddFeature(ref ourAccumulation, ourPieceNewIndex_US);
                    AddFeature(ref theirAccumulation, ourPieceNewIndex_THEM);
                }

                if (m.Capture)
                {
                    //  A captured piece needs to be removed from both perspectives as well.
                    int theirCapturedPieceIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, m.To), FishPiece(theirPiece, Not(us)), us);
                    int theirCapturedPieceIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, m.To), FishPiece(theirPiece, Not(us)), them);

                    RemoveFeature(ref ourAccumulation, theirCapturedPieceIndex_US);
                    RemoveFeature(ref theirAccumulation, theirCapturedPieceIndex_THEM);

                }

                if (m.EnPassant)
                {
                    int idxPawn = (bb.Pieces[Piece.Pawn] & SquareBB[pos.EnPassantTarget - 8]) != 0 ? pos.EnPassantTarget - 8 : pos.EnPassantTarget + 8;

                    int theirCapturedPieceIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, idxPawn), FishPiece(Piece.Pawn, Not(us)), us);
                    int theirCapturedPieceIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, idxPawn), FishPiece(Piece.Pawn, Not(us)), them);

                    RemoveFeature(ref ourAccumulation, theirCapturedPieceIndex_US);
                    RemoveFeature(ref theirAccumulation, theirCapturedPieceIndex_THEM);
                }

                if (m.Castle)
                {

                    int rookFrom = m.To switch
                    {
                        C1 => A1,
                        G1 => H1,
                        C8 => A8,
                        G8 => H8,
                    };

                    int rookTo = m.To switch
                    {
                        C1 => D1,
                        G1 => F1,
                        C8 => D8,
                        G8 => F8,
                    };

                    int ourRookOldIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, rookFrom), FishPiece(Piece.Rook, us), us);
                    int ourRookOldIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, rookFrom), FishPiece(Piece.Rook, us), them);

                    RemoveFeature(ref ourAccumulation, ourRookOldIndex_US);
                    RemoveFeature(ref theirAccumulation, ourRookOldIndex_THEM);

                    int ourRookNewIndex_US = HalfKPIndex(ourKing, Orient(us == Color.White, rookTo), FishPiece(Piece.Rook, us), us);
                    int ourRookNewIndex_THEM = HalfKPIndex(theirKing, Orient(them == Color.White, rookTo), FishPiece(Piece.Rook, us), them);

                    AddFeature(ref ourAccumulation, ourRookNewIndex_US);
                    AddFeature(ref theirAccumulation, ourRookNewIndex_THEM);
                }
            }

            return true;
        }




        /// <summary>
        /// Removed the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="Accumulator.White"/> or <see cref="Accumulator.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKPIndex"/></param>
        [MethodImpl(Inline)]
        public static void RemoveFeature(ref short[] accumulation, int index)
        {
            const uint NumChunks = HalfDimensions / (SimdWidth / 2);
            uint offset = (uint)(HalfDimensions * index);

            for (int j = 0; j < NumChunks; j++)
            {
                int vectIndex = j * FeatureTransformer.VectorSize;
                Vector256<short> inV = Load256(accumulation, vectIndex);

                int columnIndex = (int)(offset + (j * VectorSize));
                Vector256<short> column = Load256(Transformer.Weights, columnIndex);

                inV = Avx2.Subtract(inV, column);

                Store256(ref inV, accumulation, vectIndex);
            }
        }


        /// <summary>
        /// Adds the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="Accumulator.White"/> or <see cref="Accumulator.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKPIndex"/></param>
        [MethodImpl(Inline)]
        public static void AddFeature(ref short[] accumulation, int index)
        {
            const uint NumChunks = HalfDimensions / (SimdWidth / 2);
            uint offset = (uint)(HalfDimensions * index);

            for (int j = 0; j < NumChunks; j++)
            {
                int vectIndex = j * FeatureTransformer.VectorSize;
                Vector256<short> inV = Load256(accumulation, vectIndex);

                int columnIndex = (int)(offset + (j * VectorSize));
                Vector256<short> column = Load256(Transformer.Weights, columnIndex);

                inV = Avx2.Add(inV, column);

                Store256(ref inV, accumulation, vectIndex);
            }
        }





        /// <summary>
        /// Returns a list of active indices for each side. 
        /// Every piece on the board (besides kings) have a unique index based on their king's square, their color, and the
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
                int ourKing = Orient(perspective == Color.White, bb.KingIndex(perspective));

                while (us != 0)
                {
                    int idx = lsb(us);

                    int pt = bb.GetPieceAtIndex(idx);
                    if (pt != Piece.King)
                    {
                        int fishPT = FishPiece(pt, perspective);
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective == Color.White, idx), fishPT, perspective);
                        active[spanIndex++] = kpIdx;
                    }

                    us = poplsb(us);
                }

                while (them != 0)
                {
                    int idx = lsb(them);

                    int pt = bb.GetPieceAtIndex(idx);
                    if (pt != Piece.King)
                    {
                        int fishPT = FishPiece(pt, Not(perspective));
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective == Color.White, idx), fishPT, perspective);
                        active[spanIndex++] = kpIdx;
                    }

                    them = poplsb(them);
                }


                spanIndex = MaxActiveDimensions;
            }
        }


        /// <summary>
        /// Returns the index of the square <paramref name="sq"/>, rotated 180 degrees from white's perspective if <paramref name="IsWhitePOV"/> is false.
        /// <br></br>
        /// This flips the square's file from A to H, B to G ... and flips the rank from 1 to 8, 2 to 7 ... 
        /// <para></para>
        /// For example, the square B3 from white's POV is B3 == 17, and from black's POV it is G6 == 46
        /// </summary>
        [MethodImpl(Inline)]
        public static int Orient(bool IsWhitePOV, int sq)
        {
            return (63 * (IsWhitePOV ? 0 : 1)) ^ sq;
        }

        /// <summary>
        /// Returns a unique index for a piece of type <paramref name="fishPT"/> on the oriented square <paramref name="piece_sq"/> from 
        /// the perspective of the player with the color <paramref name="pc"/>, whose king is on the oriented square <paramref name="king_sq"/>.
        /// <para></para>
        /// This should be called twice for each piece on the board, once for white and once for black.
        /// 
        /// </summary>
        /// <param name="king_sq">This should be equal to the oriented bb.KingIndex(pc)</param>
        /// <param name="piece_sq">This is the oriented square that the piece is on, from <paramref name="pc"/>'s perspective </param>
        /// <param name="fishPT">The type of piece, which should be FishPiece(pt, perspective)</param>
        /// <param name="pc">The perspective of the player</param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public static int HalfKPIndex(int king_sq, int piece_sq, int fishPT, int pc)
        {
            int fishPieceSQ = (int)((kpp_board_index[fishPT].from[pc]) + piece_sq);
            int fishIndex = (((641 * king_sq) + fishPieceSQ));
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


        public static uint HashValue(int pc)
        {
            return (uint)(0x5D69D5B9u ^ (pc == Color.White ? 1 : 0));
        }








        public static void Debug_ShowActiveIndices(Position pos)
        {
            Bitboard bb = pos.bb;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                Log(ColorToString(perspective) + ": ");
                ulong us = bb.Colors[perspective];
                ulong them = bb.Colors[Not(perspective)];
                int ourKing = Orient(perspective == Color.White, bb.KingIndex(perspective));

                while (us != 0)
                {
                    int idx = lsb(us);

                    int pt = bb.GetPieceAtIndex(idx);
                    if (pt != Piece.King)
                    {
                        int fishPT = FishPiece(pt, perspective);
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective == Color.White, idx), fishPT, perspective);
                        Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));
                    }

                    us = poplsb(us);
                }

                while (them != 0)
                {
                    int idx = lsb(them);

                    int pt = bb.GetPieceAtIndex(idx);
                    if (pt != Piece.King)
                    {
                        int fishPT = FishPiece(pt, Not(perspective));
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective == Color.White, idx), fishPT, perspective);
                        Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));
                    }

                    them = poplsb(them);
                }

                Log("\n");
            }
        }

        public static void Debug_ShowAccumulatorStates()
        {
            for (int i = 0; i < AccumulatorStack.Length; i++)
            {
                Accumulator accumulator = AccumulatorStack[i];
                if (accumulator.White.Where(x => x != 0).Any())
                {
                    Log("Accumulators[" + i + "]:\t" + "NeedsRefresh: " + accumulator.NeedsRefresh + "\t" + (CurrentAccumulator == i ? "CURR" : string.Empty));
                    Log("\tWhite: " + accumulator.White[0] + "\t" + accumulator.White[1] + "\t" + accumulator.White[2]);
                    Log("\tBlack: " + accumulator.Black[0] + "\t" + accumulator.Black[1] + "\t" + accumulator.Black[2]);
                    Log("\n");
                }
                else
                {
                    break;
                }
            }

            Log("***************************************************************\n\n");
        }


        /// <summary>
        /// A unique number for each piece type on any square.
        /// <br></br>
        /// For example, the ID's between 1 and 65 are reserved for white pawns on A1-H8,
        /// and 513 through 577 are for white queens on A1-H8.
        /// </summary>
        public static class UniquePiece
        {
            public const uint PS_NONE       =  0;
            public const uint PS_W_PAWN     =  1;
            public const uint PS_B_PAWN     =  1 * SquareNB + 1;
            public const uint PS_W_KNIGHT   =  2 * SquareNB + 1;
            public const uint PS_B_KNIGHT   =  3 * SquareNB + 1;
            public const uint PS_W_BISHOP   =  4 * SquareNB + 1;
            public const uint PS_B_BISHOP   =  5 * SquareNB + 1;
            public const uint PS_W_ROOK     =  6 * SquareNB + 1;
            public const uint PS_B_ROOK     =  7 * SquareNB + 1;
            public const uint PS_W_QUEEN    =  8 * SquareNB + 1;
            public const uint PS_B_QUEEN    =  9 * SquareNB + 1;
            public const uint PS_W_KING     = 10 * SquareNB + 1;
            public const uint PS_END        = PS_W_KING;
            public const uint PS_B_KING     = 11 * SquareNB + 1;
            public const uint PS_END2       = 12 * SquareNB + 1;

            public static readonly ExtPieceSquare[] kpp_board_index = {
                 // convention: W - us, B - them
                 // viewed from other side, W and B are reversed
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_W_PAWN,   PS_B_PAWN   ),
                new ExtPieceSquare( PS_W_KNIGHT, PS_B_KNIGHT ),
                new ExtPieceSquare( PS_W_BISHOP, PS_B_BISHOP ),
                new ExtPieceSquare( PS_W_ROOK,   PS_B_ROOK   ),
                new ExtPieceSquare( PS_W_QUEEN,  PS_B_QUEEN  ),
                new ExtPieceSquare( PS_W_KING,   PS_B_KING   ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     ),
                new ExtPieceSquare( PS_B_PAWN,   PS_W_PAWN   ),
                new ExtPieceSquare( PS_B_KNIGHT, PS_W_KNIGHT ),
                new ExtPieceSquare( PS_B_BISHOP, PS_W_BISHOP ),
                new ExtPieceSquare( PS_B_ROOK,   PS_W_ROOK   ),
                new ExtPieceSquare( PS_B_QUEEN,  PS_W_QUEEN  ),
                new ExtPieceSquare( PS_B_KING,   PS_W_KING   ),
                new ExtPieceSquare( PS_NONE,     PS_NONE     )
            };
        }
    }
}
