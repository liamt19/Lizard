using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static LTChess.Magic.MagicBitboards;
using static LTChess.Search.EvaluationConstants;

namespace LTChess.Search
{
    public static unsafe class Evaluation
    {
        //  https://github.com/official-stockfish/Stockfish/tree/master/src

        public const int ScoreMate = 32000;
        public const int ScoreDraw = 0;
        public const ulong EndgamePieces = 8;

        private static Bitboard bb;

        private static int ToMove;
        private static bool IsEndgame;

        private static ulong white;
        private static ulong black;
        private static ulong all;
        private static ulong whitePieces;
        private static ulong blackPieces;

        private static int whiteKing;
        private static int blackKing;

        private static ulong WhiteAttacks = 0UL;
        private static ulong BlackAttacks = 0UL;

        private static ulong whiteRing;
        private static ulong whiteOutRing;

        private static ulong blackRing;
        private static ulong blackOutRing;

        private static EvalByColor materialScore;
        private static EvalByColor pawnScore;
        private static EvalByColor bishopScore;
        private static EvalByColor knightScore;
        private static EvalByColor rookScore;
        private static EvalByColor kingScore;
        private static EvalByColor threatScore;
        private static EvalByColor spaceScore;
        private static EvalByColor queenScore;

        public static int Evaluate(in Bitboard pBB, int pToMove, bool Trace = false)
        {
            bb = pBB;
            ToMove = pToMove;

            white = bb.Colors[Color.White];
            black = bb.Colors[Color.Black];
            all = white | black;


            whiteKing = bb.KingIndex(Color.White);
            whiteRing = NeighborsMask[whiteKing];
            whiteOutRing = OutterNeighborsMask[whiteKing];

            blackKing = bb.KingIndex(Color.Black);
            blackRing = NeighborsMask[blackKing];
            blackOutRing = OutterNeighborsMask[blackKing];

            whitePieces = popcount(white);
            blackPieces = popcount(black);

            IsEndgame = (whitePieces + blackPieces) <= EndgamePieces;

            materialScore.Clear();
            pawnScore.Clear();
            bishopScore.Clear();
            knightScore.Clear();
            rookScore.Clear();
            kingScore.Clear();
            threatScore.Clear();
            spaceScore.Clear();
            queenScore.Clear();

            EvalPawns();
            EvalKnights();
            EvalBishops();
            EvalRooks();
            EvalQueens();
            EvalKingSafety();
            EvalSpace();

            pawnScore.Scale(ScalePawns);
            knightScore.Scale(ScaleKnights);
            bishopScore.Scale(ScaleBishops);
            rookScore.Scale(ScaleRooks);
            queenScore.Scale(ScaleQueens);
            kingScore.Scale(ScaleKingSafety);
            spaceScore.Scale(ScaleSpace);

            threatScore.Scale(ScaleThreats);
            materialScore.Scale(ScaleMaterial);

            double scoreW = materialScore.white + pawnScore.white + knightScore.white + bishopScore.white + rookScore.white + kingScore.white + threatScore.white + spaceScore.white;
            double scoreB = materialScore.black + pawnScore.black + knightScore.black + bishopScore.black + rookScore.black + kingScore.black + threatScore.black + spaceScore.black;

            double scoreFinal = scoreW - scoreB;

            if (Trace)
            {
                Log("┌─────────────┬──────────┬──────────┬──────────┐");
                Log("│     Term    │   White  │   Black  │   Total  │");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│    Material │ " + FormatEvalTerm(materialScore.white) + " │ " + FormatEvalTerm(materialScore.black) + " │ " + FormatEvalTerm(materialScore.white - materialScore.black) + " │ ");
                Log("│       Pawns │ " + FormatEvalTerm(pawnScore.white) + " │ " + FormatEvalTerm(pawnScore.black) + " │ " + FormatEvalTerm(pawnScore.white - pawnScore.black) + " │ ");
                Log("│     Knights │ " + FormatEvalTerm(knightScore.white) + " │ " + FormatEvalTerm(knightScore.black) + " │ " + FormatEvalTerm(knightScore.white - knightScore.black) + " │ ");
                Log("│     Bishops │ " + FormatEvalTerm(bishopScore.white) + " │ " + FormatEvalTerm(bishopScore.black) + " │ " + FormatEvalTerm(bishopScore.white - bishopScore.black) + " │ ");
                Log("│       Rooks │ " + FormatEvalTerm(rookScore.white) + " │ " + FormatEvalTerm(rookScore.black) + " │ " + FormatEvalTerm(rookScore.white - rookScore.black) + " │ ");
                Log("│ King safety │ " + FormatEvalTerm(kingScore.white) + " │ " + FormatEvalTerm(kingScore.black) + " │ " + FormatEvalTerm(kingScore.white - kingScore.black) + " │ ");
                Log("│     Threats │ " + FormatEvalTerm(threatScore.white) + " │ " + FormatEvalTerm(threatScore.black) + " │ " + FormatEvalTerm(threatScore.white - threatScore.black) + " │ ");
                Log("│       Space │ " + FormatEvalTerm(spaceScore.white) + " │ " + FormatEvalTerm(spaceScore.black) + " │ " + FormatEvalTerm(spaceScore.white - spaceScore.black) + " │ ");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│       Total │ " + FormatEvalTerm(scoreW) + " │ " + FormatEvalTerm(scoreB) + " │ " + FormatEvalTerm(scoreFinal) + " │ ");
                Log("└─────────────┴──────────┴──────────┴──────────┘");
                Log("Final: " + FormatEvalTerm(scoreFinal) + "\t\trelative\t" + FormatEvalTerm((scoreFinal * (ToMove == Color.White ? 1 : -1))));
            }


            return (int)scoreFinal;
        }

