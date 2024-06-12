namespace Lizard.Logic.Search
{
    public static class TimeManager
    {

        /// <summary>
        /// Add this amount of milliseconds to the total search time when checking if the
        /// search should stop, in case the move overhead is very low and the UCI expects
        /// the search to stop very quickly after our time expires.
        /// </summary>
        public const int TimerBuffer = 50;

        /// <summary>
        /// If we got a "movetime" command, we use a smaller buffer to bring the time we actually search
        /// much closer to the requested time.
        /// </summary>
        private const int MoveTimeBuffer = 5;


        public static bool HasMoveTime => MoveTime != -1;
        public static int MoveTime = -1;

        public static bool HasSoftTime => SoftTimeLimit != -1;
        public static double SoftTimeLimit = -1;

        public static bool HasHardTimeLimit => HardTimeLimit != MaxSearchTime;
        public static int HardTimeLimit = MaxSearchTime;

        public static int MovesToGo = DefaultMovesToGo;

        public static int PlayerIncrement;
        public static int PlayerTime;

        //  Side note: Having this be a Stopwatch rather than keeping track of time via DateTime.Now is annoying,
        //  but DateTime.Now can have unacceptably poor resolution (on some machines, like windows 7/8) of
        //  +- 15ms, which can have a big impact for short time controls and especially "movetime" uci commands.
        //  https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-8.0&redirectedfrom=MSDN#datetime-resolution
        private static readonly Stopwatch SearchTimer = new Stopwatch();


        static TimeManager()
        {
            PlayerIncrement = 0;
            PlayerTime = MaxSearchTime;
        }

        public static void StartTimer() => SearchTimer.Start();

        public static void ResetTimer() => SearchTimer.Reset();

        public static double GetSearchTime() => SearchTimer.Elapsed.TotalMilliseconds;
        public static bool IsRunning => SearchTimer.IsRunning;

        /// <summary>
        /// Returns true if we have searched for our maximum allotted time
        /// </summary>
        public static bool CheckUp()
        {
            double currentTime = GetSearchTime();

            if (currentTime > (HardTimeLimit - MoveTimeBuffer))
            {
                //  Stop if we are close to going over the max time
                return true;
            }

            return false;
        }


        /// <summary>
        /// Sets the maximum search time for the player <paramref name="ToMove"/>, given the number of moves <paramref name="moveCount"/> made so far.
        /// <para></para>
        /// This currently prioritizes early game moves since each search is given a percentage of the player's remaining time,
        /// which works well since there are more pieces and therefore more moves that need to be considered in the early/midgame.
        /// </summary>
        public static void MakeMoveTime()
        {
            //  Clamp between [1, time - 50]
            int hardLimit = Math.Min(PlayerIncrement + (PlayerTime / 2), PlayerTime - TimerBuffer);
            hardLimit = Math.Max(hardLimit, 1);


            //  Values from Clarity, then slightly adjusted
            SoftTimeLimit = 0.65 * ((PlayerTime / MovesToGo) + (PlayerIncrement * 3 / 4));
            HardTimeLimit = hardLimit;
        }
    }
}
