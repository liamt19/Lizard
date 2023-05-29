using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static System.Formats.Asn1.AsnWriter;
using static LTChess.Search.SearchConstants;

namespace LTChess.Search
{
    public static class SimpleSearch
    {
        /// <summary>
        /// Begin a new search with the parameters in <paramref name="info"/>.
        /// This performs iterative deepening, which searches at higher and higher depths as time goes on.
        /// </summary>
        public static void StartSearching(ref SearchInformation info)
        {
            Stopwatch totalTime = Stopwatch.StartNew();

            int depth = 1;
            int maxDepth = info.MaxDepth;
            double maxTime = info.MaxSeachTime;
            bool continueDeepening = true;

            while (continueDeepening)
            {
                info.MaxDepth = depth;
                Deepen(ref info);
                if (continueDeepening && Evaluation.IsScoreMate(info.BestScore, out _))
                {
                    totalTime.Stop();
                    Log("Forced mate found (" + info.BestMove + "#), aborting at depth " + depth + " after " + totalTime.Elapsed.TotalSeconds + " seconds");
                    info.OnSearchFinish?.Invoke();
                    return;
                }
                if (info.StopSearching)
                {
                    totalTime.Stop();
                    Log("Received StopSearching command, aborting at depth " + depth + " after " + totalTime.Elapsed.TotalSeconds + " seconds");
                    info.OnSearchFinish?.Invoke();
                    return;
                }

                depth++;

                continueDeepening = (depth <= maxDepth && totalTime.Elapsed.TotalMilliseconds <= maxTime);
            }

            totalTime.Stop();
            info.OnSearchFinish?.Invoke();
        }

        /// <summary>
        /// Performs one pass of deepening, at the depth specified in <paramref name="info"/>.maxDepth
        /// </summary>
        public static void Deepen(ref SearchInformation info)
        {
            int alpha = AlphaStart;
            int beta = BetaStart;
            Stopwatch sw = Stopwatch.StartNew();
            int score = SimpleSearch.FindBest(ref info, alpha, beta, info.MaxDepth);
            sw.Stop();
            info.SearchTime = sw.Elapsed.TotalMilliseconds;
            info.BestScore = score;
            info.OnDepthFinish?.Invoke();
        }

