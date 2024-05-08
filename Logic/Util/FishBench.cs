namespace Lizard.Logic.Util
{
    //  SkipStaticConstructor since this introduces some unnecessary strings
    [SkipStaticConstructor]
    public static class FishBench
    {
        private static Dictionary<string, ulong[]> FENDict = new Dictionary<string, ulong[]>()
        {
            { "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", new ulong[] {197281, 4865609} },
            { "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 10", new ulong[] {4085603, 193690690} },
            { "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 11", new ulong[] {43238, 674624} },
            { "4rrk1/pp1n3p/3q2pQ/2p1pb2/2PP4/2P3N1/P2B2PP/4RRK1 b - - 7 19", new ulong[] {2038080, 72228466} },
            { "rq3rk1/ppp2ppp/1bnpN3/3N2B1/4P3/7P/PPPQ1PP1/2KR3R b - - 0 14", new ulong[] {1651350, 45187461} },
            { "r1bq1r1k/1pp1n1pp/1p1p4/4p2Q/4PpP1/1BNP4/PPP2P1P/3R1RK1 b - g3 0 14", new ulong[] {1267861, 40266111} },
            { "r3r1k1/2p2ppp/p1p1bn2/8/1q2P3/2NPQN2/PPP3PP/R4RK1 b - - 2 15", new ulong[] {3977950, 172436031} },
            { "r1bbk1nr/pp3p1p/2n5/1N4p1/2Np1B2/8/PPP2PPP/2KR1B1R w kq - 0 13", new ulong[] {1313494, 50688363} },
            { "r1bq1rk1/ppp1nppp/4n3/3p3Q/3P4/1BP1B3/PP1N2PP/R4RK1 w - - 1 16", new ulong[] {1577355, 72185472} },
            { "4r1k1/r1q2ppp/ppp2n2/4P3/5Rb1/1N1BQ3/PPP3PP/R5K1 w - - 1 17", new ulong[] {4189865, 210468700} },
            { "2rqkb1r/ppp2p2/2npb1p1/1N1Nn2p/2P1PP2/8/PP2B1PP/R1BQK2R b KQ - 0 11", new ulong[] {2001581, 68841702} },
            { "r1bq1r1k/b1p1npp1/p2p3p/1p6/3PP3/1B2NN2/PP3PPP/R2Q1RK1 w - - 1 16", new ulong[] {1342987, 51439641} },
            { "3r1rk1/p5pp/bpp1pp2/8/q1PP1P2/b3P3/P2NQRPP/1R2B1K1 b - - 6 22", new ulong[] {1307975, 49893295} },
            { "r1q2rk1/2p1bppp/2Pp4/p6b/Q1PNp3/4B3/PP1R1PPP/2K4R w - - 2 18", new ulong[] {1415967, 55479889} },
            { "4k2r/1pb2ppp/1p2p3/1R1p4/3P4/2r1PN2/P4PPP/1R4K1 b - - 3 22", new ulong[] {629156, 18452493} },
            { "3q2k1/pb3p1p/4pbp1/2r5/PpN2N2/1P2P2P/5PP1/Q2R2K1 b - - 4 26", new ulong[] {3982247, 177035052} },
            { "6k1/6p1/6Pp/ppp5/3pn2P/1P3K2/1PP2P2/3N4 b - - 0 1", new ulong[] {22537, 289580} },
            { "3b4/5kp1/1p1p1p1p/pP1PpP1P/P1P1P3/3KN3/8/8 w - - 0 1", new ulong[] {8212, 102373} },
            { "2K5/p7/7P/5pR1/8/5k2/r7/8 w - - 4 3", new ulong[] {79278, 1348405} },
            { "8/6pk/1p6/8/PP3p1p/5P2/4KP1q/3Q4 w - - 0 1", new ulong[] {70042, 1345987} },
            { "7k/3p2pp/4q3/8/4Q3/5Kp1/P6b/8 w - - 0 1", new ulong[] {364175, 6947406} },
            { "8/2p5/8/2kPKp1p/2p4P/2P5/3P4/8 w - - 0 1", new ulong[] {2021, 18397} },
            { "8/1p3pp1/7p/5P1P/2k3P1/8/2K2P2/8 w - - 0 1", new ulong[] {10807, 100735} },
            { "8/pp2r1k1/2p1p3/3pP2p/1P1P1P1P/P5KR/8/8 w - - 0 1", new ulong[] {30953, 487838} },
            { "8/3p4/p1bk3p/Pp6/1Kp1PpPp/2P2P1P/2P5/5B2 b - - 0 1", new ulong[] {6650, 84037} },
            { "5k2/7R/4P2p/5K2/p1r2P1p/8/8/8 b - - 0 1", new ulong[] {41391, 589437} },
            { "6k1/6p1/P6p/r1N5/5p2/7P/1b3PP1/4R1K1 w - - 0 1", new ulong[] {340174, 8065991} },
            { "1r3k2/4q3/2Pp3b/3Bp3/2Q2p2/1p1P2P1/1P2KP2/3N4 w - - 0 1", new ulong[] {654142, 19229707} },
            { "6k1/4pp1p/3p2p1/P1pPb3/R7/1r2P1PP/3B1P2/6K1 w - - 0 1", new ulong[] {425923, 8984804} },
            { "8/3p3B/5p2/5P2/p7/PP5b/k7/6K1 w - - 0 1", new ulong[] {7812, 73669} },
            { "5rk1/q6p/2p3bR/1pPp1rP1/1P1Pp3/P3B1Q1/1K3P2/R7 w - - 93 90", new ulong[] {1469918, 52898139} },
            { "4rrk1/1p1nq3/p7/2p1P1pp/3P2bp/3Q1Bn1/PPPB4/1K2R1NR w - - 40 21", new ulong[] {3311502, 150850085} },
            { "r3k2r/3nnpbp/q2pp1p1/p7/Pp1PPPP1/4BNN1/1P5P/R2Q1RK1 w kq - 0 16", new ulong[] {2428006, 90133805} },
            { "3Qb1k1/1r2ppb1/pN1n2q1/Pp1Pp1Pr/4P2p/4BP2/4B1R1/1R5K b - - 11 40", new ulong[] {1923471, 61309107} },
            { "4k3/3q1r2/1N2r1b1/3ppN2/2nPP3/1B1R2n1/2R1Q3/3K4 w - - 5 1", new ulong[] {2529287, 101660587} },
        };

        /// <summary>
        /// Runs a perft command on each of the positions in BenchFENs, and verifies that the number of nodes
		/// returned by our perft command is the same as the number that Stockfish 14 reported.
        /// </summary>
        public static bool Go(int depth = 4)
        {
            if (depth != 4 && depth != 5)
            {
                Log("FishBench 'go' commands must have a depth of 4 or 5!");
                return false;
            }

            Position pos = new Position(InitialFEN, false);

            bool nodesCorrect = true;
            ulong total = 0;

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var item in FENDict)
            {
                string fen = item.Key;
                ulong correctNodes = item.Value[depth - 4];

                pos.LoadFromFEN(fen);
                ulong ourNodes = pos.Perft(depth);
                if (ourNodes != correctNodes)
                {
                    Log('[' + fen + ']' + ": Expected " + correctNodes + " nodes but got " + ourNodes + " nodes instead!");
                    nodesCorrect = false;
                }

                total += ourNodes;
            }

            Log("\r\nNodes searched:  " + total + " in " + sw.Elapsed.TotalSeconds + " s (" + ((int)(total / sw.Elapsed.TotalSeconds)).ToString("N0") + " nps)" + "\r\n");
            return nodesCorrect;
        }
    }
}
