using System;
using System.Collections.Generic;
using ExiledToolkit;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Tells Exiled's Toolkit what New Loop Plus changes in the game's tables, so Mod Manager's Conflicts tab can show
/// another mod that changes the same thing. The talents file and the cost multipliers can both write a reward's cost,
/// so a cost the file set stays reported when the multipliers go back to 1.
/// </summary>
internal static class Reported
{
    private static readonly HashSet<(string Table, int Row, string Field)> FromFile = new();

    /// <summary>A row the talents file added.</summary>
    public static void Added(string table, int row) => Claims.AddedRows(Plugin.Guid, table, new[] { row });

    /// <summary>A field the talents file wrote on a row the game has.</summary>
    public static void Set(string table, int row, FieldSet field)
    {
        FromFile.Add((table, row, field.Field));
        Claims.Wrote(Plugin.Guid, table, row, field.Field, field.Value.AsText());
    }

    /// <summary>A value a multiplier just wrote. It is reported while it differs from the game's, and taken back once the game's value is back.</summary>
    public static void Scaled<TKey, TValue>(ScaledValues<TKey, TValue> values, TKey key, string table, object row, string field)
        where TKey : notnull
        where TValue : struct, IEquatable<TValue>
    {
        if (values.IsChanged(key) || (row is int id && FromFile.Contains((table, id, field)))) Claims.Wrote(Plugin.Guid, table, row, field, values.Scaled(key));
        else Claims.PutBack(Plugin.Guid, table, row, field);
    }
}
