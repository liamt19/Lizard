using System.Text;

using Lizard.Logic.NN;
using Lizard.Logic.Threads;

namespace Lizard
{

    public static unsafe class Program
    {
        private static Position p;
        private static SearchInformation info;

        public static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "bench")
                {
                    SearchBench.Go(12, openBench: true);
                    Environment.Exit(0);
                }
                else if (args[0] == "compiler")
                {
                    Console.WriteLine(GetCompilerInfo());
                    Environment.Exit(0);
                }
            }

            InitializeAll();

            p = new Position(owner: SearchPool.MainThread);
            info = new SearchInformation(p);

            DoInputLoop();
        }

        public static void InitializeAll()
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                AppDomain.CurrentDomain.UnhandledException += ExceptionHandling.CurrentDomain_UnhandledException;
            }


            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                if (!SearchPool.StopThreads)
                {
                    //  If a search is ongoing, stop it instead of closing the console.
                    SearchPool.StopThreads = true;
                    e.Cancel = true;
                }

                //  Otherwise, e.Cancel == false and the program exits normally
            };


            //  Console.ReadLine() has a buffer of 256 (UTF-16?) characters, which is only large enough to handle
            //  "position startpos moves ..." commands containing fewer than 404 moves.
            //  This should double the amount that Console.ReadLine() can handle.
            //  Thanks to https://github.com/eduherminio for spotting this
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8, false, 2048 * 4));


            //  Give the VS debugger a friendly name for the main program thread
            Thread.CurrentThread.Name = "MainThread";

            Utilities.CheckConcurrency();

            //  The GC seems to drag its feet collecting some of the now unneeded memory (random strings and RunClassConstructor junk).
            //  This doesn't HAVE to be done now, and generally it isn't productive to force GC collections,
            //  but it will inevitably occur at some point later so we can take a bit of pressure off of it by doing this now.
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }


        private static void DoInputLoop()
        {
            Log("Lizard version " + EngineBuildVersion + "\r\n");

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
                    UCIClient uci = new UCIClient();
                    uci.Run();
                }
                else if (input.StartsWithIgnoreCase("position"))
                {
                    HandlePositionCommand(input, setup);
                }
                else if (input.StartsWith("go"))
                {
                    HandleGoCommand(input, setup);
                }
                else if (input.EqualsIgnoreCase("ucinewgame"))
                {
                    p = new Position(InitialFEN, owner: SearchPool.MainThread);
                    Searches.HandleNewGame();
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
                    SearchPool.StopThreads = true;
                }
                else if (input.EqualsIgnoreCase("eval"))
                {
                    Log("Bucketed768 Eval: " + NNUE.GetEvaluation(p));
                }
                else if (input.EqualsIgnoreCase("eval all"))
                {
                    HandleEvalAllCommand();
                }
                else if (input.StartsWithIgnoreCase("load"))
                {
                    HandleLoadCommand(input);
                }
                else if (input.StartsWithIgnoreCase("trace"))
                {
                    HandleTraceCommand(input);
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
                else if (input.StartsWithIgnoreCase("bench"))
                {
                    HandleBenchCommand(input);
                }
                else if (input.EqualsIgnoreCase("compiler"))
                {
                    Log(GetCompilerInfo());
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
                            Log("Loaded fen '" + p.GetFEN() + "'");
                        }
                    }
                    else
                    {
                        Log("Unknown token '" + input + "'");
                    }
                }
            }
        }


        private static void DoPerftIterative(int depth)
        {
            if (depth <= 0) return;

            //  The Position "p" has UpdateNN == true, which we don't want
            //  for purely Perft usage.
            Position pos = new Position(p.GetFEN(), false, null);

            Stopwatch sw = Stopwatch.StartNew();

            for (int d = 1; d <= depth; d++)
            {
                sw.Restart();
                ulong result = pos.Perft(d);
                sw.Stop();

                var time = sw.Elapsed.TotalSeconds;
                Log("Depth " + d + ": " +
                    "\tnodes " + result.ToString().PadLeft(12) +
                    "\ttime " + time.ToString("N6").PadLeft(12) +
                    "\tnps " + ((int)(result / time)).ToString("N0").PadLeft(14));

            }
        }

        private static void DoPerftDivide(int depth)
        {
            if (depth <= 0) return;

            ulong total = 0;

            //  The Position "p" has UpdateNN == true, which we don't want
            //  for purely Perft usage.
            Position pos = new Position(p.GetFEN(), false, null);
            pos.IsChess960 = p.IsChess960;

            Stopwatch sw = Stopwatch.StartNew();

            Span<ScoredMove> list = stackalloc ScoredMove[MoveListSize];
            int size = pos.GenLegal(list);

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                pos.MakeMove(m);
                ulong result = depth > 1 ? pos.Perft(depth - 1) : 1;
                pos.UnmakeMove(m);
                Log(m.ToString() + ": " + result);
                total += result;
            }
            sw.Stop();

            Log("\r\nNodes searched:  " + total + " in " + sw.Elapsed.TotalSeconds + " s (" + ((int)(total / sw.Elapsed.TotalSeconds)).ToString("N0") + " nps)" + "\r\n");
        }

        private static void DoPerftNN(int depth)
        {
            if (depth <= 0) return;

            Stopwatch sw = Stopwatch.StartNew();

            Span<ScoredMove> list = stackalloc ScoredMove[MoveListSize];
            int size = p.GenLegal(list);

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                p.MakeMove(m);
                long result = depth > 1 ? p.PerftNN(depth - 1) : 1;
                p.UnmakeMove(m);
                Log(m.ToString() + ": " + result);
            }
            sw.Stop();

            ulong[] shannon = { 0, 20, 400, 8902, 197281, 4865609, 119060324, 3195901860, 84998978956, 2439530234167 };
            ulong nodeCount = depth < shannon.Length ? shannon[depth] : 0;
            Log("\r\nRefreshed " + nodeCount + " times in " + sw.Elapsed.TotalSeconds + " s (" + ((int)(nodeCount / sw.Elapsed.TotalSeconds)).ToString("N0") + " nps)" + "\r\n");
        }

        private static void PrintSearchInfo()
        {
            Log(info.ToString());
            Log("\r\n");
            TranspositionTable.PrintClusterStatus();
        }

        /// <summary>
        /// Prints out the current static evaluation of the position, and the static evaluations after 
        /// each of the legal moves for that position are made.
        /// </summary>
        private static void HandleEvalAllCommand()
        {
            Log("Static evaluation (" + ColorToString(p.ToMove) + "'s perspective): " + NNUE.GetEvaluation(p));
            Log("\r\nMove evaluations (" + ColorToString(p.ToMove) + "'s perspective):");

            ScoredMove* list = stackalloc ScoredMove[MoveListSize];
            int size = p.GenLegal(list);
            List<(Move mv, int eval)> scoreList = new();

            for (int i = 0; i < size; i++)
            {
                Move m = list[i].Move;
                p.MakeMove(m);
                int moveEval = 0;

                moveEval = NNUE.GetEvaluation(p);

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


        private static void HandleLoadCommand(string input)
        {
            if (input.Length < 5)
            {
                Log("No file provided!");
                return;
            }

            input = input[5..];

            if (File.Exists(input))
            {
                NNUE.LoadNewNetwork(input);
                NNUE.RefreshAccumulator(p);
            }
            else
            {
                Log($"Couldn't find the file '{input}'");
            }
        }


        private static void HandleTraceCommand(string input)
        {
            if (input.ContainsIgnoreCase("piece"))
            {
                string[] splits = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length == 4)
                {
                    int pc = StringToColor(splits[2]);

                    if (pc == Color.ColorNB)
                    {
                        Log("Invalid color for \"trace piece\" command! It should be formatted like \"trace piece black knight\".");
                        return;
                    }

                    int pt = StringToPiece(splits[3]);
                    if (pt == Piece.None)
                    {
                        Log("Invalid type for \"trace piece\" command! It should be formatted like \"trace piece black knight\".");
                        return;
                    }

                    NNUE.TracePieceValues(pt, pc);
                }
                else
                {
                    Log("Invalid input for \"trace piece\" command! It should be formatted like \"trace piece black knight\".");
                }
            }
            else
            {
                NNUE.Trace(p);
            }
        }

        private static void HandleGoCommand(string input, ThreadSetup setup)
        {
            string[] param = input.Split(' ');

            if (param.Length > 2)
            {
                if (param[1].ContainsIgnoreCase("perftp"))
                {
                    if (int.TryParse(param[2], out int depth) && depth > 0)
                    {
                        Task.Run(() =>
                        {
                            Position temp = new Position(p.GetFEN(), false, owner: SearchPool.MainThread);
                            temp.PerftParallel(depth, true);
                        });
                    }

                    return;
                }
                else if (param[1].ContainsIgnoreCase("perfti"))
                {
                    if (int.TryParse(param[2], out int depth))
                    {
                        Task.Run(() => DoPerftIterative(depth));
                    }

                    return;
                }
                else if (param[1].ContainsIgnoreCase("perftnn"))
                {
                    if (int.TryParse(param[2], out int depth))
                    {
                        Task.Run(() => DoPerftNN(depth));
                    }

                    return;
                }
                else if (param[1].ContainsIgnoreCase("perft"))
                {
                    if (int.TryParse(param[2], out int depth))
                    {
                        Task.Run(() => DoPerftDivide(depth));
                    }

                    return;
                }
            }
            else if (input.ContainsIgnoreCase("perft"))
            {
                Log("'go' commands involving perft must have a specified depth!");
                return;
            }

            TranspositionTable.Clear();
            Searches.HandleNewGame();

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
                else if (param[i] == "nodes" && i < param.Length - 1 && ulong.TryParse(param[i + 1], out ulong reqNodes))
                {
                    info.MaxNodes = reqNodes;
                }
            }

            SearchPool.StartSearch(p, ref info, setup);
        }

        private static void HandlePositionCommand(string input, ThreadSetup setup)
        {
            string fen = InitialFEN;

            if (input.ContainsIgnoreCase("fen"))
            {
                try
                {
                    fen = input.Substring(input.IndexOf("fen") + 4);

                    //  Sanitize the input a bit to prevent crashes.
                    //  I'm only doing this here (rather than in LoadFromFEN) because I need LoadFromFEN to be as fast as possible,
                    //  and the UCI should never give a poorly formatted FEN anyways.
                    if (fen.Where(x => x == '/').Count() != 7)
                    {
                        Log("Valid FEN strings should contain the character '/' exactly 7 times, and the FEN '" + fen + "' doesn't!");
                        return;
                    }

                    if (fen.Where(x => x == ' ').Count() < 2)
                    {
                        Log("Valid FEN strings should contain at least 2 spaces, and the FEN '" + fen + "' doesn't!");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Log("Couldn't parse fen from the input!");
                    Log(e.ToString());
                    return;
                }
            }

            p.LoadFromFEN(fen);
            setup.StartFEN = fen;

            if (input.ContainsIgnoreCase("moves"))
            {
                setup.SetupMoves.Clear();

                //  This expects the input to be formatted like "position fen <fen> moves <move1> <move2> <move3> ..."

                List<string> splits;
                try
                {
                    splits = input.Substring(input.IndexOf("moves") + 6).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                catch (IndexOutOfRangeException e)
                {
                    Log("Couldn't parse the list of moves to make from the input!");
                    Log(e.ToString());
                    return;
                }

                for (int i = 0; i < splits.Count; i++)
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
            }

            Log("Loaded fen '" + p.GetFEN() + "'");
        }

        private static void HandleBenchCommand(string input)
        {
            if (input.ContainsIgnoreCase("perft"))
            {
                int depth;

                try
                {
                    depth = int.Parse(input.Substring(input.IndexOf("perft") + 6));
                }
                catch (Exception e)
                {
                    Log("Couldn't parse the perft depth from the input!");
                    Log(e.ToString());
                    return;
                }

                FishBench.Go(depth);
            }
            else
            {
                int depth = 12;

                try
                {
                    if (input.Length > 5 && int.TryParse(input.AsSpan(input.IndexOf("bench") + 6), out int newDepth))
                    {
                        depth = newDepth;
                    }
                }
                catch (Exception e)
                {
                    Log("Couldn't parse the bench depth from the input!");
                    Log(e.ToString());
                    return;
                }

                SearchBench.Go(depth);
            }
        }


        private static void DotTraceProfile(int depth = 24)
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

    }

}
