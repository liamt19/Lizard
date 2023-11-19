using System.Linq.Expressions;

using static LTChess.Logic.Search.EvaluationConstants;

namespace LTChess.Logic.Search
{
    public unsafe class ThreadedEvaluation
    {

        public const short ScoreNone = 32760;
        public const int ScoreInfinite = 31200;
        public const int ScoreMate = 30000;

        public const int ScoreTTWin = ScoreMate - 512;
        public const int ScoreTTLoss = -ScoreTTWin;

        public const int ScoreMateMax = ScoreMate - 256;
        public const int ScoreMatedMax = -ScoreMateMax;

        public const int ScoreAssuredWin = 20000;
        public const int ScoreWin = 10000;

        public const int ScoreDraw = 0;

        public const ulong EndgamePieces = 8;
        public const ulong TBEndgamePieces = 5;

        private Position p;
        private Bitboard bb;

        private int gamePhase;

        private ulong whitePieces;
        private ulong blackPieces;

        private ulong[] AttackMask;
        private ulong[] MobilityArea;

        private double[] materialScore;
        private double[] positionalScore;
        private double[] kingScore;
        private double[] threatScore;
        private double[] mobilityScore;
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
            MobilityArea = new ulong[2];

            materialScore = new double[2];
            positionalScore = new double[2];
            kingScore = new double[2];
            threatScore = new double[2];
            mobilityScore = new double[2];
            endgameScore = new double[2];

            whitePieces = popcount(bb.Colors[Color.White]);
            blackPieces = popcount(bb.Colors[Color.Black]);

            bool IsEndgame = (whitePieces + blackPieces) <= EndgamePieces;

            //  Also check if there are just queens and pawns, and possibly 2 (+2 kings) other pieces.
            if (!IsEndgame && (popcount(bb.Pieces[Piece.Pawn] | bb.Pieces[Piece.Queen]) + 2 + 2 >= (whitePieces + blackPieces)))
            {
                IsEndgame = true;
            }

            bool IsTBEndgame = (whitePieces + blackPieces) <= TBEndgamePieces;

            int gamePhase = ((IsEndgame || IsTBEndgame) ? GamePhaseEndgame : GamePhaseNormal);

            for (int i = 0; i < 2; i++)
            {
                ulong b = (bb.Pieces[Piece.Pawn] & bb.Colors[i]) & (Shift(0 - ShiftUpDir(i), bb.Occupancy) | LowRanks[i]);
                MobilityArea[i] = ~(b | ((bb.Pieces[Piece.King] | bb.Pieces[Piece.Queen]) & bb.Colors[i]));
            }

            pawnScore = new[]   { EvalPawns  (Color.White), EvalPawns(Color.Black) };
            knightScore = new[] { EvalKnights(Color.White), EvalKnights(Color.Black) };
            bishopScore = new[] { EvalBishops(Color.White), EvalBishops(Color.Black) };
            rookScore = new[]   { EvalRooks  (Color.White), EvalRooks(Color.Black) };
            queenScore = new[]  { EvalQueens (Color.White), EvalQueens(Color.Black) };
            kingScore = new[]   { EvalKingSafety(Color.White), EvalKingSafety(Color.Black) };

 
            positionalScore[Color.White] *= ScalePositional[gamePhase];
            positionalScore[Color.Black] *= ScalePositional[gamePhase];

            threatScore[Color.White] *= ScaleThreats[gamePhase];
            threatScore[Color.Black] *= ScaleThreats[gamePhase];

            materialScore[Color.White] *= ScaleMaterial[gamePhase];
            materialScore[Color.Black] *= ScaleMaterial[gamePhase];

            //mobilityScore[Color.White] *= ScaleMobility[gamePhase];
            //mobilityScore[Color.Black] *= ScaleMobility[gamePhase];

