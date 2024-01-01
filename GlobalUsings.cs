//  Thanks C# 10!

global using LTChess.Logic.Core;
global using LTChess.Logic.Data;
global using LTChess.Logic.Search;
global using LTChess.Logic.Transposition;
global using LTChess.Logic.UCI;
global using LTChess.Logic.Util;

global using static LTChess.Logic.Data.Bound;
global using static LTChess.Logic.Data.Color;
global using static LTChess.Logic.Data.Piece;
global using static LTChess.Logic.Data.PrecomputedData;
global using static LTChess.Logic.Data.RunOptions;
global using static LTChess.Logic.Data.Squares;
global using static LTChess.Logic.Magic.MagicBitboards;
global using static LTChess.Logic.NN.NNRunOptions;
global using static LTChess.Logic.Search.Evaluation;
global using static LTChess.Logic.Search.EvaluationConstants;
global using static LTChess.Logic.Search.SearchConstants;
global using static LTChess.Logic.Search.SearchOptions;
global using static LTChess.Logic.Threads.SearchThreadPool;
global using static LTChess.Logic.Util.ExceptionHandling;
global using static LTChess.Logic.Util.Interop;
global using static LTChess.Logic.Util.Utilities;

global using Color = LTChess.Logic.Data.Color;
global using Debug = System.Diagnostics.Debug;
global using Stopwatch = System.Diagnostics.Stopwatch;
global using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
global using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
global using Unsafe = System.Runtime.CompilerServices.Unsafe;
