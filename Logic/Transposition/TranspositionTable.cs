using System.Diagnostics;

namespace LTChess.Logic.Transposition
{
    public static unsafe class TranspositionTable
    {

        public const int TT_BOUND_MASK = (0x3             );
        public const int TT_PV_MASK    = (0x4             );
        public const int TT_AGE_MASK   = (0xF8            );
        public const int TT_AGE_INC    = (0x8             );
        public const int TT_AGE_CYCLE  = (255 + TT_AGE_INC);

        public const int EntriesPerCluster = 3;
        [FixedAddressValueType]
        private static TTCluster[] Clusters;
        private static ulong ClusterCount;
        public static ushort Age = 0;

        private static bool Initialized = false;

        static TranspositionTable()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        /// <summary>
        /// 1 mb is enough for 65,536 entries. 
        /// </summary>
        public static unsafe void Initialize(int mb = 32)
        {

            ClusterCount = ((ulong)mb * 0x100000UL) / (ulong)sizeof(TTCluster);
            Clusters = new TTCluster[ClusterCount];
            for (ulong i = 0; i < ClusterCount; i++)
            {
                Clusters[i] = new TTCluster();
            }
        }


        [MethodImpl(Inline)]
        public static ref TTCluster GetCluster(ulong hash)
        {
            return ref Clusters[ClusterIndex(hash, ClusterCount)];
        }

        [MethodImpl(Inline)]
        public static bool Probe(ulong hash, out TTEntry* tte)
        {
            ref TTCluster cluster = ref GetCluster(hash);
            var key = (ushort)hash;

            for (int i = 0; i < EntriesPerCluster; i++)
            {
                if (cluster[i].Key == key || cluster[i].Depth == 0)
                {
                    cluster[i].AgePVType = (sbyte)(Age | (cluster[i].AgePVType & (TT_AGE_INC - 1)));
                    fixed (TTEntry* addr = &cluster[i])
                    {
                        tte = addr;
                    }
                    return (cluster[i].Depth != 0);
                }
            }

            fixed (TTEntry* addr = &cluster[0])
            {
                tte = addr;
            }

            ref TTEntry replace = ref cluster[0];
            for (int i = 1; i < EntriesPerCluster; i++)
            {
                if ((   replace.Depth - (TT_AGE_CYCLE + Age -    replace.AgePVType) & TT_AGE_MASK) >
                    (cluster[i].Depth - (TT_AGE_CYCLE + Age - cluster[i].AgePVType) & TT_AGE_MASK))
                {
                    replace = cluster[i];
                    fixed (TTEntry* addr = &cluster[i])
                    {
                        tte = addr;
                    }
                }
            }

            return false;
        }


        [MethodImpl(Inline)]
        public static ulong ClusterIndex(ulong a, ulong b)
        {
            ulong aL = (uint)a, aH = a >> 32;
            ulong bL = (uint)b, bH = b >> 32;
            ulong c1 = (aL * bL) >> 32;
            ulong c2 = aH * bL + c1;
            ulong c3 = aL * bH + (uint)c2;
            return aH * bH + (c2 >> 32) + (c3 >> 32);
        }


        /// <summary>
        /// Increases the age that TT entries must have to be considered a "TT Hit".
        /// <br></br>
        /// This is done on every call to <see cref="SimpleSearch.StartSearching"/> to prevent the transposition table
        /// from spilling into another search.
        /// </summary>
        [MethodImpl(Inline)]
        public static void TTUpdate()
        {
            Age += TT_AGE_INC;
        }



        [MethodImpl(Inline)]
        public static int GetHashFull()
        {
            int entries = 0;
            Debug.Assert(ClusterCount > 1000, "Hash is undersized!");

            if (true)
            {
                for (int i = 0; i < 1000; i++)
                {
                    for (int j = 0; j < EntriesPerCluster; j++)
                    {
                        if ((Clusters[i][j].AgePVType & TT_AGE_MASK) == Age)
                        {
                            entries++;
                        }
                    }
                }
                return entries / EntriesPerCluster;
            }

            return entries;
        }


        public static void PrintClusterStatus()
        {
            int Beta = 0;
            int Alpha = 0;
            int Exact = 0;
            int Invalid = 0;

            int NullMoves = 0;

            for (ulong i = 0; i < ClusterCount; i++)
            {
                ref var cluster = ref Clusters[i];
                for (int j = 0; j < EntriesPerCluster; j++)
                {
                    ref var tt = ref cluster[j];
                    if (tt.NodeType == TTNodeType.Beta)
                    {
                        Beta++;
                    }
                    else if (tt.NodeType == TTNodeType.Alpha)
                    {
                        Alpha++;
                    }
                    else if (tt.NodeType == TTNodeType.Exact)
                    {
                        Exact++;
                    }
                    else
                    {
                        Invalid++;
                    }

                    if (tt.BestMove.IsNull() && tt.NodeType != TTNodeType.Invalid)
                    {
                        NullMoves++;
                    }
                }
            }

            int entries = Beta + Alpha + Exact;
            double percent = (double)entries / (ClusterCount * EntriesPerCluster);
            Log("TT:\t " + entries + " / " + (ClusterCount * EntriesPerCluster) + " = " + (percent * 100) + "%");
            Log("Alpha:\t " + Alpha);
            Log("Beta:\t " + Beta);
            Log("Exact:\t " + Exact);
            Log("Invalid: " + Invalid);
            Log("Null:\t " + NullMoves);
        }
    }
}