            double scoreW = materialScore[Color.White] + positionalScore[Color.White] + pawnScore[Color.White] +
                            knightScore[Color.White] + bishopScore[Color.White] + rookScore[Color.White] +
                            queenScore[Color.White] + kingScore[Color.White] + threatScore[Color.White] +
                            mobilityScore[Color.White] + endgameScore[Color.White];

            double scoreB = materialScore[Color.Black] + positionalScore[Color.Black] + pawnScore[Color.Black] +
                            knightScore[Color.Black] + bishopScore[Color.Black] + rookScore[Color.Black] +
                            queenScore[Color.Black] + kingScore[Color.Black] + threatScore[Color.Black] +
                            mobilityScore[Color.Black] + endgameScore[Color.Black];

            double scoreFinal = scoreW - scoreB;
            double relative = (scoreFinal * (pc == Color.White ? 1 : -1));

            if (Trace)
            {
                Log("┌─────────────┬──────────┬──────────┬──────────┐");
                Log("│     Term    │   White  │   Black  │   Total  │");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│    Material │ " + FormatEvalTerm(materialScore[Color.White]) + " │ " + FormatEvalTerm(materialScore[Color.Black]) + " │ " + FormatEvalTerm(materialScore[Color.White] - materialScore[Color.Black]) + " │ ");
                Log("│  Positional │ " + FormatEvalTerm(positionalScore[Color.White]) + " │ " + FormatEvalTerm(positionalScore[Color.Black]) + " │ " + FormatEvalTerm(positionalScore[Color.White] - positionalScore[Color.Black]) + " │ ");
                Log("│    Mobility │ " + FormatEvalTerm(mobilityScore[Color.White]) + " │ " + FormatEvalTerm(mobilityScore[Color.Black]) + " │ " + FormatEvalTerm(mobilityScore[Color.White] - mobilityScore[Color.Black]) + " │ "); 
                Log("│       Pawns │ " + FormatEvalTerm(pawnScore[Color.White]) + " │ " + FormatEvalTerm(pawnScore[Color.Black]) + " │ " + FormatEvalTerm(pawnScore[Color.White] - pawnScore[Color.Black]) + " │ ");
                Log("│     Knights │ " + FormatEvalTerm(knightScore[Color.White]) + " │ " + FormatEvalTerm(knightScore[Color.Black]) + " │ " + FormatEvalTerm(knightScore[Color.White] - knightScore[Color.Black]) + " │ ");
                Log("│     Bishops │ " + FormatEvalTerm(bishopScore[Color.White]) + " │ " + FormatEvalTerm(bishopScore[Color.Black]) + " │ " + FormatEvalTerm(bishopScore[Color.White] - bishopScore[Color.Black]) + " │ ");
                Log("│       Rooks │ " + FormatEvalTerm(rookScore[Color.White]) + " │ " + FormatEvalTerm(rookScore[Color.Black]) + " │ " + FormatEvalTerm(rookScore[Color.White] - rookScore[Color.Black]) + " │ ");
                Log("│      Queens │ " + FormatEvalTerm(queenScore[Color.White]) + " │ " + FormatEvalTerm(queenScore[Color.Black]) + " │ " + FormatEvalTerm(queenScore[Color.White] - queenScore[Color.Black]) + " │ ");
                Log("│ King safety │ " + FormatEvalTerm(kingScore[Color.White]) + " │ " + FormatEvalTerm(kingScore[Color.Black]) + " │ " + FormatEvalTerm(kingScore[Color.White] - kingScore[Color.Black]) + " │ ");
                Log("│     Threats │ " + FormatEvalTerm(threatScore[Color.White]) + " │ " + FormatEvalTerm(threatScore[Color.Black]) + " │ " + FormatEvalTerm(threatScore[Color.White] - threatScore[Color.Black]) + " │ ");
                Log("│     Endgame │ " + FormatEvalTerm(endgameScore[Color.White]) + " │ " + FormatEvalTerm(endgameScore[Color.Black]) + " │ " + FormatEvalTerm(endgameScore[Color.White] - endgameScore[Color.Black]) + " │ ");
                Log("├─────────────┼──────────┼──────────┼──────────┤");
                Log("│       Total │ " + FormatEvalTerm(scoreW) + " │ " + FormatEvalTerm(scoreB) + " │ " + FormatEvalTerm(scoreFinal) + " │ ");
                Log("└─────────────┴──────────┴──────────┴──────────┘");
                Log("Final: " + FormatEvalTerm(relative));
                Log(ColorToString(pc) + " is " +
                    (relative < 0 ? "losing" : (relative > 0 ? "winning" : "equal")) + " by " + (int)InCentipawns(Math.Abs(scoreFinal) * 100) + " cp");
            }

