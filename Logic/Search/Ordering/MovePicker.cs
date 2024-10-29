
#define REF_STRUCT
#undef REF_STRUCT

using Lizard.Logic.Data;
using Lizard.Logic.Search.History;

using static Lizard.Logic.Search.Ordering.MovePicker.MovePickerStage;


namespace Lizard.Logic.Search.Ordering
{
#if REF_STRUCT
    public unsafe ref struct MovePicker
#else
    public unsafe class MovePicker
#endif
    {
        private readonly Position pos;

        private readonly ScoredMove* list;
        private int listIndex;
        private int listSize;
        private int numBadNoisy;

        private readonly SearchStackEntry* ss;
#if REF_STRUCT
        private readonly ref HistoryTable history;
#endif
        private readonly Move ttMove;
        private readonly Move killerMove;
        private readonly int depth;

        private bool _skipQuiets;
        private int _stage;
        public int Stage => _stage;

        private bool IsQSearch => depth <= 0;



        public MovePicker(Position pos, ScoredMove* list, SearchStackEntry* ss, Move ttMove, int depth)
        {
            this.pos = pos;
            this.list = list;
            this.ss = ss;
            this.ttMove = ttMove;
            this.killerMove = ss->KillerMove;
            this.depth = depth;

#if REF_STRUCT
            history = ref pos.Owner.History;
            new Span<ScoredMove>(list, MoveListSize).Clear();
#endif

            listIndex = 0;
            listSize = 0;
            numBadNoisy = 0;

            _skipQuiets = false;
            _stage = ss->InCheck ? EvasionTT : NormalTT;

            if (killerMove == ttMove)
                killerMove = Move.Null;
        }

        public Move OrderNextMove()
        {
            TOP:
            switch (_stage)
            {
                case EvasionTT:
                case NormalTT: 
                {
                        _stage++;

                        if (ttMove != Move.Null && pos.IsPseudoLegal(ttMove))
                        {
                            return ttMove;
                        }

                        goto TOP;
                }
                case MakeNoisy: 
                {
                        listSize = pos.GenAll<GenNoisy>(list, listSize);
                        ScoreNoisyMoves();

                        _stage++;
                        goto case PlayGoodNoisy;    //  Fallthrough
                }
                case PlayGoodNoisy: 
                {
                        while (listIndex < listSize)
                        {
                            int idx = FindBest();
                            ScoredMove next = list[idx];
                            Move move = next.Move;

                            if (move == ttMove)
                                continue;

                            int threshold = -next.Score / 4;

                            if (Searches.SEE_GE(pos, move, threshold))
                            {
                                return move;
                            }
                            else
                            {
                                list[numBadNoisy++] = next;
                            }
                        }
                        
                        _stage++;
                        goto case TryKiller;        //  Fallthrough
                }
                case TryKiller: 
                {
                        _stage++;

                        if (!_skipQuiets 
                            && killerMove != Move.Null 
                            && pos.IsPseudoLegal(killerMove))
                        {
                            return killerMove;
                        }

                        goto case MakeQuiet;        //  Fallthrough
                }
                case MakeQuiet: 
                {
                        if (!_skipQuiets)
                        {
                            listSize = pos.GenAll<GenQuiet>(list, listSize);
                            ScoreQuietMoves();
                        }

                        _stage++;
                        goto case PlayQuiet;        //  Fallthrough
                }
                case PlayQuiet: 
                {
                        if (!_skipQuiets) 
                        {
#if REF_STRUCT
                            int idx = FindBest();
                            Move next = list[idx].Move;
                            if (next != Move.Null && (next == ttMove || next == killerMove))
                            {
                                return OrderNextMove();
                            }
#else
                            Move next = SelectNext(SelectQuiets);
#endif

                            if (next != Move.Null)
                            {
                                return next;
                            }
                        }

                        _stage++;
                        goto case StartBadNoisy;    //  Fallthrough
                }
                case StartBadNoisy: 
                {
                        listIndex = 0;
                        listSize = numBadNoisy;

                        _stage++;
                        goto case PlayBadNoisy;     //  Fallthrough
                }
                case PlayBadNoisy: 
                {
#if REF_STRUCT
                        int idx = FindBest();
                        Move next = list[idx].Move;
                        if (next != Move.Null && (next == ttMove))
                        {
                            return OrderNextMove();
                        }
#else
                        Move next = SelectNext(SelectNonTTs);
#endif

                        if (next != Move.Null)
                        {
                            return next;
                        }

                        _stage = End;
                        goto case End;
                }
                case MakeEvasion:
                {
                        listSize = pos.GenAll<GenEvasions>(list, listSize);
                        ScoreEvasions();

                        _stage++;
                        goto case PlayEvasion;  //  Fallthrough
                }
                case PlayEvasion:
                {
#if REF_STRUCT
                        int idx = FindBest();
                        Move next = list[idx].Move;
                        if (next != Move.Null && (next == ttMove))
                        {
                            return OrderNextMove();
                        }
#else
                        Move next = SelectNext(SelectNonTTs);
#endif

                        if (next != Move.Null)
                        {
                            return next;
                        }

                        _stage = End;
                        goto case End;
                }
                case End:
                _:
                {
                        return Move.Null;
                }
            };

            throw new Exception($"Movepicker exited during stage {_stage}?");
        }


        private bool SelectNonTTs(Move m)
        {
            return (m != ttMove);
        }

        private bool SelectQuiets(Move m)
        {
            return (m != ttMove && m != killerMove);
        }


        private void ScoreNoisyMoves()
        {
#if !REF_STRUCT
            ref HistoryTable history = ref pos.Owner.History;
#endif
            ref Bitboard bb = ref pos.bb;
            int pc = pos.ToMove;

            for (int i = listIndex; i < listSize; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                int capturedPiece = m.IsEnPassant ? Pawn 
                                  : m.IsPromotion ? Queen
                                  :                 bb.GetPieceAtIndex(moveTo);

                sm.Score = (OrderingVictimMult * GetPieceValue(capturedPiece)) +
                           (history.CaptureHistory[pc, bb.GetPieceAtIndex(moveFrom), moveTo, capturedPiece]);
            }
        }

        private void ScoreQuietMoves()
        {
#if !REF_STRUCT
            ref HistoryTable history = ref pos.Owner.History;
#endif
            ref Bitboard bb = ref pos.bb;
            int pc = pos.ToMove;

            for (int i = listIndex; i < listSize; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                int pt = bb.GetPieceAtIndex(moveFrom);
                int contIdx = PieceToHistory.GetIndex(pc, pt, moveTo);

                sm.Score  = 2 * history.MainHistory[pc, m];
                sm.Score += 2 * (*(ss - 1)->ContinuationHistory)[contIdx];
                sm.Score +=     (*(ss - 2)->ContinuationHistory)[contIdx];
                sm.Score +=     (*(ss - 4)->ContinuationHistory)[contIdx];
                sm.Score +=     (*(ss - 6)->ContinuationHistory)[contIdx];

                if ((pos.State->CheckSquares[pt] & SquareBB[moveTo]) != 0)
                {
                    sm.Score += OrderingCheckBonus;
                }
            }
        }

        private void ScoreEvasions()
        {
#if !REF_STRUCT
            ref HistoryTable history = ref pos.Owner.History;
#endif
            ref Bitboard bb = ref pos.bb;
            int pc = pos.ToMove;

            for (int i = listIndex; i < listSize; i++)
            {
                ref ScoredMove sm = ref list[i];
                Move m = sm.Move;
                int moveTo = m.To;
                int moveFrom = m.From;

                //int capturedPiece = m.IsEnPassant ? Pawn 
                //                  : m.IsCastle    ? None 
                //                  : bb.GetPieceAtIndex(moveTo);

                int capturedPiece = m.IsCastle ? None : bb.GetPieceAtIndex(moveTo);

                int ourPiece = bb.GetPieceAtIndex(moveFrom);

                if (capturedPiece != None)
                {
                    sm.Score = (OrderingVictimMult * GetPieceValue(capturedPiece)) +
                               (history.CaptureHistory[pc, ourPiece, moveTo, capturedPiece]);
                }
                else
                {
                    
                    int contIdx = PieceToHistory.GetIndex(pc, ourPiece, moveTo);

                    sm.Score  = 2 * history.MainHistory[pc, m];
                    sm.Score += 2 * (*(ss - 1)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 2)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 4)->ContinuationHistory)[contIdx];
                    sm.Score +=     (*(ss - 6)->ContinuationHistory)[contIdx];

                    if ((pos.State->CheckSquares[ourPiece] & SquareBB[moveTo]) != 0)
                    {
                        sm.Score += OrderingCheckBonus;
                    }
                }
            }
        }


        private int FindBest()
        {
            int maxIndex = listIndex;
            int max = list[maxIndex].Score;

            for (int i = listIndex + 1; i < listSize; i++)
            {
                if (list[i].Score > max)
                {
                    max = list[i].Score;
                    maxIndex = i;
                }
            }

            (list[maxIndex], list[listIndex]) = (list[listIndex], list[maxIndex]);

            return listIndex++;
        }

        private Move SelectNext(Predicate<Move> filter)
        {
            while (listIndex < listSize)
            {
                int idx = FindBest();
                Move move = list[idx].Move;

                if (filter(move))
                    return move;
            }

            return Move.Null;
        }

        private Move SelectNext()
        {
            if (listIndex < listSize)
            {
                int idx = FindBest();
                return list[idx].Move;
            }

            return Move.Null;
        }


        private static Move OrderNextMove(ScoredMove* moves, int size, int listIndex)
        {
            int max = int.MinValue;
            int maxIndex = listIndex;

            for (int i = listIndex; i < size; i++)
            {
                if (moves[i].Score > max)
                {
                    max = moves[i].Score;
                    maxIndex = i;
                }
            }

            (moves[maxIndex], moves[listIndex]) = (moves[listIndex], moves[maxIndex]);

            return moves[listIndex].Move;
        }
    
    
        public static class MovePickerStage
        {
            public const int NormalTT = 0;
            public const int MakeNoisy = 1;
            public const int PlayGoodNoisy = 2;
            public const int TryKiller = 3;
            public const int MakeQuiet = 4;
            public const int PlayQuiet = 5;
            public const int StartBadNoisy = 6;
            public const int PlayBadNoisy = 7;

            public const int EvasionTT = 10;
            public const int MakeEvasion = 11;
            public const int PlayEvasion = 12;

            public const int End = 100;
        }
    }
}
