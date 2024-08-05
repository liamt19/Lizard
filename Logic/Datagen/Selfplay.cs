
//#define DBG
//#define WRITE_PGN
//#define BIND

using System.Runtime.InteropServices;

using static Lizard.Logic.Datagen.DatagenParameters;

using Lizard.Logic.NN;
using Lizard.Logic.Threads;
using System.Diagnostics;

namespace Lizard.Logic.Datagen
{
    public static unsafe class Selfplay
    {
        private static int Seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rand = new(() => new Random(Interlocked.Increment(ref Seed)));

        public static long CumulativeGames = 0;



        public static void RunGames(int gamesToRun = 1, int threadID = 0, bool dfrc = false)
        {
            SearchOptions.Hash = HashSize;

#if BIND
            var thisId = GetCurrentThreadId();
            foreach (ProcessThread td in Process.GetCurrentProcess().Threads)
            {
                if (td.Id == thisId)
                {
                    td.IdealProcessor = (1 << threadID);
                    break;
                }
            }
#endif

            if (dfrc)
            {
                UCI_Chess960 = true;
            }

            SearchThreadPool pool = new SearchThreadPool(1);
            Position pos = new Position(owner: pool.MainThread);
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;
            int bestMoveScore = 0;

            string fName = $"{(dfrc ? "dfrc_" : string.Empty)}{SoftNodeLimit / 1000}k_d{DepthLimit}_{threadID}.bin";

#if PLAINTEXT
            using StreamWriter outputWriter = new StreamWriter(fName, true);
            Span<PlaintextDataFormat> datapoints = stackalloc PlaintextDataFormat[WritableDataLimit];
#else
            using FileStream bfeOutputFileStream = File.Open(fName, FileMode.OpenOrCreate);
            using BinaryWriter outputWriter = new BinaryWriter(bfeOutputFileStream);
            Span<BulletDataFormat> datapoints = stackalloc BulletDataFormat[WritableDataLimit];
#endif

            long totalBadPositions = 0;
            long totalGoodPositions = 0;

            SearchInformation info = new SearchInformation(pos)
            {
                SoftNodeLimit = SoftNodeLimit,
                MaxNodes = HardNodeLimit,
                MaxDepth = DepthLimit,
                OnDepthFinish = null,
                OnSearchFinish = null,
            };

            SearchInformation prelimInfo = new SearchInformation(pos)
            {
                SoftNodeLimit = SoftNodeLimit * 20,
                MaxNodes = HardNodeLimit * 20,
                MaxDepth = Math.Clamp(DepthLimit, 8, 12),
                OnDepthFinish = null,
                OnSearchFinish = null,
            };

            ScoredMove* legalMoves = stackalloc ScoredMove[MoveListSize];
            Random rand = Rand.Value;

            Stopwatch sw = Stopwatch.StartNew();

            int gameNum = 0;
            for (; gameNum < gamesToRun; gameNum++)
            {
                pool.TTable.Clear();
                pool.Clear();

                if (dfrc)
                {
                    pos.SetupForDFRC(rand.Next(0, 960), rand.Next(0, 960));
                    ResetPosition(pos);
                }
                else
                {
                pos.LoadFromFEN(InitialFEN);
                }

                int randMoveCount = rand.Next(MinOpeningPly, MaxOpeningPly + 1);
                for (int i = 0; i < randMoveCount; i++)
                {
                    int l = pos.GenLegal(legalMoves);
                    if (l == 0) { gameNum--; continue; }
                    Move t = legalMoves[rand.Next(0, l)].Move;

                    pos.MakeMove(t);
                }

                if (pos.GenLegal(legalMoves) == 0) { gameNum--; continue; }

                //  Check if the starting position has a reasonable score, and scrap it if it doesn't
                pool.StartSearch(pos, ref prelimInfo);
                pool.BlockCallerUntilFinished();
                if (Math.Abs(pool.GetBestThread().RootMoves[0].Score) >= MaxOpeningScore) { gameNum--; continue; }

                GameResult result = GameResult.Draw;
                int toWrite = 0;
                int filtered = 0;
                int adjudicationCounter = 0;

                while (true)
                {
                    pool.StartSearch(pos, ref info);
                    pool.BlockCallerUntilFinished();

                    bestMove = pool.GetBestThread().RootMoves[0].Move;
                    bestMoveScore = pool.GetBestThread().RootMoves[0].Score;
                    bestMoveScore *= (pos.ToMove == Black ? -1 : 1);


                    if (Math.Abs(bestMoveScore) >= AdjudicateScore)
                    {
                        if (++adjudicationCounter > AdjudicateMoves)
                        {
                            result = (bestMoveScore > 0) ? GameResult.WhiteWin : GameResult.BlackWin;
                            break;
                        }
                    }
                    else
                        adjudicationCounter = 0;


                    bool inCheck = pos.InCheck;
                    bool bmCap = ((bb.GetPieceAtIndex(bestMove.To) != None && !bestMove.IsCastle) || bestMove.IsEnPassant);
                    bool badScore = Math.Abs(bestMoveScore) > MaxFilteringScore;
                    if (!(inCheck || bmCap || badScore))
                    {
                        datapoints[toWrite].Fill(pos, bestMove, bestMoveScore);
                        toWrite++;
                    }
                    else
                    {
                        filtered++;
                    }

                    pos.MakeMove(bestMove);

                    if (pos.GenLegal(legalMoves) == 0)
                    {
                        result = (pos.ToMove == White) ? GameResult.BlackWin : GameResult.WhiteWin;
                        break;
                    }
                    else if (pos.IsDraw())
                    {
                        result = GameResult.Draw;
                        break;
                    }
                    else if (toWrite == WritableDataLimit - 1)
                    {
                        result = bestMoveScore >  400 ? GameResult.WhiteWin :
                                 bestMoveScore < -400 ? GameResult.BlackWin :
                                                        GameResult.Draw;
                        break;
                    }
                }



                totalBadPositions += filtered;
                totalGoodPositions += toWrite;

                var goodPerSec = totalGoodPositions / sw.Elapsed.TotalSeconds;
                var totalPerSec = (totalGoodPositions + totalBadPositions) / sw.Elapsed.TotalSeconds;

                Log($"{Environment.CurrentManagedThreadId, 2}:{threadID, 2}\t" +
                    $"{toWrite,4} / {toWrite + filtered,4} good" +
                    $"\t{result,8}" +
                    $"{goodPerSec,10:N1}/sec");

                AddResultsAndWrite(datapoints[..toWrite], result, outputWriter);
            }

            
            Interlocked.Add(ref CumulativeGames, gamesToRun);
        }