        /// <summary>
        /// Finds the best move according to the Evaluation function, looking at least <paramref name="depth"/> moves in the future.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="alpha">
        ///     The evaluation of the lower bound move. 
        ///     This will eventually be set equal to the evaluation of the best move we can make.
        /// </param>
        /// <param name="beta">
        ///     The evaluation of the upper bound move.
        ///     This essentially represents the best case scenerio for our opponent based on the moves we have available to us.
        ///     If we ever evaluate a position that scores higher than the beta, 
        ///     we immediately stop searching (return beta) because that would mean our opponent has a good reply
        ///     and we know that we can probably do better by making a different move.
        /// </param>
        /// <param name="depth">
        ///     The depth to search to.
        ///     When this reaches 0 we would ordinarily stop looking, 
        ///     but there is an additional quiescence search which looks at all available captures to make sure we didn't just 
        ///     make a blunder that could have been avoided by looking an additional move in the future.
        /// </param>
        /// <returns>The evaluation of the best move.</returns>
        [MethodImpl(Inline)]
        public static int FindBest(ref SearchInformation info, int alpha, int beta, int depth)
        {
            if (info.NodeCount >= info.MaxNodes || info.StopSearching)
            {
                info.StopSearching = true;
                return 0;
            }

            info.NodeCount++;

            if (depth <= 0)
            {
                return SimpleQuiescence.FindBest(ref info, alpha, beta, depth);
            }

            Position pos = info.Position;
            Bitboard bb = pos.bb;
            ulong posHash = pos.Hash;
            TTEntry ttEntry = TranspositionTable.Probe(posHash);
            Move BestMove = Move.Null;
            int startingAlpha = alpha;

            bool useStaticEval = false;
            if (useStaticEval)
            {
                int staticEval = 0;
                ETEntry etEntry = EvaluationTable.Probe(posHash);
                if (etEntry.key == EvaluationTable.InvalidKey || !etEntry.Validate(posHash))
                {
                    staticEval = Evaluation.Evaluate(info.Position.bb, info.Position.ToMove);
                    EvaluationTable.Save(posHash, (short)staticEval);
                }
                else
                {
                    staticEval = etEntry.score;
                }
            }

            bool useTT = false;
            if (useTT)
            {
                if (ttEntry.NodeType != NodeType.Invalid && ttEntry.Key == TTEntry.MakeKey(posHash) && !ttEntry.BestMove.IsNull())
                {
                    //  Make sure it's legal since collisions are common
                    //  If we found a node for this position
                    if (ttEntry.NodeType != NodeType.Alpha && bb.IsPseudoLegal(ttEntry.BestMove) && IsLegal(pos, bb, ttEntry.BestMove, bb.KingIndex(Not(pos.ToMove)), bb.KingIndex(pos.ToMove)))
                    {
                        //  Exclude alpha nodes because they are lower bounds and probably not "the best"
                        info.BestMove = ttEntry.BestMove;
                    }

                    if (ttEntry.Depth >= depth)
                    {
                        //  If we have already searched this node at an equal or higher depth, we can update alpha and beta accordingly
                        if (ttEntry.NodeType == NodeType.Alpha)
                        {
                            if (ttEntry.Eval < beta)
                            {
                                //  This node is known to be worse than beta
                                beta = ttEntry.Eval;
                            }
                        }
                        else if (ttEntry.NodeType == NodeType.Beta)
                        {
                            if (ttEntry.Eval > alpha)
                            {
                                //  This nose is known to be better than alpha
                                alpha = ttEntry.Eval;
                            }
                        }
                        else if (ttEntry.NodeType == NodeType.Exact)
                        {
                            if ((beta - alpha >= 1) || Evaluation.IsScoreMate(ttEntry.Eval, out _))
                            {
#if DEBUG
                                SearchStatistics.NegamaxTTExactHits++;
#endif
                                return ttEntry.Eval;
                            }
                        }

                        //  Beta cutoff
                        if (alpha >= beta)
                        {
#if DEBUG
                            SearchStatistics.BetaCutoffs++;
#endif
                            return ttEntry.Eval;
                        }
                    }
                }
            }


            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = pos.GenAllLegalMoves(list);

            //  No legal moves, is this checkmate or a draw?
            if (size == 0)
            {
                if (pos.CheckInfo.InCheck || pos.CheckInfo.InDoubleCheck)
                {
                    return -Evaluation.ScoreMate - ((pos.Moves.Count + 1) / 2);
                }
                else
                {
                    return -Evaluation.ScoreDraw;
                }
            }

            //  Seems to help
            list.SortByCheck();

            for (int i = 0; i < size; i++)
            {
                pos.MakeMove(list[i]);
                int score = -SimpleSearch.FindBest(ref info, -beta, -alpha, depth - 1);
                pos.UnmakeMove();

                if (score >= beta)
                {
#if DEBUG
                    //Log(depth + " beta cutoff on move " + list[i].ToString(pos) + ", score " + score + " >= beta " + beta);
#endif
                    return beta;
                }

                if (score > alpha)
                {
#if DEBUG
                    //Log(depth + " new best move " + list[i].ToString(pos) + ", score " + score + " > alpha " + alpha);
#endif

                    alpha = score;
                    BestMove = list[i];
                }
            }

            //  We want to replace entries that we have searched to a higher depth since the new evaluation will be more accurate.
            //  But this is only done if the TTEntry was null (move hadn't already been looked at)
            //  or we are sure that this move changes the alpha score after searching at a higher depth
            bool setTT = (ttEntry.Depth <= depth && (ttEntry.NodeType == NodeType.Invalid || alpha != startingAlpha));
            if (setTT)
            {
                NodeType nodeType;
                if (alpha < startingAlpha)
                {
                    nodeType = NodeType.Alpha;
                }
                else if (alpha >= beta)
                {
                    nodeType = NodeType.Beta;
                }
                else
                {
                    nodeType = NodeType.Exact;
                }
                TranspositionTable.Save(posHash, (short)alpha, nodeType, depth, BestMove);

                //  TODO this looks misplaced
                info.BestMove = BestMove;
                info.BestScore = alpha;

#if DEBUG
                SearchStatistics.SetTTs += 1;
#endif
            }
#if DEBUG
            else
            {
                SearchStatistics.NotSetTTs += 1;
            }
#endif



            return alpha;
        }


        public static int GetPV(SearchInformation info, in Move[] moves, int numMoves)
        {
            Position position = info.Position;
            Bitboard bb = position.bb;

            int theirKing = bb.KingIndex(Not(position.ToMove));
            int ourKing = bb.KingIndex(position.ToMove);

            TTEntry stored = TranspositionTable.Probe(position.Hash);
            if (stored.NodeType == NodeType.Exact && stored.Key == TTEntry.MakeKey(position.Hash) && numMoves < MAX_DEPTH)
            {
                if (!bb.IsPseudoLegal(stored.BestMove) || !IsLegal(position, bb, stored.BestMove, ourKing, theirKing))
                {
                    return numMoves;
                }

                moves[numMoves] = stored.BestMove;
                position.MakeMove(stored.BestMove);

                numMoves = GetPV(info, moves, numMoves + 1);
                position.UnmakeMove();
            }

            return numMoves;
        }


    }
}
