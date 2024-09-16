using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lizard.Logic.Transposition
{
    public static unsafe class Zobrist
    {
        private const int DefaultSeed = 0xBEEF;

        private static readonly ulong[] ColorPieceSquareHashes = new ulong[ColorNB * 6 * 64];
        private static readonly ulong[] CastlingRightsHashes = new ulong[ColorNB * 2];
        private static readonly ulong[] EnPassantFileHashes = new ulong[8];
        private static ulong BlackHash;
        private static readonly Random rand = new Random(DefaultSeed);

        public static ulong HashForPiece(int pc, int pt, int sq) => ColorPieceSquareHashes[ColorPieceSquareHashesIndex(pc, pt, sq)];
        public static ulong ColorHash => BlackHash;

        [ModuleInitializer]
        public static void Initialize()
        {
            for (int pt = Piece.Pawn; pt <= Piece.King; pt++)
            {
                for (int i = 0; i < 64; i++)
                {
                    ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.White, pt, i)] = rand.NextUlong();
                    ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.Black, pt, i)] = rand.NextUlong();
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

        public static ulong GetHash(Position position, ulong* pawnHash, ulong* nonPawnHash)
        {
            ref Bitboard bb = ref position.bb;

            ulong hash = 0;

            ulong white = bb.Colors[Color.White];
            ulong black = bb.Colors[Color.Black];

            while (white != 0)
            {
                int idx = poplsb(&white);
                int pt = bb.GetPieceAtIndex(idx);
                hash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.White, pt, idx)];

                if (pt == Pawn)
                {
                    *pawnHash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.White, pt, idx)];
                }
                else
                {
                    *nonPawnHash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.White, pt, idx)];
                }
            }

            while (black != 0)
            {
                int idx = poplsb(&black);
                int pt = bb.GetPieceAtIndex(idx);
                hash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.Black, pt, idx)];

                if (pt == Pawn)
                {
                    *pawnHash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.Black, pt, idx)];
                }
                else
                {
                    *nonPawnHash ^= ColorPieceSquareHashes[ColorPieceSquareHashesIndex(Color.Black, pt, idx)];
                }
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
        public static void ZobristMove(this ref ulong hash, int from, int to, int color, int pt)
        {
            Assert(from is >= A1 and <= H8, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid From square! (should be 0 <= idx <= 63)");
            Assert(to is >= A1 and <= H8, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid To square! (should be 0 <= idx <= 63)");
            Assert(color is White or Black, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid piece color! (should be 0 or 1)");
            Assert(pt is >= Pawn and <= King, $"ZobristMove({from}, {to}, {color}, {pt}) wasn't given a valid piece type! (should be 0 <= pt <= 5)");

            var fromIndex = ColorPieceSquareHashesIndex(color, pt, from);
            var toIndex = ColorPieceSquareHashesIndex(color, pt, to);
            ref var start = ref MemoryMarshal.GetArrayDataReference(ColorPieceSquareHashes);

            hash ^= Unsafe.Add(ref start, fromIndex) ^ Unsafe.Add(ref start, toIndex);
        }

        /// <summary>
        /// Adds or removes the piece of type <paramref name="pt"/> and color <paramref name="color"/> at index <paramref name="idx"/>
        /// </summary>
        public static void ZobristToggleSquare(this ref ulong hash, int color, int pt, int idx)
        {
            Assert(color is White or Black, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid piece color! (should be 0 or 1)");
            Assert(pt is >= Pawn and <= King, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid piece type! (should be 0 <= pt <= 5)");
            Assert(idx is >= A1 and <= H8, $"ZobristToggleSquare({color}, {pt}, {idx}) wasn't given a valid square! (should be 0 <= idx <= 63)");

            var index = ColorPieceSquareHashesIndex(color, pt, idx);

            hash ^= Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ColorPieceSquareHashes), index);
        }

        /// <summary>
        /// Updates the castling status of the hash, and doesn't change anything if the castling status hasn't changed
        /// </summary>
        public static void ZobristCastle(this ref ulong hash, CastlingStatus prev, CastlingStatus toRemove)
        {
            ulong change = (ulong)(prev & toRemove);
            while (change != 0)
            {
                hash ^= Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(CastlingRightsHashes), poplsb(&change));
            }
        }

        /// <summary>
        /// Sets the En Passant status of the hash, which is set to the <paramref name="file"/> of the pawn that moved two squares previously
        /// </summary>
        public static void ZobristEnPassant(this ref ulong hash, int file)
        {
            hash ^= Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(EnPassantFileHashes), file);
        }

        /// <summary>
        /// Called each time White makes a move, which updates the hash to show that it's black to move now
        /// </summary>
        public static void ZobristChangeToMove(this ref ulong hash)
        {
            hash ^= BlackHash;
        }

        /// <summary>
        /// <see cref="ColorNB"/> x 6 x 64
        /// </summary>
        private static int ColorPieceSquareHashesIndex(int color, int piece, int square)
        {
            const int colorOffset = 6 * 64;
            const int pieceOffset = 64;

            return (color * colorOffset)
                + (piece * pieceOffset)
                + square;
        }
    }
}
