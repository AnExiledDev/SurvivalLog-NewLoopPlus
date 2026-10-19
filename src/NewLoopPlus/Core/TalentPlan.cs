using System.Collections.Generic;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>Which rows the game's tables hold, as far as building a plan needs to know.</summary>
public interface ITableIndex
{
    bool HasEffect(int id);

    bool HasBuff(int id);

    /// <summary>The levels the game has for a talent, lowest first; empty when the talent does not exist.</summary>
    IReadOnlyList<int> TalentLevels(int talentId);
}

public enum Table
{
    Effect,
    Buff,
    Talent,
}

/// <summary>One row to change. With <paramref name="CopyFrom"/> set, the row is first made as a copy of that row of the same table.</summary>
public sealed record RowEdit(Table Table, int Id, int? CopyFrom, IReadOnlyList<FieldSet> Fields);

/// <summary>One text the game looks up by key, in both languages.</summary>
public sealed record TextEntry(string Key, string En, string Zh);

/// <summary>Everything the talents file changes in the game's tables, in the order it must be applied.</summary>
public sealed record TalentPlan(IReadOnlyList<RowEdit> Rows, IReadOnlyList<TextEntry> Texts, IReadOnlyList<string> Problems)
{
    /// <summary>Row IDs are TalentID * 10 + level, so a talent can have at most 9 levels.</summary>
    public const int MaxLevels = 9;

    /// <summary>
    /// Works out the edits. Effects come first and buffs second, so a buff can point at an effect the file makes, and
    /// talents last, so a level can point at a buff the file makes. A disabled talent changes nothing, including its
    /// own buffs and effects. Anything that cannot be built is reported and left out; the rest still applies.
    /// </summary>
    public static TalentPlan Build(TalentFile file, ITableIndex tables)
    {
        var builder = new Builder(tables);
        var enabled = file.Talents.Where(talent => talent.Enabled).ToList();

        builder.AddRows(Table.Effect, file.Effects.Concat(enabled.SelectMany(talent => talent.Effects)));
        builder.AddRows(Table.Buff, file.Buffs.Concat(enabled.SelectMany(talent => talent.Buffs)));

        foreach (var talent in enabled) builder.AddTalent(talent);

        return new TalentPlan(builder.Rows, builder.Texts, builder.Problems);
    }

    private sealed class Builder
    {
        private const string KeyPrefix = "NewLoopPlus_Talent_";

        private readonly ITableIndex _tables;
        private readonly HashSet<int> _madeEffects = new();
        private readonly HashSet<int> _madeBuffs = new();

        public Builder(ITableIndex tables)
        {
            _tables = tables;
        }

        public List<RowEdit> Rows { get; } = new();

        public List<TextEntry> Texts { get; } = new();

        public List<string> Problems { get; } = new();

        public void AddRows(Table table, IEnumerable<RowEntry> entries)
        {
            foreach (var entry in entries)
            {
                var name = table == Table.Effect ? "effect" : "buff";

                if (entry.CopyFrom is { } source && !Exists(table, source))
                {
                    Problems.Add($"{name} {entry.Id} was skipped: copyFrom {source} is not a {name} the game has");

                    continue;
                }

                if (entry.CopyFrom == null && !Exists(table, entry.Id))
                {
                    Problems.Add($"{name} {entry.Id} was skipped: the game has no such {name}; give a copyFrom to make a new one");

                    continue;
                }

                var fields = entry.CopyFrom == null ? entry.Fields : Prepend(entry.Fields, Whole("ID", entry.Id));

                Rows.Add(new RowEdit(table, entry.Id, entry.CopyFrom, fields));
                Made(table).Add(entry.Id);
            }
        }

