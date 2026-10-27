using System;
using System.IO;
using System.Linq;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>The talents file is edited by hand, so it reads forgivingly and one mistake never costs the rest.</summary>
public class TalentFileTests
{
    private static readonly int[] CutTalentsThatWork = { 1006, 2017, 3076, 4004, 4006 };
    private static readonly int[] CutTalentsFinishedByTheMod = { 1008, 1010, 2004, 2006, 2007 };
    private static readonly int[] NewRewards = Enumerable.Range(5101, 34).ToArray();
    private static readonly int[] FiveLevelTalentsMadeTen = { 1001, 1002, 1003, 1004, 1005, 1007, 2005, 2008, 2010 };
    private static readonly int[] ThreeLevelTalentsMadeEight = { 1011, 1012, 2011, 2013, 2014, 2015, 2016, 2018 };

    private static TalentFile DefaultFile() => TalentFile.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "talents.default.json")));

    [Fact]
    public void Comments_and_trailing_commas_are_allowed()
    {
        var file = TalentFile.Parse("""
            // my talents
            { "talents": [ { "id": 1006, }, ], }
            """);

        Assert.Empty(file.Problems);
        Assert.Equal(1006, Assert.Single(file.Talents).Id);
    }

    [Fact]
    public void A_talent_is_enabled_unless_the_file_says_otherwise()
    {
        var file = TalentFile.Parse("""{ "talents": [ { "id": 1 }, { "id": 2, "enabled": false } ] }""");

        Assert.Equal(new[] { true, false }, file.Talents.Select(talent => talent.Enabled));
    }

    [Fact]
    public void A_bad_entry_is_reported_and_the_good_ones_are_kept()
    {
        var file = TalentFile.Parse("""
            { "talents": [ { "enabled": true }, { "id": 1006 }, { "id": "x" }, 5 ],
              "effects": [ { "id": 1, "fields": { "IntValue0": 2 } }, { "id": 2, "fields": { "IntList": [1, 2.5] } } ] }
            """);

        Assert.Equal(1006, Assert.Single(file.Talents).Id);
        Assert.Equal(1, Assert.Single(file.Effects).Id);
        Assert.Equal(4, file.Problems.Count);
    }

    [Fact]
    public void A_file_that_is_not_json_applies_nothing_and_says_why()
    {
        var file = TalentFile.Parse("{ talents: oops");

        Assert.Empty(file.Talents);
        Assert.Single(file.Problems);
    }

    [Fact]
    public void Field_values_keep_their_kind()
    {
        var file = TalentFile.Parse("""{ "buffs": [ { "id": 1, "fields": { "A": 0.25, "B": "Buff/Buff_Empty", "C": false, "D": [3, 4] } } ] }""");
        var fields = Assert.Single(file.Buffs).Fields;

        Assert.Equal(new FieldValue.Number(0.25), fields[0].Value);
        Assert.Equal(new FieldValue.Text("Buff/Buff_Empty"), fields[1].Value);
        Assert.Equal(new FieldValue.Flag(false), fields[2].Value);
        Assert.Equal(new[] { 3, 4 }, Assert.IsType<FieldValue.Numbers>(fields[3].Value).Values);
    }

    [Fact]
    public void Text_without_chinese_shows_the_english_in_both_languages()
    {
        var file = TalentFile.Parse("""{ "talents": [ { "id": 1, "name": { "en": "Scout" }, "description": "Plain" } ] }""");
        var talent = Assert.Single(file.Talents);

        Assert.Equal(new LocalText("Scout", "Scout"), talent.Name);
        Assert.Equal(new LocalText("Plain", "Plain"), talent.Description);
    }

    [Fact]
    public void Only_enabled_talents_reinforce_doors_and_windows()
    {
        var file = TalentFile.Parse("""
            { "talents": [
              { "id": 1, "enabled": false, "reinforce": [ { "talentBuff": 10, "doorWindowBuff": 11 } ] },
              { "id": 2, "reinforce": [ { "talentBuff": 20, "doorWindowBuff": 21 } ] } ] }
            """);

        Assert.Equal(new ReinforceRule(20, 21), Assert.Single(Assert.Single(file.ReinforceGroups())));
    }

    [Fact]
    public void A_give_reads_its_effect_its_choice_and_one_set_of_fields_per_level()
    {
        var file = TalentFile.Parse("""
            { "talents": [ { "id": 1002, "gives": [
              { "effect": "game:PreDisaster_AddRandomItem", "choice": { "IntValue0": 227 }, "levels": [ { "IntValue1": 2 }, { "IntValue1": 4 } ] } ] } ] }
            """);
        var give = Assert.Single(Assert.Single(file.Talents).Gives);

        Assert.Empty(file.Problems);
        Assert.Equal("game:PreDisaster_AddRandomItem", give.Effect);
        Assert.Equal(new FieldSet("IntValue0", new FieldValue.Number(227)), Assert.Single(give.Choice));
        Assert.Equal(new FieldValue.Number(2), Assert.Single(give.FieldsAt(1)).Value);
        Assert.Equal(new FieldValue.Number(4), Assert.Single(give.FieldsAt(5)).Value);
    }

    [Fact]
    public void A_give_without_an_effect_is_reported_and_the_talent_keeps_its_other_gives()
    {
        var file = TalentFile.Parse("""{ "talents": [ { "id": 1002, "gives": [ { "choice": {} }, { "effect": "game:PreDisaster_AddGold" } ] } ] }""");
        var talent = Assert.Single(file.Talents);

        Assert.Equal("game:PreDisaster_AddGold", Assert.Single(talent.Gives).Effect);
        Assert.Empty(talent.Gives[0].FieldsAt(1));
        Assert.Contains("talent 1002 give #1", Assert.Single(file.Problems));
    }

    [Fact]
    public void Whole_numbers_are_told_apart_from_fractions()
    {
        Assert.Equal(200, TalentFile.AsWhole(200.0));
        Assert.Null(TalentFile.AsWhole(0.5));
        Assert.Null(TalentFile.AsWhole(1e12));
    }

    [Fact]
    public void The_default_file_reads_cleanly_and_lists_the_cut_talents_and_new_rewards_all_switched_off()
    {
        var file = DefaultFile();

        Assert.Empty(file.Problems);
        Assert.Equal(CutTalentsThatWork.Concat(CutTalentsFinishedByTheMod).Concat(NewRewards).Concat(FiveLevelTalentsMadeTen).Concat(ThreeLevelTalentsMadeEight).OrderBy(id => id), file.Talents.Select(talent => talent.Id).OrderBy(id => id));
        Assert.All(file.Talents, talent => Assert.False(talent.Enabled));
        Assert.Empty(file.ReinforceGroups());
    }

    [Fact]
    public void Every_finished_cut_talent_gets_new_text_in_both_languages_and_its_own_effects()
    {
        var file = DefaultFile();

        foreach (var id in CutTalentsFinishedByTheMod)
        {
            var talent = file.Talents.Single(entry => entry.Id == id);
            var descriptions = new[] { talent.Description }.Concat(talent.Levels?.Select(level => level.Description) ?? Enumerable.Empty<LocalText?>()).OfType<LocalText>().ToList();

            Assert.NotEmpty(descriptions);
            Assert.NotNull(talent.LevelText);
            Assert.All(descriptions, text => Assert.Contains(text.Zh, ch => ch is >= '一' and <= '鿿'));
            Assert.All(descriptions.Append(talent.LevelText).OfType<LocalText>(), text => Assert.True(text.En.All(ch => ch is >= ' ' and <= '~'), text.En));
            Assert.Equal(5, talent.Effects.Count(effect => effect.CopyFrom == null));
        }
    }

    [Fact]
    public void Every_new_reward_has_a_name_text_and_level_line_in_both_languages_and_gives_something()
    {
        var file = DefaultFile();

        foreach (var id in NewRewards)
        {
            var talent = file.Talents.Single(entry => entry.Id == id);
            var texts = new[] { talent.Name, talent.Description, talent.LevelText };

            // A level line can be numbers only, so only the name and description must read as Chinese.
            Assert.All(texts, text => Assert.NotNull(text));
            Assert.All(new[] { talent.Name, talent.Description }.OfType<LocalText>(), text => Assert.Contains(text.Zh, ch => ch is >= '一' and <= '鿿'));
            Assert.All(texts.OfType<LocalText>(), text => Assert.True(text.Zh.Length > 0 && text.En.Length > 0 && text.En.All(ch => ch is >= ' ' and <= '~'), text.En));
            Assert.NotEmpty(talent.Gives);
        }
    }

    [Fact]
    public void Each_reward_added_in_0_9_0_gives_more_at_every_level_and_costs_more_too()
    {
        var file = DefaultFile();

        foreach (var id in Enumerable.Range(5124, 11))
        {
            var talent = file.Talents.Single(entry => entry.Id == id);
            var give = Assert.Single(talent.Gives);
            var costs = talent.Levels!.Select(level => level.Cost ?? 0).ToList();
            var values = give.Levels.Select(fields => Assert.IsType<FieldValue.Number>(Assert.Single(fields).Value).Value).ToList();

            Assert.True(talent.Levels!.Count is 5 or 10, $"talent {id}");
            Assert.Equal(talent.Levels!.Count, give.Levels.Count);
            Assert.True(values[0] > 0, $"talent {id}");
            Assert.All(values.Zip(values.Skip(1)), pair => Assert.True(pair.Second > pair.First, $"talent {id}"));
            Assert.All(costs.Zip(costs.Skip(1)), pair => Assert.True(pair.Second > pair.First, $"talent {id}"));
        }
    }

    [Fact]
    public void Every_lengthened_talent_gets_five_more_levels_each_with_its_own_buff_and_effect()
    {
        var file = DefaultFile();

        foreach (var (id, gameLevels) in FiveLevelTalentsMadeTen.Select(id => (id, 5)).Concat(ThreeLevelTalentsMadeEight.Select(id => (id, 3))))
        {
            var talent = file.Talents.Single(entry => entry.Id == id);
            var added = talent.Levels!.Skip(gameLevels).ToList();

            Assert.Equal(gameLevels + 5, talent.Levels!.Count);
            Assert.All(added, level => Assert.True(level.Cost > 0 && level.Buff is not null, $"talent {id}"));
            Assert.All(added, level => Assert.Contains(talent.Buffs, buff => buff.Id == level.Buff && buff.CopyFrom is not null));
            Assert.Equal(5, talent.Buffs.Select(buff => buff.Id).Distinct().Count());
            Assert.All(talent.Buffs, buff => Assert.Contains(buff.Fields, set => set.Field == "EffectConfigId" && talent.Effects.Any(effect => set.Value == new FieldValue.Number(effect.Id))));
            // Quick Fortification's game text names no numbers, so it keeps it.
            if (id != 2005) Assert.NotNull(talent.LevelText);
        }
    }

    [Fact]
    public void Enabling_every_default_talent_builds_without_a_problem()
    {
        var file = DefaultFile();
        var enabled = file with { Talents = file.Talents.Select(talent => talent with { Enabled = true }).ToList() };
        var plan = TalentPlan.Build(enabled, new GameLikeTables());

        Assert.Empty(plan.Problems);
        Assert.Equal(5, Assert.Single(enabled.ReinforceGroups()).Count);
    }

    /// <summary>The rows of the game's tables the default file relies on.</summary>
    private sealed class GameLikeTables : ITableIndex
    {
        public bool HasEffect(int id) => id == 200000009 || id / 100000 == 2001 || id / 100000 == 2002;

        // Plus the game's own talent level buffs, TalentID * 100 + level * 10 for levels 1 to 5.
        public bool HasBuff(int id) => id == 9 || (id is >= 100000 and < 400000 && id % 10 == 0 && id % 100 / 10 is >= 1 and <= 5);

        // Every script the default file's gives use, and the empty one New Loop Plus's own effects ride on.
        private static readonly string[] GameScripts =
        {
            "AE_BackpackShelfLife", "AE_CompressValueRate", "AE_ExposureReduceMultiplier", "AE_PowerSolarEfficiency", "AE_ProductionExpMultiplier",
            "AE_TrapCaptureMultiplier", "AddItemToBag_307510", "EndlessRobotGrant_HK", "PreDisaster_AddCountDownTime", "PreDisaster_AddWeight", "QuickRest", "Buff_Empty",
            "AE_BrewSpeedUp", "AE_BrewBottleYield", "AE_StrangerTradeDroneExtraTrip", "AE_TowerBiteDamageReduce",
            "AE_DroneForagingExtraTrip", "AE_ProductionSpeedMultiplier", "AE_PowerGeneratorEfficiency",
        };

        public System.Collections.Generic.IReadOnlyList<int> TalentLevels(int talentId) =>
            talentId >= 5000 ? Array.Empty<int>() : talentId is 2017 or 4004 or 4006 ? new[] { 1 } : talentId is 3076 or 1011 or 1012 or 2011 or 2013 or 2014 or 2015 or 2016 or 2018 ? new[] { 1, 2, 3 } : new[] { 1, 2, 3, 4, 5 };

        public GiveTemplate? Template(string script, int talentId, int lv) => GameScripts.Contains(script) ? new GiveTemplate(200000009, 9) : null;

        public int? LevelBuff(int talentId, int lv) => null;
    }
}
