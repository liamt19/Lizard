
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using Lizard.Properties;

using static Lizard.Logic.NN.HKA.HalfKA_HM.UniquePiece;
using static Lizard.Logic.NN.HKA.NNCommon;

namespace Lizard.Logic.NN.HKA
{
    /// <summary>
    /// Uses SFNNv6/7/8 architectures.
    /// https://raw.githubusercontent.com/official-stockfish/nnue-pytorch/c15e33ccbe0bfe63d0aa4dbe69307afc7214b1d0/docs/img/SFNNv6_architecture_detailed_v2.svg
    /// 
    /// To use a net from a different architecture, change <see cref="TransformedFeatureDimensions"/> to 1536/2048/2560 for 6/7/8.
    /// </summary>
    [SkipStaticConstructor]
    public static unsafe class HalfKA_HM
    {
        public const string NNV6_Sparse_LEB = @"nn-5af11540bbfe.nnue";
        public const string NetworkName = NNV6_Sparse_LEB;

        public const string Name = "HalfKAv2_hm(Friend)";

        public const uint VersionValue = 0x7AF32F20u;
        public const uint HashValue = 0x7F234CB8u;
        public const uint Dimensions = SquareNB * PS_NB / 2;

        public const int TransformedFeatureDimensions = 1536;

        public const int MaxActiveDimensions = 32;

        private static Network[] LayerStack;

        //private static FeatureTransformer Transformer;

        private const int _TransformedFeaturesBufferLength = FeatureTransformer.BufferSize;


        static HalfKA_HM()
        {

            string networkToLoad = NetworkName;

            try
            {
                var evalFile = Assembly.GetEntryAssembly().GetCustomAttribute<EvalFileAttribute>().EvalFile;
                networkToLoad = evalFile;
            }
            catch { }

            Initialize(networkToLoad);
        }

        public static void Initialize(string networkToLoad, bool exitIfFail = true)
        {
            //  Set up the network architecture layers
            LayerStack = new Network[LayerStacks];
            for (int i = 0; i < LayerStacks; i++)
            {
                LayerStack[i] = new Network();
            }

            using Stream netFile = NNUE.TryOpenFile(networkToLoad, exitIfFail);
            using BinaryReader br = new BinaryReader(netFile);
            var stream = br.BaseStream;
            long toRead = 40119326;// ExpectedNetworkSize;
            if (stream.Position + toRead > stream.Length)
            {
                Console.WriteLine("HalfKA_HM's BinaryReader doesn't have enough data for all weights and biases to be read!");
                Console.WriteLine("It expects to read " + toRead + " bytes, but the stream's position is " + stream.Position + "/" + stream.Length);
                Console.WriteLine("The file being loaded is either not a valid 768 network, or has different layer sizes than the hardcoded ones.");
                if (exitIfFail)
                {
                    Environment.Exit(-1);
                }
                else
                {
                    return;
                }
            }

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
            uint finalHash = netHash ^ ftHash;

            if (hashValue != finalHash)
            {
                Debug.WriteLine("Expected header hash " + hashValue.ToString("X") + " but got " + finalHash.ToString("X"));
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
            Debug.WriteLine("Network architecture: '" + arch + "'");
        }

        public static void RefreshAccumulator(Position pos)
        {
            FeatureTransformer.RefreshAccumulatorPerspective(pos, ref *pos.State->Accumulator, White);
            FeatureTransformer.RefreshAccumulatorPerspective(pos, ref *pos.State->Accumulator, Black);
        }


        /// <summary>
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        public static int GetEvaluation(Position pos, bool adjusted = false)
        {
            ref Accumulator Accumulator = ref *pos.State->Accumulator;
            return GetEvaluation(pos, ref Accumulator, adjusted);
        }

        public static int GetEvaluation(Position pos, ref Accumulator accumulator, bool adjusted = false)
        {
            const int delta = 24;

            int bucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;

            Span<sbyte> features = stackalloc sbyte[_TransformedFeaturesBufferLength];
            int psqt = FeatureTransformer.TransformFeatures(pos, features, ref accumulator, bucket);

            var positional = LayerStack[bucket].Propagate(features);

            int v;

            if (adjusted)
            {
                v = (((1024 - delta) * psqt) + ((1024 + delta) * positional)) / (1024 * OutputScale);
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

            Accumulator* accumulator = pos.NextState->Accumulator;
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
                accumulator->NeedsRefresh[us] = true;

                RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveFrom, FishPiece(us, ourPiece), theirKing));
                AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(us, ourPiece), theirKing));

