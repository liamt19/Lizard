namespace Lizard.Logic.Search
{
    public class TimeManager
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

        /// <summary>
        /// The minimum amount of time to search, regardless of the other limitations of the search.
        /// This only applies to the amount of time that we were told to search for (i.e. "movetime 100").
        /// If we receive a "stop" command from the UCI, this does no apply and we stop as soon as possible.
        /// </summary>
        public const int MinSearchTime = 200;

        /// <summary>
        /// Set to true if the go command has the "movetime" parameter.
        /// </summary>
        public bool HasMoveTime = false;

        /// <summary>
        /// The time in milliseconds that we were explicitly told to search for.
        /// </summary>
        public int MoveTime = 0;

        /// <summary>
        /// The time in milliseconds that the search should stop at.
        /// </summary>
        public int MaxSearchTime = DefaultSearchTime;


        public double SoftTimeLimit = -1;
        public bool HasSoftTime => SoftTimeLimit > 0;


        public int MovesToGo = DefaultMovesToGo;

        /// <summary>
        /// Set to the value of winc/binc if one was provided during a UCI "go" command.
        /// Only used
        /// </summary>
        public int PlayerIncrement;

        /// <summary>
        /// Set to the value of wtime/btime if one was provided during a UCI "go" command.
        /// If the search time gets too close to this, it will stop prematurely so we don't lose on time.
        /// </summary>
        public int PlayerTime;

        //  Side note: Having this be a Stopwatch rather than keeping track of time via DateTime.Now is annoying,
        //  but DateTime.Now can have unacceptably poor resolution (on some machines, like windows 7/8) of
        //  +- 15ms, which can have a big impact for short time controls and especially "movetime" uci commands.
        //  https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-8.0&redirectedfrom=MSDN#datetime-resolution
        private static Stopwatch TotalSearchTime = new Stopwatch();


        public TimeManager()
        {
            PlayerIncrement = 0;
            PlayerTime = SearchConstants.MaxSearchTime;
        }

        public void StartTimer() => TotalSearchTime.Start();

        public void StopTimer() => TotalSearchTime.Stop();

        public void ResetTimer() => TotalSearchTime.Reset();

        public void RestartTimer() => TotalSearchTime.Restart();

        /// <summary>
        /// Returns the current search time in milliseconds.
        /// </summary>
        public double GetSearchTime() => TotalSearchTime.Elapsed.TotalMilliseconds;

        /// <summary>
        /// Returns true if we have searched for our maximum allotted time
        /// </summary>
        public bool CheckUp()
        {
            double currentTime = GetSearchTime();

            if (currentTime > (MaxSearchTime - (HasMoveTime ? MoveTimeBuffer : TimerBuffer)))
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
        public void MakeMoveTime()
        {
            int newSearchTime = PlayerIncrement + (PlayerTime / 2);

            if (MovesToGo != -1)
            {
                newSearchTime = Math.Max(newSearchTime, PlayerIncrement + (PlayerTime / MovesToGo));
            }

            if (newSearchTime > PlayerTime)
            {
                Log("WARN: MakeMoveTime tried setting time to " + newSearchTime + " > time left " + PlayerTime);
                newSearchTime = PlayerTime;
            }


            //  Values from Clarity, then slightly adjusted
            SoftTimeLimit = 0.65 * ((PlayerTime / MovesToGo) + (PlayerIncrement * 3 / 4));

            MaxSearchTime = newSearchTime;
            Log("Setting search time to " + SoftTimeLimit + ", hard limit at " + newSearchTime);
        }
    }
}
