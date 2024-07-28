using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Lizard.Logic.Datagen.DatagenMatch;

namespace Lizard.Logic.Datagen
{
    public interface TOutputFormat
    {
        public int Score { get; set; }
        public GameResult Result { get; set; }
        public string GetWritableData();
        public void Fill(Position pos, int score);

        public static string ResultToMarlin(GameResult gr)
        {
            return gr switch
            {
                GameResult.WhiteWin => "1.0",
                GameResult.Draw => "0.5",
                GameResult.BlackWin => "0.0",
            };
        }
    }


}
