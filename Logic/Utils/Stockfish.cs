using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;


using static Stockfish.Stockfish;
using static Stockfish.Stockfish.Color;
using static Stockfish.Stockfish.File;
using static Stockfish.Stockfish.Rank;
using static Stockfish.Stockfish.Square;
using static Stockfish.Stockfish.Direction;
using static Stockfish.Stockfish.PieceType;
using System.Runtime.InteropServices;

namespace Stockfish
{
	/// <summary>
	/// Everything in this class is taken from the Stockfish source and modified to work in C#.
	/// I am only using it to generate the attack masks for the MagicBitboards, and eventually I'll do this myself.
	/// </summary>
	public static class Stockfish
	{
		public static ushort[,] SquareDistance = new ushort[64, 64];
		//public static ushort[] PopCnt16 = new ushort[1 << 16];

		public static ulong[] SquareBB = new ulong[64];
		//public static ulong[,] LineBB = new ulong[64, 64];
		//public static ulong[,] LineBB = new ulong[64, 64];
		//public static ulong[,] PseudoAttacks = new ulong[8, 64];
		//public static ulong[,] PawnAttacks = new ulong[2, 64];

		static Stockfish()
		{
			for (int s1 = 0; s1 < 64; s1++)
			{
				SquareBB[s1] = (1UL << s1);
				for (int s2 = 0; s2 < 64; s2++)
				{
					SquareDistance[s1, s2] = (ushort)Math.Max(distanceFile(s1, s2), distanceRank(s1, s2));

				}
			}
		}

		#region consts

		public const ulong DarkSquares = 0xAA55AA55AA55AA55UL;

		public const ulong FileABB = 0x0101010101010101UL;
		public const ulong FileBBB = FileABB << 1;
		public const ulong FileCBB = FileABB << 2;
		public const ulong FileDBB = FileABB << 3;
		public const ulong FileEBB = FileABB << 4;
		public const ulong FileFBB = FileABB << 5;
		public const ulong FileGBB = FileABB << 6;
		public const ulong FileHBB = FileABB << 7;

		public const ulong Rank1BB = 0xFF;
		public const ulong Rank2BB = Rank1BB << (8 * 1);
		public const ulong Rank3BB = Rank1BB << (8 * 2);
		public const ulong Rank4BB = Rank1BB << (8 * 3);
		public const ulong Rank5BB = Rank1BB << (8 * 4);
		public const ulong Rank6BB = Rank1BB << (8 * 5);
		public const ulong Rank7BB = Rank1BB << (8 * 6);
		public const ulong Rank8BB = Rank1BB << (8 * 7);

		#endregion

		#region enums

		public enum Square : int
		{
			SQ_A1, SQ_B1, SQ_C1, SQ_D1, SQ_E1, SQ_F1, SQ_G1, SQ_H1,
			SQ_A2, SQ_B2, SQ_C2, SQ_D2, SQ_E2, SQ_F2, SQ_G2, SQ_H2,
			SQ_A3, SQ_B3, SQ_C3, SQ_D3, SQ_E3, SQ_F3, SQ_G3, SQ_H3,
			SQ_A4, SQ_B4, SQ_C4, SQ_D4, SQ_E4, SQ_F4, SQ_G4, SQ_H4,
			SQ_A5, SQ_B5, SQ_C5, SQ_D5, SQ_E5, SQ_F5, SQ_G5, SQ_H5,
			SQ_A6, SQ_B6, SQ_C6, SQ_D6, SQ_E6, SQ_F6, SQ_G6, SQ_H6,
			SQ_A7, SQ_B7, SQ_C7, SQ_D7, SQ_E7, SQ_F7, SQ_G7, SQ_H7,
			SQ_A8, SQ_B8, SQ_C8, SQ_D8, SQ_E8, SQ_F8, SQ_G8, SQ_H8,
			SQ_NONE,

			SQUARE_ZERO = 0,
			SQUARE_NB = 64
		};

		public enum File : int
		{
			FILE_A, FILE_B, FILE_C, FILE_D, FILE_E, FILE_F, FILE_G, FILE_H, FILE_NB
		};

		public enum Rank : int
		{
			RANK_1, RANK_2, RANK_3, RANK_4, RANK_5, RANK_6, RANK_7, RANK_8, RANK_NB
		};

		public enum Direction : int
		{
			NORTH = 8,
			EAST = 1,
			SOUTH = -NORTH,
			WEST = -EAST,

			NORTH_EAST = NORTH + EAST,
			SOUTH_EAST = SOUTH + EAST,
			SOUTH_WEST = SOUTH + WEST,
			NORTH_WEST = NORTH + WEST
		};

		public enum PieceType
		{
			NO_PIECE_TYPE, PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING,
			ALL_PIECES = 0,
			PIECE_TYPE_NB = 8
		};

		public enum Color
		{
			WHITE, BLACK, COLOR_NB = 2
		};

		#endregion

		public static bool is_ok(Square s)
		{
			return s >= SQ_A1 && s <= SQ_H8;
		}

		public static int file_of(Square square)
		{
			return (int)square & 7;
		}

		public static int file_of(int square) => square & 7;

		public static int rank_of(Square square)
		{
			return (int)square >> 3;
		}

		public static int rank_of(int square) => square >> 3;

		public static ulong square_bb(Square s)
		{
			return SquareBB[(int)s];
		}

		public static int distanceFile(Square x, Square y)
		{
			return Math.Abs(file_of(x) - file_of(y));
		}

		public static int distanceRank(Square x, Square y)
		{
			return Math.Abs(rank_of(x) - rank_of(y));
		}

		public static int distanceSquare(Square x, Square y)
		{
			return SquareDistance[(int)x, (int)y];
		}

		public static int distanceFile(int x, int y)
		{
			return Math.Abs(file_of(x) - file_of(y));
		}

		public static int distanceRank(int x, int y)
		{
			return Math.Abs(rank_of(x) - rank_of(y));
		}

		public static int distanceSquare(int x, int y)
		{
			return SquareDistance[x, y];
		}

		public static ulong safe_destination(Square s, int step)
		{
			Square to = (s + step);
			return is_ok(to) && distanceSquare(s, to) <= 2 ? square_bb(to) : 0UL;
		}

		public static ulong shift(Direction D, ulong b)
		{
			return D == NORTH ? b << 8 : D == SOUTH ? b >> 8
				: D == (int)NORTH + NORTH ? b << 16 : D == (int)SOUTH + SOUTH ? b >> 16
				: D == EAST ? (b & ~FileHBB) << 1 : D == WEST ? (b & ~FileABB) >> 1
				: D == NORTH_EAST ? (b & ~FileHBB) << 9 : D == NORTH_WEST ? (b & ~FileABB) << 7
				: D == SOUTH_EAST ? (b & ~FileHBB) >> 7 : D == SOUTH_WEST ? (b & ~FileABB) >> 9
				: 0;
		}
		public static ulong sliding_attack(PieceType pt, Square sq, ulong occupied)
		{
			ulong attacks = 0;
			Direction[] RookDirections = { NORTH, SOUTH, EAST, WEST };
			Direction[] BishopDirections = { NORTH_EAST, SOUTH_EAST, SOUTH_WEST, NORTH_WEST };

			foreach (int d in (pt == ROOK ? RookDirections : BishopDirections))
			{
				Square s = sq;
				//	   safe_destination(s, d) && !(occupied & square_bb(s))
				while ((safe_destination(s, d) != 0) && ((occupied & square_bb(s)) == 0))
				{
					s += d;
					attacks |= square_bb(s);
				}
			}

			return attacks;
		}

	}
}
