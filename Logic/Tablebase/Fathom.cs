using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lizard.Logic.Tablebase;

public static unsafe class Fathom
{
    public static void ProbeRoot(Position pos)
    {
        RootProbeMove* results = stackalloc RootProbeMove[MoveListSize];
        FathomPos fPos = FathomPos.FromPosition(pos);

        var root = TBProbe.tb_probe_root_impl(fPos, results);
        Log($"root: {root:X}");

        ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
        int size = pos.GenLegal(legal);

        for (int i = 0; i < size; i++)
            Log($"Results[{i}]\t {results[i]}");

        OrderResults(results, size);
        Log("SORTED\n");

        for (int i = 0; i < size; i++)
            Log($"Results[{i}]\t {results[i]}");

    }

    public static void ProbeRootWDL(Position pos)
    {
        TbRootMoves results = new TbRootMoves();
        FathomPos fPos = FathomPos.FromPosition(pos);
        uint castling = (uint)pos.State->CastleStatus;
        bool r50 = false;

        var root = TBProbe.tb_probe_root_wdl(fPos, castling, r50, &results);
        Log($"root: {root:X}");

        ScoredMove* legal = stackalloc ScoredMove[MoveListSize];
        int size = pos.GenLegal(legal);


        var moves = new Span<TbRootMove>(&results.moves, (int)results.size);
        for (int i = 0; i < size; i++)
            Log($"Results[{i}]\t {moves[i]}");

        
        OrderResults(moves, size);
        Log("SORTED\n");

        for (int i = 0; i < size; i++)
            Log($"Results[{i}]\t {moves[i]}");

    }


    public static void OrderResults(RootProbeMove* entries, int size)
    {
        Span<RootProbeMove> span = new(entries, size);
        RootProbeMove[] arr = span.ToArray();

        arr = arr.OrderByDescending(r => r.WDL)
                 .ThenBy(r => r.DTZ)
                 .ToArray();

        arr.CopyTo(span);
    }

    public static void OrderResults(Span<TbRootMove> entries, int size)
    {
        TbRootMove[] arr = entries.ToArray();
        arr = arr.OrderByDescending(r => r.tbScore)
            .ThenBy(r => r.tbRank)
            .ToArray();

        arr.CopyTo(entries);
    }
}
