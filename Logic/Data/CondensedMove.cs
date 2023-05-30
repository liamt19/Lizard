using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LTChess.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CondensedMove
    {
        //  6 bits for: from, to, idxEnPassant, idxChecker, idxDoubleChecker
        //  3 bits for: PromotionTo
        //  1 bit for each of the 6 (7 when IsMate works) flags.
        //  Total of 39.

        //  fffffftttttteeeeeeccccccdddddd

        public const int indexSize = 6;
        public const int dataSize = (indexSize * 5);

        public const int promotionToIndex = 8;
        public const int numFlags = 6;

        public const int flagsSize = (3 + numFlags);

        private int data;
        private short flags;


        public CondensedMove(Move m)
        {
            data = (m.from << (dataSize - indexSize));
            data |= (m.to << (dataSize - (2 * indexSize)));
            data |= (m.idxEnPassant << (dataSize - (3 * indexSize)));
            data |= (m.idxChecker << (dataSize - (4 * indexSize)));
            data |= (m.idxDoubleChecker << (dataSize - (5 * indexSize)));
            
            flags = (short)(m.PromotionTo << promotionToIndex);
            flags |= (short)((m.Capture ? 1 : 0) << 5);
            flags |= (short)((m.EnPassant ? 1 : 0) << 4);
            flags |= (short)((m.Castle ? 1 : 0) << 3);
            flags |= (short)((m.CausesCheck ? 1 : 0) << 2);
            flags |= (short)((m.CausesDoubleCheck ? 1 : 0) << 1);
            flags |= (short)(m.Promotion ? 1 : 0);

            Log("cm: [" + ToString() + "]");
        }

        [MethodImpl(Inline)]
        public Move ToMove()
        {
            Move m = new Move(From, To, PromotionTo);
            m.idxEnPassant = idxEnPassant;
            m.idxChecker = idxChecker;
            m.idxDoubleChecker = idxDoubleChecker;
            
            m.Capture = Capture;
            m.EnPassant = EnPassant;
            m.Castle = Castle;
            m.CausesCheck = CausesCheck;
            m.CausesDoubleCheck = CausesDoubleCheck;
            m.Promotion = Promotion;

            return m;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Convert.ToString(data, 2));
            if (sb.Length != 32)
            {
                sb.Insert(0, "0", 32 - sb.Length);
            }

            sb.Insert(30, " ");
            sb.Insert(24, " ");
            sb.Insert(18, " ");
            sb.Insert(12, " ");
            sb.Insert(6, " ");

            sb.Insert(2, "|");

            sb.Append(Convert.ToString(flags, 2));

            sb.Insert(35, " ");

            return sb.ToString();
        }

        public int From => (data >> (dataSize - indexSize));
        
        public int To => (data >> (dataSize - (2 * indexSize)));
        
        public int idxEnPassant => (data >> (dataSize - (3 * indexSize)));
        
        public int idxChecker => (data >> (dataSize - (4 * indexSize)));

        public int idxDoubleChecker => (data >> (dataSize - (5 * indexSize)));

        public int PromotionTo => (flags >> promotionToIndex);

        public bool Capture => ((flags & 0b100000) != 0);

        public bool EnPassant => ((flags & 0b10000) != 0);

        public bool Castle => ((flags & 0b1000) != 0);

        public bool CausesCheck => ((flags & 0b100) != 0);

        public bool CausesDoubleCheck => ((flags & 0b10) != 0);

        public bool Promotion => ((flags & 0b1) != 0);
    }
}
