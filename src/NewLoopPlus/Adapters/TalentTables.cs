using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using Il2CppInterop.Runtime.InteropTypes;
using NewLoopPlus.Core;
using LanguageMap = Il2CppSystem.Collections.Generic.Dictionary<string, string>;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Writes the talents file into the game's effect, buff and talent tables and its two language maps, once the game has
/// loaded them. It writes again only if the game loads the tables afresh, so it never fights the cost scaling, which
/// treats whatever it finds there as the game's own price. Before writing, it reads the game's own loop rewards for the
/// Talents tab, which needs them as the game made them.
/// </summary>
internal sealed class TalentTables
{
    private const float CheckSeconds = 1f;
    private const string ScriptPrefix = "Buff/";

    private readonly TalentFile _file;
    private readonly TalentsTab _tab;
    private readonly Action<string> _log;
    private (IntPtr Talents, IntPtr Buffs, IntPtr Effects) _applied;
    private float _nextCheckAt;

    public TalentTables(TalentFile file, TalentsTab tab, Action<string> log)
    {
        _file = file;
        _tab = tab;
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

        var index = new GameTableIndex(talents, buffs, effects);
        var game = index.Snapshot(english, chinese);
        var plan = TalentPlan.Build(_file, index);
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

        CompanionBuffs.Use(plan.Companions);
        Perks.Grants = plan.Grants;
        _tab.Started(game, index.Scripts, ItemIcons(config), problems);
        Report(plan, problems);
    }

    /// <summary>The images of the game's items, which a reward can show as well as any talent's.</summary>
    private static List<GameIcon> ItemIcons(ConfigManager config)
    {
        var icons = new List<GameIcon>();
        var items = config._Config_Item_Dict;

        if (items == null) return icons;

        foreach (var item in items.Values)
        {
            if (item != null && !string.IsNullOrEmpty(item.WebIcon)) icons.Add(new GameIcon(item.WebIcon, item.Icon ?? ""));
        }

        return icons;
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

    /// <summary>What the plan and the Talents tab need to know of the game's tables, read once per write, before it.</summary>
    private sealed class GameTableIndex : ITableIndex
    {
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> _buffs;
        private readonly Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> _effects;
        private readonly Dictionary<int, List<Config_Talent>> _levels = new();
        private readonly Dictionary<string, GiveTemplate> _firstByScript = new();

        public GameTableIndex(
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Talent> talents,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Buff> buffs,
            Il2CppSystem.Collections.Generic.Dictionary<int, Config_Effect> effects)
        {
            _buffs = buffs;
            _effects = effects;

            var rows = new List<Config_Talent>();

            foreach (var row in talents.Values)
            {
                if (row != null) rows.Add(row);
            }

            foreach (var row in rows.OrderBy(row => row._ID_k__BackingField))
            {
                if (!_levels.TryGetValue(row._TalentID_k__BackingField, out var levels)) _levels[row._TalentID_k__BackingField] = levels = new List<Config_Talent>();

                levels.Add(row);

                if (Effect(row) is { } effect && Script(effect.Row) is { } script && !_firstByScript.ContainsKey(script)) _firstByScript[script] = new GiveTemplate(effect.Id, row._BuffID_k__BackingField);
            }

            foreach (var levels in _levels.Values) levels.Sort((a, b) => a._Lv_k__BackingField.CompareTo(b._Lv_k__BackingField));
        }

        /// <summary>Every effect script some talent runs, so a give can use it; read before the file adds its own.</summary>
        public IReadOnlyList<string> Scripts => _firstByScript.Keys.ToList();

        public bool HasEffect(int id) => _effects.ContainsKey(id);

        public bool HasBuff(int id) => _buffs.ContainsKey(id);

        public IReadOnlyList<int> TalentLevels(int talentId) =>
            _levels.TryGetValue(talentId, out var levels) ? levels.Select(row => row._Lv_k__BackingField).ToList() : Array.Empty<int>();

        public GiveTemplate? Template(string script, int talentId, int lv)
        {
            if (Level(talentId, lv) is { } row && Effect(row) is { } own && Script(own.Row) == script) return new GiveTemplate(own.Id, row._BuffID_k__BackingField);

            return _firstByScript.TryGetValue(script, out var first) ? first : null;
        }

        public int? LevelBuff(int talentId, int lv) => Level(talentId, lv)?._BuffID_k__BackingField;

        /// <summary>Every talent as the game made them, for the Talents tab, which lists the loop rewards and offers every talent's image.</summary>
        public List<GameTalent> Snapshot(LanguageMap english, LanguageMap chinese) =>
            _levels.Values.Select(levels => Describe(levels, english, chinese)).ToList();

        private GameTalent Describe(List<Config_Talent> levels, LanguageMap english, LanguageMap chinese)
        {
            var first = levels[0];
            var described = levels.Select(row => DescribeLevel(row, english, chinese)).ToList();

            return new GameTalent(
                first._TalentID_k__BackingField,
                first._Type_k__BackingField,
                first._EndlessMode_k__BackingField,
                first._Available_k__BackingField,
                first._ShowType_k__BackingField,
                first._WebIcon_k__BackingField ?? "",
                first._Icon_k__BackingField ?? "",
                Text(english, chinese, first._Name_k__BackingField, first._Name_Local_k__BackingField),
                Text(english, chinese, first._LvParaShow_k__BackingField, first._LvParaShow_Local_k__BackingField),
                described);
        }

        private GameLevel DescribeLevel(Config_Talent row, LanguageMap english, LanguageMap chinese)
        {
            var effect = Effect(row)?.Row;

            return new GameLevel(
                row._Lv_k__BackingField,
                row._LvUpCost_k__BackingField,
                row._BuffID_k__BackingField,
                Text(english, chinese, row._Dec_k__BackingField, row._Dec_Local_k__BackingField),
                effect?._ConfigName_k__BackingField ?? "",
                effect == null ? new List<FieldSet>() : RowFields.ReadAll(effect));
        }

        private Config_Talent? Level(int talentId, int lv) =>
            _levels.TryGetValue(talentId, out var levels) ? levels.FirstOrDefault(row => row._Lv_k__BackingField == lv) : null;

        /// <summary>The effect a talent level runs: its buff names the effect.</summary>
        private (int Id, Config_Effect Row)? Effect(Config_Talent row)
        {
            if (!_buffs.ContainsKey(row._BuffID_k__BackingField)) return null;

            var buff = _buffs[row._BuffID_k__BackingField];
            var id = buff?._EffectConfigId_k__BackingField ?? 0;

            if (buff == null || !_effects.ContainsKey(id) || _effects[id] is not { } effect) return null;

            return (id, effect);
        }

        private static string? Script(Config_Effect effect) =>
            effect._ConfigName_k__BackingField is { } name && name.StartsWith(ScriptPrefix, StringComparison.Ordinal) ? name.Substring(ScriptPrefix.Length) : null;

        /// <summary>A table names its text by a language key; a key with no translation shows as itself, as the game shows it.</summary>
        private static LocalText Text(LanguageMap english, LanguageMap chinese, string? key, string? local)
        {
            if (string.IsNullOrEmpty(key)) return new LocalText("", local ?? "");

            var en = english.ContainsKey(key) ? english[key] : key;
            var zh = chinese.ContainsKey(key) ? chinese[key] : string.IsNullOrEmpty(local) ? key : local;

            return new LocalText(en ?? key, zh ?? key);
        }
    }
}
