
#define JB
#undef JB

//  Thanks C# 10!
global using System.Runtime.CompilerServices;
global using System.Diagnostics;

global using LTChess.Logic.Core;
global using LTChess.Logic.Data;
global using LTChess.Logic.Magic;
global using LTChess.Logic.Search;
global using LTChess.Logic.Transposition;
global using LTChess.Logic.Util;

global using static LTChess.Logic.Core.UCI;
global using static LTChess.Logic.Data.CheckInfo;
global using static LTChess.Logic.Data.PrecomputedData;
global using static LTChess.Logic.Data.RunOptions;
global using static LTChess.Logic.Data.Squares;
global using static LTChess.Logic.Data.Color;
global using Color = LTChess.Logic.Data.Color;
global using static LTChess.Logic.Data.Piece;
global using static LTChess.Logic.Data.Bound;
global using static LTChess.Logic.Magic.MagicBitboards;
global using static LTChess.Logic.Search.SearchOptions;
global using static LTChess.Logic.Search.SearchConstants;
global using static LTChess.Logic.Search.Evaluation;
global using static LTChess.Logic.Util.PositionUtilities;
global using static LTChess.Logic.Util.Utilities;
global using static LTChess.Logic.Util.Interop;
global using static LTChess.Logic.Util.ExceptionHandling;
global using static LTChess.Logic.NN.NNRunOptions;
global using static LTChess.Logic.Threads.SearchThreadPool;

using LTChess.Logic.Book;
using LTChess.Logic.NN.Simple768;
using System.Runtime.Intrinsics;
using System.Reflection;
using LTChess.Logic.NN;
using System.Runtime.InteropServices;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.HalfKP;
using LTChess.Logic.Threads;

namespace LTChess
{

    public static unsafe class Program
    {
        private static Position p;
        private static SearchInformation info;

        public static void Main()
        {
            InitializeAll();

            p = new Position(owner: SearchPool.MainThread);
            info = new SearchInformation(p);

            DoInputLoop();
        }

        public static void InitializeAll()
        {
#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();

            //  Give the VS debugger a friendly name for the main program thread
            Thread.CurrentThread.Name = "MainThread";
#endif

            Utilities.CheckConcurrency();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                //  Don't run the constructors of NN classes.
                if (type.CustomAttributes.Any(x => x.AttributeType == typeof(SkipStaticConstructorAttribute))) continue;

                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
            }

            WarmUpJIT();

            if (!System.Diagnostics.Debugger.IsAttached) 
            {
                AppDomain.CurrentDomain.UnhandledException += ExceptionHandling.CurrentDomain_UnhandledException;
            }


            if (UseSimple768)
            {
                NNUEEvaluation.ResetNN();
            }

            //  The GC seems to drag its feet collecting some of the now unneeded memory (random strings and RunClassConstructor junk).
            //  This doesn't HAVE to be done now, and generally it isn't productive to force GC collections,
            //  but it will inevitably occur at some point later so we can take a bit of pressure off of it by doing this now.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();

#if DEBUG
            Log("InitializeAll done in " + sw.Elapsed.TotalSeconds + " s");
            sw.Stop();
#endif
        }

