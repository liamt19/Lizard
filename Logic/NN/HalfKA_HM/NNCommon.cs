


using System.Text;

namespace LTChess.Logic.NN.HalfKA_HM
{
    public static class NNCommon
    {
        public const int OutputScale = 16;
        public const int WeightScaleBits = 6;

        public const int CacheLineSize = 64;

        public const int PSQTBuckets = 8;
        public const int LayerStacks = 8;

        public const int SimdWidth = 32;
        public const int MaxSimdWidth = 32;


        /// <summary>
        /// Rounds <paramref name="n"/> up to be a multiple of <paramref name="numBase"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToMultiple(short n, short numBase)
        {
            return (n + numBase - 1) / numBase * numBase;
        }

        //  https://stackoverflow.com/questions/19497765/equivalent-of-cs-reinterpret-cast-in-c-sharp
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TDest reinterpret_cast<TSource, TDest>(TSource source)
        {
            var sourceRef = __makeref(source);
            var dest = default(TDest);
            var destRef = __makeref(dest);
            *(IntPtr*)&destRef = *(IntPtr*)&sourceRef;
            return __refvalue(destRef, TDest);
        }

        public static void format_cp_compact(int v, StringBuilder buffer)
        {

            buffer[0] = (v < 0 ? '-' : v > 0 ? '+' : ' ');

            int cp = Math.Abs(100 * v / EvaluationConstants.ValuePawn);
            if (cp >= 10000)
            {
                buffer.Append((char) ('0' + cp / 10000)); cp %= 10000;
                buffer.Append((char) ('0' + cp / 1000)); cp %= 1000;
                buffer.Append((char) ('0' + cp / 100)); cp %= 100;
                buffer.Append((char) (' '));
            }
            else if (cp >= 1000)
            {
                buffer.Append((char) ('0' + cp / 1000)); cp %= 1000;
                buffer.Append((char) ('0' + cp / 100)); cp %= 100;
                buffer.Append((char) ('.'));
                buffer.Append((char) ('0' + cp / 10));
            }
            else
            {
                buffer.Append((char) ('0' + cp / 100)); cp %= 100;
                buffer.Append((char) ('.'));
                buffer.Append((char) ('0' + cp / 10)); cp %= 10;
                buffer.Append((char) ('0' + cp / 1));
            }
        }

        public unsafe static void format_cp_ptr(int v, char* buffer)
        {
            buffer[0] = (v < 0 ? '-' : v > 0 ? '+' : ' ');

            int cp = Math.Abs(100 * v / EvaluationConstants.ValuePawn);

            if (cp >= 10000)
            {
                buffer[1] = (char) ('0' + cp / 10000); cp %= 10000;
                buffer[2] = (char) ('0' + cp / 1000); cp %= 1000;
                buffer[3] = (char) ('0' + cp / 100); cp %= 100;
                buffer[4] = (char) (' ');
            }
            else if (cp >= 1000)
            {
                buffer[1] = (char) ('0' + cp / 1000); cp %= 1000;
                buffer[2] = (char) ('0' + cp / 100); cp %= 100;
                buffer[3] = (char) ('.');
                buffer[4] = (char) ('0' + cp / 10);
            }
            else
            {
                buffer[1] = (char) ('0' + cp / 100); cp %= 100;
                buffer[2] = (char) ('.');
                buffer[3] = (char) ('0' + cp / 10); cp %= 10;
                buffer[4] = (char) ('0' + cp / 1);
            }
        }


        private enum PieceType
        {
            NO_PIECE_TYPE, PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING,
            ALL_PIECES = 0,
            PIECE_TYPE_NB = 8
        };

        private enum Piece
        {
            NO_PIECE,
            W_PAWN = PieceType.PAWN, W_KNIGHT, W_BISHOP, W_ROOK, W_QUEEN, W_KING,
            B_PAWN = PieceType.PAWN + 8, B_KNIGHT, B_BISHOP, B_ROOK, B_QUEEN, B_KING,
            PIECE_NB = 16
        };
    }

    public struct ExtPieceSquare
    {
        public uint[] from = new uint[2];

        public ExtPieceSquare(uint a, uint b)
        {
            from[0] = a; 
            from[1] = b;
        }

        public uint this[int i]
        {
            get
            {
                return from[i];
            }
        }
    }
}
