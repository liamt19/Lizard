using System.Runtime.InteropServices;
using static LTChess.Logic.Transposition.TranspositionTable;


namespace LTChess.Logic.Transposition
{
    /// <summary>
    /// Represents an entry within a transposition table. <br></br>
    /// Entries are added when a beta cutoff occurs or new best move is found during a search.
    /// <para></para>
    /// Setting Pack=1/2 causes the struct to NOT pad itself with an extra 2 bytes, so its size would increase from 10 -> 12. 
    /// Each TTCluster contains 3 TTEntry, and TTClusters are meant to align on 32 byte boundaries, so we need this to be 10 bytes max.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack=2, Size=10)]
    public struct TTEntry
    {
        public static readonly TTEntry Null = new TTEntry(0, 0, TTNodeType.Invalid, 0, Move.Null);

        public const int KeyShift = 64 - (sizeof(ushort) * 8);

        public const int DepthOffset = 7;
        public const int DepthNone = -6;

        public int _ScoreStatEval;      //  4 = 16 bits + 16 bits
        public CondensedMove BestMove;  //  2 = 16 bits
        public ushort Key;              //  2 = 16 bits
        public sbyte AgePVType;         //  1 =  8 bits (5 bits for age, 1 for isPV, 2 for type)
        public sbyte _depth;            //  1 =  8 bits


        public short Score
        {
            get => ((short)(_ScoreStatEval & 0xFFFF));
            set => _ScoreStatEval = ((_ScoreStatEval & ~0xFFFF) | value);
        }

        public short StatEval
        {
            get => ((short)((_ScoreStatEval & 0xFFFF0000) >> 16));
            set => _ScoreStatEval = (int)((_ScoreStatEval & 0x0000FFFF) | (value << 16));
        }

        public int Age
        {
            get => (AgePVType & 0b11111000);
            set => AgePVType = (sbyte)((AgePVType & ~(0b11111000)) | ((sbyte)(value) << 14));
        }

        public bool PV
        {
            get => ((AgePVType & TT_PV_MASK) != 0);
            set
            {
                if (value)
                {
                    AgePVType |= TT_PV_MASK;
                }
                else
                {
                    AgePVType &= ~TT_PV_MASK;
                }
            }
        }

        public TTNodeType NodeType
        {
            get => (((TTNodeType)(AgePVType & TT_BOUND_MASK)));
            set => AgePVType = (sbyte) ((AgePVType & ~TT_BOUND_MASK) | (sbyte)value);
        }

        public int Bound
        {
            get => (AgePVType & TT_BOUND_MASK);
        }

        public sbyte Depth
        {
            get => (sbyte)(_depth - DepthOffset);
            //set => _depth = value;
            set => _depth = (sbyte)(value + DepthOffset);
        }


        public TTEntry(ulong key, short score, TTNodeType nodeType, int depth, Move move)
        {
            this.Key = MakeKey(key);
            this.Score = score;
            this.NodeType = nodeType;
            //this.Depth = (sbyte)(depth + DepthOffset);
            this.Depth = (sbyte)depth;

            this.BestMove = move;
        }

        [MethodImpl(Inline)]
        public static ushort MakeKey(ulong posHash)
        {
            return (ushort)posHash;
        }


        [MethodImpl(Inline)]
        public void Update(ulong key, short score, TTNodeType nodeType, int depth, Move move, short statEval, bool isPV = false)
        {
            if (!move.IsNull() || (ushort) key != this.Key)
            {
                this.BestMove = move;
            }

            if (nodeType == TTNodeType.Exact ||
                (ushort)key != this.Key || 
                depth + 2 * (isPV ? 1 : 0) > this._depth - 11)
            {
                this.Key = (ushort)key;
                this.Score = score;
                this.StatEval = statEval;
                //this.Depth = (sbyte)(depth + DepthOffset);
                this.Depth = (sbyte)depth;
                this.AgePVType = (sbyte)(TranspositionTable.Age | (isPV ? 1 : 0) << 2 | (int)nodeType);

#if DEBUG
                if (score != ScoreNone && (score >= ScoreTTWin || score <= ScoreTTLoss))
                {
                    Log("WARN the score " + score + " is outside of bounds for normal TT entries!");
                }
#endif
            }
        }

        public override string ToString()
        {
            return NodeType.ToString() + ", Depth " + Depth + ", BestMove " + BestMove.ToString() + ", Score " + Score + ", StatEval: " + StatEval + ", Key " + Key;
        }
    }
}
