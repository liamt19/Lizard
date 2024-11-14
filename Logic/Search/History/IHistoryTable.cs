
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.History
{
    public abstract unsafe class IHistoryTable<T>
    {
        protected readonly T* _History;

        public readonly int TableSize;
        public readonly int TableCount;

        private int TableElements => TableSize * TableCount;

        protected IHistoryTable(int tables = ColorNB, int size = 16384)
        {
            TableCount = tables;
            TableSize = size;
            _History = (T*)AlignedAllocZeroed((nuint)(sizeof(T) * TableElements), AllocAlignment);
        }

        public void Dispose() => NativeMemory.AlignedFree(_History);
        public void Clear() => NativeMemory.Clear(_History, (nuint)(sizeof(T) * TableElements));
    }
}