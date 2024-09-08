namespace Lizard.Logic.Threads
{
    public class ThreadSetup
    {
        public string StartFEN;
        public List<Move> SetupMoves;
        public List<Move> UCISearchMoves;

        public ThreadSetup(List<Move> setupMoves) : this(InitialFEN, setupMoves, new List<Move>()) { }
        public ThreadSetup(string fen = InitialFEN) : this(fen, new List<Move>(), new List<Move>()) { }

        public ThreadSetup(string fen, List<Move> setupMoves, List<Move> uciSearchMoves)
        {
            this.StartFEN = fen;
            this.SetupMoves = setupMoves;
            this.UCISearchMoves = uciSearchMoves;
        }
    }
}
