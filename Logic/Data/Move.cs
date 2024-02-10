using System.Text;

namespace Lizard.Logic.Data
{

    public unsafe struct Move
    {
        public static readonly Move Null = new Move();

        //  6 bits for: From, To, SqChecker
        //  
        //  2 bits for PromotionTo, which defaults to a knight (1), so the "Promotion" flag MUST be looked at before "PromotionTo" is.
        //  (Otherwise every move would show up as a promotion to a knight, woohoo for horses!).
        //  
        //  6 bits for the 6 move flags.
        //  
        //  Total of 26, padded to 32.
        private ushort _data;
        public ushort Data
        {
            get => _data;
            set => _data = value;
        }



        public const int FlagEnPassant = 0b000001 << 14;
        public const int FlagCastle = 0b000010 << 14;
        public const int FlagPromotion = 0b000011 << 14;

        private const int SpecialFlagsMask = 0b000011 << 14;


        /// <summary>
        /// A mask of <see cref="To"/> and <see cref="From"/>
        /// </summary>
        private const int Mask_ToFrom = 0xFFF;


        /// <summary>
        /// Gets or sets the square that this piece is moving to.
        /// </summary>
        public int To
        {
            //  Reminder to future self: This is a property, and calling move.get_To() 157,582,869 times at depth 15 is a no-no.
            get => _data & 0x3F;
            set => _data = (ushort)((_data & ~0x3F) | value);
        }

        /// <summary>
        /// Gets or sets the square that this piece is moving from.
        /// </summary>
        public int From
        {
            //  Reminder to future self: This is a property, and calling move.get_From() 124,310,980 times at depth 15 is a no-no.
            get => (_data >> 6) & 0x3F;
            set => _data = (ushort)((_data & ~(0x3F << 6)) | (value << 6));
        }

        /// <summary>
        /// Gets a mask of this move's To and From, with the From data still remaining left shifted by 6.
        /// Only for use in History/Capture heuristic tables.
        /// </summary>
        public int MoveMask
        {
            get => _data & Mask_ToFrom;
        }

        /// <summary>
        /// Gets or sets the piece type that this pawn is promoting to. This is stored as (piece type - 1) to save space,
        /// so a PromotionTo == 0 (Piece.Pawn) is treated as 1 (Piece.Knight).
        /// </summary>
        public int PromotionTo
        {
            get => ((_data >> 12) & 0x3) + 1;
            set => _data = (ushort)((_data & ~(0x3 << 12)) | ((value - 1) << 12));
        }

        /// <summary>
        /// Gets or sets whether this pawn move is an en passant or not.
        /// </summary>
        public bool EnPassant
        {
            get => (_data & SpecialFlagsMask) == FlagEnPassant;
            set => _data ^= FlagEnPassant;
        }

        /// <summary>
        /// Gets or sets whether this king move is a castling one or not.
        /// </summary>
        public bool Castle
        {
            get => (_data & SpecialFlagsMask) == FlagCastle;
            set => _data ^= FlagCastle;
        }

        /// <summary>
        /// Gets or sets whether this pawn move is a promotion.
        /// </summary>
        public bool Promotion
        {
            get => (_data & SpecialFlagsMask) == FlagPromotion;
            set => _data ^= FlagPromotion;
        }


        public void SetNew(int from, int to) => _data = (ushort)(to | (from << 6));

        public void SetNew(int from, int to, int promotionTo) => _data = (ushort)(to | (from << 6) | ((promotionTo - 1) << 12) | FlagPromotion);


        [MethodImpl(Inline)]
        public bool IsNull()
        {
            return (_data & Mask_ToFrom) == 0;
        }

        /// <summary>
        /// Returns the generic string representation of a move, which is just the move's From square, the To square,
        /// and the piece that the move is promoting to if applicable.
        /// <br></br>
        /// For example, the opening moves "e4 e5, Nf3 Nc6, ..." would be "e2e4 e7e5, g1f3 b8c6, ..."
        /// </summary>
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
            ref Bitboard bb = ref position.bb;

            int moveTo = To;
            int moveFrom = From;

            int pt = bb.PieceTypes[moveFrom];

            if (Castle)
            {
                if (moveTo > moveFrom)
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
                bool cap = bb.Occupied(moveTo);

                if (pt == Piece.Pawn)
                {
                    if (cap || EnPassant)
                    {
                        sb.Append(GetFileChar(GetIndexFile(moveFrom)));
                    }
                }
                else
                {
                    sb.Append(PieceToFENChar(pt));
                }

                //  If multiple of the same piece type can move to the same square, then we have to
                //  differentiate them by including either the file, rank, or both, that this piece is moving from.
                ulong multPieces = bb.AttackersTo(moveTo, bb.Occupancy) & bb.Colors[bb.GetColorAtIndex(moveFrom)] & bb.Pieces[pt];

                //  This isn't done for pawns though since their notation is always unambiguous
                if (popcount(multPieces) > 1 && pt != Pawn)
                {
                    if ((multPieces & GetFileBB(moveFrom)) == SquareBB[moveFrom])
                    {
                        //  If this piece is alone on its file, we only specify the file.
                        sb.Append(GetFileChar(GetIndexFile(moveFrom)));
                    }
                    else if ((multPieces & GetRankBB(moveFrom)) == SquareBB[moveFrom])
                    {
                        //  If this piece wasn't alone on its file, but is alone on its rank, then include the rank.
                        sb.Append(GetIndexRank(moveFrom) + 1);
                    }
                    else
                    {
                        //  If neither the rank/file alone could differentiate this move, then we need both the file and rank
                        sb.Append(GetFileChar(GetIndexFile(moveFrom)));
                        sb.Append(GetIndexRank(moveFrom) + 1);
                    }
                }

                if (cap || EnPassant)
                {
                    sb.Append('x');
                }


                sb.Append(IndexToString(moveTo));

                if (Promotion)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
                }
            }

            return sb.ToString();
        }

        [MethodImpl(Inline)]
        public override string ToString()
        {
            return SmithNotation();
        }


        /// <summary>
        /// Returns true if the <see cref="Move"/> <paramref name="move"/> has the same From/To squares, the same "Castle" flag, and the same PromotionTo piece.
        /// </summary>
        [MethodImpl(Inline)]
        public bool Equals(Move move)
        {
            //  Today we learned that the JIT doesn't appear to create separate paths for Equals(object) and Equals(Move/CondensedMove).
            //  This meant that every time we did move.Equals(other) the generated IL had ~8 extra instructions,
            //  plus a pair of lengthy "box/unbox" instructions since a Move is a value type being passes as an object.

            return (move.Data == Data);
        }



        [MethodImpl(Inline)]
        public bool Equals(ScoredMove move)
        {
            return move.Move.Equals(this);
        }


        [MethodImpl(Inline)]
        public static bool operator ==(Move left, Move right)
        {
            return left.Equals(right);
        }

        [MethodImpl(Inline)]
        public static bool operator !=(Move left, Move right)
        {
            return !left.Equals(right);
        }

        [MethodImpl(Inline)]
        public static bool operator ==(Move left, ScoredMove right)
        {
            return left.Equals(right);
        }

        [MethodImpl(Inline)]
        public static bool operator !=(Move left, ScoredMove right)
        {
            return !left.Equals(right);
        }
    }
}
