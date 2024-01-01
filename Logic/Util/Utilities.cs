using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

using LTChess.Logic.Threads;

namespace LTChess.Logic.Util
{
    public static class Utilities
    {
        public const int NumPieces = 6;

        public const int IndexTop = 7;
        public const int IndexBot = 0;
        public const int IndexLeft = 0;
        public const int IndexRight = 7;
        public const string InitialFEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public const string EngineBuildVersion = "9.3.1";
        public const string EngineTagLine = "A (somewhat) fresh start!";

        public const int MaxListCapacity = 512;
        public const int ExtendedListCapacity = 256;
        public const int NormalListCapacity = 128;
        public const int MoveListSize = 256;
        public const int LSBEmpty = 64;
        public const int MaxDepth = 64;

        public const int MaxPly = 256;

        /// <summary>
        /// The maximum ply that SimpleSearch's SearchStackEntry* array can be indexed at.
        /// <br></br>
        /// The array actually contains MaxPly == 256 entries, but the first 10 of them are off limits to
        /// prevent accidentally indexing memory before the stack.
        /// </summary>
        public const int MaxSearchStackPly = 256 - 10;

        public const nuint AllocAlignment = 64;

        public const ulong Border = 0xFF818181818181FF;
        public const ulong PawnPromotionSquares = 0xFF000000000000FF;
        public const ulong Corners = 0x8100000000000081;

        public const ulong Empty = 0UL;
        public const ulong AllSquares = ~0UL;
        public const ulong DarkSquares = 0xAA55AA55AA55AA55UL;
        public const ulong LightSquares = 0x55AA55AA55AA55AAUL;

        public const ulong FileABB = 0x0101010101010101UL;
        public const ulong FileBBB = FileABB << 1;
        public const ulong FileCBB = FileABB << 2;
        public const ulong FileDBB = FileABB << 3;
        public const ulong FileEBB = FileABB << 4;
        public const ulong FileFBB = FileABB << 5;
        public const ulong FileGBB = FileABB << 6;
        public const ulong FileHBB = FileABB << 7;

        public const ulong Rank1BB = 0xFF;
        public const ulong Rank2BB = Rank1BB << (8 * 1);
        public const ulong Rank3BB = Rank1BB << (8 * 2);
        public const ulong Rank4BB = Rank1BB << (8 * 3);
        public const ulong Rank5BB = Rank1BB << (8 * 4);
        public const ulong Rank6BB = Rank1BB << (8 * 5);
        public const ulong Rank7BB = Rank1BB << (8 * 6);
        public const ulong Rank8BB = Rank1BB << (8 * 7);

        public const ulong QueenSide = FileABB | FileBBB | FileCBB | FileDBB;
        public const ulong CenterFiles = FileCBB | FileDBB | FileEBB | FileFBB;
        public const ulong KingSide = FileEBB | FileFBB | FileGBB | FileHBB;
        public const ulong Center = (FileDBB | FileEBB) & (Rank4BB | Rank5BB);

        public const ulong MobilityInner = 0x3C3C3C3C0000;
        public const ulong MobilityOutter = AllSquares ^ MobilityInner;

        public static readonly ulong[] LowRanks = new ulong[]
        {
            Rank2BB | Rank3BB,
            Rank7BB | Rank6BB,
        };

        public const ulong WhiteKingsideMask = (1UL << F1) | (1UL << G1);
        public const ulong WhiteQueensideMask = (1UL << B1) | (1UL << C1) | (1UL << D1);
        public const ulong BlackKingsideMask = (1UL << F8) | (1UL << G8);
        public const ulong BlackQueensideMask = (1UL << B8) | (1UL << C8) | (1UL << D8);

        /// <summary>
        /// A mask of the ranks that outpost squares can be on for each color
        /// </summary>
        public static readonly ulong[] OutpostSquares = { Rank4BB | Rank5BB | Rank6BB, Rank3BB | Rank4BB | Rank5BB };



        /// <summary>
        /// Set to true if the GC is running in server mode instead of workstation mode.
        /// </summary>
        public static readonly bool ServerGC = System.Runtime.GCSettings.IsServerGC;

        /// <summary>
        /// If true, will block all output to the console and log file.
        /// </summary>
        public static bool BlockOutputForJIT = false;