        [MethodImpl(Inline)]
        private static void EvalPawns()
        {
            ulong whitePawns = (white & bb.Pieces[Piece.Pawn]);
            ulong blackPawns = (black & bb.Pieces[Piece.Pawn]);

            while (whitePawns != 0)
            {
                int idx = lsb(whitePawns);

                ulong thisPawnAttacks = WhitePawnAttackMasks[idx];
                WhiteAttacks |= thisPawnAttacks;

                while (thisPawnAttacks != 0)
                {
                    int attackIdx = lsb(thisPawnAttacks);

                    if ((white & SquareBB[attackIdx]) != 0)
                    {
                        pawnScore.white += ScorePawnSupport;
                    }
                    else if ((black & SquareBB[attackIdx]) != 0)
                    {
                        int theirPiece = bb.PieceTypes[attackIdx];
                        int theirValue = GetPieceValue(theirPiece);

                        int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.White, white, black));
                        if (defenders == 0)
                        {
                            threatScore.white += (theirValue * CoefficientHanging);
                        }
                        else
                        {
                            double totalValue = theirValue - ValuePawn;
                            if (totalValue > 0)
                            {
                                threatScore.white += (totalValue * CoefficientPositiveTrade);
                            }
                        }
                    }

                    thisPawnAttacks = poplsb(thisPawnAttacks);
                }

                //  TODO: Bitboards/masks for determining if a knight geometrically can't stop the pawn.

                if (IsEndgame)
                {
                    if (IsPasser(bb, idx))
                    {
                        pawnScore.white += ScorePasser;
                    }

                    //  If black only has pawns and is too far to stop this pwan from promoting, then add bonus.
                    if (popcount(black & bb.Pieces[Piece.Pawn]) == blackPieces - 1 && WillPromote(idx))
                    {
                        pawnScore.white += ScorePromotingPawn;
                    }
                }

                if (IsIsolated(idx))
                {
                    pawnScore.white += ScoreIsolatedPawn;
                }

                pawnScore.white += (PSQT.WhitePawns[idx] * CoefficientPSQTPawns);
                pawnScore.white += (PSQT.Center[idx] * CoefficientPSQTCenter);

                materialScore.white += ValuePawn;

                whitePawns = poplsb(whitePawns);
            }

            while (blackPawns != 0)
            {
                int idx = lsb(blackPawns);

                ulong thisPawnAttacks = BlackPawnAttackMasks[idx];
                BlackAttacks |= thisPawnAttacks;

                while (thisPawnAttacks != 0)
                {
                    int attackIdx = lsb(thisPawnAttacks);

                    if ((black & SquareBB[attackIdx]) != 0)
                    {
                        pawnScore.black += ScorePawnSupport;
                    }
                    else if ((white & SquareBB[attackIdx]) != 0)
                    {
                        int theirPiece = bb.PieceTypes[attackIdx];
                        int theirValue = GetPieceValue(theirPiece);

                        int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.Black, black, white));
                        if (defenders == 0)
                        {
                            threatScore.black += (theirValue * CoefficientHanging);
                        }
                        else
                        {
                            double totalValue = theirValue - ValuePawn;
                            if (totalValue > 0)
                            {
                                threatScore.black += (totalValue * CoefficientPositiveTrade);
                            }
                        }
                    }

