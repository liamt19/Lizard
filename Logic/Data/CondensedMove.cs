using System.Runtime.InteropServices;
using System.Text;

namespace Lizard.Logic.Data
{
    /// <summary>
    /// Stores a Move's To and From squares, its PromotionTo piece, and whether or not it is an En Passant / Castle.
    /// <para></para>
    /// A <see cref="CondensedMove"/> only has enough information to differentiate it from any other possible move,
    /// so it can't be "Made" with <see cref="Position.MakeMove"/>. 
    /// <br></br>
    /// The point of this is to compare if a move generated with <see cref="Position.GenPseudoLegal"/>
    /// is the same as the move stored in a TTEntry.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct CondensedMove
    {
        public static readonly CondensedMove Null = new CondensedMove(0, 0);

        //  6 bits for: From, To
        //  2 bits for: PromotionTo
        //  2 bits for the Castle and EP flags.

        //  Total of 16.
        private ushort _data;
        public ushort Data
        {
            get => _data;
            set => _data = value;
        }

        private const int FlagEnPassant = 0b000001 << 14;
        private const int FlagCastle = 0b000010 << 14;

        /// <summary>
        /// This is (0x3F | (0x3F &lt;&lt; 6) | (0x3 &lt;&lt; 12))
        /// </summary>
        private const int Mask_Condensed_EQ = 0x3FFF;

        public int GetToFromPromotion => _data & Mask_Condensed_EQ;


        public int To
        {
            get => _data & 0x3F;
            set => _data = (ushort)((_data & ~0x3F) | value);
        }

        public int From
        {
            get => (_data >> 6) & 0x3F;
            set => _data = (ushort)((_data & ~(0x3F << 6)) | (value << 6));
        }

        public int PromotionTo
        {
            get => ((_data >> 12) & 0x3) + 1;
            set => _data = (ushort)((_data & ~(0x3 << 12)) | ((value - 1) << 12));
        }

        /// <summary>
        /// TODO: this isn't needed
        /// </summary>
        public bool EnPassant
        {
            get => (_data & FlagEnPassant) != 0;
            set => _data |= FlagEnPassant;
        }

        public bool Castle
        {
            get => (_data & FlagCastle) != 0;
            set => _data |= FlagCastle;
        }


        public CondensedMove(int from, int to)
        {
            _data = (ushort)(to | (from << 6));
        }

        public CondensedMove(int from, int to, int promotionTo) : this(from, to)
        {
            this.PromotionTo = promotionTo;
        }

        public CondensedMove(Move m)
        {
            _data = (ushort)(m.To | (m.From << 6));

            if (m.Promotion)
            {
                PromotionTo = m.PromotionTo;
            }

            if (m.EnPassant)
            {
                EnPassant = true;
            }

            if (m.Castle)
            {
                Castle = true;
            }
        }



        [MethodImpl(Inline)]
        public string SmithNotation()
        {
            IndexToCoord(From, out int fx, out int fy);
            IndexToCoord(To, out int tx, out int ty);

            if (PromotionTo != 0 && (ty == 0 || ty == 7))
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1) + char.ToLower(PieceToFENChar(PromotionTo));
            }
            else
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1);
            }
        }

        [MethodImpl(Inline)]
        public override string ToString()
        {
            return SmithNotation();
        }

        [MethodImpl(Inline)]
        public bool Equals(CondensedMove other)
        {
            return other.GetToFromPromotion == GetToFromPromotion;
        }

        [MethodImpl(Inline)]
        public bool Equals(Move other)
        {
            return other.Equals(this);
        }



        [MethodImpl(Inline)]
        public bool IsNull()
        {
            return From == 0 && To == 0;
        }

        [MethodImpl(Inline)]
        public string ToString(Position position)
        {
            StringBuilder sb = new StringBuilder();
            int pt = position.bb.PieceTypes[From];

            if (Castle)
            {
                if (To > From)
                {
                    sb.Append("O-O");
                }
                else
                {
                    sb.Append("O-O-O");
                }
            }
            else
            {
                bool cap = position.bb.Occupied(To);

                if (pt == Piece.Pawn)
                {
                    if (cap || EnPassant)
                    {
                        sb.Append(GetFileChar(GetIndexFile(From)));
                    }
                }
                else
                {
                    sb.Append(PieceToFENChar(pt));
                }

                if (cap || EnPassant)
                {
                    sb.Append('x');
                }


                sb.Append(IndexToString(To));

                if (PromotionTo != 0)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
                }
            }

            return sb.ToString();
        }

    }
}