        private static void AddResultsAndWrite<Format>(Span<Format> datapoints, GameResult gr, StreamWriter outputWriter) where Format : TOutputFormat
        {
            for (int i = 0; i < datapoints.Length; i++)
            {
                datapoints[i].SetResult(gr);
                outputWriter.WriteLine(datapoints[i].GetWritableTextData());
            }

            outputWriter.Flush();
        }

        private static void AddResultsAndWrite<Format>(Span<Format> datapoints, GameResult gr, BinaryWriter outputWriter) where Format : TOutputFormat
        {
            for (int i = 0; i < datapoints.Length; i++)
                datapoints[i].SetResult(gr);

            fixed (Format* fmt = datapoints)
            {
                byte* data = (byte*)fmt;
                outputWriter.Write(new Span<byte>(data, datapoints.Length * sizeof(Format)));
            }

            outputWriter.Flush();
        }



        public static void ScrambleBoard(Position pos, Random rand)
        {
            //  Increase the ordinary bounds for movement in the Y direction by this much.
            //  Basically "move up/down by 33% more than left/right"
            const double YBias = 0.33333;

            //  Attempt to move a piece to a nearby random + empty square # times
            const int MaxPlacementTries = 4;

            //  If a king would be scrambled, ignore that # percent of the time
            const int KeepKsqFrequency = 80;


#if DBG
            Log($"Pre  scrambling: {pos.GetFEN()}");
#endif

            ref Bitboard bb = ref pos.bb;

            int us = pos.ToMove;
            int them = Not(us);

            //  0.25 + 0.5 - (0.25 * 0.5) == 62.5% == ~40 squares
            ulong scrambleMask = (ulong)((rand.NextInt64() & rand.NextInt64()) | rand.NextInt64());
            CastlingStatus cr = pos.State->CastleStatus;

            int bitSide = White;
            while (scrambleMask != 0)
            {
                //  Alternate popping bits from white's side and black's side
                int idx = (bitSide == White) ? poplsb(&scrambleMask) : popmsb(&scrambleMask);
                bitSide = Not(bitSide);

                int pt = bb.GetPieceAtIndex(idx);
                if (pt != None)
                {
                    int pc = bb.GetColorAtIndex(idx);

                    int dist = rand.Next(0, Math.Max(2, pt + 1));

                    if (pt == King && rand.Next(0, 100) <= KeepKsqFrequency)
                        continue;

                    for (int attempt = 0; attempt < MaxPlacementTries; attempt++)
                    {
                        int sq = RandomManhattanDistWithBias(rand, idx, dist, YBias);

                        if (pt == Pawn && (sq < A2 || sq > H7))
                            continue;   //  Don't put pawns on the back rank

                        if (bb.GetPieceAtIndex(sq) == None)
                        {
                            bb.MoveSimple(idx, sq, pc, pt);
                            scrambleMask &= ~SquareBB[sq];  //  Don't allow a piece to be moved twice

#if DBG
                            Log($"    Scrambled {ColorToString(pc)} {PieceToString(pt), -6}\t{IndexToString(idx)} -> {IndexToString(sq)}");
#endif

                            if (pt == King)
                                cr &= ~((pc == White) ? CastlingStatus.White : CastlingStatus.Black);
                            else if (pt == Rook)
                            {
                                var toRemove = (pc == White) ? CastlingStatus.White : CastlingStatus.Black;
                                toRemove &= (GetIndexFile(idx) >= Files.E) ? CastlingStatus.Kingside : CastlingStatus.Queenside;

                                cr &= ~toRemove;
                            }

                            break;
                        }
                    }
                    
                }
            }

            int nstmKing = bb.KingIndex(them);
            if ((bb.AttackersTo(nstmKing, bb.Occupancy) & bb.Colors[us]) != 0)
            {
                //  The nstm's king is attacked, so this position isn't legal.
                //  We'll try to fix this by moving the king to an empty neighboring square,
                //  trying the upper or lower squares first depending on the nstm's color
                //  (in the hopes of putting it closer to it's pieces)
                bool kingPlaced = false;
                ulong ring = NeighborsMask[nstmKing] & ~bb.Occupancy;
                while (ring != 0)
                {
                    int idx = (them == White) ? poplsb(&ring) : popmsb(&ring);
                    if ((bb.AttackersTo(idx, bb.Occupancy ^ SquareBB[nstmKing]) & bb.Colors[us]) == 0)
                    {
                        bb.MoveSimple(nstmKing, idx, them, King);
                        kingPlaced = true;
                        cr &= ~((them == Black) ? CastlingStatus.Black : CastlingStatus.White);

#if DBG
                        Log($"*   Fixed {ColorToString(them)} {PieceToString(King),-6}\t{IndexToString(nstmKing)} -> {IndexToString(idx)}");
#endif

                        break;
                    }
                }


                //  In case there isn't a suitable square immediately surrounding the king,
                //  start trying empty squares one-by-one, starting from that king's friendly corner
                //  (h8 and moving left/down for black, A1 and moving right/up for white)
                if (!kingPlaced)
                {
                    ulong sqrs = ~bb.Occupancy;
                    while (sqrs != 0)
                    {
                        int idx = (them == White) ? poplsb(&sqrs) : popmsb(&sqrs);
                        if ((bb.AttackersTo(idx, bb.Occupancy ^ SquareBB[nstmKing]) & bb.Colors[us]) == 0)
                        {
                            bb.MoveSimple(nstmKing, idx, them, King);
                            cr &= ~((them == Black) ? CastlingStatus.Black : CastlingStatus.White);

#if DBG
                            Log($"**  Fixed {ColorToString(them)} {PieceToString(King),-6}\t{IndexToString(nstmKing)} -> {IndexToString(idx)}");
#endif

                            break;
                        }
                    }
                }
            }


#if DBG
            Log($"Post scrambling: {pos.GetFEN()}");
#endif

            //pos.LoadFromFEN(pos.GetFEN());
            ResetPosition(pos, cr);

#if DBG
            Log($"Post reset:      {pos.GetFEN()}");
#endif

            static int RandomManhattanDistWithBias(Random rand, int startSq, int N, double YBias = 0.0)
            {
                IndexToCoord(startSq, out int sx, out int sy);

                int dx, dy;
                int newSq;
                do
                {
                    int amt = (int)(N * (1 + YBias));
                    dy = Math.Clamp(rand.Next(-amt, amt + 1), -N, N);
                    dx = (N - Math.Abs(dy)) * (rand.Next(2) == 0 ? 1 : -1);

                    newSq = CoordToIndex(sx + dx, sy + dy);
                }
                while (Math.Abs(dx) + Math.Abs(dy) <= N - 2 && !(newSq >= A1 && newSq <= H8));

                return newSq;
            }

        }

        public static void ResetPosition(Position pos, CastlingStatus cr = CastlingStatus.None)
        {
            ref Bitboard bb = ref pos.bb;

            pos.FullMoves = 1;
            pos.GamePly = 0;

            pos.State = pos.StartingState;

            var st = pos.State;
            NativeMemory.Clear(st, StateInfo.StateCopySize);
            st->CastleStatus = cr;
            st->HalfmoveClock = 0;
            st->PliesFromNull = 0;
            st->EPSquare = EPNone;
            st->CapturedPiece = None;
            st->KingSquares[White] = bb.KingIndex(White);
            st->KingSquares[Black] = bb.KingIndex(Black);

            pos.SetState();

            NNUE.RefreshAccumulator(pos);
        }



        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
    }
}
