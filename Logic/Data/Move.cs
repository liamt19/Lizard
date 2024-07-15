
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

using System.Text;
using System.Runtime.CompilerServices;

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


        [MethodImpl(Inline)] 
        public ushort GetData() => _data;



        public const int FlagEnPassant = 0b000001 << 14;
        public const int FlagCastle = 0b000010 << 14;
        public const int FlagPromotion = 0b000011 << 14;

        private const int SpecialFlagsMask = 0b000011 << 14;


        /// <summary>
        /// A mask of <see cref="GetTo()"/> and <see cref="GetFrom()"/>
        /// </summary>
        private const int Mask_ToFrom = 0xFFF;



        [MethodImpl(Inline)]
        public int GetTo() => (_data & 0x3F);


        [MethodImpl(Inline)]
        public int GetFrom() => (_data >> 6) & 0x3F;


        [MethodImpl(Inline)] 
        public int GetMoveMask() => (_data & Mask_ToFrom);

        /// <summary>
        /// Gets the piece type that this pawn is promoting to. This is stored as (piece type - 1) to save space,
        /// so a PromotionTo == 0 (Piece.Pawn) is treated as 1 (Piece.Knight).
        /// </summary>
        [MethodImpl(Inline)]
        public int GetPromotionTo() => ((_data >> 12) & 0x3) + 1;


        [MethodImpl(Inline)]
        public bool GetEnPassant() => (_data & SpecialFlagsMask) == FlagEnPassant;


        [MethodImpl(Inline)]
        public bool GetCastle() => (_data & SpecialFlagsMask) == FlagCastle;


        [MethodImpl(Inline)]
        public bool GetPromotion() => (_data & SpecialFlagsMask) == FlagPromotion;


        public Move(int from, int to) => _data = (ushort)(to | (from << 6));

        [MethodImpl(Inline)] 
        public void SetNew(int from, int to) => _data = (ushort)(to | (from << 6));

        [MethodImpl(Inline)] 
        public void SetNew(int from, int to, int promotionTo) => _data = (ushort)(to | (from << 6) | ((promotionTo - 1) << 12) | FlagPromotion);

        [MethodImpl(Inline)] 
        public void SetNewCastle(int from, int to) => _data = (ushort)(to | (from << 6) | FlagCastle);

        [MethodImpl(Inline)] 
        public void SetNewEnPassant(int from, int to) => _data = (ushort)(to | (from << 6) | FlagEnPassant);


        [MethodImpl(Inline)]
        public bool IsNull() => (_data & Mask_ToFrom) == 0;


        [MethodImpl(Inline)]
        public int CastlingKingSquare()
        {
            if (GetFrom() < A2)
            {
                return (GetTo() > GetFrom()) ? G1 : C1;
            }

            return (GetTo() > GetFrom()) ? G8 : C8;
        }

        [MethodImpl(Inline)]
        public int CastlingRookSquare()
        {
            if (GetFrom() < A2)
            {
                return (GetTo() > GetFrom()) ? F1 : D1;
            }

            return (GetTo() > GetFrom()) ? F8 : D8;
        }

        public CastlingStatus RelevantCastlingRight
        {
            get
            {
                if (GetFrom() < A2)
                {
                    return (GetTo() > GetFrom()) ? CastlingStatus.WK : CastlingStatus.WQ;
                }

                return (GetTo() > GetFrom()) ? CastlingStatus.BK : CastlingStatus.BQ;
            }
        }

        /// <summary>
        /// Returns the generic string representation of a move, which is just the move's From square, the To square,
        /// and the piece that the move is promoting to if applicable.
        /// <br></br>
        /// For example, the opening moves "e4 e5, Nf3 Nc6, ..." would be "e2e4 e7e5, g1f3 b8c6, ..."
        /// </summary>
        public string SmithNotation(bool is960 = false)
        {
            IndexToCoord(GetFrom(), out int fx, out int fy);
            IndexToCoord(GetTo(), out int tx, out int ty);

            if (GetCastle() && !is960)
            {
                tx = (tx > fx) ? Files.G : Files.C;
            }

            if (GetPromotion())
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1) + char.ToLower(PieceToFENChar(GetPromotionTo()));
            }
            else
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1);
            }
        }

        public string ToString(Position position)
        {
            StringBuilder sb = new StringBuilder();
            ref Bitboard bb = ref position.bb;

            int moveTo = GetTo();
            int moveFrom = GetFrom();

            int pt = bb.GetPieceAtIndex(moveFrom);

            if (GetCastle())
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
                bool cap = bb.GetPieceAtIndex(moveTo) != None;

                if (pt == Piece.Pawn)
                {
                    if (cap || GetEnPassant())
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

                if (cap || GetEnPassant())
                {
                    sb.Append('x');
                }


                sb.Append(IndexToString(moveTo));

                if (GetPromotion())
                {
                    sb.Append("=" + PieceToFENChar(GetPromotionTo()));
                }
            }

            return sb.ToString();
        }

        public string ToString(bool is960 = false)
        {
            return SmithNotation(is960);
        }

        public override string ToString()
        {
            return ToString(false);
        }


        /// <summary>
        /// Returns true if the <see cref="Move"/> <paramref name="move"/> has the same From/To squares, the same "Castle" flag, and the same PromotionTo piece.
        /// </summary>
        public bool Equals(Move move)
        {
            //  Today we learned that the JIT doesn't appear to create separate paths for Equals(object) and Equals(Move/CondensedMove).
            //  This meant that every time we did move.Equals(other) the generated IL had ~8 extra instructions,
            //  plus a pair of lengthy "box/unbox" instructions since a Move is a value type being passes as an object.

            return (move.GetData() == GetData());
        }



        public bool Equals(ScoredMove move)
        {
            return move.Move.Equals(this);
        }


        public static bool operator ==(Move left, Move right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(Move left, ScoredMove right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Move left, ScoredMove right)
        {
            return !left.Equals(right);
        }
    }
}
