using static Lizard.Logic.Tablebase.TBProbeHeader;

namespace Lizard.Logic.Tablebase
{
    public readonly struct TbMove(ushort data)
    {
        public readonly ushort Data = data;

        public int From => TB_MOVE_FROM(this);
        public int To => TB_MOVE_TO(this);
        public int PromotionTo => FathomPromoToLizard(TB_MOVE_PROMOTES(this));
        public int EnPassant => TB_MOVE_EP(this);

        public static implicit operator ushort(TbMove tbMove) => tbMove.Data;
        public static implicit operator TbMove(ushort s) => new(s);

        public Move ToMove()
        {
            Move m = new Move();
            m.SetNew(From, To);
            if (PromotionTo != None)
            {
                m.PromotionTo = PromotionTo;
                m.Promotion = true;
            }

            if (EnPassant != EPNone)
            {
                m.EnPassant = true;
            }

            return m;
        }

        public static TbMove FromResult(uint _res) => FromResult((int)_res);
        public static TbMove FromResult(int res)
        {
            uint retVal = 0;

            retVal = TB_SET_FROM(retVal, TB_GET_FROM(res));
            retVal = TB_SET_TO(retVal, TB_GET_TO(res));
            retVal = TB_SET_PROMOTES(retVal, TB_GET_PROMOTES(res));
            retVal = TB_SET_EP(retVal, TB_GET_EP(res));

            return (ushort)(retVal >> 4);
        }

        public override string ToString()
        {
            return ToMove().ToString();
        }
    }
}
