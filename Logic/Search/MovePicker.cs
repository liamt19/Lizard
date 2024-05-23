using System.Runtime.InteropServices;

using Lizard.Logic.Search.History;

using static Lizard.Logic.Search.MovePicker.MovePickerStage;

namespace Lizard.Logic.Search
{

    public unsafe class MovePicker
    {
        public string StageName
        {
            get
            {
                foreach (var field in typeof(MovePickerStage).GetFields())
                {
                    if ((int)field.GetValue(null) == stage)
                        return field.Name.ToString();
                }
                return "(Unknown!)";
            }
        }

        private readonly Position pos;

        private readonly PieceToHistory*[] continuations;
        private readonly int depth;
        private int previousSquare;

        private int stage;

        private ScoredMove* moveBufferStart;
        private ScoredMove* currentMove;
        private ScoredMove* lastMove;
        private ScoredMove* endBadCaptures;

        private int killerNumber = 0;
        private Move k1;
        private Move k2;

        private Move TTMove;

        private int genSize = 0;


        public MovePicker(Position pos, SearchStackEntry* ss, ScoredMove* moveBuffer,
                          Move ttMove, int depth, int previousSquare = SquareNB)
        {
            this.pos = pos;
            this.continuations = 
            [
                                         null, (ss - 1)->ContinuationHistory,
                (ss - 2)->ContinuationHistory, null,
                (ss - 4)->ContinuationHistory, null,
                (ss - 6)->ContinuationHistory,
            ];

            //  The "killers" parameter that was passed is a ScoredMove array, not a Move array.
            //  The second move is at killers[2] instead of killers[1].
            k1 = ss->Killer0;
            k2 = ss->Killer1;

            this.moveBufferStart = moveBuffer;
            this.depth = depth;
            this.previousSquare = previousSquare;

            //  If we are in check at any depth, we only look at evasions.
            //  Otherwise, we use the Negamax path for depth > 0, or the quiescence path for <= 0
            stage = this.pos.Checked ? EvasionsTT :
                          (depth > 0 ? NegamaxTT :
                                       QuiesceTT);

            TTMove = ttMove;

            if (!ttMove.IsNull())
            {
                if (!this.pos.IsPseudoLegal(ttMove))
                {
                    //  The ttMove we got isn't pseudo-legal, so skip the TT stage.
                    stage++;
                }

                //  In case the TTMove is also a killer, then we overwrite the killer with a null move
                //  because we don't want to end up returning it twice in NextMove().
                if (k1.Equals(TTMove))
                    k1 = Move.Null;

                if (k2.Equals(TTMove))
                    k2 = Move.Null;
            }
            else
            {
                //  We didn't get a ttMove, so skip the TT stage.
                stage++;
            }
        }

        /// <summary>
        /// Returns true if the current move has a static exchange value greater than 1
        /// </summary>
        private bool SelectGoodCaptures()
        {
            if (Searches.SEE_GE(pos, currentMove->Move, 1))
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
                1 => k1 != Move.Null && (pos.bb.GetPieceAtIndex(k1.To) == None) && pos.IsPseudoLegal(k1),
                2 => k2 != Move.Null && (pos.bb.GetPieceAtIndex(k2.To) == None) && pos.IsPseudoLegal(k2),
                _ => false,
            };
        }

        /// <summary>
        /// Returns true so long as the current move is not one of the Killer moves.
        /// </summary>
        private bool SelectQuiets()
        {
            return !currentMove->Move.Equals(k1)
                && !currentMove->Move.Equals(k2);
        }

