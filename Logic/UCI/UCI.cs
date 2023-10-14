
//#define TUNE

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using LTChess.Logic.Book;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.Threads;

using static LTChess.Logic.Search.Search;

namespace LTChess.Logic.Core
{
    public unsafe class UCI
    {

        private Position pos;
        private SearchInformation info;
        private ThreadSetup setup;

        public const string LogFileName = @".\ucilog.txt";
        public const string FilenameLast = @".\ucilog_last.txt";

        /// <summary>
        /// If this is true, then engine instances will attempt to write to their own "ucilog_#.txt" files, 
        /// where # is a (hopefully) unique number for this instance.
        /// </summary>
        private const bool WriteToConcurrentLogs = false;

        private Dictionary<string, UCIOption> Options;

        private static object LogFileLock = new object();

        public static bool Active = false;

        public UCI()
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
            lock (LogFileLock)
            {
                try
                {
                    string fileToWrite = LogFileName;

                    if (IsRunningConcurrently)
                    {
                        if (WriteToConcurrentLogs)
                        {
                            fileToWrite = @".\ucilog_" + ConcurrencyCount + ".txt";
                        }
                        else
                        {
                            //  Concurrent logging off, just return here.
                            return;
                        }
                    }

                    using FileStream fs = new FileStream(fileToWrite, FileMode.Append, FileAccess.Write, FileShare.Read);
                    using StreamWriter sw = new StreamWriter(fs);

#if DEBUG
                    long timeMS = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - Utilities.StartTimeMS;
                    sw.WriteLine(timeMS.ToString("0000000") + " " + s);
#else
                    if (newLine)
                    {
                        sw.WriteLine(s);
                    }
                    else
                    {
                        sw.Write(s);
                    }
#endif

                    sw.Flush();
                }
                catch(Exception e)
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
        public string[] ReceiveString(out string cmd)
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

            SendString("id name LTChess " + EngineBuildVersion);
            SendString("id author Liam McGuire");
            foreach (string k in Options.Keys)
            {
                SendString(Options[k].ToString());
            }
            SendString("uciok");
            InputLoop();
        }

        /// <summary>
        /// The main loop of the UCI, which handles commands sent by UCI's.
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
                        Debug.Assert(param[0] == "fen");
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

