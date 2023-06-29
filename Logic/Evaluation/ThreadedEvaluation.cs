using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static LTChess.Magic.MagicBitboards;
using static LTChess.Search.Endgame;
using static LTChess.Search.EvaluationConstants;

namespace LTChess.Search
{
    public unsafe class ThreadedEvaluation
    {
        //  https://github.com/official-stockfish/Stockfish/tree/master/src

        public const int ScoreMate = 32000;
        public const int ScoreDraw = 0;
        public const ulong EndgamePieces = 8;
        public const ulong TBEndgamePieces = 5;

        private Position p;
        private Bitboard bb;

        private int ToMove;
        private bool IsEndgame;
        private bool IsTBEndgame;

        private ulong white;
        private ulong black;
        private ulong all;
        private ulong whitePieces;
        private ulong blackPieces;

        private int whiteKing;
        private int blackKing;

        private ulong WhiteAttacks = 0UL;
        private ulong BlackAttacks = 0UL;

        private ulong whiteRing;
        private ulong whiteOutRing;

        private ulong blackRing;
        private ulong blackOutRing;

        private EvalByColor materialScore;
        private EvalByColor pawnScore;
        private EvalByColor bishopScore;
        private EvalByColor knightScore;
        private EvalByColor rookScore;
        private EvalByColor kingScore;
        private EvalByColor threatScore;
        private EvalByColor spaceScore;
        private EvalByColor queenScore;
        private EvalByColor endgameScore;

        public int Evaluate(in Position position, int pToMove, bool Trace = false)
        {
            p = position;
            bb = p.bb;
            ToMove = pToMove;

            white = bb.Colors[Color.White];
            black = bb.Colors[Color.Black];
            all = white | black;

            whitePieces = popcount(white);
            blackPieces = popcount(black);

            IsEndgame = (whitePieces + blackPieces) <= EndgamePieces;

            //  Also check if 
            if (!IsEndgame && popcount(bb.Pieces[Piece.Pawn] | bb.Pieces[Piece.Queen]) + 2 + 2 >
                (whitePieces + blackPieces))
            {
                IsEndgame = true;
            }

            IsTBEndgame = (whitePieces + blackPieces) <= TBEndgamePieces;

            int gamePhase = ((IsEndgame || IsTBEndgame) ? GamePhaseEndgame : GamePhaseNormal);

            //  The "king ring" evaluations see how many enemy pieces are attacking the squares nearby our king,
            //  Which is often but not necessarily always a bad thing.
            whiteKing = bb.KingIndex(Color.White);
            whiteRing = NeighborsMask[whiteKing];
            whiteOutRing = OutterNeighborsMask[whiteKing];

            blackKing = bb.KingIndex(Color.Black);
            blackRing = NeighborsMask[blackKing];
            blackOutRing = OutterNeighborsMask[blackKing];

            materialScore.Clear();
            pawnScore.Clear();
            bishopScore.Clear();
            knightScore.Clear();
            rookScore.Clear();
            kingScore.Clear();
            threatScore.Clear();
            spaceScore.Clear();
            queenScore.Clear();
            endgameScore.Clear();

            if (IsEndgame || IsTBEndgame)
            {
                gamePhase = GamePhaseEndgame;
                endgameScore = EvalEndgame(p, ToMove);
                endgameScore.Scale(ScaleEndgame);
            }
            else
            {
                EvalPawns();
                EvalKnights();
                EvalBishops();
                EvalRooks();
                EvalQueens();
                EvalKingSafety();
                EvalSpace();
            }

            pawnScore.Scale(ScalePawns[gamePhase]);
            knightScore.Scale(ScaleKnights[gamePhase]);
            bishopScore.Scale(ScaleBishops[gamePhase]);
            rookScore.Scale(ScaleRooks[gamePhase]);
            queenScore.Scale(ScaleQueens[gamePhase]);
            kingScore.Scale(ScaleKingSafety[gamePhase]);
            spaceScore.Scale(ScaleSpace[gamePhase]);

            threatScore.Scale(ScaleThreats[gamePhase]);
            materialScore.Scale(ScaleMaterial[gamePhase]);

            double scoreW = materialScore.white + pawnScore.white + knightScore.white + bishopScore.white + rookScore.white + kingScore.white + threatScore.white + spaceScore.white + endgameScore.white;
            double scoreB = materialScore.black + pawnScore.black + knightScore.black + bishopScore.black + rookScore.black + kingScore.black + threatScore.black + spaceScore.black + endgameScore.black;

            double scoreFinal = scoreW - scoreB;
            double relative = (scoreFinal * (ToMove == Color.White ? 1 : -1));

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
                Log("│     Endgame │ " + FormatEvalTerm(endgameScore.white) + " │ " + FormatEvalTerm(endgameScore.black) + " │ " + FormatEvalTerm(endgameScore.white - endgameScore.black) + " │ ");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│       Total │ " + FormatEvalTerm(scoreW) + " │ " + FormatEvalTerm(scoreB) + " │ " + FormatEvalTerm(scoreFinal) + " │ ");
                Log("└─────────────┴──────────┴──────────┴──────────┘");
                Log("Final: " + FormatEvalTerm(scoreFinal) + "\t\trelative\t" + FormatEvalTerm(relative));
                Log(ColorToString(ToMove) + " is " +
                    (relative < 0 ? "losing" : (relative > 0 ? "winning" : "equal")) + " by " + InCentipawns(scoreFinal) + " cp");
            }

