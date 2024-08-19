
using Lizard.Logic.Core;
using System.Collections.Concurrent;

namespace Lizard.Logic.Datagen
{
    public static class ProgressBroker
    {
        private static readonly ConcurrentDictionary<int, ulong> ThreadGameTotals = new ConcurrentDictionary<int, ulong>();
        private static readonly ConcurrentDictionary<int, ulong> ThreadPositionTotals = new ConcurrentDictionary<int, ulong>();
        private static readonly ConcurrentDictionary<int, double> ThreadNPS = new ConcurrentDictionary<int, double>();
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();

        public static void StartMonitoring()
        {
            Task.Run(() => MonitorProgress(TokenSource.Token));
        }

        public static void StopMonitoring()
        {
            TokenSource.Cancel();
        }

        private static void MonitorProgress(CancellationToken token)
        {
            Console.WriteLine("\n");
            Console.WriteLine("                   games       positions      pos/sec");
            (int _, int top) = Console.GetCursorPosition();

            while (!token.IsCancellationRequested)
            {
                Console.SetCursorPosition(0, top);
                Console.CursorVisible = false;
                for (int y = 0; y < Console.WindowHeight - top; y++)
                    Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, top);
                //Console.CursorVisible = true;

                ulong totalGames = 0;
                double totalNPS = 0;
                ulong totalPositions = 0;

                foreach (var kvp in ThreadGameTotals)
                {
                    int id = kvp.Key;
                    var games = kvp.Value;
                    var positions = ThreadPositionTotals[id];
                    var nps = ThreadNPS[id];

                    Console.WriteLine($"Thread {id,3}: {games,12} {positions,15:N0} {nps,12:N2}");

                    totalGames += games;
                    totalPositions += positions;
                    totalNPS += nps;
                }

                Console.WriteLine($"           ------------------------------------------");
                Console.WriteLine($"            {totalGames,12} {totalPositions,15:N0} {totalNPS,12:N2}");

                Thread.Sleep(250);
            }
        }

        public static void ReportProgress(int threadId, ulong gameNum, ulong totalPositions, double nps)
        {
            ThreadGameTotals[threadId] = gameNum;
            ThreadPositionTotals[threadId] = totalPositions;
            ThreadNPS[threadId] = nps;
        }
    }
}