        /// <summary>
        /// Set to true if multiple instances of this engine are running, in which case the log file won't be written to.
        /// </summary>
        public static bool IsRunningConcurrently = false;

        /// <summary>
        /// The total number of processes running, which will be > 1 if there are duplicates.
        /// </summary>
        public static int ConcurrencyCount = 0;

        public static int ProcessID;


        /// <summary>
        /// The time in milliseconds that this engine instance was started.
        /// </summary>
        public static readonly long StartTimeMS = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();


        public const bool NO_LOG_FILE = true;

        /// <summary>
        /// Writes the string <paramref name="s"/> to the debugger, and to the log file if in UCI mode or to the console otherwise.
        /// </summary>
        [MethodImpl(Inline)]
        public static void Log(string s)
        {
            if (BlockOutputForJIT)
            {
                return;
            }

            if (!UCIClient.Active)
            {
                Console.WriteLine(s);
            }
            else if (!NO_LOG_FILE)
            {
                UCIClient.LogString("[LOG]: " + s);
            }

            Debug.WriteLine(s);
        }


        /// <summary>
        /// Forces a blocking and compacting garbage collection. 
        /// <para></para>
        /// I don't care what StackOverflow has to say about calling <see cref="GC.Collect"/> yourself.
        /// There is no good reason to leave an additional 190 MB of RAM in use because the GC hasn't decided to deal with it yet.
        /// <br></br>
        /// I'd rather force it to collect during an opponent's turn than have it decide to collect during our own.
        /// </summary>
        [MethodImpl(Inline)]
        public static void ForceGC()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }


        /// <summary>
        /// If there are multiple instances of this engine running, we won't write to the ucilog file.
        /// <br></br>
        /// This uses a FileStream to access it and a mutex to make writes atomic, so having multiple
        /// processes all doing that at the same time is needlessly risky.
        /// </summary>
        public static void CheckConcurrency()
        {
            Process thisProc = Process.GetCurrentProcess();
            ProcessID = thisProc.Id;

            var selfProcs = Process.GetProcesses().Where(x => x.ProcessName == thisProc.ProcessName).ToList();

            int concurrencyCount = 0;
            int duplicates = 0;

            var thisTime = thisProc.StartTime.Ticks;

            for (int i = 0; i < selfProcs.Count; i++)
            {
                try
                {
                    //  Windows doesn't like you touching the "System Idle Process" (0) or "System" (4)
                    //  and will throw an error if you try to get their MainModules,
                    //  so in case you prefer to rename the engine binary to "Idle" this will hopefully avoid that issue :)
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (selfProcs[i].Id == 0 || selfProcs[i].Id == 4))
                    {
                        continue;
                    }

                    //  Ensure that the processes are exactly the same as this one.
                    //  This checks their entire path, since multiple engine instance concern is
                    //  only if they are started from the same path.
                    if (selfProcs[i].MainModule.FileName != thisProc.MainModule.FileName)
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    //  This will be a Win32Exception for trying to enumerate the modules of a SYSTEM process
                    concurrencyCount++;
                    continue;
                }

                duplicates++;

                if (selfProcs[i].StartTime.Ticks < thisTime)
                {
                    //  This instance was launched after another, so increase this one's count
                    concurrencyCount++;
                }
            }

