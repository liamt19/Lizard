using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Logic.Search.Ordering
{

    /// <summary>
    /// Records how successful different moves have been in the past by recording that move's
    /// piece type and color, and the square that it is moving to. 
    /// <br></br>
    /// This is a short array with dimensions [12][64], with a size of <inheritdoc cref="ByteSize"/>.
    /// </summary>
    public unsafe struct PieceToHistory
    {
        private short* _History;

        public const short Clamp = 28000;


        private const short FillValue = -50;

        private const int DimX = PieceNB * 2;
        private const int DimY = SquareNB;

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const nuint Length = DimX * DimY;

        /// <summary>
        /// 2 * (<inheritdoc cref="Length"/>) == 1536 bytes
        /// </summary>
        public const nuint ByteSize = sizeof(short) * Length;

        public PieceToHistory() { }

        /// <summary>
        /// Returns the score at the index <paramref name="idx"/>, which should have been calculated with <see cref="GetIndex"/>
        /// </summary>
        public short this[int pc, int pt, int sq]
        {
            get
            {
                int idx = GetIndex(pc, pt, sq);
                return _History[idx];
            }
            set
            {
                int idx = GetIndex(pc, pt, sq);
                _History[idx] = value;
            }
        }

        /// <summary>
        /// Returns the score at the index <paramref name="idx"/>, which should have been calculated with <see cref="GetIndex"/>
        /// </summary>
        public short this[int idx]
        {
            get
            {
                return _History[idx];
            }
            set
            {
                _History[idx] = value;
            }
        }

        /// <summary>
        /// Returns the index of the score in the History array for a piece of color <paramref name="pc"/> 
        /// and type <paramref name="pt"/> moving to the square <paramref name="sq"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public static int GetIndex(int pc, int pt, int sq)
        {
            return (((pt + (PieceNB * pc)) * DimX) + sq);
        }

        /// <summary>
        /// Allocates memory for this instance's array.
        /// </summary>
        public void Alloc()
        {
            _History = (short*) AlignedAllocZeroed(ByteSize, AllocAlignment);
        }

        /// <summary>
        /// Fills this instance's array with the value of <see cref="FillValue"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void Clear()
        {
            NativeMemory.Clear(_History, ByteSize);
            Span<short> span = new Span<short>(_History, (int) Length);
            span.Fill(FillValue);
        }
    }


    /// <summary>
    /// Records the history for a pair of moves.
    /// <br></br>
    /// This is an array of <see cref="PieceToHistory"/> [12][64], with a size of <inheritdoc cref="ByteSize"/>.
    /// </summary>
    public unsafe struct ContinuationHistory
    {
        private PieceToHistory* _History;

        private const int DimX = PieceNB * 2;
        private const int DimY = SquareNB;

        /// <summary>
        /// 12 * 64 == 768 elements
        /// </summary>
        public const nuint Length = DimX * DimY;

        /// <summary>
        /// 8 * (<inheritdoc cref="Length"/>) == 6144 bytes
        /// </summary>
        private const nuint ByteSize = (nuint) (sizeof(ulong) * Length);

        public ContinuationHistory()
        {
            _History = (PieceToHistory*) AlignedAllocZeroed(ByteSize, AllocAlignment);

            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Alloc();
            }
        }



        public PieceToHistory* this[int pc, int pt, int sq]
        {
            get
            {
                int idx = PieceToHistory.GetIndex(pc, pt, sq);

                Debug.Assert(idx >= 0 && idx < (int) Length);
                return &_History[idx];
            }
        }

        public PieceToHistory* this[int idx]
        {
            get
            {
                return &_History[idx];
            }
        }


        [MethodImpl(Inline)]
        public void Clear()
        {
            for (nuint i = 0; i < Length; i++)
            {
                _History[i].Clear();
            }
        }
    }
}