                    thisPawnAttacks = poplsb(thisPawnAttacks);
                }
                if (IsEndgame)
                {
                    if (IsPasser(bb, idx))
                    {
                        pawnScore.black += ScorePasser;
                    }
                    if (popcount(white & bb.Pieces[Piece.Pawn]) == whitePieces - 1 && WillPromote(idx))
                    {
                        pawnScore.black += ScorePromotingPawn;
                    }
                    
                }

                if (IsIsolated(idx))
                {
                    pawnScore.black += ScoreIsolatedPawn;
                }

                pawnScore.black += (PSQT.BlackPawns[idx] * CoefficientPSQTPawns);
                pawnScore.black += (PSQT.Center[idx] * CoefficientPSQTCenter);

                materialScore.black += ValuePawn;

                blackPawns = poplsb(blackPawns);
            }
        }

        [MethodImpl(Inline)]
        private static void EvalKnights()
        {
            ulong whiteKnights = white & bb.Pieces[Piece.Knight];
            ulong blackKnights = black & bb.Pieces[Piece.Knight];

            while (whiteKnights != 0)
            {
                int idx = lsb(whiteKnights);

                ulong thisAttacks = KnightMasks[idx] & black;
                WhiteAttacks |= KnightMasks[idx];

                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.White, white, black));
                    if (defenders == 0)
                    {
                        threatScore.white += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueKnight;
                        if (totalValue > 0)
                        {
                            threatScore.white += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                knightScore.white += (PSQT.Center[idx] * CoefficientPSQTKnights);

                materialScore.white += ValueKnight;

                whiteKnights = poplsb(whiteKnights);
            }

            while (blackKnights != 0)
            {
                int idx = lsb(blackKnights);

                ulong thisAttacks = KnightMasks[idx] & white;
                BlackAttacks |= KnightMasks[idx];

                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.Black, black, white));
                    if (defenders == 0)
                    {
                        threatScore.black += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueKnight;
                        if (totalValue > 0)
                        {
                            threatScore.black += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                knightScore.black += (PSQT.Center[idx] * CoefficientPSQTKnights);

                materialScore.black += ValueKnight;

                blackKnights = poplsb(blackKnights);
            }
        }

        [MethodImpl(Inline)]
        private static void EvalBishops()
        {
            ulong whiteBishops = white & bb.Pieces[Piece.Bishop];
            ulong blackBishops = black & bb.Pieces[Piece.Bishop];

            if (popcount(whiteBishops) == 2)
            {
                bishopScore.white += ScoreBishopPair;
            }
            if (popcount(blackBishops) == 2)
            {
                bishopScore.black += ScoreBishopPair;
            }

            while (whiteBishops != 0)
            {
                int idx = lsb(whiteBishops);

                ulong thisMoves = GetBishopMoves(all, idx);
                WhiteAttacks |= (thisMoves & ~white);

                ulong thisAttacks = thisMoves & black;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.White, white, black));
                    if (defenders == 0)
                    {
                        threatScore.white += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueBishop;
                        if (totalValue > 0)
                        {
                            threatScore.white += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore.white += ValueBishop;

                whiteBishops = poplsb(whiteBishops);
            }

            while (blackBishops != 0)
            {
                int idx = lsb(blackBishops);

                ulong thisMoves = GetBishopMoves(all, idx);
                BlackAttacks |= (thisMoves & ~black);

                ulong thisAttacks = GetBishopMoves(all, idx) & white;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.Black, black, white));
                    if (defenders == 0)
                    {
                        threatScore.black += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueBishop;
                        if (totalValue > 0)
                        {
                            threatScore.black += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore.black += ValueBishop;

                blackBishops = poplsb(blackBishops);
            }

        }

        [MethodImpl(Inline)]
        private static void EvalRooks()
        {
            ulong whiteRooks = white & bb.Pieces[Piece.Rook];
            ulong blackRooks = black & bb.Pieces[Piece.Rook];

            while (whiteRooks != 0)
            {
                int idx = lsb(whiteRooks);

                ulong thisMoves = GetRookMoves(all, idx);
                WhiteAttacks |= (thisMoves & ~white);

                ulong thisAttacks = thisMoves & black;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.White, white, black));
                    if (defenders == 0)
                    {
                        threatScore.white += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueRook;
                        if (totalValue > 0)
                        {
                            threatScore.white += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore.white += ValueRook;

                if (IsFileOpen(idx))
                {
                    rookScore.white += ScoreRookOpenFile;
                }
                else if (IsFileSemiOpen(idx))
                {
                    rookScore.white += ScoreRookSemiOpenFile;
                }

                whiteRooks = poplsb(whiteRooks);
            }

            while (blackRooks != 0)
            {
                int idx = lsb(blackRooks);

                ulong thisMoves = GetRookMoves(all, idx);
                BlackAttacks |= (thisMoves & ~black);

                ulong thisAttacks = thisMoves & white;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.Black, black, white));
                    if (defenders == 0)
                    {
                        threatScore.black += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueRook;
                        if (totalValue > 0)
                        {
                            threatScore.black += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore.black += ValueRook;

                if (IsFileOpen(idx))
                {
                    rookScore.black += ScoreRookOpenFile;
                }
                else if (IsFileSemiOpen(idx))
                {
                    rookScore.black += ScoreRookSemiOpenFile;
                }

                blackRooks = poplsb(blackRooks);
            }

        }

        [MethodImpl(Inline)]
        private static void EvalQueens()
        {
            ulong whiteQueens = white & bb.Pieces[Piece.Queen];
            ulong blackQueens = black & bb.Pieces[Piece.Queen];

            while (whiteQueens != 0)
            {
                int idx = lsb(whiteQueens);

                ulong thisMoves = (GetBishopMoves(all, idx) | GetRookMoves(all, idx));
                WhiteAttacks |= (thisMoves & ~white);

                ulong thisAttacks = thisMoves & black;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.White, white, black));
                    if (defenders == 0)
                    {
                        threatScore.white += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueQueen;
                        if (totalValue > 0)
                        {
                            threatScore.white += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                //  TODO: IsPinned sucks and there is a better way to figure this out.
                //  Opponent gets a bonus if they are pinning our queen
                //if (false && PositionUtilities.IsPinned(bb, idx, Color.White, whiteKing, out int pinner))
                //{
                //    int theirPiece = bb.PieceTypes[pinner];
                //    int theirValue = GetPieceValue(theirPiece);

                //    queenScore.black += ((ValueQueen - theirValue) * CoefficientPinnedQueen);
                //}

                materialScore.white += ValueQueen;

                whiteQueens = poplsb(whiteQueens);
            }

            while (blackQueens != 0)
            {
                int idx = lsb(blackQueens);

                ulong thisMoves = (GetBishopMoves(all, idx) | GetRookMoves(all, idx));
                BlackAttacks |= (thisMoves & ~black);

                ulong thisAttacks = thisMoves & white;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx, Color.Black, black, white));
                    if (defenders == 0)
                    {
                        threatScore.black += (theirValue * CoefficientHanging);
                    }
                    else
                    {
                        double totalValue = theirValue - ValueQueen;
                        if (totalValue > 0)
                        {
                            threatScore.black += (totalValue * CoefficientPositiveTrade);
                        }
                    }
                    thisAttacks = poplsb(thisAttacks);
                }

                //  Opponent gets a bonus if they are pinning our queen
                //if (PositionUtilities.IsPinned(bb, idx, Color.Black, blackKing, out int pinner))
                //{
                //    int theirPiece = bb.PieceTypes[pinner];
                //    int theirValue = GetPieceValue(theirPiece);

                //    queenScore.white += ((ValueQueen - theirValue) * CoefficientPinnedQueen);
                //}

                materialScore.black += ValueQueen;

                blackQueens = poplsb(blackQueens);
            }
        }

        [MethodImpl(Inline)]
        private static void EvalKingSafety()
        {
            double wAtt = popcount(blackRing & WhiteAttacks);
            double wAttOut = popcount(blackOutRing & WhiteAttacks);
            kingScore.black -= (wAtt * ScoreKingRingAttack);
            kingScore.black -= (wAttOut * ScoreKingOutterRingAttack);

            double bAtt = popcount(whiteRing & BlackAttacks);
            double bAttOut = popcount(whiteOutRing & BlackAttacks);
            kingScore.white -= (bAtt * ScoreKingRingAttack);
            kingScore.white -= (bAttOut * ScoreKingOutterRingAttack);

        }

        [MethodImpl(Inline)]
        private static void EvalSpace()
        {
            //  TODO: actually do this...

            spaceScore.white = ((int)popcount(WhiteAttacks) * ScorePerSquare);
            spaceScore.black = ((int)popcount(BlackAttacks) * ScorePerSquare);
        }

        [MethodImpl(Inline)]
        private static ulong _CountDefenders(int idx, bool IgnoreKing = false)
        {
            int ourColor = bb.GetColorAtIndex(idx);

            ulong us;
            ulong[] pawnBB;

            if (ourColor == Color.White)
            {
                us = white;
                pawnBB = WhitePawnAttackMasks;
            }
            else
            {
                us = black;
                pawnBB = BlackPawnAttackMasks;
            }

            ulong ourBishopDefenders = GenPseudoSlidersMask(bb, idx, Piece.Bishop, Not(ourColor)) & us & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Queen]);
            ulong ourRookDefenders = GenPseudoSlidersMask(bb, idx, Piece.Rook, Not(ourColor)) & us & (bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Queen]);

            ulong ourKnightDefenders = (bb.Pieces[Piece.Knight] & us & KnightMasks[idx]);
            ulong ourPawnDefenders = (bb.Pieces[Piece.Pawn] & us & pawnBB[idx]);

            ulong all = (ourBishopDefenders | ourRookDefenders | ourKnightDefenders | ourPawnDefenders);

            if (!IgnoreKing)
            {
                all |= (bb.Pieces[Piece.King] & us & NeighborsMask[idx]);
            }

            int def = (int)popcount(all);
            return all;
        }

        /// <summary>
        /// Returns true if the pawn on <paramref name="idx"/> is a passed pawn.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public static bool IsPasser(in Bitboard bb, int idx)
        {
            int ourColor = bb.GetColorAtIndex(idx);
            ulong them = (ourColor == Color.White) ? black : white;
            ulong theirPawns = (them & bb.Pieces[Piece.Pawn]);

            int ourFile = GetIndexFile(idx);

            while (theirPawns != 0)
            {
                int i = lsb(theirPawns);

                int file = GetIndexFile(i);
                if (Math.Abs(file - ourFile) <= 1)
                {
                    return false;
                }

                theirPawns = poplsb(theirPawns);
            }

            return true;
        }

        /// <summary>
        /// Returns true if the pawn on <paramref name="idx"/> can promote before the other color's king can capture it.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public static bool WillPromote(int idx)
        {
            int ourColor = bb.GetColorAtIndex(idx);
            int theirKing = bb.KingIndex(Not(ourColor));
            int promotionSquare = CoordToIndex(GetIndexFile(idx), ourColor == Color.White ? 7 : 0);

            int pawnDistance = SquareDistances[idx][promotionSquare];
            int kingDistance = SquareDistances[theirKing][promotionSquare];

            return (pawnDistance < kingDistance);
        }

        /// <summary>
        /// Returns true if the pawn on <paramref name="idx"/> doesn't have any pawns on the files beside it.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsIsolated(int idx)
        {
            int ourColor = bb.GetColorAtIndex(idx);
            ulong us = (ourColor == Color.White) ? white : black;
            ulong ourPawns = (us & bb.Pieces[Piece.Pawn]);

            ulong fileMask = 0UL;
            if (GetIndexFile(idx) < Files.H)
            {
                fileMask |= GetFileBB(idx + 1);
            }
            if (GetIndexFile(idx) > Files.A)
            {
                fileMask |= GetFileBB(idx - 1);
            }

            return ((fileMask & ourPawns) == 0);
        }


        /// <summary>
        /// Returns true if the file of <paramref name="idx"/> is semi-open, which means there are pawns of only one color on it.
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsFileSemiOpen(int idx)
        {
            ulong whitePawns = bb.Pieces[Piece.Pawn] & white;
            ulong blackPawns = bb.Pieces[Piece.Pawn] & black;

            ulong fileMask = GetFileBB(idx);
            if ((fileMask & whitePawns) == 0)
            {
                return ((fileMask & blackPawns) != 0);
            }
            
            return ((fileMask & blackPawns) == 0);
        }

        /// <summary>
        /// Returns true if the file of <paramref name="idx"/> is an open file, meaning there aren't any pawns on it
        /// </summary>
        [MethodImpl(Inline)]
        public static bool IsFileOpen(int idx)
        {
            return ((GetFileBB(idx) & bb.Pieces[Piece.Pawn]) == 0);
        }

        [MethodImpl(Inline)]
        public static bool IsScoreMate(int score, out int mateIn)
        {
            int abs = Math.Abs(score - ScoreMate);
            mateIn = abs;

            if (abs < MAX_DEPTH)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(Inline)]
        private static int GetPieceValue(int pt)
        {
            switch (pt)
            {
                case Piece.Pawn:
                    return ValuePawn;
                case Piece.Knight:
                    return ValueKnight;
                case Piece.Bishop:
                    return ValueBishop;
                case Piece.Rook:
                    return ValueRook;
                case Piece.Queen:
                    return ValueQueen;
                default:
                    break;
            }

            return 0;
        }
    }
}
