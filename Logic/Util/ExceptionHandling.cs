using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Util
{
    public static class ExceptionHandling
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Assert(bool condition, string? message)
        {
#if DEBUG
            Debug.Assert(condition, message);
#else
            if (!condition)
            {
                throw new AssertionException("Assertion failed: " + message + Environment.NewLine);
            }
#endif
        }

        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;

            if (e.GetType() == typeof(AssertionException))
            {
                //  This is "handled"
                return;
            }

            Log("An UnhandledException occurred!\r\n" + e.ToString());
            using (FileStream fs = new FileStream(@".\crashlog.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using StreamWriter sw = new StreamWriter(fs);

                sw.WriteLine("An UnhandledException occurred!\r\n" + e.ToString());

                sw.Flush();
            }

            if (UCI.Active)
            {
                //  Try to tell the UCI what happened before this process terminates
                UCI.SendString("info string I'm going to crash! Exception: ");

                //  Send each exception line separately, in case the UCI doesn't like
                //  newlines in the strings that it reads.
                foreach (string s in e.ToString().Split(Environment.NewLine))
                {
                    UCI.SendString("info string " + s);
                    Thread.Sleep(10);
                }

            }
        }

    }

    public class AssertionException : Exception
    {
        public AssertionException() { }
        public AssertionException(string message) : base(message) { }
        public AssertionException(string message, Exception inner) : base(message, inner) { }
    }
}
