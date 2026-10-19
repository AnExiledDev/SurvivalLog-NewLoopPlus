using System;
using System.Collections.Generic;
using System.Reflection;
using NewLoopPlus.Core;
using Il2CppInts = Il2CppSystem.Collections.Generic.List<int>;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Reads and writes the fields of a game table row by the name the table gives them. The game's rows keep each field
/// in a compiler-made backing field, which the interop layer shows as a property named <c>_Field_k__BackingField</c>.
/// </summary>
internal static class RowFields
{
    private const string Suffix = "_k__BackingField";
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.Instance;

    private static readonly Dictionary<(Type, string), PropertyInfo?> Found = new();

    /// <summary>Copies every field of <paramref name="from"/> onto <paramref name="to"/>, a row of the same table.</summary>
    public static void CopyAll(object from, object to)
    {
        foreach (var property in to.GetType().GetProperties(Instance))
        {
            if (!IsField(property) || !property.CanRead || !property.CanWrite) continue;

            property.SetValue(to, property.GetValue(from));
        }
    }

    /// <summary>Sets one field, or returns why it could not be set.</summary>
    public static string? Set(object row, string field, FieldValue value)
    {
        var property = Find(row.GetType(), field);

        if (property == null) return $"{row.GetType().Name} has no field {field}";

        if (Convert(property.PropertyType, value) is not { } converted) return $"{field} on {row.GetType().Name} cannot hold {Describe(value)}";

        property.SetValue(row, converted);

        return null;
    }

    private static object? Convert(Type type, FieldValue value) => value switch
    {
        FieldValue.Number number when type == typeof(int) => TalentFile.AsWhole(number.Value),
        FieldValue.Number number when type == typeof(long) => TalentFile.AsWhole(number.Value) is { } whole ? (long)whole : null,
        FieldValue.Number number when type == typeof(float) => (float)number.Value,
        FieldValue.Number number when type == typeof(double) => number.Value,
        FieldValue.Flag flag when type == typeof(bool) => flag.Value,
        FieldValue.Text text when type == typeof(string) => text.Value,
        FieldValue.Numbers numbers when type == typeof(Il2CppInts) => ToList(numbers.Values),
        _ => null,
    };

    private static Il2CppInts ToList(IReadOnlyList<int> values)
    {
        var list = new Il2CppInts();

        foreach (var value in values) list.Add(value);

        return list;
    }

    private static string Describe(FieldValue value) => value switch
    {
        FieldValue.Number number => number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        FieldValue.Text text => $"\"{text.Value}\"",
        FieldValue.Flag flag => flag.Value ? "true" : "false",
        FieldValue.Numbers => "a list of numbers",
        _ => "that value",
    };

    private static PropertyInfo? Find(Type type, string field)
    {
        if (Found.TryGetValue((type, field), out var cached)) return cached;

        var property = type.GetProperty("_" + field + Suffix, Instance);

        Found[(type, field)] = property;

        return property;
    }

    private static bool IsField(PropertyInfo property) => property.Name.StartsWith("_", StringComparison.Ordinal) && property.Name.EndsWith(Suffix, StringComparison.Ordinal);
}