            return (int)relative;
        }

        [MethodImpl(Inline)]
        private void EvalKvKEndgame()
        {
            int weak = (materialScore.white > materialScore.black ? Color.Black : Color.White);
            int strong = Not(weak);
            bool loneWeakKing = (bb.Colors[weak] == SquareBB[bb.KingIndex(weak)]);

            int kingDist = SquareDistances[whiteKing][blackKing];

            bool KPvK = ((bb.Pieces[Piece.Queen] | bb.Pieces[Piece.Rook]) == 0 && bb.Pieces[Piece.Pawn] != 0 && loneWeakKing);
            int nearestPawn = NearestPawn(bb, strong);
            if (weak == Color.Black)
            {
                if (KPvK)
                {
                    int pawnDist = SquareDistances[whiteKing][nearestPawn];
                    endgameScore.white -= ScoreEGKingDistance[pawnDist];
                }
                else
                {
                    endgameScore.black += (PSQT.EGWeakKingPosition[blackKing] * CoefficientPSQTEKG);
                    endgameScore.white += ScoreEGKingDistance[kingDist];
                }
            }
            else
            {
                if (KPvK)
                {
                    int pawnDist = SquareDistances[blackKing][nearestPawn];
                    endgameScore.black -= ScoreEGKingDistance[pawnDist];
                }
                else
                {
                    endgameScore.white += (PSQT.EGWeakKingPosition[whiteKing] * CoefficientPSQTEKG);
                    endgameScore.black += ScoreEGKingDistance[kingDist];
                }
            }
        }

