using System.Runtime.InteropServices;

using Lizard.Logic.Threads;

namespace Lizard.Logic.Search.History
{
    public unsafe abstract class ICorrectionTable
    {
        protected readonly StatEntry* _History;

        public readonly int TableSize;
        public readonly int TableCount;

        private int TableElements => TableSize * TableCount;

        protected ICorrectionTable(int size = 16384, int tables = ColorNB)
        {
            TableSize = size;
            TableCount = tables;
            _History = (StatEntry*)AlignedAllocZeroed((nuint)(sizeof(StatEntry) * TableElements), AllocAlignment);
        }

        public abstract ref StatEntry this[Position pos, int pc] { get; }
        public abstract ref StatEntry this[Position pos, int pc, int side] { get; }

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)(sizeof(StatEntry) * TableElements));

        public abstract int CorrectionIndex(Position pos, int pc, int side = 0);
    }
}