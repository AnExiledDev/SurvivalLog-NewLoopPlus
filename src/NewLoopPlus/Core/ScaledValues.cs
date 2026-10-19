using System;
using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>
/// Keeps values in a game table scaled without ever scaling one twice. The first value seen for a key is the game's
/// own; after that, a value other than the one written means the game reloaded the table, so it becomes the new
/// original.
/// </summary>
public sealed class ScaledValues<TKey, TValue>
    where TKey : notnull
    where TValue : struct, IEquatable<TValue>
{
    private readonly Func<TKey, TValue, TValue> _scale;
    private readonly Dictionary<TKey, (TValue Original, TValue Scaled)> _known = new();

    public ScaledValues(Func<TKey, TValue, TValue> scale)
    {
        _scale = scale;
    }

    /// <summary>The value to write for <paramref name="key"/>, or null when it already holds it.</summary>
    public TValue? Next(TKey key, TValue current)
    {
        if (!_known.TryGetValue(key, out var known) || !known.Scaled.Equals(current))
        {
            known = (current, _scale(key, current));
            _known[key] = known;
        }

        return known.Scaled.Equals(current) ? null : known.Scaled;
    }

    /// <summary>The scaled value last worked out for <paramref name="key"/>, or null before it was first seen.</summary>
    public TValue? Scaled(TKey key) => _known.TryGetValue(key, out var known) ? known.Scaled : null;

    /// <summary>The game's own value for <paramref name="key"/>, or null before it was first seen.</summary>
    public TValue? Original(TKey key) => _known.TryGetValue(key, out var known) ? known.Original : null;
}