        [MethodImpl(Inline)]
        private void EvalPawns()
        {
            ulong whitePawns = (white & bb.Pieces[Piece.Pawn]);
            ulong blackPawns = (black & bb.Pieces[Piece.Pawn]);

            ulong whiteDoubledPawns = Forward(Color.White, whitePawns) & whitePawns;
            pawnScore.white += ((int)popcount(whiteDoubledPawns) * ScorePawnDoubled);

            ulong whiteDoubledDistantPawns = Forward(Color.White, Forward(Color.White, whitePawns)) & whitePawns;
            pawnScore.white += ((int)popcount(whiteDoubledDistantPawns) * ScorePawnDoubledDistant);

            ulong blackDoubledPawns = Forward(Color.Black, blackPawns) & blackPawns;
            pawnScore.black += ((int)popcount(blackDoubledPawns) * ScorePawnDoubled);

            ulong blackDoubledDistantPawns = Forward(Color.Black, Forward(Color.Black, blackPawns)) & blackPawns;
            pawnScore.black += ((int)popcount(blackDoubledDistantPawns) * ScorePawnDoubledDistant);

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

                        int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                //  TODO: not doing this here anymore
                if (IsEndgame)
                {
                    if (bb.IsPasser(idx))
                    {
                        pawnScore.white += ScorePasser;
                    }


                    //  If black only has pawns and is too far to stop this pawn from promoting, then add bonus.
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
                pawnScore.white += (PSQT.PawnCenter[idx] * CoefficientPSQTCenter);

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

                        int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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
                    if (bb.IsPasser(idx))
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
                pawnScore.black += (PSQT.PawnCenter[idx] * CoefficientPSQTCenter);

                materialScore.black += ValuePawn;

                blackPawns = poplsb(blackPawns);
            }
        }

        [MethodImpl(Inline)]
        private void EvalKnights()
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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                knightScore.white += (PSQT.Center[idx] * CoefficientPSQTCenter * CoefficientPSQTKnights);

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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                knightScore.black += (PSQT.Center[idx] * CoefficientPSQTCenter * CoefficientPSQTKnights);

                materialScore.black += ValueKnight;

