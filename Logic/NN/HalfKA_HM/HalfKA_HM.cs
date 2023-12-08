
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
    /// Uses SFNNv6/7/8 architectures.
    /// https://raw.githubusercontent.com/official-stockfish/nnue-pytorch/c15e33ccbe0bfe63d0aa4dbe69307afc7214b1d0/docs/img/SFNNv6_architecture_detailed_v2.svg
    /// 
    /// To use a net from a different architecture, change <see cref="TransformedFeatureDimensions"/> to 1536/2048/2560 for 6/7/8.
    /// </summary>
    public static unsafe class HalfKA_HM
    {
        public const string NNV6_Sparse = @"nn-cd2ff4716c34.nnue";
        public const string NNV6_Sparse_LEB = @"nn-5af11540bbfe.nnue";

        public const string Name = "HalfKAv2_hm(Friend)";

        public const uint VersionValue = 0x7AF32F20u;
        public const uint HashValue = 0x7F234CB8u;
        public const uint Dimensions = SquareNB * PS_NB / 2;

        public const int TransformedFeatureDimensions = 1536;

        public const int MaxActiveDimensions = 32;

        private static Network[] LayerStack;

        //private static FeatureTransformer Transformer;

        private const int _TransformedFeaturesBufferLength = FeatureTransformer.BufferSize;

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

            //  Set up the network architecture layers
            LayerStack = new Network[LayerStacks];
            for (int i = 0; i < LayerStacks; i++)
            {
                LayerStack[i] = new Network();
            }


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
                networkToLoad = NNV6_Sparse_LEB;
                Log("Using embedded NNUE with HalfKA_v2_hm network " + networkToLoad);

                string resourceName = (networkToLoad.Replace(".nnue", string.Empty));
                try
                {
                    buff = (byte[])Resources.ResourceManager.GetObject(resourceName);
                }
                catch (Exception e)
                {
                    Log("Attempt to load '" + resourceName + "' from embedded resources failed!");
                    throw;
                }
                
            }

            kpFile = new MemoryStream(buff);

            using BinaryReader br = new BinaryReader(kpFile);
            ReadHeader(br);
            FeatureTransformer.ReadParameters(br);
            for (int i = 0; i < LayerStacks; i++)
            {
                uint header = br.ReadUInt32();
                if (!LayerStack[i].ReadParameters(br))
                {
                    throw new Exception("Failed reading network parameters for LayerStack[" + i + " / " + LayerStacks + "]!");
                }
            }

            kpFile.Dispose();
        }


        public static void ReadHeader(BinaryReader br)
        {
            

            uint version = br.ReadUInt32();
            if (version != VersionValue)
            {
                Debug.WriteLine("Expected header version " + VersionValue.ToString("X") + " but got " + version.ToString("X"));
            }

            uint hashValue = br.ReadUInt32();
            uint netHash = LayerStack[0].GetHashValue();
            uint ftHash = FeatureTransformer.HashValue;
            uint finalHash = (netHash ^ ftHash);

            if (hashValue != finalHash)
            {
                Debug.WriteLine("Expected header hash " + hashValue.ToString("X") + " but got " + finalHash.ToString("X"));
            }

            uint size = br.ReadUInt32();

            byte[] archBuffer = new byte[size];
            br.Read(archBuffer);

            string arch = System.Text.Encoding.UTF8.GetString(archBuffer);
            Debug.WriteLine("Network architecture: '" + arch + "'");
        }

        /// <summary>
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        public static int GetEvaluation(Position pos, bool adjusted = false)
        {
            ref AccumulatorPSQT Accumulator = ref *(pos.State->Accumulator);
            return GetEvaluation(pos, ref Accumulator, adjusted);
        }

        public static int GetEvaluation(Position pos, ref AccumulatorPSQT accumulator, bool adjusted = false)
        {
            const int delta = 24;

            int bucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;

            Span<sbyte> features = stackalloc sbyte[_TransformedFeaturesBufferLength];
            int psqt = FeatureTransformer.TransformFeatures(pos, features, ref accumulator, bucket);

            var positional = LayerStack[bucket].Propagate(features);

            int v;

            if (adjusted)
            {
                v = (((1024 - delta) * psqt + (1024 + delta) * positional) / (1024 * OutputScale));
            }
            else
            {
                v = (psqt + positional) / OutputScale;
            }

            return v;
        }


        /// <summary>
        /// Updates the features in the next accumulator by copying the current features and adding/removing
        /// the features that will be changing. 
        /// <br></br>
        /// If <paramref name="m"/> is a king move, the next accumulator will be marked as needing a refresh.
        /// </summary>
        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            int moveFrom = m.From;
            int moveTo = m.To;

            AccumulatorPSQT* accumulator = pos.NextState->Accumulator;
            pos.State->Accumulator->CopyTo(accumulator);

            int us = bb.GetColorAtIndex(moveFrom);
            int them = Not(us);

            int ourPiece = bb.GetPieceAtIndex(moveFrom);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            int ourKing = pos.State->KingSquares[us];
            int theirKing = pos.State->KingSquares[them];

            Vector256<short>* ourAccumulation = (*accumulator)[us];
            Vector256<short>* theirAccumulation = (*accumulator)[them];

            Vector256<int>* ourPsq = accumulator->PSQ(us);
            Vector256<int>* theirPsq = accumulator->PSQ(them);


            if (ourPiece == Piece.King)
            {
                //  When we make a king move, we will need to do a full recalculation of our features.
                //  We can still update the opponent's side however, since their features are dependent on where THEIR king is, not ours.
                //  This saves us a bit of time later since we won't need to refresh both sides for every king move.
                accumulator->RefreshPerspective[us] = true;

                RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveFrom, FishPiece(us, ourPiece), theirKing));
                AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(us, ourPiece), theirKing));

                if (m.Capture)
                {
                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(Not(us), theirPiece), theirKing));
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

                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookFrom, FishPiece(us, Piece.Rook), theirKing));
                    AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookTo, FishPiece(us, Piece.Rook), theirKing));
                }

                return;
            }

            //  Otherwise, we only need to remove the features that are no longer there (move.From) and the piece that was on
            //  move.To before it was captured, and add the new features (move.To).

            RemoveFeature(ourAccumulation, ourPsq, HalfKAIndex(us, moveFrom, FishPiece(us, ourPiece), ourKing));
            RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveFrom, FishPiece(us, ourPiece), theirKing));

            if (m.Promotion)
            {
                //  Add the promotion piece instead.

                AddFeature(ourAccumulation, ourPsq, HalfKAIndex(us, moveTo, FishPiece(us, m.PromotionTo), ourKing));
                AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(us, m.PromotionTo), theirKing));
            }
            else
            {
                AddFeature(ourAccumulation, ourPsq, HalfKAIndex(us, moveTo, FishPiece(us, ourPiece), ourKing));
                AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(us, ourPiece), theirKing));
            }

            if (m.Capture)
            {
                //  A captured piece needs to be removed from both perspectives as well.

                RemoveFeature(ourAccumulation, ourPsq, HalfKAIndex(us, moveTo, FishPiece(Not(us), theirPiece), ourKing));
                RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(Not(us), theirPiece), theirKing));
            }

            if (m.EnPassant)
            {
                //  pos.EnPassantTarget isn't set yet for this move, so we have to calculate it this way
                int idxPawn = moveTo - ShiftUpDir(us);

                RemoveFeature(ourAccumulation, ourPsq, HalfKAIndex(us, idxPawn, FishPiece(Not(us), Piece.Pawn), ourKing));
                RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, idxPawn, FishPiece(Not(us), Piece.Pawn), theirKing));
            }
        }

        /// <summary>
        /// Removes the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void RemoveFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = (RelativeDimensions * index);
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Subtract(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }

            for (int j = 0; j < PSQTBuckets / FeatureTransformer.PsqtTileHeight; j++)
            {
                psqtAccumulation[j] = Sub256(psqtAccumulation[j], FeatureTransformer.PSQTWeights[index + j * RelativeTileHeight]);
            }
        }


        /// <summary>
        /// Adds the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="AccumulatorPSQT.White"/> or <see cref="AccumulatorPSQT.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void AddFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = (RelativeDimensions * index);
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Add(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }

            for (int j = 0; j < PSQTBuckets / FeatureTransformer.PsqtTileHeight; j++)
            {
                psqtAccumulation[j] = Add256(psqtAccumulation[j], FeatureTransformer.PSQTWeights[index + j * RelativeTileHeight]);
            }

        }



        /// <summary>
        /// Returns a list of active indices for each side. 
        /// Every piece on the board has a unique index based on their king's square, their color, and the
        /// perspective of the player looking at them.
        /// </summary>
        public static int AppendActiveIndices(Position pos, Span<int> active, int perspective)
        {
            int spanIndex = 0;

            ref Bitboard bb = ref pos.bb;

            ulong us = bb.Colors[perspective];
            ulong them = bb.Colors[Not(perspective)];

            if (EnableAssertions)
            {
                Assert(popcount(us) <= 16, "popcount(bb.Colors[" + ColorToString(perspective) + "] was " + popcount(us) + "! (should be <= 16)");
                Assert(popcount(them) <= 16, "popcount(bb.Colors[" + ColorToString(Not(perspective)) + "] was " + popcount(them) + "! (should be <= 16)");
            }

            int ourKing = pos.State->KingSquares[perspective];

            while (us != 0)
            {
                int idx = poplsb(&us);

                int pt = bb.GetPieceAtIndex(idx);
                int fishPT = FishPiece(perspective, pt);
                int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                active[spanIndex++] = kpIdx;
            }

            while (them != 0)
            {
                int idx = poplsb(&them);

                int pt = bb.GetPieceAtIndex(idx);
                int fishPT = FishPiece(Not(perspective), pt);
                int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                active[spanIndex++] = kpIdx;
            }

            return spanIndex;
        }


        /// <summary>
        /// Returns the index of the square <paramref name="s"/>, rotated 180 degrees from white's perspective if <paramref name="perspective"/> is false.
        /// This is then mirrored if <paramref name="ksq"/> is on the A/B/C/D files, which would change the index of a piece on the E file to the D file
        /// and vice versa.
        /// </summary>
        public static int Orient(int perspective, int s, int ksq = 0)
        {
            return (s ^ (perspective * Squares.A8) ^ ((GetIndexFile(ksq) < Files.E ? 1 : 0) * Squares.H1));
        }

        /// <summary>
        /// Returns the feature index for a piece of type <paramref name="fishPT"/> on the square <paramref name="s"/>,
        /// seen by the player with the color <paramref name="perspective"/> and whose king is on <paramref name="ksq"/>
        /// </summary>
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
        public static int FishPiece(int pc, int pt)
        {
            return ((pt + 1) + (pc * 8));
        }




        /// <summary>
        /// Shows the feature indices for each piece on the board and for both perspectives.
        /// </summary>
        public static void Debug_ShowActiveIndices(Position pos)
        {
            ref Bitboard bb = ref pos.bb;

            for (int perspective = 0; perspective < 2; perspective++)
            {
                Log(ColorToString(perspective) + ": ");
                ulong us = bb.Colors[perspective];
                ulong them = bb.Colors[Not(perspective)];
                int ourKing = pos.State->KingSquares[perspective];

                while (us != 0)
                {
                    int idx = poplsb(&us);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(perspective, pt);
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));
                }

                while (them != 0)
                {
                    int idx = poplsb(&them);

                    int pt = bb.GetPieceAtIndex(idx);
                    int fishPT = FishPiece(Not(perspective), pt);
                    int kpIdx = HalfKAIndex(perspective, idx, fishPT, ourKing);
                    Log("\t" + kpIdx + "\t = " + bb.SquareToString(idx));
                }

                Log("\n");
            }
        }

        /// <summary>
        /// Prints out the board, and calculates the value that each piece has on the square it is on by
        /// comparing the NNUE evaluation of the position when that piece is removed to the base evaluation.
        /// This is entirely taken from:
        /// <br></br>
        /// https://github.com/official-stockfish/Stockfish/blob/b25d68f6ee2d016cc0c14b076e79e6c44fdaea2a/src/nnue/evaluate_nnue.cpp#L272C17-L272C17
        /// </summary>
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

            Log("\nNNUE evaluation: " + baseEval + "\n");

            ref AccumulatorPSQT Accumulator = ref *(pos.State->Accumulator);
            ref Bitboard bb = ref pos.bb;
            for (int f = Files.A; f <= Files.H; f++)
            {
                for (int r = 0; r <= 7; r++)
                {
                    int idx = CoordToIndex(f, r);
                    int pt = bb.GetPieceAtIndex(idx);
                    int pc = bb.GetColorAtIndex(idx);
                    int fishPc = FishPiece(pc, pt);
                    int v = ScoreMate;

                    if (pt != None && bb.GetPieceAtIndex(idx) != King)
                    {
                        bb.RemovePiece(idx, pc, pt);

                        Accumulator.RefreshPerspective[White] = true;
                        Accumulator.RefreshPerspective[Black] = true;
                        int eval = GetEvaluation(pos, false);
                        v = baseEval - eval;

                        bb.AddPiece(idx, pc, pt);
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


            int correctBucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;
            Span<sbyte> features = stackalloc sbyte[_TransformedFeaturesBufferLength];

            Log("Bucket\t\tPSQT\t\tPositional\tTotal");
            for (int bucket = 0; bucket < LayerStacks; bucket++)
            {
                Accumulator.RefreshPerspective[White] = Accumulator.RefreshPerspective[Black] = true;
                int psqt = FeatureTransformer.TransformFeatures(pos, features, ref Accumulator, bucket);
                var output = LayerStack[bucket].Propagate(features);

                psqt /= 16;
                output /= 16;

                Log(bucket + "\t\t" + psqt + "\t\t" + output + "\t\t" + (psqt + output) +
                    (bucket == correctBucket ? "\t<-- this bucket is used" : string.Empty));
            }
        }


        /// <summary>
        /// Prints out the board, and shows the value that the given piece has on each empty square on the board.
        /// This is only meant for debugging, or just 
        /// </summary>
        public static void TracePieceValues(int pieceType, int pieceColor)
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

            //  White king on A1, black king on H8
            Position pos = new Position("7k/8/8/8/8/8/8/K7 w - - 0 1", true, owner: SearchPool.MainThread);
            int baseEval = GetEvaluation(pos, false);

            Log("\nNNUE evaluation: " + baseEval + "\n");

            ref Bitboard bb = ref pos.bb;

            for (int i = 0; i < SquareNB; i++)
            {
                if (bb.GetPieceAtIndex(i) != None)
                {
                    writeSquare(GetIndexFile(i), GetIndexRank(i), FishPiece(bb.GetColorAtIndex(i), bb.GetPieceAtIndex(i)), ScoreMate);
                    continue;
                }

                bb.AddPiece(i, pieceColor, pieceType);

                pos.State->Accumulator->RefreshPerspective[White] = true;
                pos.State->Accumulator->RefreshPerspective[Black] = true;
                int eval = GetEvaluation(pos, false);

                bb.RemovePiece(i, pieceColor, pieceType);

                writeSquare(GetIndexFile(i), GetIndexRank(i), FishPiece(pieceColor, pieceType), eval);
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < 3 * 8 + 1; row++)
            {
                Log(new string(board[row]));
            }

            Log("\n");
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
        }
    }
}
