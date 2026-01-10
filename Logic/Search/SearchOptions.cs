namespace Lizard.Logic.Search
{
    public static class SearchOptions
    {
        /// <summary>
        /// This number of threads will be used during searches.
        /// <para></para>
        /// For values above 1, the engine will create extra threads to increase the amount of nodes that can be looked at.
        /// Do note that a decent amount of the nodes that are looked at by secondary threads won't influence anything,
        /// but higher numbers of threads tends to correlate with better playing strength.
        /// </summary>
        public static int Threads = 1;


        /// <summary>
        /// Calls to search will display this amount of principal variation lines. 
        /// <para></para>
        /// Ordinarily engines only search for the 1 "best" move, but with MultiPV values 
        /// above 1 this will also display the 2nd best move, the 3rd best, etc.
        /// </summary>
        public static int MultiPV = 1;


        /// <summary>
        /// The size in megabytes of the transposition table.
        /// </summary>
        public static int Hash = 32;


        public static bool UCI_Chess960 = false;
        public static bool UCI_ShowWDL = false;
        public static bool UCI_PrettyPrint = true;


        public const int CorrectionScale = 1024;
        public const int CorrectionGrain = 256;
        public const short CorrectionMax = CorrectionGrain * 64;


        public const bool ShallowPruning = true;
        public const bool UseSingularExtensions = true;
        public const bool UseNMP = true;
        public const bool UseRazoring = true;
        public const bool UseRFP = true;
        public const bool UseProbCut = true;


        public static int QuietOrderMin = 101;
        public static int QuietOrderMax = 192;
        public static int QuietOrderMult = 153;

        public static int SEMinDepth = 5;
        public static int SENumerator = 11;
        public static int SEDoubleMargin = 21;
        public static int SETripleMargin = 101;
        public static int SETripleCapSub = 73;
        public static int SEDepthAdj = -1;

        public static int NMPMinDepth = 6;
        public static int NMPBaseRed = 4;
        public static int NMPDepthDiv = 4;
        public static int NMPEvalDiv = 151;
        public static int NMPEvalMin = 2;

        public static int RazoringMaxDepth = 4;
        public static int RazoringMult = 292;

        public static int RFPMaxDepth = 6;
        public static int RFPMargin = 51;

        public static int ProbcutBeta = 252;
        public static int ProbcutBetaImp = 97;

        public static int NMFutileBase = 58;
        public static int NMFutilePVCoeff = 136;
        public static int NMFutileImpCoeff = 132;
        public static int NMFutileHistCoeff = 128;
        public static int NMFutMarginB = 163;
        public static int NMFutMarginM = 81;
        public static int NMFutMarginDiv = 129;
        public static int ShallowSEEMargin = 81;
        public static int ShallowMaxDepth = 9;

        public static int LMRNotImpCoeff = 119;
        public static int LMRCutNodeCoeff = 280;
        public static int LMRTTPVCoeff = 124;
        public static int LMRKillerCoeff = 129;

        public static int LMRHist = 189;
        public static int LMRHistSS1 = 268;
        public static int LMRHistSS2 = 128;
        public static int LMRHistSS4 = 127;

        public static int PawnCorrCoeff = 129;
        public static int NonPawnCorrCoeff = 70;
        public static int CorrDivisor = 3136;

        public static int NMOrderingMH = 419;
        public static int NMOrderingSS1 = 491;
        public static int NMOrderingSS2 = 258;
        public static int NMOrderingSS4 = 255;
        public static int NMOrderingSS6 = 246;

        public static int QSOrderingMH = 498;
        public static int QSOrderingSS1 = 500;
        public static int QSOrderingSS2 = 258;
        public static int QSOrderingSS4 = 247;
        public static int QSOrderingSS6 = 261;

        public static int OrderingEnPriseMult = 520;

        public static int LMRQuietDiv = 15234;
        public static int LMRCaptureDiv = 9229;

        public static int DeeperMargin = 45;

        public static int QSFutileMargin = 204;
        public static int QSSeeMargin = 75;

        public static int CheckBonus = 9816;
        public static int MVVMult = 363;

        public static int IIRMinDepth = 3;
        public static int AspWindow = 12;

        public static int StatBonusMult = 192;
        public static int StatBonusSub = 91;
        public static int StatBonusMax = 1735;

        public static int StatPenaltyMult = 655;
        public static int StatPenaltySub = 106;
        public static int StatPenaltyMax = 1267;

        public static int LMRBonusMult = 182;
        public static int LMRBonusSub = 83;
        public static int LMRBonusMax = 1703;

        public static int LMRPenaltyMult = 173;
        public static int LMRPenaltySub = 82;
        public static int LMRPenaltyMax = 1569;

        public const int SEEValuePawn = 105;
        public const int SEEValueKnight = 300;
        public const int SEEValueBishop = 315;
        public const int SEEValueRook = 535;
        public const int SEEValueQueen = 970;

        public static int ValuePawn = 161;
        public static int ValueKnight = 737;
        public static int ValueBishop = 901;
        public static int ValueRook = 1518;
        public static int ValueQueen = 3200;
    }
}
