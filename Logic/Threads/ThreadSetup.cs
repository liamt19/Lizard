namespace LTChess.Logic.Threads
{
    public class ThreadSetup
    {
        public string StartFEN;
        public List<Move> SetupMoves;


        public ThreadSetup(List<Move> setupMoves) : this(InitialFEN, setupMoves) { }
        public ThreadSetup(string fen = InitialFEN) : this(fen, new List<Move>()) { }

        public ThreadSetup(string fen, List<Move> setupMoves)
        {
            this.StartFEN = fen;
            this.SetupMoves = setupMoves;
        }
    }
}
