﻿using System.Diagnostics;

namespace LTChess.Logic.Search
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

        public int MovesToGo = -1;

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

        [MethodImpl(Inline)]
        public void StartTimer() => TotalSearchTime.Start();

        [MethodImpl(Inline)]
        public void StopTimer() => TotalSearchTime.Stop();

        [MethodImpl(Inline)]
        public void ResetTimer() => TotalSearchTime.Reset();

        [MethodImpl(Inline)]
        public void RestartTimer() => TotalSearchTime.Restart();

        /// <summary>
        /// Returns the current search time in milliseconds.
        /// </summary>
        [MethodImpl(Inline)]
        public double GetSearchTime() => TotalSearchTime.Elapsed.TotalMilliseconds;

        /// <summary>
        /// Checks if the player <paramref name="ToMove"/> has searched for their allotted time,
        /// returning true if they should stop searching as soon as possible.
        /// </summary>
        [MethodImpl(Inline)]
        public bool CheckUp()
        {
            bool shouldStop = false;

            double currentTime = GetSearchTime();

            if (currentTime > (MaxSearchTime - (HasMoveTime ? MoveTimeBuffer : TimerBuffer)))
            {
                //  Stop if we are close to going over the max time
                if (UCI.Active)
                {
                    Log("Stopping normally! Used " + currentTime + " of allowed " + MaxSearchTime + "ms at " + FormatCurrentTime());
                }

                shouldStop = true;
            }
            else if (MaxSearchTime >= PlayerTime && (PlayerTime - currentTime) < SearchLowTimeThreshold)
            {
                //  Stop early if:
                //  We were told to search for more time than we have left AND
                //  We now have less time than the low time threshold

                if ((currentTime < MinSearchTime) && ((PlayerTime - TimerBuffer) > MinSearchTime))
                {
                    //  If we ordinarily would stop, try enforcing a minimum search time
                    //  to prevent the time spent on moves from oscillating to a large degree.

                    //  As long as we have enough time left that this condition will be checked again,
                    //  postpone stopping until TotalSearchTime.Elapsed.TotalMilliseconds > MinSearchTime

                    Log("Postponed stopping! Only searched for " + currentTime + "ms of our " + (PlayerTime - TimerBuffer) + " at " + FormatCurrentTime());
                }
                else
                {
                    Log("Stopping early! maxTime: " + MaxSearchTime + " >= playerTimeLeft: " + PlayerTime + " and we are in low time at " + FormatCurrentTime());

                    shouldStop = true;
                }

            }

            return shouldStop;
        }


        /// <summary>
        /// Sets the maximum search time for the player <paramref name="ToMove"/>, given the number of moves <paramref name="moveCount"/> made so far.
        /// <para></para>
        /// This currently prioritizes early game moves since each search is given a percentage of the player's remaining time,
        /// which works well since there are more pieces and therefore more moves that need to be considered in the early/midgame.
        /// </summary>
        [MethodImpl(Inline)]
        public void MakeMoveTime(int ToMove)
        {
            int inc = PlayerIncrement;

#if DEV
            int newSearchTime = PlayerIncrement + (PlayerTime / 3);
#else
            int newSearchTime = PlayerIncrement + (PlayerTime / 20);
#endif

            if (MovesToGo != -1)
            {
                //  This is a fairly simple approach to this:
                //  we either search for 1/20th of our time or 1 / MovesToGo, whichever is greater.
                newSearchTime = Math.Max(newSearchTime, PlayerTime / MovesToGo);
            }

            if (newSearchTime > PlayerTime)
            {
                Log("WARN: MakeMoveTime tried setting time to " + newSearchTime + " > time left " + PlayerTime);
                newSearchTime = PlayerTime;
            }

            this.MaxSearchTime = newSearchTime;
            Log("Setting search time to " + (newSearchTime - inc) + " + " + inc + " = " + newSearchTime);
        }
    }
}
