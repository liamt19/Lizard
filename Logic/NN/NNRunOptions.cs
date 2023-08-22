using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.NN
{
    public static class NNRunOptions
    {
        /// <summary>
        /// If true, the Simple768 network will take the place of the classical evaluation function.
        /// The included network (Thanks https://github.com/TheBlackPlague) runs extremely quickly (around 1,750,000 nps)
        /// but isn't quite as strong as the larger + slower HalfKP or HalfKA.
        /// </summary>
        public const bool UseSimple768 = false;

        /// <summary>
        /// If true, the HalfKP network will take the place of the classical evaluation function.
        /// This architecture is marginally weaker than the 768, and a good deal slower than it too (around 700,000 nps).
        /// </summary>
        public const bool UseHalfKP = false;

        /// <summary>
        /// If true, the HalfKA network will take the place of the classical evaluation function.
        /// This architecture is the largest, slowest, but strongest out of the three.
        /// It only runs around 250,000 nps on my machine, but the nodes that it does get to are likely to get a good "true" evaluation.
        /// </summary>
        public const bool UseHalfKA = true;
    }
}
