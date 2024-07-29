
namespace Lizard.Logic.Datagen
{
    public unsafe struct PlaintextDataFormat : TOutputFormat
    {
        public const int BufferSize = 92;
        public fixed char FEN[BufferSize];

        public int Score { get; set; }
        public GameResult Result { get; set; }

        public string GetWritableData()
        {
            fixed (char* fen = FEN)
            {
                var fenStr = new string(fen);
                return $"{fenStr} | {Score} | {TOutputFormat.ResultToMarlin(Result)}";
            }
        }

        public void Fill(Position pos, int score)
        {
            var posSpan = pos.GetFEN().AsSpan();

            fixed (char* fen = FEN)
            {
                var fenSpan = new Span<char>(fen, BufferSize);
                fenSpan.Clear();

                posSpan.CopyTo(fenSpan);
            }

            Score = score;
        }
    }
}
