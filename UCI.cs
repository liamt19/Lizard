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

namespace LTChess.Core
{
    public class UCI
    {
        public SearchInformation info;

        public const string Filename = @".\ucilog.txt";
        public const string FilenameLast = @".\ucilog_last.txt";

        public UCI()
        {
            info = new SearchInformation(new Position(), DefaultSearchDepth);
            info.OnDepthFinish += OnSearchDone;
            if (File.Exists(Filename))
            {
                File.Move(Filename, FilenameLast, true);
            }
        }

        public void SendString(string s)
        {
            Console.WriteLine(s);
            LogString("[OUT]: " + s);
        }

        public static void LogString(string s)
        {
            using StreamWriter file = new(Filename, append: true);
            file.WriteLine(s);
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
            SendString("id name LTChess 5.0");
            SendString("id author Liam McGuire");
            SendString("option name hello type spin default 2 min 1 max 3");
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
                    if (param[0] == "startpos")
                    {
                        LogString("Set position to " + InitialFEN);
                        info.Position = new Position();
                        if (param.Length > 1 && param[1] == "moves")
                        {
                            for (int i = 2; i < param.Length; i++)
                            {
                                info.Position.TryMakeMove(param[i]);
                            }

                            LogString("New FEN is " + info.Position.GetFEN());
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
                                        LogString("Failed doing extra moves! '" + param[j] + "' didn't work with FEN " + info.Position.GetFEN());
                                    }
                                }

                                LogString("New FEN is " + info.Position.GetFEN());
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
                            LogString("Set position to " + fen);
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
            }
        }

        private void OnSearchDone()
        {
            SendEval(info);
        }

        private void SendEval(SearchInformation info)
        {
            SendString(FormatSearchInformation(info));
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
            //  Default to 5
            info.MaxDepth = 5;
            LogString("[INFO]: Got 'go' command");


            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.MaxSearchTime = long.Parse(param[i + 1]);
                    LogString("[INFO]: MaxSearchTime is set to " + info.MaxSearchTime);
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
                    info.MaxNodes = ulong.MaxValue - 1;
                    info.MaxSearchTime = MaxSearchTime;
                }
            }

            DoSearch();
        }

        private void DoSearch()
        {
            Task.Run(() =>
            {
                SimpleSearch.StartSearching(ref info);
                SendString("bestmove " + info.BestMove.ToString());
                LogString("[INFO]: sent 'bestmove " + info.BestMove.ToString() + "'");
            });
        }
    }
}
