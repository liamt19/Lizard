namespace LTChess.Logic.Transposition
{
    public static unsafe class Zobrist
    {
        private static ulong[][][] ColorPieceSquareHashes;
        private static ulong[] CastlingRightsHashes;
        private static ulong[] EnPassantFileHashes;
        private static ulong BlackHash;
        private static Random rand;

        private const int DefaultSeed = 0xBEEF;

        private static bool Initialized = false;

        static Zobrist()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize(int randomSeed = DefaultSeed)
        {
            ColorPieceSquareHashes = new ulong[2][][];
            CastlingRightsHashes = new ulong[4];
            EnPassantFileHashes = new ulong[8];
            rand = new Random(randomSeed);

            ColorPieceSquareHashes[Color.White] = new ulong[6][];
            ColorPieceSquareHashes[Color.Black] = new ulong[6][];

            for (int pt = Piece.Pawn; pt <= Piece.King; pt++)
            {
                ColorPieceSquareHashes[Color.White][pt] = new ulong[64];
                ColorPieceSquareHashes[Color.Black][pt] = new ulong[64];

                for (int i = 0; i < 64; i++)
                {
                    ColorPieceSquareHashes[Color.White][pt][i] = rand.NextUlong();
                    ColorPieceSquareHashes[Color.Black][pt][i] = rand.NextUlong();
                }
            }

            for (int i = 0; i < 4; i++)
            {
                CastlingRightsHashes[i] = rand.NextUlong();
            }

            for (int i = 0; i < 8; i++)
            {
                EnPassantFileHashes[i] = rand.NextUlong();
            }

            BlackHash = rand.NextUlong();
        }

        [MethodImpl(Inline)]
        public static ulong GetHash(Position position)
        {
            ref Bitboard bb = ref position.bb;

            ulong hash = 0;

            ulong white = bb.Colors[Color.White];
            ulong black = bb.Colors[Color.Black];

            while (white != 0)
            {
                int idx = poplsb(&white);
                hash ^= ColorPieceSquareHashes[Color.White][bb.PieceTypes[idx]][idx];
            }

            while (black != 0)
            {
                int idx = poplsb(&black);
                hash ^= ColorPieceSquareHashes[Color.Black][bb.PieceTypes[idx]][idx];
            }

            if ((position.State->CastleStatus & CastlingStatus.WK) != 0)
            {
                hash ^= CastlingRightsHashes[0];
            }
            if ((position.State->CastleStatus & CastlingStatus.WQ) != 0)
            {
                hash ^= CastlingRightsHashes[1];
            }
            if ((position.State->CastleStatus & CastlingStatus.BK) != 0)
            {
                hash ^= CastlingRightsHashes[2];
            }
            if ((position.State->CastleStatus & CastlingStatus.BQ) != 0)
            {
                hash ^= CastlingRightsHashes[3];
            }

            if (position.State->EPSquare != EPNone)
            {
                hash ^= EnPassantFileHashes[GetIndexFile(position.State->EPSquare)];
            }

            if (position.ToMove == Color.Black)
            {
                hash ^= BlackHash;
            }

            return hash;
        }

        /// <summary>
        /// Updates the hash by moving the piece of type <paramref name="pt"/> and color <paramref name="color"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// If the move is a capture, ZobristToggleSquare needs to be done as well.
        /// </summary>
        [MethodImpl(Inline)]
        public static void ZobristMove(this ref ulong hash, int from, int to, int color, int pt)
        {
            if (EnableAssertions)
            {
                Assert(from is >= A1 and <= H8, "ZobristMove(from = " + from + ", to = " + to + ", color = " + color + ", type = " + pt + ") wasn't given a valid From square! (should be 0 <= idx <= 63)");
                Assert(to is >= A1 and <= H8, "ZobristMove(from = " + from + ", to = " + to + ", color = " + color + ", type = " + pt + ") wasn't given a valid To square! (should be 0 <= idx <= 63)");
                Assert(color is White or Black, "ZobristMove(from = " + from + ", to = " + to + ", color = " + color + ", type = " + pt + ") wasn't given a valid piece color! (should be 0 or 1)");
                Assert(pt is >= Pawn and <= King, "ZobristMove(from = " + from + ", to = " + to + ", color = " + color + ", type = " + pt + ") wasn't given a valid piece type! (should be 0 <= pt <= 5)");
            }

            hash ^= ColorPieceSquareHashes[color][pt][from] ^ ColorPieceSquareHashes[color][pt][to];
        }

        /// <summary>
        /// Adds or removes the piece of type <paramref name="pt"/> and color <paramref name="color"/> at index <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static void ZobristToggleSquare(this ref ulong hash, int color, int pt, int idx)
        {
            if (EnableAssertions)
            {
                Assert(color is White or Black, "ZobristToggleSquare(color = " + color + ", type = " + pt + ", square = " + idx + ") wasn't given a valid piece color! (should be 0 or 1)");
                Assert(pt is >= Pawn and <= King, "ZobristToggleSquare(color = " + color + ", type = " + pt + ", square = " + idx + ") wasn't given a valid piece type! (should be 0 <= pt <= 5)");
                Assert(idx is >= A1 and <= H8, "ZobristToggleSquare(color = " + color + ", type = " + pt + ", square = " + idx + ") wasn't given a valid square! (should be 0 <= idx <= 63)");
            }

            hash ^= ColorPieceSquareHashes[color][pt][idx];
        }

        /// <summary>
        /// Updates the castling status of the hash, and doesn't change anything if the castling status hasn't changed
        /// </summary>
        [MethodImpl(Inline)]
        public static void ZobristCastle(this ref ulong hash, CastlingStatus prev, CastlingStatus toRemove)
        {
            ulong change = (ulong)(prev & toRemove);
            while (change != 0)
            {
                hash ^= CastlingRightsHashes[poplsb(&change)];
            }
        }

        /// <summary>
        /// Sets the En Passant status of the hash, which is set to the <paramref name="file"/> of the pawn that moved two squares previously
        /// </summary>
        [MethodImpl(Inline)]
        public static void ZobristEnPassant(this ref ulong hash, int file)
        {
            hash ^= EnPassantFileHashes[file];
        }

        /// <summary>
        /// Called each time White makes a move, which updates the hash to show that it's black to move now
        /// </summary>
        [MethodImpl(Inline)]
        public static void ZobristChangeToMove(this ref ulong hash)
        {
            hash ^= BlackHash;
        }
    }
}
