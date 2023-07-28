using System.Runtime.InteropServices;

namespace LTChess.Transposition
{
    /// <summary>
    /// Represents an entry within a transposition table.
    /// Entries are added when a beta cutoff occurs or new best move is found during a search.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct TTEntry
    {
        public static readonly TTEntry Null = new TTEntry(0, 0, TTNodeType.Invalid, 0, Move.Null);

        public const int KeyShift = 64 - sizeof(uint);

        public uint Key;        //  4 bytes
        private int _data;      //  4 bytes
        public Move BestMove;   //  8 bytes


        public short Eval
        {
            get => ((short)(_data & 0xFFFF));
            set => _data = ((_data & ~0xFFFF) | value);
        }

        public TTNodeType NodeType
        {
            get => (TTNodeType) ((_data >> 28) & 0x0F);
            set => _data = ((_data & ~(0x0F << 28)) | (((int)value) << 28));
        }

        public byte Depth
        {
            get => ((byte)((_data >> 16) & 0xFF));
            set => _data = ((_data & ~(0xFF << 16)) | ((value) << 16));
        }


        public TTEntry(ulong key, short eval, TTNodeType nodeType, int depth, Move move)
        {
            this.Key = MakeKey(key);
            this.Eval = eval;
            this.NodeType = nodeType;
            this.Depth = (byte)depth;
            
            this.BestMove = move;
        }

        [MethodImpl(Inline)]
        public static uint MakeKey(ulong posHash)
        {
            return (uint)(posHash >> KeyShift);
        }

        [MethodImpl(Inline)]
        public bool ValidateKey(ulong hash)
        {
            return Key == (uint)(hash >> KeyShift);
        }

        public override string ToString()
        {
            return NodeType.ToString() + ", depth " + Depth + ", move " + BestMove.ToString() + " MoveEval " + Eval + ", key " + Key;
        }
    }
}
