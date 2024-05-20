
/*

Translated from C to C# based on https://github.com/jdart1/Fathom, which uses the MIT license:

The MIT License (MIT)

Copyright (c) 2013-2018 Ronald de Man
Copyright (c) 2015 basil00
Copyright (c) 2016-2023 by Jon Dart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using uint8_t = byte;
using uint64_t = ulong;

namespace Lizard.Logic.Tablebase
{
    public struct Pos
    {
        public uint64_t white;
        public uint64_t black;

        public uint64_t kings;
        public uint64_t queens;
        public uint64_t rooks;
        public uint64_t bishops;
        public uint64_t knights;
        public uint64_t pawns;

        public uint8_t rule50;
        public uint8_t ep;

        public bool turn;

        public Pos(ulong white, ulong black, ulong kings, ulong queens, ulong rooks, ulong bishops, ulong knights, ulong pawns, byte rule50, byte ep, bool turn)
        {
            this.white = white;
            this.black = black;
            this.kings = kings;
            this.queens = queens;
            this.rooks = rooks;
            this.bishops = bishops;
            this.knights = knights;
            this.pawns = pawns;
            this.rule50 = rule50;
            this.ep = ep;
            this.turn = turn;
        }
    }
}
