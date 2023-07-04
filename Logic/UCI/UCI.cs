using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Data.RunOptions;
using LTChess.Search;
using LTChess.Magic;
using LTChess.Data;
using static LTChess.Data.Squares;
using System.Diagnostics;
using LTChess.Core;


namespace LTChess.Core
{
    public class UCI
    {
        public SearchInformation info;

        public const string LogFileName = @".\ucilog.txt";
        public const string FilenameLast = @".\ucilog_last.txt";

        public static int DefaultMoveOverhead = 10;

        public Dictionary<string, UCIOption> options;

        private static object logFileLock = new object();

        public UCI()
        {
            

            options = new Dictionary<string, UCIOption>
            {
                { UCIOption.Opt_DefaultMoveOverhead, new UCIOption(UCIOption.Opt_DefaultMoveOverhead, "spin", DefaultMoveOverhead.ToString(), "1", "3000")},
                { UCIOption.Opt_DefaultSearchTime, new UCIOption(UCIOption.Opt_DefaultSearchTime, "spin", (DefaultSearchTime / 1000).ToString(), "1", "120")},
                { UCIOption.Opt_DefaultSearchDepth, new UCIOption(UCIOption.Opt_DefaultSearchDepth, "spin", DefaultSearchDepth.ToString(), "1", "99")},
                { UCIOption.Opt_UseAspirationWindows, new UCIOption(UCIOption.Opt_UseAspirationWindows, "check", UseAspirationWindows.ToString().ToLower(), "", "")},
                { UCIOption.Opt_AspirationWindowMargin, new UCIOption(UCIOption.Opt_AspirationWindowMargin, "spin", AspirationWindowMargin.ToString(), "10", "250")},
                { UCIOption.Opt_AspirationMarginPerDepth, new UCIOption(UCIOption.Opt_AspirationMarginPerDepth, "spin", AspirationMarginPerDepth.ToString(), "0", "50")},
                { UCIOption.Opt_UseDeltaPruning, new UCIOption(UCIOption.Opt_UseDeltaPruning, "check", UseDeltaPruning.ToString().ToLower(), "", "")},
                { UCIOption.Opt_DeltaPruningMargin, new UCIOption(UCIOption.Opt_DeltaPruningMargin, "spin", DeltaPruningMargin.ToString(), "200", "220")},
                { UCIOption.Opt_UseFutilityPruning, new UCIOption(UCIOption.Opt_UseFutilityPruning, "check", UseFutilityPruning.ToString().ToLower(), "", "")},
                { UCIOption.Opt_UseStaticExchangeEval, new UCIOption(UCIOption.Opt_UseStaticExchangeEval, "check", UseStaticExchangeEval.ToString().ToLower(), "", "")},
            };

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

        public void SendString(string s)
        {
            Console.WriteLine(s);
            LogString("[OUT]: " + s);
        }

        public static void LogString(string s)
        {
            lock (logFileLock)
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

        public string[] ReceiveString(out string cmd)
        {
            string input = Console.ReadLine();
            if (input == null || input.Length == 0)
            {
                cmd = "uh oh";
                return new string[0];
            }

            string[] splits = input.Split(" ");
            cmd = splits[0].ToLower();
            string[] param = splits.ToList().GetRange(1, splits.Length - 1).ToArray();

            LogString("[IN]: " + input);

            return param;
        }

        public void Run()
        {
            UCIMode = true;

            SendString("id name LTChess " + EngineBuildVersion);
            SendString("id author Liam McGuire");
            foreach (string k in options.Keys)
            {
                SendString(options[k].ToString());
            }
            SendString("uciok");
            InputLoop();
        }

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
                        LogString("[INFO]: Set position to " + InitialFEN);
                        info.Position = new Position();
                        if (param.Length > 1 && param[1] == "moves")
                        {
                            for (int i = 2; i < param.Length; i++)
                            {
                                info.Position.TryMakeMove(param[i]);
                            }

                            LogString("[INFO]: New FEN is " + info.Position.GetFEN());
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
                }
                else if (cmd == "go")
                {
                    info.StopSearching = false;
                    Go(param);
                }
                else if (cmd == "stop")
                {
                    info.StopSearching = true;
                    LogString("[INFO]: Stopping search");
                }
                else if (cmd == "leave")
                {
                    LogString("[INFO]: Leaving");
                    return;
                }
                else if (cmd == "setoption")
                {
                    if (param.Length <= 4)
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
                        

                        if (!options.ContainsKey(optName)) 
                        {
                            LogString("[INFO]: Got setoption for '" + optName + "' but that isn't an option!");
                            continue;
                        }

                        LogString("[INFO]: Changing '" + optName + "' from " + options[optName].DefaultValue + " to " + optValue);
                        switch (optName)
                        {
                            case UCIOption.Opt_DefaultMoveOverhead:
                                DefaultMoveOverhead = int.Parse(optValue);
                                break;
                            case UCIOption.Opt_DefaultSearchTime:
                                DefaultSearchTime = int.Parse(optValue) * 1000;
                                break;
                            case UCIOption.Opt_DefaultSearchDepth:
                                DefaultSearchDepth = int.Parse(optValue);
                                break;
                            case UCIOption.Opt_UseAspirationWindows:
                                UseAspirationWindows = bool.Parse(optValue);
                                break;
                            case UCIOption.Opt_AspirationWindowMargin:
                                AspirationWindowMargin = int.Parse(optValue);
                                break;
                            case UCIOption.Opt_AspirationMarginPerDepth:
                                AspirationMarginPerDepth = int.Parse(optValue);
                                break;
                            case UCIOption.Opt_UseDeltaPruning:
                                UseDeltaPruning = bool.Parse(optValue);
                                break;
                            case UCIOption.Opt_DeltaPruningMargin:
                                DeltaPruningMargin = int.Parse(optValue);
                                break;
                            case UCIOption.Opt_UseFutilityPruning:
                                UseFutilityPruning = bool.Parse(optValue);
                                break;
                            case UCIOption.Opt_UseStaticExchangeEval:
                                UseStaticExchangeEval = bool.Parse(optValue);
                                break;
                        }
                    }

                }
            }
        }

