
//#define DBG
//#define WRITE_PGN

using System.Runtime.InteropServices;
using System.Text;

using Lizard.Logic.NN;
using Lizard.Logic.Threads;

namespace Lizard.Logic.Datagen
{
    public static unsafe class DatagenMatch
    {
        private const int HashSize = 8;

        private const int MinPly = 8;
        private const int MaxPly = 9;

        public const int SoftNodeLimit = 5000;
        public const int HardNodeLimit = 20000;

        private const int WritableDataLimit = 512;
        private const int DepthLimit = 10;

        private const int AdjudicateMoves = 2;
        private const int AdjudicateScore = 4000;
        private const int MaxFilteringScore = 5000;

        private const int MaxOpeningScore = 1200;

        private static int Seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rand = new(() => new Random(Interlocked.Increment(ref Seed)));

        public static long CumulativeGames = 0;


        public static void RunGames(int gamesToRun = 1, int threadID = 0, bool scramble = true)
        {
            SearchOptions.Hash = HashSize;

            SearchThreadPool pool = new SearchThreadPool(1);
            Position pos = new Position(owner: pool.MainThread);
            ref Bitboard bb = ref pos.bb;

            Move bestMove = Move.Null;
            int bestMoveScore = 0;

#if RAW_STRING
                StringBuilder outputBuffer = new StringBuilder();
#endif

            using StreamWriter outputWriter = new StreamWriter($"datagen{(scramble ? "_scr" : string.Empty)}{threadID}.txt", true);

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
                MaxDepth = DepthLimit,
                OnDepthFinish = null,
                OnSearchFinish = null,
            };

            ScoredMove* legalMoves = stackalloc ScoredMove[MoveListSize];
            Span<PlaintextData> datapoints = stackalloc PlaintextData[WritableDataLimit];

            Stopwatch sw = Stopwatch.StartNew();

            for (int gameNum = 0; gameNum < gamesToRun; gameNum++)
            {
                pool.TTable.Clear();
                pool.Clear();
                pos.LoadFromFEN(InitialFEN);

                int randMoveCount = Rand.Value.Next(MinPly, MaxPly + 1);
                for (int i = 0; i < randMoveCount; i++)
                {
                    int l = pos.GenLegal(legalMoves);
                    if (l == 0) { gameNum--; continue; }
                    Move t = legalMoves[Rand.Value.Next(0, l)].Move;

                    pos.MakeMove(t);
                }

                if (scramble)
                {
                    ScrambleBoard(pos, Rand.Value);
                }

                if (pos.GenLegal(legalMoves) == 0) { gameNum--; continue; }

                //  Check if the starting position has a reasonable score, and scrap it if it doesn't
                pool.StartSearch(pos, ref prelimInfo);
                pool.BlockCallerUntilFinished();
                if (Math.Abs(pool.GetBestThread().RootMoves[0].Score) >= MaxOpeningScore) { gameNum--; continue; }

#if RAW_STRING
                outputBuffer.Clear();
#endif

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
#if RAW_STRING
                        outputBuffer.Append(pos.GetFEN());
                        outputBuffer.Append(" | ");
                        outputBuffer.Append(bestMoveScore);
                        outputBuffer.Append(" | ");
                        outputBuffer.AppendLine(bestMove.ToString(pos));
#else
                        datapoints[toWrite].Fill(pos, bestMoveScore);
#endif

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

#if RAW_STRING
                outputWriter.Write(outputBuffer);
#else
                AddResultsAndWrite(datapoints[..toWrite], result, outputWriter);
#endif
            }

            
            Interlocked.Add(ref CumulativeGames, gamesToRun);
        }



        private static void AddResultsAndWrite<Format>(Span<Format> datapoints, GameResult gr, StreamWriter outputWriter) where Format : TOutputFormat
        {
            if (typeof(Format) == typeof(PlaintextData))
            {
                for (int i = 0; i < datapoints.Length; i++)
                {
                    datapoints[i].Result = gr;
                    outputWriter.WriteLine(datapoints[i].GetWritableData());
                }
                
            }
            else
            {
                for (int i = 0; i < datapoints.Length; i++)
                    datapoints[i].Result = gr;

                using BinaryWriter br = new BinaryWriter(outputWriter.BaseStream);
                fixed (Format* fmt = datapoints)
                {
                    byte* data = (byte*)fmt;
                    br.Write(new Span<byte>(data, datapoints.Length * sizeof(Format)));
                }
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

                    if (pt == King)
                        dist = (rand.Next(0, 100) <= KeepKsqFrequency ? 0 : 1);

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
            DatagenScrambleReset(pos, cr);

#if DBG
            Log($"Post reset:      {pos.GetFEN()}");
#endif

            static void DatagenScrambleReset(Position pos, CastlingStatus cr = CastlingStatus.None)
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
    }
}
