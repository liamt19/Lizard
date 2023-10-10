# LTChess - A C# chess engine


## Info
Creating this in my spare time, mainly using it to learn more about optimization and computer games. 
I'm uploading it here so I can keep backups of it and not lose it when my laptop finally dies.


## Features
### NNUE Evaluation:
Version 9.1 currently supports [Stockfish neural networks](https://tests.stockfishchess.org/nns) created for their [SFNNv6 architecture](https://github.com/official-stockfish/Stockfish/commit/c1fff71650e2f8bf5a2d63bdc043161cdfe8e460), a diagram of which is available [here](https://raw.githubusercontent.com/official-stockfish/nnue-pytorch/master/docs/img/SFNNv6_architecture_detailed.svg).

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
Version 9.1 has some major speed improvements to both searches and move generation.
Currently rated a bit above 2500 bullet/blitz on [Lichess](https://lichess.org/@/LTChessBot).

## Some spotty history:
#### Version 8.4:
A decent rating increase, and a lot fewer "dumb" moves. 
Many of the commits between 8.0 and 8.4 improved some of the early architectural decisions, and it is now far easier to debug and improve the code. 
It was rated a bit above 2400 bullet/blitz on Lichess.

#### Version 7.0:
A large rating increase (around 250) and was far more polished. 
It was rated a bit above 2000 bullet on Lichess.



## Contributing
If you have any ideas or comments, feel free to create an issue or pull request!
