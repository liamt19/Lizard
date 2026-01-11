//#define NO
using System.Runtime.CompilerServices;
using static Lizard.Logic.NN.FunUnrollThings;
using static Lizard.Logic.NN.NNUE;

namespace Lizard.Logic.NN
{
    public unsafe class AccumulatorStack
    {
        private readonly UnsafeVector<Accumulator> AccStack;
        private int HeadIndex;

        public Accumulator* Previous => AccStack[HeadIndex - 1];
        public Accumulator* Current => AccStack[HeadIndex];
        public Accumulator* Next => AccStack[HeadIndex + 1];

        public AccumulatorStack()
        {
            AccStack = new(256);
            Reset();
        }

        public void Reset()
        {
            HeadIndex = 0;
#if NO
            CurrentAccumulator = AccStack[HeadIndex];
#endif
            Current->MarkDirty();
        }

        public void MoveNext()
        {
            HeadIndex++;
            if (HeadIndex == AccStack.Size)
                AccStack.EmplaceBack();

#if NO
            CurrentAccumulator = AccStack[HeadIndex];
#endif
        }

        public void UndoMove()
        {
            Debug.Assert(HeadIndex > 0);
            HeadIndex--;

#if NO
            CurrentAccumulator = AccStack[HeadIndex];
#endif
        }

        public void MakeMove(Position pos, Move m)
        {
            ref var bb = ref pos.bb;

            MoveNext();

            Accumulator* src = Previous;
            Accumulator* dst = Current;

            dst->NeedsRefresh[0] = src->NeedsRefresh[0];
            dst->NeedsRefresh[1] = src->NeedsRefresh[1];

            dst->Computed[0] = dst->Computed[1] = false;

            int moveTo = m.To;
            int moveFrom = m.From;

            int us = pos.ToMove;
            int ourPiece = bb.GetPieceAtIndex(moveFrom);

            int them = Not(us);
            int theirPiece = bb.GetPieceAtIndex(moveTo);

            ref PerspectiveUpdate wUpdate = ref dst->Update[White];
            ref PerspectiveUpdate bUpdate = ref dst->Update[Black];

            //  Remove any updates that are present
            wUpdate.Clear();
            bUpdate.Clear();

            //  Refreshes are only required if our king moves to a different bucket
            if (ourPiece == King && (KingBuckets[moveFrom ^ (56 * us)] != KingBuckets[moveTo ^ (56 * us)]))
            {
                //  We will need to fully refresh our perspective, but we can still do theirs.
                dst->NeedsRefresh[us] = true;

                ref PerspectiveUpdate theirUpdate = ref dst->Update[them];

                int theirKing = pos.KingSquare(them);

                int from = FeatureIndexSingle(us, ourPiece, moveFrom, theirKing, them);
                int to = FeatureIndexSingle(us, ourPiece, moveTo, theirKing, them);

                if (theirPiece != None && !m.IsCastle)
                {
                    int cap = FeatureIndexSingle(them, theirPiece, moveTo, theirKing, them);
                    theirUpdate.PushSubSubAdd(from, cap, to);
                }
                else if (m.IsCastle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare();

                    to = FeatureIndexSingle(us, ourPiece, m.CastlingKingSquare(), theirKing, them);

                    int rookFrom = FeatureIndexSingle(us, Rook, rookFromSq, theirKing, them);
                    int rookTo = FeatureIndexSingle(us, Rook, rookToSq, theirKing, them);

                    theirUpdate.PushSubSubAddAdd(from, rookFrom, to, rookTo);
                }
                else
                {
                    theirUpdate.PushSubAdd(from, to);
                }
            }
            else
            {
                int wKing = pos.KingSquare(White);
                int bKing = pos.KingSquare(Black);

                (int wFrom, int bFrom) = FeatureIndex(us, ourPiece, moveFrom, wKing, bKing);
                (int wTo, int bTo) = FeatureIndex(us, m.IsPromotion ? m.PromotionTo : ourPiece, moveTo, wKing, bKing);

                if (m.IsCastle)
                {
                    int rookFromSq = moveTo;
                    int rookToSq = m.CastlingRookSquare();

                    (wTo, bTo) = FeatureIndex(us, ourPiece, m.CastlingKingSquare(), wKing, bKing);

                    (int wRookFrom, int bRookFrom) = FeatureIndex(us, Rook, rookFromSq, wKing, bKing);
                    (int wRookTo, int bRookTo) = FeatureIndex(us, Rook, rookToSq, wKing, bKing);

                    wUpdate.PushSubSubAddAdd(wFrom, wRookFrom, wTo, wRookTo);
                    bUpdate.PushSubSubAddAdd(bFrom, bRookFrom, bTo, bRookTo);
                }
                else if (theirPiece != None)
                {
                    (int wCap, int bCap) = FeatureIndex(them, theirPiece, moveTo, wKing, bKing);

                    wUpdate.PushSubSubAdd(wFrom, wCap, wTo);
                    bUpdate.PushSubSubAdd(bFrom, bCap, bTo);
                }
                else if (m.IsEnPassant)
                {
                    int idxPawn = moveTo - ShiftUpDir(us);

                    (int wCap, int bCap) = FeatureIndex(them, Pawn, idxPawn, wKing, bKing);

                    wUpdate.PushSubSubAdd(wFrom, wCap, wTo);
                    bUpdate.PushSubSubAdd(bFrom, bCap, bTo);
                }
                else
                {
                    wUpdate.PushSubAdd(wFrom, wTo);
                    bUpdate.PushSubAdd(bFrom, bTo);
                }
            }
        }

        public void RefreshIntoCache(Position pos)
        {
            RefreshIntoCache(pos, White);
            RefreshIntoCache(pos, Black);
        }

