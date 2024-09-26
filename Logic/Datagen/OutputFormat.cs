namespace Lizard.Logic.Datagen
{
    public interface TOutputFormat
    {
        public int Score { get; set; }
        public GameResult Result { get; set; }
        public void SetResult(GameResult gr);
        public void SetSTM(int stm);
        public string GetWritableTextData();
        public void Fill(Position pos, Move bestMove, int score);

        public static string ResultToMarlin(GameResult gr)
        {
            return gr switch
            {
                GameResult.WhiteWin => "1.0",
                GameResult.Draw => "0.5",
                GameResult.BlackWin => "0.0",
                _ => "0.5",
            };
        }
    }


}
