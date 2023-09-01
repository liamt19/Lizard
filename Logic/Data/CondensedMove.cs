using System.Runtime.InteropServices;
using System.Text;

namespace LTChess.Logic.Data
{
    /// <summary>
    /// Stores a Move's To and From squares, its PromotionTo piece, and whether or not it is an En Passant / Castle.
    /// <para></para>
    /// A <see cref="CondensedMove"/> only has enough information to differentiate it from any other possible move,
    /// so it can't be "Made" with <see cref="Position.MakeMove"/>. 
    /// <br></br>
    /// The point of this is to compare if a move generated with <see cref="Position.GenAllPseudoLegalMovesTogether"/>
    /// is the same as the move stored in a TTEntry.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct CondensedMove
    {
        public static readonly CondensedMove Null = new CondensedMove(0, 0);

        //  6 bits for: From, To
        //  2 bits for: PromotionTo
        //  Total of 16.
        private ushort _data;

        private const int FlagEnPassant = 0b000001 << 14;
        private const int FlagCastle = 0b000010 << 14;


        public int To
        {
            get => (_data & 0x3F);
            set => _data = (ushort) (((_data & ~0x3F) | value));
        }

        public int From
        {
            get => ((_data >> 6) & 0x3F);
            set => _data = (ushort) ((_data & ~(0x3F << 6)) | (value << 6));
        }

        public int PromotionTo
        {
            get => ((_data >> 12) & 0x3) + 1;
            set => _data = (ushort) ((_data & ~(0x3 << 12)) | ((value - 1) << 12));
        }

        public bool EnPassant
        {
            get => ((_data & FlagEnPassant) != 0);
            set => _data |= FlagEnPassant;
        }

        public bool Castle
        {
            get => ((_data & FlagCastle) != 0);
            set => _data |= FlagCastle;
        }


        public CondensedMove(int from, int to)
        {
            _data = (ushort) (to | (from << 6));
            //_data |= (ushort) (to);
            //_data |= (ushort) (from << 6);
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

            if (PromotionTo != 0 && (ty == IndexBot || ty == IndexTop))
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
        public override bool Equals(object? obj)
        {
            if (obj is CondensedMove)
            {
                CondensedMove other = (CondensedMove)obj;
                return (other.From == this.From && other.To == this.To && other.Castle == this.Castle && other.PromotionTo == this.PromotionTo);
            }

            if (obj is Move)
            {
                Move other = (Move)obj;
                return (other.From == this.From && other.To == this.To && other.Castle == this.Castle && other.PromotionTo == this.PromotionTo);
            }

            return false;
        }

        [MethodImpl(Inline)]
        public static bool operator ==(CondensedMove left, CondensedMove right)
        {
            return left.Equals(right);
        }

        [MethodImpl(Inline)]
        public static bool operator !=(CondensedMove left, CondensedMove right)
        {
            return !(left == right);
        }

        public static implicit operator CondensedMove(Move m) => new CondensedMove(m);



        [MethodImpl(Inline)]
        public bool IsNull()
        {
            return (From == 0 && To == 0);
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
