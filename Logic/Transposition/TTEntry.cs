using System.Runtime.InteropServices;

using static Lizard.Logic.Transposition.TranspositionTable;


namespace Lizard.Logic.Transposition
{
    /// <summary>
    /// Represents an entry within a transposition table. <br></br>
    /// Entries are added when a beta cutoff occurs or new best move is found during a search.
    /// <para></para>
    /// Setting Pack=1/2 causes the struct to NOT pad itself with an extra 2 bytes, so its size would increase from 10 -> 12. 
    /// Each TTCluster contains 3 TTEntry, and TTClusters are meant to align on 32 byte boundaries, so we need this to be 10 bytes max.
    /// <para></para>
    /// The replacement strategy and depth logic are inspired by Stockfish and Berserk.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 10)]
    public struct TTEntry
    {
        public const int DepthOffset = -7;
        public const int DepthNone = -6;

        [FieldOffset(0)]
        public short _Score;            //  2 = 16 bits

        [FieldOffset(2)]
        public short _StatEval;           //  2 = 16 bits

        [FieldOffset(4)]
        public Move BestMove;           //  2 = 16 bits

        [FieldOffset(6)]
        public ushort Key;              //  2 = 16 bits

        [FieldOffset(8)]
        public byte AgePVType;          //  1 =  8 bits (5 bits for age, 1 for isPV, 2 for type)

        [FieldOffset(9)]
        public byte _depth;             //  1 =  8 bits


        public short Score
        {
            get => _Score;
            set => _Score = value;
        }

        public short StatEval
        {
            get => _StatEval;
            set => _StatEval = value;
        }

        public int Age
        {
            get => AgePVType & 0b11111000;
        }

        public bool PV
        {
            get => (AgePVType & TT_PV_MASK) != 0;
        }

        public TTNodeType NodeType
        {
            get => (TTNodeType)(AgePVType & TT_BOUND_MASK);
        }

        public int Bound
        {
            get => AgePVType & TT_BOUND_MASK;
        }

        public int Depth
        {
            get => (_depth + DepthOffset);
            set => _depth = (byte)(value);
        }

        public sbyte RelAge(byte age) => (sbyte)((TT_AGE_CYCLE + age - AgePVType) & TranspositionTable.TT_AGE_MASK);
        public bool IsEmpty => _depth == 0;

        public TTEntry(ulong key, short score, TTNodeType nodeType, int depth, Move move)
        {
            this.Key = MakeKey(key);
            this.Score = score;
            this.AgePVType = 0;
            this.Depth = (sbyte)depth;

            this.BestMove = move;
        }

        public static ushort MakeKey(ulong posHash)
        {
            return (ushort)posHash;
        }

        public void Update(ulong key, short score, TTNodeType nodeType, int depth, Move move, short statEval, bool isPV = false)
        {
            var k = (ushort)key;
            if (!move.IsNull() || k != Key)
            {
                BestMove = move;
            }

            if (nodeType == TTNodeType.Exact
                || k != Key
                || depth + (isPV ? 2 : 0) > _depth - 4 + DepthOffset)
            {
                Key = k;
                Score = score;
                StatEval = statEval;
                Depth = (byte)(depth - DepthOffset);
                AgePVType = (byte)(TranspositionTable.Age | ((isPV ? 1u : 0u) << 2) | (uint)nodeType);

                Assert(score == ScoreNone || (score <= ScoreMate && score >= -ScoreMate),
                    $"WARN the score {score} is outside of bounds for normal TT entries!");
            }
        }


        /// <summary>
        /// Converts the <paramref name="ttScore"/> retrieved from a TT hit to a usable score from the root position.
        /// </summary>
        public static short MakeNormalScore(short ttScore, int ply)
        {
            if (ttScore == ScoreNone)
            {
                return ttScore;
            }

            if (ttScore >= ScoreTTWin)
            {
                return (short)(ttScore - ply);
            }

            if (ttScore <= ScoreTTLoss)
            {
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
        public static short MakeTTScore(short score, int ply)
        {
            if (score == ScoreNone)
            {
                return score;
            }

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
