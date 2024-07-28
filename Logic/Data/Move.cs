
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()

using System.Text;
using System.Runtime.CompilerServices;

namespace Lizard.Logic.Data
{
    public unsafe readonly struct Move(int from, int to, int flags = 0)
    {
        public static readonly Move Null = new Move();

        //  6 bits for From and To
        //  
        //  2 bits for the EnPassant/Castle/Promotion flags
        //  2 bits for PromotionTo, which defaults to a knight (1), so the "Promotion" flag MUST be looked at before "PromotionTo" is.
        //  (Otherwise every move would show up as a promotion to a knight, woohoo for horses!).
        private readonly ushort _data = (ushort)(to | (from << 6) | flags);


        [MethodImpl(Inline)] 
        public ushort GetData() => _data;



        public const int FlagEnPassant  = 0b0001 << 12;
        public const int FlagCastle     = 0b0010 << 12;
        public const int FlagPromotion  = 0b0011 << 12;

        private const int SpecialFlagsMask = 0b0011 << 12;

        public const int FlagPromoKnight = 0b00 << 14 | FlagPromotion;
        public const int FlagPromoBishop = 0b01 << 14 | FlagPromotion;
        public const int FlagPromoRook   = 0b10 << 14 | FlagPromotion;
        public const int FlagPromoQueen  = 0b11 << 14 | FlagPromotion;

        /// <summary>
        /// A mask of <see cref="GetTo()"/> and <see cref="GetFrom()"/>
        /// </summary>
        private const int Mask_ToFrom = 0xFFF;



        public readonly int To   => (_data >> 0) & 0x3F;
        public readonly int From => (_data >> 6) & 0x3F;

        public readonly int MoveMask => (_data & Mask_ToFrom);

        /// <summary>
        /// Gets the piece type that this pawn is promoting to. This is stored as (piece type - 1) to save space,
        /// so a PromotionTo == 0 (Piece.Pawn) is treated as 1 (Piece.Knight).
        /// </summary>
        public readonly int PromotionTo => ((_data >> 14) & 0x3) + 1;

        public readonly bool IsEnPassant => (_data & SpecialFlagsMask) == FlagEnPassant;
        public readonly bool IsCastle    => (_data & SpecialFlagsMask) == FlagCastle;
        public readonly bool IsPromotion => (_data & SpecialFlagsMask) == FlagPromotion;

        [MethodImpl(Inline)]
        public readonly bool IsNull() => (_data & Mask_ToFrom) == 0;


        [MethodImpl(Inline)]
        public readonly int CastlingKingSquare()
        {
            if (From < A2)
            {
                return (To > From) ? G1 : C1;
            }

            return (To > From) ? G8 : C8;
        }

        [MethodImpl(Inline)]
        public readonly int CastlingRookSquare()
        {
            if (From < A2)
            {
                return (To > From) ? F1 : D1;
            }

            return (To > From) ? F8 : D8;
        }

        [MethodImpl(Inline)]
        public readonly CastlingStatus RelevantCastlingRight()
        {
            if (From < A2)
            {
                return (To > From) ? CastlingStatus.WK : CastlingStatus.WQ;
            }

            return (To > From) ? CastlingStatus.BK : CastlingStatus.BQ;
        }

        /// <summary>
        /// Returns the generic string representation of a move, which is just the move's From square, the To square,
        /// and the piece that the move is promoting to if applicable.
        /// <br></br>
        /// For example, the opening moves "e4 e5, Nf3 Nc6, ..." would be "e2e4 e7e5, g1f3 b8c6, ..."
        /// </summary>
        public string SmithNotation(bool is960 = false)
        {
            IndexToCoord(From, out int fx, out int fy);
            IndexToCoord(To, out int tx, out int ty);

            if (IsCastle && !is960)
            {
                tx = (tx > fx) ? Files.G : Files.C;
            }

            if (IsPromotion)
            {
                return "" + GetFileChar(fx) + (fy + 1) + GetFileChar(tx) + (ty + 1) + char.ToLower(PieceToFENChar(PromotionTo));
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

            int moveTo = To;
            int moveFrom = From;

            int pt = bb.GetPieceAtIndex(moveFrom);

            if (IsCastle)
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
                    if (cap || IsEnPassant)
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

                if (cap || IsEnPassant)
                {
                    sb.Append('x');
                }


                sb.Append(IndexToString(moveTo));

                if (IsPromotion)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
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
