using System.Collections.Generic;
using System.Linq;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>The talents file turns into the right changes to the game's tables, and never into a broken row.</summary>
public class TalentPlanTests
{
    /// <summary>The game's tables as the plan sees them: talent 1006 has 5 levels, 3076 has 3.</summary>
    private sealed class FakeTables : ITableIndex
    {
        public HashSet<int> Effects { get; } = new() { 200000009, 200100610 };

        public HashSet<int> Buffs { get; } = new() { 9, 100610 };

        public Dictionary<int, int[]> Talents { get; } = new() { [1006] = new[] { 1, 2, 3, 4, 5 }, [3076] = new[] { 1, 2, 3 } };

        public bool HasEffect(int id) => Effects.Contains(id);

        public bool HasBuff(int id) => Buffs.Contains(id);

        public IReadOnlyList<int> TalentLevels(int talentId) => Talents.TryGetValue(talentId, out var levels) ? levels : new int[0];

        /// <summary>Talent 1006 runs VehicleTimeReduce; PreDisaster_AddGold is run only by talent 1002's first level; talent 1009 runs the do-nothing Buff_Empty.</summary>
        public GiveTemplate? Template(string script, int talentId, int lv) => script switch
        {
            "VehicleTimeReduce" => new GiveTemplate(200100610, 100610),
            "PreDisaster_AddGold" => new GiveTemplate(200100210, 100210),
            "Buff_Empty" => new GiveTemplate(200100910, 100910),
            _ => null,
        };

        /// <summary>A level whose game buff is not the usual TalentID * 100 + level * 10, keyed by its talent row ID.</summary>
        public Dictionary<int, int> OddLevelBuffs { get; } = new();

        public int? LevelBuff(int talentId, int lv) =>
            OddLevelBuffs.TryGetValue(talentId * 10 + lv, out var odd) ? odd : talentId == 1006 && lv <= 5 ? 100600 + lv * 10 : null;
    }

    private static TalentPlan Plan(string json) => Plan(json, new FakeTables());

    private static TalentPlan Plan(string json, FakeTables tables) => TalentPlan.Build(TalentFile.Parse(json), tables);

    private static FieldValue? Field(RowEdit row, string field) => row.Fields.LastOrDefault(set => set.Field == field)?.Value;

    private static RowEdit Row(TalentPlan plan, Table table, int id) => Assert.Single(plan.Rows, row => row.Table == table && row.Id == id);

    [Fact]
    public void A_disabled_talent_changes_nothing_not_even_its_own_buffs_and_effects()
    {
        var plan = Plan("""
            { "talents": [ { "id": 1006, "enabled": false, "levels": [ { "cost": 1 } ],
              "effects": [ { "id": 200100610, "fields": { "FloatValue0": 0.5 } } ],
              "buffs": [ { "id": 100610, "fields": { "IsHide": false } } ] } ] }
            """);

        Assert.Empty(plan.Rows);
        Assert.Empty(plan.Texts);
        Assert.Empty(plan.Problems);
    }

    [Fact]
    public void Enabling_a_cut_talent_makes_every_one_of_its_levels_available()
    {
        var plan = Plan("""{ "talents": [ { "id": 1006, "enabled": true } ] }""");

        Assert.Equal(new[] { 10061, 10062, 10063, 10064, 10065 }, plan.Rows.Select(row => row.Id));
        Assert.All(plan.Rows, row => Assert.Equal(new FieldValue.Number(1), Field(row, "Available")));
        Assert.All(plan.Rows, row => Assert.Null(row.CopyFrom));
    }