        /// <summary>
        /// Returns true so long as the move's To square is the same as our opponent's previous To square.
        /// </summary>
        private bool SelectQuiescenceRecaptures()
        {
            return currentMove->Move.To == previousSquare;
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
                    stage++;
                    return TTMove;


                case NMCapturesInit:
                case QSCapturesInit:
                    currentMove = endBadCaptures = moveBufferStart;
                    genSize = pos.GenAll<GenLoud>(currentMove);

                    lastMove = currentMove + genSize;

                    ScoreCaptures();
                    PartialSort(currentMove, lastMove);
                    stage++;
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

                    killerNumber++;
                    if (killerNumber == 1 && KillerWorks(killerNumber))
                        return k1;

                    if (killerNumber == 2 && KillerWorks(killerNumber))
                        return k2;


                    stage = QuietsInit;
                    //  Fallthrough
                    goto case QuietsInit;


                case QuietsInit:
                    if (!skipQuiets)
                    {
                        currentMove = endBadCaptures;
                        genSize = pos.GenAll<GenQuiets>(currentMove);
                        lastMove = currentMove + genSize;

                        ScoreQuiets();
                        PartialSort(currentMove, lastMove);
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
                    lastMove = currentMove + genSize;

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

                    if (depth != Searches.DepthQChecks)
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
                    lastMove = currentMove + genSize;

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
            ref Bitboard bb = ref pos.bb;
            ref HistoryTable history = ref pos.Owner.History;

            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                Move m = iter->Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                int capturedPiece = pos.bb.GetPieceAtIndex(moveTo);
                if (m.EnPassant)
                {
                    capturedPiece = Pawn;
                }
                else if (m.Promotion)
                {
                    //  For non-capture queen promotions
                    capturedPiece = Rook;
                }

                iter->Score = (OrderingVictimValueMultiplier * GetPieceValue(capturedPiece)) +
                    history.CaptureHistory[pos.ToMove, bb.GetPieceAtIndex(moveFrom), moveTo, capturedPiece] / OrderingHistoryDivisor;
            }
        }

        public void ScoreQuiets()
        {
            ref Bitboard bb = ref pos.bb;
            ref HistoryTable history = ref pos.Owner.History;

            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                Move m = iter->Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                int pt = bb.GetPieceAtIndex(moveFrom);
                int contIdx = PieceToHistory.GetIndex(pos.ToMove, pt, moveTo);

                iter->Score = 2 * history.MainHistory[pos.ToMove, m] +
                             (2 * (*continuations[1])[contIdx]) +
                                  (*continuations[2])[contIdx] +
                                  (*continuations[4])[contIdx] +
                                  (*continuations[6])[contIdx];

                if ((pos.State->CheckSquares[pt] & SquareBB[moveTo]) != 0)
                {
                    iter->Score += OrderingGivesCheckBonus;
                }
            }

        }


        public void ScoreEvasions()
        {
            ref Bitboard bb = ref pos.bb;
            ref HistoryTable history = ref pos.Owner.History;

            const int CapturesFirst = HistoryTable.NormalClamp * 1000;

            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                Move m = iter->Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                Assert(m.Castle is false, "ScoreEvasions tried scoring a castling move!");

                int pt = bb.GetPieceAtIndex(moveFrom);
                int capturedPiece = bb.GetPieceAtIndex(moveTo);
                
                if (m.EnPassant)
                    capturedPiece = Pawn;

                if (capturedPiece != None)
                {
                    //  MVV and LVA
                    iter->Score = GetPieceValue(capturedPiece) - pt + CapturesFirst;
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pos.ToMove, pt, moveTo);

                    iter->Score = (2 * history.MainHistory[pos.ToMove, m]) +
                                  (2 * (*continuations[1])[contIdx]) +
                                       (*continuations[2])[contIdx] +
                                       (*continuations[4])[contIdx] +
                                       (*continuations[6])[contIdx];
                }

            }
        }


        public static class MovePickerStage
        {
            public const int NegamaxTT = 0;

            public const int NMCapturesInit = NegamaxTT + 1;
            public const int Captures = NMCapturesInit + 1;
            public const int Killers = Captures + 1;
            public const int QuietsInit = Killers + 1;
            public const int Quiets = QuietsInit + 1;
            public const int BadCaptures = Quiets + 1;



            public const int EvasionsTT = 10;
            public const int EvasionsInit = EvasionsTT + 1;
            public const int Evasions = EvasionsInit + 1;



            public const int QuiesceTT = 20;
            public const int QSCapturesInit = QuiesceTT + 1;
            public const int QuiesceCaptures = QSCapturesInit + 1;
            public const int QuiesceChecksInit = QuiesceCaptures + 1;
            public const int QuiesceChecks = QuiesceChecksInit + 1;
        }
    }
}
