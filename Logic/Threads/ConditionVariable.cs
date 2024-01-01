namespace LTChess.Logic.Threads
{
    /// <summary>
    /// 
    /// 
    /// https://stackoverflow.com/questions/15657637/condition-variables-c-net/37163788#37163788
    /// 
    /// 
    /// </summary>
    public class ConditionVariable
    {
        private int waiters = 0;
        private object waitersLock = "cond_t";
        private SemaphoreSlim sema = new SemaphoreSlim(0, Int32.MaxValue);

        public ConditionVariable()
        {
        }

        /// <summary>
        /// Releases the semaphore once if any threads are waiting on it.
        /// <para></para>
        /// This is similar to pthread_cond_signal for the Linux inclined
        /// </summary>
        public void Pulse()
        {
            bool release;

            lock (waitersLock)
            {
                release = waiters > 0;
            }

            if (release)
            {
                sema.Release();
            }
        }

        /// <summary>
        /// Releases the lock on <paramref name="mutex"/>, blocks on the condition, 
        /// and finally reacquires the lock on <paramref name="mutex"/> before returning.
        /// <para></para>
        /// <paramref name="mutex"/> must be locked before it is waited on or an exception will be thrown.
        /// <para></para>
        /// This is similar to pthread_cond_wait for the Linux inclined
        /// </summary>
        public void Wait(object mutex)
        {
            lock (waitersLock)
            {
                ++waiters;
            }

            Monitor.Exit(mutex);

            sema.Wait();

            lock (waitersLock)
            {
                --waiters;
            }

            Monitor.Enter(mutex);
        }
    }

}