        public static void DoInputLoop()
        {
            Log("LTChess version " + EngineBuildVersion + " - " + EngineTagLine + "\r\n");

            ThreadSetup setup = new ThreadSetup();
            while (true)
            {
                string input = Console.ReadLine();
                if (input == null || input.Length == 0)
                {
                    continue;
                }
                string[] param = input.Split(' ');
                
                if (input.EqualsIgnoreCase("uci"))
                {
                    UCI uci = new UCI();
                    uci.Run();
                }
                else if (input.StartsWithIgnoreCase("position fen "))
                {
                    p.LoadFromFEN(input.Substring(13));
                    setup.StartFEN = input.Substring(13);
                }
                else if (input.StartsWithIgnoreCase("position startpos moves "))
                {
                    p.LoadFromFEN(InitialFEN);

                    setup.StartFEN = InitialFEN;
                    setup.SetupMoves.Clear();

                    var splits = input.Split(' ').ToList();
                    for (int i = 3; i < splits.Count; i++)
                    {
                        if (p.TryFindMove(splits[i], out Move m))
                        {
                            p.MakeMove(m);
                            setup.SetupMoves.Add(m);
                        }
                        else
                        {
                            Log("Failed doing extra moves! '" + splits[i] + "' isn't a legal move in the FEN " + p.GetFEN());
                            break;
                        }
                    }
                    Log("New FEN is " + p.GetFEN());
                }
                else if (input.StartsWith("go"))
                {
                    if (param.Length > 2 && param[1].ContainsIgnoreCase("perftp"))
                    {
                        int depth = int.Parse(param[2]);
                        Task.Run(() =>
                        {
                            Position temp = new Position(p.GetFEN(), false, owner: SearchPool.MainThread);
                            temp.PerftParallel(depth, true);
                        });
                        continue;
                    }
                    else if (param.Length > 2 && param[1].ContainsIgnoreCase("perft"))
                    {
                        int depth = int.Parse(param[2]);
                        Task.Run(() => DoPerftDivide(depth, false));
                        continue;
                    }

                    info = new SearchInformation(p, MaxDepth);
                    info.TimeManager.MaxSearchTime = SearchConstants.MaxSearchTime;

                    for (int i = 1; i < param.Length; i++)
                    {
                        if (param[i] == "movetime" && i < param.Length - 1 && int.TryParse(param[i + 1], out int moveTime))
                        {
                            info.SetMoveTime(moveTime);
                        }
                        else if (param[i] == "time" && i < param.Length - 1 && int.TryParse(param[i + 1], out int time))
                        {
                            info.TimeManager.MaxSearchTime = time;
                        }
                        else if (param[i] == "depth" && i < param.Length - 1 && int.TryParse(param[i + 1], out int depth))
                        {
                            info.MaxDepth = depth;
                        }
                    }

                    SearchPool.StartSearch(p, ref info, setup);
                }
                else if (input.EqualsIgnoreCase("ucinewgame"))
                {
                    p = new Position(InitialFEN, owner: SearchPool.MainThread);
                    Search.HandleNewGame();
                }
                else if (input.Equals("listmoves"))
                {
                    PrintMoves(true);
                }
                else if (input.StartsWithIgnoreCase("move "))
                {
                    string move = input.Substring(5).ToLower();

                    p.TryMakeMove(move);

                    Log(p.ToString());
                }
                else if (input.StartsWithIgnoreCase("stop"))
                {
                    info.StopSearching = true;
                    SearchPool.StopThreads = true;

                }
                else if (input.EqualsIgnoreCase("eval"))
                {
                    Log((UseHalfKA ? "HalfKA" : (UseHalfKP ? "HalfKP" : "Simple768")) + " Eval: " + Evaluation.GetEvaluation(p));
                    }
                else if (input.EqualsIgnoreCase("eval all"))
                {
                    DoEvalAllMoves();
                }
                else if (input.EqualsIgnoreCase("trace"))
                {
                    if (UseHalfKA)
                    {
                        HalfKA_HM.Trace(p);
                    }
                }
                else if (input.EqualsIgnoreCase("d"))
                {
                    Log(p.ToString());
                }
                else if (input.StartsWithIgnoreCase("threads"))
                {
                    if (int.TryParse(param[1], out int threadCount))
                    {
                        SearchPool.Resize(threadCount);
                    }
                }
                else if (input.ContainsIgnoreCase("searchinfo"))
                {
                    PrintSearchInfo();
                }
                else if (input.EqualsIgnoreCase("snapshots"))
                {
                    SearchStatistics.PrintSnapshots();
                }
                else if (input.EqualsIgnoreCase("terms"))
                {
                    EvaluationConstants.PrintConstants();
                }
                else if (input.StartsWithIgnoreCase("bench "))
                {
                    int depth = int.Parse(input.Substring(6));
                    FishBench.Go(depth);
                }
                else if (input.EqualsIgnoreCase("gc"))
                {
                    ForceGC();
                }
                else if (input.StartsWithIgnoreCase("quit") || input.StartsWithIgnoreCase("exit"))
                {
                    break;
                }
                else
                {
                    //  You can just copy paste in a FEN string rather than typing "position fen" before it.
                    if (input.Where(x => x == '/').Count() == 7)
                    {
                        if (p.LoadFromFEN(input.Trim()))
                        {
                            Log("Loaded fen");

                            if (UseHalfKA || UseHalfKP || UseSimple768)
                            {
                                p.State->Accumulator->RefreshPerspective[White] = true;
                                p.State->Accumulator->RefreshPerspective[Black] = true;
                            }
                        }
                    }
                    else
                    {
                        Log("Unknown token '" + input + "'");
                    }
                }
            }
        }

        /// <summary>
        /// The first time a search is performed, we have to spend about 50ms JIT'ing the various little functions in the search and evaluation classes.
        /// This can be done as soon as the engine starts so the time spent during the first search command isn't lost.
        /// </summary>
        private static void WarmUpJIT()
        {
            BlockOutputForJIT = true;

            Position temp = new Position(owner: SearchPool.MainThread);

            temp.Perft(4);

            info = new SearchInformation(temp);
            info.MaxDepth = 12;
            info.SetMoveTime(250);
            
            SearchPool.StartSearch(temp, ref info);
            SearchPool.BlockCallerUntilFinished();

            Search.HandleNewGame();
            SearchStatistics.Zero();
            TranspositionTable.Clear();
            SearchPool.Clear();

            BlockOutputForJIT = false;
        }


