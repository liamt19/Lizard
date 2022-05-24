using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;

namespace LTChess.Transposition
{
    public static unsafe class Zobrist
    {
        private static ulong[][][] ColorPieceSquareHashes;
        private static ulong[] CastlingRightsHashes;
        private static ulong[] EnPessantFileHashes;
        private static ulong BlackHash;
        private static Random rand;

        //  const int randomSeed :)
        private const int randomSeed = 0xBEEF;

        private static bool Initialized = false;

        static Zobrist()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            ColorPieceSquareHashes = new ulong[2][][];
            CastlingRightsHashes = new ulong[4];
            EnPessantFileHashes = new ulong[8];
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
                EnPessantFileHashes[i] = rand.NextUlong();
            }

            BlackHash = rand.NextUlong();
        }

        [MethodImpl(Optimize)]
        public static ulong GetHash(Position position)
        {
            Bitboard bb = position.bb;

            ulong hash = 0;

            ulong white = bb.Colors[Color.White];
            ulong black = bb.Colors[Color.Black];

            while (white != 0)
            {
                int idx = lsb(white);

                int pt = bb.PieceTypes[idx];
                hash ^= ColorPieceSquareHashes[Color.White][pt][idx];

                white = poplsb(white);
            }

            while (black != 0)
            {
                int idx = lsb(black);

                int pt = bb.PieceTypes[idx];
                hash ^= ColorPieceSquareHashes[Color.Black][pt][idx];

                black = poplsb(black);
            }

            if ((position.Castling & CastlingStatus.WK) != 0)
            {
                hash ^= CastlingRightsHashes[0];
            }
            if ((position.Castling & CastlingStatus.WQ) != 0)
            {
                hash ^= CastlingRightsHashes[1];
            }
            if ((position.Castling & CastlingStatus.BK) != 0)
            {
                hash ^= CastlingRightsHashes[2];
            }
            if ((position.Castling & CastlingStatus.BQ) != 0)
            {
                hash ^= CastlingRightsHashes[3];
            }

            if (position.EnPassantTarget != 0)
            {
                hash ^= EnPessantFileHashes[GetIndexFile(position.EnPassantTarget)];
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
        public static ulong ZobristMove(this ulong hash, int from, int to, int color, int pt)
        {
            return hash ^ (ColorPieceSquareHashes[color][pt][from] ^ ColorPieceSquareHashes[color][pt][to]);
        }

        /// <summary>
        /// Adds or removes the piece of type <paramref name="pt"/> and color <paramref name="color"/> at index <paramref name="idx"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong ZobristToggleSquare(this ulong hash, int color, int pt, int idx)
        {
            return hash ^ ColorPieceSquareHashes[color][pt][idx];
        }

        /// <summary>
        /// Updates the castling status of the hash, and doesn't change anything if the castling status hasn't shanged
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong ZobristCastle(this ulong hash, CastlingStatus prev, CastlingStatus curr)
        {
            //  Nothing is changing
            if ((prev & curr) == 0)
            {
                return hash;
            }
            else
            {
                int idx = lsb((ulong)curr);
                return hash ^ CastlingRightsHashes[idx];
            }
            
        }

        /// <summary>
        /// Sets the En Passant status of the hash, which is set to the <paramref name="file"/> of the pawn that moved two squares previously
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong ZobristEnPassant(this ulong hash, int file)
        {
            return hash ^ EnPessantFileHashes[file];
        }

        /// <summary>
        /// Called each time White makes a move, which updates the hash to show that it's black to move now
        /// </summary>
        [MethodImpl(Inline)]
        public static ulong ZobristChangeToMove(this ulong hash)
        {
            return hash ^ BlackHash;
        }
    }
}
