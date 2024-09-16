using System.Runtime.InteropServices;

using Lizard.Logic.Threads;

namespace Lizard.Logic.Search.History
{
    public unsafe abstract class ICorrectionTable
    {
        private readonly StatEntry* _History;

        public readonly int TableSize;
        public readonly int TableCount;

        private int TableElements => TableSize * TableCount;

        protected ICorrectionTable(int size = 16384, int tables = ColorNB)
        {
            TableSize = size;
            TableCount = tables;
            _History = (StatEntry*)AlignedAllocZeroed((nuint)(sizeof(StatEntry) * TableElements), AllocAlignment);
        }

        public ref StatEntry this[Position pos, int pc] => ref _History[CorrectionIndex(pos, pc)];

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)(sizeof(StatEntry) * TableElements));

        public abstract int CorrectionIndex(Position pos, int pc);
    }
}