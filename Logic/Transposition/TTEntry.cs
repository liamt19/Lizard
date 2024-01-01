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
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 10)]
    public struct TTEntry
    {
        public static readonly TTEntry Null = new TTEntry(0, 0, TTNodeType.Invalid, 0, Move.Null);

        public const int KeyShift = 64 - (sizeof(ushort) * 8);

        public const int DepthOffset = 7;
        public const int DepthNone = -6;

        [FieldOffset(0)]
        public int _ScoreStatEval;      //  4 = 16 bits + 16 bits

        [FieldOffset(4)]
        public CondensedMove BestMove;  //  2 = 16 bits

        [FieldOffset(6)]
        public ushort Key;              //  2 = 16 bits

        [FieldOffset(8)]
        public sbyte AgePVType;         //  1 =  8 bits (5 bits for age, 1 for isPV, 2 for type)

        [FieldOffset(9)]
        public sbyte _depth;            //  1 =  8 bits


        public short Score
        {
            get => (short)(_ScoreStatEval & 0xFFFF);
            set => _ScoreStatEval = (_ScoreStatEval & ~0xFFFF) | value;
        }

        public short StatEval
        {
            get => (short)((_ScoreStatEval & 0xFFFF0000) >> 16);
            set => _ScoreStatEval = (int)((_ScoreStatEval & 0x0000FFFF) | (value << 16));
        }

        public int Age
        {
            get => AgePVType & 0b11111000;
            set => AgePVType = (sbyte)((AgePVType & 0b00000111) | ((sbyte)value));
        }

        public bool PV
        {
            get => (AgePVType & TT_PV_MASK) != 0;
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
            get => (TTNodeType)(AgePVType & TT_BOUND_MASK);
            set => AgePVType = (sbyte)((AgePVType & ~TT_BOUND_MASK) | (sbyte)value);
        }

        public int Bound
        {
            get => AgePVType & TT_BOUND_MASK;
        }

        public sbyte Depth
        {
            get => (sbyte)(_depth - DepthOffset);
            set => _depth = (sbyte)(value + DepthOffset);
        }

        public bool IsEmpty => _depth == 0;

        public TTEntry(ulong key, short score, TTNodeType nodeType, int depth, Move move)
        {
            this.Key = MakeKey(key);
            this.Score = score;
            this.NodeType = nodeType;
            this.AgePVType = 0;
            this.Depth = (sbyte)depth;

            this.BestMove = new CondensedMove(move);
        }

        [MethodImpl(Inline)]
        public static ushort MakeKey(ulong posHash)
        {
            return (ushort)posHash;
        }

        [MethodImpl(Inline)]
        public void Update(ulong key, short score, TTNodeType nodeType, int depth, CondensedMove move, short statEval, bool isPV = false)
        {
            if (!move.IsNull() || (ushort)key != this.Key)
            {
                this.BestMove = move;
            }

            if (nodeType == TTNodeType.Exact
                || (ushort)key != this.Key
                || depth + (isPV ? 2 : 0) > this._depth - 11)
            {
                this.Key = (ushort)key;
                this.Score = score;
                this.StatEval = statEval;
                this.Depth = (sbyte)depth;
                this.AgePVType = (sbyte)(TranspositionTable.Age | ((isPV ? 1 : 0) << 2) | (int)nodeType);

                if (EnableAssertions)
                {
                    Assert(score == ScoreNone || (score <= ScoreMate && score >= -ScoreMate),
                        "WARN the score " + score + " is outside of bounds for normal TT entries!");
                }
            }
        }

        [MethodImpl(Inline)]
        public void Update(ulong key, short score, TTNodeType nodeType, int depth, Move move, short statEval, bool isPV = false)
        {
            if (move.IsNull())
            {
                Update(key, score, nodeType, depth, CondensedMove.Null, statEval, isPV);
            }
            else
            {
                Update(key, score, nodeType, depth, new CondensedMove(move), statEval, isPV);
            }
        }

        /// <summary>
        /// Converts the <paramref name="ttScore"/> retrieved from a TT hit to a usable score from the root position.
        /// <para></para>
        /// The returned score is also clamped by the root position's HalfmoveClock, so a mate score is treated as a non-mate score
        /// if we might lose by the 50 move rule before that mate.
        /// </summary>
        [MethodImpl(Inline)]
        public static short MakeNormalScore(short ttScore, int ply, int halfmoves)
        {
            if (ttScore == ScoreNone)
            {
                return ttScore;
            }

            if (ttScore >= ScoreTTWin)
            {
                if (ttScore >= ScoreMateMax && ScoreMate - ttScore > 99 - halfmoves)
                {
                    return ScoreMateMax - 1;
                }

                return (short)(ttScore - ply);
            }

            if (ttScore <= ScoreTTLoss)
            {
                if (ttScore <= ScoreMatedMax && ScoreMate + ttScore > 99 - halfmoves)
                {
                    return ScoreMatedMax + 1;
                }

                return (short)(ttScore + ply);
            }

            return ttScore;
        }

        /// <summary>
        /// Converts the <paramref name="score"/> to one suitable for the TT.
        /// <para></para>
        /// If <paramref name="score"/> is a mate score, it would ordinarily be saved as a "mate in X" in relation to the current search ply of X.
        /// This is not correct since a mate in 1 could be delayed by a few moves to make it a mate in mate in 2/3/... instead, so what we really
        /// care about is the number of plies from the current position and not the number of plies when the score was calculated.
        /// </summary>
        [MethodImpl(Inline)]
        public static short MakeTTScore(short score, int ply)
        {
            if (score >= ScoreTTWin)
            {
                return (short)(score + ply);
            }

            if (score <= ScoreTTLoss)
            {
                return (short)(score - ply);
            }

            return score;
        }

        public override string ToString()
        {
            return NodeType.ToString() + ", Depth " + Depth + ", Age: " + Age + ", BestMove " + BestMove.ToString() + ", Score " + Score + ", StatEval: " + StatEval + ", Key " + Key;
        }
    }
}
