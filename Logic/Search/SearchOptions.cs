﻿namespace Lizard.Logic.Search
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
        public const bool UseRFP = true;
        public const bool UseProbCut = true;


        public static int SEMinDepth = 5;
        public static int SENumerator = 11;
        public static int SEDoubleMargin = 24;
        public static int SETripleMargin = 96;
        public static int SETripleCapSub = 75;
        public static int SEDepthAdj = -1;

        public static int NMPMinDepth = 6;
        public static int NMPBaseRed = 4;
        public static int NMPDepthDiv = 4;
        public static int NMPEvalDiv = 181;
        public static int NMPEvalMin = 2;

        public static int RFPMaxDepth = 6;
        public static int RFPMargin = 47;

        public static int ProbcutBeta = 256;
        public static int ProbcutBetaImp = 101;

        public static int NMFutileBase = 512;
        public static int NMFutilePVCoeff = 1024;
        public static int NMFutileImpCoeff = 1024;
        public static int NMFutileHistCoeff = 1024;
        public static int NMFutMarginB = 200;
        public static int NMFutMarginM = 80;
        public static int NMFutMarginDiv = 150;
        public static int ShallowSEEMargin = 216;
        public static int ShallowMaxDepth = 9;

        public static int LMRQuietDiv = 12288;
        public static int LMRCaptureDiv = 10288;
        public static int LMRExtMargin = 128;

        public static int QSFutileMargin = 183;
        public static int QSSeeMargin = 81;

        public static int OrderingCheckBonus = 9546;
        public static int OrderingVictimMult = 12;

        public static int IIRMinDepth = 3;
        public static int AspWindow = 12;

        public static int StatBonusMult = 184;
        public static int StatBonusSub = 80;
        public static int StatBonusMax = 1667;

        public static int StatMalusMult = 611;
        public static int StatMalusSub = 111;
        public static int StatMalusMax = 1663;

        public static int SEEValuePawn = 105;
        public static int SEEValueKnight = 900;
        public static int SEEValueBishop = 1054;
        public static int SEEValueRook = 1332;
        public static int SEEValueQueen = 2300;

        public static int ValuePawn = 171;
        public static int ValueKnight = 794;
        public static int ValueBishop = 943;
        public static int ValueRook = 1620;
        public static int ValueQueen = 2994;
    }
}
