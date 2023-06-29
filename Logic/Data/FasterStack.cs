using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Data
{
    public class FasterStack<T>
    {
        public const int MaxCapacity = 512;
        public const int NormalCapacity = 256;

        private readonly T[] arr;
        private int size;

        public int Capacity;
        public int Count => size;

        [MethodImpl(Inline)]
        public FasterStack(int capacity = NormalCapacity)
        {
            this.Capacity = capacity;
            arr = new T[Capacity];
        }

        [MethodImpl(Inline)]
        public FasterStack<T> Clone()
        {
            FasterStack<T> clone = new FasterStack<T>(this.arr.Length);
            Array.Copy(this.arr, clone.arr, this.arr.Length);
            return clone;
        }

        [MethodImpl(Inline)]
        public void Push(T item)
        {
            arr[size++] = item;
        }

        [MethodImpl(Inline)]
        public T Pop()
        {
            return arr[--size];
        }

        /// <summary>
        /// Returns the value at the top of the stack (index 0), which is the rightmost element of the array.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(Inline)]
        public T Peek() => Peek(0);

        /// <summary>
        /// Returns the value of the <paramref name="idx"/>'th element in the stack, with index 0 being the rightmost element, index 1 being the second to last, etc.
        /// </summary>
        [MethodImpl(Inline)]
        public T Peek(int idx)
        {
            return arr[size - idx - 1];
        }

        /// <summary>
        /// Sets the size to 0, which essentially clears the array.
        /// </summary>
        public void Clear()
        {
            size = 0;
            arr[size] = default;
        }

        public T[] AsArray()
        {
            T[] arrCopy = new T[size];
            for (int i = 0; i < size; i++)
            {
                arrCopy[i] = arr[i];
            }
            return arrCopy;
        }

        public void CopyFromArray(FasterStack<T> otherStack) => CopyFromArray(otherStack.AsArray());

        public void CopyFromArray(T[] otherArr)
        {
            for (int i = 0; i < Math.Min(Capacity, otherArr.Length); i++)
            {
                this.arr[i] = otherArr[i];
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < size; i++)
            {
                sb.Append(arr[i].ToString() + ", ");
            }
            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }

            return sb.ToString();
        }

    }
}
