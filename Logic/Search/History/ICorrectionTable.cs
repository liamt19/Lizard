using System.Runtime.InteropServices;

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

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)(sizeof(StatEntry) * TableElements));
    }
}