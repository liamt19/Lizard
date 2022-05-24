using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Data
{
    /// <summary>
    /// Represents a branch during perft
    /// </summary>
    public struct PerftNode
    {
        /// <summary>
        /// The ToString() of the move that was made to create this node
        /// </summary>
        public string root;

        /// <summary>
        /// How many leaves this node has
        /// </summary>
        public ulong number;

        public override string ToString()
        {
            return root + ": " + number;
        }
    }
}
