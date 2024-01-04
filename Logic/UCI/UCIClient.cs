using System.Reflection;

using LTChess.Logic.NN;
using LTChess.Logic.Threads;

namespace LTChess.Logic.UCI
{
    public unsafe class UCIClient
    {

        private Position pos;
        private SearchInformation info;
        private ThreadSetup setup;

        private const string LogFileName = @".\ucilog.txt";

        /// <summary>
        /// If this is true, then engine instances will attempt to write to their own "ucilog_#.txt" files, 
        /// where # is a (hopefully) unique number for this instance.
        /// </summary>
        private const bool WriteToConcurrentLogs = false;

        private Dictionary<string, UCIOption> Options;

        private static object LogFileLock = new object();

        public static bool Active = false;

        public UCIClient()
        {
            ProcessUCIOptions();

            setup = new ThreadSetup();
            pos = new Position(owner: SearchPool.MainThread);
            info = new SearchInformation(pos, DefaultSearchDepth);
            info.OnDepthFinish = OnDepthDone;
            info.OnSearchFinish = OnSearchDone;
            if (File.Exists(LogFileName))
            {
                LogString("\n\n**************************************************\n"
                    + CenteredString(DateTime.Now.ToString(), 50) + "\n"
                    + "**************************************************");
            }
        }

        /// <summary>
        /// Writes the string <paramref name="s"/> to standard output, which will be received by chess UCI's.
        /// </summary>
        public static void SendString(string s)
        {
            Console.WriteLine(s);
            LogString("[OUT]: " + s);
        }

