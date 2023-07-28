using System.Runtime.InteropServices;
using System.Text;

namespace LTChess.Data
{
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct Move
    {
        public static readonly Move Null = new Move();

        //  7 bits for: idxChecker and idxDoubleChecker since they can be 64.
        //  6 bits for: From, To, and PromotionTo
        //  Total of 32.
        private int _data;

        //  1 bit for each of the 6 flags.
        //  Total of 6.
        //  This could be a byte instead since we only need 6 bits
        //  but it will be padded to a size of 4 bytes anyways.
        private int _flags;

        private const int FlagCapture =     0b000001;
        private const int FlagEnPassant =   0b000010;
        private const int FlagCastle =      0b000100;
        private const int FlagCheck =       0b001000;
        private const int FlagDoubleCheck = 0b010000;
        private const int FlagPromotion =   0b100000;

        public int To
        {
            get => (_data & 0x3F);
            set => _data = ((_data & ~0x3F) | value);
        }

        public int From
        {
            get => ((_data >> 6) & 0x3F);
            set => _data = ((_data & ~(0x3F << 6)) | (value << 6));
        }

        public int PromotionTo
        {
            get => ((_data >> 12) & 0x3F);
            set => _data = ((_data & ~(0x3F << 12)) | (value << 12));
        }

        public int SqChecker
        {
            get => ((_data >> 18) & 0x7F);
            set => _data = ((_data & ~(0x7F << 18)) | (value << 18));
        }
        
        public int SqDoubleChecker
        {
            get => ((_data >> 25) & 0x7F);
            set => _data = ((_data & ~(0x7F << 25)) | (value << 25));
        }

        public bool Capture
        {
            get => ((_flags & FlagCapture) != 0);
            set => _flags |= FlagCapture;
        }

        public bool EnPassant
        {
            get => ((_flags & FlagEnPassant) != 0);
            set => _flags |= FlagEnPassant;
        }

        public bool Castle
        {
            get => ((_flags & FlagCastle) != 0);
            set => _flags |= FlagCastle;
        }

        public bool CausesCheck
        {
            get => ((_flags & FlagCheck) != 0);
            set => _flags |= FlagCheck;
        }

        public bool CausesDoubleCheck
        {
            get => ((_flags & FlagDoubleCheck) != 0);
            set => _flags |= FlagDoubleCheck;
        }

        public bool Promotion
        {
            get => ((_flags & FlagPromotion) != 0);
            set => _flags |= FlagPromotion;
        }


        public Move(int from, int to)
        {
            _data |= (to);
            _data |= (from << 6);
        }

        public Move(int from, int to, int promotionTo)
        {
            _data |= (to);
            _data |= (from << 6);
            _data |= (promotionTo << 12);
            Promotion = true;
        }

        public Move(Move m)
        {
            _data |= (m.To);
            _data |= (m.From << 6);
            _data |= (m.PromotionTo << 12);
            _data |= (m.SqChecker << 18);
            _data |= (m.SqDoubleChecker << 25);


            if (m.Capture)
            {
                _flags |= FlagCapture;
            }
            if (m.EnPassant)
            {
                _flags |= FlagEnPassant;
            }
            if (m.Castle)
            {
                _flags |= FlagCastle;
            }
            if (m.CausesCheck)
            {
                _flags |= FlagCheck;
            }
            if (m.CausesDoubleCheck)
            {
                _flags |= FlagDoubleCheck;
            }
            if (m.Promotion)
            {
                _flags |= FlagPromotion;
            }

        }


        [MethodImpl(Inline)]
        public bool IsNull()
        {
            return (From == 0 && To == 0 && SqChecker == 0);
        }

        [MethodImpl(Inline)]
        public ulong GetMoveMask()
        {
            return (SquareBB[From] | SquareBB[To]);
        }

        [MethodImpl(Inline)]
        public string SmithNotation()
        {
            IndexToCoord(From, out int fx, out int fy);
            IndexToCoord(To, out int tx, out int ty);

            if (Promotion)
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1) + char.ToLower(PieceToFENChar(PromotionTo));
            }
            else
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1);
            }
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

                if (Promotion)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
                }
            }

            if (CausesCheck || CausesDoubleCheck)
            {
                sb.Append("+");
            }


            return sb.ToString();
        }

        [MethodImpl(Inline)]
        public override string ToString()
        {
            return SmithNotation();


            StringBuilder sb = new StringBuilder();

            sb.Append(Convert.ToString(_data, 2));
            if (sb.Length != 32)
            {
                sb.Insert(0, "0", 32 - sb.Length);
            }

            sb.Insert(26, " ");
            sb.Insert(20, " ");
            sb.Insert(14, " ");
            sb.Insert(7, " ");

            //sb.Append(Convert.ToString(flags, 2));

            return sb.ToString();
        }

        [MethodImpl(Inline)]
        public override bool Equals(object? obj)
        {
            Move other = (Move)obj;
            return (other.From == this.From && other.To == this.To && other.SqChecker == this.SqChecker);
        }

        [MethodImpl(Inline)]
        public bool ExactlyEqual(object? obj)
        {
            Move other = (Move)obj;
            if (other.From != this.From || other.To != this.To)
            {
                return false;
            }

            if (other.SqChecker != this.SqChecker || other.SqDoubleChecker != this.SqDoubleChecker)
            {
                return false;
            }

            if (other.PromotionTo != this.PromotionTo)
            {
                return false;
            }

            if (other.CausesCheck != this.CausesCheck || other.CausesDoubleCheck != this.CausesDoubleCheck)
            {
                return false;
            }

            if (other.Castle != this.Castle || other.Capture != this.Capture)
            {
                return false;
            }

            if (other.EnPassant != this.EnPassant || other.Promotion != this.Promotion)
            {
                return false;
            }


            return true;
        }


        [MethodImpl(Inline)]
        public static bool operator ==(Move left, Move right)
        {
            return left.Equals(right);
        }

        [MethodImpl(Inline)]
        public static bool operator !=(Move left, Move right)
        {
            return !(left == right);
        }
    }
}
