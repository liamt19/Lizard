

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using LTChess.Logic.Core;
using LTChess.Logic.Data;
using LTChess.Logic.Search.Ordering;

using static LTChess.Logic.Search.MovePicker.MovePickerStage;
using static LTChess.Logic.Search.EvaluationConstants;

namespace LTChess.Logic.Search
{

    public unsafe class MovePicker
    {
        static MovePicker()
        {
            //  Ensure proper offsets for Killer0 and Killer1 in a SearchStackEntry
            if (EnableAssertions)
            {
                int off0 = ((FieldOffsetAttribute)(typeof(SearchStackEntry).GetField("Killer0").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0])).Value;
                int off1 = ((FieldOffsetAttribute)(typeof(SearchStackEntry).GetField("Killer1").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0])).Value;

                Assert(off0 == off1 - sizeof(ScoredMove),
                    "The offset of Killer1 must be exactly 8 bytes after Killer0. " +
                    "Killer0 is at " + off0 + ", and Killer1 is at " + off1);
            }
        }

        public string StageName
        {
            get
            {
                foreach (var field in typeof(MovePickerStage).GetFields())
                {
                    if ((int) field.GetValue(null) == stage)
                        return field.Name.ToString();
                }
                return "(Unknown!)";
            }
        }

        private readonly Position pos;

        private readonly short* mainHistory;
        private readonly short* captureHistory;

        private readonly PieceToHistory*[] continuations;
        private readonly int depth;
        private ScoredMove TTMove;
        private int previousSquare;

        private readonly CondensedMove TTCondMove;

        private int stage;

        private ScoredMove* moveBufferStart;
        private ScoredMove* currentMove;
        private ScoredMove* lastMove;
        private ScoredMove* endBadCaptures;

        private int killerNumber = 0;
        private Move k0;
        private Move k1;

        private int genSize = 0;


        public MovePicker(Position pos, short* mainHistory, short* captureHistory, PieceToHistory*[] contHist,
                          Move* killers, ScoredMove* moveBuffer, int depth, 
                          CondensedMove ttMove, int previousSquare = SquareNB)
        {
            this.pos = pos;

            //  The SearchThread's HistoryTable field isn't pinned, so trying to store this information as "private HistoryTable* history"
            //  will cause a crash if/when the GC decides to move the field somewhere else.
            //  We either need to store a reference to the SearchThread itself, or to the main/capture histories within the HistoryTable.
            this.mainHistory = mainHistory;
            this.captureHistory = captureHistory;

            this.continuations = contHist;

            //  The "killers" parameter that was passed is a ScoredMove array, not a Move array.
            //  The second move is at killers[2] instead of killers[1].
            k0 = *(killers + 0);
            k1 = *(killers + 2);

            /***
             *  Previously, all legal moves were generated and compared against the set of killer moves (i.e. moveList[i].Equals(killer0)),
             *  and if the Move moveList[i] was equal then it would be given a score accordingly. There wasn't any "returning", the entire
             *  list of moves was just sorted in place each time we tried a new move
             *  
             *  But moves are now generated in stages, and since we can't be certain which stage a killer move would be generated in
             *  we instead just try the killers in their own stage, and return them if they are pseudo-legal.
             *  
             *  One issue with this approach is that a killer move might have caused check in the position it was generated in,
             *  but doesn't cause check in the position that we are currently looking at.
             *  We don't want to just discard them altogether though since they might still be good moves irrespective of them causing check,
             *  so before they are returned we call MakeCheck on them.
             *  
             **/
            k0.Checks = false;
            k1.Checks = false;

            if (EnableAssertions)
            {
                Assert(!k0.Capture,
                    "MovePicker(" + pos.GetFEN() + ") was given the Killer0 move " + k0 + " = " + k0.ToString(pos) + " which is a capture! " +
                    "(Killer moves should never be captures)");
                
                Assert(!k1.Capture,
                    "MovePicker(" + pos.GetFEN() + ") was given the Killer1 move " + k1 + " = " + k1.ToString(pos) + " which is a capture! " +
                    "(Killer moves should never be captures)");
            }

            this.moveBufferStart = moveBuffer;
            this.depth = depth;
            this.TTCondMove = ttMove;
            this.previousSquare = previousSquare;

            //  If we are in check at any depth, we only look at evasions.
            //  Otherwise, we use the Negamax path for depth > 0, or the quiescence path for <= 0
            stage = this.pos.Checked ? EvasionsTT : 
                          (depth > 0 ? NegamaxTT :
                                       QuiesceTT);

            if (TTCondMove.Equals(CondensedMove.Null))
            {
                //  We have a ttMove, so try to convert it to a normal move.
                TTMove = this.pos.CondensedToNormalMove(TTCondMove);
                if (!this.pos.IsPseudoLegal(TTMove.Move))
                {
                    //  The ttMove we got isn't pseudo-legal, so skip the TT stage.
                    stage++;
                }

                //  In case the TTMove is also a killer, then we overwrite the killer with a null move
                //  because we don't want to end up returning it twice in NextMove().
                if (k0.Equals(TTMove))
                    k0 = Move.Null;

                if (k1.Equals(TTMove))
                    k1 = Move.Null;
            }
            else
            {
                //  We didn't get a ttMove, so skip the TT stage.
                stage++;
            }
        }

        /// <summary>
        /// Returns true if the current move has a static exchange value greater than <c>-(currentMove-&gt;Score)</c>
        /// </summary>
        private bool SelectGoodCaptures()
        {
            if (Search.SEE_GE(pos, currentMove->Move, -(currentMove->Score)))
            {
                return true;
            }
            *endBadCaptures++ = *currentMove;
            return false;
        }


        /// <summary>
        /// Returns true if the Killer numbered <paramref name="number"/> isn't a null move, is pseudo-legal,
        /// and is not a capture in the current position.
        /// </summary>
        private bool KillerWorks(int number)
        {
            return number switch
            {
                0 => k0 != Move.Null && (pos.bb.GetPieceAtIndex(k0.To) == None) && pos.IsPseudoLegal(k0),
                1 => k1 != Move.Null && (pos.bb.GetPieceAtIndex(k1.To) == None) && pos.IsPseudoLegal(k1),
                _ => false,
            };
        }

        /// <summary>
        /// Returns true so long as the current move is not one of the Killer moves.
        /// </summary>
        private bool SelectQuiets()
        {
            return (!currentMove->Move.Equals(k0)
                 && !currentMove->Move.Equals(k1));
        }

        /// <summary>
        /// Returns true so long as the move's To square is the same as our opponent's previous To square.
        /// </summary>
        private bool SelectQuiescenceRecaptures()
        {
            return (currentMove->Move.To == previousSquare);
        }

        /// <summary>
        /// Always returns true.
        /// </summary>
        private bool SelectEverything()
        {
            return true;
        }


        public Move NextMove(bool skipQuiets = false)
        {
            Top:
            switch (stage)
            {
                case NegamaxTT:
                case EvasionsTT:
                case QuiesceTT:
                    ++stage;
                    return TTMove.Move;


                case CapturesInit:
                case QuiesceCapturesInit:
                    currentMove = endBadCaptures = moveBufferStart;
                    genSize = pos.GenAll<GenLoud>(currentMove);

                    lastMove = (currentMove + genSize);

                    if (EnableAssertions)
                    {
                        for (ScoredMove* iter = currentMove; iter != lastMove; iter++)
                        {
                            Move m = iter->Move;
                            if (m.Capture)
                            {
                                Assert((pos.bb.Colors[Not(pos.ToMove)] & SquareBB[m.To]) != 0,
                                    "GenAll<GenLoud> generated a move " + m + " = '" + m.ToString(pos) + "' " +
                                    "marked as a capture, but " + ColorToString(Not(pos.ToMove)) + " doesn't have a piece on " + IndexToString(m.To) + "!");
                            }
                            else if (m.EnPassant)
                            {
                                int up = ShiftUpDir(pos.ToMove);
                                Assert(m.To == pos.State->EPSquare, "GenAll<GenLoud> generated an en passant move " + m + " = '" + m.ToString(pos) + "', " +
                                    "but the move's To square should be " + IndexToString(pos.State->EPSquare) + ", not " + IndexToString(m.To));

                                int epPawnSquare = m.To - up;

                                Assert((pos.bb.Colors[Not(pos.ToMove)] & SquareBB[epPawnSquare]) != 0,
                                    "GenAll<GenLoud> generated a move " + m + " = '" + m.ToString(pos) + "', " + 
                                    "marked as an en passant, but " + ColorToString(Not(pos.ToMove)) + " doesn't have a pawn to be captured on " + IndexToString(epPawnSquare) + "!");
                            }
                            else if (m.Promotion)
                            {
                                Assert(m.PromotionTo == Queen,
                                    "GenAll<GenLoud> generated a move " + m + " = '" + m.ToString(pos) + "', " + 
                                    "but GenLoud is only supposed to generate queen promotions for non-captures!");
                            }
                            else
                            {
                                Assert(false, "GenAll<GenLoud> generated a move " + m + " = '" + m.ToString(pos) + "', which isn't a capture, queen promotion, or en passant!");
                            }
                        }
                    }

                    ScoreCaptures();
                    PartialSort(currentMove, lastMove);
                    ++stage;
                    goto Top;


                case Captures:
                    if (Select<PredicateNext>(_ => SelectGoodCaptures()) != Move.Null)
                    {
                        return (currentMove - 1)->Move;
                    }

                    stage = Killers;

                    //  Fallthrough
                    goto case Killers;


                case Killers:
                    
                    if (killerNumber == 0 && KillerWorks(killerNumber))
                    {
                        killerNumber++;
                        if (k0.Promotion)
                        {
                            pos.MakeCheckPromotion(ref k0);
                        }
                        else if (k0.EnPassant)
                        {
                            pos.MakeCheckEnPassant(ref k0);
                        }
                        else if (k0.Castle)
                        {
                            pos.MakeCheckCastle(ref k0);
                        }
                        else
                        {
                            pos.MakeCheck(pos.bb.GetPieceAtIndex(k0.From), ref k0);
                        }

                        return k0;
                    }

                    if (killerNumber == 1 && KillerWorks(killerNumber))
                    {
                        killerNumber++;
                        if (k1.Promotion)
                        {
                            pos.MakeCheckPromotion(ref k1);
                        }
                        else if (k1.EnPassant)
                        {
                            pos.MakeCheckEnPassant(ref k1);
                        }
                        else if (k1.Castle)
                        {
                            pos.MakeCheckCastle(ref k1);
                        }
                        else
                        {
                            pos.MakeCheck(pos.bb.GetPieceAtIndex(k1.From), ref k1);
                        }
                        return k1;
                    }


                    stage = QuietsInit;

                    //  Fallthrough
                    goto case QuietsInit;


                case QuietsInit:
                    if (!skipQuiets)
                    {
                        currentMove = endBadCaptures;
                        genSize = pos.GenAll<GenQuiets>(currentMove);
                        lastMove = (currentMove + genSize);

                        ScoreQuiets();
                        PartialSort(currentMove, lastMove, -3000 * depth);
                    }

                    stage = Quiets;

                    //  Fallthrough
                    goto case Quiets;


                case Quiets:
                    if (!skipQuiets && Select<PredicateNext>(_ => SelectQuiets()) != Move.Null)
                    {
                        return (currentMove - 1)->Move;
                    }

                    currentMove = moveBufferStart;
                    lastMove = endBadCaptures;

                    stage = BadCaptures;

                    //  Fallthrough
                    goto case BadCaptures;


                case BadCaptures:
                    return Select<PredicateNext>(_ => SelectEverything());


                case EvasionsInit:
                    currentMove = moveBufferStart;
                    genSize = pos.GenAll<GenEvasions>(currentMove);
                    lastMove = (currentMove + genSize);

                    ScoreEvasions();
                    
                    stage = Evasions;

                    //  Fallthrough
                    goto case Evasions;


                case Evasions:
                    return Select<PredicateBest>(_ => SelectEverything());


                case QuiesceCaptures:
                    if (Select<PredicateNext>(_ => SelectQuiescenceRecaptures()) != Move.Null)
                    {
                        return (currentMove - 1)->Move;
                    }

                    if (depth != Search.DepthQChecks)
                    {
                        //  Only go to QuiesceChecks(Init) if the depth == DepthQChecks, otherwise we're done.
                        return Move.Null;
                    }

                    stage = QuiesceChecksInit;

                    //  Fallthrough
                    goto case QuiesceChecksInit;


                case QuiesceChecksInit:
                    currentMove = moveBufferStart;
                    genSize = pos.GenAll<GenQChecks>(currentMove);
                    lastMove = (currentMove + genSize);

                    stage = QuiesceChecks;

                    //  Fallthrough
                    goto case QuiesceChecks;


                case QuiesceChecks:
                    return Select<PredicateNext>(_ => SelectEverything());


                default:
                    throw new NotImplementedException("MovePicker entered a stage numbered " + stage);
                    break;
            }

            throw new Exception("MovePicker left the switch statement at stage " + stage);
            return Move.Null;
        }

        /// <summary>
        /// Returns the next non-TT move satisfying the <paramref name="filter"/>, or <see cref="Move.Null"/> if none of the moves between
        /// <see cref="currentMove"/> and <see cref="lastMove"/> fit the criteria.
        /// <br></br>
        /// If <typeparamref name="PredType"/> is <see cref="PredicateBest"/>, the returned move will be the move with the 
        /// highest scored move within the currentMove-lastMove range, instead of the next one.
        /// </summary>
        public Move Select<PredType>(Predicate<ScoredMove> filter) where PredType : PredicateType
        {
            while (currentMove < lastMove)
            {
                if (typeof(PredType) == typeof(PredicateBest))
                {
                    ScoredMove* max = MaxValue(currentMove, lastMove);
                    (*currentMove, *max) = (*max, *currentMove);
                }

                if (!currentMove->Move.Equals(TTMove) && filter(*currentMove))
                {
                    return (*currentMove++).Move;
                }

                currentMove++;
            }

            return Move.Null;


            ScoredMove* MaxValue(ScoredMove* first, ScoredMove* last)
            {
                if (first == last)
                {
                    return last;
                }

                ScoredMove* largest = first;
                ++first;

                for (; first != last; ++first)
                {
                    if (first->Score > largest->Score)
                    {
                        largest = first;
                    }
                }

                return largest;
            }

        }


        public void PartialSort(ScoredMove* begin, ScoredMove* end, int scoreLimit = int.MinValue)
        {
            for (ScoredMove* sortedEnd = begin, p = begin + 1; p < end; ++p)
            {
                if (p->Score >= scoreLimit)
                {
                    ScoredMove temp = *p;
                    *p = *++sortedEnd;

                    ScoredMove* q;
                    for (q = sortedEnd; q != begin && *(q - 1) < temp; --q)
                    {
                        *q = *(q - 1);
                    }
                    *q = temp;
                }
            }
        }










        public void ScoreCaptures()
        {
            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                Move m = iter->Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                int capturedPiece;
                if (m.Capture)
                {
                    capturedPiece = pos.bb.GetPieceAtIndex(moveTo);
                }
                else if (m.EnPassant)
                {
                    capturedPiece = Pawn;
                }
                else
                {
                    if (EnableAssertions)
                    {
                        Assert(m.Promotion && m.PromotionTo == Queen,
                            "ScoreCaptures() for move " + m + " = " + m.ToString(pos) + " wasn't a capture nor en passant, " +
                            "so it must be a queen promotion. m.Promotion is " + (m.Promotion) + ", and m.PromotionTo is " + PieceToString(m.PromotionTo));
                    }

                    //  For non-capture queen promotions
                    capturedPiece = None;
                }

                int capIdx = HistoryTable.CapIndex(pos.ToMove, pos.bb.GetPieceAtIndex(moveFrom), moveTo, capturedPiece);
                iter->Score = (13 * GetPieceValue(capturedPiece)) + captureHistory[capIdx] / 12;
            }
        }

        public void ScoreQuiets()
        {
            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                int contIdx = PieceToHistory.GetIndex(pos.ToMove, pos.bb.GetPieceAtIndex(iter->Move.From), iter->Move.To);

                iter->Score = 2 * (mainHistory[HistoryTable.HistoryIndex(pos.ToMove, iter->Move)]) +
                              2 * (*continuations[0])[contIdx] +
                                  (*continuations[1])[contIdx] +
                                  (*continuations[3])[contIdx] +
                                  (*continuations[5])[contIdx];

                if (iter->Move.Checks)
                {
                    iter->Score += 10000;
                }
            }

        }

        public void ScoreEvasions()
        {
            ref Bitboard bb = ref pos.bb;
            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                if (iter->Move.Capture || iter->Move.EnPassant)
                {
                    int capturedPiece = (iter->Move.EnPassant ? Piece.Pawn : bb.GetPieceAtIndex(iter->Move.To));
                    iter->Score = GetPieceValue(capturedPiece) + 10000;

                    if (EnableAssertions)
                    {
                        Assert(capturedPiece != None, 
                            "ScoreEvasions() got the move " + iter->Move.ToString() + " = " + iter->Move.ToString(pos) + ", " +
                            "which was generated as a capture/EP but isn't in the current position!");
                    }
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pos.ToMove, pos.bb.GetPieceAtIndex(iter->Move.From), iter->Move.To);

                    iter->Score = 2 * (mainHistory[HistoryTable.HistoryIndex(pos.ToMove, iter->Move)]) +
                                  2 * (*continuations[0])[contIdx] +
                                      (*continuations[1])[contIdx] +
                                      (*continuations[3])[contIdx] +
                                      (*continuations[5])[contIdx];
                }

            }
        }


        public static class MovePickerStage
        {
            public const int NegamaxTT = 0;

            public const int CapturesInit = NegamaxTT + 1;
            public const int Captures = CapturesInit + 1;
            public const int Killers = Captures + 1;
            public const int QuietsInit = Killers + 1;
            public const int Quiets = QuietsInit + 1;
            public const int BadCaptures = Quiets + 1;



            public const int EvasionsTT = 10;
            public const int EvasionsInit = EvasionsTT + 1;
            public const int Evasions = EvasionsInit + 1;



            public const int QuiesceTT = 20;
            public const int QuiesceCapturesInit = QuiesceTT + 1;
            public const int QuiesceCaptures = QuiesceCapturesInit + 1;
            public const int QuiesceChecksInit = QuiesceCaptures + 1;
            public const int QuiesceChecks = QuiesceChecksInit + 1;
        }
    }
}
