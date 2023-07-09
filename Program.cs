
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
global using static LTChess.Data.CheckInfo;
global using static LTChess.Data.PrecomputedData;
global using static LTChess.Data.RunOptions;
global using static LTChess.Data.Squares;
global using static LTChess.Magic.MagicBitboards;
global using static LTChess.Search.SearchConstants;
global using static LTChess.Search.SearchStatistics;
global using static LTChess.Util.Utilities;
global using static LTChess.Util.PositionUtilities;


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
using System.Drawing;
using System.Formats.Tar;
using System.Threading;
using LTChess.Utils;

namespace LTChess
{
    public static class Program
    {
        private static Position p = new Position();
        private static SearchInformation info;

        public static void Main()
        {
            InitializeAll();
            info = new SearchInformation(p);

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
                else if (input.StartsWithIgnoreCase("go perftp "))
                {
                    int depth = int.Parse(input.Substring(10));
                    Task.Run(() => DoPerftDivideParallel(depth, false));
                }
                else if (input.StartsWithIgnoreCase("go depth"))
                {
                    info = new SearchInformation(p, DefaultSearchDepth);
                    if (input.Length > 9 && int.TryParse(input.Substring(9), out int selDepth))
                    {
                        info.MaxDepth = selDepth;
                    }

                    info.MaxSearchTime = SearchConstants.MaxSearchTime;

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.StartsWithIgnoreCase("go time"))
                {
                    info = new SearchInformation(p, MaxDepth);
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
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.EqualsIgnoreCase("go") || input.EqualsIgnoreCase("go infinite"))
                {
                    info = new SearchInformation(p, MaxDepth);
                    info.MaxSearchTime = SearchConstants.MaxSearchTime;

                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
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
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
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
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
                        p.MakeMove(info.BestMove);
                        Log(p.ToString());
                    });
                }
                else if (input.StartsWithIgnoreCase("respond"))
                {
                    string move = input.ToLower().Substring(7).Trim();
                    Span<Move> list = new Move[NormalListCapacity];
                    int size = p.GenAllLegalMovesTogether(list);
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
                else if (input.StartsWithIgnoreCase("puzzle"))
                {
                    p = new Position(input.Substring(7));
                    info = new SearchInformation(p, 12, MaxSearchTime);
                    Task.Run(() =>
                    {
                        SimpleSearch.StartSearching(ref info);
                        Log("Line: " + info.GetPVString() + " = " + FormatMoveScore(info.BestScore));
                    });
                }
                else if (input.StartsWithIgnoreCase("move "))
                {
                    string move = input.Substring(5).ToLower();

                    p.TryMakeMove(move);

                    Log(p.ToString());
                }
                else if (input.StartsWithIgnoreCase("undo"))
                {
                    p.UnmakeMove();
                }
                else if (input.StartsWithIgnoreCase("stop"))
                {
                    info.StopSearching = true;

                }
                else if (input.ContainsIgnoreCase("eval"))
                {
                    info.GetEvaluation(p, p.ToMove, true);
                }
                else if (input.EqualsIgnoreCase("d"))
                {
                    Log(p.ToString());
                }
                else if (input.ContainsIgnoreCase("searchinfo"))
                {
                    PrintSearchInfo();
                }
                else if (input.ContainsIgnoreCase("time perft"))
                {
                    int toDepth = int.Parse(input.Substring(11));
                    TimePerftToDepth(toDepth);
                }
                else if (input.StartsWithIgnoreCase("bench "))
                {
                    int depth = int.Parse(input.Substring(6));
                    FishBench.Go(depth);
                }
                else if (input.StartsWithIgnoreCase("quit") || input.StartsWithIgnoreCase("exit"))
                {
                    break;
                }
                else
                {
                    //  You can just copy paste in a FEN string rather than typing "position fen" before it.
                    if (input.Where(x => x == '/').Count() != 7 || !p.LoadFromFEN(input.Trim()))
                    {
                        Log("Unknown token '" + input + "'");
                    }
                }
            }
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

        public static void DoPerftDivideParallel(int depth, bool sortAlphabetical = true)
        {
            ulong res = 0;
            Stopwatch sw = Stopwatch.StartNew();
            List<PerftNode> nodes = p.PerftDivideParallel(depth);
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
            Log(info.ToString());
            Log("\r\n");
            TranspositionTable.PrintStatus();
            Log("\r\n");
            EvaluationTable.PrintStatus();
            Log("\r\n");
            SearchStatistics.PrintStatistics();
        }

        public static void PrintMoves()
        {
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = p.GenAllLegalMovesTogether(list);
            Log("Legal (" + size + "): " + list.Stringify(p));
        }

    }

}