        public void RefreshIntoCache(Position pos, int perspective)
        {
            var accumulator = Current;
            ref Bitboard bb = ref pos.bb;

            var ourAccumulation = (short*)((*accumulator)[perspective]);
            Unsafe.CopyBlock(ourAccumulation, Bucketed768.Net.FTBiases, sizeof(short) * Bucketed768.L1_SIZE);
            accumulator->NeedsRefresh[perspective] = false;
            accumulator->Computed[perspective] = true;

            int ourKing = pos.KingSquare(perspective);
            ulong occ = bb.Occupancy;
            while (occ != 0)
            {
                int pieceIdx = poplsb(&occ);

                int pt = bb.GetPieceAtIndex(pieceIdx);
                int pc = bb.GetColorAtIndex(pieceIdx);

                int idx = FeatureIndexSingle(pc, pt, pieceIdx, ourKing, perspective);
                UnrollAdd(ourAccumulation, ourAccumulation, &Bucketed768.Net.FTWeights[idx]);
            }

            if (pos.Owner.CachedBuckets == null)
            {
                //  TODO: Upon SearchThread init, this isn't created yet :(
                return;
            }

            ref BucketCache cache = ref pos.Owner.CachedBuckets[BucketForPerspective(ourKing, perspective)];
            ref Bitboard entryBB = ref cache.Boards[perspective];
            ref Accumulator entryAcc = ref cache.Accumulator;

            accumulator->CopyTo(ref entryAcc, perspective);
            bb.CopyTo(ref entryBB);
        }

        public void RefreshFromCache(Position pos, int perspective)
        {
            var accumulator = Current;
            ref Bitboard bb = ref pos.bb;

            var ourKing = pos.KingSquare(perspective);

            ref BucketCache rtEntry = ref pos.Owner.CachedBuckets[BucketForPerspective(ourKing, perspective)];
            ref Bitboard entryBB = ref rtEntry.Boards[perspective];
            ref Accumulator entryAcc = ref rtEntry.Accumulator;

            var ourAccumulation = (short*)entryAcc[perspective];
            accumulator->NeedsRefresh[perspective] = false;

            for (int pc = 0; pc < ColorNB; pc++)
            {
                for (int pt = 0; pt < PieceNB; pt++)
                {
                    ulong prev = entryBB.Pieces[pt] & entryBB.Colors[pc];
                    ulong curr =      bb.Pieces[pt] &      bb.Colors[pc];

                    ulong added   = curr & ~prev;
                    ulong removed = prev & ~curr;

                    while (added != 0)
                    {
                        int sq = poplsb(&added);
                        int idx = FeatureIndexSingle(pc, pt, sq, ourKing, perspective);
                        UnrollAdd(ourAccumulation, ourAccumulation, Bucketed768.Net.FTWeights + idx);
                    }

                    while (removed != 0)
                    {
                        int sq = poplsb(&removed);
                        int idx = FeatureIndexSingle(pc, pt, sq, ourKing, perspective);
                        UnrollSubtract(ourAccumulation, ourAccumulation, Bucketed768.Net.FTWeights + idx);
                    }
                }
            }

            entryAcc.CopyTo(accumulator, perspective);
            bb.CopyTo(ref entryBB);

            accumulator->Computed[perspective] = true;
        }

        //  The general concept here is based off of Stormphrax's implementation:
        //  https://github.com/Ciekce/Stormphrax/commit/9b76f2a35531513239ed7078acc21294a11e75c6
        [MethodImpl(Inline)]
        public void EnsureUpdated(Position pos)
        {
            for (int perspective = 0; perspective < 2; perspective++)
            {
                //  If the current state is correct for our perspective, no work is needed
                if (Current->Computed[perspective])
                    continue;

                //  If the current state needs a refresh, don't bother with previous states
                if (Current->NeedsRefresh[perspective])
                {
                    RefreshFromCache(pos, perspective);
                    continue;
                }

                //  Find the most recent computed or refresh-needed accumulator
                Accumulator* curr = AccStack[HeadIndex - 1];
                while (!curr->Computed[perspective] && !curr->NeedsRefresh[perspective])
                    curr--;

                if (curr->NeedsRefresh[perspective])
                {
                    //  The most recent accumulator would need to be refreshed,
                    //  so don't bother and refresh the current one instead
                    RefreshFromCache(pos, perspective);
                }
                else
                {
                    //  Update incrementally till the current accumulator is correct
                    while (curr != Current)
                    {
                        Accumulator* prev = curr;
                        curr++;
                        ProcessUpdate(prev, curr, perspective);
                    }
                }

            }
        }

        [MethodImpl(Inline)]
        public void ProcessUpdate(Accumulator* prev, Accumulator* curr, int perspective)
        {
            ref var updates = ref curr->Update[perspective];

            Debug.Assert(updates.AddCnt != 0 || updates.SubCnt != 0);

            var src = (short*)((*prev)[perspective]);
            var dst = (short*)((*curr)[perspective]);

            var FeatureWeights = Bucketed768.Net.FTWeights;

            if (updates.AddCnt == 1 && updates.SubCnt == 1)
            {
                SubAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Adds[0]]);
            }
            else if (updates.AddCnt == 1 && updates.SubCnt == 2)
            {
                SubSubAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Subs[1]],
                    &FeatureWeights[updates.Adds[0]]);
            }
            else if (updates.AddCnt == 2 && updates.SubCnt == 2)
            {
                SubSubAddAdd(src, dst,
                    &FeatureWeights[updates.Subs[0]],
                    &FeatureWeights[updates.Subs[1]],
                    &FeatureWeights[updates.Adds[0]],
                    &FeatureWeights[updates.Adds[1]]);
            }

            curr->Computed[perspective] = true;
        }

    }
}
