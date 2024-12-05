
#pragma warning disable CS0162 // Unreachable code detected

using System.Reflection;

using Lizard.Logic.NN;
using Lizard.Logic.Threads;

namespace Lizard.Logic.UCI
{
    public unsafe class UCIClient
    {
        private Position pos;
        private SearchInformation info;
        private ThreadSetup setup;

        private static Dictionary<string, UCIOption> Options;

        public static bool Active = false;

        public UCIClient(Position pos)
        {
            ProcessUCIOptions();

            this.pos = pos;

            info = new SearchInformation(pos);
            info.OnDepthFinish = OnDepthDone;
            info.OnSearchFinish = OnSearchDone;

            setup = new ThreadSetup();
        }

        /// <summary>
        /// Blocks until a command is sent in the standard input stream
        /// </summary>
        /// <param name="cmd">Set to the command, which is the first word in the input</param>
        /// <returns>The remaining words in the input, which are parameters for the command</returns>
        private static string[] ReceiveString(out string cmd)
        {
            string input = Console.ReadLine();
            if (input == null || input.Length == 0)
            {
                cmd = ":(";
                return Array.Empty<string>();
            }

            string[] splits = input.Split(" ");
            cmd = splits[0].ToLower();
            string[] param = splits.ToList().GetRange(1, splits.Length - 1).ToArray();

            return param;
        }

        /// <summary>
        /// Sends the UCI options, and begins waiting for input.
        /// </summary>
        public void Run()
        {
            Active = true;

#if DEV
            Console.WriteLine($"id name Lizard {EngineBuildVersion} DEV");
#else
            Console.WriteLine($"id name Lizard {EngineBuildVersion}");
#endif
            Console.WriteLine("id author Liam McGuire");
            Console.WriteLine("info string Using Bucketed768 evaluation.");

            PrintUCIOptions();
            Console.WriteLine("uciok");

            //  In case a "ucinewgame" isn't sent for the first game
            HandleNewGame(pos.Owner.AssocPool);
            InputLoop();
        }

        /// <summary>
        /// Handles commands sent by UCI's.
        /// </summary>
        private void InputLoop()
        {
            while (true)
            {
                string[] param = ReceiveString(out string cmd);

                if (cmd == "quit")
                {
                    Environment.Exit(0);
                }
                else if (cmd == "isready")
                {
                    Console.WriteLine("readyok");
                }
                else if (cmd == "ucinewgame")
                {
                    HandleNewGame(GlobalSearchPool);
                }
                else if (cmd == "position")
                {
                    pos.IsChess960 = UCI_Chess960;

                    info = new SearchInformation(pos);
                    info.OnDepthFinish = OnDepthDone;
                    info.OnSearchFinish = OnSearchDone;

                    ParsePositionCommand(param, pos, setup);
                    NNUE.RefreshAccumulator(info.Position);
                }
                else if (cmd == "go")
                {
                    GlobalSearchPool.StopThreads = false;
                    HandleGo(param);
                }
                else if (cmd == "stop")
                {
                    GlobalSearchPool.StopThreads = true;
                }
                else if (cmd == "leave")
                {
                    Active = false;
                    return;
                }
                else if (cmd == "setoption")
                {
                    try
                    {
                        //  param[0] == "name"
                        string optName = param[1];
                        string optValue = "";

                        for (int i = 2; i < param.Length; i++)
                        {
                            if (param[i] == "value")
                            {
                                for (int j = i + 1; j < param.Length; j++)
                                {
                                    optValue += param[j] + " ";
                                }
                                optValue = optValue.Trim();
                                break;
                            }
                            else
                            {
                                optName += " " + param[i];
                            }
                        }

                        //  param[2] == "value"
                        HandleSetOption(optName, optValue);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[ERROR]: Failed parsing setoption command, got '{param}' -> {e}");
                    }
                }
                else if (cmd == "tune")
                {
                    PrintSPSAParams();
                }
                else if (cmd == "eval")
                {
                    Console.WriteLine($"{NNUE.GetEvaluation(pos)}");
                }
            }
        }




        //  https://gist.github.com/DOBRO/2592c6dad754ba67e6dcaec8c90165bf

