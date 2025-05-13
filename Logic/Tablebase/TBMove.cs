
using uint8_t = byte;
using uint16_t = ushort;
using uint32_t = uint;
using uint64_t = ulong;

using int8_t = sbyte;
using int16_t = short;
using int32_t = int;
using int64_t = long;

using size_t = ulong;
using unsigned = uint;


using static Lizard.Logic.Tablebase.TBDefs;
using static Lizard.Logic.Tablebase.TBProbe;
using System.Runtime.CompilerServices;
using Lizard.Logic.Data;

namespace Lizard.Logic.Tablebase;

public readonly struct TbMove(uint16_t v)
{
    public static readonly TbMove Zero = new(0);
    public static readonly TbMove MOVE_STALEMATE = new(0xFFFF);
    public static readonly TbMove MOVE_CHECKMATE = new(0xFFFE);

    public readonly uint16_t Value = v;

    public int32_t From => (((Value) >> 6) & 0x3F);
    public int32_t To => ((Value) & 0x3F);
    public int32_t Promotes => (((Value) >> 12) & 0x7);
    public int32_t EnPassant => (((Value) >> 15) & 0x1);


    public static implicit operator ushort(TbMove tbMove) => tbMove.Value;
    public static implicit operator TbMove(ushort s) => new(s);

    public bool Equals(TbMove o) => Value == o.Value;

    public static bool operator ==(TbMove left, TbMove right) => left.Equals(right);
    public static bool operator !=(TbMove left, TbMove right) => !left.Equals(right);


    public static TbMove FromResult(uint res)
    {
        uint retVal = 0;
        retVal = TB_SET_FROM(retVal, TB_GET_FROM(res));
        retVal = TB_SET_TO(retVal, TB_GET_TO(res));
        retVal = TB_SET_PROMOTES(retVal, TB_GET_PROMOTES(res));
        retVal = TB_SET_EP(retVal, TB_GET_EP(res));

        return (TbMove)(retVal >> 4);
    }

    public Move ToMove()
    {
        int flags = 0;
        var p = Promotes + 1;
        flags |= (p == Knight) ? Move.FlagPromoKnight : 0;
        flags |= (p == Bishop) ? Move.FlagPromoBishop : 0;
        flags |= (p == Rook) ? Move.FlagPromoRook : 0;
        flags |= (p == Queen) ? Move.FlagPromoQueen : 0;
        flags |= (EnPassant != EPNone) ? Move.FlagEnPassant : 0;

        return new Move(From, To, flags);
    }

    public override string ToString() => ToMove().ToString();
}

[InlineArray(TB_MAX_PLY)] public struct TbRootMovePvBuffer { TbMove _; }

public unsafe struct TbRootMove
{
    public TbMove move;
    public TbRootMovePvBuffer pv;
    public unsigned pvSize;
    public int32_t tbScore, tbRank;

    public override string ToString() => $"{move}\ttbScore {tbScore}\ttbRank {tbRank}";
}

[InlineArray(TB_MAX_MOVES)] public struct TbRootMovesBuffer { TbRootMove _; }

public struct TbRootMoves
{
    public unsigned size;
    public TbRootMovesBuffer moves;
}


public readonly struct RootProbeMove(uint data)
{
    public readonly uint Data = data;

    public uint From => TB_GET_FROM(Data);
    public uint To => TB_GET_TO(Data);
    public uint PromotionTo => TB_GET_PROMOTES(Data);
    public bool EnPassant => TB_GET_EP(Data) != 0;
    public uint WDL => TB_GET_WDL(Data);
    public uint DTZ => TB_GET_DTZ(Data);

    public TbMove ResultMove => TbMove.FromResult(Data);

    public override string ToString()
    {
        return $"{ResultMove}: {GetWDLResult((uint)WDL)} {DTZ}";
    }
}
