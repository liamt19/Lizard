//  Thanks C# 10!

global using Lizard.Logic.Core;
global using Lizard.Logic.Data;
global using Lizard.Logic.Search;
global using Lizard.Logic.Transposition;
global using Lizard.Logic.UCI;
global using Lizard.Logic.Util;

global using static Lizard.Logic.Data.Bound;
global using static Lizard.Logic.Data.Color;
global using static Lizard.Logic.Data.Piece;
global using static Lizard.Logic.Data.PrecomputedData;
global using static Lizard.Logic.Data.RunOptions;
global using static Lizard.Logic.Data.Squares;
global using static Lizard.Logic.Magic.MagicBitboards;
global using static Lizard.Logic.Search.Evaluation;
global using static Lizard.Logic.Search.EvaluationConstants;
global using static Lizard.Logic.Search.SearchConstants;
global using static Lizard.Logic.Search.SearchOptions;
global using static Lizard.Logic.Threads.SearchThreadPool;
global using static Lizard.Logic.Util.ExceptionHandling;
global using static Lizard.Logic.Util.Interop;
global using static Lizard.Logic.Util.Utilities;

global using Color = Lizard.Logic.Data.Color;
global using Debug = System.Diagnostics.Debug;
global using Stopwatch = System.Diagnostics.Stopwatch;
global using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
global using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
global using Unsafe = System.Runtime.CompilerServices.Unsafe;
