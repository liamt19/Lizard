using System.Collections.Generic;

namespace Lizard.Logic.Transposition
{
    public static unsafe class Cuckoo
    {
        private static readonly Move* Moves;
        private static readonly ulong* Keys;

        private const int TableSize = 8192;

        private static int Hash1(ulong key) => (int)(key & 0x1FFF);
        private static int Hash2(ulong key) => (int)((key >> 16) & 0x1FFF);

        static Cuckoo()
        {
            Moves = AlignedAllocZeroed<Move>(TableSize);
            Keys = AlignedAllocZeroed<ulong>(TableSize);
            new Span<Move>(Moves, TableSize).Fill(Move.Null);

            for (int pc = White; pc <= Black; pc++)
            {
                for (int pt = Knight; pt <= King; pt++)
                {
                    for (int s1 = A1; s1 <= H8; s1++)
                    {
                        for (int s2 = s1 + 1; s2 <= H8; s2++)
                        {
                            if ((AttackMask(s1, pc, pt) & SquareBB[s2]) != 0)
                            {
                                Move m = new Move(s1, s2);
                                ulong key = Zobrist.HashForPiece(pc, pt, s1) ^ Zobrist.HashForPiece(pc, pt, s2) ^ Zobrist.ColorHash;

                                int iter = Hash1(key);
                                while (true)
                                {
                                    (Keys[iter], key) = (key, Keys[iter]);
                                    (Moves[iter], m) = (m, Moves[iter]);

                                    if (m == Move.Null)
                                        break;

                                    iter = iter == Hash1(key) ? Hash2(key) : Hash1(key);
                                }
                            }
                        }
                    }
                }
            }

            ulong AttackMask(int idx, int pc, int pt)
            {
                return pt switch
                {
                    Knight => KnightMasks[idx],
                    Bishop => GetBishopMoves(0, idx),
                    Rook   => GetRookMoves(0, idx),
                    Queen  => GetBishopMoves(0, idx) | GetRookMoves(0, idx),
                    _      => NeighborsMask[idx],
                };
            }
        }

        public static bool HasCycle(Position pos, int ply)
        {
            ref Bitboard bb = ref pos.bb;
            var occ = bb.Occupancy;
            StateInfo* st = pos.State;

            int dist = Math.Min(st->HalfmoveClock, st->PliesFromNull);
            if (dist < 3)
                return false;

            ulong HashFromStack(int i) => pos.Hashes[^i];

            int slot;
            var other = st->Hash ^ HashFromStack(1) ^ Zobrist.ColorHash;
            for (int i = 3; i <= dist; i += 2)
            {
                var currKey = HashFromStack(i);
                other ^= currKey ^ HashFromStack(i - 1) ^ Zobrist.ColorHash;

                if (other != 0)
                    continue;

                var diff = st->Hash ^ currKey;

                if (diff != Keys[(slot = Hash1(diff))] &&
                    diff != Keys[(slot = Hash2(diff))])
                    continue;

                Move m = Moves[slot];
                int moveFrom = m.From;
                int moveTo = m.To;

                if ((occ & BetweenBB[moveFrom][moveTo]) != 0)
                    continue;

                if (ply >= i)
                    return true;

                for (int j = i + 4; j <= dist; j += 2)
                {
                    if (HashFromStack(j) == HashFromStack(i))
                        return true;
                }
            }

            return false;
        }
    }
}
