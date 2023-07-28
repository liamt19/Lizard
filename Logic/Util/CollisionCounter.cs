namespace LTChess.Util
{
    /// <summary>
    /// Used to find better random seeds for Zobrist hashing.
    /// </summary>
    public static class CollisionCounter
    {
        public const int Depth = 4;


        private static Position p;

        private static Random r;

        public static Dictionary<int, ulong> SeedCollisionDict = new Dictionary<int, ulong>();
        public static Dictionary<uint, ulong> KeyHashDict = new Dictionary<uint, ulong>();

        public static ulong CollisionCount = 0;

        static CollisionCounter()
        {
            r = new Random();
        }


        public static void Run()
        {
            int j = 0;

            while (true)
            {
                CollisionCount = 0;
                int thisSeed = r.Next();
                thisSeed = j++;
                Zobrist.Initialize(thisSeed);

                for (int i = 0; i < FishBench.BenchFENs.Length; i++)
                {
                    string fen = FishBench.BenchFENs[i];
                    KeyHashDict.Clear();
                    p = new Position(fen);
                    ColPerft(Depth);

                    if (i == 10)
                        break;
                }

                SeedCollisionDict.Add(thisSeed, CollisionCount);

                string s = thisSeed.ToString();

                int rightPadding = 10 - s.Length;
                Log((s + new string(' ', rightPadding)) + ": " + CollisionCount);
            }
        }


        [MethodImpl(Inline)]
        public static ulong ColPerft(int depth)
        {
            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = p.GenAllLegalMovesTogether(list);

            if (depth == 0)
            {
                return 1UL;
            }
            else if (depth == 1)
            {
                return (ulong)size;
            }

            ulong n = 0;

            for (int i = 0; i < size; i++)
            {
                p.MakeMove(list[i]);

                uint thisKey = TTEntry.MakeKey(p.Hash);

                if (KeyHashDict.ContainsKey(thisKey))
                {
                    if (KeyHashDict[thisKey] != p.Hash)
                    {
                        CollisionCount++;
                    }
                }
                else
                {
                    KeyHashDict.Add(thisKey, p.Hash);
                }

                n += ColPerft(depth - 1);
                p.UnmakeMove();
            }

            return n;
        }
    }
}