            return (int)relative;
        }


        [MethodImpl(Inline)]
        private double EvalPawns(int pc)
        {
            double score = 0;

            ulong us = bb.Colors[pc];
            ulong them = bb.Colors[Not(pc)];

            const int pt = Piece.Pawn;
            const int thisPieceValue = ValuePawn;

            ulong ourPawns = us & bb.Pieces[pt];

            ulong doubledPawns = Forward(pc, ourPawns) & ourPawns;
            score += ((int)popcount(doubledPawns) * ScorePawnDoubled);

            ulong doubledDistantPawns = Forward(pc, Forward(pc, ourPawns)) & ourPawns;
            score += ((int)popcount(doubledDistantPawns) * ScorePawnDoubledDistant);

            while (ourPawns != 0)
            {
                int idx = lsb(ourPawns);

                ulong thisPawnAttacks = PawnAttackMasks[pc][idx];
                AttackMask[pc] |= thisPawnAttacks;

                MobilityArea[Not(pc)] &= (~thisPawnAttacks);

                //positionalScore[pc] += GetCenterControlScore(thisPawnAttacks);
                positionalScore[pc] += (PSQT.PawnsByColor[pc][idx] * CoefficientPSQTPawns);
                positionalScore[pc] += (PSQT.PawnCenter[idx] * CoefficientPSQTCenter);

                materialScore[pc] += thisPieceValue;

                while (thisPawnAttacks != 0)
                {
                    int attackIdx = lsb(thisPawnAttacks);

                    if ((us & SquareBB[attackIdx]) != 0)
                    {
                        score += ScorePawnSupport;
                    }
                    else if ((them & SquareBB[attackIdx]) != 0)
                    {
                        threatScore[pc] += GetThreatValue(attackIdx, thisPieceValue);

                        //  If the piece that this pawn is attacking is also a pawn,
                        //  and that pawn is supporting a non-pawn piece, we are undermining that piece.
                        if ((them & bb.Pieces[Piece.Pawn] & SquareBB[attackIdx]) != 0)
                        {
                            if ((them & ~bb.Pieces[Piece.Pawn] & PawnAttackMasks[Not(pc)][attackIdx]) != 0)
                            {
                                score += ScorePawnUndermine;

                                //  Small additional bonus if the pawn that is doing the undermining is supported.
                                if ((us & PawnAttackMasks[Not(pc)][idx] & bb.Pieces[Piece.Pawn]) != 0)
                                {
                                    score += ScorePawnUndermineSupported;
                                }
                            }
                        }
                    }

                    thisPawnAttacks = poplsb(thisPawnAttacks);
                }

                if (IsIsolated(idx))
                {
                    score += ScoreIsolatedPawn;
                }

                if (bb.IsPasser(idx))
                {
                    //  Bonus for passed pawns that are advanced
                    score += ScorePasser * (PassedPawnPromotionDistanceFactor - DistanceFromPromotion(idx, pc));
                }

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

            const int pt = Piece.Knight;
            const int thisPieceValue = ValueKnight;

            ulong ourKnights = us & bb.Pieces[pt];

            while (ourKnights != 0)
            {
                int idx = lsb(ourKnights);

                ulong thisMoves = KnightMasks[idx];
                AttackMask[pc] |= (thisMoves & ~us);

                mobilityScore[pc] += GetPieceMobility(pc, pt, thisMoves & ~us);
                
                threatScore[pc] += GetAttackThreatScore(thisMoves & them, pt);

                //positionalScore[pc] += GetCenterControlScore(KnightMasks[idx]);
                positionalScore[pc] += (PSQT.FishPSQT[pc][pt][idx] * CoefficientPSQTFish) / 2;
                score += (PSQT.FishPSQT[pc][pt][idx] * CoefficientPSQTFish) / 2;

                materialScore[pc] += thisPieceValue;

                if ((SquareBB[idx] & OutpostSquares[pc]) != 0)
                {
                    //  Then this knight is on an outpost square,
                    //  but that square also needs to not be attacked by one of their pawns,
                    //  and must be supported by one of our pawns
                    if ((PawnAttackMasks[pc][idx] & bb.Pieces[Piece.Pawn] & them) == 0 &&
                        (PawnAttackMasks[Not(pc)][idx] & bb.Pieces[Piece.Pawn] & us) != 0)
                    {
                        score += ScoreKnightOutpost[gamePhase];
                    }
                }

                //  Small bonus for knights under our pawns
                if ((SquareBB[idx] & Backward(pc, (us & bb.Pieces[Piece.Pawn])) & (Rank3BB | Rank4BB | Rank5BB | Rank6BB)) != 0)
                {
                    score += ScoreKnightUnderPawn[gamePhase];
                }

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

            const int pt = Piece.Bishop;
            const int thisPieceValue = ValueBishop;

            int theirKing = bb.KingIndex(Not(pc));

            ulong ourBishops = us & bb.Pieces[pt];

            if (popcount(ourBishops) == 2)
            {
                //  Moderate bonus for having 2 bishops.
                //  If both players have 2 bishops, this does nothing anyways.
                score += ScoreBishopPair;
            }

            while (ourBishops != 0)
            {
                int idx = lsb(ourBishops);

                ulong thisMoves = GetBishopMoves((us | them), idx);
                AttackMask[pc] |= (thisMoves & ~us);

                mobilityScore[pc] += GetPieceMobility(pc, pt, thisMoves & ~us);

                threatScore[pc] += GetAttackThreatScore(thisMoves & them, pt);

                //positionalScore[pc] += GetCenterControlScore(thisMoves);
                positionalScore[pc] += (PSQT.FishPSQT[pc][pt][idx] * CoefficientPSQTFish);
                materialScore[pc] += thisPieceValue;

                if ((SquareBB[idx] & OutpostSquares[pc]) != 0)
                {
                    if ((PawnAttackMasks[pc][idx] & bb.Pieces[Piece.Pawn] & them) == 0 &&
                        (PawnAttackMasks[Not(pc)][idx] & bb.Pieces[Piece.Pawn] & us) != 0)
                    {
                        score += ScoreBishopOutpost[gamePhase];
                    }
                }

                //  Bonus for our bishop being on the same diagonal as their king.
                if ((BishopRays[idx] & SquareBB[theirKing]) != 0)
                {
                    score += ScoreBishopOnKingDiagonal[gamePhase];
                }

                //  Bonus for bishops that are on the a diagonal next to the king,
                //  meaning they are able to attack the "king ring" squares.
                score += (int)(popcount(BishopRays[idx] & NeighborsMask[theirKing]) * ScoreBishopNearKingDiagonal[gamePhase]);


                var centerSquaresInRay = popcount(BishopRays[idx] & Center);

                //  Bonus for our bishop being on a long diagonal
                if (centerSquaresInRay == 2)
                {
                    score += ScoreBishopOnCenterDiagonal[gamePhase];
                }
                else if (centerSquaresInRay == 1)
                {
                    score += ScoreBishopNearCenterDiagonal[gamePhase];
                }

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

            const int pt = Piece.Rook;
            const int thisPieceValue = ValueRook;

            ulong ourRooks = us & bb.Pieces[pt];

            while (ourRooks != 0)
            {
                int idx = lsb(ourRooks);

                ulong thisMoves = GetRookMoves((us | them), idx);
                AttackMask[pc] |= (thisMoves & ~us);

                mobilityScore[pc] += GetPieceMobility(pc, pt, thisMoves & ~us);

                threatScore[pc] += GetAttackThreatScore(thisMoves & them, pt);

                //positionalScore[pc] += GetCenterControlScore(thisMoves);
                positionalScore[pc] += (PSQT.FishPSQT[pc][pt][idx] * CoefficientPSQTFish);
                materialScore[pc] += thisPieceValue;

                if ((pc == Color.White && GetIndexRank(idx) == 6) || (pc == Color.Black && GetIndexRank(idx) == 1))
                {
                    score += ScoreRookOn7th;
                }

                if (IsFileSemiOpen(idx, pc))
                {
                    if (IsFileSemiOpen(idx, Not(pc)))
                    {
                        //  No pawns of either color
                        score += ScoreRookOpenFile[gamePhase];
                    }
                    else
                    {
                        //  Only they have pawns on this file
                        score += ScoreRookSemiOpenFile[gamePhase];
                    }
                }
                else
                {
                    //  Then our rook is behind one of our pawns, which can be good but not if the pawn is blockaded

                    //  Penalty if our pawn on this file has a piece directly in front of it
                    if ((bb.Pieces[Piece.Pawn] & GetFileBB(idx) & Backward(pc, us | them)) != 0)
                    {
                        score -= (ScoreRookSemiOpenFile[gamePhase] / 2);
                    }
                }

                //score += (ScoreSupportingPiece * popcount(thisMoves & us));
                //score += ((ScorePerSquare / 2) * popcount(thisMoves & ~(us | them)));

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

            const int pt = Piece.Queen;
            const int thisPieceValue = ValueQueen;

            ulong ourQueens = us & bb.Pieces[pt];

            while (ourQueens != 0)
            {
                int idx = lsb(ourQueens);

                ulong thisMoves = (GetBishopMoves((us | them), idx) | GetRookMoves((us | them), idx));
                AttackMask[pc] |= (thisMoves & ~us);

                mobilityScore[pc] += GetPieceMobility(pc, pt, thisMoves & ~us);

                threatScore[pc] += GetAttackThreatScore(thisMoves & them, pt);

                //positionalScore[pc] += GetCenterControlScore(thisMoves);
                positionalScore[pc] += (PSQT.FishPSQT[pc][pt][idx] * CoefficientPSQTFish);
                materialScore[pc] += thisPieceValue;

                if ((pc == Color.White && GetIndexRank(idx) == 6) || (pc == Color.Black && GetIndexRank(idx) == 1))
                {
                    score += ScoreRookOn7th;
                }

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
            score -= (theirAttacks * ScoreKingRingAttack[gamePhase]);
            score -= (theirAttacksOut * ScoreKingOutterRingAttack[gamePhase]);

            //  I really don't know why it likes playing Ka1/Kh1 so often,
            //  so giving it a small penalty here to stop it from doing so
            //  unless it is necessary.
            if ((SquareBB[ourKing] & Corners) != 0)
            {
                score += ScoreKingInCorner;
            }

            if (MoreThanOne(NeighborsFileMasks[ourKing] & them & (bb.Pieces[Piece.Queen] | bb.Pieces[Piece.Rook] | bb.Pieces[Piece.Bishop]))) {
                if (GetIndexFile(ourKing) > Files.A && IsFileOpen(ourKing - 1))
                {
                    score += ScoreKingNearOpenFile[gamePhase];
                }
                if (IsFileOpen(ourKing))
                {
                    score += ScoreKingNearOpenFile[gamePhase];
                }
                if (GetIndexFile(ourKing) < Files.A && IsFileOpen(ourKing + 1))
                {
                    score += ScoreKingNearOpenFile[gamePhase];
                }
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

        /// <summary>
        /// Returns a bonus for each of the squares near the center of the board that a piece can move to.
        /// </summary>
        /// <param name="thisMoves">The move mask of a piece, which should include squares that friendly pieces are on (and this piece is supporting).</param>
        [MethodImpl(Inline)]
        private double GetCenterControlScore(ulong thisMoves)
        {
            double score = 0;

            while (thisMoves != 0)
            {
                int controlledSquare = lsb(thisMoves);
                score += (PSQT.CenterControl[controlledSquare] * CoefficientPSQTCenterControl);
                thisMoves = poplsb(thisMoves);
            }

            return score;
        }

        /// <summary>
        /// Calculates the threat score for a piece of type <paramref name="pt"/> 
        /// for each of the squares in <paramref name="thisAttacks"/> that it can capture pieces on.
        /// </summary>
        [MethodImpl(Inline)]
        private double GetAttackThreatScore(ulong thisAttacks, int pt)
        {
            double score = 0;

            while (thisAttacks != 0)
            {
                int attackIdx = lsb(thisAttacks);

                score += GetThreatValue(attackIdx, PieceValues[pt]);

                thisAttacks = poplsb(thisAttacks);
            }

            return score;
        }

        /// <summary>
        /// Returns the difference in material value for pieces that the piece on <paramref name="attackIdx"/> can capture.
        /// Bonuses are given for hanging pieces or pieces worth more than ours.
        /// </summary>
        /// <param name="attackIdx">The square that the piece is on</param>
        /// <param name="ourPieceValue">The value of the piece on <paramref name="attackIdx"/></param>
        /// <returns></returns>
        [MethodImpl(Inline)]
        private double GetThreatValue(int attackIdx, int ourPieceValue)
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

        private int GetPieceMobility(int pc, int pt, ulong moves)
        {
            return MobilityBonus[gamePhase][pt - 1][popcount(MobilityArea[pc] & moves)];


            ulong innerSquares = popcount(MobilityArea[pc] & moves & MobilityInner);
            ulong outterSquares = popcount(MobilityArea[pc] & moves & MobilityOutter);

            return (int) ((innerSquares * MobilityScoreInner) + (outterSquares * MobilityScoreOutter));
        }


        [MethodImpl]
        private int NearestPawn(in Bitboard bb, int color)
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
        private bool WillPromote(int idx)
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
        private bool IsIsolated(int idx)
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
        /// Returns true if the file of <paramref name="idx"/> is semi-open, 
        /// which means that there are no pawns of the color <paramref name="pc"/> on that file.
        /// <br></br>
        /// This will return true for a white rook on A1 if there are no white pawns on A2-A7.
        /// </summary>
        [MethodImpl(Inline)]
        private bool IsFileSemiOpen(int idx, int pc)
        {
            ulong ourPawns = bb.Pieces[Piece.Pawn] & bb.Colors[pc];
            ulong fileMask = GetFileBB(idx);
            return ((fileMask & ourPawns) == 0);
        }

        /// <summary>
        /// Returns true if the file of <paramref name="idx"/> is an open file, meaning there aren't any pawns on it
        /// </summary>
        [MethodImpl(Inline)]
        private bool IsFileOpen(int idx)
        {
            return ((GetFileBB(idx) & bb.Pieces[Piece.Pawn]) == 0);
        }

        [MethodImpl(Inline)]
        public static int MakeMateScore(int ply)
        {
            return -ScoreMate + ply;
        }

        [MethodImpl(Inline)]
        public static bool IsScoreMate(int score)
        {
            return (Math.Abs(Math.Abs(score) - ScoreMate) < MaxDepth);
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
