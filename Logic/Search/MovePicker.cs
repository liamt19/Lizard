using System.Runtime.InteropServices;

using Lizard.Logic.Search.History;

using static Lizard.Logic.Search.MovePicker.MovePickerStage;

namespace Lizard.Logic.Search
{

    public unsafe class MovePicker
    {
        static MovePicker()
        {
            int off0 = ((FieldOffsetAttribute)typeof(SearchStackEntry).GetField("Killer0").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0]).Value;
            int off1 = ((FieldOffsetAttribute)typeof(SearchStackEntry).GetField("Killer1").GetCustomAttributes(typeof(FieldOffsetAttribute), true)[0]).Value;

            Assert(off0 == off1 - sizeof(ScoredMove),
                $"The offset of Killer1 must be exactly 8 bytes after Killer0. " +
                 "Killer0 is at {off0}, and Killer1 is at {off1}");
        }

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

        private readonly short* mainHistory;
        private readonly short* captureHistory;

        private readonly PieceToHistory*[] continuations;
        private readonly int depth;
        private ScoredMove TTMove;
        private int previousSquare;

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
                          Move ttMove, int previousSquare = SquareNB)
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

            this.moveBufferStart = moveBuffer;
            this.depth = depth;
            this.previousSquare = previousSquare;

            //  If we are in check at any depth, we only look at evasions.
            //  Otherwise, we use the Negamax path for depth > 0, or the quiescence path for <= 0
            stage = this.pos.Checked ? EvasionsTT :
                          (depth > 0 ? NegamaxTT :
                                       QuiesceTT);

            if (!ttMove.Equals(Move.Null))
            {
                //  We have a ttMove, so try to convert it to a normal move.
                TTMove = new ScoredMove(ref ttMove);
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
            if (Searches.SEE_GE(pos, currentMove->Move, -currentMove->Score))
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
            return !currentMove->Move.Equals(k0)
                 && !currentMove->Move.Equals(k1);
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
                    ++stage;
                    return TTMove.Move;


                case CapturesInit:
                case QuiesceCapturesInit:
                    currentMove = endBadCaptures = moveBufferStart;
                    genSize = pos.GenAll<GenLoud>(currentMove);

                    lastMove = currentMove + genSize;

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
                        return k0;
                    }

                    if (killerNumber == 1 && KillerWorks(killerNumber))
                    {
                        killerNumber++;
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
                        lastMove = currentMove + genSize;

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

                iter->Score = (13 * GetPieceValue(capturedPiece)) + -9999;// (captureHistory[pos.ToMove, pos.bb.GetPieceAtIndex(moveFrom), moveTo, capturedPiece] / 12);
            }
        }

        public void ScoreQuiets()
        {
            for (ScoredMove* iter = currentMove; iter != lastMove; ++iter)
            {
                int contIdx = PieceToHistory.GetIndex(pos.ToMove, pos.bb.GetPieceAtIndex(iter->Move.From), iter->Move.To);

                iter->Score = -9999 + //(2 * mainHistory[HistoryTable.HistoryIndex(pos.ToMove, iter->Move)]) +
                              (2 * (*continuations[0])[contIdx]) +
                                  (*continuations[1])[contIdx] +
                                  (*continuations[3])[contIdx] +
                                  (*continuations[5])[contIdx];

                if ((pos.State->CheckSquares[pos.bb.GetPieceAtIndex(iter->Move.From)] & SquareBB[iter->Move.To]) != 0)
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
                if ((bb.GetPieceAtIndex(iter->Move.To) != None) || iter->Move.EnPassant)
                {
                    int capturedPiece = iter->Move.EnPassant ? Piece.Pawn : bb.GetPieceAtIndex(iter->Move.To);
                    iter->Score = GetPieceValue(capturedPiece) + 10000;
                }
                else
                {
                    int contIdx = PieceToHistory.GetIndex(pos.ToMove, pos.bb.GetPieceAtIndex(iter->Move.From), iter->Move.To);

                    iter->Score = -9999 + //(2 * mainHistory[HistoryTable.HistoryIndex(pos.ToMove, iter->Move)]) +
                                  (2 * (*continuations[0])[contIdx]) +
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