        public static void DoPerftDivide(int depth, bool sortAlphabetical = true)
        {
            ulong total = 0;

            //  The Position "p" has UpdateNN == true, which we don't want
            //  for purely Perft usage.
            Position pos = new Position(p.GetFEN(), false, null);

            Stopwatch sw = Stopwatch.StartNew();

            Span<ScoredMove> list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenLegal(list);

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                pos.MakeMove(m);
                ulong result = (depth > 1 ? pos.Perft(depth - 1) : 1);
                pos.UnmakeMove(m);
                Log(m.ToString() + ": " + result);
                total += result;
            }
            sw.Stop();

            Log("\r\nNodes searched:  " + total + " in " + sw.Elapsed.TotalSeconds + " s (" + ((int) (total / sw.Elapsed.TotalSeconds)).ToString("N0") + " nps)" + "\r\n");
        }
        
        public static void PrintSearchInfo()
        {
            Log(info.ToString());
            Log("\r\n");
            TranspositionTable.PrintClusterStatus();
            Log("\r\n");
            SearchStatistics.PrintStatistics();
        }

        /// <summary>
        /// Prints out the current static evaluation of the position, and the static evaluations after 
        /// each of the legal moves for that position are made.
        /// </summary>
        public static void DoEvalAllMoves()
        {
            Log("Static evaluation (White's perspective): " + Evaluation.GetEvaluation(p));
            Log("\r\nMove evaluations (White's perspective):");

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = p.GenLegal(list);
            List<(Move mv, int eval)> scoreList = new();

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                p.MakeMove(m);
                int moveEval = 0;

                moveEval = Evaluation.GetEvaluation(p);

                p.UnmakeMove(m);
                scoreList.Add((m, moveEval));
            }

            var sorted = scoreList.OrderBy(x => x.eval).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                Log(sorted[i].mv.ToString(p) + ": " + (sorted[i].eval * -1));
            }
        }


        private static void PrintMoves(bool full = false)
        {
            ScoredMove* pseudo = stackalloc ScoredMove[MoveListSize];
            int pseudoCnt = p.GenPseudoLegal(pseudo);

            Log("Pseudo: [" + Stringify(pseudo, p, pseudoCnt) + "]");

            ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
            int legalCnt = p.GenLegal(legal);

            Log("Legal: [" + Stringify(legal, p, legalCnt) + "]");


            if (full)
            {
                Log("\n");

                if (p.Checked)
                {
                    ScoredMove* evasions = stackalloc ScoredMove[MoveListSize];
                    int evasionsSize = p.GenAll<GenEvasions>(evasions);
                    Log("Evasions: [" + Stringify(evasions, p, evasionsSize) + "]");
                }
                else
                {
                    ScoredMove* nonEvasions = stackalloc ScoredMove[MoveListSize];
                    int nonEvasionsSize = p.GenAll<GenNonEvasions>(nonEvasions);
                    Log("Non-Evasions: [" + Stringify(nonEvasions, p, nonEvasionsSize) + "]");
                }

                ScoredMove* captures = stackalloc ScoredMove[MoveListSize];
                int capturesSize = p.GenAll<GenLoud>(captures);
                Log("Captures: [" + Stringify(captures, p, capturesSize) + "]");

                ScoredMove* quiets = stackalloc ScoredMove[MoveListSize];
                int quietsSize = p.GenAll<GenQuiets>(quiets);
                Log("Quiets: [" + Stringify(quiets, p, quietsSize) + "]");

                ScoredMove* checks = stackalloc ScoredMove[MoveListSize];
                int checksSize = p.GenAll<GenQChecks>(checks);
                Log("Checks: [" + Stringify(checks, p, checksSize) + "]");
                Log("\n\n");
            }
        }

        public static void DotTraceProfile(int depth = 24)
        {
            info.TimeManager.MaxSearchTime = 30000;
            info.MaxDepth = depth;

#if JB
            JetBrains.Profiler.Api.MeasureProfiler.StartCollectingData();
#endif
            SearchPool.StartSearch(p, ref info);
            SearchPool.BlockCallerUntilFinished();

#if JB
            JetBrains.Profiler.Api.MeasureProfiler.SaveData();
#endif

            Environment.Exit(123);
        }

        public static void TryToBreakSomething(int min = 100, int max = 300)
        {
            Random r = new Random();
            while (true)
            {
                Console.WriteLine("\r\nStart:\r\n");
                SearchPool.StartSearch(p, ref info);
                Thread.Sleep(r.Next(min, max));
                SearchPool.StopThreads = true;
                Thread.Sleep(r.Next(min, max));
            }
        }
    }

}