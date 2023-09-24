using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Transposition
{
    [StructLayout(LayoutKind.Explicit, Size=32)]
    public unsafe struct TTCluster
    {
        [FieldOffset(0)]
        private TTEntry _elem0;

        [FieldOffset(10)]
        private TTEntry _elem1;

        [FieldOffset(20)]
        private TTEntry _elem2;

        [FieldOffset(30)]
        private fixed byte _pad[2];

        public TTCluster() 
        {
            _elem0 = new TTEntry();
            _elem1 = new TTEntry();
            _elem2 = new TTEntry();

            _pad[0] = (byte) ':';
            _pad[1] = (byte) ')';
        }

        public void Clear()
        {
            fixed(void* ptr = &_elem0)
            {
                //  Clear all 3 here.
                NativeMemory.Clear((void*)ptr, (nuint)sizeof(TTEntry) * 3);
            }
        }

        public ref TTEntry this[int index]
        {
            get
            {
                switch (index)
                {
                    default:
                        return ref _elem0;
                    case 1:
                        return ref _elem1;
                    case 2:
                        return ref _elem2;
                }
            }
        }
    }
}
