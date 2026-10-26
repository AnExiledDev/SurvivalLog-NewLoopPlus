using System;
using System.Collections.Generic;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>
/// Decides when a game table needs sweeping again. A sweep reads every row through the game's runtime, so it runs
/// only when the table was replaced (the game loads a fresh table object), its row count changed, or one of a few
/// watched rows no longer holds the value this mod wrote. Only rows this mod changed are watched: a reload puts the
/// game's own value back, which only shows on a row whose value this mod changed.
/// </summary>
public sealed class SweepGate<TKey, TValue>
    where TValue : struct, IEquatable<TValue>
{
    public const int WatchCount = 8;

    private (long Table, int Count)? _swept;
    private IReadOnlyList<(TKey Key, TValue Written)> _watched = Array.Empty<(TKey, TValue)>();

    /// <summary>True when the table must be swept: never swept, replaced, resized, or a watched row lost its value. A row that cannot be read reads as null.</summary>
    public bool IsDue(long table, int count, Func<TKey, TValue?> read) =>
        _swept != (table, count) || _watched.Any(row => read(row.Key) is not { } now || !now.Equals(row.Written));

    /// <summary>Notes a full sweep, with every row this mod changed in it, so the next checks can skip the table.</summary>
    public void Swept(long table, int count, IEnumerable<(TKey Key, TValue Written)> changed)
    {
        var rows = changed.ToList();

        _swept = (table, count);
        _watched = rows.Count <= WatchCount ? rows : Enumerable.Range(0, WatchCount).Select(i => rows[i * (rows.Count - 1) / (WatchCount - 1)]).ToList();
    }

    /// <summary>The values to write changed, so the next check sweeps.</summary>
    public void Forget() => _swept = null;
}
