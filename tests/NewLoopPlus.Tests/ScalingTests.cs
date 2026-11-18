using System.Collections.Generic;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>The points a run earns and what loop rewards cost follow the player's multipliers.</summary>
public class ScalingTests
{
    [Fact]
    public void Each_part_is_multiplied_by_its_own_setting_and_by_the_overall_one()
    {
        var multipliers = new PointsMultipliers(Overall: 2, Survival: 1.5, Crisis: 3, Record: 0.5);

        Assert.Equal(0.3f, multipliers.SurvivalRate(0.1f), 5);
        Assert.Equal(60, multipliers.EventReward(10));
        Assert.Equal(5, multipliers.RecordPerDay(5));
    }

    [Fact]
    public void Crisis_penalties_are_never_scaled()
    {
        var multipliers = new PointsMultipliers(Overall: 1, Survival: 1, Crisis: 3, Record: 1);

        Assert.Equal(-4, multipliers.EventReward(-4));
        Assert.Equal(0, multipliers.EventReward(0));
    }

    [Fact]
    public void A_negative_setting_earns_nothing_rather_than_taking_points_away()
    {
        var multipliers = new PointsMultipliers(Overall: 1, Survival: -2, Crisis: -2, Record: -2);

        Assert.Equal(0f, multipliers.SurvivalRate(0.1f));
        Assert.Equal(0, multipliers.EventReward(10));
        Assert.Equal(0, multipliers.RecordPerDay(5));
    }

    [Fact]
    public void Multipliers_that_all_come_to_1_change_nothing()
    {
        Assert.False(PointsMultipliers.None.ChangesAnything);
        Assert.False(new PointsMultipliers(Overall: 2, Survival: 0.5, Crisis: 0.5, Record: 0.5).ChangesAnything);
        Assert.True(new PointsMultipliers(Overall: 1, Survival: 1, Crisis: 1, Record: 2).ChangesAnything);
    }

    [Theory]
    [InlineData(5, 0.5, 3)]
    [InlineData(7, 0.5, 4)]
    [InlineData(1, 0.4, 0)]
    public void Whole_points_round_halves_up(int original, double multiplier, int expected)
    {
        Assert.Equal(expected, new PointsMultipliers(1, 1, 1, multiplier).RecordPerDay(original));
    }

    [Fact]
    public void A_talent_with_its_own_multiplier_ignores_the_overall_one()
    {
        var costs = new CostMultipliers(Overall: 2, PerTalent: new Dictionary<int, double> { [1001] = 0.5 });

        Assert.Equal(50, costs.Cost(1001, 100));
        Assert.Equal(200, costs.Cost(1002, 100));
    }

    [Fact]
    public void A_cost_multiplier_of_0_makes_upgrades_free()
    {
        Assert.Equal(0, new CostMultipliers(0, new Dictionary<int, double>()).Cost(1001, 120));
    }

    [Fact]
    public void Overrides_alone_count_as_a_change_even_at_an_overall_of_1()
    {
        Assert.False(CostMultipliers.None.ChangesAnything);
        Assert.True(new CostMultipliers(1, new Dictionary<int, double> { [1001] = 1 }).ChangesAnything);
    }
}

/// <summary>The per-talent cost setting is typed by hand, so one bad entry must not lose the rest.</summary>
public class CostOverridesTests
{
    [Fact]
    public void Pairs_are_read_with_spaces_and_decimal_points()
    {
        var parsed = CostOverrides.Parse(" 1001:0.5, 2017 : 2 ");

        Assert.Equal(new Dictionary<int, double> { [1001] = 0.5, [2017] = 2 }, parsed.PerTalent);
        Assert.Empty(parsed.Rejected);
    }

    [Fact]
    public void Bad_entries_are_skipped_and_reported_while_good_ones_still_count()
    {
        var parsed = CostOverrides.Parse("1001:0.5, abc, 2017:-1, 3000, 4001:x, 0:2, 4004:1:2,, 4006:NaN");

        Assert.Equal(new Dictionary<int, double> { [1001] = 0.5 }, parsed.PerTalent);
        Assert.Equal(new[] { "abc", "2017:-1", "3000", "4001:x", "0:2", "4004:1:2", "4006:NaN" }, parsed.Rejected);
    }