    [Fact]
    public void Costs_and_buffs_from_the_file_land_on_their_own_level()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "levels": [ { "cost": 5 }, { "cost": 6, "buff": 9 }, {} ] } ] }""");

        Assert.Equal(new FieldValue.Number(5), Field(Row(plan, Table.Talent, 30761), "LvUpCost"));
        Assert.Equal(new FieldValue.Number(6), Field(Row(plan, Table.Talent, 30762), "LvUpCost"));
        Assert.Equal(new FieldValue.Number(9), Field(Row(plan, Table.Talent, 30762), "BuffID"));
        Assert.Null(Field(Row(plan, Table.Talent, 30763), "LvUpCost"));
    }

    [Fact]
    public void An_extra_level_is_a_new_row_copied_from_the_highest_level_the_game_has()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "levels": [ {}, {}, {}, { "cost": 50 } ] } ] }""");
        var added = Row(plan, Table.Talent, 30764);

        Assert.Equal(30763, added.CopyFrom);
        Assert.Equal(new FieldValue.Number(30764), Field(added, "ID"));
        Assert.Equal(new FieldValue.Number(3076), Field(added, "TalentID"));
        Assert.Equal(new FieldValue.Number(4), Field(added, "Lv"));
        Assert.Equal(new FieldValue.Number(50), Field(added, "LvUpCost"));
    }

    [Fact]
    public void Listing_fewer_levels_than_the_game_has_hides_the_rest()
    {
        var plan = Plan("""{ "talents": [ { "id": 1006, "levels": [ {}, {} ] } ] }""");

        Assert.Equal(new FieldValue.Number(1), Field(Row(plan, Table.Talent, 10062), "Available"));
        Assert.Equal(new FieldValue.Number(0), Field(Row(plan, Table.Talent, 10063), "Available"));
        Assert.Equal(new FieldValue.Number(0), Field(Row(plan, Table.Talent, 10065), "Available"));
    }

    [Fact]
    public void A_new_talent_copies_its_rows_from_the_talent_it_names()
    {
        var plan = Plan("""{ "talents": [ { "id": 5001, "copyFrom": 3076, "levels": [ {}, {}, {}, {} ] } ] }""");

        Assert.Equal(new int?[] { 30761, 30762, 30763, 30763 }, plan.Rows.Select(row => row.CopyFrom));
        Assert.Equal(new[] { 50011, 50012, 50013, 50014 }, plan.Rows.Select(row => row.Id));
        Assert.All(plan.Rows, row => Assert.Equal(new FieldValue.Number(5001), Field(row, "TalentID")));
    }

    [Fact]
    public void A_new_talent_without_a_source_is_reported_and_left_out()
    {
        var plan = Plan("""{ "talents": [ { "id": 5001 }, { "id": 5002, "copyFrom": 9999 }, { "id": 1006 } ] }""");

        Assert.All(plan.Rows, row => Assert.Equal(1006, row.Id / 10));
        Assert.Equal(2, plan.Problems.Count);
        Assert.Contains(plan.Problems, problem => problem.Contains("5001"));
        Assert.Contains(plan.Problems, problem => problem.Contains("9999"));
    }

    [Fact]
    public void More_than_nine_levels_is_refused_because_row_ids_would_run_into_the_next_talent()
    {
        var levels = string.Join(",", Enumerable.Repeat("{}", 10));
        var plan = Plan($$"""{ "talents": [ { "id": 1006, "levels": [ {{levels}} ] } ] }""");

        Assert.Empty(plan.Rows);
        Assert.Contains("1006", Assert.Single(plan.Problems));
    }

    [Fact]
    public void Talent_text_gets_keys_of_its_own_and_a_level_description_beats_the_talent_one()
    {
        var plan = Plan("""
            { "talents": [ { "id": 3076, "name": { "en": "Pathfinder", "zh": "开路人" }, "description": "Every level",
              "levels": [ {}, { "description": { "en": "Second", "zh": "第二" } }, {} ] } ] }
            """);
        var second = Row(plan, Table.Talent, 30762);
        var nameKey = Assert.IsType<FieldValue.Text>(Field(second, "Name")).Value;
        var descriptionKey = Assert.IsType<FieldValue.Text>(Field(second, "Dec")).Value;
        var firstDescriptionKey = Assert.IsType<FieldValue.Text>(Field(Row(plan, Table.Talent, 30761), "Dec")).Value;

        Assert.Equal(new TextEntry(nameKey, "Pathfinder", "开路人"), plan.Texts.Single(text => text.Key == nameKey));
        Assert.Equal(new TextEntry(descriptionKey, "Second", "第二"), plan.Texts.Single(text => text.Key == descriptionKey));
        Assert.Equal(new TextEntry(firstDescriptionKey, "Every level", "Every level"), plan.Texts.Single(text => text.Key == firstDescriptionKey));
        Assert.Equal(6, plan.Texts.Select(text => text.Key).Distinct().Count());
    }

    [Fact]
    public void New_effects_come_before_the_buffs_that_use_them_and_both_before_talents()
    {
        var plan = Plan("""
            { "talents": [ { "id": 1006, "levels": [ { "buff": 7001 } ],
              "buffs": [ { "id": 7001, "copyFrom": 9, "fields": { "EffectConfigId": 8001 } } ],
              "effects": [ { "id": 8001, "copyFrom": 200000009, "fields": { "IntValue0": 300 } } ] } ] }
            """);

        Assert.Equal(new[] { Table.Effect, Table.Buff, Table.Talent }, plan.Rows.Take(3).Select(row => row.Table));
        Assert.Equal(new FieldValue.Number(8001), Field(plan.Rows[0], "ID"));
        Assert.Equal(new FieldValue.Number(7001), Field(plan.Rows[1], "ID"));
        Assert.Empty(plan.Problems);
    }

    [Fact]
    public void A_buff_or_effect_the_game_lacks_needs_a_copy_source_that_exists()
    {
        var plan = Plan("""
            { "buffs": [ { "id": 7001 }, { "id": 7002, "copyFrom": 12345 }, { "id": 9, "fields": { "IsHide": true } } ],
              "effects": [ { "id": 8001 } ] }
            """);

        Assert.Equal(9, Assert.Single(plan.Rows).Id);
        Assert.Equal(3, plan.Problems.Count);
    }

    [Fact]
    public void A_give_makes_an_effect_and_a_buff_per_level_numbered_after_the_talent_and_copied_from_the_script_template()
    {
        var plan = Plan("""
            { "talents": [ { "id": 3076, "gives": [ { "effect": "game:PreDisaster_AddGold", "levels": [ { "IntValue0": 100 }, { "IntValue0": 200 } ] } ] } ] }
            """);
        var effect = Row(plan, Table.Effect, 200307610);
        var buff = Row(plan, Table.Buff, 307610);

        Assert.Empty(plan.Problems);
        Assert.Equal(200100210, effect.CopyFrom);
        Assert.Equal(new FieldValue.Number(200307610), Field(effect, "ID"));
        Assert.Equal(new FieldValue.Number(100), Field(effect, "IntValue0"));
        Assert.Equal(new FieldValue.Number(200), Field(Row(plan, Table.Effect, 200307620), "IntValue0"));
        Assert.Equal(new FieldValue.Number(200), Field(Row(plan, Table.Effect, 200307630), "IntValue0"));
        Assert.Equal(100210, buff.CopyFrom);
        Assert.Equal(new FieldValue.Number(200307610), Field(buff, "EffectConfigId"));
        Assert.Equal(new FieldValue.Flag(true), Field(buff, "IsHide"));
        Assert.Equal(new FieldValue.Number(307610), Field(Row(plan, Table.Talent, 30761), "BuffID"));
        Assert.Equal(new FieldValue.Number(307630), Field(Row(plan, Table.Talent, 30763), "BuffID"));
        Assert.Empty(plan.Companions);
    }

    [Fact]
    public void Every_give_after_the_first_rides_along_with_the_level_buff_and_keeps_its_choice_at_every_level()
    {
        var plan = Plan("""
            { "talents": [ { "id": 3076, "levels": [ {}, {} ], "gives": [
              { "effect": "game:PreDisaster_AddGold" },
              { "effect": "game:PreDisaster_AddGold", "choice": { "StrValue0": "Production" } },
              { "effect": "game:VehicleTimeReduce" } ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new[] { 307610, 307620 }, plan.Companions.Select(companion => companion.LevelBuff));
        Assert.Equal(new[] { 307611, 307612 }, plan.Companions[0].Buffs);
        Assert.Equal(new[] { 307621, 307622 }, plan.Companions[1].Buffs);
        Assert.Equal(new FieldValue.Text("Production"), Field(Row(plan, Table.Effect, 200307621), "StrValue0"));
        Assert.Equal(200100610, Row(plan, Table.Effect, 200307622).CopyFrom);
    }

    [Fact]
    public void The_first_give_may_take_over_the_level_buff_the_game_already_gives()
    {
        var plan = Plan("""{ "talents": [ { "id": 1006, "gives": [ { "effect": "game:PreDisaster_AddGold", "levels": [ { "IntValue0": 50 } ] } ] } ] }""");

        Assert.Empty(plan.Problems);
        Assert.Equal(200100210, Row(plan, Table.Effect, 200100610).CopyFrom);
        Assert.Equal(new FieldValue.Number(100650), Field(Row(plan, Table.Talent, 10065), "BuffID"));
    }

    [Fact]
    public void A_give_that_would_overwrite_a_game_row_skips_the_whole_talent()
    {
        var tables = new FakeTables();

        tables.Buffs.Add(100631);

        var plan = Plan("""{ "talents": [ { "id": 1006, "gives": [ { "effect": "game:VehicleTimeReduce" }, { "effect": "game:VehicleTimeReduce" } ] } ] }""", tables);

        Assert.Empty(plan.Rows);
        Assert.Empty(plan.Companions);
        Assert.Contains("100631", Assert.Single(plan.Problems));
    }

    [Fact]
    public void Only_the_first_give_may_take_over_the_level_buff()
    {
        var tables = new FakeTables();

        tables.Buffs.Add(307611);
        tables.OddLevelBuffs[30761] = 307611;

        var plan = Plan("""{ "talents": [ { "id": 3076, "gives": [ { "effect": "game:VehicleTimeReduce" }, { "effect": "game:VehicleTimeReduce" } ] } ] }""", tables);

        Assert.Empty(plan.Rows);
        Assert.Contains("307611", Assert.Single(plan.Problems));
    }

    [Fact]
    public void A_give_cannot_reuse_a_buff_the_file_already_makes()
    {
        var plan = Plan("""
            { "buffs": [ { "id": 307610, "copyFrom": 9 } ],
              "talents": [ { "id": 3076, "gives": [ { "effect": "game:PreDisaster_AddGold" } ] } ] }
            """);

        Assert.Equal(307610, Assert.Single(plan.Rows).Id);
        Assert.Contains("307610", Assert.Single(plan.Problems));
    }

    [Theory]
    [InlineData("game:NoSuchScript", "NoSuchScript")]
    [InlineData("mod:Example", "mod:Example")]
    public void A_give_the_game_cannot_run_skips_the_talent_and_names_the_effect(string effect, string named)
    {
        var plan = Plan($$"""{ "talents": [ { "id": 3076, "gives": [ { "effect": "game:PreDisaster_AddGold" }, { "effect": "{{effect}}" } ] } ] }""");

        Assert.Empty(plan.Rows);
        Assert.Contains(named, Assert.Single(plan.Problems));
    }

    [Fact]
    public void A_New_Loop_Plus_give_runs_the_do_nothing_script_and_keeps_its_numbers_for_the_mod()
    {
        var plan = Plan("""
            { "talents": [ { "id": 3076, "gives": [ { "effect": "nlp:interest", "levels": [ { "FloatValue0": 0.05 }, { "FloatValue0": 0.1 } ] } ] } ] }
            """);
        var effect = Row(plan, Table.Effect, 200307610);
        var grant = Assert.Single(plan.Grants);

        Assert.Empty(plan.Problems);
        Assert.Equal(200100910, effect.CopyFrom);
        Assert.Equal(100910, Row(plan, Table.Buff, 307610).CopyFrom);
        Assert.Null(Field(effect, "FloatValue0"));
        Assert.Equal(NlpEffect.Interest, grant.Effect);
        Assert.Equal(3076, grant.TalentId);
        Assert.Equal(3, grant.Levels.Count);
        Assert.Equal(0.05, NlpPerks.Value(plan.Grants, _ => 1, NlpEffect.Interest, NlpPerks.Share), 6);
        Assert.Equal(0.1, NlpPerks.Value(plan.Grants, _ => 3, NlpEffect.Interest, NlpPerks.Share), 6);
    }

    [Fact]
    public void A_New_Loop_Plus_effect_the_mod_does_not_have_skips_the_talent_and_lists_the_ones_it_has()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "gives": [ { "effect": "nlp:no-such-perk" } ] } ] }""");
        var problem = Assert.Single(plan.Problems);

        Assert.Empty(plan.Rows);
        Assert.Empty(plan.Grants);
        Assert.Contains("nlp:no-such-perk", problem);
        Assert.Contains("nlp:second-wind", problem);
    }

    [Fact]
    public void Game_and_New_Loop_Plus_gives_mix_on_one_talent()
    {
        var plan = Plan("""
            { "talents": [ { "id": 3076, "gives": [ { "effect": "game:PreDisaster_AddGold", "levels": [ { "IntValue0": 100 } ] }, { "effect": "nlp:safety-net", "levels": [ { "IntValue0": 200 } ] } ] } ] }
            """);

        Assert.Empty(plan.Problems);
        Assert.Equal(new FieldValue.Number(100), Field(Row(plan, Table.Effect, 200307610), "IntValue0"));
        Assert.Equal(200100910, Row(plan, Table.Effect, 200307611).CopyFrom);
        Assert.Equal(NlpEffect.SafetyNet, Assert.Single(plan.Grants).Effect);
    }

    [Fact]
    public void A_disabled_talent_grants_no_New_Loop_Plus_effect()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "enabled": false, "gives": [ { "effect": "nlp:extra-pick", "levels": [ { "IntValue0": 1 } ] } ] } ] }""");

        Assert.Empty(plan.Grants);
    }

    [Fact]
    public void Gives_need_a_four_digit_talent_id_and_at_most_ten_of_them()
    {
        var eleven = string.Join(",", Enumerable.Repeat("""{ "effect": "game:PreDisaster_AddGold" }""", 11));
        var plan = Plan($$"""
            { "talents": [ { "id": 31271, "copyFrom": 3076, "gives": [ { "effect": "game:PreDisaster_AddGold" } ] },
              { "id": 1006, "gives": [ {{eleven}} ] } ] }
            """);

        Assert.Empty(plan.Rows);
        Assert.Equal(2, plan.Problems.Count);
        Assert.Contains(plan.Problems, problem => problem.Contains("31271"));
        Assert.Contains(plan.Problems, problem => problem.Contains("11 gives"));
    }

    [Fact]
    public void A_level_buff_written_next_to_gives_is_ignored_and_reported()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "levels": [ { "buff": 9 } ], "gives": [ { "effect": "game:PreDisaster_AddGold" } ] } ] }""");

        Assert.Equal(new FieldValue.Number(307610), Field(Row(plan, Table.Talent, 30761), "BuffID"));
        Assert.Contains("ignored", Assert.Single(plan.Problems));
    }

    [Fact]
    public void A_level_pointing_at_a_buff_nobody_has_is_written_but_reported()
    {
        var plan = Plan("""{ "talents": [ { "id": 3076, "levels": [ { "buff": 424242 }, {}, {} ] } ] }""");

        Assert.Equal(new FieldValue.Number(424242), Field(Row(plan, Table.Talent, 30761), "BuffID"));
        Assert.Contains("424242", Assert.Single(plan.Problems));
    }
}
