using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void Wait(object cs)
        {
            lock (waitersLock)
            {
                ++waiters;
            }

            Monitor.Exit(cs);

            sema.Wait();

            lock (waitersLock)
            {
                --waiters;
            }

            Monitor.Enter(cs);
        }
    }

}
