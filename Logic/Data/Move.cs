using System.Runtime.InteropServices;
using System.Text;

namespace LTChess.Logic.Data
{
    [StructLayout(LayoutKind.Auto)]
    public unsafe struct Move
    {
        public static readonly Move Null = new Move();

        //  6 bits for: From, To, SqChecker, and SqDoubleChecker
        //  
        //  2 bits for PromotionTo, which defaults to a knight (1), so the "Promotion" flag MUST be looked at before "PromotionTo" is.
        //  (Otherwise every move would show up as a promotion to a knight, woohoo for horses!).
        //  
        //  6 bits for the 6 move flags.
        //  
        //  Total of 32.
        private int _data;

        private const int FlagCapture =     0b000001 << 26;
        private const int FlagEnPassant =   0b000010 << 26;
        private const int FlagCastle =      0b000100 << 26;
        private const int FlagCheck =       0b001000 << 26;
        private const int FlagDoubleCheck = 0b010000 << 26;
        private const int FlagPromotion =   0b100000 << 26;

        private const int Mask_Check = 0b011000 << 26;
        private const int Mask_ToFrom = 0xFFFF;

        /// <summary>
        /// Reminder to future self: This is a property, and calling move.get_To() 157,582,869 times at depth 15 is a no-no.
        /// </summary>
        public int To
        {
            get => (_data & 0x3F);
            set => _data = ((_data & ~0x3F) | value);
        }

        /// <summary>
        /// Reminder to future self: This is a property, and calling move.get_From() 124,310,980 times at depth 15 is a no-no.
        /// </summary>
        public int From
        {
            get => ((_data >> 6) & 0x3F);
            set => _data = ((_data & ~(0x3F << 6)) | (value << 6));
        }

        public int SqChecker
        {
            get => ((_data >> 12) & 0x3F);
            set => _data = ((_data & ~(0x3F << 12)) | (value << 12));
        }

        public int SqDoubleChecker
        {
            get => ((_data >> 18) & 0x3F);
            set => _data = ((_data & ~(0x3F << 18)) | (value << 18));
        }

        public int PromotionTo
        {
            get => ((_data >> 24) & 0x3) + 1;
            set => _data = ((_data & ~(0x3 << 24)) | ((value - 1) << 24));
        }

        public bool Capture
        {
            get => ((_data & FlagCapture) != 0);
            set => _data |= FlagCapture;
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

        public bool CausesCheck
        {
            get => ((_data & FlagCheck) != 0);
            set => _data |= FlagCheck;
        }

        public bool CausesDoubleCheck
        {
            get => ((_data & FlagDoubleCheck) != 0);
            set => _data |= FlagDoubleCheck;
        }

        public bool Promotion
        {
            get => ((_data & FlagPromotion) != 0);
            set => _data |= FlagPromotion;
        }

        public bool Checks => ((_data & Mask_Check) != 0);


        public Move(int from, int to)
        {
            _data |= (to);
            _data |= (from << 6);
        }

        public Move(int from, int to, int promotionTo)
        {
            _data |= (to);
            _data |= (from << 6);
            this.PromotionTo = promotionTo;
            Promotion = true;
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
            if (obj is Move move)
            {
                return (move.From == this.From && move.To == this.To && move.Castle == this.Castle && move.PromotionTo == this.PromotionTo);
            }

            if (obj is CondensedMove other)
            {
                return (other.From == this.From && other.To == this.To && other.Castle == this.Castle && other.PromotionTo == this.PromotionTo);
            }

            return false;
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
