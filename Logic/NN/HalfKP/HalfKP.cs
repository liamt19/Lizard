using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using LTChess.Properties;

using static LTChess.Logic.NN.HalfKP.HalfKP.UniquePiece;
using static LTChess.Logic.NN.HalfKP.NNCommon;

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
    /// <para></para>
    /// 
    /// Generally works pretty well, although there are sometimes issues with this architecture. For example:
    /// Stockfish 12's static NNUE evaluation for the position (2K5/p7/1b6/1P6/6p1/7k/5p2/8 b - - 1 70) is -16.12.
    /// If you promote to a queen, it is -13.90.
    /// If you promote to a knight, it is -15.19.
    /// Guess which one this engine chose :) https://lichess.org/GKOKPEcG/black#140
    /// 
    /// </summary>
    [SkipStaticConstructor]
    public static unsafe class HalfKP
    {
        public const string Stockfish12FinalNet = @"nn-62ef826d1a6d.nnue";

        public const string Name = "HalfKP(Friend)";
        public const int Dimensions = SquareNB * ((10 * SquareNB) + 1);

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

        public static FeatureTransformer Transformer;

        public static Network NetworkLayers;

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
            if (Initialized || !UseHalfKP)
            {
                return;
            }

            Initialized = true;

            //  Set up the network architecture layers
            NetworkLayers = new Network();


            //  Set up the feature transformer
            Transformer = new FeatureTransformer();


            //  Try loading 'nn.nnue' from the working directory, or just use the default network otherwise.
            Stream kpFile;

            string networkToLoad = @"nn.nnue";
            if (File.Exists(networkToLoad))
            {
                kpFile = File.OpenRead(networkToLoad);
            }
            else if (File.Exists(Stockfish12FinalNet))
            {
                kpFile = File.OpenRead(Stockfish12FinalNet);
            }
            else
            {
                //  Just load the default network
                networkToLoad = Stockfish12FinalNet;

                Log("Using embedded NNUE with HalfKP network " + networkToLoad);
                string resourceName = networkToLoad.Replace(".nnue", string.Empty);

                object? o = Resources.ResourceManager.GetObject(resourceName);
                if (o == null)
                {
                    Console.WriteLine("The UseHalfKP NNRunOption was set to true, but there isn't a valid HalfKP network to load!");
                    Console.WriteLine("This program looks for a HalfKP network named " + "'nn.nnue' or '" + Stockfish12FinalNet + "' within the current directory.");
                    Console.WriteLine("If neither can be found, then '" + Stockfish12FinalNet + "' needs to be a compiled as a resource as a fallback!");
                    Console.ReadLine();
                    Environment.Exit(-1);
                }

                byte[] data = (byte[])o;
                kpFile = new MemoryStream(data);
            }

            using BinaryReader br = new BinaryReader(kpFile);
            ReadHeader(br);
            ReadTransformParameters(br);
            ReadNetworkParameters(br);

            Log("Using NNUE with HalfKP network " + networkToLoad);

            kpFile.Dispose();
        }



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
            uint netHash = NetworkLayers.GetHashValue();
            uint finalHash = hashValue ^ netHash;

            if (finalHash != CorrectHashValue)
            {
                Debug.WriteLine("Expected header hash " + CorrectHashValue.ToString("X") + " but got " + finalHash.ToString("X"));
            }

            uint size = br.ReadUInt32();

            if (size > 1024)
            {
                Console.WriteLine("The network header is claiming that the architecture description string is " + size + " bytes long, which is probably wrong.");
                Console.WriteLine("For reference, the headers of NNUE-Pytorch networks are generally under 256 bytes.");
                Console.WriteLine("Press enter to continue loading...");
                Console.ReadLine();
            }

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
            uint header = br.ReadUInt32();
            Debug.WriteLine("FeatureTransformer header: " + header.ToString("X"));

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

            NetworkLayers.ReadParameters(br);
        }




        /// <summary>
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetEvaluation(Position pos)
        {
            Span<sbyte> transformed_features = stackalloc sbyte[(int)FeatureTransformer.BufferSize];

            ref AccumulatorPSQT accumulator = ref *pos.State->Accumulator;
            Transformer.TransformFeatures(pos, transformed_features, ref accumulator);

            var score = NetworkLayers.Propagate(transformed_features);
            return score;
        }

        /// <summary>
        /// Refreshes the current accumulator using the active features in the position <paramref name="pos"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static void RefreshNN(Position pos)
        {
            ref AccumulatorPSQT accumulator = ref *pos.State->Accumulator;
            Transformer.RefreshAccumulatorPerspective(pos, ref accumulator, Color.White);
        }


        /// <summary>
        /// Updates the features in the next accumulator by copying the current features and adding/removing
        /// the features that will be changing. 
        /// <br></br>
        /// If <paramref name="m"/> is a king move, the next accumulator
        /// will only be marked as needing a refresh.
        /// </summary>
        [MethodImpl(Inline)]
        public static void MakeMoveNN(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            AccumulatorPSQT* accumulator = pos.NextState->Accumulator;
            pos.State->Accumulator->CopyTo(accumulator);

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = bb.GetColorAtIndex(moveFrom);
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            int ourKing = Orient(us, pos.State->KingSquares[us]);
            int theirKing = Orient(them, pos.State->KingSquares[them]);

            var ourAccumulation = (*accumulator)[us];
            var theirAccumulation = (*accumulator)[them];

            if (ourPiece == Piece.King)
            {
                //  We will need a full refresh of our side's accumulator, but we can still update theirs.
                accumulator->RefreshPerspective[us] = true;

                //  Importantly, we do NOT change any features for our king.
                //  A king should never be an active feature in HalfKP, so unlike in HalfKA
                //  don't do RemoveFeature(...moveFrom, ourPiece...) + AddFeature(...moveTo, ourPiece...)

                if (m.Capture)
                {
                    RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, moveTo), FishPieceKP(them, theirPiece), them));
                }
                else if (m.Castle)
                {
                    //  The generated code freaks out about these switch statements not covering all options (although in practice they did),
                    //  so giving it these the default option of "G8" reduces the code size by about 5%.
                    int rookFrom = moveTo switch
                    {
                        C1 => A1,
                        G1 => H1,
                        C8 => A8,
                        _ => H8,    //  G8 => H8
                    };

                    int rookTo = moveTo switch
                    {
                        C1 => D1,
                        G1 => F1,
                        C8 => D8,
                        _ => F8,    //  G8 => F8
                    };

                    RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, rookFrom), FishPieceKP(us, Piece.Rook), them));
                    AddFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, rookTo), FishPieceKP(us, Piece.Rook), them));
                }

                return;
            }
            else
            {
                //  Otherwise, we only need to remove the features that are no longer there (move.From) and the piece that was on
                //  move.To before it was captured, and add the new features (move.To).
                RemoveFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, moveFrom), FishPieceKP(us, ourPiece), us));
                RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, moveFrom), FishPieceKP(us, ourPiece), them));

                if (!m.Promotion)
                {
                    AddFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, moveTo), FishPieceKP(us, ourPiece), us));
                    AddFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, moveTo), FishPieceKP(us, ourPiece), them));
                }
                else
                {
                    //  Add the promotion piece instead.
                    AddFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, moveTo), FishPieceKP(us, m.PromotionTo), us));
                    AddFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, moveTo), FishPieceKP(us, m.PromotionTo), them));
                }

                if (m.Capture)
                {
                    //  A captured piece needs to be removed from both perspectives as well.
                    RemoveFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, moveTo), FishPieceKP(Not(us), theirPiece), us));
                    RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, moveTo), FishPieceKP(them, theirPiece), them));

                }

                if (m.EnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    RemoveFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, idxPawn), FishPieceKP(Not(us), Piece.Pawn), us));
                    RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, idxPawn), FishPieceKP(Not(us), Piece.Pawn), them));
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

                    RemoveFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, rookFrom), FishPieceKP(us, Piece.Rook), us));
                    RemoveFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, rookFrom), FishPieceKP(us, Piece.Rook), them));

                    AddFeature(ourAccumulation, HalfKPIndex(ourKing, Orient(us, rookTo), FishPieceKP(us, Piece.Rook), us));
                    AddFeature(theirAccumulation, HalfKPIndex(theirKing, Orient(them, rookTo), FishPieceKP(us, Piece.Rook), them));
                }
            }

            return;
        }



        /// <summary>
        /// Removes the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void RemoveFeature(in Vector256<short>* accumulation, int index)
        {
            const uint NumChunks = TransformedFeatureDimensions / (SimdWidth / 2);
            const int RelativeDimensions = (int)TransformedFeatureDimensions / 16;

            int ci = RelativeDimensions * index;
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Subtract(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }
        }


        /// <summary>
        /// Adds the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void AddFeature(in Vector256<short>* accumulation, int index)
        {
            const uint NumChunks = TransformedFeatureDimensions / (SimdWidth / 2);
            const int RelativeDimensions = (int)TransformedFeatureDimensions / 16;

            int ci = RelativeDimensions * index;
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Add(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }
        }



        /// <summary>
        /// Returns a list of active indices for each side. 
        /// Every piece on the board (besides kings) have a unique index based on their king's square, their color, and the
        /// perspective of the player looking at them.
        /// </summary>
        public static int AppendActiveIndices(Position pos, Span<int> active, int perspective)
        {
            int spanIndex = 0;

            ref Bitboard bb = ref pos.bb;

            ulong us = bb.Colors[perspective];
            ulong them = bb.Colors[Not(perspective)];

            int ourKing = Orient(perspective, pos.State->KingSquares[perspective]);

            while (us != 0)
            {
                int idx = poplsb(&us);

                int pt = bb.GetPieceAtIndex(idx);
                if (pt != Piece.King)
                {
                    int fishPT = FishPieceKP(perspective, pt);
                    int kpIdx = HalfKPIndex(ourKing, Orient(perspective, idx), fishPT, perspective);
                    active[spanIndex++] = kpIdx;
                }
            }

            while (them != 0)
            {
                int idx = poplsb(&them);

                int pt = bb.GetPieceAtIndex(idx);
                if (pt != Piece.King)
                {
                    int fishPT = FishPieceKP(Not(perspective), pt);
                    int kpIdx = HalfKPIndex(ourKing, Orient(perspective, idx), fishPT, perspective);
                    active[spanIndex++] = kpIdx;
                }
            }

            return spanIndex;
        }


        /// <summary>
        /// Returns the index of the square <paramref name="sq"/>, rotated 180 degrees from white's perspective if <paramref name="IsWhitePOV"/> is false.
        /// <br></br>
        /// This flips the square's file from A to H, B to G ... and flips the rank from 1 to 8, 2 to 7 ... 
        /// <para></para>
        /// For example, the square B3 from white's POV is B3 == 17, and from black's POV it is G6 == 46
        /// </summary>
        [MethodImpl(Inline)]
        public static int Orient(int perspective, int sq)
        {
            return (63 * perspective) ^ sq;
        }

        /// <summary>
        /// Returns a unique index for a piece of type <paramref name="fishPT"/> on the oriented square <paramref name="pieceSq"/> from 
        /// the perspective of the player with the color <paramref name="perspective"/>, whose king is on the oriented square <paramref name="kingSq"/>.
        /// <para></para>
        /// This should be called twice for each piece on the board, once for white and once for black.
        /// 
        /// </summary>
        /// <param name="kingSq">This should be equal to the oriented bb.KingIndex(pc)</param>
        /// <param name="pieceSq">This is the oriented square that the piece is on, from <paramref name="perspective"/>'s perspective </param>
        /// <param name="fishPT">The type of piece, which should be FishPiece(pt, perspective)</param>
        /// <param name="perspective">The perspective of the player</param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public static int HalfKPIndex(int kingSq, int pieceSq, int fishPT, int perspective)
        {
            int fishPieceSQ = (int)(PieceSquareIndex[perspective][fishPT] + pieceSq);
            int fishIndex = (641 * kingSq) + fishPieceSQ;
            return fishIndex;
        }


        /// <summary>
        /// Returns an integer representing the piece of color <paramref name="pc"/> and type <paramref name="pt"/>
        /// in Stockfish 12's format, which has all of white's pieces before black's. It looks like:
        /// PS_NONE = 0, W_PAWN = 1, W_KNIGHT = 2, ... B_PAWN = 9, ... PIECE_NB = 16
        /// </summary>
        [MethodImpl(Inline)]
        public static int FishPieceKP(int pc, int pt)
        {
            return pt + 1 + (pc * 8);
        }


        public static uint HashValue(int pc)
        {
            return (uint)(0x5D69D5B9u ^ (pc == Color.White ? 1 : 0));
        }








        public static void Debug_ShowActiveIndices(Position pos)
        {
            ref Bitboard bb = ref pos.bb;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                Log(ColorToString(perspective) + ": ");
                ulong us = bb.Colors[perspective];
                ulong them = bb.Colors[Not(perspective)];
                int ourKing = Orient(perspective, pos.State->KingSquares[perspective]);

                while (us != 0)
                {
                    int idx = lsb(us);

                    int pt = bb.GetPieceAtIndex(idx);
                    if (pt != Piece.King)
                    {
                        int fishPT = FishPieceKP(perspective, pt);
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective, idx), fishPT, perspective);
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
                        int fishPT = FishPieceKP(Not(perspective), pt);
                        int kpIdx = HalfKPIndex(ourKing, Orient(perspective, idx), fishPT, perspective);
                        Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));
                    }

                    them = poplsb(them);
                }

                Log("\n");
            }
        }



        /// <summary>
        /// A unique number for each piece type on any square.
        /// <br></br>
        /// For example, the ID's between 1 and 65 are reserved for white pawns on A1-H8,
        /// and 513 through 577 are for white queens on A1-H8.
        /// </summary>
        public static class UniquePiece
        {
            public const uint PS_NONE = 0;
            public const uint PS_W_PAWN = 1;
            public const uint PS_B_PAWN = (1 * SquareNB) + 1;
            public const uint PS_W_KNIGHT = (2 * SquareNB) + 1;
            public const uint PS_B_KNIGHT = (3 * SquareNB) + 1;
            public const uint PS_W_BISHOP = (4 * SquareNB) + 1;
            public const uint PS_B_BISHOP = (5 * SquareNB) + 1;
            public const uint PS_W_ROOK = (6 * SquareNB) + 1;
            public const uint PS_B_ROOK = (7 * SquareNB) + 1;
            public const uint PS_W_QUEEN = (8 * SquareNB) + 1;
            public const uint PS_B_QUEEN = (9 * SquareNB) + 1;
            public const uint PS_W_KING = (10 * SquareNB) + 1;
            public const uint PS_END = PS_W_KING;
            public const uint PS_B_KING = (11 * SquareNB) + 1;
            public const uint PS_END2 = (12 * SquareNB) + 1;

            public static readonly uint[][] PieceSquareIndex = {
                // convention: W - us, B - them
                // viewed from other side, W and B are reversed
                new uint[] { PS_NONE, PS_W_PAWN, PS_W_KNIGHT, PS_W_BISHOP, PS_W_ROOK, PS_W_QUEEN, PS_W_KING, PS_NONE,
                             PS_NONE, PS_B_PAWN, PS_B_KNIGHT, PS_B_BISHOP, PS_B_ROOK, PS_B_QUEEN, PS_B_KING, PS_NONE},
                new uint[] { PS_NONE, PS_B_PAWN, PS_B_KNIGHT, PS_B_BISHOP, PS_B_ROOK, PS_B_QUEEN, PS_B_KING, PS_NONE,
                             PS_NONE, PS_W_PAWN, PS_W_KNIGHT, PS_W_BISHOP, PS_W_ROOK, PS_W_QUEEN, PS_W_KING, PS_NONE}
            };
        }
    }
}
