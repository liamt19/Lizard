namespace Lizard.Logic.Search
{
    public static unsafe class WDL
    {
        private static ReadOnlySpan<double> Mat_As => [-140.14947546, 473.31880899, -605.86369938, 524.99731145];
        private static ReadOnlySpan<double> Mat_Bs => [-28.43262875, 101.83936924, -80.27661412, 94.27337419];
        public static (int win, int loss) MaterialModel(int score, int mat)
        {
            double m = Math.Clamp(mat, 17, 78) / 48.0;
            double x = Math.Clamp(score, -4000, 4000);

            double a = (((Mat_As[0] * m + Mat_As[1]) * m + Mat_As[2]) * m) + Mat_As[3];
            double b = (((Mat_Bs[0] * m + Mat_Bs[1]) * m + Mat_Bs[2]) * m) + Mat_Bs[3];

            int win  = (int)double.Round(1000.0 / (1 + double.Exp((a - x) / b)));
            int loss = (int)double.Round(1000.0 / (1 + double.Exp((a + x) / b)));

            return (win, loss);
        }

        public static (int win, int loss) MaterialModel(int score, Position pos)
        {
            ref Bitboard bb = ref pos.bb;
            var mat = popcount(bb.Pieces[Queen])  * 9 +
                      popcount(bb.Pieces[Rook])   * 5 +
                      popcount(bb.Pieces[Bishop]) * 3 +
                      popcount(bb.Pieces[Knight]) * 3 +
                      popcount(bb.Pieces[Pawn])   * 1;
            return MaterialModel(score, (int)mat);
        }


        private static ReadOnlySpan<double> Ply_As => [10.96887666, -62.17283318, 251.25333664, 172.65209925];
        private static ReadOnlySpan<double> Ply_Bs => [-23.34690728, 98.46140259, -111.32704842, 117.15171803];

        public static (int win, int loss) PlyModel(int score, int ply)
        {
            double m = Math.Clamp(ply, 8, 160) / 64.0;
            double x = Math.Clamp(score, -4000, 4000);

            double a = (((Ply_As[0] * m + Ply_As[1]) * m + Ply_As[2]) * m) + Ply_As[3];
            double b = (((Ply_Bs[0] * m + Ply_Bs[1]) * m + Ply_Bs[2]) * m) + Ply_Bs[3];

            int win  = (int)double.Round(1000.0 / (1 + double.Exp((a - x) / b)));
            int loss = (int)double.Round(1000.0 / (1 + double.Exp((a + x) / b)));

            return (win, loss);
        }

        public static (int win, int loss) PlyModel(int score, Position pos)
        {
            int ply = pos.FullMoves * 2 - Not(pos.ToMove) - 1;
            return PlyModel(score, ply);
        }
    }
}
