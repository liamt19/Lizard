
using System.Runtime.CompilerServices;

namespace Lizard.Logic.NN
{
    public unsafe struct PerspectiveUpdate
    {
        public fixed int Adds[2];
        public fixed int Subs[2];
        public int AddCnt = 0;
        public int SubCnt = 0;

        public PerspectiveUpdate() { }

        public void Clear()
        {
            AddCnt = SubCnt = 0;
        }

        public void PushSub(int sub1)
        {
            Subs[SubCnt++] = sub1;
        }

        public void PushSubAdd(int sub1, int add1)
        {
            Subs[SubCnt++] = sub1;
            Adds[AddCnt++] = add1;
        }

        public void PushSubSubAdd(int sub1, int sub2, int add1)
        {
            Subs[SubCnt++] = sub1;
            Subs[SubCnt++] = sub2;
            Adds[AddCnt++] = add1;
        }

        public void PushSubSubAddAdd(int sub1, int sub2, int add1, int add2)
        {
            Subs[SubCnt++] = sub1;
            Subs[SubCnt++] = sub2;
            Adds[AddCnt++] = add1;
            Adds[AddCnt++] = add2;
        }
    }

    [InlineArray(2)]
    public unsafe struct NetworkUpdate
    {
        public PerspectiveUpdate _Update;
    }
}
