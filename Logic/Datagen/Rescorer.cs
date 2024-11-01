
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using static Lizard.Logic.Datagen.DatagenParameters;

using Lizard.Logic.Datagen;
using Lizard.Logic.Threads;

namespace Lizard.Logic.Util
{
    public unsafe static class Rescorer
    {
        private const int MaxEntriesInQueue = 64;
        private const int ChunkSize = 1024;
        private const int BytesToRead = BulletFormatEntry.Size * ChunkSize;
        private const double BlendPercentage = 0.0;

        static readonly ConcurrentQueue<byte[]> InputQueue = new ConcurrentQueue<byte[]>();
        static readonly SemaphoreSlim QueueSignal = new SemaphoreSlim(MaxEntriesInQueue);
        static bool readingCompleted = false;


        public static void Start(string inputFile, int workerCount = 1)
        {
            SearchOptions.Hash = HashSize;

            Task readerThread = Task.Run(() => ReaderTask(inputFile));

            Parallel.For(0, workerCount, i =>
            {
                WorkerTask(inputFile, i);
            });

            readerThread.Wait();
        }

        static void ReaderTask(string inputDataFile)
        {
            long fileSize = new FileInfo(inputDataFile).Length;

            using FileStream fs = new FileStream(inputDataFile, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[BytesToRead];
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                // If the last chunk is smaller than the buffer size, resize it
                if (bytesRead < buffer.Length)
                {
                    Array.Resize(ref buffer, bytesRead);
                }

                Span<BulletFormatEntry> entries = MemoryMarshal.Cast<byte, BulletFormatEntry>(buffer);
                for (int i = 0; i < entries.Length; i++)
                {
                    fixed (byte* b = entries[i]._pad)
                    {
                        (*(byte*)&b[0]) = 58;
                        (*(short*)&b[1]) = 25395;
                    }
                }

                Console.Title = $"Progress {((double)fs.Position / fs.Length) * 100:N4}%";

                QueueSignal.Wait(); // Only allow MaxEntriesInQueue items to be in memory at any given time

                InputQueue.Enqueue(buffer);
                buffer = new byte[BytesToRead]; // Allocate a new buffer for the next chunk
            }

            readingCompleted = true;
        }

        static void WorkerTask(string inputDataFile, int workerId)
        {
            var finfo = new FileInfo(inputDataFile);
            string outputFile = Path.Join(finfo.Directory.FullName, $"{finfo.Name[..finfo.Name.IndexOf(".bin")]}_rescored{workerId}.bin");

            if (outputFile == inputDataFile) { throw new Exception($"Failed creating outputFile for thread {workerId}!"); }
            Log($"{Environment.CurrentManagedThreadId,2}:{workerId,2} writing to {outputFile}");

            using var file = File.Open(outputFile, FileMode.Create);
            using BinaryWriter outputWriter = new BinaryWriter(file, System.Text.Encoding.UTF8);

            SearchThreadPool pool = new SearchThreadPool(1);
            Position pos = new Position(owner: pool.MainThread);
            ref Bitboard bb = ref pos.bb;

            SearchInformation info = new SearchInformation(pos)
            {
                SoftNodeLimit = SoftNodeLimit,
                NodeLimit = SoftNodeLimit * 20,
                DepthLimit = DepthLimit,
                OnDepthFinish = null,
                OnSearchFinish = null,
            };

            long numRescored = 0;
            Stopwatch sw = Stopwatch.StartNew();

            while (!readingCompleted || !InputQueue.IsEmpty)
            {
                if (InputQueue.TryDequeue(out byte[] data))
                {
                    QueueSignal.Release();
                    Span<BulletFormatEntry> entries = MemoryMarshal.Cast<byte, BulletFormatEntry>(data);

                    pos.LoadFromFEN(InitialFEN);

                    int entryCount = data.Length / BulletFormatEntry.Size;
                    //Log($"WorkerThread{workerId} got {entryCount} entries");
                    for (int i = 0; i < entryCount; i++)
                    {
                        ref BulletFormatEntry e = ref entries[i];

                        e.FillBitboard(ref bb);
                        Selfplay.ResetPosition(pos);

                        pool.TTable.Clear();
                        pool.Clear();

                        pool.StartSearch(pos, ref info);
                        pool.BlockCallerUntilFinished();

                        int score = pool.GetBestThread().RootMoves[0].Score;

                        if (Math.Abs(score) > MaxFilteringScore)
                        {
                            //  If the score is outside the acceptable bounds, leave the entry as it was
                            continue;
                        }

                        //Log($"{pos.GetFEN(),-72}\t{e.score}\t->\t{score}");

                        //  Adjust scores based on BlendPercentage, where:
                        //        0% will save the score as returned by search
                        //      100% will save the existing, unchanged score
                        int blendedScore = (int)((score * (1 - BlendPercentage)) + (e.score * BlendPercentage));
                        e.score = (short)int.Clamp(blendedScore, short.MinValue, short.MaxValue);
                    }
                    numRescored += entryCount;

                    var posPerSec = (double)numRescored / sw.Elapsed.TotalSeconds;
                    Log($"{Environment.CurrentManagedThreadId,2}:{workerId,2}\t" +
                        $"{numRescored,9}" +
                        $"{posPerSec,10:N1}/sec");

                    outputWriter.Write(data, 0, data.Length);
                }
            }
        }

    }
}
