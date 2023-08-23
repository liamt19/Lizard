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
        /// <para></para>
        /// The included network (Thanks https://github.com/TheBlackPlague) runs extremely quickly (around 1,750,000 nps)
        /// <para></para>
        /// but isn't quite as strong as the larger and slower HalfKA.
        /// </summary>
        public const bool UseSimple768 = false;

        /// <summary>
        /// If true, the HalfKP network will take the place of the classical evaluation function.
        /// <para></para>
        /// This architecture is marginally weaker than the 768, and a good deal slower than it too (around 700,000 nps).
        /// </summary>
        public const bool UseHalfKP = false;

        /// <summary>
        /// If true, the HalfKA network will take the place of the classical evaluation function.
        /// <para></para>
        /// This architecture is the largest, slowest, but strongest out of the three.
        /// On my machine, this hits around 400,000 to 800,000 nps depending on how many king moves are made.
        /// <para></para>
        /// It takes around 8 times as long to recalculate the entire board using <see cref="HalfKA_HM.FeatureTransformer.RefreshAccumulator"/>
        /// than it does to update it incrementally with <see cref="HalfKA_HM.HalfKA_HM.MakeMove"/>
        /// </summary>
        public const bool UseHalfKA = true;
    }
}