        /// <summary>
        /// Appends the string <paramref name="s"/> to the file <see cref="LogFileName"/>
        /// </summary>
        public static void LogString(string s, bool newLine = true)
        {
            if (NO_LOG_FILE)
            {
                return;
            }

            lock (LogFileLock)
            {
                try
                {
                    string fileToWrite = LogFileName;

                    if (IsRunningConcurrently)
                    {
                        if (WriteToConcurrentLogs)
                        {
                            fileToWrite = @".\ucilog_" + ProcessID + ".txt";
                        }
                        else
                        {
                            //  Concurrent logging off, just return here.
                            return;
                        }
                    }

                    using FileStream fs = new FileStream(fileToWrite, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using StreamWriter sw = new StreamWriter(fs);

                    if (newLine)
                    {
                        sw.WriteLine(s);
                    }
                    else
                    {
                        sw.Write(s);
                    }

                    sw.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR LogString('" + s + "') failed!");
                    Console.WriteLine(e.ToString());
                }
            }
        }

        /// <summary>
        /// Blocks until a command is sent in the standard input stream
        /// </summary>
        /// <param name="cmd">Set to the command, which is the first word in the input</param>
        /// <returns>The remaining words in the input, which are parameters for the command</returns>
        private string[] ReceiveString(out string cmd)
        {
            string input = Console.ReadLine();
            if (input == null || input.Length == 0)
            {
                cmd = ":(";
                return new string[0];
            }

            string[] splits = input.Split(" ");
            cmd = splits[0].ToLower();
            string[] param = splits.ToList().GetRange(1, splits.Length - 1).ToArray();

            LogString("[IN]: " + input);

            return param;
        }

        /// <summary>
        /// Sends the UCI options, and begins waiting for input.
        /// </summary>
        public void Run()
        {
            Active = true;

#if DEV
            SendString("id name LTChess " + EngineBuildVersion + " DEV");
#else
            SendString("id name LTChess " + EngineBuildVersion);
#endif
            SendString("id author Liam McGuire");
            SendString("info string Using Simple768 evaluation.");

            foreach (string k in Options.Keys)
            {
                SendString(Options[k].ToString());
            }
            SendString("uciok");

            LogString("[INFO]: Compiler info -> '" + GetCompilerInfo() + "'");

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
                    LogString("[INFO]: Exiting with code " + 1001);
                    Environment.Exit(1001);
                }
                else if (cmd == "isready")
                {
                    SendString("readyok");
                }
                else if (cmd == "ucinewgame")
                {
                    HandleNewGame();
                }
                else if (cmd == "position")
                {
                    info = new SearchInformation(pos, DefaultSearchDepth);
                    info.OnDepthFinish = OnDepthDone;
                    info.OnSearchFinish = OnSearchDone;

                    setup.SetupMoves.Clear();

                    if (param[0] == "startpos")
                    {
                        setup.StartFEN = InitialFEN;

                        //  Some UCI's send commands that look like "position startpos moves e2e4 c7c5 g1f3"
                        //  If the command does have a "moves" component, then set the fen normally,
                        //  and try to make the moves that we were told to.
                        info.Position.LoadFromFEN(setup.StartFEN);
                        if (param.Length > 1 && param[1] == "moves")
                        {
                            for (int i = 2; i < param.Length; i++)
                            {
                                if (info.Position.TryFindMove(param[i], out Move m))
                                {
                                    info.Position.MakeMove(m);
                                }
                                else
                                {
                                    LogString("[ERROR]: Failed doing extra moves! '" + param[i] + "' didn't work with FEN " + info.Position.GetFEN());
                                }

                                setup.SetupMoves.Add(m);
                            }

                            LogString("[INFO]: New FEN is " + info.Position.GetFEN());
                        }
                        else
                        {
                            LogString("[INFO]: Set position to " + InitialFEN);
                        }
                    }
                    else
                    {
                        if (EnableAssertions)
                        {
                            Assert(param[0] == "fen",
                                "The first parameter for a 'position' UCI command was '" + param[0] + "', but it should have been 'fen'! " +
                                "A 'position' command must either be followed by 'startpos' for the initial position, or by 'fen ...' for an arbitrary FEN.");
                        }

                        string fen = param[1];

                        bool hasExtraMoves = false;
                        for (int i = 2; i < param.Length; i++)
                        {
                            if (param[i] == "moves")
                            {
                                info.Position.LoadFromFEN(fen);
                                for (int j = i + 1; j < param.Length; j++)
                                {

                                    if (info.Position.TryFindMove(param[j], out Move m))
                                    {
                                        info.Position.MakeMove(m);
                                    }
                                    else
                                    {
                                        LogString("[ERROR]: Failed doing extra moves! '" + param[j] + "' didn't work with FEN " + info.Position.GetFEN());
                                    }

                                    setup.SetupMoves.Add(m);
                                }

                                LogString("[INFO]: New FEN is " + info.Position.GetFEN());
                                hasExtraMoves = true;
                                break;
                            }
                            else
                            {
                                fen += " " + param[i];
                            }

                        }

                        setup.StartFEN = fen;

                        if (!hasExtraMoves)
                        {
                            LogString("[INFO]: Set position to " + fen);
                            info.Position.LoadFromFEN(fen);
                        }

                    }

                    Simple768.RefreshAccumulator(info.Position, ref *info.Position.State->Accumulator);
                }
                else if (cmd == "go")
                {
                    SearchPool.StopThreads = false;
                    HandleGo(param);
                }
                else if (cmd == "stop")
                {
                    LogString("[INFO]: got stop command at " + FormatCurrentTime());
                    SearchPool.StopThreads = true;
                }
                else if (cmd == "leave")
                {
                    LogString("[INFO]: Leaving");
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
                        LogString("[ERROR]: Failed parsing setoption command, got '" + param.ToString() + "'");
                        LogString(e.ToString());
                    }

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
            LogString("[INFO]: Got 'go' command at " + FormatCurrentTime());
            if (info.SearchActive)
            {
                LogString("[WARN]: Got 'go' command while a search is already in progress, ignoring");
                return;
            }

            TimeManager tm = info.TimeManager;

            //  Assume that we can search infinitely, and let the UCI's "go" parameters constrain us accordingly.
            info.MaxNodes = MaxSearchNodes;
            tm.MaxSearchTime = MaxSearchTime;
            info.MaxDepth = MaxDepth;

            if (info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = false;
                LogString("[INFO]: Reusing old SearchInfo object, info.SearchFinishedCalled was true");
            }

            bool isMoveTimeCommand = false;
            bool hasPlayerTime = false;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.SetMoveTime(int.Parse(param[i + 1]));
                    LogString("[INFO]: MaxSearchTime is set to " + tm.MaxSearchTime);

                    isMoveTimeCommand = true;
                }
                else if (param[i] == "depth")
                {
                    if (i + 1 >= param.Length)
                    {
                        break;
                    }
                    if (int.TryParse(param[i + 1], out int reqDepth))
                    {
                        info.MaxDepth = reqDepth;
                        LogString("[INFO]: MaxDepth is set to " + info.MaxDepth);
                    }

                }
                else if (param[i] == "nodes")
                {
                    if (i + 1 >= param.Length)
                    {
                        break;
                    }
                    if (ulong.TryParse(param[i + 1], out ulong reqNodes))
                    {
                        info.MaxNodes = reqNodes;
                        LogString("[INFO]: MaxNodes is set to " + info.MaxNodes);
                    }
                }
                else if (param[i] == "infinite")
                {
                    if (EnableAssertions)
                    {
                        Assert(info.MaxNodes == MaxSearchNodes,
                            "An infinite search command should have MaxNodes == " + MaxSearchNodes + ", but it was " + info.MaxNodes);

                        Assert(tm.MaxSearchTime == MaxSearchTime,
                            "An infinite search command should have MaxSearchTime == " + MaxSearchTime + ", but it was " + tm.MaxSearchTime);

                        Assert(info.MaxDepth == MaxDepth,
                            "An infinite search command should have MaxDepth == " + MaxDepth + ", but it was " + info.MaxDepth);
                    }

                    info.MaxNodes = MaxSearchNodes;
                    tm.MaxSearchTime = MaxSearchTime;
                    info.MaxDepth = MaxDepth;
                }
                else if ((param[i] == "wtime" && info.Position.ToMove == Color.White) ||
                         (param[i] == "btime" && info.Position.ToMove == Color.Black))
                {
                    tm.PlayerTime = int.Parse(param[i + 1]);
                    hasPlayerTime = true;

                    LogString("[INFO]: We have " + tm.PlayerTime + " ms left on our clock, should STOP by " +
                              (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(tm.PlayerTime)).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000") +
                              ", current time " + FormatCurrentTime());
                }
                else if ((param[i] == "winc" && info.Position.ToMove == Color.White) ||
                         (param[i] == "binc" && info.Position.ToMove == Color.Black))
                {
                    tm.PlayerIncrement = int.Parse(param[i + 1]);
                }
                else if (param[i] == "movestogo")
                {
                    tm.MovesToGo = int.Parse(param[i + 1]);
                }
            }