        public void AddTalent(TalentEntry talent)
        {
            var existing = _tables.TalentLevels(talent.Id);
            var source = existing.Count > 0 ? talent.Id : talent.CopyFrom ?? 0;
            var sourceLevels = _tables.TalentLevels(source);

            if (sourceLevels.Count == 0)
            {
                Problems.Add(talent.CopyFrom == null
                    ? $"talent {talent.Id} was skipped: the game has no such talent; give a copyFrom to make a new one"
                    : $"talent {talent.Id} was skipped: copyFrom {talent.CopyFrom} is not a talent the game has");

                return;
            }

            var count = talent.Levels?.Count ?? sourceLevels.Count;

            if (count is 0 or > MaxLevels)
            {
                Problems.Add($"talent {talent.Id} was skipped: it needs 1 to {MaxLevels} levels, not {count}");

                return;
            }

            for (var lv = 1; lv <= count; lv++) AddLevel(talent, existing, source, sourceLevels, lv);

            foreach (var extra in existing.Where(lv => lv > count)) Rows.Add(new RowEdit(Table.Talent, RowId(talent.Id, extra), null, new[] { Whole("Available", 0) }));
        }

        private void AddLevel(TalentEntry talent, IReadOnlyList<int> existing, int source, IReadOnlyList<int> sourceLevels, int lv)
        {
            var rowId = RowId(talent.Id, lv);
            var level = talent.Levels?[lv - 1];
            var fields = new List<FieldSet> { Whole("Available", 1) };
            int? copyFrom = null;

            if (!existing.Contains(lv))
            {
                copyFrom = RowId(source, NearestLevel(sourceLevels, lv));
                fields.Add(Whole("ID", rowId));
                fields.Add(Whole("TalentID", talent.Id));
                fields.Add(Whole("Lv", lv));
            }

            fields.AddRange(talent.Fields);

            if (level != null)
            {
                fields.AddRange(level.Fields);

                if (level.Cost is { } cost) fields.Add(Whole("LvUpCost", cost));

                if (level.Buff is { } buff) AddBuffLink(talent, fields, buff);
            }

            AddText(fields, "Name", rowId, talent.Name);
            AddText(fields, "Dec", rowId, level?.Description ?? talent.Description);
            AddText(fields, "LvParaShow", rowId, level?.LevelText ?? talent.LevelText);
            Rows.Add(new RowEdit(Table.Talent, rowId, copyFrom, fields));
        }

        private void AddBuffLink(TalentEntry talent, List<FieldSet> fields, int buff)
        {
            if (!Exists(Table.Buff, buff)) Problems.Add($"talent {talent.Id} points a level at buff {buff}, which neither the game nor the file has; that level gives nothing");

            fields.Add(Whole("BuffID", buff));
        }

        /// <summary>Talent text gets keys of its own, so a copied row never changes the text of the row it came from.</summary>
        private void AddText(List<FieldSet> fields, string field, int rowId, LocalText? text)
        {
            if (text == null) return;

            var key = $"{KeyPrefix}{field}_{rowId}";

            fields.Add(new FieldSet(field, new FieldValue.Text(key)));
            Texts.Add(new TextEntry(key, text.En, text.Zh));
        }

        private bool Exists(Table table, int id) =>
            Made(table).Contains(id) || (table == Table.Effect ? _tables.HasEffect(id) : _tables.HasBuff(id));

        private HashSet<int> Made(Table table) => table == Table.Effect ? _madeEffects : _madeBuffs;
    }

    /// <summary>A new level copies the closest level at or below it, so an added fourth level starts from the third.</summary>
    private static int NearestLevel(IReadOnlyList<int> levels, int lv)
    {
        var below = levels.Where(level => level <= lv).ToList();

        return below.Count > 0 ? below.Max() : levels.Min();
    }

    private static int RowId(int talentId, int lv) => talentId * 10 + lv;

    private static FieldSet Whole(string field, int value) => new(field, new FieldValue.Number(value));

    private static List<FieldSet> Prepend(IReadOnlyList<FieldSet> fields, FieldSet first)
    {
        var all = new List<FieldSet> { first };

        all.AddRange(fields);

        return all;
    }
}
