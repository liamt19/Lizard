using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Util
{
    public static class CommonFENs
    {
        public const string ExchangeQGD = "rnbqkb1r/ppp2ppp/5n2/3p2B1/3P4/2N5/PP2PPPP/R2QKBNR b KQkq - 1 5";
        public const string QueensIndian = "rnbqkb1r/p1pp1ppp/1p2pn2/8/2PP4/5NP1/PP2PP1P/RNBQKB1R b KQkq - 0 4";
        public const string NimzoLarsenAttack = "r1bqkb1r/pppp1ppp/2n2n2/1B2p3/8/1P2P3/PBPP1PPP/RN1QK1NR b KQkq - 2 4";
        public const string Beefeater = "rnbqk1nr/pp1pp2p/6p1/2pP1p2/2P5/2P5/P3PPPP/R1BQKBNR w KQkq - 0 6";
        public const string ModernPterodactyl = "rnbqk1nr/pp1pppbp/6p1/2p5/3PP3/2N5/PPP2PPP/R1BQKBNR w KQkq - 0 4";
        public const string BenkoGambit = "rnbqkb1r/p2ppppp/5n2/1ppP4/2P5/8/PP2PPPP/RNBQKBNR w KQkq - 0 4";
        public const string ViennaMieses = "rnbq1rk1/ppp2ppp/3b4/4p3/8/2P2NP1/P1PP1PBP/R1BQ1RK1 b - - 4 8";
        public const string BerlinEndgame = "r1bk1b1r/ppp2ppp/2p5/4Pn2/8/2N2N2/PPP2PPP/R1B2RK1 b - - 1 9";
        public const string Jerome = "r1bqk1nr/pppp1Bpp/2n5/2b1p3/4P3/5N2/PPPP1PPP/RNBQK2R b KQkq - 0 4";
        public const string Traxler = "r1bqk2r/pppp1ppp/2n2n2/2b1p1N1/2B1P3/8/PPPP1PPP/RNBQK2R w KQkq - 6 5";
        public const string FriedLiver = "r1bqkb1r/ppp2Npp/2n5/3np3/2B5/8/PPPP1PPP/RNBQK2R b KQkq - 0 6";


        public const string BackRankInOne = "6k1/5ppp/8/8/8/8/8/R6K w - - 0 1";
        public const string BackRankInTwo = "6k1/5ppp/8/8/8/8/1r6/R6K w - - 0 1";
        public const string BackRankInThree = "6k1/5ppp/8/8/8/2r5/1r6/R6K w - - 0 1";
        public const string BackRankInFour = "6k1/5ppp/8/8/3r4/2r5/1r6/R6K w - - 0 1";
        public const string BackRankInFive = "6k1/5ppp/8/4r3/3r4/2r5/1r6/R6K w - - 0 1";

        /// <summary>
        /// 1. Qg8+ Rxg8 2. Nf7# 
        /// </summary>
        public const string SmotheredMateInTwo = "r6k/6pp/7N/8/2Q5/8/8/7K w - - 0 1";

        /// <summary>
        /// 1. Nf5 Rg8 2. Qg3 h6 3. Qg6 Ra8 4. Qxg7# 
        /// </summary>
        public const string NoChecksMateInFour = "r6k/6pp/3N4/8/8/2Q5/8/7K w - - 0 1";

        /// <summary>
        /// 1. Qg3 h6 3. Qg6 Ra8 4. Qxg7# 
        /// </summary>
        public const string NoChecksMateInThree = "6rk/6pp/8/5N2/8/2Q5/8/7K w - - 2 2";

        /// <summary>
        /// 1. Qg5 Ra8 2. Qh6# 
        /// </summary>
        public const string NoChecksMateInTwo = "6rk/6p1/7p/5N2/8/6Q1/8/7K w - - 0 3";

        /// <summary>
        /// Qc7+ Ke8 Nd6+ Kf8 Qf7#
        /// </summary>
        public const string Mate3 = "3k4/8/2Q5/1N6/8/8/8/1K6 w - - 0 1";

        /// <summary>
        /// Qxa1+ Qf1 Bh2+ Kxh2 Qxf1
        /// </summary>
        public const string RemoveDefenderQueen = "8/q4pk1/2R3pp/1Q1Pb3/1P6/7P/5PP1/R5K1 b - - 0 34";


        public const string SimpleRookEnd = "8/8/2k5/8/8/8/7K/7R w - - 0 1";

        public const string Pawns = "4k3/pppppppp/pppppppp/8/8/PPPPPPPP/PPPPPPPP/4K3 w - - 0 1";
        public const string Horses = "nnnnknnn/nnnnnnnn/8/8/8/8/NNNNNNNN/NNNNKNNN w - - 0 1";
        public const string TwoHorses = "8/8/8/8/8/8/8/KNN2nnk w - - 0 1";
        public const string LessHorses = "4k3/nnnnnnnn/8/8/8/8/NNNNNNNN/4K3 w - - 0 1";
        public const string IfHorsesInventedChess = "nnnnknnn/nnnnnnnn/nnnnnnnn/nnnnnnnn/NNNNNNNN/NNNNNNNN/NNNNNNNN/NNNNKNNN w - - 0 1";
        public const string Bishops = "bbbbkbbb/bbbbbbbb/8/8/8/8/BBBBBBBB/BBBBKBBB w - - 0 1";
        public const string Rooks = "rrrrkrrr/rrrrrrrr/8/8/8/8/RRRRRRRR/RRRRKRRR w - - 0 1";
        public const string Queens = "qqqqkqqq/qqqqqqqq/8/8/8/8/QQQQQQQQ/QQQQKQQQ w - - 0 1";
    }
}
