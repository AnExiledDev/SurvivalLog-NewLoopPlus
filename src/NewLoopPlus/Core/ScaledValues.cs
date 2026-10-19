using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>
/// Keeps values in a game table scaled without ever scaling one twice. The first value seen for a key is the game's
/// own; after that, a value other than the one written means the game reloaded the table, so it becomes the new
/// original. When the multiplier changes, <see cref="Rescale"/> works every value out again from the game's own, and
/// the value written before still counts as ours, so it is replaced rather than taken for a reload.
/// </summary>
public sealed class ScaledValues<TKey, TValue>
    where TKey : notnull
    where TValue : struct, IEquatable<TValue>
{
    private readonly Func<TKey, TValue, TValue> _scale;
    private readonly Dictionary<TKey, Known> _known = new();

    public ScaledValues(Func<TKey, TValue, TValue> scale)
    {
        _scale = scale;
    }

    /// <summary>The value to write for <paramref name="key"/>, or null when it already holds it.</summary>
    public TValue? Next(TKey key, TValue current)
    {
        if (!_known.TryGetValue(key, out var known) || !known.IsOurs(current)) known = new Known(current, _scale(key, current), null);

        // From here the table holds the scaled value, so an older one is a reload again.
        _known[key] = known with { Replaced = null };

        return known.Scaled.Equals(current) ? null : known.Scaled;
    }

    /// <summary>Works every known value out again from the game's own, after the scale changed.</summary>
    public void Rescale()
    {
        foreach (var key in new List<TKey>(_known.Keys))
        {
            var known = _known[key];
            var scaled = _scale(key, known.Original);

            if (scaled.Equals(known.Scaled)) continue;

            _known[key] = new Known(known.Original, scaled, known.Replaced ?? known.Scaled);
        }
    }

    /// <summary>The scaled value last worked out for <paramref name="key"/>, or null before it was first seen.</summary>
    public TValue? Scaled(TKey key) => _known.TryGetValue(key, out var known) ? known.Scaled : null;

    /// <summary>The game's own value for <paramref name="key"/>, or null before it was first seen.</summary>
    public TValue? Original(TKey key) => _known.TryGetValue(key, out var known) ? known.Original : null;

    /// <summary><paramref name="Replaced"/> is the value still in the table from before the scale changed, if any.</summary>
    private readonly record struct Known(TValue Original, TValue Scaled, TValue? Replaced)
    {
        public bool IsOurs(TValue current) => Scaled.Equals(current) || (Replaced is { } replaced && replaced.Equals(current));
    }
}
