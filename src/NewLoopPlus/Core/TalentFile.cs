using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace NewLoopPlus.Core;

/// <summary>A value for one field of a game table row, as the talents file gives it.</summary>
public abstract record FieldValue
{
    private FieldValue()
    {
    }

    public sealed record Number(double Value) : FieldValue;

    public sealed record Text(string Value) : FieldValue;

    public sealed record Flag(bool Value) : FieldValue;

    public sealed record Numbers(IReadOnlyList<int> Values) : FieldValue;

    /// <summary>The value written the way Mod Manager shows a table's value, so it can tell whether the table still holds it. A list is its numbers with commas between.</summary>
    public string AsText() => this switch
    {
        Number number => number.Value.ToString(CultureInfo.InvariantCulture),
        Text text => text.Value,
        Flag flag => flag.Value ? "True" : "False",
        Numbers numbers => string.Join(",", numbers.Values),
        _ => "",
    };
}

/// <summary>One field set on a row, named as the game's table names it (LvUpCost, IntValue0, ConfigName...).</summary>
public sealed record FieldSet(string Field, FieldValue Value);

/// <summary>Text in both of the game's languages.</summary>
public sealed record LocalText(string En, string Zh);

/// <summary>A buff or effect row: an existing one changed, or a new one copied from <paramref name="CopyFrom"/>.</summary>
public sealed record RowEntry(int Id, int? CopyFrom, IReadOnlyList<FieldSet> Fields);

/// <summary>One level of a talent. Anything left out keeps the game's value.</summary>
public sealed record LevelEntry(int? Cost, int? Buff, LocalText? Description, LocalText? LevelText, IReadOnlyList<FieldSet> Fields);

/// <summary>While the talent's owner has <paramref name="TalentBuff"/>, every home door and window gets <paramref name="DoorWindowBuff"/>.</summary>
public sealed record ReinforceRule(int TalentBuff, int DoorWindowBuff);

/// <summary>
/// One thing a talent gives: a game effect script, run with <paramref name="Choice"/> at every level and with the fields of
/// <paramref name="Levels"/> at each level. A level past the end of <paramref name="Levels"/> uses the last one.
/// </summary>
public sealed record GiveEntry(string Effect, IReadOnlyList<FieldSet> Choice, IReadOnlyList<IReadOnlyList<FieldSet>> Levels)
{
    public IReadOnlyList<FieldSet> FieldsAt(int lv) =>
        Levels.Count == 0 ? Array.Empty<FieldSet>() : Levels[Math.Min(lv, Levels.Count) - 1];
}

/// <summary>
/// One talent in the file. Its buffs, effects and reinforce rules belong to it and apply only while it is enabled.
/// <paramref name="Levels"/> is null when the file leaves the game's levels as they are.
/// </summary>
public sealed record TalentEntry(
    int Id,
    bool Enabled,
    int? CopyFrom,
    LocalText? Name,
    LocalText? Description,
    LocalText? LevelText,
    IReadOnlyList<FieldSet> Fields,
    IReadOnlyList<LevelEntry>? Levels,
    IReadOnlyList<RowEntry> Buffs,
    IReadOnlyList<RowEntry> Effects,
    IReadOnlyList<ReinforceRule> Reinforce,
    IReadOnlyList<GiveEntry> Gives);

/// <summary>The talents file as read, with every entry that could not be read.</summary>
public sealed record TalentFile(IReadOnlyList<TalentEntry> Talents, IReadOnlyList<RowEntry> Buffs, IReadOnlyList<RowEntry> Effects, IReadOnlyList<string> Problems)
{
    public static readonly TalentFile Empty = new(Array.Empty<TalentEntry>(), Array.Empty<RowEntry>(), Array.Empty<RowEntry>(), Array.Empty<string>());

    /// <summary>The reinforce rules of each enabled talent, one list per talent.</summary>
    public IReadOnlyList<IReadOnlyList<ReinforceRule>> ReinforceGroups()
    {
        var groups = new List<IReadOnlyList<ReinforceRule>>();

        foreach (var talent in Talents)
        {
            if (talent.Enabled && talent.Reinforce.Count > 0) groups.Add(talent.Reinforce);
        }

        return groups;
    }

