namespace LTChess.Logic.Search
{
    public static class EvaluationConstants
    {
        public const int EndgameNoEval = short.MinValue - 1;
        public const int GamePhaseNormal = 0;
        public const int GamePhaseEndgame = 1;

        public const int EndgameMaterial = (ValuePawn * 2 * 4) + (ValueRook * 2) + (ValueKnight * 2);

        public const int ValuePawn = 210;
        public const int ValueKnight = 840;
        public const int ValueBishop = 920;
        public const int ValueRook = 1370;
        public const int ValueQueen = 2690;

        public static readonly int[] PieceValues = { ValuePawn, ValueKnight, ValueBishop, ValueRook, ValueQueen };

        public static double[] ScaleMaterial = { 1.75, 2.00 };
        public static double[] ScalePositional = { 1.55, 1.80 };
        public static double[] ScalePawns = { 0.8, 1.25 };
        public static double[] ScaleKnights = { 0.9, 1.0 };
        public static double[] ScaleBishops = { 0.9, 1.0 };
        public static double[] ScaleRooks = { 0.9, 1.0 };
        public static double[] ScaleQueens = { 0.9, 1.1 };
        public static double[] ScaleKingSafety = { 0.9, 0.6 };
        public static double[] ScaleThreats = { 0.10, 0.10 };
        public static double[] ScaleMobility = { 1.0, 1.0 };

        public const double ScorePawnOverloaded = -25;
        public const double ScorePawnUndermineSupported = 10;
        public const double ScorePawnUndermine = 30;
        public const double ScorePawnDoubled = -30;
        public const double ScorePawnDoubledDistant = -10;
        public const double ScorePawnSupport = 15;
        public const double ScoreIsolatedPawn = -20;
        public const double ScorePasser = 10 - ScoreIsolatedPawn;
        public const double ScoreNearlyPromotingPawn = (ValueQueen - (ValuePawn * 2)) / 2;
        public const double ScorePromotingPawn = ValueQueen - (ValuePawn * 2);

        public static readonly double[] ScoreKingRingAttack = { 7, 0 };
        public static readonly double[] ScoreKingOutterRingAttack = { 3, 0 };
        public const double ScoreKingInCorner = -20;
        public const double ScoreDefendedPieceNearKing = 30;
        public const double ScoreDefendedPieceNearKingCoeff = 10;
        public const double ScoreKingWithHomies = 15;
        public static readonly double[] ScoreKingNearOpenFile = { -10, 0 };

        public const double ScorePerCenterSquare = 10;
        public const double ScorePerSquare = 4;
        public const double ScoreUndevelopedPiece = -15;
        public const double ScoreSupportingPiece = 17;

        public static readonly double[] ScoreKnightOutpost = { 50, 40 };
        public static readonly double[] ScoreKnightUnderPawn = { 15, 2 };

        public static readonly double[] ScoreBishopOnCenterDiagonal = { 45, 5 };
        public static readonly double[] ScoreBishopNearCenterDiagonal = { 12, 5 };
        public static readonly double[] ScoreBishopOnKingDiagonal = { 15, 0 };
        public static readonly double[] ScoreBishopNearKingDiagonal = { 25, 0 };
        public static readonly double[] ScoreBishopOutpost = { 25, 20 };
        public static double ScoreBishopPair = 50;

        public static readonly double[] ScoreRookOpenFile = { 55, 20 };
        public static readonly double[] ScoreRookSemiOpenFile = { 25, 10 };
        public static double ScoreRookOn7th = 40;

        public const double ScoreQueenSquares = 2;

        public const ulong MobilityScoreOutter = 5;
        public const ulong MobilityScoreInner = 6;

        public const double CoefficientHanging = 1.4;
        public const double CoefficientPositiveTrade = 1.2;
        public const double CoefficientPinnedQueen = 0.4;

