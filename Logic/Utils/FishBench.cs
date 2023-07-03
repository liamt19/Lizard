using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using LTChess.Properties;

namespace LTChess.Util
{
    public static class FishBench
    {
        private static Position p;

		public static Dictionary<string, ulong> FENDepths4 = new Dictionary<string, ulong>();
		public static Dictionary<string, ulong> FENDepths5 = new Dictionary<string, ulong>();

		public static string[] BenchFENs = new string[] {
			"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
			"r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 10",
			"8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 11",
			"4rrk1/pp1n3p/3q2pQ/2p1pb2/2PP4/2P3N1/P2B2PP/4RRK1 b - - 7 19",
			"rq3rk1/ppp2ppp/1bnpN3/3N2B1/4P3/7P/PPPQ1PP1/2KR3R b - - 0 14",
			"r1bq1r1k/1pp1n1pp/1p1p4/4p2Q/4PpP1/1BNP4/PPP2P1P/3R1RK1 b - g3 0 14",
			"r3r1k1/2p2ppp/p1p1bn2/8/1q2P3/2NPQN2/PPP3PP/R4RK1 b - - 2 15",
			"r1bbk1nr/pp3p1p/2n5/1N4p1/2Np1B2/8/PPP2PPP/2KR1B1R w kq - 0 13",
			"r1bq1rk1/ppp1nppp/4n3/3p3Q/3P4/1BP1B3/PP1N2PP/R4RK1 w - - 1 16",
			"4r1k1/r1q2ppp/ppp2n2/4P3/5Rb1/1N1BQ3/PPP3PP/R5K1 w - - 1 17",
			"2rqkb1r/ppp2p2/2npb1p1/1N1Nn2p/2P1PP2/8/PP2B1PP/R1BQK2R b KQ - 0 11",
			"r1bq1r1k/b1p1npp1/p2p3p/1p6/3PP3/1B2NN2/PP3PPP/R2Q1RK1 w - - 1 16",
			"3r1rk1/p5pp/bpp1pp2/8/q1PP1P2/b3P3/P2NQRPP/1R2B1K1 b - - 6 22",
			"r1q2rk1/2p1bppp/2Pp4/p6b/Q1PNp3/4B3/PP1R1PPP/2K4R w - - 2 18",
			"4k2r/1pb2ppp/1p2p3/1R1p4/3P4/2r1PN2/P4PPP/1R4K1 b - - 3 22",
			"3q2k1/pb3p1p/4pbp1/2r5/PpN2N2/1P2P2P/5PP1/Q2R2K1 b - - 4 26",
			"6k1/6p1/6Pp/ppp5/3pn2P/1P3K2/1PP2P2/3N4 b - - 0 1",
			"3b4/5kp1/1p1p1p1p/pP1PpP1P/P1P1P3/3KN3/8/8 w - - 0 1",
			"2K5/p7/7P/5pR1/8/5k2/r7/8 w - - 4 3",
			"8/6pk/1p6/8/PP3p1p/5P2/4KP1q/3Q4 w - - 0 1",
			"7k/3p2pp/4q3/8/4Q3/5Kp1/P6b/8 w - - 0 1",
			"8/2p5/8/2kPKp1p/2p4P/2P5/3P4/8 w - - 0 1",
			"8/1p3pp1/7p/5P1P/2k3P1/8/2K2P2/8 w - - 0 1",
			"8/pp2r1k1/2p1p3/3pP2p/1P1P1P1P/P5KR/8/8 w - - 0 1",
			"8/3p4/p1bk3p/Pp6/1Kp1PpPp/2P2P1P/2P5/5B2 b - - 0 1",
			"5k2/7R/4P2p/5K2/p1r2P1p/8/8/8 b - - 0 1",
			"6k1/6p1/P6p/r1N5/5p2/7P/1b3PP1/4R1K1 w - - 0 1",
			"1r3k2/4q3/2Pp3b/3Bp3/2Q2p2/1p1P2P1/1P2KP2/3N4 w - - 0 1",
			"6k1/4pp1p/3p2p1/P1pPb3/R7/1r2P1PP/3B1P2/6K1 w - - 0 1",
			"8/3p3B/5p2/5P2/p7/PP5b/k7/6K1 w - - 0 1",
			"5rk1/q6p/2p3bR/1pPp1rP1/1P1Pp3/P3B1Q1/1K3P2/R7 w - - 93 90",
			"4rrk1/1p1nq3/p7/2p1P1pp/3P2bp/3Q1Bn1/PPPB4/1K2R1NR w - - 40 21",
			"r3k2r/3nnpbp/q2pp1p1/p7/Pp1PPPP1/4BNN1/1P5P/R2Q1RK1 w kq - 0 16",
			"3Qb1k1/1r2ppb1/pN1n2q1/Pp1Pp1Pr/4P2p/4BP2/4B1R1/1R5K b - - 11 40",
			"4k3/3q1r2/1N2r1b1/3ppN2/2nPP3/1B1R2n1/2R1Q3/3K4 w - - 5 1"
		};

		static FishBench()
        {
			Load(Resources.sf14bench_perft4, 4);
			Load(Resources.sf14bench_perft5, 5);
		}

		public static void Load(string str, int Depth)
        {

			string[] lines = str.Split(Environment.NewLine);

			string lastFen = "";

			for (int i = 0; i < lines.Length; i++)
            {
				string line = lines[i];
				if (line.StartsWith("Position"))
                {
					lastFen = line.Substring(line.IndexOf('(') + 1).Replace(")", string.Empty);
				}
				else if (line.StartsWith("Nodes"))
                {
					ulong n = ulong.Parse(line.Substring(15));

					if (Depth == 4)
                    {
						FENDepths4.Add(lastFen, n);
					}
					else if (Depth == 5)
                    {
						FENDepths5.Add(lastFen, n);
					}
				}
            }
		}

        /// <summary>
        /// Runs a perft command on each of the positions in BenchFENs, and verifies that the number of nodes
		/// returned by our perft command is the same as the number that Stockfish 14 reported.
        /// </summary>
        public static double Go(int Depth = 4)
        {
			Dictionary<string, ulong> dict = FENDepths4;

			if (Depth == 5)
            {
				dict = FENDepths5;
			}

			Stopwatch sw = Stopwatch.StartNew();
			int i = 1;
			foreach (var item in dict)
            {
				string fen = item.Key;
				ulong correctNodes = item.Value;
				Console.Title = "Progress: " + i.ToString();
				i++;

				p = new Position(fen);
				ulong ourNodes = p.Perft(Depth);
				if (ourNodes != correctNodes)
                {
					Log('[' + fen + ']' + ": Expected " + correctNodes + " nodes but got " + ourNodes + " nodes instead!");
                }
            }

			Log("Done in " + sw.Elapsed.TotalSeconds + " s!");
			return sw.Elapsed.TotalSeconds;
		}

		public static double Simple(int Depth)
        {
			Stopwatch sw = Stopwatch.StartNew();
			for (int i = 0; i < BenchFENs.Length; i++)
			{
				p.LoadFromFEN(BenchFENs[i]);
				p.Perft(Depth);
			}
			Log("Done in " + sw.Elapsed.TotalSeconds + " s!");
			return sw.Elapsed.TotalSeconds;
		}

    }
}
