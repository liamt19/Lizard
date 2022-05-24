using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Data.RunOptions;

namespace LTChess.Data
{
    /// <summary>
    /// Just a container for an array of Moves which is faster than using a List and looks nicer than a simple array.
    /// </summary>
    public ref struct _MoveList
    {
        public const int MAX_CAPACITY = 256;
        public const int NORMAL_CAPACITY = 128;
        public Span<Move> arr;

        /// <summary>
        /// The number of moves that the array contains.
        /// </summary>
        public int Count;

        public _MoveList(int capacity = NORMAL_CAPACITY)
        {
            arr = new Move[capacity];
            Count = 0;
        }

        /// <summary>
        /// Apparently doing 'new Movelist()' doesn't call the other constructor even though the parameter has a default value?
        /// </summary>
        public _MoveList()
        {
            arr = new Move[NORMAL_CAPACITY];
            Count = 0;
        }

        public Move this[int idx]
        {
            get { return arr[idx]; }
            set { arr[idx] = value; }
        }

        [MethodImpl(Inline)]
        public void Add(in Move m)
        {
            arr[Count++] = m;
        }

        public void Add(_MoveList other)
        {
            for (int i = 0; i < other.Count; i++)
            {
                arr[Count++] = other.arr[i];
            }
        }

        public Move[] ToArray()
        {
            Move[] temp = new Move[Count];
            for (int i = 0; i < Count; i++)
            {
                temp[i] = arr[i];
            }
            return temp;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Count; i++)
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
