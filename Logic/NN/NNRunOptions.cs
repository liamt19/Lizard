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
        /// If true, the Simple768 network will be used for static evaluation.
        /// </summary>
        public const bool UseSimple768 = true;

        /// <summary>
        /// If true, the HalfKA network will be used for static evaluation.
        /// <para></para>
        /// This architecture is the largest, slowest, but strongest out of the three.
        /// On my machine, this hits around 400,000 to 800,000 nps depending on how many king moves are made.
        /// <para></para>
        /// Can handle around 215,000 full refreshes per second, per thread.
        /// </summary>
        public const bool UseHalfKA = false;

        /// <summary>
        /// If true, the HalfKP network will be used for static evaluation.
        /// <para></para>
        /// Can handle around 600,000 full refreshes per second, per thread.
        /// </summary>
        public const bool UseHalfKP = false;


        
    }
}
