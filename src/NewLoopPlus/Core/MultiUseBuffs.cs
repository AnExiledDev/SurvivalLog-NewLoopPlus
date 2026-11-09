using System.Collections.Generic;

namespace NewLoopPlus.Core;

/// <summary>
/// Buffs a player spends one use at a time, like Mental Toughness's levels with more than one use. The game ends such a
/// buff, and runs its end effects, only when its last layer goes; with uses left it only takes a layer off, so a use
/// would do nothing. These say when a use must end the buff and start it again with the uses that are left.
/// </summary>
public sealed class MultiUseBuffs
{
    private readonly HashSet<int> _ids = new();

    public void Clear() => _ids.Clear();

    public void Add(int buffId) => _ids.Add(buffId);

    public int Count => _ids.Count;

    /// <summary>The uses left after spending <paramref name="spent"/> of <paramref name="uses"/>, or null when the game's step is right: not one of these buffs, or no use left after it.</summary>
    public int? UsesLeft(int buffId, int uses, int spent)
    {
        if (!_ids.Contains(buffId) || spent <= 0) return null;

        var left = uses - spent;

        return left > 0 ? left : null;
    }
}