                    if (UseHalfKA)
                    {
                        info.Position.State->Accumulator->RefreshPerspective[White] = true;
                        info.Position.State->Accumulator->RefreshPerspective[Black] = true;
                    }
                }
                else if (cmd == "go")
                {
                    info.StopSearching = false;
                    SearchPool.StopThreads = false;
                    HandleGo(param);
                }
                else if (cmd == "stop")
                {
                    LogString("[INFO]: got stop command at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000")));
                    info.StopSearching = true;
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
            LogString("[INFO]: Got 'go' command" + TimeManager.GetFormattedTime());
            if (info.SearchActive)
            {
                LogString("[WARN]: Got 'go' command while a search is already in progress, ignoring");
                return;
            }

            //  Assume that we can search infinitely, and let the UCI's "go" parameters constrain us accordingly.
            info.MaxNodes = ulong.MaxValue - 1;
            info.TimeManager.MaxSearchTime = MaxSearchTime;
            info.MaxDepth = MaxDepth;

            if (info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = false;
                LogString("[INFO]: Reusing old SearchInfo object, info.SearchFinishedCalled was true");
            }

            bool hasMoveTime = false;
            bool hasWhiteTime = false;
            bool hasBlackTime = false;

            bool hasDepthCommand = false;
            bool isInfinite = false;

            int whiteTime = 0;
            int blackTime = 0;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.SetMoveTime(int.Parse(param[i + 1]));
                    //info.TimeManager.MaxSearchTime = int.Parse(param[i + 1]);
                    LogString("[INFO]: MaxSearchTime is set to " + info.TimeManager.MaxSearchTime);

                    hasMoveTime = true;
                }
                else if (param[i] == "depth")
                {
                    if (i + 1 >= param.Length)
                    {
                        break;
                    }
                    if (int.TryParse(param[i + 1], out int reqDepth))
                    {
                        hasDepthCommand = true;
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
                    //  TODO: These are to make sure that a plain "go" command is treated the same as a "go infinite",
                    //  and that a "go infinite" shouldn't actually have to change any of the constraints.
                    Debug.Assert(info.MaxNodes == ulong.MaxValue - 1);
                    Debug.Assert(info.TimeManager.MaxSearchTime == MaxSearchTime);
                    Debug.Assert(info.MaxDepth == MaxDepth);

                    info.MaxNodes = ulong.MaxValue - 1;
                    info.TimeManager.MaxSearchTime = MaxSearchTime;
                    info.MaxDepth = MaxDepth;
                    isInfinite = true;
                }
                else if (param[i] == "wtime")
                {
                    whiteTime = int.Parse(param[i + 1]);
                    hasWhiteTime = true;

                    if (info.Position.ToMove == Color.White)
                    {
                        info.TimeManager.PlayerTime[info.Position.ToMove] = whiteTime;
                        info.RootPlayerToMove = Color.White;

                        LogString("[INFO]: We have " + info.TimeManager.PlayerTime[info.Position.ToMove] + " ms left on our clock, should STOP by " +
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.TimeManager.PlayerTime[info.Position.ToMove])).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000") +
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000")));
                    }
                }
                else if (param[i] == "btime")
                {
                    blackTime = int.Parse(param[i + 1]);
                    hasBlackTime = true;

                    if (info.Position.ToMove == Color.Black)
                    {
                        info.TimeManager.PlayerTime[info.Position.ToMove] = blackTime;
                        info.RootPlayerToMove = Color.Black;

                        LogString("[INFO]: We have " + info.TimeManager.PlayerTime[info.Position.ToMove] + " ms left on our clock, should STOP by " +
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.TimeManager.PlayerTime[info.Position.ToMove])).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000") +
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000")));
                    }
                }
                else if (param[i] == "winc")
                {
                    info.TimeManager.PlayerIncrement[Color.White] = int.Parse(param[i + 1]);
                }
                else if (param[i] == "binc")
                {
                    info.TimeManager.PlayerIncrement[Color.Black] = int.Parse(param[i + 1]);
                }
                else if (param[i] == "movestogo")
                {
                    info.TimeManager.MovesToGo = int.Parse(param[i + 1]);
                }
            }

            if ((hasWhiteTime && info.Position.ToMove == Color.White && hasMoveTime && (whiteTime < info.TimeManager.MaxSearchTime)) ||
                (hasBlackTime && info.Position.ToMove == Color.Black && hasMoveTime && (blackTime < info.TimeManager.MaxSearchTime)))
            {
                //  We will need to reduce our search time if we have less time than info.MaxSearchTime

                int newTime = (info.Position.ToMove == Color.White ? whiteTime : blackTime);

                // TODO: this should no longer happen
                LogString("[ERROR]: only have " + whiteTime + "ms left <= MaxSearchTime: " + info.TimeManager.MaxSearchTime + ", setting time to " + newTime + "ms");
                info.TimeManager.MaxSearchTime = newTime;
            }

            //  If we weren't told to search for a specific time (no "movetime" and not "infinite"),
            //  then we make one ourselves
            if (!hasMoveTime && hasWhiteTime && hasBlackTime)
            {
                info.TimeManager.MakeMoveTime(info.Position.ToMove);
            }

            //Search.Search.StartSearching(ref info, !hasDepthCommand);
            SearchPool.StartSearch(info.Position, ref info, setup);
            LogString("[INFO]: Returned from call to start_thinking at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000")));
        }


        /// <summary>
        /// Sends the "info depth (number) ..." string to the UCI
        /// </summary>
        private void OnDepthDone(ref SearchInformation info)
        {
            //info.LastSearchInfo = FormatSearchInformation(ref info);
            info.LastSearchInfo = FormatSearchInformationMultiPV(ref info);
            SendString(info.LastSearchInfo);
        }



        private void OnSearchDone(ref SearchInformation info)
        {
            info.SearchActive = false;

            //  TODO: make sure we can't send illegal moves
            if (!info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = true;

                Move bestThreadMove = SearchPool.GetBestThread().RootMoves[0].Move;
                info.BestMove = bestThreadMove;

#if DEBUG
                if (SearchPool.MainThread.RootMoves[0].Move != bestThreadMove)
                {
                    Log("WARN MainThread best move = " + SearchPool.MainThread.RootMoves[0].Move + " was different than BestThread's = " + bestThreadMove);
                }
#endif

                if (info.BestMove.IsNull())
                {
                    if (StopLosingOnTimeFromVerizon && info.TimeManager.PlayerTime[info.Position.ToMove] <= 1)
                    {
                        //  If the bestmove is null, and our search time was too low to give a reasonable time to search,
                        //  then just pick the first legal move we can make and send that instead.
                        Move* legal = stackalloc Move[NormalListCapacity];
                        int size = info.Position.GenAllLegalMovesTogether(legal);

                        LogString("[ERROR]: info.BestMove in OnSearchDone was null! Replaced it with first legal move " + legal[0]);
                        info.BestMove = legal[0];
                    }
                    else
                    {
                        LogString("[ERROR]: info.BestMove in OnSearchDone was null!");
                    }

                    LogString("[INFO]: info.LastSearchInfo = '" + info.LastSearchInfo + "'");
                }
                else if (!info.Position.IsLegal(info.BestMove))
                {
                    LogString("[ERROR]: info.BestMove (" + info.BestMove.ToString() + ") in OnSearchDone isn't legal!");
                    LogString("[INFO]: FEN = '" + info.Position.GetFEN() + "'");
                    LogString("[INFO]: info.LastSearchInfo = '" + info.LastSearchInfo + "'");
                }

                SendString("bestmove " + info.BestMove.ToString());
                LogString("[INFO]: sent 'bestmove " + info.BestMove.ToString() + "' at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000")));
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

        public void HandleNewGame()
        {
            SearchPool.MainThread.WaitForThreadFinished();
            TranspositionTable.Clear();
            Search.Search.HandleNewGame();
        }

        private void HandleSetOption(string optName, string optValue)
        {

            //  Currently only "Threads", "MultiPV", and "UseSingularExtensions" can be changed at runtime.
            if (optName.EqualsIgnoreCase("Threads"))
            {
                if (int.TryParse(optValue, out int newVal))
                {
                    SearchPool.Resize(newVal);
                    LogString("[INFO]: Set SearchPool's ThreadCount to " + SearchPool.ThreadCount);
                }
                else
                {
                    LogString("[ERROR]: Failed setting SearchPool Threads to '" + optValue + "', couldn't parse the number");
                }
            }
            else if (optName.EqualsIgnoreCase("MultiPV"))
            {
                if (int.TryParse(optValue, out int newVal))
                {
                    MultiPV = newVal;
                    LogString("[INFO]: Set MultiPV to " + MultiPV);
                }
                else
                {
                    LogString("[ERROR]: Failed setting MultiPV to '" + optValue + "', couldn't parse the number");
                }
            }
            else if (optName.Replace(" ", string.Empty).EqualsIgnoreCase("UseSingularExtensions"))
            {
                if (optValue.EqualsIgnoreCase("true") || optValue.Equals("1"))
                {
                    UseSingularExtensions = true;
                    LogString("[INFO]: Enabled Singular Extensions");
                }
                else if (optValue.EqualsIgnoreCase("false") || optValue.Equals("0"))
                {
                    UseSingularExtensions = false;
                    LogString("[INFO]: Disabled Singular Extensions");
                }
                else
                {
                    LogString("[ERROR]: Failed setting UseSingularExtensions to '" + optValue + "', should be true/false or 1/0");
                }
            }
            else
            {
                LogString("[WARN]: Got setoption for '" + optName + "' but that isn't an option!");
            }

            return;


            //  This checks if optName was sent in it's "friendly format" (Use Quiescence SEE), or sent regularly (UseQuiescenceSEE)
            if (!Options.ContainsKey(optName))
            {
                string noSpaces = optName.Replace(" ", string.Empty);
                string addedSpaces = Regex.Replace(optName, "([A-Z]+)", (match) => { return " " + match; }).Trim();

                bool fixedName = false;
                foreach (var key in Options.Keys)
                {
                    if (key == noSpaces || key == addedSpaces)
                    {
                        LogString("[WARN]: Fixed name for setoption command '" + optName + "' -> '" + key + "'");
                        optName = key;
                        fixedName = true;
                        break;
                    }
                }

                if (!fixedName)
                {
                    LogString("[WARN]: Got setoption for '" + optName + "' but that isn't an option!");
                    return;
                }

            }

            try
            {
                LogString("[INFO]: Changing '" + optName + "' from " + Options[optName].DefaultValue + " to " + optValue);
                UCIOption opt = Options[optName];
                if (opt.FieldHandle.FieldType == typeof(bool))
                {
                    opt.FieldHandle.SetValue(null, bool.Parse(optValue));
                }
                else if (opt.FieldHandle.FieldType == typeof(int))
                {
                    opt.FieldHandle.SetValue(null, int.Parse(optValue));
                }
                else if (opt.FieldHandle.FieldType == typeof(double[]))
                {
                    double[] arr = (double[]) opt.FieldHandle.GetValue(null);
                    arr[opt.ValueArrayIndex] = double.Parse(optValue);
                    opt.FieldHandle.SetValue(null, arr);
                }

#if TUNE
                Tune.NormalizeTerms();
#endif

            }
            catch (Exception e)
            {
                LogString("[ERROR]: Failed handling setoption command for '" + optName + "' -> " + optValue);
                LogString(e.ToString());
            }


        }

        private void ProcessUCIOptions(bool friendly = false)
        {
            Options = new Dictionary<string, UCIOption>();

            //  Get all "public static" fields, and specifically exclude constant fields (which have field.IsLiteral == true)
            List<FieldInfo>? fields = typeof(SearchConstants).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => !x.IsLiteral).ToList();

            foreach (FieldInfo field in fields)
            {
                string fieldName = field.Name;

                if (friendly)
                {
                    //  Give that field a friendly name, which has spaces between capital letters.
                    //  i.e. "UseQuiescenceSEE" is presented as "Use Quiescence SEE"
                    fieldName = Regex.Replace(field.Name, "([A-Z]+)", (match) => { return " " + match; }).Trim();
                }

                //  Most options are numbers and are called "spin"
                //  If they are true/false, they are called "check"
                string fieldType = (field.FieldType == typeof(bool) ? "check" : "spin");
                string defaultValue = field.GetValue(null).ToString().ToLower();

                UCIOption opt = new UCIOption(fieldName, fieldType, defaultValue, field);
                Options.Add(fieldName, opt);
            }

            //  Add some of the terms in EvaluationConstants as UCI options
#if TUNE
            fields = typeof(EvaluationConstants).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => (!x.IsLiteral && x.Name.StartsWith("Scale"))).ToList();

            foreach (FieldInfo field in fields)
            {
                string fieldType = (field.FieldType == typeof(bool) ? "check" : "spin");

                string defaultValueMG = ((double[])field.GetValue(null))[EvaluationConstants.GamePhaseNormal].ToString().ToLower();
                UCIOption optMG = new UCIOption(field.Name + "MG", fieldType, defaultValueMG, field);
                optMG.ValueArrayIndex = EvaluationConstants.GamePhaseNormal;
                Options.Add(optMG.Name, optMG);

                string defaultValueEG = ((double[])field.GetValue(null))[EvaluationConstants.GamePhaseEndgame].ToString().ToLower();
                UCIOption optEG = new UCIOption(field.Name + "EG", fieldType, defaultValueEG, field);
                optEG.ValueArrayIndex = EvaluationConstants.GamePhaseEndgame;
                Options.Add(optEG.Name, optEG);
            }
#endif

        }
    }
}