    [Fact]
    public void A_talent_listed_twice_keeps_its_last_value()
    {
        Assert.Equal(3, CostOverrides.Parse("1001:2, 1001:3").PerTalent[1001]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void An_empty_setting_overrides_nothing(string? text)
    {
        var parsed = CostOverrides.Parse(text);

        Assert.Empty(parsed.PerTalent);
        Assert.Empty(parsed.Rejected);
    }
}

/// <summary>The tables are checked again and again, so a value must be scaled once and never compound.</summary>
public class ScaledValuesTests
{
    private static ScaledValues<int, int> Doubling() => new((_, value) => value * 2);

    [Fact]
    public void The_first_value_seen_is_scaled()
    {
        var values = Doubling();

        Assert.Equal((int?)20, values.Next(1, 10));
        Assert.Equal((int?)10, values.Original(1));
    }

    [Fact]
    public void A_value_already_written_is_left_alone_and_not_scaled_again()
    {
        var values = Doubling();

        values.Next(1, 10);

        Assert.Null(values.Next(1, 20));
        Assert.Null(values.Next(1, 20));
        Assert.Equal((int?)20, values.Scaled(1));
    }

    [Fact]
    public void A_reloaded_table_value_is_taken_as_the_game_s_and_scaled_again()
    {
        var values = Doubling();

        values.Next(1, 10);
        values.Next(1, 20);

        Assert.Equal((int?)14, values.Next(1, 7));
        Assert.Equal((int?)7, values.Original(1));
    }

    [Fact]
    public void A_value_the_scale_leaves_the_same_needs_no_write()
    {
        Assert.Null(Doubling().Next(1, 0));
    }

    /// <summary>Mod Manager is told about a value only while it differs from the game's.</summary>
    [Fact]
    public void A_value_counts_as_changed_only_while_it_differs_from_the_game_s()
    {
        var factor = 2;
        var values = new ScaledValues<int, int>((_, value) => value * factor);

        values.Next(1, 10);

        Assert.True(values.IsChanged(1));

        factor = 1;
        values.Rescale();
        values.Next(1, 20);

        Assert.False(values.IsChanged(1));
        Assert.False(values.IsChanged(2));
    }

    [Fact]
    public void Nothing_is_known_about_a_key_before_it_is_seen()
    {
        Assert.Null(Doubling().Scaled(1));
        Assert.Null(Doubling().Original(1));
    }

    [Fact]
    public void A_changed_multiplier_scales_the_game_s_own_value_and_never_compounds()
    {
        var factor = 2;
        var values = new ScaledValues<int, int>((_, value) => value * factor);

        values.Next(1, 10);
        factor = 3;
        values.Rescale();

        Assert.Equal((int?)30, values.Next(1, 20));
        Assert.Null(values.Next(1, 30));
        Assert.Equal((int?)10, values.Original(1));
    }

    [Fact]
    public void Two_changes_before_the_next_check_still_replace_the_value_in_the_table()
    {
        var factor = 2;
        var values = new ScaledValues<int, int>((_, value) => value * factor);

        values.Next(1, 10);
        factor = 3;
        values.Rescale();
        factor = 5;
        values.Rescale();

        Assert.Equal((int?)50, values.Next(1, 20));
        Assert.Equal((int?)10, values.Original(1));
    }

    [Fact]
    public void A_reload_after_a_change_is_taken_as_the_game_s_new_value()
    {
        var factor = 2;
        var values = new ScaledValues<int, int>((_, value) => value * factor);

        values.Next(1, 10);
        factor = 3;
        values.Rescale();
        values.Next(1, 20);

        Assert.Equal((int?)21, values.Next(1, 7));
        Assert.Equal((int?)7, values.Original(1));
    }

    [Fact]
    public void Once_replaced_the_old_value_counts_as_a_reload_again()
    {
        var factor = 2;
        var values = new ScaledValues<int, int>((_, value) => value * factor);

        values.Next(1, 10);
        factor = 3;
        values.Rescale();
        values.Next(1, 20);

        Assert.Equal((int?)60, values.Next(1, 20));
        Assert.Equal((int?)20, values.Original(1));
    }
}

/// <summary>A new endless game starts with the game's 500 points times the player's setting.</summary>
public class EndlessGrantTests
{
    [Theory]
    [InlineData(2, 1000)]
    [InlineData(0.5, 250)]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    public void The_starting_points_follow_their_own_multiplier(double multiplier, int expected)
    {
        Assert.Equal(expected, new PointsMultipliers(1, 1, 1, 1, multiplier).EndlessGrant(500));
    }

    [Fact]
    public void The_overall_multiplier_does_not_touch_the_starting_points()
    {
        Assert.Equal(500, new PointsMultipliers(3, 1, 1, 1).EndlessGrant(500));
    }

    [Fact]
    public void A_starting_points_setting_alone_counts_as_a_change()
    {
        Assert.True(new PointsMultipliers(1, 1, 1, 1, 2).ChangesAnything);
    }
}

/// <summary>The loop bonus is paid once, as the loop reward screen opens after a run.</summary>
public class StartingBonusTests
{
    [Fact]
    public void The_bonus_is_paid_when_the_screen_opens_after_a_run()
    {
        Assert.Equal((int?)50, StartingBonus.Due(StartingBonus.AfterRunMode, isAlreadyWaiting: false, 50));
    }

    [Fact]
    public void Reopening_the_screen_in_the_same_loop_pays_nothing_more()
    {
        Assert.Null(StartingBonus.Due(StartingBonus.AfterRunMode, isAlreadyWaiting: true, 50));
    }

    [Fact]
    public void Opening_the_screen_from_the_menu_pays_nothing()
    {
        Assert.Null(StartingBonus.Due(1, isAlreadyWaiting: false, 50));
    }

    [Fact]
    public void A_bonus_of_0_pays_nothing()
    {
        Assert.Null(StartingBonus.Due(StartingBonus.AfterRunMode, isAlreadyWaiting: false, 0));
    }

    [Fact]
    public void A_huge_bonus_is_capped()
    {
        Assert.Equal((int?)StartingBonus.Max, StartingBonus.Due(StartingBonus.AfterRunMode, isAlreadyWaiting: false, int.MaxValue));
    }
}

/// <summary>The game caches two of the scaled values the first time it reads them.</summary>
public class GameCacheTests
{
    [Fact]
    public void A_cache_holding_the_game_s_value_is_set_to_the_scaled_one()
    {
        Assert.Equal((float?)0.2f, GameCache.Next(0.1f, (float?)0.2f));
    }

    [Fact]
    public void An_empty_cache_is_left_for_the_game_to_fill_from_the_scaled_table()
    {
        Assert.Null(GameCache.Next(0, (int?)10));
    }

    [Fact]
    public void A_cache_already_holding_the_scaled_value_is_left_alone()
    {
        Assert.Null(GameCache.Next(10, (int?)10));
    }

    [Fact]
    public void Nothing_is_written_before_the_table_was_scaled()
    {
        Assert.Null(GameCache.Next(5, (int?)null));
    }
}
