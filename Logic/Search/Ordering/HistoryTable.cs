


using System.Runtime.InteropServices;

namespace Lizard.Logic.Search.Ordering
{
    public unsafe struct HistoryTable
    {
        /// <summary>
        /// Index using Color * <see cref="MainHistoryPCStride"/> + Move.MoveMask
        /// </summary>
        public readonly short* MainHistory;
        public const int MainHistoryClamp = 16384;
        public const int MainHistoryPCStride = 4096;
        public const int MainHistoryElements = ColorNB * SquareNB * SquareNB;


        public readonly short* CaptureHistory;
        public const int CaptureClamp = 16384;
        public const int CaptureHistoryElements = ColorNB * (PieceNB + 1) * SquareNB * (PieceNB + 1);


        /// <summary>
        /// Index with [inCheck] [Capture]
        /// <para></para>
        /// Continuations[0][0] is the PieceToHistory[][] for a non-capture while we aren't in check,
        /// and that PieceToHistory[0, 1, 2] is the correct PieceToHistory for a white (0) knight (1) moving to C1 (2).
        /// This is then used by <see cref="MoveOrdering"/>.AssignScores
        /// </summary>
        public readonly ContinuationHistory** Continuations;

        public HistoryTable()
        {
            MainHistory = (short*)AlignedAllocZeroed(sizeof(short) * MainHistoryElements, AllocAlignment);
            CaptureHistory = (short*)AlignedAllocZeroed(sizeof(short) * CaptureHistoryElements, AllocAlignment);

            //  5D arrays aren't real, they can't hurt you.
            //  5D arrays:
            Continuations = (ContinuationHistory**)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2), AllocAlignment);
            ContinuationHistory* cont0 = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2), AllocAlignment);
            ContinuationHistory* cont1 = (ContinuationHistory*)AlignedAllocZeroed((nuint)(sizeof(ContinuationHistory*) * 2), AllocAlignment);

            cont0[0] = new ContinuationHistory();
            cont0[1] = new ContinuationHistory();

            cont1[0] = new ContinuationHistory();
            cont1[1] = new ContinuationHistory();

            Continuations[0] = cont0;
            Continuations[1] = cont1;
        }

        public void Dispose()
        {
            NativeMemory.AlignedFree(MainHistory);
            NativeMemory.AlignedFree(CaptureHistory);

            for (int i = 0; i < 2; i++)
            {
                Continuations[i][0].Dispose();
                Continuations[i][1].Dispose();
            }

        }


        /// <summary>
        /// Applies the <paramref name="bonus"/> to the score at the specified <paramref name="index"/> in the short* <paramref name="field"/>.
        /// This <paramref name="bonus"/> is clamped by the value of <paramref name="clamp"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void ApplyBonus(short* field, int index, int bonus, int clamp)
        {
            field[index] += (short)(bonus - (field[index] * Math.Abs(bonus) / clamp));
        }

        [MethodImpl(Inline)]
        public static int HistoryIndex(int pc, Move m)
        {
            if (EnableAssertions)
            {
                Assert(((pc * MainHistoryPCStride) + m.MoveMask) is >= 0 and < MainHistoryElements,
                    "HistoryIndex(" + pc + ", " + m.MoveMask + ") is OOB! (should be 0 <= idx < " + MainHistoryElements + ")");
            }
            return (pc * MainHistoryPCStride) + m.MoveMask;
        }

        /// <summary>
        /// Returns the index of the score that should be applied to a piece of type <paramref name="pt"/> and color <paramref name="pc"/> 
        /// capturing a piece of type <paramref name="capturedPt"/> on the square <paramref name="toSquare"/>.
        /// <br></br>
        /// This just calculates the flattened 3D array index for 
        /// <see cref="CaptureHistory"/>[<paramref name="pt"/> + <paramref name="pc"/>][<paramref name="toSquare"/>][<paramref name="capturedPt"/>].
        /// </summary>
        [MethodImpl(Inline)]
        public static int CapIndex(int pc, int pt, int toSquare, int capturedPt)
        {
            const int xMax = (PieceNB + 1) * ColorNB;
            const int yMax = SquareNB;
            const int zMax = PieceNB;

            int x = pt + ((PieceNB + 1) * pc);
            int y = toSquare;
            int z = capturedPt;

            if (EnableAssertions)
            {
                int idx = (z * xMax * yMax) + (y * xMax) + x;
                Assert(idx is >= 0 and < CaptureHistoryElements,
                    "CapIndex(" + pt + ", " + pc + ", " + toSquare + ", " + capturedPt + ") == " + idx + " is OOB! (should be 0 <= idx < " + CaptureHistoryElements + ")");
            }

            return (z * xMax * yMax) + (y * xMax) + x;
        }
    }

    /// <summary>
    /// Records how successful different moves have been in the past by recording that move's
    /// piece type and color, and the square that it is moving to. 
    /// <br></br>
    /// This is a short array with dimensions [12][64], with a size of <inheritdoc cref="ByteSize"/>.
    /// </summary>
    public unsafe struct PieceToHistory
    {
        private short* _History;

        public const short Clamp = 16384;


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

        public void Dispose()
        {
            NativeMemory.AlignedFree(_History);
        }

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
            if (EnableAssertions)
            {
                Assert((((pt + (PieceNB * pc)) * DimY) + sq) is >= 0 and < (int)Length,
                    "GetIndex(" + pc + ", " + pt + ", " + sq + ") is OOB! (should be 0 <= idx < " + Length + ")");
            }

            return ((pt + (PieceNB * pc)) * DimY) + sq;
        }

        /// <summary>
        /// Allocates memory for this instance's array.
        /// </summary>
        public void Alloc()
        {
            _History = (short*)AlignedAllocZeroed(ByteSize, AllocAlignment);
        }

        /// <summary>
        /// Fills this instance's array with the value of <see cref="FillValue"/>.
        /// </summary>
        [MethodImpl(Inline)]
        public void Clear()
        {
            NativeMemory.Clear(_History, ByteSize);
            Span<short> span = new Span<short>(_History, (int)Length);
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
        private const nuint ByteSize = (nuint)(sizeof(ulong) * Length);

        public ContinuationHistory()
        {
            _History = (PieceToHistory*)AlignedAllocZeroed(ByteSize, AllocAlignment);

            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Alloc();
            }
        }

        public void Dispose()
        {
            for (nuint i = 0; i < Length; i++)
            {
                (_History + i)->Dispose();
            }

            NativeMemory.AlignedFree(_History);
        }


        public PieceToHistory* this[int pc, int pt, int sq]
        {
            get
            {
                int idx = PieceToHistory.GetIndex(pc, pt, sq);
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
