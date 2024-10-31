using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

using Lizard.Logic.Magic;
using Lizard.Logic.Threads;

namespace Lizard.Logic.Util
{
    public static class Utilities
    {
        public const string EngineBuildVersion = "11.0.9";

        public const int NormalListCapacity = 128;
        public const int MoveListSize = 256;

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


        public const string InitialFEN = @"rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


        public static int ProcessID => Environment.ProcessId;

        public const bool NO_LOG_FILE = true;

        /// <summary>
        /// Writes the string <paramref name="s"/> to the debugger, and to the log file if in UCI mode or to the console otherwise.
        /// </summary>
        public static void Log(string s)
        {
            if (!UCIClient.Active)
            {
                Console.WriteLine(s);
            }
            else if (!NO_LOG_FILE)
            {
#pragma warning disable CS0162 // Unreachable code detected
                UCIClient.LogString("[LOG]: " + s);
#pragma warning restore CS0162 // Unreachable code detected
            }

            Debug.WriteLine(s);
        }


        public static string GetCompilerInfo()
        {
            StringBuilder sb = new StringBuilder();

#if DATAGEN
            sb.Append("Datagen ");
#endif

#if DEBUG
            sb.Append("Debug ");
#endif


            sb.Append(IsAOTAttribute.IsAOT() ? "AOT " : string.Empty);

            sb.Append(HasSkipInit ? "SkipInit " : string.Empty);

            sb.Append(Avx2.IsSupported ? "Avx2 " : string.Empty);

#if AVX512
            sb.Append(Avx512BW.IsSupported ? "Avx512=(supported, used) " : "Avx512=(unsupported, used!) ");
#else
            sb.Append(Avx512BW.IsSupported ? "Avx512=(supported, unused!) " : "Avx512=(unsupported, unused) ");
#endif

            sb.Append(Bmi2.IsSupported ? "Bmi2 " : string.Empty);
            sb.Append(Sse3.IsSupported ? "Sse3 " : string.Empty);
            sb.Append(Sse42.IsSupported ? "Sse42 " : string.Empty);
            sb.Append(Sse.IsSupported ? "Prefetch " : string.Empty);
            sb.Append(Popcnt.X64.IsSupported ? "Popcount " : string.Empty);
            sb.Append(Bmi2.X64.IsSupported && MagicBitboards.UsePext ? "Pext " : string.Empty);
            sb.Append(Lzcnt.X64.IsSupported ? "Lzcnt " : string.Empty);

            return sb.ToString();
        }


        /// <summary>
        /// Returns the <see cref="Direction"/> that the <paramref name="color"/> pawns move in, white pawns up, black pawns down.
        /// </summary>
        [MethodImpl(Inline)]
        public static int ShiftUpDir(int color) => (color == Color.White) ? Direction.NORTH : Direction.SOUTH;


        /// <summary>
        /// Shifts the bits in <paramref name="b"/> in the direction <paramref name="dir"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong Shift(int dir, ulong b)
        {
            return dir switch
            {
                Direction.NORTH => b << 8,
                Direction.SOUTH => b >> 8,
                Direction.NORTH + Direction.NORTH => b << 16,
                Direction.SOUTH + Direction.SOUTH => b >> 16,
                Direction.EAST => (b & ~FileHBB) << 1,
                Direction.WEST => (b & ~FileABB) >> 1,
                Direction.NORTH_EAST => (b & ~FileHBB) << 9,
                Direction.NORTH_WEST => (b & ~FileABB) << 7,
                Direction.SOUTH_EAST => (b & ~FileHBB) >> 7,
                Direction.SOUTH_WEST => (b & ~FileABB) >> 9,
                _ => 0
            };
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
        public static int Not(int color)
        {
            return color ^ 1;
        }


        /// <summary>
        /// Returns the name of the <paramref name="color"/>.
        /// </summary>
        public static string ColorToString(int color)
        {
            return color switch
            {
                Color.White => nameof(Color.White),
                Color.Black => nameof(Color.Black),
                _           => "None"
            };
        }


        /// <summary>
        /// Returns the numerical value of the <paramref name="colorName"/>.
        /// </summary>
        public static int StringToColor(string colorName)
        {
            return colorName.ToLower() switch
            {
                "white" => Color.White,
                "black" => Color.Black,
                _       => Color.ColorNB
            };
        }


        /// <summary>
        /// Returns the name of the piece of type <paramref name="n"/>.
        /// </summary>
        public static string PieceToString(int n)
        {
            return n switch
            {
                Piece.Pawn   => nameof(Piece.Pawn),
                Piece.Knight => nameof(Piece.Knight),
                Piece.Bishop => nameof(Piece.Bishop),
                Piece.Rook   => nameof(Piece.Rook),
                Piece.Queen  => nameof(Piece.Queen),
                Piece.King   => nameof(Piece.King),
                _            => "None"
            };
        }


        /// <summary>
        /// Returns the type of the piece called <paramref name="pieceName"/>.
        /// </summary>
        public static int StringToPiece(string pieceName)
        {
            return pieceName.ToLower() switch
            {
                "pawn"   => Piece.Pawn,
                "knight" => Piece.Knight,
                "bishop" => Piece.Bishop,
                "rook"   => Piece.Rook,
                "queen"  => Piece.Queen,
                "king"   => Piece.King,
                _        => Piece.None
            };
        }


        /// <summary>
        /// Returns the first letter of the name of the piece of type <paramref name="pieceType"/>, so PieceToFENChar(0 [Piece.Pawn]) returns 'P'.
        /// </summary>
        public static char PieceToFENChar(int pieceType)
        {
            return pieceType switch
            {
                Piece.Pawn   => 'P',
                Piece.Knight => 'N',
                Piece.Bishop => 'B',
                Piece.Rook   => 'R',
                Piece.Queen  => 'Q',
                Piece.King   => 'K',
                _            => ' '
            };
        }


        /// <summary>
        /// Returns the numerical piece type of the piece given its FEN character <paramref name="fenChar"/>.
        /// </summary>
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
                _   => Piece.None
            };
        }