                blackKnights = poplsb(blackKnights);
            }
        }

        [MethodImpl(Inline)]
        private void EvalBishops()
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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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
        private void EvalRooks()
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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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
        private void EvalQueens()
        {
            ulong whiteQueens = white & bb.Pieces[Piece.Queen];
            ulong blackQueens = black & bb.Pieces[Piece.Queen];

            while (whiteQueens != 0)
            {
                int idx = lsb(whiteQueens);

                ulong thisMoves = (GetBishopMoves(all, idx) | GetRookMoves(all, idx));
                WhiteAttacks |= (thisMoves & ~white);

                queenScore.white += (popcount(thisMoves) * ScoreQueenSquares);

                ulong thisAttacks = thisMoves & black;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                materialScore.white += ValueQueen;

                whiteQueens = poplsb(whiteQueens);
            }

            while (blackQueens != 0)
            {
                int idx = lsb(blackQueens);

                ulong thisMoves = (GetBishopMoves(all, idx) | GetRookMoves(all, idx));
                BlackAttacks |= (thisMoves & ~black);

                queenScore.black += (popcount(thisMoves) * ScoreQueenSquares);

                ulong thisAttacks = thisMoves & white;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);
                    int theirPiece = bb.PieceTypes[attackIdx];
                    int theirValue = GetPieceValue(theirPiece);

                    int defenders = (int)popcount(DefendersOf(bb, attackIdx));
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

                materialScore.black += ValueQueen;

                blackQueens = poplsb(blackQueens);
            }
        }

        [MethodImpl(Inline)]
        private void EvalKingSafety()
        {
            double wAtt = popcount(blackRing & WhiteAttacks);
            double wAttOut = popcount(blackOutRing & WhiteAttacks);
            kingScore.black -= (wAtt * ScoreKingRingAttack);
            kingScore.black -= (wAttOut * ScoreKingOutterRingAttack);

            double bAtt = popcount(whiteRing & BlackAttacks);
            double bAttOut = popcount(whiteOutRing & BlackAttacks);
            kingScore.white -= (bAtt * ScoreKingRingAttack);
            kingScore.white -= (bAttOut * ScoreKingOutterRingAttack);

            //  I really don't know why it likes playing Ka1/Kh1 so often,
            //  so giving it a small penalty here to stop it from doing so
            //  unless it is necessary.
            if ((SquareBB[whiteKing] & Corners) != 0)
            {
                kingScore.white += ScoreKingInCorner;
            }

            if ((SquareBB[blackKing] & Corners) != 0)
            {
                kingScore.black += ScoreKingInCorner;
            }


            //  Divide the threat score because it might be risky to actually capture the piece (deflection)
            double riskCoefficient = 1;

            //  Check if White's king is threatening any material
            ulong wKingAttacks = whiteRing & black;
            while (wKingAttacks != 0)
            {
                int attackIdx = lsb(wKingAttacks);
                int theirPiece = bb.PieceTypes[attackIdx];
                int theirValue = GetPieceValue(theirPiece);

                int defenders = (int)popcount(DefendersOf(bb, attackIdx));
                if (defenders == 0)
                {
                    threatScore.white += (theirValue * CoefficientHanging) / riskCoefficient;
                }
                else
                {
                    //  If the piece is actually defended, this is a bad thing,
                    //  so give a bonus to our opponent based on the number of pieces they have defending it.
                    threatScore.black += defenders * ScoreDefendedPieceNearKing * (theirValue / ScoreDefendedPieceNearKingCoeff);
                }
                wKingAttacks = poplsb(wKingAttacks);
            }

            ulong bKingAttacks = blackRing & white;
            while (bKingAttacks != 0)
            {
                int attackIdx = lsb(bKingAttacks);
                int theirPiece = bb.PieceTypes[attackIdx];
                int theirValue = GetPieceValue(theirPiece);

                int defenders = (int)popcount(DefendersOf(bb, attackIdx));
                if (defenders == 0)
                {
                    threatScore.black += (theirValue * CoefficientHanging) / riskCoefficient;
                }
                else
                {
                    threatScore.white += defenders * ScoreDefendedPieceNearKing * (theirValue / ScoreDefendedPieceNearKingCoeff);
                }
                bKingAttacks = poplsb(bKingAttacks);
            }
        }

        [MethodImpl(Inline)]
        private void EvalSpace()
        {
            //  TODO: actually do this...

            spaceScore.white = ((int)popcount(WhiteAttacks) * ScorePerSquare);
            spaceScore.black = ((int)popcount(BlackAttacks) * ScorePerSquare);

            ulong undevelopedWhite = ((Rank1BB & bb.Colors[Color.White]) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Knight]));
            ulong undevelopedBlack = ((Rank8BB & bb.Colors[Color.Black]) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Knight]));

            spaceScore.white += ((int)popcount(undevelopedWhite) * ScoreUndevelopedPiece);
            spaceScore.black += ((int)popcount(undevelopedBlack) * ScoreUndevelopedPiece);

            if (p.whiteCastled)
            {
                spaceScore.white += ScoreKingCastled;
            }

            if (p.blackCastled)
            {
                spaceScore.black += ScoreKingCastled;
            }
        }

        [MethodImpl]
        public int NearestPawn(in Bitboard bb, int color)
        {
            int nearestIdx = 0;
            int nearestDist = 64;

            int ourKing = bb.KingIndex(color);

            ulong temp = bb.Pieces[Piece.Pawn] & bb.Colors[color];
            while (temp != 0)
            {
                int idx = lsb(temp);

                int thisDist = SquareDistances[ourKing][idx];
                if (thisDist < nearestDist)
                {
                    nearestDist = thisDist;
                    nearestIdx = idx;
                }

                temp = poplsb(temp);
            }

            return nearestIdx;
        }

        /// <summary>
        /// Returns true if the pawn on <paramref name="idx"/> can promote before the other color's king can capture it.
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public bool WillPromote(int idx)
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
        public bool IsIsolated(int idx)
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
        public bool IsFileSemiOpen(int idx)
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
        public bool IsFileOpen(int idx)
        {
            return ((GetFileBB(idx) & bb.Pieces[Piece.Pawn]) == 0);
        }

        [MethodImpl(Inline)]
        public static bool IsScoreMate(int score, out int mateIn)
        {
            int abs = Math.Abs(Math.Abs(score) - ScoreMate);
            mateIn = abs;

            if (abs < MaxDepth)
            {
                return true;
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static int GetPieceValue(int pt)
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