                if (theirPiece != None && !m.Castle)
                {
                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(Not(us), theirPiece), theirKing));
                }
                else if (m.Castle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare;

                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookFromSq, FishPiece(us, Piece.Rook), theirKing));
                    AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookToSq, FishPiece(us, Piece.Rook), theirKing));
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

            if (theirPiece != None && !m.Castle)
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
        /// <param name="accumulation">A reference to either <see cref="Accumulator.White"/> or <see cref="Accumulator.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void _RemoveFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = RelativeDimensions * index;
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Subtract(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }

            for (int j = 0; j < PSQTBuckets / FeatureTransformer.PsqtTileHeight; j++)
            {
                psqtAccumulation[j] = Avx2.Subtract(psqtAccumulation[j], FeatureTransformer.PSQTWeights[index + (j * RelativeTileHeight)]);
            }
        }


        /// <summary>
        /// Adds the feature with the corresponding <paramref name="index"/> to the Accumulator side <paramref name="accumulation"/>.
        /// </summary>
        /// <param name="accumulation">A reference to either <see cref="Accumulator.White"/> or <see cref="Accumulator.Black"/></param>
        /// <param name="index">The feature index calculated with <see cref="HalfKAIndex"/></param>
        public static void _AddFeature(in Vector256<short>* accumulation, in Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = RelativeDimensions * index;
            for (int j = 0; j < NumChunks; j++)
            {
                accumulation[j] = Avx2.Add(accumulation[j], FeatureTransformer.Weights[ci + j]);
            }

            for (int j = 0; j < PSQTBuckets / FeatureTransformer.PsqtTileHeight; j++)
            {
                psqtAccumulation[j] = Avx2.Add(psqtAccumulation[j], FeatureTransformer.PSQTWeights[index + (j * RelativeTileHeight)]);
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
            return s ^ (perspective * Squares.A8) ^ ((GetIndexFile(ksq) < Files.E ? 1 : 0) * Squares.H1);
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
            int fishIndex = (int)(orientSq + psqI + (PS_NB * KingBuckets[o_ksq]));
            return fishIndex;
        }


        /// <summary>
        /// Returns an integer representing the piece of color <paramref name="pc"/> and type <paramref name="pt"/>
        /// in Stockfish 12's format, which has all of white's pieces before black's. It looks like:
        /// PS_NONE = 0, W_PAWN = 1, W_KNIGHT = 2, ... B_PAWN = 9, ... PIECE_NB = 16
        /// </summary>
        public static int FishPiece(int pc, int pt)
        {
            return pt + 1 + (pc * 8);
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
            char[][] board = new char[(3 * 8) + 1][];
            for (int i = 0; i < (3 * 8) + 1; i++)
            {
                board[i] = new char[(8 * 8) + 2];
                Array.Fill(board[i], ' ');
            }



            for (int row = 0; row < (3 * 8) + 1; row++)
            {
                board[row][(8 * 8) + 1] = '\0';
            }

            void writeSquare(int file, int rank, int pc, int value)
            {
                const string PieceToChar = " PNBRQK  pnbrqk";

                int x = file * 8;
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

            ref Accumulator Accumulator = ref *pos.State->Accumulator;
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

                        Accumulator.NeedsRefresh[White] = true;
                        Accumulator.NeedsRefresh[Black] = true;
                        int eval = GetEvaluation(pos, false);
                        v = baseEval - eval;

                        bb.AddPiece(idx, pc, pt);
                    }

                    writeSquare(f, r, fishPc, v);
                }
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < (3 * 8) + 1; row++)
            {
                Log(new string(board[row]));
            }

            Log("\n");


            int correctBucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;
            Span<sbyte> features = stackalloc sbyte[_TransformedFeaturesBufferLength];

            Log("Bucket\t\tPSQT\t\tPositional\tTotal");
            for (int bucket = 0; bucket < LayerStacks; bucket++)
            {
                Accumulator.NeedsRefresh[White] = Accumulator.NeedsRefresh[Black] = true;
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
            char[][] board = new char[(3 * 8) + 1][];
            for (int i = 0; i < (3 * 8) + 1; i++)
            {
                board[i] = new char[(8 * 8) + 2];
                Array.Fill(board[i], ' ');
            }



            for (int row = 0; row < (3 * 8) + 1; row++)
            {
                board[row][(8 * 8) + 1] = '\0';
            }

            void writeSquare(int file, int rank, int pc, int value)
            {
                const string PieceToChar = " PNBRQK  pnbrqk";

                int x = file * 8;
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

                pos.State->Accumulator->NeedsRefresh[White] = true;
                pos.State->Accumulator->NeedsRefresh[Black] = true;
                int eval = GetEvaluation(pos, false);

                bb.RemovePiece(i, pieceColor, pieceType);

                writeSquare(GetIndexFile(i), GetIndexRank(i), FishPiece(pieceColor, pieceType), eval);
            }

            Log("NNUE derived piece values:\n");
            for (int row = 0; row < (3 * 8) + 1; row++)
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
            public const uint PS_NONE = 0;
            public const uint PS_W_PAWN = 0;
            public const uint PS_B_PAWN = 1 * SquareNB;
            public const uint PS_W_KNIGHT = 2 * SquareNB;
            public const uint PS_B_KNIGHT = 3 * SquareNB;
            public const uint PS_W_BISHOP = 4 * SquareNB;
            public const uint PS_B_BISHOP = 5 * SquareNB;
            public const uint PS_W_ROOK = 6 * SquareNB;
            public const uint PS_B_ROOK = 7 * SquareNB;
            public const uint PS_W_QUEEN = 8 * SquareNB;
            public const uint PS_B_QUEEN = 9 * SquareNB;
            public const uint PS_KING = 10 * SquareNB;
            public const uint PS_NB = 11 * SquareNB;
        }




        public static void AddFeature(Vector256<short>* accumulation, Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = RelativeDimensions * index;
            accumulation[ 0] = Avx2.Add(accumulation[ 0], FeatureTransformer.Weights[ci +  0]);
            accumulation[ 1] = Avx2.Add(accumulation[ 1], FeatureTransformer.Weights[ci +  1]);
            accumulation[ 2] = Avx2.Add(accumulation[ 2], FeatureTransformer.Weights[ci +  2]);
            accumulation[ 3] = Avx2.Add(accumulation[ 3], FeatureTransformer.Weights[ci +  3]);
            accumulation[ 4] = Avx2.Add(accumulation[ 4], FeatureTransformer.Weights[ci +  4]);
            accumulation[ 5] = Avx2.Add(accumulation[ 5], FeatureTransformer.Weights[ci +  5]);
            accumulation[ 6] = Avx2.Add(accumulation[ 6], FeatureTransformer.Weights[ci +  6]);
            accumulation[ 7] = Avx2.Add(accumulation[ 7], FeatureTransformer.Weights[ci +  7]);
            accumulation[ 8] = Avx2.Add(accumulation[ 8], FeatureTransformer.Weights[ci +  8]);
            accumulation[ 9] = Avx2.Add(accumulation[ 9], FeatureTransformer.Weights[ci +  9]);
            accumulation[10] = Avx2.Add(accumulation[10], FeatureTransformer.Weights[ci + 10]);
            accumulation[11] = Avx2.Add(accumulation[11], FeatureTransformer.Weights[ci + 11]);
            accumulation[12] = Avx2.Add(accumulation[12], FeatureTransformer.Weights[ci + 12]);
            accumulation[13] = Avx2.Add(accumulation[13], FeatureTransformer.Weights[ci + 13]);
            accumulation[14] = Avx2.Add(accumulation[14], FeatureTransformer.Weights[ci + 14]);
            accumulation[15] = Avx2.Add(accumulation[15], FeatureTransformer.Weights[ci + 15]);
            accumulation[16] = Avx2.Add(accumulation[16], FeatureTransformer.Weights[ci + 16]);
            accumulation[17] = Avx2.Add(accumulation[17], FeatureTransformer.Weights[ci + 17]);
            accumulation[18] = Avx2.Add(accumulation[18], FeatureTransformer.Weights[ci + 18]);
            accumulation[19] = Avx2.Add(accumulation[19], FeatureTransformer.Weights[ci + 19]);
            accumulation[20] = Avx2.Add(accumulation[20], FeatureTransformer.Weights[ci + 20]);
            accumulation[21] = Avx2.Add(accumulation[21], FeatureTransformer.Weights[ci + 21]);
            accumulation[22] = Avx2.Add(accumulation[22], FeatureTransformer.Weights[ci + 22]);
            accumulation[23] = Avx2.Add(accumulation[23], FeatureTransformer.Weights[ci + 23]);
            accumulation[24] = Avx2.Add(accumulation[24], FeatureTransformer.Weights[ci + 24]);
            accumulation[25] = Avx2.Add(accumulation[25], FeatureTransformer.Weights[ci + 25]);
            accumulation[26] = Avx2.Add(accumulation[26], FeatureTransformer.Weights[ci + 26]);
            accumulation[27] = Avx2.Add(accumulation[27], FeatureTransformer.Weights[ci + 27]);
            accumulation[28] = Avx2.Add(accumulation[28], FeatureTransformer.Weights[ci + 28]);
            accumulation[29] = Avx2.Add(accumulation[29], FeatureTransformer.Weights[ci + 29]);
            accumulation[30] = Avx2.Add(accumulation[30], FeatureTransformer.Weights[ci + 30]);
            accumulation[31] = Avx2.Add(accumulation[31], FeatureTransformer.Weights[ci + 31]);
            accumulation[32] = Avx2.Add(accumulation[32], FeatureTransformer.Weights[ci + 32]);
            accumulation[33] = Avx2.Add(accumulation[33], FeatureTransformer.Weights[ci + 33]);
            accumulation[34] = Avx2.Add(accumulation[34], FeatureTransformer.Weights[ci + 34]);
            accumulation[35] = Avx2.Add(accumulation[35], FeatureTransformer.Weights[ci + 35]);
            accumulation[36] = Avx2.Add(accumulation[36], FeatureTransformer.Weights[ci + 36]);
            accumulation[37] = Avx2.Add(accumulation[37], FeatureTransformer.Weights[ci + 37]);
            accumulation[38] = Avx2.Add(accumulation[38], FeatureTransformer.Weights[ci + 38]);
            accumulation[39] = Avx2.Add(accumulation[39], FeatureTransformer.Weights[ci + 39]);
            accumulation[40] = Avx2.Add(accumulation[40], FeatureTransformer.Weights[ci + 40]);
            accumulation[41] = Avx2.Add(accumulation[41], FeatureTransformer.Weights[ci + 41]);
            accumulation[42] = Avx2.Add(accumulation[42], FeatureTransformer.Weights[ci + 42]);
            accumulation[43] = Avx2.Add(accumulation[43], FeatureTransformer.Weights[ci + 43]);
            accumulation[44] = Avx2.Add(accumulation[44], FeatureTransformer.Weights[ci + 44]);
            accumulation[45] = Avx2.Add(accumulation[45], FeatureTransformer.Weights[ci + 45]);
            accumulation[46] = Avx2.Add(accumulation[46], FeatureTransformer.Weights[ci + 46]);
            accumulation[47] = Avx2.Add(accumulation[47], FeatureTransformer.Weights[ci + 47]);
            accumulation[48] = Avx2.Add(accumulation[48], FeatureTransformer.Weights[ci + 48]);
            accumulation[49] = Avx2.Add(accumulation[49], FeatureTransformer.Weights[ci + 49]);
            accumulation[50] = Avx2.Add(accumulation[50], FeatureTransformer.Weights[ci + 50]);
            accumulation[51] = Avx2.Add(accumulation[51], FeatureTransformer.Weights[ci + 51]);
            accumulation[52] = Avx2.Add(accumulation[52], FeatureTransformer.Weights[ci + 52]);
            accumulation[53] = Avx2.Add(accumulation[53], FeatureTransformer.Weights[ci + 53]);
            accumulation[54] = Avx2.Add(accumulation[54], FeatureTransformer.Weights[ci + 54]);
            accumulation[55] = Avx2.Add(accumulation[55], FeatureTransformer.Weights[ci + 55]);
            accumulation[56] = Avx2.Add(accumulation[56], FeatureTransformer.Weights[ci + 56]);
            accumulation[57] = Avx2.Add(accumulation[57], FeatureTransformer.Weights[ci + 57]);
            accumulation[58] = Avx2.Add(accumulation[58], FeatureTransformer.Weights[ci + 58]);
            accumulation[59] = Avx2.Add(accumulation[59], FeatureTransformer.Weights[ci + 59]);
            accumulation[60] = Avx2.Add(accumulation[60], FeatureTransformer.Weights[ci + 60]);
            accumulation[61] = Avx2.Add(accumulation[61], FeatureTransformer.Weights[ci + 61]);
            accumulation[62] = Avx2.Add(accumulation[62], FeatureTransformer.Weights[ci + 62]);
            accumulation[63] = Avx2.Add(accumulation[63], FeatureTransformer.Weights[ci + 63]);
            accumulation[64] = Avx2.Add(accumulation[64], FeatureTransformer.Weights[ci + 64]);
            accumulation[65] = Avx2.Add(accumulation[65], FeatureTransformer.Weights[ci + 65]);
            accumulation[66] = Avx2.Add(accumulation[66], FeatureTransformer.Weights[ci + 66]);
            accumulation[67] = Avx2.Add(accumulation[67], FeatureTransformer.Weights[ci + 67]);
            accumulation[68] = Avx2.Add(accumulation[68], FeatureTransformer.Weights[ci + 68]);
            accumulation[69] = Avx2.Add(accumulation[69], FeatureTransformer.Weights[ci + 69]);
            accumulation[70] = Avx2.Add(accumulation[70], FeatureTransformer.Weights[ci + 70]);
            accumulation[71] = Avx2.Add(accumulation[71], FeatureTransformer.Weights[ci + 71]);
            accumulation[72] = Avx2.Add(accumulation[72], FeatureTransformer.Weights[ci + 72]);
            accumulation[73] = Avx2.Add(accumulation[73], FeatureTransformer.Weights[ci + 73]);
            accumulation[74] = Avx2.Add(accumulation[74], FeatureTransformer.Weights[ci + 74]);
            accumulation[75] = Avx2.Add(accumulation[75], FeatureTransformer.Weights[ci + 75]);
            accumulation[76] = Avx2.Add(accumulation[76], FeatureTransformer.Weights[ci + 76]);
            accumulation[77] = Avx2.Add(accumulation[77], FeatureTransformer.Weights[ci + 77]);
            accumulation[78] = Avx2.Add(accumulation[78], FeatureTransformer.Weights[ci + 78]);
            accumulation[79] = Avx2.Add(accumulation[79], FeatureTransformer.Weights[ci + 79]);
            accumulation[80] = Avx2.Add(accumulation[80], FeatureTransformer.Weights[ci + 80]);
            accumulation[81] = Avx2.Add(accumulation[81], FeatureTransformer.Weights[ci + 81]);
            accumulation[82] = Avx2.Add(accumulation[82], FeatureTransformer.Weights[ci + 82]);
            accumulation[83] = Avx2.Add(accumulation[83], FeatureTransformer.Weights[ci + 83]);
            accumulation[84] = Avx2.Add(accumulation[84], FeatureTransformer.Weights[ci + 84]);
            accumulation[85] = Avx2.Add(accumulation[85], FeatureTransformer.Weights[ci + 85]);
            accumulation[86] = Avx2.Add(accumulation[86], FeatureTransformer.Weights[ci + 86]);
            accumulation[87] = Avx2.Add(accumulation[87], FeatureTransformer.Weights[ci + 87]);
            accumulation[88] = Avx2.Add(accumulation[88], FeatureTransformer.Weights[ci + 88]);
            accumulation[89] = Avx2.Add(accumulation[89], FeatureTransformer.Weights[ci + 89]);
            accumulation[90] = Avx2.Add(accumulation[90], FeatureTransformer.Weights[ci + 90]);
            accumulation[91] = Avx2.Add(accumulation[91], FeatureTransformer.Weights[ci + 91]);
            accumulation[92] = Avx2.Add(accumulation[92], FeatureTransformer.Weights[ci + 92]);
            accumulation[93] = Avx2.Add(accumulation[93], FeatureTransformer.Weights[ci + 93]);
            accumulation[94] = Avx2.Add(accumulation[94], FeatureTransformer.Weights[ci + 94]);
            accumulation[95] = Avx2.Add(accumulation[95], FeatureTransformer.Weights[ci + 95]);
            accumulation[96] = Avx2.Add(accumulation[96], FeatureTransformer.Weights[ci + 96]);

            psqtAccumulation[0] = Avx2.Add(psqtAccumulation[0], FeatureTransformer.PSQTWeights[index + (0 * RelativeTileHeight)]);
        }

        public static void RemoveFeature(Vector256<short>* accumulation, Vector256<int>* psqtAccumulation, int index)
        {
            const uint NumChunks = FeatureTransformer.HalfDimensions / (SimdWidth / 2);

            const int RelativeDimensions = (int)FeatureTransformer.HalfDimensions / 16;
            const int RelativeTileHeight = FeatureTransformer.TileHeight / 16;

            int ci = RelativeDimensions * index;
            accumulation[ 0] = Avx2.Subtract(accumulation[ 0], FeatureTransformer.Weights[ci +  0]);
            accumulation[ 1] = Avx2.Subtract(accumulation[ 1], FeatureTransformer.Weights[ci +  1]);
            accumulation[ 2] = Avx2.Subtract(accumulation[ 2], FeatureTransformer.Weights[ci +  2]);
            accumulation[ 3] = Avx2.Subtract(accumulation[ 3], FeatureTransformer.Weights[ci +  3]);
            accumulation[ 4] = Avx2.Subtract(accumulation[ 4], FeatureTransformer.Weights[ci +  4]);
            accumulation[ 5] = Avx2.Subtract(accumulation[ 5], FeatureTransformer.Weights[ci +  5]);
            accumulation[ 6] = Avx2.Subtract(accumulation[ 6], FeatureTransformer.Weights[ci +  6]);
            accumulation[ 7] = Avx2.Subtract(accumulation[ 7], FeatureTransformer.Weights[ci +  7]);
            accumulation[ 8] = Avx2.Subtract(accumulation[ 8], FeatureTransformer.Weights[ci +  8]);
            accumulation[ 9] = Avx2.Subtract(accumulation[ 9], FeatureTransformer.Weights[ci +  9]);
            accumulation[10] = Avx2.Subtract(accumulation[10], FeatureTransformer.Weights[ci + 10]);
            accumulation[11] = Avx2.Subtract(accumulation[11], FeatureTransformer.Weights[ci + 11]);
            accumulation[12] = Avx2.Subtract(accumulation[12], FeatureTransformer.Weights[ci + 12]);
            accumulation[13] = Avx2.Subtract(accumulation[13], FeatureTransformer.Weights[ci + 13]);
            accumulation[14] = Avx2.Subtract(accumulation[14], FeatureTransformer.Weights[ci + 14]);
            accumulation[15] = Avx2.Subtract(accumulation[15], FeatureTransformer.Weights[ci + 15]);
            accumulation[16] = Avx2.Subtract(accumulation[16], FeatureTransformer.Weights[ci + 16]);
            accumulation[17] = Avx2.Subtract(accumulation[17], FeatureTransformer.Weights[ci + 17]);
            accumulation[18] = Avx2.Subtract(accumulation[18], FeatureTransformer.Weights[ci + 18]);
            accumulation[19] = Avx2.Subtract(accumulation[19], FeatureTransformer.Weights[ci + 19]);
            accumulation[20] = Avx2.Subtract(accumulation[20], FeatureTransformer.Weights[ci + 20]);
            accumulation[21] = Avx2.Subtract(accumulation[21], FeatureTransformer.Weights[ci + 21]);
            accumulation[22] = Avx2.Subtract(accumulation[22], FeatureTransformer.Weights[ci + 22]);
            accumulation[23] = Avx2.Subtract(accumulation[23], FeatureTransformer.Weights[ci + 23]);
            accumulation[24] = Avx2.Subtract(accumulation[24], FeatureTransformer.Weights[ci + 24]);
            accumulation[25] = Avx2.Subtract(accumulation[25], FeatureTransformer.Weights[ci + 25]);
            accumulation[26] = Avx2.Subtract(accumulation[26], FeatureTransformer.Weights[ci + 26]);
            accumulation[27] = Avx2.Subtract(accumulation[27], FeatureTransformer.Weights[ci + 27]);
            accumulation[28] = Avx2.Subtract(accumulation[28], FeatureTransformer.Weights[ci + 28]);
            accumulation[29] = Avx2.Subtract(accumulation[29], FeatureTransformer.Weights[ci + 29]);
            accumulation[30] = Avx2.Subtract(accumulation[30], FeatureTransformer.Weights[ci + 30]);
            accumulation[31] = Avx2.Subtract(accumulation[31], FeatureTransformer.Weights[ci + 31]);
            accumulation[32] = Avx2.Subtract(accumulation[32], FeatureTransformer.Weights[ci + 32]);
            accumulation[33] = Avx2.Subtract(accumulation[33], FeatureTransformer.Weights[ci + 33]);
            accumulation[34] = Avx2.Subtract(accumulation[34], FeatureTransformer.Weights[ci + 34]);
            accumulation[35] = Avx2.Subtract(accumulation[35], FeatureTransformer.Weights[ci + 35]);
            accumulation[36] = Avx2.Subtract(accumulation[36], FeatureTransformer.Weights[ci + 36]);
            accumulation[37] = Avx2.Subtract(accumulation[37], FeatureTransformer.Weights[ci + 37]);
            accumulation[38] = Avx2.Subtract(accumulation[38], FeatureTransformer.Weights[ci + 38]);
            accumulation[39] = Avx2.Subtract(accumulation[39], FeatureTransformer.Weights[ci + 39]);
            accumulation[40] = Avx2.Subtract(accumulation[40], FeatureTransformer.Weights[ci + 40]);
            accumulation[41] = Avx2.Subtract(accumulation[41], FeatureTransformer.Weights[ci + 41]);
            accumulation[42] = Avx2.Subtract(accumulation[42], FeatureTransformer.Weights[ci + 42]);
            accumulation[43] = Avx2.Subtract(accumulation[43], FeatureTransformer.Weights[ci + 43]);
            accumulation[44] = Avx2.Subtract(accumulation[44], FeatureTransformer.Weights[ci + 44]);
            accumulation[45] = Avx2.Subtract(accumulation[45], FeatureTransformer.Weights[ci + 45]);
            accumulation[46] = Avx2.Subtract(accumulation[46], FeatureTransformer.Weights[ci + 46]);
            accumulation[47] = Avx2.Subtract(accumulation[47], FeatureTransformer.Weights[ci + 47]);
            accumulation[48] = Avx2.Subtract(accumulation[48], FeatureTransformer.Weights[ci + 48]);
            accumulation[49] = Avx2.Subtract(accumulation[49], FeatureTransformer.Weights[ci + 49]);
            accumulation[50] = Avx2.Subtract(accumulation[50], FeatureTransformer.Weights[ci + 50]);
            accumulation[51] = Avx2.Subtract(accumulation[51], FeatureTransformer.Weights[ci + 51]);
            accumulation[52] = Avx2.Subtract(accumulation[52], FeatureTransformer.Weights[ci + 52]);
            accumulation[53] = Avx2.Subtract(accumulation[53], FeatureTransformer.Weights[ci + 53]);
            accumulation[54] = Avx2.Subtract(accumulation[54], FeatureTransformer.Weights[ci + 54]);
            accumulation[55] = Avx2.Subtract(accumulation[55], FeatureTransformer.Weights[ci + 55]);
            accumulation[56] = Avx2.Subtract(accumulation[56], FeatureTransformer.Weights[ci + 56]);
            accumulation[57] = Avx2.Subtract(accumulation[57], FeatureTransformer.Weights[ci + 57]);
            accumulation[58] = Avx2.Subtract(accumulation[58], FeatureTransformer.Weights[ci + 58]);
            accumulation[59] = Avx2.Subtract(accumulation[59], FeatureTransformer.Weights[ci + 59]);
            accumulation[60] = Avx2.Subtract(accumulation[60], FeatureTransformer.Weights[ci + 60]);
            accumulation[61] = Avx2.Subtract(accumulation[61], FeatureTransformer.Weights[ci + 61]);
            accumulation[62] = Avx2.Subtract(accumulation[62], FeatureTransformer.Weights[ci + 62]);
            accumulation[63] = Avx2.Subtract(accumulation[63], FeatureTransformer.Weights[ci + 63]);
            accumulation[64] = Avx2.Subtract(accumulation[64], FeatureTransformer.Weights[ci + 64]);
            accumulation[65] = Avx2.Subtract(accumulation[65], FeatureTransformer.Weights[ci + 65]);
            accumulation[66] = Avx2.Subtract(accumulation[66], FeatureTransformer.Weights[ci + 66]);
            accumulation[67] = Avx2.Subtract(accumulation[67], FeatureTransformer.Weights[ci + 67]);
            accumulation[68] = Avx2.Subtract(accumulation[68], FeatureTransformer.Weights[ci + 68]);
            accumulation[69] = Avx2.Subtract(accumulation[69], FeatureTransformer.Weights[ci + 69]);
            accumulation[70] = Avx2.Subtract(accumulation[70], FeatureTransformer.Weights[ci + 70]);
            accumulation[71] = Avx2.Subtract(accumulation[71], FeatureTransformer.Weights[ci + 71]);
            accumulation[72] = Avx2.Subtract(accumulation[72], FeatureTransformer.Weights[ci + 72]);
            accumulation[73] = Avx2.Subtract(accumulation[73], FeatureTransformer.Weights[ci + 73]);
            accumulation[74] = Avx2.Subtract(accumulation[74], FeatureTransformer.Weights[ci + 74]);
            accumulation[75] = Avx2.Subtract(accumulation[75], FeatureTransformer.Weights[ci + 75]);
            accumulation[76] = Avx2.Subtract(accumulation[76], FeatureTransformer.Weights[ci + 76]);
            accumulation[77] = Avx2.Subtract(accumulation[77], FeatureTransformer.Weights[ci + 77]);
            accumulation[78] = Avx2.Subtract(accumulation[78], FeatureTransformer.Weights[ci + 78]);
            accumulation[79] = Avx2.Subtract(accumulation[79], FeatureTransformer.Weights[ci + 79]);
            accumulation[80] = Avx2.Subtract(accumulation[80], FeatureTransformer.Weights[ci + 80]);
            accumulation[81] = Avx2.Subtract(accumulation[81], FeatureTransformer.Weights[ci + 81]);
            accumulation[82] = Avx2.Subtract(accumulation[82], FeatureTransformer.Weights[ci + 82]);
            accumulation[83] = Avx2.Subtract(accumulation[83], FeatureTransformer.Weights[ci + 83]);
            accumulation[84] = Avx2.Subtract(accumulation[84], FeatureTransformer.Weights[ci + 84]);
            accumulation[85] = Avx2.Subtract(accumulation[85], FeatureTransformer.Weights[ci + 85]);
            accumulation[86] = Avx2.Subtract(accumulation[86], FeatureTransformer.Weights[ci + 86]);
            accumulation[87] = Avx2.Subtract(accumulation[87], FeatureTransformer.Weights[ci + 87]);
            accumulation[88] = Avx2.Subtract(accumulation[88], FeatureTransformer.Weights[ci + 88]);
            accumulation[89] = Avx2.Subtract(accumulation[89], FeatureTransformer.Weights[ci + 89]);
            accumulation[90] = Avx2.Subtract(accumulation[90], FeatureTransformer.Weights[ci + 90]);
            accumulation[91] = Avx2.Subtract(accumulation[91], FeatureTransformer.Weights[ci + 91]);
            accumulation[92] = Avx2.Subtract(accumulation[92], FeatureTransformer.Weights[ci + 92]);
            accumulation[93] = Avx2.Subtract(accumulation[93], FeatureTransformer.Weights[ci + 93]);
            accumulation[94] = Avx2.Subtract(accumulation[94], FeatureTransformer.Weights[ci + 94]);
            accumulation[95] = Avx2.Subtract(accumulation[95], FeatureTransformer.Weights[ci + 95]);
            accumulation[96] = Avx2.Subtract(accumulation[96], FeatureTransformer.Weights[ci + 96]);

            psqtAccumulation[0] = Avx2.Subtract(psqtAccumulation[0], FeatureTransformer.PSQTWeights[index + (0 * RelativeTileHeight)]);
        }
    }
}
