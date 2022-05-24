using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LTChess.Search
{
    /// <summary>
    /// TODO: This doesn't work yet.
    /// </summary>
    public static class Mate
    {
        public const int MATE_NONE = -1;
        public const int MATE_DRAW = -2;
        public const int MATE_SELF = -3;
        public const int MATE_OUT_DEPTH = -4;

        private static int StartColor;
        private static Position p;
        private static int maxDepth;

        public static void Search(Position pos, int depth = 6)
        {
            p = pos;
            maxDepth = depth;
            StartColor = p.ToMove;

            Log("Looking for mates at or below depth " + depth + " for " + ColorToString(StartColor));
            int result = Deepen(0);
            Log("In Search, Deepen returned " + result);
        }

        private static int Deepen(int currDepth)
        {
            Span<Move> list = stackalloc Move[NORMAL_CAPACITY];
            int size = GenAllLegalMoves(p, list);

            if (size == 0)
            {
                if (p.CheckInfo.InCheck || p.CheckInfo.InDoubleCheck)
                {
                    if (p.ToMove != StartColor)
                    {
                        return currDepth;
                    }
                    else
                    {
                        return MATE_SELF;
                    }
                }
                else
                {
                    return MATE_DRAW;
                }
            }

            Span<int> results = stackalloc int[size];
            int movesThatDontMate = 0;

            int fastestMate = MAX_DEPTH;
            int fastestIndex = 0;

            int slowestMate = MATE_NONE;
            int slowestIndex = 0;

            for (int i = 0; i < size; i++)
            {
                //Log("".Indent(currDepth) + "Move " + list[i].ToString(p));

                p.MakeMove(list[i]);

                int thisResult;
                if (currDepth < maxDepth)
                {
                    int nextDepth = (p.ToMove != StartColor) ? currDepth + 1 : currDepth;
                    thisResult = Deepen(nextDepth);
                }
                else
                {
                    thisResult = MATE_OUT_DEPTH;
                }

                results[i] = thisResult;
                if (results[i] <= 0)
                {
                    movesThatDontMate++;
                }
                else
                {
                    if (thisResult < fastestMate)
                    {
                        fastestMate = thisResult;
                        fastestIndex = i;
                    }
                    if (thisResult > slowestMate)
                    {
                        slowestMate = thisResult;
                        slowestIndex = i;
                    }
                }

                p.UnmakeMove();
            }

            if (p.ToMove == StartColor)
            {
                if (fastestMate != MAX_DEPTH)
                {
                    Log("".Indent(currDepth) + ColorToString(StartColor) + " has mate in " + fastestMate + " with " + list[fastestIndex].ToString(p) + "\r\n");
                    //Log("".Indent(currDepth) + ColorToString(StartColor) + " has mate in " + fastestMate + " with " + list[fastestIndex].ToString(p));
                    return fastestMate;
                }
                else
                {
                    //Log("".Indent(currDepth) + ColorToString(StartColor) + " at depth " + currDepth + " doesn't have a mate");
                    return MATE_NONE;
                }
            }
            else
            {
                if (slowestMate > MATE_NONE)
                {
                    //Log("".Indent(currDepth) + ColorToString(p.ToMove) + " at depth " + currDepth + " is getting mated in " + slowestMate + " if they play " + list[slowestIndex].ToString(p));
                    return slowestMate;
                }
                else
                {
                    //Log("".Indent(currDepth) + ColorToString(p.ToMove) + " at depth " + currDepth + " isn't being mated");
                    return MATE_NONE;
                }
            }
        }
    }
}
