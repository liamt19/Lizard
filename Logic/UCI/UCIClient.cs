using System.Reflection;

using Lizard.Logic.NN;
using Lizard.Logic.Threads;

namespace Lizard.Logic.UCI
{
    public unsafe class UCIClient
    {
        private readonly Position pos;
        private SearchInformation info;
        private readonly ThreadSetup setup;

        private static Dictionary<string, UCIOption> Options;

        public static bool Active = false;

        public UCIClient()
        {
            ProcessUCIOptions();

            setup = new ThreadSetup();
            pos = new Position(owner: SearchPool.MainThread);
            info = new SearchInformation(pos);
            info.OnDepthFinish = SearchThreadPool.OnDepthDone;
            info.OnSearchFinish = SearchThreadPool.OnSearchDone;
        }

        /// <summary>
        /// Writes the string <paramref name="s"/> to standard output, which will be received by chess UCI's.
        /// </summary>
        public static void SendString(string s)
        {
            Console.WriteLine(s);
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
            SendString("id name Lizard " + EngineBuildVersion + " DEV");
#else
            SendString("id name Lizard " + EngineBuildVersion);
#endif
            SendString("id author Liam McGuire");
            SendString("info string Using Bucketed768 evaluation.");

            foreach (string k in Options.Keys)
            {
                SendString(Options[k].ToString());
            }
            SendString("uciok");

            //  In case a "ucinewgame" isn't sent for the first game
            HandleNewGame();
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
                    SendString("readyok");
                }
                else if (cmd == "ucinewgame")
                {
                    HandleNewGame();
                }
                else if (cmd == "position")
                {
                    pos.IsChess960 = UCI_Chess960;

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

                                setup.SetupMoves.Add(m);
                            }
                        }
                    }
                    else
                    {
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

                                    setup.SetupMoves.Add(m);
                                }

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
                            info.Position.LoadFromFEN(fen);
                        }

                    }

                    NNUE.RefreshAccumulator(info.Position);
                }
                else if (cmd == "go")
                {
                    SearchPool.StopThreads = false;
                    HandleGo(param);
                }
                else if (cmd == "stop")
                {
                    SearchPool.StopThreads = true;
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
                    catch (Exception e) { }

                }
                else if (cmd == "tune")
                {
                    PrintSPSAParams();
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
            if (TimeManager.IsRunning)
            {
                return;
            }

            //  Assume that we can search infinitely, and let the UCI's "go" parameters constrain us accordingly.
            info.NodeLimit = MaxSearchNodes;
            TimeManager.HardTimeLimit = MaxSearchTime;
            info.DepthLimit = MaxDepth;

            bool isMoveTimeCommand = false;
            bool hasPlayerTime = false;

            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    TimeManager.MoveTime = int.Parse(param[i + 1]);

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
                        info.DepthLimit = reqDepth;
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
                        info.NodeLimit = reqNodes;
                    }
                }
                else if (param[i] == "infinite")
                {
                    Assert(info.NodeLimit == MaxSearchNodes, $"go infinite should have MaxNodes == {MaxSearchNodes}, but it was {info.NodeLimit}");
                    Assert(TimeManager.HardTimeLimit == MaxSearchTime, $"go infinite should have MaxSearchTime == {MaxSearchTime}, but it was {TimeManager.HardTimeLimit}");
                    Assert(info.DepthLimit == MaxDepth, $"go infinite should have MaxDepth == {MaxDepth}, but it was {info.DepthLimit}");

                    info.NodeLimit = MaxSearchNodes;
                    TimeManager.HardTimeLimit = MaxSearchTime;
                    info.DepthLimit = MaxDepth;
                }
                else if ((param[i] == "wtime" && info.Position.ToMove == Color.White) ||
                         (param[i] == "btime" && info.Position.ToMove == Color.Black))
                {
                    TimeManager.PlayerTime = int.Parse(param[i + 1]);
                    hasPlayerTime = true;
                }
                else if ((param[i] == "winc" && info.Position.ToMove == Color.White) ||
                         (param[i] == "binc" && info.Position.ToMove == Color.Black))
                {
                    TimeManager.PlayerIncrement = int.Parse(param[i + 1]);
                }
                else if (param[i] == "movestogo")
                {
                    TimeManager.MovesToGo = int.Parse(param[i + 1]);
                }
            }

            //  If we weren't told to search for a specific time (no "movetime" and not "infinite"),
            //  then we make one ourselves
            if (!isMoveTimeCommand && hasPlayerTime)
            {
                TimeManager.MakeMoveTime();
            }

            SearchPool.StartSearch(info.Position, ref info, setup);
        }


        private static void HandleNewGame()
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
                                    }
                                    else if (opt.Name == nameof(Hash))
                                    {
                                        TranspositionTable.Initialize(newValue);
                                    }
                                    else
                                    {
                                        opt.FieldHandle.SetValue(null, newValue);
                                    }
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                return;
                            }

                        }
                    }
                    catch (Exception e) { }

                    return;
                }
            }
        }

        public static void ProcessUCIOptions()
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

            //SetSPSAOutputParams();

            Options[nameof(Threads)].SetMinMax(1, 512);
            Options[nameof(MultiPV)].SetMinMax(1, 5);
            Options[nameof(Hash)].SetMinMax(1, 1048576);

            Options[nameof(SingularExtensionsMinDepth)].SetMinMax(2, 10);
            Options[nameof(SingularExtensionsNumerator)].AutoMinMax();
            Options[nameof(SingularExtensionsBeta)].AutoMinMax();
            Options[nameof(SingularExtensionsDepthAugment)].SetMinMax(-3, 1);

            Options[nameof(NMPMinDepth)].SetMinMax(1, 9);
            Options[nameof(NMPReductionBase)].SetMinMax(1, 9);
            Options[nameof(NMPReductionDivisor)].SetMinMax(1, 9);
            Options[nameof(NMPEvalDivisor)].AutoMinMax();
            Options[nameof(NMPEvalMin)].SetMinMax(0, 6); 

            Options[nameof(ReverseFutilityPruningMaxDepth)].SetMinMax(4, 12);
            Options[nameof(ReverseFutilityPruningPerDepth)].AutoMinMax();

            Options[nameof(ProbCutMinDepth)].SetMinMax(1, 8);
            Options[nameof(ProbCutBeta)].AutoMinMax();
            Options[nameof(ProbCutBetaImproving)].AutoMinMax();

            Options[nameof(LMRExtensionThreshold)].AutoMinMax();
            Options[nameof(LMRExchangeBase)].AutoMinMax();

            Options[nameof(HistoryReductionMultiplier)].SetMinMax(1, 9);

            Options[nameof(FutilityExchangeBase)].AutoMinMax();

            Options[nameof(ExtraCutNodeReductionMinDepth)].SetMinMax(2, 10);
            Options[nameof(AspirationWindowMargin)].AutoMinMax();

            Options[nameof(HistoryCaptureBonusMargin)].AutoMinMax();

            Options[nameof(OrderingGivesCheckBonus)].AutoMinMax();
            Options[nameof(OrderingVictimValueMultiplier)].AutoMinMax();

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

        public static void PrintSPSAParams()
        {
            foreach (var optName in Options.Keys)
            {
                if (optName == nameof(SearchOptions.Threads) || optName == nameof(SearchOptions.MultiPV) || optName == nameof(SearchOptions.Hash) || optName == nameof(SearchOptions.UCI_Chess960))
                {
                    continue;
                }

                var opt = Options[optName];
                SendString(opt.GetSPSAFormat());
            }
        }

        private static void SetSPSAOutputParams()
        {
            string output =
                "" +
                "SingularExtensionsMinDepth, 7\r\nSingularExtensionsNumerator, 9\r\nSingularExtensionsBeta, 22\r\nSingularExtensionsDepthAugment, -1\r\nNMPMinDepth, 6\r\nNMPReductionBase, 5\r\nNMPReductionDivisor, 5\r\nReverseFutilityPruningMaxDepth, 7\r\nReverseFutilityPruningPerDepth, 47\r\nProbCutBeta, 191\r\nProbCutBetaImproving, 100\r\nProbCutMinDepth, 2\r\nLMRExtensionThreshold, 131\r\nLMRExchangeBase, 216\r\nHistoryReductionMultiplier, 3\r\nFutilityExchangeBase, 181\r\nExtraCutNodeReductionMinDepth, 5\r\nAspirationWindowMargin, 11\r\nHistoryCaptureBonusMargin, 158\r\nOrderingGivesCheckBonus, 10345\r\nOrderingVictimValueMultiplier, 14\r\nOrderingHistoryDivisor, 11\r\nStatBonusMult, 170\r\nStatBonusSub, 95\r\nStatBonusMax, 1822\r\nStatMalusMult, 466\r\nStatMalusSub, 97\r\nStatMalusMax, 1787\r\nSEEValue_Pawn, 112\r\nSEEValue_Knight, 794\r\nSEEValue_Bishop, 868\r\nSEEValue_Rook, 1324\r\nSEEValue_Queen, 2107\r\nValuePawn, 199\r\nValueKnight, 920\r\nValueBishop, 1058\r\nValueRook, 1553\r\nValueQueen, 3127" +
                "";

            var lines = output.Split("\r\n");

            foreach (string line in lines)
            {
                var splits = line.Split(", ");
                Options[splits[0]].DefaultValue = splits[1];
                Options[splits[0]].RefreshBackingField();
            }
        }
    }
}
