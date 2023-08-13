namespace LTChess.Logic.Variants
{
    /// <summary>
    /// TODO: Chess.com's Automate
    /// </summary>
    public unsafe class Automate
    {
        public Bitboard bb;
        public int whitePoints;
        public int blackPoints;
        public int currColor;

        private int blackPawns;
        private int whitePawns;

        private const int MinPawns = 6;
        private const int MaxPawns = 10;

        private const int MaxPoints = 35;
        private const int MinPoints = PointsKnight - 1;

        private const int PointsQueen = 7;
        private const int PointsRook = 4;
        private const int PointsBishop = 3;
        private const int PointsKnight = 3;
        private const int PointsPawn = 1;

        private const ulong ValidWhitePawnRanks = 0xFFFF00;
        private const ulong ValidWhitePieceRanks = 0xFFFF;

        private const ulong ValidBlackPawnRanks = 0xFFFF0000000000;
        private const ulong ValidBlackPieceRanks = 0xFFFF000000000000;

        public Automate()
        {
            NewGame();
        }

        public void NewGame()
        {
            bb = new Bitboard();

            currColor = Color.White;
            whitePoints = MaxPoints;
            blackPoints = MaxPoints;


            Setup();

            Log("Finished placing pieces! Position: ");
            Log(PrintBoard(bb));
        }

        public void Setup()
        {
            Log("Start placing pawns, enter in the form 'PieceSquare' or just 'Square', each player needs to place 6 pawns: ");
            bool allPawnsPlaced = false;
            while (!allPawnsPlaced)
            {
                if (currColor == Color.White)
                {
                    whitePawns++;
                    whitePoints -= PointsPawn;
                }
                else
                {
                    blackPawns++;
                    blackPoints -= PointsPawn;
                }

                int idx = A1;
                bool gotInput = false;
                while (!gotInput)
                {
                    gotInput = ReadInput(out idx, out _);
                    if (gotInput && !PlacementValid(idx, Piece.Pawn, currColor))
                    {
                        string sqs = (currColor == Color.White) ? (IndexToString(lsb(ValidWhitePawnRanks)) + " and " + IndexToString(msb(ValidWhitePawnRanks))) : (IndexToString(lsb(ValidBlackPawnRanks)) + " and " + IndexToString(msb(ValidBlackPawnRanks)));
                        Log("Square " + IndexToString(idx) + " isn't valid for " + PieceToString(Piece.Pawn) + ", must be between " + sqs);
                        gotInput = false;
                    }
                }

                Place(idx, Piece.Pawn, currColor);
                allPawnsPlaced = (whitePawns >= MinPawns && blackPawns >= MinPawns);
                currColor = Not(currColor);
            }

            Log("All pawns placed! Position: ");
            Log(PrintBoard(bb));

            Log("Start placing pieces, enter in the form 'PieceSquare', i.e. 'Ne4' to put a knight on E4: ");

            bool whitePiecesPlaced = false;
            bool blackPiecesPlaced = false;
            bool whiteKingPlaced = false;
            bool blackKingPlaced = false;

            while (!whiteKingPlaced || !blackKingPlaced)
            {
                ref int currPoints = ref blackPoints;
                if (currColor == Color.White)
                {
                    currPoints = ref whitePoints;
                }

                if ((whitePiecesPlaced && currColor == Color.White))
                {
                    if ((bb.Pieces[Piece.King] & bb.Colors[currColor]) == 0)
                    {
                        //  Place their king now
                        int kingIdx = A1;
                        while (!whiteKingPlaced)
                        {
                            whiteKingPlaced = ReadInput(out kingIdx, out _);

                            ulong attackers = bb.AttackersTo(kingIdx, currColor);
                            if (attackers != 0)
                            {
                                Log("Putting " + ColorToString(currColor) + "'s king on " + IndexToString(kingIdx) + " would put it in check from " + IndexToString(lsb(attackers)));
                                whiteKingPlaced = false;
                                continue;
                            }

                            Place(kingIdx, Piece.King, currColor);
                        }
                    }
                    else
                    {
                        Log(ColorToString(currColor) + " has finished placing their pieces, " + ColorToString(Not(currColor)) + " is placing again.");
                        currColor = Not(currColor);
                        currPoints = ref blackPoints;
                    }
                }

                if (blackPiecesPlaced && currColor == Color.Black)
                {
                    if ((bb.Pieces[Piece.King] & bb.Colors[currColor]) == 0)
                    {
                        //  Place their king now
                        int kingIdx = A1;
                        while (!blackKingPlaced)
                        {
                            blackKingPlaced = ReadInput(out kingIdx, out _);

                            ulong attackers = bb.AttackersTo(kingIdx, currColor);
                            if (attackers != 0)
                            {
                                Log("Putting " + ColorToString(currColor) + "'s king on " + IndexToString(kingIdx) + " would put it in check from " + IndexToString(lsb(attackers)));
                                blackKingPlaced = false;
                                continue;
                            }

                            Place(kingIdx, Piece.King, currColor);
                        }
                    }
                    else
                    {
                        Log(ColorToString(currColor) + " has finished placing their pieces, " + ColorToString(Not(currColor)) + " is placing again.");
                        currColor = Not(currColor);
                        currPoints = ref whitePoints;
                    }
                }

                int idx = A1;
                int pt = Piece.None;
                bool gotInput = false;
                while (!gotInput)
                {
                    gotInput = ReadInput(out idx, out pt);
                    int points = GetPiecePoints(pt);
                    if (points < currPoints)
                    {
                        gotInput = false;
                        Log(ColorToString(currColor) + " only has " + currPoints + " points left, not enough for a " + PieceToString(pt));
                    }
                    else
                    {
                        Place(idx, pt, currColor);
                        currPoints -= points;
                        Log(ColorToString(currColor) + " now has " + currPoints + " points left");
                    }
                }

                //  You are done when you have 0 points, or when you have 1/2 but already have 10 pawns, and those 1/2 points are wasted.
                whitePiecesPlaced = (whitePoints == 0 || (whitePoints <= MinPoints && (whitePawns == MaxPawns)));
                blackPiecesPlaced = (blackPoints == 0 || (blackPoints <= MinPoints && (blackPawns == MaxPawns)));

                currColor = Not(currColor);
            }
        }

        public bool ReadInput(out int idx, out int pt)
        {
            pt = Piece.None;
            idx = A1;

            string input = Console.ReadLine();
            char pieceChar = char.ToLower(input[0]);
            int colorPointsLeft = (currColor == Color.White) ? whitePoints : blackPoints;
            if (pieceChar == 'k' && colorPointsLeft >= 3)
            {
                pieceChar = 'n';
            }
            if (pieceChar >= 'a' && pieceChar <= 'h')
            {
                input = 'p' + input;
                pieceChar = 'p';
            }

            pt = FENToPiece(pieceChar);
            if (pt < Piece.Pawn || pt > Piece.King)
            {
                return false;
            }

            string pos = input.Substring(1);
            if (pos.Length != 2)
            {
                return false;
            }
            idx = StringToIndex(pos);

            if (((bb.Colors[Color.White] | bb.Colors[Color.Black]) & SquareBB[idx]) != 0)
            {
                Log("Square " + pos + " is already occupied!");
                return false;
            }

            return true;
        }

        public void Place(int idx, int pt, int pc)
        {
            bb.Colors[pc] |= SquareBB[idx];
            bb.Pieces[pt] |= SquareBB[idx];

            bb.PieceTypes[idx] = pt;
            Log("Adding a " + ColorToString(pc) + " " + PieceToString(pt) + " on " + IndexToString(idx));
        }

        public bool PlacementValid(int idx, int pt, int pc)
        {
            if (pc == Color.White)
            {
                if (pt == Piece.Pawn)
                {
                    return (currColor == Color.White && (SquareBB[idx] & ValidWhitePawnRanks) == 0);
                }
                else
                {
                    return (currColor == Color.White && (SquareBB[idx] & ValidWhitePieceRanks) == 0);
                }
            }
            else
            {
                if (pt == Piece.Pawn)
                {
                    return (currColor == Color.Black && (SquareBB[idx] & ValidBlackPawnRanks) == 0);
                }
                else
                {
                    return (currColor == Color.Black && (SquareBB[idx] & ValidBlackPieceRanks) == 0);
                }
            }

        }

        private int GetPiecePoints(int pt)
        {
            switch (pt)
            {
                case Piece.Pawn:
                    return PointsPawn;
                case Piece.Knight:
                    return PointsKnight;
                case Piece.Bishop:
                    return PointsBishop;
                case Piece.Rook:
                    return PointsRook;
                case Piece.Queen:
                    return PointsQueen;
                default:
                    return -1;
            }
        }


    }
}