    /// <summary>
    /// Reads the file. Comments and trailing commas are allowed, since people edit it by hand. An entry that cannot be
    /// read is skipped and reported; the rest still apply. A file that is not JSON at all applies nothing.
    /// </summary>
    public static TalentFile Parse(string json)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException ex)
        {
            return Empty with { Problems = new[] { $"the file is not valid JSON ({ex.Message}); nothing in it was applied" } };
        }

        using (document)
        {
            var problems = new List<string>();
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object) return Empty with { Problems = new[] { "the file must hold one { } object; nothing in it was applied" } };

            var talents = ReadList(root, "talents", "talent", problems, ReadTalent);
            var buffs = ReadList(root, "buffs", "buff", problems, ReadRow);
            var effects = ReadList(root, "effects", "effect", problems, ReadRow);

            return new TalentFile(talents, buffs, effects, problems);
        }
    }

    private static TalentEntry ReadTalent(JsonElement entry, List<string> problems)
    {
        var id = RequireInt(entry, "id");
        var where = $"talent {id}";

        return new TalentEntry(
            id,
            OptionalBool(entry, "enabled") ?? true,
            OptionalInt(entry, "copyFrom"),
            OptionalText(entry, "name"),
            OptionalText(entry, "description"),
            OptionalText(entry, "levelText"),
            ReadFields(entry),
            entry.TryGetProperty("levels", out _) ? ReadList(entry, "levels", where + " level", problems, (level, _) => ReadLevel(level)) : null,
            ReadList(entry, "buffs", where + " buff", problems, ReadRow),
            ReadList(entry, "effects", where + " effect", problems, ReadRow),
            ReadList(entry, "reinforce", where + " reinforce rule", problems, (rule, _) => new ReinforceRule(RequireInt(rule, "talentBuff"), RequireInt(rule, "doorWindowBuff"))),
            ReadList(entry, "gives", where + " give", problems, (give, _) => ReadGive(give)));
    }

    private static GiveEntry ReadGive(JsonElement give)
    {
        var effect = give.TryGetProperty("effect", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() ?? "" : "";

        if (effect.Length == 0) throw new FormatException("\"effect\" is missing");

        var levels = new List<IReadOnlyList<FieldSet>>();

        if (give.TryGetProperty("levels", out var list))
        {
            if (list.ValueKind != JsonValueKind.Array) throw new FormatException("\"levels\" must be a [ ] list");

            foreach (var level in list.EnumerateArray()) levels.Add(ReadFieldObject("a level", level));
        }

        return new GiveEntry(effect, ReadFields(give, "choice"), levels);
    }

    private static LevelEntry ReadLevel(JsonElement level) =>
        new(OptionalInt(level, "cost"), OptionalInt(level, "buff"), OptionalText(level, "description"), OptionalText(level, "levelText"), ReadFields(level));

    private static RowEntry ReadRow(JsonElement row, List<string> problems) => new(RequireInt(row, "id"), OptionalInt(row, "copyFrom"), ReadFields(row));

    /// <summary>A list whose bad entries are reported by position and left out.</summary>
    private static List<T> ReadList<T>(JsonElement parent, string name, string what, List<string> problems, Func<JsonElement, List<string>, T> read)
    {
        var items = new List<T>();

        if (!parent.TryGetProperty(name, out var list)) return items;

        if (list.ValueKind != JsonValueKind.Array)
        {
            problems.Add($"\"{name}\" must be a [ ] list; it was skipped");

            return items;
        }

        var position = 0;

        foreach (var element in list.EnumerateArray())
        {
            position++;

            try
            {
                if (element.ValueKind != JsonValueKind.Object) throw new FormatException("it must be a { } object");

                items.Add(read(element, problems));
            }
            catch (FormatException ex)
            {
                problems.Add($"{what} #{position} was skipped: {ex.Message}");
            }
        }

        return items;
    }

    private static IReadOnlyList<FieldSet> ReadFields(JsonElement entry, string name = "fields") =>
        entry.TryGetProperty(name, out var set) ? ReadFieldObject($"\"{name}\"", set) : Array.Empty<FieldSet>();

    private static IReadOnlyList<FieldSet> ReadFieldObject(string what, JsonElement set)
    {
        if (set.ValueKind != JsonValueKind.Object) throw new FormatException($"{what} must be a {{ }} object");

        var fields = new List<FieldSet>();

        foreach (var field in set.EnumerateObject()) fields.Add(new FieldSet(field.Name, ReadValue(field.Name, field.Value)));

        return fields;
    }

    private static FieldValue ReadValue(string field, JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => new FieldValue.Number(value.GetDouble()),
        JsonValueKind.String => new FieldValue.Text(value.GetString() ?? ""),
        JsonValueKind.True => new FieldValue.Flag(true),
        JsonValueKind.False => new FieldValue.Flag(false),
        JsonValueKind.Array => new FieldValue.Numbers(ReadInts(field, value)),
        _ => throw new FormatException($"field {field} must be a number, text, true/false or a list of whole numbers"),
    };

    private static List<int> ReadInts(string field, JsonElement list)
    {
        var values = new List<int>();

        foreach (var item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var number)) throw new FormatException($"field {field} must list whole numbers only");

            values.Add(number);
        }

        return values;
    }

    private static int RequireInt(JsonElement entry, string name) =>
        OptionalInt(entry, name) ?? throw new FormatException($"\"{name}\" is missing");

    private static int? OptionalInt(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;

        throw new FormatException($"\"{name}\" must be a whole number, not {value.GetRawText()}");
    }

    private static bool? OptionalBool(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new FormatException($"\"{name}\" must be true or false"),
        };
    }

    /// <summary>Either a plain string, used for both languages, or { "en": ..., "zh": ... }; a missing Chinese text falls back to the English.</summary>
    private static LocalText? OptionalText(JsonElement entry, string name)
    {
        if (!entry.TryGetProperty(name, out var value)) return null;

        if (value.ValueKind == JsonValueKind.String) return Both(value.GetString() ?? "");

        if (value.ValueKind != JsonValueKind.Object) throw new FormatException($"\"{name}\" must be text or {{ \"en\": ..., \"zh\": ... }}");

        var en = value.TryGetProperty("en", out var enText) && enText.ValueKind == JsonValueKind.String ? enText.GetString() ?? "" : "";
        var zh = value.TryGetProperty("zh", out var zhText) && zhText.ValueKind == JsonValueKind.String ? zhText.GetString() ?? "" : "";

        if (en.Length == 0 && zh.Length == 0) throw new FormatException($"\"{name}\" has neither \"en\" nor \"zh\" text");

        return new LocalText(en.Length > 0 ? en : zh, zh.Length > 0 ? zh : en);
    }

    private static LocalText Both(string text) => new(text, text);

    /// <summary>A whole number field value, or null when the number has a fraction.</summary>
    public static int? AsWhole(double value) =>
        value is >= int.MinValue and <= int.MaxValue && Math.Floor(value) == value ? (int)value : null;
}