        /// <summary>
        /// Process "go" command parameters and begin a search.
        /// 
        /// <para> Currently handled: </para>
        /// <br> movetime -> search for milliseconds </br>
        /// <br> depth -> search until a specific depth (in plies) </br>
        /// <br> nodes -> only look at a maximum number of nodes </br>
        /// <br> infinite -> keep looking until we get a "stop" command </br>
        /// <br> (w/b)time -> the specified player has x amount of milliseconds left</br>
        /// <br> (w/b)inc -> the specified player gains x milliseconds after they move</br>
        /// 
        /// <para> Currently ignored: </para>
        /// <br> ponder, movestogo, mate </br>
        /// 
        /// </summary>
        /// <param name="param">List of parameters sent with the "go" command.</param>
        private void HandleGo(string[] param)
        {
            if (info.SearchActive)
            {
                return;
            }

            bool makeTime = ParseGo(param, ref info, setup);

            //  If we weren't told to search for a specific time (no "movetime" and not "infinite"),
            //  then we make one ourselves
            if (makeTime)
            {
                info.TimeManager.MakeMoveTime();
            }

            GlobalSearchPool.StartSearch(info.Position, ref info, setup);
        }


        public static bool ParseGo(string[] param, ref SearchInformation info, ThreadSetup setup)
        {
            TimeManager tm = info.TimeManager;

            //  Assume that we can search infinitely, and let the UCI's "go" parameters constrain us accordingly.
            info.NodeLimit = MaxSearchNodes;
            tm.MaxSearchTime = MaxSearchTime;
            info.DepthLimit = MaxDepth;

            if (info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = false;
            }

            int stm = info.Position.ToMove;

            setup.UCISearchMoves = new List<Move>();

            for (int i = 0; i < param.Length - 1; i++)
            {
                if (param[i] == "movetime" && int.TryParse(param[i + 1], out int reqMovetime))
                {
                    info.SetMoveTime(reqMovetime);
                }
                else if (param[i] == "depth" && int.TryParse(param[i + 1], out int reqDepth))
                {
                    info.DepthLimit = reqDepth;
                }
                else if (param[i] == "nodes" && ulong.TryParse(param[i + 1], out ulong reqNodes))
                {
                    info.NodeLimit = reqNodes;
                }
                else if (param[i] == "movestogo" && int.TryParse(param[i + 1], out int reqMovestogo))
                {
                    tm.MovesToGo = reqMovestogo;
                }
                else if (((param[i] == "wtime" && stm == White) || (param[i] == "btime" && stm == Black))
                    && int.TryParse(param[i + 1], out int reqPlayerTime))
                {
                    tm.PlayerTime = reqPlayerTime;
                }
                else if (((param[i] == "winc" && stm == White) || (param[i] == "binc" && stm == Black))
                    && int.TryParse(param[i + 1], out int reqPlayerIncrement))
                {
                    tm.PlayerIncrement = reqPlayerIncrement;
                }
                else if (param[i] == "searchmoves")
                {
                    i++;

                    while (i <= param.Length - 1)
                    {
                        if (info.Position.TryFindMove(param[i], out Move m))
                        {
                            setup.UCISearchMoves.Add(m);
                        }

                        i++;
                    }
                }
                else if (param[i] == "infinite")
                {
                    Assert(info.NodeLimit == MaxSearchNodes, $"go infinite should have NodeLimit == {MaxSearchNodes}, but it was {info.NodeLimit}");
                    Assert(tm.MaxSearchTime == MaxSearchTime, $"go infinite should have MaxSearchTime == {MaxSearchTime}, but it was {tm.MaxSearchTime}");
                    Assert(info.DepthLimit == MaxDepth, $"go infinite should have DepthLimit == {MaxDepth}, but it was {info.DepthLimit}");

                    info.NodeLimit = MaxSearchNodes;
                    tm.MaxSearchTime = MaxSearchTime;
                    info.DepthLimit = MaxDepth;
                }
            }

            return param.Any(x => x.EndsWith("time") && x.StartsWith(ColorToString(stm).ToLower()[0])) && !param.Any(x => x == "movetime");
        }


        /// <summary>
        /// Sends the "info depth (number) ..." string to the UCI
        /// </summary>
        private void OnDepthDone(ref SearchInformation info)
        {
            PrintSearchInfo(ref info);
        }



