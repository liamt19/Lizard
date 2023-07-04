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

        public const int ScoreNone = short.MaxValue - 7;
        public const int ScoreMate = 32000;
        public const int ScoreDraw = 0;

        public const ulong EndgamePieces = 8;
        public const ulong TBEndgamePieces = 5;

        private Position p;
        private Bitboard bb;

        private int gamePhase;

        private ulong whitePieces;
        private ulong blackPieces;

        private ulong[] AttackMask;

        private double[] materialScore;
        private double[] kingScore;
        private double[] threatScore;
        private double[] spaceScore;
        private double[] endgameScore;

        private double[] pawnScore = new double[2];
        private double[] bishopScore = new double[2];
        private double[] knightScore = new double[2];
        private double[] rookScore = new double[2];
        private double[] queenScore = new double[2];


        [MethodImpl(Inline)]
        public int Evaluate(in Position position, int pc, bool Trace = false)
        {
            p = position;
            bb = p.bb;

            AttackMask = new ulong[2];
            materialScore = new double[2];
            kingScore = new double[2];
            threatScore = new double[2];
            spaceScore = new double[2];
            endgameScore = new double[2];

            whitePieces = popcount(bb.Colors[Color.White]);
            blackPieces = popcount(bb.Colors[Color.Black]);

            bool IsEndgame = (whitePieces + blackPieces) <= EndgamePieces;

            //  Also check if there are just queens and pawns, and possibly 2 (+2 kings) other pieces.
            if (!IsEndgame && popcount(bb.Pieces[Piece.Pawn] | bb.Pieces[Piece.Queen]) + 2 + 2 > (whitePieces + blackPieces))
            {
                IsEndgame = true;
            }

            bool IsTBEndgame = (whitePieces + blackPieces) <= TBEndgamePieces;

            int gamePhase = ((IsEndgame || IsTBEndgame) ? GamePhaseEndgame : GamePhaseNormal);

            if (IsEndgame || IsTBEndgame)
            {
                gamePhase = GamePhaseEndgame;
                endgameScore = EvalEndgame(p, pc);
            }

            pawnScore = new[]   { EvalPawns  (Color.White), EvalPawns(Color.Black) };
            knightScore = new[] { EvalKnights(Color.White), EvalKnights(Color.Black) };
            bishopScore = new[] { EvalBishops(Color.White), EvalBishops(Color.Black) };
            rookScore = new[]   { EvalRooks  (Color.White), EvalRooks(Color.Black) };
            queenScore = new[]  { EvalQueens (Color.White), EvalQueens(Color.Black) };
            spaceScore = new[]  { EvalSpace  (Color.White), EvalSpace(Color.Black) };
            kingScore = new[]   { EvalKingSafety(Color.White), EvalKingSafety(Color.Black) };
            

            threatScore[Color.White] *= ScaleThreats[gamePhase];
            threatScore[Color.Black] *= ScaleThreats[gamePhase];

            materialScore[Color.White] *= ScaleMaterial[gamePhase];
            materialScore[Color.Black] *= ScaleMaterial[gamePhase];

            double scoreW = materialScore[Color.White] + pawnScore[Color.White] + knightScore[Color.White] + 
                            bishopScore[Color.White] + rookScore[Color.White] + queenScore[Color.White] +
                            kingScore[Color.White] + threatScore[Color.White] + spaceScore[Color.White] + 
                            endgameScore[Color.White];

            double scoreB = materialScore[Color.Black] + pawnScore[Color.Black] + knightScore[Color.Black] + 
                            bishopScore[Color.Black] + rookScore[Color.Black] + queenScore[Color.Black] + 
                            kingScore[Color.Black] + threatScore[Color.Black] + spaceScore[Color.Black] + 
                            endgameScore[Color.Black];

            double scoreFinal = scoreW - scoreB;
            double relative = (scoreFinal * (pc == Color.White ? 1 : -1));

            if (Trace)
            {
                Log("┌─────────────┬──────────┬──────────┬──────────┐");
                Log("│     Term    │   White  │   Black  │   Total  │");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│    Material │ " + FormatEvalTerm(materialScore[Color.White]) + " │ " + FormatEvalTerm(materialScore[Color.Black]) + " │ " + FormatEvalTerm(materialScore[Color.White] - materialScore[Color.Black]) + " │ ");
                Log("│       Pawns │ " + FormatEvalTerm(pawnScore[Color.White]) + " │ " + FormatEvalTerm(pawnScore[Color.Black]) + " │ " + FormatEvalTerm(pawnScore[Color.White] - pawnScore[Color.Black]) + " │ ");
                Log("│     Knights │ " + FormatEvalTerm(knightScore[Color.White]) + " │ " + FormatEvalTerm(knightScore[Color.Black]) + " │ " + FormatEvalTerm(knightScore[Color.White] - knightScore[Color.Black]) + " │ ");
                Log("│     Bishops │ " + FormatEvalTerm(bishopScore[Color.White]) + " │ " + FormatEvalTerm(bishopScore[Color.Black]) + " │ " + FormatEvalTerm(bishopScore[Color.White] - bishopScore[Color.Black]) + " │ ");
                Log("│       Rooks │ " + FormatEvalTerm(rookScore[Color.White]) + " │ " + FormatEvalTerm(rookScore[Color.Black]) + " │ " + FormatEvalTerm(rookScore[Color.White] - rookScore[Color.Black]) + " │ ");
                Log("│      Queens │ " + FormatEvalTerm(queenScore[Color.White]) + " │ " + FormatEvalTerm(queenScore[Color.Black]) + " │ " + FormatEvalTerm(queenScore[Color.White] - queenScore[Color.Black]) + " │ ");
                Log("│ King safety │ " + FormatEvalTerm(kingScore[Color.White]) + " │ " + FormatEvalTerm(kingScore[Color.Black]) + " │ " + FormatEvalTerm(kingScore[Color.White] - kingScore[Color.Black]) + " │ ");
                Log("│     Threats │ " + FormatEvalTerm(threatScore[Color.White]) + " │ " + FormatEvalTerm(threatScore[Color.Black]) + " │ " + FormatEvalTerm(threatScore[Color.White] - threatScore[Color.Black]) + " │ ");
                Log("│       Space │ " + FormatEvalTerm(spaceScore[Color.White]) + " │ " + FormatEvalTerm(spaceScore[Color.Black]) + " │ " + FormatEvalTerm(spaceScore[Color.White] - spaceScore[Color.Black]) + " │ ");
                Log("│     Endgame │ " + FormatEvalTerm(endgameScore[Color.White]) + " │ " + FormatEvalTerm(endgameScore[Color.Black]) + " │ " + FormatEvalTerm(endgameScore[Color.White] - endgameScore[Color.Black]) + " │ ");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│       Total │ " + FormatEvalTerm(scoreW) + " │ " + FormatEvalTerm(scoreB) + " │ " + FormatEvalTerm(scoreFinal) + " │ ");
                Log("└─────────────┴──────────┴──────────┴──────────┘");
                Log("Final: " + FormatEvalTerm(scoreFinal) + "\t\trelative\t" + FormatEvalTerm(relative));
                Log(ColorToString(pc) + " is " +
                    (relative < 0 ? "losing" : (relative > 0 ? "winning" : "equal")) + " by " + InCentipawns(scoreFinal) + " cp");
            }

            return (int)relative;
        }


        [MethodImpl(Inline)]
        private double EvalPawns(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            ulong ourPawns = (us & bb.Pieces[Piece.Pawn]);

            ulong doubledPawns = Forward(pc, ourPawns) & ourPawns;
            score += ((int)popcount(doubledPawns) * ScorePawnDoubled);

            ulong doubledDistantPawns = Forward(pc, Forward(pc, ourPawns)) & ourPawns;
            score += ((int)popcount(doubledDistantPawns) * ScorePawnDoubledDistant);

            while (ourPawns != 0)
            {
                int idx = lsb(ourPawns);

                ulong thisPawnAttacks = PawnAttackMasks[pc][idx];
                AttackMask[pc] |= thisPawnAttacks;

                ulong temp = (thisPawnAttacks);
                while (temp != 0)
                {
                    int controlledSquare = lsb(temp);
                    score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                    temp = poplsb(temp);
                }

                while (thisPawnAttacks != 0)
                {
                    int attackIdx = lsb(thisPawnAttacks);

                    if ((us & SquareBB[attackIdx]) != 0)
                    {
                        score += ScorePawnSupport;
                    }
                    else if ((them & SquareBB[attackIdx]) != 0)
                    {
                        threatScore[pc] += GetThreatValue(attackIdx, ValuePawn);
                    }

                    thisPawnAttacks = poplsb(thisPawnAttacks);
                }

                if (IsIsolated(idx))
                {
                    score += ScoreIsolatedPawn;
                }

                score += (PSQT.PawnsByColor[pc][idx] * CoefficientPSQTPawns);
                score += (PSQT.PawnCenter[idx] * CoefficientPSQTCenter);

                materialScore[pc] += ValuePawn;

                ourPawns = poplsb(ourPawns);
            }

            return score * ScalePawns[gamePhase];
        }

        [MethodImpl(Inline)]
        private double EvalKnights(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            ulong ourKnights = us & bb.Pieces[Piece.Knight];

            while (ourKnights != 0)
            {
                int idx = lsb(ourKnights);

                ulong thisAttacks = KnightMasks[idx] & them;
                AttackMask[pc] |= KnightMasks[idx] & ~us;

                ulong temp = (KnightMasks[idx]);
                while (temp != 0)
                {
                    int controlledSquare = lsb(temp);
                    score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                    temp = poplsb(temp);
                }

                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);

                    threatScore[pc] += GetThreatValue(attackIdx, ValueKnight);

                    thisAttacks = poplsb(thisAttacks);
                }

                score += (PSQT.Center[idx] * CoefficientPSQTCenter * CoefficientPSQTKnights);

                materialScore[pc] += ValueKnight;

                ourKnights = poplsb(ourKnights);
            }

            return score * ScaleKnights[gamePhase];
        }

        [MethodImpl(Inline)]
        private double EvalBishops(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            int theirKing = bb.KingIndex(Not(pc));

            ulong ourBishops = us & bb.Pieces[Piece.Bishop];

            if (popcount(ourBishops) == 2)
            {
                score += ScoreBishopPair;
            }

            while (ourBishops != 0)
            {
                int idx = lsb(ourBishops);

                ulong thisMoves = GetBishopMoves((us | them), idx);
                AttackMask[pc] |= (thisMoves & ~us);

                ulong temp = (thisMoves);
                while (temp != 0)
                {
                    int controlledSquare = lsb(temp);
                    score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                    temp = poplsb(temp);
                }

                //  Bonus for our bishop being on the same diagonal as their king.
                if ((BishopRays[idx] & SquareBB[theirKing]) != 0)
                {
                    score += ScoreBishopOnKingDiagonal;
                }

                //  Bonus for bishops that are on the a diagonal next to the king,
                //  meaning they are able to attack the "king ring" squares.

                //score += (popcount(BishopRays[idx] & NeighborsMask[theirKing]) * ScoreBishopNearKingDiagonal);
                if ((BishopRays[idx] & NeighborsMask[theirKing]) != 0)
                {
                    score += ScoreBishopNearKingDiagonal;
                }

                ulong thisAttacks = thisMoves & them;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);

                    threatScore[pc] += GetThreatValue(attackIdx, ValueBishop);

                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore[pc] += ValueBishop;

                ourBishops = poplsb(ourBishops);
            }

            return score * ScaleBishops[gamePhase];
        }


        [MethodImpl(Inline)]
        private double EvalRooks(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            ulong ourRooks = us & bb.Pieces[Piece.Rook];

            while (ourRooks != 0)
            {
                int idx = lsb(ourRooks);

                ulong thisMoves = GetRookMoves((us | them), idx);
                AttackMask[pc] |= (thisMoves & ~us);

                ulong temp = (thisMoves);
                while (temp != 0)
                {
                    int controlledSquare = lsb(temp);
                    score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                    temp = poplsb(temp);
                }

                score += (ScoreSupportingPiece * popcount(thisMoves & us));
                score += ((ScorePerSquare / 2) * popcount(thisMoves & ~(us | them)));

                score += (PSQT.Center[idx] * (CoefficientPSQTCenter / 2));

                if ((pc == Color.White && GetIndexRank(idx) == 6) || (pc == Color.Black && GetIndexRank(idx) == 1))
                {
                    score += ScoreRookOn7th;
                }

                ulong thisAttacks = thisMoves & them;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);

                    threatScore[pc] += GetThreatValue(attackIdx, ValueRook);

                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore[pc] += ValueRook;

                if (IsFileOpen(idx))
                {
                    score += ScoreRookOpenFile;
                }
                else if (IsFileSemiOpen(idx))
                {
                    score += ScoreRookSemiOpenFile;
                }

                ourRooks = poplsb(ourRooks);
            }

            return score * ScaleRooks[gamePhase];
        }

        [MethodImpl(Inline)]
        private double EvalQueens(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            ulong ourQueens = us & bb.Pieces[Piece.Queen];

            while (ourQueens != 0)
            {
                int idx = lsb(ourQueens);

                ulong thisMoves = (GetBishopMoves((us | them), idx) | GetRookMoves((us | them), idx));
                AttackMask[pc] |= (thisMoves & ~us);

                ulong temp = (thisMoves);
                while (temp != 0)
                {
                    int controlledSquare = lsb(temp);
                    score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                    temp = poplsb(temp);
                }

                score += (popcount(thisMoves) * ScoreQueenSquares);

                if ((pc == Color.White && GetIndexRank(idx) == 6) || (pc == Color.Black && GetIndexRank(idx) == 1))
                {
                   score += ScoreRookOn7th;
                }

                ulong thisAttacks = thisMoves & them;
                while (thisAttacks != 0)
                {
                    int attackIdx = lsb(thisAttacks);

                    threatScore[pc] += GetThreatValue(attackIdx, ValueQueen);

                    thisAttacks = poplsb(thisAttacks);
                }

                materialScore[pc] += ValueQueen;

                ourQueens = poplsb(ourQueens);
            }

            return score * ScaleQueens[gamePhase];
        }

        [MethodImpl(Inline)]
        private double EvalKingSafety(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            //  The "king ring" evaluations see how many enemy pieces are attacking the squares nearby our king,
            //  Which is often but not necessarily always a bad thing.
            int ourKing = bb.KingIndex(pc);
            ulong ourKingRing = NeighborsMask[ourKing];
            ulong ourKingRingOut = OutterNeighborsMask[ourKing];

            double theirAttacks = popcount(ourKingRing & AttackMask[Not(pc)]);
            double theirAttacksOut = popcount(ourKingRingOut & AttackMask[Not(pc)]);
            score -= (theirAttacks * ScoreKingRingAttack);
            score -= (theirAttacksOut * ScoreKingOutterRingAttack);

            //  I really don't know why it likes playing Ka1/Kh1 so often,
            //  so giving it a small penalty here to stop it from doing so
            //  unless it is necessary.
            if ((SquareBB[ourKing] & Corners) != 0)
            {
                score += ScoreKingInCorner;
            }

            //  Divide the threat score because it might be risky to actually capture the piece (deflection)
            double riskCoefficient = 1;

            //  Check if White's king is threatening any material
            ulong ourKingKingAttacks = ourKingRing & them;
            while (ourKingKingAttacks != 0)
            {
                int attackIdx = lsb(ourKingKingAttacks);
                int theirPiece = bb.PieceTypes[attackIdx];
                int theirValue = GetPieceValue(theirPiece);

                int defenders = (int)popcount(DefendersOf(bb, attackIdx));
                if (defenders == 0)
                {
                    threatScore[pc] += (theirValue * CoefficientHanging) / riskCoefficient;
                }
                else
                {
                    //  If the piece is actually defended, this is a bad thing,
                    //  so give a bonus to our opponent based on the number of pieces they have defending it.
                    threatScore[Not(pc)] += defenders * ScoreDefendedPieceNearKing * (theirValue / ScoreDefendedPieceNearKingCoeff);
                }
                ourKingKingAttacks = poplsb(ourKingKingAttacks);
            }

            score += (popcount(ourKingRing & us) * ScoreKingWithHomies);
            score += (popcount(ourKingRingOut & us) * (ScoreKingWithHomies / 3));

            return score * ScaleKingSafety[gamePhase];
        }

        [MethodImpl(Inline)]
        private double EvalSpace(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            score += ((int)popcount(AttackMask[pc]) * ScorePerSquare);

            ulong ourBackRank = (pc == Color.White ? Rank1BB : Rank8BB);
            ulong ourUndeveloped = ((ourBackRank & us) & (bb.Pieces[Piece.Bishop] | bb.Pieces[Piece.Knight]));

            score += ((int)popcount(ourUndeveloped) * ScoreUndevelopedPiece);

            if ((pc == Color.White && p.whiteCastled) || (pc == Color.Black && p.blackCastled))
            {
                score += ScoreKingCastled;
            }

            return score * ScaleSpace[gamePhase];
        }

        [MethodImpl(Inline)]
        public double GetThreatValue(int attackIdx, int ourPieceValue)
        {
            double score = 0;

            int theirPiece = bb.PieceTypes[attackIdx];
            int theirValue = GetPieceValue(theirPiece);

            int pc = bb.GetColorAtIndex(attackIdx);

            bool isDefendedByPawn = ((PawnAttackMasks[Not(pc)][attackIdx] & bb.Pieces[Piece.Pawn] & bb.Colors[pc]) != 0);
            if (isDefendedByPawn && ourPieceValue > theirValue)
            {
                return 0;
            }

            int defenders = (int)popcount(DefendersOf(bb, attackIdx));
            if (defenders == 0)
            {
                score += (theirValue * CoefficientHanging);
            }
            else
            {
                double totalValue = theirValue - ourPieceValue;
                if (totalValue > 0)
                {
                    score += (totalValue * CoefficientPositiveTrade);
                }
            }

            return score;
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
            ulong us = bb.Colors[ourColor];
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
            ulong whitePawns = bb.Pieces[Piece.Pawn] & bb.Colors[Color.White];
            ulong blackPawns = bb.Pieces[Piece.Pawn] & bb.Colors[Color.Black];

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
