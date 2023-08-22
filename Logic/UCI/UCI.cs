
//#define TUNE

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using LTChess.Logic.Book;
using LTChess.Logic.NN;
using LTChess.Logic.NN.HalfKA_HM;
using LTChess.Logic.NN.HalfKP;

namespace LTChess.Logic.Core
{
    public class UCI
    {
        private SearchInformation info;

        public const string LogFileName = @".\ucilog.txt";
        public const string FilenameLast = @".\ucilog_last.txt";

        public static int DefaultMoveOverhead = 10;

        private Dictionary<string, UCIOption> Options;

        private static object LogFileLock = new object();

        public UCI()
        {
            ProcessUCIOptions();

            info = new SearchInformation(new Position(), DefaultSearchDepth);
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
        public void SendString(string s)
        {
            Console.WriteLine(s);
            LogString("[OUT]: " + s);
        }

        /// <summary>
        /// Appends the string <paramref name="s"/> to the file <see cref="LogFileName"/>
        /// </summary>
        public static void LogString(string s)
        {
            if (IsRunningConcurrently)
            {
                return;
            }

            lock (LogFileLock)
            {
                using (FileStream fs = new FileStream(LogFileName, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    using StreamWriter sw = new StreamWriter(fs);

#if DEBUG
                    long timeMS = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - Utilities.debug_time_off;
                    sw.WriteLine(timeMS.ToString("0000000") + " " + s);
#else
                    sw.WriteLine(s);
#endif

                    sw.Flush();
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
            UCIMode = true;

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
                    info.Position = new Position();
                }
                else if (cmd == "position")
                {
                    info = new SearchInformation(new Position(), DefaultSearchDepth);
                    info.OnDepthFinish = OnDepthDone;
                    info.OnSearchFinish = OnSearchDone;

                    if (param[0] == "startpos")
                    {
                        info.Position = new Position();
                        if (param.Length > 1 && param[1] == "moves")
                        {
                            for (int i = 2; i < param.Length; i++)
                            {
                                info.Position.TryMakeMove(param[i]);
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
                                info.Position = new Position(fen);
                                for (int j = i + 1; j < param.Length; j++)
                                {
                                    if (!info.Position.TryMakeMove(param[j]))
                                    {
                                        LogString("[ERROR]: Failed doing extra moves! '" + param[j] + "' didn't work with FEN " + info.Position.GetFEN());
                                    }
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

                        if (!hasExtraMoves)
                        {
                            LogString("[INFO]: Set position to " + fen);
                            info.Position = new Position(fen);
                        }

                    }

                    if (UseHalfKP)
                    {
                        //  TODO: is this necessary?
                        HalfKP.RefreshNN(info.Position);
                        HalfKP.ResetNN();
                    }

                    if (UseHalfKA)
                    {
                        HalfKA_HM.RefreshNN(info.Position);
                        HalfKA_HM.ResetNN();
                    }
                }
                else if (cmd == "go")
                {
                    info.StopSearching = false;
                    HandleGo(param);
                }
                else if (cmd == "stop")
                {
                    LogString("[INFO]: got stop command at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
                    info.StopSearching = true;
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

        /// <summary>
        /// Sends the "info depth (number) ..." string to the UCI
        /// </summary>
        private void OnDepthDone(SearchInformation info)
        {

            info.LastSearchInfo = FormatSearchInformation(ref info);
            SendString(info.LastSearchInfo);
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
                LogString("[WARN]: Got 'go' command while a search is already in progress, ignoring");
                return;
            }

            info.MaxDepth = DefaultSearchDepth;
            info.TimeManager.MaxSearchTime = DefaultSearchTime;
            LogString("[INFO]: Got 'go' command");

            bool hasMoveTime = false;
            bool hasWhiteTime = false;
            bool hasBlackTime = false;

            bool hasDepthCommand = false;

            int whiteTime = 0;
            int blackTime = 0;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.SetMoveTime(int.Parse(param[i + 1]));
                    info.TimeManager.MoveTime = int.Parse(param[i + 1]);
                    info.TimeManager.HasMoveTime = true;

                    info.TimeManager.MaxSearchTime = int.Parse(param[i + 1]);
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
                    info.MaxNodes = ulong.MaxValue - 1;
                    info.TimeManager.MaxSearchTime = MaxSearchTime;
                    info.MaxDepth = MaxDepth;
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
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.TimeManager.PlayerTime[info.Position.ToMove])).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000") +
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
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
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.TimeManager.PlayerTime[info.Position.ToMove])).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000") +
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
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

                //  If we have more than 5 seconds, we can use all of the time we have above that.
                //  Otherwise, just use our remaining time.
                int newTime = (info.Position.ToMove == Color.White ? whiteTime : blackTime);

                LogString("[INFO]: only have " + whiteTime + "ms left <= MaxSearchTime: " + info.TimeManager.MaxSearchTime + ", setting time to " + newTime + "ms");
                info.TimeManager.MaxSearchTime = newTime;
            }

            if (!hasMoveTime && (info.TimeManager.MaxSearchTime == SearchConstants.DefaultSearchTime) && hasWhiteTime && hasBlackTime)
            {
                info.TimeManager.MakeMoveTime(info.Position.ToMove, info.Position.Moves.Count);

                //int inc = this.info.Position.ToMove == Color.White ? whiteInc : blackInc;
                //int playerTime = this.info.Position.ToMove == 0 ? whiteTime : blackTime;
                //int newSearchTime = inc + (playerTime / Math.Max(20, 20 - this.info.Position.Moves.Count));

                //info.TimeManager.MaxSearchTime = newSearchTime;
                //LogString("[INFO]: setting search time to " + (newSearchTime - inc) + " + " + inc + " = " + newSearchTime);
            }

            bool gotBookMove = false;
            if (SearchConstants.UsePolyglot && info.Position.Moves.Count < (SearchConstants.PolyglotMaxPly * 2))
            {
                if (Polyglot.Probe(info.Position, Polyglot.DefaultMethod, out Move bookMove, out int moveWeight))
                {
                    gotBookMove = true;
                    info.BestMove = bookMove;
                    info.BestScore = moveWeight;
                    Log("Polyglot probe returned " + bookMove.ToString(info.Position) + " eval " + moveWeight);

                    SendString("info depth 1 seldepth 1 time 1 score cp " + moveWeight + " nodes 1 nps 1 hashfull 0 pv " + bookMove.ToString());

                    if (SearchConstants.PolyglotSimulateTime)
                    {
                        Log("PolyglotSimulateTime is true, sleeping for " + info.TimeManager.MaxSearchTime + " ms" + TimeManager.GetFormattedTime());
                        Thread.Sleep(info.TimeManager.MaxSearchTime);
                        Log("Woke up" + TimeManager.GetFormattedTime());
                    }

                    info.OnSearchFinish?.Invoke(info);
                }
                else
                {
                    Log("Polyglot probe failed");
                }
            }

            if (gotBookMove)
            {
                return;
            }

            SimpleSearch.StartSearching(ref info, !hasDepthCommand);
            LogString("[INFO]: Returned from call to StartSearching at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
        }


        private void OnSearchDone(SearchInformation info)
        {
            info.SearchActive = false;

            //  TODO: make sure we can't send illegal moves
            if (!info.SearchFinishedCalled)
            {
                info.SearchFinishedCalled = true;

                if (info.BestMove.IsNull())
                {
                    LogString("[ERROR]: info.BestMove in OnSearchDone was null!");
                    LogString("[INFO]: info.LastSearchInfo = '" + info.LastSearchInfo + "'");
                }
                else if (!info.Position.IsLegal(info.BestMove))
                {
                    LogString("[ERROR]: info.BestMove (" + info.BestMove.ToString() + ") in OnSearchDone isn't legal!");
                    LogString("[INFO]: FEN = '" + info.Position.GetFEN() + "'");
                    LogString("[INFO]: info.LastSearchInfo = '" + info.LastSearchInfo + "'");
                }

                SendString("bestmove " + info.BestMove.ToString());
                LogString("[INFO]: sent 'bestmove " + info.BestMove.ToString() + "' at " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
            }
            else
            {
                LogString("[INFO]: SearchFinishedCalled was true, ignoring.");
            }
        }

        private void HandleSetOption(string optName, string optValue)
        {
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

                Tune.NormalizeTerms();

            }
            catch (Exception e)
            {
                LogString("[ERROR]: Failed handling setoption command for '" + optName + "' -> " + optValue);
                LogString(e.ToString());
            }


        }

        private void ProcessUCIOptions()
        {
            Options = new Dictionary<string, UCIOption>();

            //  Get all "public static" fields, and specifically exclude constant fields (which have field.IsLiteral == true)
            List<FieldInfo>? fields = typeof(SearchConstants).GetFields(BindingFlags.Public | BindingFlags.Static).Where(x => !x.IsLiteral).ToList();

            foreach (FieldInfo field in fields)
            {
                //  Give that field a friendly name, which has spaces between capital letters.
                //  i.e. "UseQuiescenceSEE" is presented as "Use Quiescence SEE"
                string friendlyName = Regex.Replace(field.Name, "([A-Z]+)", (match) => { return " " + match; }).Trim();

                //  Most options are numbers and are called "spin"
                //  If they are true/false, they are called "check"
                string fieldType = (field.FieldType == typeof(bool) ? "check" : "spin");
                string defaultValue = field.GetValue(null).ToString().ToLower();

                UCIOption opt = new UCIOption(friendlyName, fieldType, defaultValue, field);
                Options.Add(friendlyName, opt);
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