        private void OnSearchDone(ref SearchInformation info)
        {
            info.SearchActive = false;

            if (info.SearchFinishedCalled)
            {
                return;
            }

            info.SearchFinishedCalled = true;
            var bestThread = info.Position.Owner.AssocPool.GetBestThread();
            if (bestThread.RootMoves.Count == 0)
            {
                Console.WriteLine("bestmove 0000");
                return;
            }

            Move bestThreadMove = bestThread.RootMoves[0].Move;
            if (bestThreadMove.IsNull())
            {
                ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
                int size = info.Position.GenLegal(legal);
                bestThreadMove = legal[0].Move;
            }

            Console.WriteLine($"bestmove {bestThreadMove.ToString(info.Position.IsChess960)}");
        }

        private static void HandleNewGame(SearchThreadPool pool)
        {
            pool.MainThread.WaitForThreadFinished();
            pool.TTable.Clear();
            pool.Clear();
        }

        private void HandleSetOption(string optName, string optValue)
        {
            optName = optName.Replace(" ", string.Empty);

            try
            {
                string key = Options.Keys.First(x => x.Replace(" ", string.Empty).EqualsIgnoreCase(optName));
                UCIOption opt = Options[key];
                object prevValue = opt.FieldHandle.GetValue(null);

                if (opt.FieldHandle.FieldType == typeof(bool) && bool.TryParse(optValue, out bool newBool))
                {
                    opt.FieldHandle.SetValue(null, newBool);
                }
                else if (opt.FieldHandle.FieldType == typeof(int) && int.TryParse(optValue, out int newValue))
                {
                    if (newValue >= opt.MinValue && newValue <= opt.MaxValue)
                    {
                        opt.FieldHandle.SetValue(null, newValue);

                        if (opt.Name == nameof(Threads))
                        {
                            GlobalSearchPool.Resize(SearchOptions.Threads);
                        }

                        if (opt.Name == nameof(Hash))
                        {
                            GlobalSearchPool.TTable.Initialize(SearchOptions.Hash);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[ERROR]: Failed handling setoption command for '{optName}' -> {optValue}! {e}");
            }

        }

        private static void ProcessUCIOptions()
        {
            Options = new Dictionary<string, UCIOption>();

            //  Get all "public static" fields, and specifically exclude constant fields (which have field.IsLiteral == true)
            List<FieldInfo> fields = typeof(SearchOptions).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => !x.IsLiteral).ToList();

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;

                //  Most options are numbers and are called "spin"
                //  If they are true/false, they are called "check"
                string fieldType = field.FieldType == typeof(bool) ? "check" : "spin";
                string defaultValue = field.GetValue(null).ToString().ToLower();

                UCIOption opt = new UCIOption(fieldName, fieldType, defaultValue, field);

                Options.Add(fieldName, opt);
            }

            Options[nameof(Threads)].SetMinMax(1, 512);
            Options[nameof(MultiPV)].SetMinMax(1, 256);
            Options[nameof(Hash)].SetMinMax(1, 1048576);

            Options[nameof(SEMinDepth)].AutoMinMax();
            Options[nameof(SENumerator)].AutoMinMax();
            Options[nameof(SEDoubleMargin)].AutoMinMax();
            Options[nameof(SETripleMargin)].AutoMinMax();
            Options[nameof(SETripleCapSub)].AutoMinMax();
            Options[nameof(SEDepthAdj)].SetMinMax(-3, 2);

            Options[nameof(NMPMinDepth)].AutoMinMax();
            Options[nameof(NMPBaseRed)].AutoMinMax();
            Options[nameof(NMPDepthDiv)].AutoMinMax();
            Options[nameof(NMPEvalDiv)].AutoMinMax();
            Options[nameof(NMPEvalMin)].SetMinMax(0, 6);

            Options[nameof(RFPMaxDepth)].AutoMinMax();
            Options[nameof(RFPMargin)].AutoMinMax();

            Options[nameof(ProbcutMinDepth)].SetMinMax(1, 5);
            Options[nameof(ProbcutBeta)].AutoMinMax();
            Options[nameof(ProbcutBetaImp)].AutoMinMax();

            Options[nameof(ShallowSEEMargin)].AutoMinMax();
            Options[nameof(ShallowMaxDepth)].AutoMinMax();

            Options[nameof(LMRQuietDiv)].AutoMinMax();
            Options[nameof(LMRCaptureDiv)].AutoMinMax();
            Options[nameof(LMRExtMargin)].AutoMinMax();

            Options[nameof(QSFutileMargin)].AutoMinMax();
            Options[nameof(QSSeeMargin)].AutoMinMax();

            Options[nameof(OrderingCheckBonus)].AutoMinMax();
            Options[nameof(OrderingVictimMult)].AutoMinMax();

            Options[nameof(IIRMinDepth)].SetMinMax(2, 6);
            Options[nameof(AspWindow)].AutoMinMax();

            Options[nameof(StatBonusMult)].AutoMinMax();
            Options[nameof(StatBonusSub)].AutoMinMax();
            Options[nameof(StatBonusMax)].AutoMinMax();

            Options[nameof(StatMalusMult)].AutoMinMax();
            Options[nameof(StatMalusSub)].AutoMinMax();
            Options[nameof(StatMalusMax)].AutoMinMax();

            Options[nameof(SEEValue_Pawn)].AutoMinMax();
            Options[nameof(SEEValue_Knight)].AutoMinMax();
            Options[nameof(SEEValue_Bishop)].AutoMinMax();
            Options[nameof(SEEValue_Rook)].AutoMinMax();
            Options[nameof(SEEValue_Queen)].AutoMinMax();

            Options[nameof(ValuePawn)].AutoMinMax();
            Options[nameof(ValueKnight)].AutoMinMax();
            Options[nameof(ValueBishop)].AutoMinMax();
            Options[nameof(ValueRook)].AutoMinMax();
            Options[nameof(ValueQueen)].AutoMinMax();


            foreach (var optName in Options.Keys)
            {
                var opt = Options[optName];
                if (opt.FieldHandle.FieldType != typeof(int))
                {
                    continue;
                }

                //  Ensure values are within [Min, Max] and Max > Min
                int currValue = int.Parse(opt.DefaultValue);
                if (currValue < opt.MinValue || currValue > opt.MaxValue || opt.MaxValue < opt.MinValue)
                {
                    Log($"Option '{optName}' has an invalid range! -> [{opt.MinValue} <= {opt.DefaultValue} <= {opt.MaxValue}]!");
                }
            }
        }


        private static void PrintUCIOptions()
        {
            List<string> whitelist =
            [
                nameof(SearchOptions.Threads),
                nameof(SearchOptions.MultiPV),
                nameof(SearchOptions.Hash),
                nameof(SearchOptions.UCI_Chess960),
                nameof(SearchOptions.UCI_ShowWDL),
                nameof(SearchOptions.UCI_PrettyPrint),

                nameof(SearchOptions.SEMinDepth),
                nameof(SearchOptions.NMPMinDepth),
                nameof(SearchOptions.RFPMaxDepth),
                nameof(SearchOptions.RFPMargin),
                nameof(SearchOptions.ProbcutMinDepth),
                nameof(SearchOptions.ProbcutBeta),
                nameof(SearchOptions.IIRMinDepth),
                nameof(SearchOptions.AspWindow),
                nameof(SearchOptions.ValuePawn),
                nameof(SearchOptions.ValueKnight),
                nameof(SearchOptions.ValueBishop),
                nameof(SearchOptions.ValueRook),
                nameof(SearchOptions.ValueQueen),
            ];

            foreach (string k in Options.Keys.Where(x => whitelist.Contains(x)))
            {
                Console.WriteLine(Options[k].ToString());
            }
        }

        private static void PrintSPSAParams()
        {
            List<string> ignore =
            [
                nameof(SearchOptions.Threads),
                nameof(SearchOptions.MultiPV),
                nameof(SearchOptions.Hash),
                nameof(SearchOptions.UCI_Chess960),
                nameof(SearchOptions.UCI_ShowWDL),
                nameof(SearchOptions.UCI_PrettyPrint),
            ];

            foreach (var optName in Options.Keys)
            {
                if (ignore.Contains(optName))
                {
                    continue;
                }

                var opt = Options[optName];
                Console.WriteLine(opt.GetSPSAFormat());
            }
        }

    }
}
