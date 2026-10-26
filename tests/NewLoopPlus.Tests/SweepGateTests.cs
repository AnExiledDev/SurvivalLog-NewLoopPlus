using System.Collections.Generic;
using System.Linq;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>A table sweep reads every row through the game's runtime, so it is skipped while the table is as this mod left it.</summary>
public class SweepGateTests
{
    private static readonly Dictionary<int, int> Table = new() { [1] = 20, [2] = 40, [3] = 7 };

    private static int? Read(int id) => Table.TryGetValue(id, out var value) ? value : null;

    private static SweepGate<int, int> SweptGate()
    {
        var gate = new SweepGate<int, int>();

        gate.Swept(100, 3, new[] { (1, 20), (2, 40) });

        return gate;
    }

    [Fact]
    public void A_table_never_swept_is_due()
    {
        Assert.True(new SweepGate<int, int>().IsDue(100, 3, Read));
    }

    [Fact]
    public void The_same_table_still_holding_what_was_written_is_skipped()
    {
        Assert.False(SweptGate().IsDue(100, 3, Read));
    }

    [Fact]
    public void A_replaced_or_resized_table_is_due()
    {
        var gate = SweptGate();

        Assert.True(gate.IsDue(200, 3, Read));
        Assert.True(gate.IsDue(100, 4, Read));
    }

    [Fact]
    public void A_watched_row_that_lost_its_value_in_place_is_due()
    {
        var gate = SweptGate();

        Assert.True(gate.IsDue(100, 3, id => id == 2 ? 10 : Read(id)));
    }

    [Fact]
    public void A_watched_row_that_cannot_be_read_is_due()
    {
        var gate = SweptGate();

        Assert.True(gate.IsDue(100, 3, id => id == 1 ? null : Read(id)));
    }

    [Fact]
    public void Forgetting_makes_the_next_check_sweep()
    {
        var gate = SweptGate();

        gate.Forget();

        Assert.True(gate.IsDue(100, 3, Read));
    }

    [Fact]
    public void Many_changed_rows_are_watched_from_both_ends_of_the_table()
    {
        var gate = new SweepGate<int, int>();
        var rows = Enumerable.Range(1, 100).Select(id => (id, id * 2)).ToList();
        var asked = new HashSet<int>();

        gate.Swept(100, 100, rows);
        gate.IsDue(100, 100, id => { asked.Add(id); return id * 2; });

        Assert.Equal(SweepGate<int, int>.WatchCount, asked.Count);
        Assert.Contains(1, asked);
        Assert.Contains(100, asked);
        Assert.True(gate.IsDue(100, 100, id => id == 100 ? 0 : id * 2));
    }
}