        private void OnDepthDone(SearchInformation info)
        {
            //  Send the "info depth (number) ..." string
            info.LastSearchInfo = FormatSearchInformation(info);
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
        /// 
        /// <para> Currently ignored: </para>
        /// <br> ponder, wtime / btime, winc/binc, movestogo, mate </br>
        /// 
        /// </summary>
        /// <param name="param">List of parameters sent with the "go" command.</param>
        private void Go(string[] param)
        {
            info.MaxDepth = DefaultSearchDepth;
            info.MaxSearchTime = DefaultSearchTime;
            LogString("[INFO]: Got 'go' command");

            bool hasMoveTime = false;
            bool hasWhiteTime = false;
            bool hasBlackTime = false;

            bool hasDepthCommand = false;

            int whiteTime = 0;
            int blackTime = 0;
            int whiteInc = 0;
            int blackInc = 0;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.MaxSearchTime = long.Parse(param[i + 1]);
                    LogString("[INFO]: MaxSearchTime is set to " + info.MaxSearchTime);
                    
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
                        info.MaxDepth = reqDepth;
                        LogString("[INFO]: MaxDepth is set to " + info.MaxDepth);
                    }

                    hasDepthCommand = true;
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
                    info.MaxSearchTime = MaxSearchTime;
                    info.MaxDepth = MaxDepth;
                }
                else if (param[i] == "wtime")
                {
                    whiteTime = int.Parse(param[i + 1]);
                    hasWhiteTime = true;

                    if (info.Position.ToMove == Color.White)
                    {
                        info.PlayerTimeLeft = whiteTime;

                        LogString("[INFO]: We have " + info.PlayerTimeLeft + " ms left on our clock, should STOP by " + 
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.PlayerTimeLeft)).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000") + 
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
                    }
                }
                else if (param[i] == "btime")
                {
                    blackTime = int.Parse(param[i + 1]);
                    hasBlackTime = true;

                    if (info.Position.ToMove == Color.Black)
                    {
                        info.PlayerTimeLeft = blackTime;
                        LogString("[INFO]: We have " + info.PlayerTimeLeft + " ms left on our clock, should STOP by " + 
                                  (new DateTimeOffset(DateTime.UtcNow.AddMilliseconds(info.PlayerTimeLeft)).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000") + 
                                  ", current time " + ((new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - debug_time_off).ToString("0000000")));
                    }
                }
                else if (param[i] == "winc")
                {
                    whiteInc = int.Parse(param[i + 1]);
                }
                else if (param[i] == "binc")
                {
                    blackInc = int.Parse(param[i + 1]);
                }
            }

            if (hasWhiteTime)
            {
                //  We will need to reduce our search time if we have less time than info.MaxSearchTime
                if (info.Position.ToMove == Color.White && hasMoveTime && (whiteTime < info.MaxSearchTime))
                {
                    //  If we have more than 5 seconds, we can use all of the time we have above that.
                    //  Otherwise, just use our remaining time.
                    int newTime = whiteTime - SimpleSearch.TimerTickInterval;
                    if (whiteTime > SearchLowTimeThreshold)
                    {
                        newTime -= SearchLowTimeThreshold;
                    }

                    LogString("[INFO]: only have " + whiteTime + "ms left <= MaxSearchTime: " + info.MaxSearchTime + ", setting time to " + newTime + "ms");
                    info.MaxSearchTime = newTime;
                }
            }

            if (hasBlackTime)
            {
                if (info.Position.ToMove == Color.Black && hasMoveTime && (blackTime < info.MaxSearchTime))
                {
                    //  If we have more than 5 seconds, we can use all of the time we have above that.
                    //  Otherwise, just use our remaining time.
                    int newTime = blackTime - SimpleSearch.TimerTickInterval;
                    if (blackTime > SearchLowTimeThreshold)
                    {
                        newTime -= SearchLowTimeThreshold;
                    }

                    LogString("[INFO]: only have " + blackTime + "ms left <= MaxSearchTime: " + info.MaxSearchTime + ", setting time to " + newTime + "ms");
                    info.MaxSearchTime = newTime;
                }
            }

            if (!hasMoveTime && (info.MaxSearchTime == SearchConstants.DefaultSearchTime) && hasWhiteTime && hasBlackTime)
            {
                int inc = this.info.Position.ToMove == Color.White ? whiteInc : blackInc;
                int playerTime = this.info.Position.ToMove == 0 ? whiteTime : blackTime;
                long newSearchTime = inc + (playerTime / Math.Max(20, 20 - this.info.Position.Moves.Count));

                info.MaxSearchTime = newSearchTime;
                LogString("[INFO]: setting search time to " + (newSearchTime - inc) + " + " + inc + " = " + newSearchTime);
            }

            DoSearch(hasDepthCommand);
        }

        private void DoSearch(bool hasDepthCommand)
        {
            SimpleSearch.StartSearching(ref info, !hasDepthCommand);
            LogString("[INFO]: DoSearch task returned from call to StartSearching");
            //info.OnSearchFinish?.Invoke(info);
        }

        private void OnSearchDone(SearchInformation info)
        {
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
    }
}
