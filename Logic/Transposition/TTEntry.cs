
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
        public TTNodeType NodeType;
        public byte Depth;
        public Move BestMove;

        public TTEntry(ulong key, short eval, TTNodeType nodeType, int depth, Move move)
        {
            this.Key = (ushort)(key >> KeyShift);
            this.Eval = eval;
            this.NodeType = nodeType;
            this.Depth = (byte)depth;
            this.BestMove = move;
        }

        [MethodImpl(Inline)]
        public static ushort MakeKey(ulong posHash)
        {
            return (ushort)(posHash >> KeyShift);
        }

        [MethodImpl(Inline)]
        public bool Validate(ulong hash)
        {
            return Key == (ushort)(hash >> KeyShift);
        }

        public override string ToString()
        {
            return NodeType.ToString() + ", depth " + Depth + ", move " + BestMove.ToString() + " eval " + Eval + ", key " + Key;
        }
    }
}
