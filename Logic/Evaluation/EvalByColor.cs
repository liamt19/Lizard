namespace LTChess.Search
{
    public struct EvalByColor
    {
        public double white = 0;
        public double black = 0;

        public double Total => white - black;

        public EvalByColor()
        {

        }

        [MethodImpl(Inline)]
        public void Clear()
        {
            white = 0;
            black = 0;
        }

        [MethodImpl(Inline)]
        public void Scale(double scale)
        {
            white *= scale;
            black *= scale;
        }
    }
}
