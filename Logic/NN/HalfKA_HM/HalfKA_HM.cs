using static LTChess.Logic.NN.HalfKA_HM.FeatureTransformer;
using static LTChess.Logic.NN.HalfKA_HM.NNCommon;
using static LTChess.Logic.NN.HalfKA_HM.HalfKA_HM;
using static LTChess.Logic.NN.SIMD;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System;

using LTChess.Logic.Data;
using LTChess.Logic.NN.HalfKA_HM.Layers;
using Lizard.Properties;

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

        public const string NNV3 = @"nn-735bba95dec0.nnue";
        public const string NNV4 = @"nn-6877cd24400e.nnue";
        public const string NNV5 = @"nn-e1fb1ade4432.nnue";

        public const string Name = "HalfKAv2_hm(Friend)";

        public const uint VersionValue = 0x7AF32F20u;
        public const uint HashValue = 0x7F234CB8u;
        public const uint Dimensions = SquareNB * PS_NB / 2;
        public const int TransformedFeatureDimensions = 1024;

        public const int MaxActiveDimensions = 32;



        private static AccumulatorPSQT[] AccumulatorStack;
        public static int CurrentAccumulator { private set; get; }

        private static Network[] LayerStack;

        private static FeatureTransformer Transformer;


        private static nint _FeatureBuffer;
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
                LayerStack[i] = new Network();
            }


            //  Set up the feature transformer
            Transformer = new FeatureTransformer();


            _FeatureBuffer = (nint) AlignedAllocZeroed((sizeof(sbyte) * _TransformedFeaturesBufferLength), AllocAlignment);

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
                networkToLoad = NNV5;
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
            Transformer.ReadParameters(br);
            for (int i = 0; i < LayerStacks; i++)
            {
                uint header = br.ReadUInt32();
                LayerStack[i].ReadParameters(br);
            }

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
            uint netHash = LayerStack[0].GetHashValue();
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
        /// Transforms the features on the board into network input, and returns the network output as the evaluation of the position
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetEvaluation(Position pos, bool adjusted = false)
        {
            const int delta = 24;

            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];
            int bucket = (int) (popcount(pos.bb.Occupancy) - 1) / 4;

            Span<sbyte> features = new Span<sbyte>((void*)_FeatureBuffer, _TransformedFeaturesBufferLength);
            int psqt = Transformer.TransformFeatures(pos, features, ref Accumulator, bucket);

            var output = LayerStack[bucket].Propagate(features);

            if (adjusted)
            {
                return (((1024 - delta) * psqt + (1024 + delta) * output) / (1024 * OutputScale));
            }

            return (psqt + output) / OutputScale;
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
        /// Marks both perspectives of the current accumulator as needing to be refreshed.
        /// </summary>
        [MethodImpl(Inline)]
        public static void RefreshNN()
        {
            if (CurrentAccumulator < 0)
            {
                Log("WARN RefreshNN called when CurrentAccumulator == " + CurrentAccumulator + ", setting to 0");
                CurrentAccumulator = 0;
            }

            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];

            Accumulator.RefreshPerspective[White] = true;
            Accumulator.RefreshPerspective[Black] = true;
        }


        /// <summary>
        /// Updates the features in the next accumulator by copying the current features and adding/removing
        /// the features that will be changing. 
        /// <br></br>
        /// If <paramref name="m"/> is a king move, the next accumulator will be marked as needing a refresh.
        /// </summary>
        [MethodImpl(Optimize)]
        public static void MakeMove(Position pos, Move m)
        {
            ref Bitboard bb = ref pos.bb;

            int moveFrom = m.From;
            int moveTo = m.To;

            PushAccumulator();
            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];

            int us = bb.GetColorAtIndex(moveFrom);
            int them = Not(us);

            int ourPiece = bb.GetPieceAtIndex(moveFrom);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            int ourKing = bb.KingIndex(us);
            int theirKing = bb.KingIndex(them);

            var ourAccumulation = Accumulator[us];
            var theirAccumulation = Accumulator[them];

            var ourPsq = Accumulator.PSQ(us);
            var theirPsq = Accumulator.PSQ(them);


            if (ourPiece == Piece.King)
            {
                //  When we make a king move, we will need to do a full recalculation of our features.
                //  We can still update the opponent's side however, since their features are dependent on where THEIR king is, not ours.
                //  This saves us a bit of time later since we won't need to refresh both sides for every king move.
                Accumulator.RefreshPerspective[us] = true;

                RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveFrom, FishPiece(ourPiece, us), theirKing));
                AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(ourPiece, us), theirKing));

                if (m.Capture)
                {
                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, moveTo, FishPiece(theirPiece, Not(us)), theirKing));
                }
                else if (m.Castle)
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

                    RemoveFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookFrom, FishPiece(Piece.Rook, us), theirKing));
                    AddFeature(theirAccumulation, theirPsq, HalfKAIndex(them, rookTo, FishPiece(Piece.Rook, us), theirKing));
                }

                return;
            }

            //  Otherwise, we only need to remove the features that are no longer there (move.From) and the piece that was on
            //  move.To before it was captured, and add the new features (move.To).

            int ourPieceOldIndex_US = HalfKAIndex(us, moveFrom, FishPiece(ourPiece, us), ourKing);
            int ourPieceOldIndex_THEM = HalfKAIndex(them, moveFrom, FishPiece(ourPiece, us), theirKing);

            RemoveFeature(ourAccumulation, ourPsq, ourPieceOldIndex_US);
            RemoveFeature(theirAccumulation, theirPsq, ourPieceOldIndex_THEM);

            if (m.Promotion)
            {
                //  Add the promotion piece instead.
                int ourPieceNewIndex_US = HalfKAIndex(us, moveTo, FishPiece(m.PromotionTo, us), ourKing);
                int ourPieceNewIndex_THEM = HalfKAIndex(them, moveTo, FishPiece(m.PromotionTo, us), theirKing);

                AddFeature(ourAccumulation, ourPsq, ourPieceNewIndex_US);
                AddFeature(theirAccumulation, theirPsq, ourPieceNewIndex_THEM);
            }
            else
            {
                int ourPieceNewIndex_US = HalfKAIndex(us, moveTo, FishPiece(ourPiece, us), ourKing);
                int ourPieceNewIndex_THEM = HalfKAIndex(them, moveTo, FishPiece(ourPiece, us), theirKing);

                AddFeature(ourAccumulation, ourPsq, ourPieceNewIndex_US);
                AddFeature(theirAccumulation, theirPsq, ourPieceNewIndex_THEM);
            }

            if (m.Capture)
            {
                //  A captured piece needs to be removed from both perspectives as well.
                int theirCapturedPieceIndex_US = HalfKAIndex(us, moveTo, FishPiece(theirPiece, Not(us)), ourKing);
                int theirCapturedPieceIndex_THEM = HalfKAIndex(them, moveTo, FishPiece(theirPiece, Not(us)), theirKing);

                RemoveFeature(ourAccumulation, ourPsq, theirCapturedPieceIndex_US);
                RemoveFeature(theirAccumulation, theirPsq, theirCapturedPieceIndex_THEM);
            }

            if (m.EnPassant)
            {
                //  pos.EnPassantTarget isn't set yet for this move, so we have to calculate it this way
                int idxPawn = moveTo + ShiftDownDir(us);

                int theirCapturedPieceIndex_US = HalfKAIndex(us, idxPawn, FishPiece(Piece.Pawn, Not(us)), ourKing);
                int theirCapturedPieceIndex_THEM = HalfKAIndex(them, idxPawn, FishPiece(Piece.Pawn, Not(us)), theirKing);

                RemoveFeature(ourAccumulation, ourPsq, theirCapturedPieceIndex_US);
                RemoveFeature(theirAccumulation, theirPsq, theirCapturedPieceIndex_THEM);
            }
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

            ref Bitboard bb = ref pos.bb;

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
        /// Returns a list of active indices for each side. 
        /// Every piece on the board has a unique index based on their king's square, their color, and the
        /// perspective of the player looking at them.
        /// </summary>
        [MethodImpl(Optimize)]
        public static int AppendActiveIndices(Position pos, Span<int> active, int perspective)
        {
            int spanIndex = 0;

            ref Bitboard bb = ref pos.bb;

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

            return spanIndex;
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
            ref Bitboard bb = ref pos.bb;

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

            ref Bitboard bb = ref pos.bb;
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
                        bb.RemovePiece(idx, pc, pt);

                        AccumulatorStack[CurrentAccumulator].RefreshPerspective[White] = true;
                        AccumulatorStack[CurrentAccumulator].RefreshPerspective[Black] = true;
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

            ref AccumulatorPSQT Accumulator = ref AccumulatorStack[CurrentAccumulator];
            int correctBucket = (int)(popcount(pos.bb.Occupancy) - 1) / 4;

            Span<sbyte> features = new Span<sbyte>((void*)_FeatureBuffer, _TransformedFeaturesBufferLength);


            Log("Bucket\t\tPSQT\t\tPositional\tTotal");
            for (int bucket = 0; bucket < LayerStacks; bucket++)
            {
                Accumulator.RefreshPerspective[White] = Accumulator.RefreshPerspective[Black] = true;
                int psqt = Transformer.TransformFeatures(pos, features, ref Accumulator, bucket);
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
            Position pos = new Position("7k/8/8/8/8/8/8/K7 w - - 0 1");
            int baseEval = GetEvaluation(pos, false);

            Log("\nNNUE evaluation: " + baseEval + "\n");

            ref Bitboard bb = ref pos.bb;

            for (int i = 0; i < SquareNB; i++)
            {
                if (bb.GetPieceAtIndex(i) != None)
                {
                    writeSquare(GetIndexFile(i), GetIndexRank(i), FishPiece(bb.GetPieceAtIndex(i), bb.GetColorAtIndex(i)), ScoreMate);
                    continue;
                }

                bb.AddPiece(i, pieceColor, pieceType);

                AccumulatorStack[CurrentAccumulator].RefreshPerspective[White] = true;
                AccumulatorStack[CurrentAccumulator].RefreshPerspective[Black] = true;
                int eval = GetEvaluation(pos, false);

                bb.RemovePiece(i, pieceColor, pieceType);

                writeSquare(GetIndexFile(i), GetIndexRank(i), FishPiece(pieceType, pieceColor), eval);
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
