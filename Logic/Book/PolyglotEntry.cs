using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Book
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PolyglotEntry
    {
        public const int EntrySize = 16;

        //  The format is 3 bits for file, then 3 bits for row.
        //  This is 111111_2 = 0x3F = 63
        private const int MoveMask = 0b111111;

        public ulong Key;
        public ushort RawMove;
        public ushort Weight;
        public uint Learn;

        public int ToSquare => (RawMove & MoveMask);
        public int FromSquare => (RawMove >> 6) & MoveMask;
        public int PromotionTo => (RawMove >> 12) & 0b111;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Key: " + Key.ToString("X") + ", Move: " + IndexToString(FromSquare) + IndexToString(ToSquare));

            if (PromotionTo != 0)
            {
                sb.Append(PieceToFENChar(PromotionTo));
            }

            sb.Append(", Weight: " + Weight + ", Learn: " + Learn);

            return sb.ToString();
        }
    }
}
