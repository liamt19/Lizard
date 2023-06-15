
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LTChess.Transposition
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TTEntry
    {
        public ushort Key;
        public const int KeyShift = 48;

        public short Eval;
        public NodeType NodeType;
        public byte Depth;
        public Move BestMove;

        public TTEntry(ulong key, short eval, NodeType nodeType, int depth, Move move)
        {
            this.Key = (ushort)(key >> KeyShift);
            this.Eval = eval;
            this.NodeType = nodeType;
            this.Depth = (byte)depth;
            this.BestMove = move;
        }

        public static ushort MakeKey(ulong posHash)
        {
            return (ushort)(posHash >> KeyShift);
        }

        public override string ToString()
        {
            return "[" + Key + "]: " + NodeType.ToString() + " at depth " + Depth + ", " + BestMove.ToString() + " eval " + Eval;
        }
    }
}
