using System.Runtime.InteropServices;

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
        private static TTCluster* Clusters;
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
            Clusters = (TTCluster*) AlignedAllocZeroed((nuint)(sizeof(TTCluster) * (int)ClusterCount), AllocAlignment);
            for (ulong i = 0; i < ClusterCount; i++)
            {
                Clusters[i] = new TTCluster();
            }
        }

        /// <summary>
        /// Reinitializes each <see cref="TTCluster"/> within the table.
        /// </summary>
        [MethodImpl(Inline)]
        public static void Clear()
        {
            for (ulong i = 0; i < ClusterCount; i++)
            {
                var cluster = Clusters[i];
                if (cluster[0].Key != 0 || cluster[1].Key != 0 || cluster[2].Key != 0)
                {
                    Clusters[i].Clear();
                }
            }
        }

        /// <summary>
        /// Returns a reference to the <see cref="TTCluster"/> that the <paramref name="hash"/> maps to.
        /// </summary>
        [MethodImpl(Inline)]
        public static ref TTCluster GetCluster(ulong hash)
        {
            return ref Clusters[ClusterIndex(hash, ClusterCount)];
        }

        /// <summary>
        /// Sets <paramref name="tte"/> to the address of a <see cref="TTEntry"/>.
        /// <para></para>
        /// If the <see cref="TTCluster"/> that the <paramref name="hash"/> maps to contains an entry, 
        /// then <paramref name="tte"/> is the address of that entry and this method returns true, signifying that this was a TT Hit.
        /// <br></br>
        /// Otherwise, this method sets <paramref name="tte"/> to the address of the <see cref="TTEntry"/> within the cluster that should
        /// be overwritten with new information, and this method returns false.
        /// </summary>
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
        private static ulong ClusterIndex(ulong a, ulong b)
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
        /// This is done on every call to <see cref="Search.Search.StartSearching"/> to prevent the transposition table
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
            int recentEntries = 0;
            int Beta = 0;
            int Alpha = 0;
            int Exact = 0;
            int Invalid = 0;

            int NullMoves = 0;

            int[] slots = new int[3];

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

                    if (tt.NodeType != TTNodeType.Invalid)
                    {
                        slots[j]++;
                    }

                    if (tt.BestMove.IsNull() && tt.NodeType != TTNodeType.Invalid)
                    {
                        NullMoves++;
                    }

                    if (tt.Age == Age)
                    {
                        recentEntries++;
                    }
                }
            }

            int entries = Beta + Alpha + Exact;

            //  "Full" is the total number of entries of any age in the TT.
            Log("Full:\t " + entries + " / " + (ClusterCount * EntriesPerCluster) + " = " + (((double)entries / (ClusterCount * EntriesPerCluster)) * 100) + "%");

            //  "Recent" is the number of entries that have the same age as the TT's Age.
            Log("Recent:\t " + recentEntries + " / " + (ClusterCount * EntriesPerCluster) + " = " + (((double)recentEntries / (ClusterCount * EntriesPerCluster)) * 100) + "%");
            
            //  "Slots[0,1,2]" are the number of entries that exist in each TTCluster slot
            Log("Slots:\t " + slots[0] + " / " + slots[1] + " / " + slots[2]);
            Log("Alpha:\t " + Alpha);
            Log("Beta:\t " + Beta);
            Log("Exact:\t " + Exact);
            Log("Invalid: " + Invalid);
            Log("Null:\t " + NullMoves);
        }
    }
}
