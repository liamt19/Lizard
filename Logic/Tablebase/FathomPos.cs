using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uint8_t = byte;
using uint64_t = ulong;

namespace Lizard.Logic.Tablebase
{
    public struct FathomPos(ulong white, ulong black, ulong kings, ulong queens, ulong rooks, ulong bishops, ulong knights, ulong pawns, byte rule50, byte ep, bool turn)
    {
        public uint64_t white = white;
        public uint64_t black = black;

        public uint64_t kings = kings;
        public uint64_t queens = queens;
        public uint64_t rooks = rooks;
        public uint64_t bishops = bishops;
        public uint64_t knights = knights;
        public uint64_t pawns = pawns;

        public uint8_t rule50 = rule50;
        public uint8_t ep = ep;

        public bool turn = turn;

        public static unsafe FathomPos FromPosition(Position pos)
        {
            return new(
                pos.bb.Colors[White],
                pos.bb.Colors[Black],
                pos.bb.Pieces[King],
                pos.bb.Pieces[Queen],
                pos.bb.Pieces[Rook],
                pos.bb.Pieces[Bishop],
                pos.bb.Pieces[Knight],
                pos.bb.Pieces[Pawn],
                (byte)(pos.State->HalfmoveClock),
                (byte)(pos.State->EPSquare),
                (pos.ToMove == White));
        }
    }
}
