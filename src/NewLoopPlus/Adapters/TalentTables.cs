using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using Il2CppInterop.Runtime.InteropTypes;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Writes the talents file into the game's effect, buff and talent tables and its two language maps, once the game has
/// loaded them. It writes again only if the game loads the tables afresh, so it never fights the cost scaling, which
/// treats whatever it finds there as the game's own price.
/// </summary>
internal sealed class TalentTables
{
    private const float CheckSeconds = 1f;

    private readonly TalentFile _file;
    private readonly Action<string> _log;
    private (IntPtr Talents, IntPtr Buffs, IntPtr Effects) _applied;
    private float _nextCheckAt;

    public TalentTables(TalentFile file, Action<string> log)
    {
        _file = file;
        _log = log;
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var config = GameSingleton.Existing<ConfigManager>();
        var talents = config?._Config_Talent_Dict;
        var buffs = config?._Config_Buff_Dict;
        var effects = config?._Config_Effect_Dict;
        var english = config?.customCache?.EnglishLanguageMap;
        var chinese = config?.customCache?.ChineseLanguageMap;

        if (talents == null || buffs == null || effects == null || english == null || chinese == null) return;

        if (talents.Count == 0 || buffs.Count == 0 || effects.Count == 0) return;

        var tables = (talents.Pointer, buffs.Pointer, effects.Pointer);

        if (tables == _applied) return;

        _applied = tables;

        var plan = TalentPlan.Build(_file, new GameTableIndex(talents, buffs, effects));
        var problems = new List<string>(plan.Problems);

        foreach (var edit in plan.Rows)
        {
            var problem = edit.Table switch
            {
                Table.Effect => Apply(effects, edit, () => new Config_Effect()),
                Table.Buff => Apply(buffs, edit, () => new Config_Buff()),
                _ => Apply(talents, edit, () => new Config_Talent()),
            };

            if (problem != null) problems.Add(problem);
        }

        foreach (var text in plan.Texts)
        {
            english[text.Key] = text.En;
            chinese[text.Key] = text.Zh;
        }

        Report(plan, problems);
    }

    private static string? Apply<T>(Il2CppSystem.Collections.Generic.Dictionary<int, T> table, RowEdit edit, Func<T> make) where T : Il2CppObjectBase
    {
        T row;

        if (edit.CopyFrom is { } source)
        {
            if (!table.ContainsKey(source)) return $"{edit.Table} {edit.Id}: copyFrom {source} is gone";

            row = make();
            RowFields.CopyAll(table[source], row);
            table[edit.Id] = row;
        }
        else
        {
            if (!table.ContainsKey(edit.Id)) return $"{edit.Table} {edit.Id} is gone";

            row = table[edit.Id];
        }

        var refused = edit.Fields.Select(field => RowFields.Set(row, field.Field, field.Value)).OfType<string>().ToList();

        return refused.Count == 0 ? null : $"{edit.Table} {edit.Id}: " + string.Join("; ", refused);
    }

    private void Report(TalentPlan plan, List<string> problems)
    {
        var enabled = _file.Talents.Where(talent => talent.Enabled).Select(talent => talent.Id).ToList();

        _log(enabled.Count == 0
            ? "talents file: no talent is enabled"
            : $"talents file: enabled {string.Join(", ", enabled)}; {plan.Rows.Count} table rows written");

        foreach (var problem in problems) _log("talents file: " + problem);
    }

    /// <summary>What the plan needs to know of the game's tables, read once per write.</summary>
    private sealed class GameTableIndex : ITableIndex
    {
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> _buffs;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> _effects;
        private readonly Dictionary<int, List<int>> _levels = new();

        public GameTableIndex(
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Talent> talents,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> buffs,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects)
        {
            _buffs = buffs;
            _effects = effects;

            foreach (var row in talents.Values)
            {
                if (row == null) continue;

                if (!_levels.TryGetValue(row._TalentID_k__BackingField, out var levels)) _levels[row._TalentID_k__BackingField] = levels = new List<int>();

                levels.Add(row._Lv_k__BackingField);
            }

            foreach (var levels in _levels.Values) levels.Sort();
        }

        public bool HasEffect(int id) => _effects.ContainsKey(id);

        public bool HasBuff(int id) => _buffs.ContainsKey(id);

        public IReadOnlyList<int> TalentLevels(int talentId) => _levels.TryGetValue(talentId, out var levels) ? levels : Array.Empty<int>();
    }
}
