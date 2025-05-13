
using uint8_t = byte;
using uint16_t = ushort;
using uint64_t = ulong;
using size_t = ulong;

using System.Runtime.InteropServices;
using static Lizard.Logic.Tablebase.TBDefs;

namespace Lizard.Logic.Tablebase;

public unsafe struct PairsData
{
    public uint8_t* indexTable;
    public uint16_t* sizeTable;
    public uint8_t* data;
    public uint16_t* offset;
    public uint8_t* symLen;
    public uint8_t* symPat;
    public uint8_t blockSize;
    public uint8_t idxBits;
    public uint8_t minLen;
    public fixed uint8_t constValue[2];

    /// <summary>
    /// This slice actually covers more than 10 or so uint64_t's.
    /// The memory for PairsData is allocated with sufficient size for the preceding members,
    /// plus however many additional uint64_t
    /// </summary>
    public fixed uint64_t dataSlice[1];
};


public unsafe struct EncInfo
{
    public PairsData* precomp;
    public fixed size_t factor[TB_PIECES];
    public fixed uint8_t pieces[TB_PIECES];
    public fixed uint8_t norm[TB_PIECES];

    //  Works, but new Span<byte>(...) doesn't...
    public Span<byte> pieceSpan => MemoryMarshal.CreateSpan(ref pieces[0], TB_PIECES);
};

public unsafe abstract class BaseEntry
{
    public uint64_t key;
    public uint8_t*[] data = new uint8_t*[3];
    public object[] mapping = new object[3];
    public bool[] ready = new bool[3];
    public uint8_t num;
    public bool symmetric, hasPawns, hasDtm, hasDtz;
    public bool kk_enc;
    public uint8_t pawns0;
    public uint8_t pawns1;
    public bool dtmLossOnly;
    public abstract Span<EncInfo> first_ei(int type);
    public int num_tables(int type) => (hasPawns ? type == TBDefs.DTM ? 6 : 4 : 1);
};

public unsafe class PieceEntry : BaseEntry
{
    public EncInfo[] ei = new EncInfo[2 + 2 + 1];
    public uint16_t* dtmMap;
    public uint16_t[,,] dtmMapIdx = new uint16_t[1, 2, 2];
    public void* dtzMap;
    public uint16_t[,] dtzMapIdx = new uint16_t[1, 4];
    public uint8_t[] dtzFlags = new uint8_t[1];

    public PieceEntry() { mapping = [new(), new(), new()]; }

    public override Span<EncInfo> first_ei(int type)
    {
        int start = type == TBDefs.WDL ? 0 : type == DTM ? 2 : 4;
        return new Span<EncInfo>(ei, start, 5 - start);
    }
};

public unsafe class PawnEntry : BaseEntry
{
    public EncInfo[] ei = new EncInfo[4 * 2 + 6 * 2 + 4];
    public uint16_t* dtmMap;
    public uint16_t[,,] dtmMapIdx = new uint16_t[6, 2, 2];
    public void* dtzMap;
    public uint16_t[,] dtzMapIdx = new uint16_t[4, 4];
    public uint8_t[] dtzFlags = new uint8_t[4];
    public bool dtmSwitched;

    public PawnEntry() { mapping = [new(), new(), new()]; }

    public override Span<EncInfo> first_ei(int type)
    {
        int start = type == TBDefs.WDL ? 0 : type == DTM ? 8 : 20;
        return new Span<EncInfo>(ei, start, 24 - start);
    }
};

public unsafe struct TbHashEntry
{
    public uint64_t key;
    public BaseEntry ptr;
    public bool error;
};
