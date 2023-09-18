# LTChess - A C# chess engine


## Info
Creating this in my spare time, mainly using it to learn more about optimization and computer games. 
I'm uploading it here so I can keep backups of it and not lose it when my laptop finally dies.


## Features
### NNUE Evaluation:
Version 8.4 currently supports [Stockfish neural networks](https://tests.stockfishchess.org/nns) created for their [SFNNv5 architecture](https://github.com/official-stockfish/Stockfish/commit/c079acc26f93acc2eda08c7218c60559854f52f0), a diagram of which is available [here](https://github.com/official-stockfish/nnue-pytorch/blob/7d8770316238314e77a0209ae81d74f573dcae74/docs/img/SFNNv5_architecture_detailed.svg).

In the near future I want to train my own network with a similar architecture. The use of Stockfish's networks was meant to make improving move searching easier since I didn't have to rely on my fairly poor classical evaluation.


### Other things:
  - [Aspiration Windows](https://www.chessprogramming.org/Aspiration_Windows)
  - [Futility Pruning](https://www.chessprogramming.org/Futility_Pruning)
  - [Delta Pruning](https://www.chessprogramming.org/Delta_Pruning)
  - [Late Move Reductions](https://www.chessprogramming.org/Late_Move_Reductions)
  - [Null Move Pruning](https://www.chessprogramming.org/Null_Move_Pruning).
  - [Late Move Pruning](https://www.chessprogramming.org/Futility_Pruning#MoveCountBasedPruning)
  - [Reverse Futility Pruning](https://www.chessprogramming.org/Reverse_Futility_Pruning)
  - [Razoring](https://www.chessprogramming.org/Razoring)
  - [Killer Heuristic](https://www.chessprogramming.org/Killer_Heuristic)
  - [History Heuristic](https://www.chessprogramming.org/History_Heuristic)

## Status
Version 8.4 brings a decent rating increase, and makes a lot fewer "dumb" moves.
Many of the recent commits improved some of my early architectural decisions, and it is now far easier to debug and improve this engine's speed and playing strength.

Currently rated a bit above 2400 bullet/blitz on [Lichess](https://lichess.org/@/LTChessBot).

### Contributing
If you have any ideas or comments, feel free to create an issue or pull request!