        public const double CoefficientPSQTCenterControl = 0.3;
        public const double CoefficientPSQTCenter = 0.7;
        public const double CoefficientPSQTPawns = 1.0;
        public const double CoefficientPSQTKnights = 1.0;
        public const double CoefficientPSQTFish = 1.0;

        public const double ScaleEndgame = 1.0;

        public const double CoefficientPSQTEKG = 2;
        public const double CoefficientEndgameKingThreats = 0.25;

        public const int PassedPawnPromotionDistanceFactor = 6;
        public const int PawnRelativeDistanceMultiplier = 2;

        public const double ScoreEndgamePawnDistancePenalty = -20;

        public static readonly int[] ScoreEGPawnPromotionDistance = { 0, 320, 160, 80, 50, 30, 40, 0 };

        /// <summary>
        /// Score given to the strong side for their king being however many squares away from the weak side's.
        /// We generally want to be 1 square away, since many endgames (KQvK, KRvK) require our king to be close
        /// to take away squares.
        /// </summary>
        public static readonly int[] ScoreEGKingDistance = { 0, 640, 320, 160, 80, 40, 20, -10 };

        /// <summary>
        /// Score given to the strong side for their rook or queen being close to the weak side's king.
        /// These are smaller than the king bonuses since having your king closer
        /// is more important for checkmating than your rook.
        /// </summary>
        public static readonly int[] ScoreEGSliderDistance = { 0, 130, 70, 30, 10, 5, 0, -50 };


        /// <summary>
        /// Index using [gamePhase] [PieceType - 1] [popcount(moves)]
        /// </summary>
        public static readonly int[][][] MobilityBonus = {
            new int[][] {
                new int[] { -62, -53, -12, -3, 3, 12, 21, 28, 37 },
                new int[] { -47, -20, 14, 29, 39, 53, 53, 60, 62, 69, 78, 83, 91, 96 },
                new int[] { -60, -24, 0 ,3, 4, 14, 20, 30, 41, 41 , 41, 45, 57, 58, 67 },
                new int[] { -29, -16, -8, -8, 18, 25, 23, 37, 41, 54, 65, 68, 69, 70, 70, 70, 71, 72, 74, 76, 90, 104, 105, 106, 112, 114, 114, 119 }
            },
            new int[][] {
                new int[] { -79, -57, -31, -17,  7,  13,  16,  21,  26 },
                new int[] { -59, -25,  -8,  12,  21,  40,  56,  58,  65,  72,  78,  87,  88,  98 },
                new int[] { -82, -15,  17 , 43,  72, 100, 102, 122, 133, 139, 153, 160, 165, 170, 175 },
                new int[] { -49, -29,  -8,  17,  39,  54,  59,  73,  76,  95,  95, 101, 124, 128, 132, 133, 136, 140,147, 149, 153, 169, 171, 171,178, 185, 187, 221 }
            },
        };


        /// <summary>
        /// Prints out all of the public fields in this class along with their values.
        /// </summary>
        internal static void PrintConstants()
        {
            var fields = typeof(EvaluationConstants).GetFields().Where(x => x.IsPublic).ToList();
            foreach (var term in fields)
            {
                Console.Write(term.Name + ": ");
                if (term.FieldType == typeof(int[]))
                {
                    int[] arr = (int[])term.GetValue(null);
                    Console.Write("[");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        Console.Write(arr[i] + (i != arr.Length - 1 ? ", " : string.Empty));
                    }
                    Console.Write("]");
                }
                else if (term.FieldType == typeof(double[])) {
                    double[] arr = (double[])term.GetValue(null);
                    Console.Write("[");
                    for (int i = 0; i < arr.Length; i++)
                    {
                        Console.Write(arr[i] + (i != arr.Length - 1 ? ", " : string.Empty));
                    }
                    Console.Write("]");
                }
                else
                {
                    Console.Write(term.GetValue(null));
                }

                Console.WriteLine();
            }
        }
    }
}
