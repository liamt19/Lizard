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

        [FieldOffset(0)] private short _Score;      //  2 = 16 bits
        [FieldOffset(2)] private short _StatEval;   //  2 = 16 bits
        [FieldOffset(4)] private Move _Move;        //  2 = 16 bits
        [FieldOffset(6)] private ushort _Key;       //  2 = 16 bits
        [FieldOffset(8)] private byte _AgePVType;   //  1 =  8 bits (5 bits for age, 1 for isPV, 2 for type)
        [FieldOffset(9)] private byte _Depth;       //  1 =  8 bits


        public readonly short Score => _Score;
        public readonly short StatEval => _StatEval;
        public readonly Move BestMove => _Move;
        public readonly ushort Key => _Key;
        public readonly int Depth => (_Depth + DepthOffset);
        public readonly int RawDepth => (_Depth);

        public readonly int Age => _AgePVType & TT_AGE_MASK;
        public readonly bool PV => (_AgePVType & TT_PV_MASK) != 0;
        public readonly TTNodeType NodeType => (TTNodeType)(_AgePVType & TT_BOUND_MASK);
        public readonly int Bound => _AgePVType & TT_BOUND_MASK;


        public readonly sbyte RelAge(byte age) => (sbyte)((TT_AGE_CYCLE + age - _AgePVType) & TT_AGE_MASK);
        public readonly bool IsEmpty => _Depth == 0;

        public void Update(ulong key, short score, TTNodeType nodeType, int depth, Move move, short statEval, byte age, bool isPV = false)
        {
            var k = (ushort)key;
            if (move != Move.Null || k != Key)
            {
                _Move = move;
            }

            if (nodeType == TTNodeType.Exact
                || k != Key
                || depth + (isPV ? 2 : 0) > _Depth - 4 + DepthOffset)
            {
                _Key = k;
                _Score = score;
                _StatEval = statEval;
                _Depth = (byte)(depth - DepthOffset);
                _AgePVType = (byte)(age | ((isPV ? 1u : 0u) << 2) | (uint)nodeType);

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
