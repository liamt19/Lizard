using static Lizard.Logic.Tablebase.TBProbeHeader;

namespace Lizard.Logic.Tablebase
{
    public readonly struct RootProbeMove(uint data)
    {
        public readonly uint Data = data;

        public int From => TB_GET_FROM((int)Data);
        public int To => TB_GET_TO((int)Data);
        public int PromotionTo => TB_GET_PROMOTES((int)Data);
        public bool EnPassant => TB_GET_EP((int)Data) != 0;
        public int WDL => TB_GET_WDL((int)Data);
        public uint DTZ => TB_GET_DTZ(Data);
        
        public TbMove ResultMove => TbMove.FromResult(Data);

        public override string ToString()
        {
            return $"{ResultMove}: {GetWDLResult((uint)WDL)} {DTZ}";
        }
    }
}
