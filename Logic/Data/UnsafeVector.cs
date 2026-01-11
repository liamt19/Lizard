#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lizard.Logic.Data
{
    public unsafe class UnsafeVector<T> where T : new()
    {
        private T* Items;
        private uint Count;
        private uint Extent;

        public uint Size => Count;
        public bool IsEmpty => Count == 0;
        public void Clear() => Count = 0;
        public T* Raw() => Items;

        public UnsafeVector(uint initialItems)
        {
            Extent = initialItems;
            Count = 0;
            Items = AlignedAllocZeroed<T>(Extent);

            for (uint i = 0; i < initialItems - 1; i++)
                EmplaceBack();
        }

        ~UnsafeVector()
        {
            NativeMemory.AlignedFree(Items);
        }

        public T* this[int i] => &Items[i];


        [MethodImpl(Inline)]
        public void EmplaceBack()
        {
            Add(new T());
        }


        [MethodImpl(Inline)] 
        public void Add(T* item) => Add(*item);

        [MethodImpl(Inline)] 
        public void Add(ref T item) => Add(item);

        [MethodImpl(Inline)]
        public void Add(T item)
        {
            if (Count == Extent - 1)
            {
                Extent += 64;
                Items = AlignedRealloc(Items, Extent);
            }

            Items[Count++] = item;
        }



        [MethodImpl(Inline)]
        public T* RemoveLastPtr()
        {
            Debug.Assert(Count > 0);
            return &Items[--Count];
        }

        [MethodImpl(Inline)]
        public ref T RemoveLast()
        {
            Debug.Assert(Count > 0);
            return ref Items[--Count];
        }


        [MethodImpl(Inline)]
        public T* Last()
        {
            Debug.Assert(Count > 0);
            return &Items[Count - 1];
        }

        [MethodImpl(Inline)]
        public ref T LastByRef()
        {
            Debug.Assert(Count > 0);
            return ref Items[Count - 1];
        }
    }
}