            //  If this is the only instance, this should be 0
            if (concurrencyCount != 0)
            {
                Log("Running concurrently! (" + concurrencyCount + " other process" + (concurrencyCount > 1 ? "es" : string.Empty) + ")");
                IsRunningConcurrently = true;
            }
        }


        public static string GetCompilerInfo()
        {
            StringBuilder sb = new StringBuilder();

#if DEV
            sb.Append("DEV ");
#endif

#if DEBUG
            sb.Append("Debug ");
#endif

#if PUBLISH_AOT
            sb.Append("AOT ");
#endif

            if (HasSkipInit)
            {
                sb.Append("SkipInit ");
            }

            sb.Append(Avx2.IsSupported ? "Avx2 " : string.Empty);
            sb.Append(AvxVnni.IsSupported ? "AvxVnni " : string.Empty);
            sb.Append(Bmi2.IsSupported ? "Bmi2 " : string.Empty);
            sb.Append(Sse3.IsSupported ? "Sse3 " : string.Empty);
            sb.Append(Sse42.IsSupported ? "Sse42 " : string.Empty);

            sb.Append(Sse.IsSupported ? "Prefetch " : string.Empty);
            sb.Append(Popcnt.X64.IsSupported ? "Popcount " : string.Empty);
#if PEXT
            //  Magic bitboards will only use Pext if "PEXT" is also defined
            sb.Append(Bmi2.X64.IsSupported ? "Pext " : string.Empty);
#endif
            sb.Append(Lzcnt.X64.IsSupported ? "Lzcnt " : string.Empty);

            return sb.ToString();
        }

        public static string FormatCurrentTime() => (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() - StartTimeMS).ToString("0000000");

        public static class Direction
        {
            public const int NORTH = 8;
            public const int EAST = 1;
            public const int SOUTH = -NORTH;
            public const int WEST = -EAST;

            public const int NORTH_EAST = NORTH + EAST;
            public const int SOUTH_EAST = SOUTH + EAST;
            public const int SOUTH_WEST = SOUTH + WEST;
            public const int NORTH_WEST = NORTH + WEST;
        }

        /// <summary>
        /// Returns the <see cref="Direction"/> that the <paramref name="color"/> pawns move in, white pawns up, black pawns down.
        /// </summary>
        [MethodImpl(Inline)]
        public static int ShiftUpDir(int color) => (color == Color.White) ? Direction.NORTH : Direction.SOUTH;

        /// <summary>
        /// Returns a bitboard with bits set 1 "above" the bits in <paramref name="b"/>.
        /// So Forward(Color.White) with a bitboard that has A2 set will return one with A3 set,
        /// and Forward(Color.Black) returns one with A1 set instead.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Forward(int color, ulong b)
        {
            return (color == Color.White) ? Shift(Direction.NORTH, b) : Shift(Direction.SOUTH, b);
        }

        /// <summary>
        /// Returns a bitboard with bits set 1 "below" the bits in <paramref name="b"/>.
        /// So Backward(Color.White) with a bitboard that has A2 set will return one with A1 set,
        /// and Backward(Color.Black) returns one with A3 set instead.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Backward(int color, ulong b)
        {
            if (color == Color.White)
            {
                return Shift(Direction.SOUTH, b);
            }

            return Shift(Direction.NORTH, b);
        }

        /// <summary>
        /// Shifts the bits in <paramref name="b"/> in the direction <paramref name="dir"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Shift(int dir, ulong b)
        {
            return dir == Direction.NORTH ? b << 8
                : dir == Direction.SOUTH ? b >> 8
                : dir == Direction.NORTH + Direction.NORTH ? b << 16
                : dir == Direction.SOUTH + Direction.SOUTH ? b >> 16
                : dir == Direction.EAST ? (b & ~FileHBB) << 1
                : dir == Direction.WEST ? (b & ~FileABB) >> 1
                : dir == Direction.NORTH_EAST ? (b & ~FileHBB) << 9
                : dir == Direction.NORTH_WEST ? (b & ~FileABB) << 7
                : dir == Direction.SOUTH_EAST ? (b & ~FileHBB) >> 7
                : dir == Direction.SOUTH_WEST ? (b & ~FileABB) >> 9
                : 0;
        }



        /// <summary>
        /// Returns a ulong with bits set along whichever file <paramref name="idx"/> is in.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetFileBB(int idx)
        {
            return FileABB << GetIndexFile(idx);
        }

        /// <summary>
        /// Returns a ulong with bits set along whichever rank <paramref name="idx"/> is on.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong GetRankBB(int idx)
        {
            return Rank1BB << (8 * GetIndexRank(idx));
        }




        /// <summary>
        /// Returns the opposite of <paramref name="color"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int Not(int color)
        {
            return color ^ 1;
        }


        /// <summary>
        /// Returns the name of the <paramref name="color"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static string ColorToString(int color)
        {
            return color switch
            {
                Color.White => nameof(Color.White),
                Color.Black => nameof(Color.Black),
                _ => "None"
            };
        }

        /// <summary>
        /// Returns the numerical value of the <paramref name="colorName"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int StringToColor(string colorName)
        {
            return colorName.ToLower() switch
            {
                "white" => Color.White,
                "black" => Color.Black,
                _ => Color.ColorNB
            };
        }

        /// <summary>
        /// Returns the name of the piece of type <paramref name="n"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static string PieceToString(int n)
        {
            return n switch
            {
                Piece.Pawn => nameof(Piece.Pawn),
                Piece.Knight => nameof(Piece.Knight),
                Piece.Bishop => nameof(Piece.Bishop),
                Piece.Rook => nameof(Piece.Rook),
                Piece.Queen => nameof(Piece.Queen),
                Piece.King => nameof(Piece.King),
                _ => "None"
            };
        }

        /// <summary>
        /// Returns the type of the piece called <paramref name="pieceName"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int StringToPiece(string pieceName)
        {
            return pieceName.ToLower() switch
            {
                "pawn" => Piece.Pawn,
                "knight" => Piece.Knight,
                "bishop" => Piece.Bishop,
                "rook" => Piece.Rook,
                "queen" => Piece.Queen,
                "king" => Piece.King,
                _ => Piece.None
            };
        }

        /// <summary>
        /// Returns the first letter of the name of the piece of type <paramref name="pieceType"/>, so PieceToFENChar(0 [Piece.Pawn]) returns 'P'.
        /// </summary>
        [MethodImpl(Inline)]
        public static char PieceToFENChar(int pieceType)
        {
            return pieceType switch
            {
                Piece.Pawn => 'P',
                Piece.Knight => 'N',
                Piece.Bishop => 'B',
                Piece.Rook => 'R',
                Piece.Queen => 'Q',
                Piece.King => 'K',
                _ => ' '
            };
        }

        /// <summary>
        /// Returns the numerical piece type of the piece given its FEN character <paramref name="fenChar"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int FENToPiece(char fenChar)
        {
            return char.ToLower(fenChar) switch
            {
                'p' => Piece.Pawn,
                'n' => Piece.Knight,
                'b' => Piece.Bishop,
                'r' => Piece.Rook,
                'q' => Piece.Queen,
                'k' => Piece.King,
                _ => Piece.None
            };
        }



        /// <summary>
        /// Returns a random ulong using the Random instance <paramref name="random"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong NextUlong(this Random random)
        {
            Span<byte> arr = new byte[8];
            random.NextBytes(arr);

            return BitConverter.ToUInt64(arr);
        }


        [MethodImpl(Inline)]
        public static bool DirectionOK(int sq, int dir)
        {
            if (sq + dir < A1 || sq + dir > H8)
            {
                //  Make sure we aren't going off the board.
                return false;
            }

            //  The rank and file of (sq + dir) should only change by at most 2 for knight moves,
            //  and 1 for bishop or rook moves.
            int rankDistance = Math.Abs(GetIndexRank(sq) - GetIndexRank(sq + dir));
            int fileDistance = Math.Abs(GetIndexFile(sq) - GetIndexFile(sq + dir));
            return Math.Max(rankDistance, fileDistance) <= 2;
        }


        /// <summary>
        /// Returns the letter of the file numbered <paramref name="fileNumber"/>, so GetFileChar(0) returns 'a'.
        /// </summary>
        [MethodImpl(Inline)]
        public static char GetFileChar(int fileNumber) => (char)(97 + fileNumber);

        /// <summary>
        /// Returns the number of the file with the letter <paramref name="fileLetter"/>, so GetFileInt('a') returns 0.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetFileInt(char fileLetter) => fileLetter - 97;

        /// <summary>
        /// Returns the file (x coordinate) for the index, which is between A=0 and H=7.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetIndexFile(int index) => index & 7;

        /// <summary>
        /// Returns the rank (y coordinate) for the index, which is between 0 and 7.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetIndexRank(int index) => index >> 3;

        /// <summary>
        /// Sets <paramref name="x"/> to the file of <paramref name="index"/>, and <paramref name="y"/> to its rank.
        /// </summary>
        [MethodImpl(Inline)]
        public static void IndexToCoord(in int index, out int x, out int y)
        {
            x = index % 8;
            y = index / 8;
        }

        /// <summary>
        /// Returns the index of the square with the rank <paramref name="x"/> and file <paramref name="y"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int CoordToIndex(int x, int y)
        {
            return (y * 8) + x;
        }

        /// <summary>
        /// Returns the rank and file of the square <paramref name="idx"/>, which looks like "a1" or "e4".
        /// </summary>
        [MethodImpl(Inline)]
        public static string IndexToString(int idx)
        {
            return "" + GetFileChar(GetIndexFile(idx)) + (GetIndexRank(idx) + 1);
        }


        /// <summary>
        /// Returns the index of the square <paramref name="s"/>, which should look like "a1" or "e4".
        /// </summary>
        [MethodImpl(Inline)]
        public static int StringToIndex(string s)
        {
            return CoordToIndex(GetFileInt(s[0]), int.Parse(s[1].ToString()) - 1);
        }



        /// <summary>
        /// Returns a text representation of the board
        /// </summary>
        public static string PrintBoard(Bitboard bb)
        {
            StringBuilder sb = new StringBuilder();

            for (int y = 7; y >= 0; y--)
            {
                sb.Append(y + 1 + " |");
                for (int x = 0; x < 8; x++)
                {
                    int idx = CoordToIndex(x, y);
                    int pt = bb.GetPieceAtIndex(idx);

                    if (bb.IsColorSet(Color.White, idx))
                    {
                        char c = PieceToFENChar(pt);
                        sb.Append(char.ToUpper(c) + " ");
                    }
                    else if (bb.IsColorSet(Color.Black, idx))
                    {
                        char c = PieceToFENChar(pt);
                        sb.Append(char.ToLower(c) + " ");
                    }
                    else
                    {
                        sb.Append(" " + " ");
                    }
                }
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine("|");

            }
            sb.AppendLine("   A B C D E F G H");

            return sb.ToString();
        }


        /// <summary>
        /// Returns a string containing a ToString() version of a list of moves in human readable format.
        /// This would return something similar to "g1f3, e2e4, d2d4".
        /// </summary>
        public static unsafe string Stringify(Move* list, int listSize = 0) => Stringify(new Span<Move>(list, MoveListSize), listSize);

        public static string Stringify(Span<Move> list, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();

            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Equals(Move.Null))
                {
                    break;
                }
                sb.Append(list[i].ToString() + ", ");
            }
            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }


        /// <summary>
        /// Returns a string containing a ToString() version of a list of moves in human readable format.
        /// So instead of seeing "g1f3, e2e4, d2d4" it would show "Nf3, e4, d4".
        /// </summary>
        public static unsafe string Stringify(Move* list, Position position, int listSize = 0) => Stringify(new Span<Move>(list, MoveListSize), position, listSize);

        public static string Stringify(Span<Move> list, Position position, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();
            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Equals(Move.Null))
                {
                    break;
                }
                string s = list[i].ToString(position);
                sb.Append(s + ", ");
            }

            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }



        public static unsafe string Stringify(ScoredMove* list, int listSize = 0) => Stringify(new Span<ScoredMove>(list, MoveListSize), listSize);

        public static string Stringify(Span<ScoredMove> list, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();
            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Move.Equals(Move.Null))
                {
                    break;
                }
                string s = list[i].Move.ToString();
                sb.Append(s + ", ");
            }

            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }


        public static unsafe string Stringify(ScoredMove* list, Position position, int listSize = 0) => Stringify(new Span<ScoredMove>(list, MoveListSize), position, listSize);

        public static string Stringify(Span<ScoredMove> list, Position position, int listSize = 0)
        {
            StringBuilder sb = new StringBuilder();
            int loopMax = (listSize > 0) ? Math.Min(list.Length, listSize) : list.Length;
            for (int i = 0; i < loopMax; i++)
            {
                if (list[i].Move.Equals(Move.Null))
                {
                    break;
                }
                string s = list[i].Move.ToString(position);
                sb.Append(s + ", ");
            }

            if (sb.Length > 3)
            {
                sb.Remove(sb.Length - 2, 2);
            }
            return sb.ToString();
        }


        //  https://stackoverflow.com/questions/18573004/how-to-center-align-arguments-in-a-format-string
        public static string CenteredString(string s, int width)
        {
            if (s.Length >= width)
            {
                return s;
            }

            int leftPadding = (width - s.Length) / 2;
            int rightPadding = width - s.Length - leftPadding;

            return new string(' ', leftPadding) + s + new string(' ', rightPadding);
        }

        public static bool EqualsIgnoreCase(this string s, string other)
        {
            return s.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string s, string other)
        {
            return s.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string s, string other)
        {
            return s.Contains(other, StringComparison.OrdinalIgnoreCase);
        }


        public static string FormatSearchInformationMultiPV(ref SearchInformation info)
        {
            SearchThread thisThread = info.Position.Owner;

            List<RootMove> rootMoves = thisThread.RootMoves;
            int multiPV = Math.Min(MultiPV, rootMoves.Count);

            double time = Math.Max(1, Math.Round(info.TimeManager.GetSearchTime()));
            double nodes = SearchPool.GetNodeCount();
            int nodesPerSec = (int)(nodes / (time / 1000));

            int lastValidScore = 0;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < multiPV; i++)
            {
                RootMove rm = rootMoves[i];
                bool moveSearched = rm.Score != -ScoreInfinite;

                int depth = moveSearched ? thisThread.RootDepth : Math.Max(1, thisThread.RootDepth - 1);
                int moveScore = moveSearched ? rm.Score : rm.PreviousScore;

                if (!moveSearched && i > 0)
                {
                    if (depth == 1)
                    {
                        continue;
                    }

                    if (moveScore == -ScoreInfinite)
                    {
                        //  Much of the time, the 4th/5th and beyond MultiPV moves aren't given a score when the search ends.
                        //  If this is the case, either display the average score if it is lower than the last properly score move,
                        //  or just display the previous score minus one. This isn't technically correct but it is better than showing "-31200"
                        moveScore = Math.Min(lastValidScore - 1, rm.AverageScore);
                    }
                }

                if (moveScore != -ScoreInfinite)
                {
                    lastValidScore = moveScore;
                }

                var score = FormatMoveScore(moveScore);

                sb.Append("info depth " + depth);
                sb.Append(" seldepth " + rm.Depth);
                sb.Append(" multipv " + (i + 1));
                sb.Append(" time " + time);
                sb.Append(" score " + score);
                sb.Append(" nodes " + nodes);
                sb.Append(" nps " + nodesPerSec);
                sb.Append(" hashfull " + TranspositionTable.GetHashFull());

                sb.Append(" pv");
                for (int j = 0; j < MaxPly; j++)
                {
                    if (rm.PV[j] == Move.Null)
                    {
                        break;
                    }

                    sb.Append(" " + rm.PV[j]);
                }


                if (i != multiPV - 1)
                {
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns an appropriately formatted string representing the Score, which is either "cp #" or "mate #".
        /// </summary>
        [MethodImpl(Inline)]
        public static string FormatMoveScore(int score)
        {
            if (Evaluation.IsScoreMate(score))
            {
                //  "mateIn" is returned in plies, but we want it in actual moves
                if (score > 0)
                {
                    return "mate " + ((ScoreMate - score + 1) / 2);
                }
                else
                {
                    return "mate " + ((-ScoreMate - score) / 2);
                }

                //return "mate " + mateIn;
            }
            else
            {
                return "cp " + score;
            }
        }

        /// <summary>
        /// Used in evaluation traces to truncate the evaluation term's Score <paramref name="normalScore"/> to two decimal places,
        /// and to keep that number centered within its column, which has a size of <paramref name="sz"/>.
        /// </summary>
        public static string FormatEvalTerm(double normalScore, int sz = 8)
        {
            return CenteredString(string.Format("{0:N2}", InCentipawns(normalScore)), sz);
        }

        [MethodImpl(Inline)]
        public static double InCentipawns(double score)
        {
            double div = Math.Round(score / 100, 2);
            return div;
        }


        /// <summary>
        /// Sorts the <paramref name="items"/> between the starting index <paramref name="offset"/> and last index <paramref name="end"/>
        /// using <typeparamref name="T"/>'s CompareTo method. This is done in a stable manner so long as the CompareTo method returns
        /// 0 (or negative numbers) for items with identical values.
        /// <para></para>
        /// This is a rather inefficient algorithm ( O(n^2)? ) but for small amounts of <paramref name="items"/> or small ranges 
        /// of [<paramref name="offset"/>, <paramref name="end"/>] this works well enough.
        /// </summary>
        public static void StableSort<T>(ref List<T> items, int offset = 0, int end = -1) where T : IComparable<T>
        {
            if (end == -1)
            {
                end = items.Count;
            }

            for (int i = offset; i < end; i++)
            {
                int best = i;

                for (int j = i + 1; j < end; j++)
                {
                    if (items[j].CompareTo(items[best]) > 0)
                    {
                        best = j;
                    }
                }

                if (best != i)
                {
                    (items[i], items[best]) = (items[best], items[i]);
                }
            }
        }

    }


}
