using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace LTChess.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Move
    {
        public static Move Null = new Move();

        public int from;
        public int to;

        /// <summary>
        /// Set to the index of the pawn that would be captured via en passant
        /// </summary>
        public int idxEnPassant = 0;

        /// <summary>
        /// Set to the index of the piece that causes the check, which is this.to unless this move causes a discovery
        /// </summary>
        public int idxChecker = 64;
        public int idxDoubleChecker = 64;
        public int PromotionTo = Piece.None;

        public bool Capture = false;
        public bool EnPassant = false;
        public bool Castle = false;
        public bool CausesCheck = false;
        public bool CausesDoubleCheck = false;
        public bool Promotion = false;

        public bool IsMate = false;


        public Move(int from, int to)
        {
            this.from = from;
            this.to = to;
        }

        public Move(int from, int to, int promotion)
        {
            this.from = from;
            this.to = to;

            this.Promotion = true;
            this.PromotionTo = promotion;
        }

        public Move()
        {
            this.from = 0;
            this.to = 0;
            this.idxChecker = 0;
            this.idxDoubleChecker = 0;
            this.PromotionTo = 0;
        }

        [MethodImpl(Inline)]
        public bool IsNull()
        {
            return (from == 0 && to == 0 && idxChecker == 0);
        }

        /// <summary>
        /// This notation is what chess UCI's use, which just shows the "from" square and "to" square. So 1. e4 looks like "e2e4".
        /// </summary>
        public string SmithNotation()
        {
            IndexToCoord(from, out int fx, out int fy);
            IndexToCoord(to, out int tx, out int ty);

            if (Promotion)
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
            int pt = position.bb.PieceTypes[from];

            if (Castle)
            {
                if (to > from)
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
                bool cap = position.bb.Occupied(to);

                if (pt == Piece.Pawn)
                {
                    if (cap || EnPassant)
                    {
                        sb.Append(GetFileChar(GetIndexFile(from)));
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


                sb.Append(IndexToString(to));

                if (Promotion)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
                }
            }

            if (IsMate)
            {
                sb.Append("#");
            }
            else if (CausesCheck || CausesDoubleCheck)
            {
                sb.Append("+");
            }


            return sb.ToString();
        }


        public string ToStringCorrect(Position position)
        {
            //  TODO: this.
            Bitboard bb = position.bb;
            
            int pt = bb.GetPieceAtIndex(from);
            int pc = bb.GetColorAtIndex(from);
            ulong pieceBB = (bb.Pieces[pt] & bb.Colors[pc]);

            Span<Move> list = stackalloc Move[NormalListCapacity];
            int size = position.GenAllLegalMoves(list);

            //  If multiple of the same type of piece can move to the same square, We include on of the following to differentiate them:
            //  The file from which they moved.
            //  The rank from which they moved.
            //  Both the file and rank.

            //  7k/2N1N3/1N6/8/1N3N2/2N1N3/8/K7 w - - 0 1

            ulong rankBB = GetRankBB(from) & pieceBB;
            ulong fileBB = GetFileBB(from) & pieceBB;

            StringBuilder sb = new StringBuilder();
            if (Castle)
            {
                if (to > from)
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
                bool cap = position.bb.Occupied(to);

                if (pt == Piece.Pawn)
                {
                    if (cap || EnPassant)
                    {
                        sb.Append(GetFileChar(GetIndexFile(from)));
                    }
                }
                else
                {
                    sb.Append(PieceToFENChar(pt));
                }

                if (pt == Piece.Knight && popcount(PrecomputedData.KnightMasks[to] & pieceBB) > 1)
                {
                    if (popcount(fileBB) == 1)
                    {
                        sb.Append(GetFileChar(GetIndexFile(from)));
                    }
                    else if (popcount(rankBB) == 1)
                    {
                        sb.Append(GetIndexRank(from) + 1);
                    }
                    else
                    {
                        sb.Append(GetFileChar(GetIndexFile(from)));
                        sb.Append(GetIndexRank(from) + 1);
                    }
                }

                if (cap || EnPassant)
                {
                    sb.Append('x');
                }


                sb.Append(IndexToString(to));

                if (Promotion)
                {
                    sb.Append("=" + PieceToFENChar(PromotionTo));
                }
            }

            if (IsMate)
            {
                sb.Append("#");
            }
            else if (CausesCheck || CausesDoubleCheck)
            {
                sb.Append("+");
            }


            return sb.ToString();
        }

        public override string ToString()
        {
            return SmithNotation();
        }

        /// <summary>
        /// Returns a ulong with bits set at the indices this.from and this.to, to be xor'd with the board.
        /// </summary>
        [MethodImpl(Inline)]
        public ulong GetMoveMask()
        {
            return (SquareBB[from] | SquareBB[to]);
        }

        [MethodImpl(Inline)]
        public override bool Equals(object? obj)
        {
            Move other = (Move)obj;
            return (other.from == this.from && other.to == this.to && other.idxChecker == this.idxChecker);
        }

        public static bool operator ==(Move left, Move right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !(left == right);
        }
    }
}
