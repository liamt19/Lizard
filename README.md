# LTChess
~Mostly~ Completely functional C# chess move generation.

Creating this in my spare time, and mostly using it to learn more about optimization and computer games, and uploading it here so I can keep backups of it and not lose it when my laptop finally dies.

The evaluation isn't very good, but [it was enough to beat the 1600 ELO bot on Chess.com](https://www.chess.com/analysis/game/computer/69606251).
It was also able to get a score of 44 in puzzle rush, though it got a decent amount of forced checkmates which it almost never misses given enough time/depth.

At this point, the move generation is marginally faster than [Cosette](https://github.com/Tearth/Cosette), at least on my computer, but the evaluation is far worse.