        /// <summary>
        /// Returns a random ulong using the Random instance <paramref name="random"/>.
        /// </summary>
        public static ulong NextUlong(this Random random)
        {
            Span<byte> arr = new byte[8];
            random.NextBytes(arr);

            return BitConverter.ToUInt64(arr);
        }


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
        public static string IndexToString(int idx)
        {
            return "" + GetFileChar(GetIndexFile(idx)) + (GetIndexRank(idx) + 1);
        }


        /// <summary>
        /// Returns the index of the square <paramref name="s"/>, which should look like "a1" or "e4".
        /// </summary>
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
                    int pc = bb.GetColorAtIndex(idx);

                    if (pc == White)
                    {
                        char c = PieceToFENChar(pt);
                        sb.Append(char.ToUpper(c) + " ");
                    }
                    else if (pc == Black)
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
            double nodes = thisThread.AssocPool.GetNodeCount();
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
                if (UCI_ShowWDL)
                {
                    if (moveScore > ScoreWin)
                        sb.Append(" wdl 1000 0 0");
                    else if (moveScore < -ScoreWin)
                        sb.Append(" wdl 0 0 1000");
                    else
                    {
                        (var win, var loss) = WDL.MaterialModel(moveScore, info.Position);
                        sb.Append($" wdl {win} {1000 - win - loss} {loss}");
                    }
                }
                sb.Append(" nodes " + nodes);
                sb.Append(" nps " + nodesPerSec);
                sb.Append(" hashfull " + thisThread.TT.GetHashFull());

                sb.Append(" pv");
                for (int j = 0; j < MaxPly; j++)
                {
                    if (rm.PV[j] == Move.Null)
                    {
                        break;
                    }

                    sb.Append(" " + rm.PV[j].ToString(info.Position.IsChess960));
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
        public static string FormatMoveScore(int score)
        {
            if (Evaluation.IsScoreMate(score))
            {
                return (score > 0) ? ("mate " + ( ScoreMate - score + 1) / 2) :
                                     ("mate " + (-ScoreMate - score    ) / 2);
            }

            const double NormalizeEvalFactor = 252;
            return "cp " + (int)(((double)score * 100) / NormalizeEvalFactor);
        }


        /// <summary>
        /// Sorts the <paramref name="items"/> between the starting index <paramref name="offset"/> and last index <paramref name="end"/>
        /// using <typeparamref name="T"/>'s CompareTo method. This is done in a stable manner so long as the CompareTo method returns
        /// 0 (or negative numbers) for items with identical values.
        /// <para></para>
        /// This is a rather inefficient algorithm ( O(n^2)? ) but for small amounts of <paramref name="items"/> or small ranges 
        /// of [<paramref name="offset"/>, <paramref name="end"/>] this works well enough.
        /// </summary>
        public static void StableSort(List<RootMove> items, int offset = 0, int end = -1)
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



        public static unsafe void FillWithScharnaglNumber(int n, int* types)
        {
            int n2 = n / 4;
            int b1 = n % 4;

            int n3 = n2 / 4;
            int b2 = n2 % 4;

            int n4 = n3 / 6;
            int  q = n3 % 6;

            (int knight1, int knight2) = N5N[n4];

            types[b1 * 2 + 1] = Bishop;
            types[b2 * 2 + 0] = Bishop;

            PlaceInSpot(Queen, q);

            PlaceInSpot(Knight, knight1);
            PlaceInSpot(Knight, knight2);

            PlaceInSpot(Rook);
            PlaceInSpot(King);
            PlaceInSpot(Rook);

            void PlaceInSpot(int pt, int skip = 0)
            {
                int skips = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (types[i] == 0 && skips++ >= skip)
                    {
                        types[i] = pt;
                        break;
                    }
                }
            }

        }

        private static readonly (int a, int b)[] N5N =
        [
            (0, 0), (0, 1), (0, 2), (0, 3),
            (1, 1), (1, 2), (1, 3),
            (2, 2), (2, 3),
            (3, 3),
        ];


        public static int AsInt(this bool v) => v ? 1 : 0;
        public static bool AsBool(this int v) => v != 0;
    }


}
