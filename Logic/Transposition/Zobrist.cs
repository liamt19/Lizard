using System.Runtime.CompilerServices;

namespace Lizard.Logic.Transposition
{
    public static unsafe class Zobrist
    {
        private const int DefaultSeed = 0xBEEF;

        private static readonly ulong* ColorPieceSquareHashes;
        private static readonly ulong* CastlingRightsHashes;
        private static readonly ulong* EnPassantFileHashes;
        private static readonly ulong BlackHash;

        private static int HashIndex(int pc, int pt, int sq) => (pc * PieceNB * SquareNB) + (pt * SquareNB) + sq;

        static Zobrist()
        {
            Random rand = new Random(DefaultSeed);

            ColorPieceSquareHashes = (ulong*)AlignedAllocZeroed(sizeof(ulong) * ColorNB * PieceNB * SquareNB, AllocAlignment);
            CastlingRightsHashes   = (ulong*)AlignedAllocZeroed(sizeof(ulong) * ColorNB * 2, AllocAlignment);
            EnPassantFileHashes    = (ulong*)AlignedAllocZeroed(sizeof(ulong) * 8, AllocAlignment);

            for (int pt = Piece.Pawn; pt <= Piece.King; pt++)
            {
                for (int i = 0; i < 64; i++)
                {
                    ColorPieceSquareHashes[HashIndex(White, pt, i)] = rand.NextUlong();
                    ColorPieceSquareHashes[HashIndex(Black, pt, i)] = rand.NextUlong();
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

        public static ulong GetHash(Position position)
        {
            ref Bitboard bb = ref position.bb;

            ulong hash = 0;

            ulong white = bb.Colors[Color.White];
            ulong black = bb.Colors[Color.Black];

            while (white != 0)
            {
                int idx = poplsb(&white);
                hash ^= ColorPieceSquareHashes[HashIndex(White, bb.GetPieceAtIndex(idx), idx)];
            }

            while (black != 0)
            {
                int idx = poplsb(&black);
                hash ^= ColorPieceSquareHashes[HashIndex(Black, bb.GetPieceAtIndex(idx), idx)];
            }

            hash ^= (position.State->CastleStatus.HasFlag(CastlingStatus.WK)) ? CastlingRightsHashes[0] : 0;
            hash ^= (position.State->CastleStatus.HasFlag(CastlingStatus.WQ)) ? CastlingRightsHashes[1] : 0;
            hash ^= (position.State->CastleStatus.HasFlag(CastlingStatus.BK)) ? CastlingRightsHashes[2] : 0;
            hash ^= (position.State->CastleStatus.HasFlag(CastlingStatus.BQ)) ? CastlingRightsHashes[3] : 0;

            hash ^= (position.State->EPSquare != EPNone) ? EnPassantFileHashes[GetIndexFile(position.State->EPSquare)] : 0;
            hash ^= (position.ToMove == Color.Black) ? BlackHash : 0;

            return hash;
        }

        /// <summary>
        /// Updates the hash by moving the piece of type <paramref name="pt"/> and color <paramref name="color"/> from <paramref name="from"/> to <paramref name="to"/>.
        /// If the move is a capture, ZobristToggleSquare needs to be done as well.
        /// </summary>
        public static void ZobristMove(this ref ulong hash, int from, int to, int color, int pt)
        {
            Assert(from is >= A1 and <= H8, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid From square! (should be 0 <= idx <= 63)");
            Assert(to is >= A1 and <= H8, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid To square! (should be 0 <= idx <= 63)");
            Assert(color is White or Black, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid piece color! (should be 0 or 1)");
            Assert(pt is >= Pawn and <= King, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid piece type! (should be 0 <= pt <= 5)");

            hash ^= ColorPieceSquareHashes[HashIndex(color, pt, from)] ^ ColorPieceSquareHashes[HashIndex(color, pt, to)];
        }

        /// <summary>
        /// Adds or removes the piece of type <paramref name="pt"/> and color <paramref name="color"/> at index <paramref name="idx"/>
        /// </summary>
        public static void ZobristToggleSquare(this ref ulong hash, int color, int pt, int idx)
        {
            Assert(color is White or Black, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid piece color! (should be 0 or 1)");
            Assert(pt is >= Pawn and <= King, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid piece type! (should be 0 <= pt <= 5)");
            Assert(idx is >= A1 and <= H8, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid square! (should be 0 <= idx <= 63)");

            hash ^= ColorPieceSquareHashes[HashIndex(color, pt, idx)];
        }

        /// <summary>
        /// Updates the castling status of the hash, and doesn't change anything if the castling status hasn't changed
        /// </summary>
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
        public static void ZobristEnPassant(this ref ulong hash, int file)
        {
            hash ^= EnPassantFileHashes[file];
        }

        /// <summary>
        /// Called each time White makes a move, which updates the hash to show that it's black to move now
        /// </summary>
        public static void ZobristChangeToMove(this ref ulong hash)
        {
            hash ^= BlackHash;
        }
    }
}
