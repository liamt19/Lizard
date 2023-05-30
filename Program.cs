
//  Thanks C# 10!
global using LTChess;
global using LTChess.Core;
global using LTChess.Data;
global using LTChess.Magic;
global using LTChess.Search;
global using LTChess.Transposition;
global using LTChess.Util;

global using static LTChess.Core.Bitboard;
global using static LTChess.Core.MoveGenerator;
global using static LTChess.Core.Position;
global using static LTChess.Core.UCI;
global using static LTChess.Data.PrecomputedData;
global using static LTChess.Data.RunOptions;
global using static LTChess.Data.Squares;
global using static LTChess.Magic.MagicBitboards;
global using static LTChess.Search.SearchStatistics;
global using static LTChess.Util.Utilities;
global using static LTChess.Util.CommonFENs;
global using static LTChess.Util.PositionUtilities;
global using static LTChess.Search.SearchConstants;

global using Fish = Stockfish.Stockfish;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace LTChess
{
    public static class Program
    {
        public static Position p = new Position();
        public static SearchInformation info;

        public static void Main()
        {
            InitializeAll();

            DoInputLoop();

            Console.WriteLine("Done");
            Console.ReadLine();
        }

        public static void InitializeAll()
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif

            MagicBitboards.Initialize();
            PrecomputedData.Initialize();
            Zobrist.Initialize();
            EvaluationTable.Initialize();
            TranspositionTable.Initialize();
            PSQT.Initialize();

#if DEBUG
            Log("InitializeAll done in " + sw.Elapsed.TotalSeconds + " s");
            sw.Stop();
#endif
        }

        public static void DoInputLoop()
        {
            Log("LTChess version " + EngineBuildVersion + " - " + EngineTagLine + "\r\n");

            string input;
            while (true)
            {
                input = Console.ReadLine();
                if (input == null || input.Length == 0)
                {
                    continue;
                }
                else if (input.EqualsIgnoreCase("uci"))
                {
                    UCI uci = new UCI();
                    uci.Run();
                }
                else if (input.StartsWithIgnoreCase("position fen "))
                {
                    p = new Position(input.Substring(13));
                }
                else if (input.StartsWithIgnoreCase("go perft "))
                {
                    int depth = int.Parse(input.Substring(9));
                    Task.Run(() => DoPerftDivide(depth, false));
                }
                else if (input.StartsWithIgnoreCase("move "))
                {
                    string move = input.Substring(5).ToLower();

                    p.TryMakeMove(move);

                    Log(p.ToString());
                }
                else if (input.StartsWithIgnoreCase("go depth"))
                {
                    info = new SearchInformation(p);
                    if (input.Length > 9 && int.TryParse(input.Substring(9), out int selDepth))
                    {
                        info.MaxDepth = selDepth;
                    }

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + "= " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.StartsWithIgnoreCase("go time"))
                {
                    info = new SearchInformation(p, MAX_DEPTH);
                    bool timeInMS = false;

                    if (input.EndsWith("ms"))
                    {
                        timeInMS = true;
                        input = input.Substring(0, input.Length - 2);
                    }
                    else if (input.EndsWith("s"))
                    {
                        input = input.Substring(0, input.Length - 1);
                    }

                    if (input.Length > 8 && long.TryParse(input.Substring(8), out long searchTime))
                    {
                        info.MaxSearchTime = searchTime;
                        if (!timeInMS)
                        {
                            info.MaxSearchTime *= 1000;
                        }
                    }

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + "= " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.StartsWithIgnoreCase("best"))
                {
                    info = new SearchInformation(p, DefaultSearchDepth);
                    if (input.Length > 5 && int.TryParse(input.Substring(5), out int selDepth))
                    {
                        info.MaxDepth = selDepth;
                    }

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + "= " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.StartsWithIgnoreCase("play"))
                {
                    info = new SearchInformation(p, DefaultSearchDepth);
                    if (input.Length > 5 && int.TryParse(input.Substring(5), out int selDepth))
                    {
                        info.MaxDepth = selDepth;
                    }

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + "= " + FormatMoveScore(info.BestScore));
                        p.MakeMove(info.BestMove);
                        Log(p.ToString());
                    });
                }
                else if (input.StartsWithIgnoreCase("respond"))
                {
                    string move = input.ToLower().Substring(7).Trim();
                    Span<Move> list = new Move[NORMAL_CAPACITY];
                    int size = p.GenAllLegalMoves(list);
                    bool failed = true;
                    for (int i = 0; i < size; i++)
                    {
                        Move m = list[i];
                        if (m.ToString(p).ToLower().Equals(move) || m.ToString().ToLower().Equals(move))
                        {
                            p.MakeMove(m);
                            info = new SearchInformation(p, DefaultSearchDepth);

                            SimpleSearch.StartSearching(ref info);

                            p.MakeMove(info.BestMove);
                            Log(p.ToString());
                            failed = false;
                            break;
                        }
                    }
                    if (failed)
                    {
                        Log("No move '" + move + "' found, try one of the following: ");
                        Log(list.Stringify(p) + "\r\n" + list.Stringify());
                    }

                }
                else if (input.StartsWithIgnoreCase("undo"))
                {
                    p.UnmakeMove();
                }
                else if (input.StartsWithIgnoreCase("bench "))
                {
                    int depth = int.Parse(input.Substring(6));
                    FishBench.Go(depth);
                }
                else if (input.EqualsIgnoreCase("d"))
                {
                    Log(p.ToString());
                }
                else if (input.ContainsIgnoreCase("time perft"))
                {
                    int toDepth = int.Parse(input.Substring(11));
                    TimePerftToDepth(toDepth);
                }
                else if (input.ContainsIgnoreCase("eval"))
                {
                    Evaluation.Evaluate(p.bb, p.ToMove, true);
                }
                else if (input.StartsWithIgnoreCase("stop"))
                {
                    if (info != null)
                    {
                        info.StopSearching = true;
                    }

                }
                else if (input.ContainsIgnoreCase("searchinfo"))
                {
                    PrintSearchInfo();
                }
                else if (input.StartsWithIgnoreCase("quit") || input.StartsWithIgnoreCase("exit"))
                {
                    break;
                }
                else
                {
                    Log("Unknown token '" + input + "'");
                }
            }
        }

        public static double TimePerft(int depth)
        {
            Stopwatch sw = Stopwatch.StartNew();
            ulong res = p.Perft(depth);
            sw.Stop();
            Log("Nodes searched:  " + res + " in " + sw.Elapsed.TotalSeconds + " s" + "\r\n");
            return sw.Elapsed.TotalSeconds;
        }

        public static void TimePerftToDepth(int depth)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 1; i <= depth; i++)
            {
                sw.Restart();
                ulong res = p.Perft(i);
                sw.Stop();
                double r = Math.Round(sw.Elapsed.TotalSeconds, 4);
                Log("Depth " + i + " - Time: " + Math.Round(sw.Elapsed.TotalSeconds, 4) + " s");
            }
        }

        public static void TimePerftNodesToDepth(int depth)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 1; i <= depth; i++)
            {
                sw.Restart();
                List<PerftNode> nodes = p.PerftDivide(i);
                sw.Stop();
                Log("Depth " + i + " - Time: " + Math.Round(sw.Elapsed.TotalSeconds, 4) + " s");
            }
        }

        public static void RunCpuUsage()
        {
            Thread.Sleep(2000);

            //TimePerft(5);
            //Bench.Go();
            TimePerftToDepth(6);


            Thread.Sleep(1000);
            Environment.Exit(0);
        }

        public static void RunInstrumentation()
        {
            p.Perft(5);
            Environment.Exit(0);
        }

        public static void RunEval(int toDepth = DefaultSearchDepth, string fen = InitialFEN)
        {
            p = new Position(fen);
            info = new SearchInformation(p, toDepth);

            //Thread.Sleep(1000);

            SimpleSearch.StartSearching(ref info);

            Environment.Exit(0);
        }

        public static void RunBenchIterations(int iter, int benchDepth = 4)
        {
            double sum = 0;
            for (int i = 0; i < iter; i++)
            {
                sum += FishBench.Go(benchDepth);
            }

            Log("Total: " + iter + " iters in " + sum + " seconds = " + (sum / iter) + " seconds / iter");
        }

        public static void DoMateSearch(int toDepth = DefaultSearchDepth)
        {
            Mate.Search(p, toDepth);
        }

        public static void DoNegaMaxIterative(int toDepth = DefaultSearchDepth)
        {
            info = new SearchInformation(p, toDepth);

            SimpleSearch.StartSearching(ref info);
        }

        public static void DoNegaMaxAtDepth(int toDepth = DefaultSearchDepth)
        {
            info = new SearchInformation(p, toDepth);

            SimpleSearch.Deepen(ref info);
        }

        public static void DoHashPerft(int depth)
        {
            ulong res = 0;
            HashPerft hp = new HashPerft(p, 8, depth);
            Stopwatch sw = Stopwatch.StartNew();
            List<PerftNode> nodes = hp.PerftDivide(depth);
            sw.Stop();

            foreach (PerftNode node in nodes)
            {
                Log(node.root + ": " + node.number);
                res += node.number;
            }

            Log("\r\n" + hp.TableHits + " hits" + ", " + hp.TableMisses + " misses and " + hp.TableSaves + " saves");
            Log("\r\nNodes searched:  " + res + " in " + sw.Elapsed.TotalSeconds + " s" + "\r\n");
        }

        public static void DoPerftDivide(int depth, bool sortAlphabetical = true)
        {
            ulong res = 0;
            Stopwatch sw = Stopwatch.StartNew();
            List<PerftNode> nodes = p.PerftDivide(depth);
            sw.Stop();

            foreach (PerftNode node in nodes)
            {
                if (!sortAlphabetical)
                {
                    Log(node.root + ": " + node.number);
                }

                res += node.number;
            }

            if (sortAlphabetical)
            {
                nodes.OrderBy(x => x.root).ToList().ForEach(node => Log(node.root + ": " + node.number));
            }

            Log("\r\nNodes searched:  " + res + " in " + sw.Elapsed.TotalSeconds + " s" + "\r\n");
        }

        public static void PrintSearchInfo()
        {
            TranspositionTable.PrintStatus();
            Log("\r\n");
            EvaluationTable.PrintStatus();
            Log("\r\n");
            SearchStatistics.PrintStatistics();
        }

        public static void PrintMoves()
        {
            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = p.GenAllLegalMoves(list);
            Log("Legal (" + size + "): " + list.Stringify(p));
        }

        public static void PrintPseudoMoves()
        {
            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = p.GenAllPseudoMoves(list);
            Log("Pseudo (" + size + "): " + list.Stringify(p));
        }


    }

}