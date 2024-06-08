using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lizard.Logic.Transposition
{
    public static unsafe class Cuckoo
    {
        private static Move* Moves;
        private static ulong* Keys;

        private const int TableSize = 8192;

        private static int Hash1(ulong key) => (int)(key & 0x1FFF);
        private static int Hash2(ulong key) => (int)((key >> 16) & 0x1FFF);

        static Cuckoo()
        {
            Moves = (Move*)AlignedAllocZeroed((nuint)sizeof(Move) * TableSize, AllocAlignment);
            Keys = (ulong*)AlignedAllocZeroed((nuint)sizeof(ulong) * TableSize, AllocAlignment);
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
                    Rook => GetRookMoves(0, idx),
                    Queen => GetBishopMoves(0, idx) | GetRookMoves(0, idx),
                    _ => NeighborsMask[idx],
                };
            }
        }

        public static bool HasCycle(Position pos, int ply)
        {
            StateInfo* st = pos.State;
            int dist = Math.Min(st->HalfmoveClock, st->PliesFromNull);

            if (dist < 3)
                return false;

            ref Bitboard bb = ref pos.bb;
            ulong ogHash = st->Hash;

            ulong HashFromStack(int i) => pos.StartingState[pos.GamePly - i].Hash;

            //ulong other = ~(st[0].Hash ^ st[-1].Hash);
            ulong other = ~(HashFromStack(0) ^ HashFromStack(1));
            for (int d = 3; d <= dist; d += 2)
            {
                //other ^= ~(st[-d-1].Hash ^ st[-d].Hash);
                ulong now = HashFromStack(d);
                other ^= ~(now ^ HashFromStack(d - 1));

                var diff = ogHash ^ now;
                int slot = Hash1(diff);

                if (diff != Keys[slot])
                    slot = Hash2(diff);

                if (diff != Keys[slot])
                    continue;

                Move m = Moves[slot];
                int moveFrom = m.From;
                int moveTo = m.To;

                if ((bb.Occupancy & LineBB[moveFrom][moveTo]) == 0)
                {
                    if (ply > d)
                        return true;

                    int pc = (bb.GetPieceAtIndex(moveFrom) != None) ? bb.GetColorAtIndex(moveFrom) : bb.GetColorAtIndex(moveTo);
                    return pc == pos.ToMove;
                }
            }

            return false;
        }
    }
}
