<h1 align="center">
LTChess - A C# chess engine
</h1>

<h2 align="center">
<img src="./Resources/logo.png" width="500">
</h2>

## Info
Creating this in my spare time, mainly using it to learn more about optimization and computer games. 
I'm uploading it here so I can keep backups of it and not lose it when my laptop finally dies.


## Features
### NNUE Evaluation:
Version 9.3 uses a 768 -> 1024 -> 1 neural network to evaluate positions, which was trained on 1.5 billion positions of [an Lc0 dataset](https://drive.google.com/file/d/1RFkQES3DpsiJqsOtUshENtzPfFgUmEff/view) using [Bullet](https://github.com/jw1912/bullet).

In addition, this engine can use [Stockfish neural networks](https://tests.stockfishchess.org/nns) created for their [SFNNv6/7/8 architectures](https://github.com/official-stockfish/Stockfish/commit/c1fff71650e2f8bf5a2d63bdc043161cdfe8e460), a diagram of which is available [here](https://raw.githubusercontent.com/official-stockfish/nnue-pytorch/master/docs/img/SFNNv6_architecture_detailed.svg).



### Other things:
  - [Aspiration Windows](https://www.chessprogramming.org/Aspiration_Windows)
  - [Futility Pruning](https://www.chessprogramming.org/Futility_Pruning)
  - [Delta Pruning](https://www.chessprogramming.org/Delta_Pruning)
  - [Late Move Reductions](https://www.chessprogramming.org/Late_Move_Reductions)
  - [Null Move Pruning](https://www.chessprogramming.org/Null_Move_Pruning)
  - [Late Move Pruning](https://www.chessprogramming.org/Futility_Pruning#MoveCountBasedPruning)
  - [Reverse Futility Pruning](https://www.chessprogramming.org/Reverse_Futility_Pruning)
  - [Killer Heuristic](https://www.chessprogramming.org/Killer_Heuristic)
  - [History Heuristic](https://www.chessprogramming.org/History_Heuristic)

## Status
Version 9.3 uses its own NNUE evaluation, and began proper parameter testing with [SPRT](https://en.wikipedia.org/wiki/Sequential_probability_ratio_test).

Currently rated a bit above 2600 bullet/blitz on [Lichess](https://lichess.org/@/LTChessBot).

## Some spotty history:
#### Version 9.1:
Some major speed improvements to both searches and move generation.
It was rated a bit above 2500 bullet/blitz on Lichess.

#### Version 8.4:
A decent rating increase, and a lot fewer "dumb" moves. 
Many of the commits between 8.0 and 8.4 improved some of the early architectural decisions, and it is now far easier to debug and improve the code. 
It was rated a bit above 2400 bullet/blitz on Lichess.

#### Version 7.0:
A large rating increase (around 250) and was far more polished. 
It was rated a bit above 2000 bullet on Lichess.



## Contributing
If you have any ideas or comments, feel free to create an issue or pull request!