            //  If we weren't told to search for a specific time (no "movetime" and not "infinite"),
            //  then we make one ourselves
            if (!isMoveTimeCommand && hasPlayerTime)
            {
                info.TimeManager.MakeMoveTime();
            }

            SearchPool.StartSearch(info.Position, ref info, setup);
            LogString("[INFO]: Returned from call to StartSearch at " + FormatCurrentTime());
        }


        /// <summary>
        /// Sends the "info depth (number) ..." string to the UCI
        /// </summary>
        private void OnDepthDone(ref SearchInformation info)
        {
            SendString(FormatSearchInformationMultiPV(ref info));
        }



        private void OnSearchDone(ref SearchInformation info)
        {
            info.SearchActive = false;

            //  TODO: make sure we can't send illegal moves
            if (!info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = true;

                var bestThread = SearchPool.GetBestThread();

                if (bestThread.RootMoves.Count == 0)
                {
                    LogString("[ERROR]: bestThread.RootMoves.Count was 0!\n" + info.ToString());
                    SendString("bestmove 0000");
                    return;
                }

                Move bestThreadMove = bestThread.RootMoves[0].Move;

                if (EnableAssertions)
                {
                    Assert(SearchPool.MainThread.RootMoves[0].Move == bestThreadMove,
                        "MainThread's best move = " + SearchPool.MainThread.RootMoves[0].Move + " was different than the BestThread's = " + bestThreadMove + "!");
                }

                if (bestThreadMove.IsNull())
                {
                    ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
                    int size = info.Position.GenLegal(legal);
                    bestThreadMove = legal[0].Move;

                    LogString("[ERROR]: info.BestMove in OnSearchDone was null! Replaced it with first legal move " + legal[0].Move);
                }
                else if (!info.Position.IsLegal(bestThreadMove))
                {
                    LogString("[ERROR]: bestThreadMove (" + bestThreadMove.ToString() + ") in OnSearchDone isn't legal for FEN '" + info.Position.GetFEN() + "'");
                }

                SendString("bestmove " + bestThreadMove.ToString());
                LogString("[INFO]: sent 'bestmove " + bestThreadMove.ToString() + "' at " + FormatCurrentTime());
            }
            else
            {
                LogString("[INFO]: SearchFinishedCalled was true, ignoring.");
            }



            if (ServerGC)
            {
                //  Force a GC now if we are running in the server mode.
                ForceGC();
            }
        }

        private void HandleNewGame()
        {
            SearchPool.MainThread.WaitForThreadFinished();
            TranspositionTable.Clear();
            Search.Searches.HandleNewGame();
        }

        private void HandleSetOption(string optName, string optValue)
        {
            foreach (var key in Options.Keys)
            {
                if (key.Replace(" ", string.Empty).EqualsIgnoreCase(optName.Replace(" ", string.Empty)))
                {
                    try
                    {
                        UCIOption opt = Options[key];
                        object prevValue = opt.FieldHandle.GetValue(null);

                        if (opt.FieldHandle.FieldType == typeof(bool))
                        {
                            bool newValue = true;
                            if (bool.TryParse(optValue, out bool tf))
                            {
                                newValue = tf;
                            }
                            else if (optValue == "1")
                            {
                                newValue = true;
                            }
                            else if (optValue == "0")
                            {
                                newValue = false;
                            }
                            else
                            {
                                LogString("[ERROR]: setoption commands for booleans need to have a \"value\" of 'True/False' or '1/0', " +
                                    "and '" + optValue + "' wasn't one of those!");
                                return;
                            }

                            opt.FieldHandle.SetValue(null, newValue);
                        }
                        else if (opt.FieldHandle.FieldType == typeof(int))
                        {
                            if (int.TryParse(optValue, out int newValue))
                            {
                                if (newValue >= opt.MinValue && newValue <= opt.MaxValue)
                                {
                                    if (opt.Name == nameof(Threads))
                                    {
                                        SearchPool.Resize(newValue);
                                        LogString("Changed '" + key + "' from " + prevValue + " to " + SearchPool.ThreadCount);
                                    }
                                    else if (opt.Name == nameof(Hash))
                                    {
                                        TranspositionTable.Initialize(newValue);
                                        LogString("Changed '" + key + "' from " + prevValue + " to " + newValue + " mb");
                                    }
                                    else
                                    {
                                        opt.FieldHandle.SetValue(null, newValue);
                                        LogString("Changed '" + key + "' from " + prevValue + " to " + opt.FieldHandle.GetValue(null));
                                    }
                                }
                                else
                                {
                                    LogString("[ERROR]: '" + key + "' needs a value between [" + opt.MinValue + ", " + opt.MaxValue + "], " +
                                        "and '" + optValue + "' isn't in that range!");
                                    return;
                                }
                            }
                            else
                            {
                                LogString("[ERROR]: setoption commands for integers need to have a numerical value, " +
                                    "and '" + optValue + "' isn't!");
                                return;
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        LogString("[ERROR]: Failed handling setoption command for '" + optName + "' -> " + optValue);
                        LogString(e.ToString());
                    }

                    return;
                }
            }

            LogString("[WARN]: Got setoption for '" + optName + "' but that isn't an option!");
        }

        private void ProcessUCIOptions()
        {
            Options = new Dictionary<string, UCIOption>();

            //  Get all "public static" fields, and specifically exclude constant fields (which have field.IsLiteral == true)
            List<FieldInfo>? fields = typeof(SearchOptions).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => !x.IsLiteral).ToList();

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

            Options[nameof(Threads)].SetMinMax(1, 8);
            Options[nameof(MultiPV)].SetMinMax(1, 5);
            Options[nameof(Hash)].SetMinMax(1, 2048);
            Options[nameof(SingularExtensionsMinDepth)].SetMinMax(1, 16);
            Options[nameof(NullMovePruningMinDepth)].SetMinMax(1, 16);
            Options[nameof(ReverseFutilityPruningMaxDepth)].SetMinMax(1, 16);
            Options[nameof(ReverseFutilityPruningPerDepth)].SetMinMax(1, 200);
            Options[nameof(ExchangeBase)].SetMinMax(1, 500);
            Options[nameof(ExtraCutNodeReductionMinDepth)].SetMinMax(1, 16);
            Options[nameof(AspirationWindowMargin)].SetMinMax(1, 500);

#if SPSA
            Options[nameof(SPSA_SINGLE_NUMERATOR)].SetMinMax(1, 20);
            Options[nameof(SPSA_SINGLE_MIN_DEPTH)].SetMinMax(1, 10);
            Options[nameof(SPSA_SINGLE_BETA)].SetMinMax(0, 60);

            Options[nameof(SPSA_STATBONUS_MULT)].SetMinMax(100, 400);
            Options[nameof(SPSA_STATBONUS_SUB)].SetMinMax(0, 500);
            Options[nameof(SPSA_STATBONUS_MIN)].SetMinMax(500, 3000);

            Options[nameof(SPSA_RFP_MAX_DEPTH)].SetMinMax(4, 12);
            Options[nameof(SPSA_RFP_PER_DEPTH)].SetMinMax(35, 95);

            Options[nameof(SPSA_EXCHANGE_BASE)].SetMinMax(80, 360);

            Options[nameof(SPSA_ASPIRATION_MARGIN)].SetMinMax(5, 80);
#endif
        }
    }
}
