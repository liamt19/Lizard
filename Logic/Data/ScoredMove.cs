using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Data
{
    /// <summary>
    /// Contains a <see cref="Data.Move"/> and a score.
    /// </summary>
    public struct ScoredMove
    {
        /// <summary>
        /// CS0199: Fields of static readonly field 'name' cannot be passed ref or out (except in a static constructor)
        /// 
        /// Since a ScoredMove needs a ref Move, the compiler doesn't let it use <see cref="Move.Null"/>
        /// because it wasn't created in the same static constructor as the invisible one below.
        /// </summary>
        private static readonly Move CS0199_NullMove = new Move();
        public static readonly ScoredMove Null = new ScoredMove(ref CS0199_NullMove);

        public Move Move = Move.Null;
        public int Score = ScoreNone;

        public ScoredMove(ref Move m, int score = 0)
        {
            this.Move = m;
            this.Score = score;
        }

        public override string ToString()
        {
            return Move.ToString() + ", " + Score;
        }

        public static bool operator <(ScoredMove a, ScoredMove b) => (a.Score < b.Score);
        public static bool operator >(ScoredMove a, ScoredMove b) => (a.Score > b.Score);

        public bool Equals(ScoredMove other)
        {
            return Score == other.Score && Move.Equals(other.Move);
        }
    }
}
