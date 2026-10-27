using System.Collections.Generic;
using System.Linq;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>A New Loop Plus effect is as strong as the levels the player owns of the talents that give it.</summary>
public class NlpPerksTests
{
    private static NlpGrant Grant(string effect, int talentId, params double[] perLevel) =>
        new(effect, talentId, perLevel.Select(value => (IReadOnlyList<FieldSet>)new[] { new FieldSet(NlpPerks.Share, new FieldValue.Number(value)) }).ToList());

    [Fact]
    public void The_level_owned_picks_the_value()
    {
        var grants = new[] { Grant(NlpEffect.Interest, 5101, 0.05, 0.1, 0.15) };

        Assert.Equal(0.1, NlpPerks.Value(grants, _ => 2, NlpEffect.Interest, NlpPerks.Share), 6);
    }

    [Fact]
    public void A_talent_not_owned_adds_nothing()
    {
        var grants = new[] { Grant(NlpEffect.Interest, 5101, 0.05) };

        Assert.Equal(0, NlpPerks.Value(grants, _ => 0, NlpEffect.Interest, NlpPerks.Share));
    }

    [Fact]
    public void A_level_past_the_last_one_listed_reads_the_last()
    {
        var grants = new[] { Grant(NlpEffect.Interest, 5101, 0.05, 0.1) };

        Assert.Equal(0.1, NlpPerks.Value(grants, _ => 7, NlpEffect.Interest, NlpPerks.Share), 6);
    }

    [Fact]
    public void Two_talents_giving_the_same_effect_add_up_and_other_effects_stay_out()
    {
        var grants = new[] { Grant(NlpEffect.LuckyFinds, 5101, 0.1), Grant(NlpEffect.LuckyFinds, 5102, 0.2), Grant(NlpEffect.MuscleMemory, 5103, 0.5) };

        Assert.Equal(0.3, NlpPerks.Value(grants, _ => 1, NlpEffect.LuckyFinds, NlpPerks.Share), 6);
    }

    [Fact]
    public void A_level_that_leaves_the_field_out_adds_nothing()
    {
        var grants = new[] { Grant(NlpEffect.SecondWind, 5101, 0.5) };

        Assert.Equal(0, NlpPerks.Value(grants, _ => 1, NlpEffect.SecondWind, NlpPerks.Whole));
    }
}

/// <summary>The single decisions behind Interest, Safety Net, Second Wind, Lucky Finds and Scout's Notes.</summary>
public class LoopPerksTests
{
    [Theory]
    [InlineData(1000, 0.05, 50)]
    [InlineData(10, 0.05, 1)]
    [InlineData(0, 0.05, 0)]
    [InlineData(-200, 0.05, 0)]
    [InlineData(1000, 0, 0)]
    public void Interest_pays_the_share_of_the_points_kept_rounded(int kept, double rate, int paid)
    {
        Assert.Equal(paid, LoopPerks.Interest(kept, rate));
    }

    [Fact]
    public void Interest_is_capped()
    {
        Assert.Equal(LoopPerks.InterestMax, LoopPerks.Interest(int.MaxValue, 1));
    }

    [Theory]
    [InlineData(50, 200, 150)]
    [InlineData(250, 200, 0)]
    [InlineData(-30, 200, 200)]
    [InlineData(50, 0, 0)]
    public void Safety_Net_tops_a_run_up_to_its_floor_and_never_takes_points(int settled, double floor, int topUp)
    {
        Assert.Equal(topUp, LoopPerks.SafetyNet(settled, floor));
    }

    [Fact]
    public void Second_Wind_saves_the_player_whose_Vitality_ran_out_while_uses_are_left()
    {
        Assert.True(LoopPerks.TurnsDeathAway(isLeader: true, isAlreadyDead: false, isGameOver: false, vitality: 0, usesPerRun: 1, usesSpent: 0));
    }

    [Theory]
    [InlineData(false, false, false, 0.0, 1.0, 0)]
    [InlineData(true, true, false, 0.0, 1.0, 0)]
    [InlineData(true, false, true, 0.0, 1.0, 0)]
    [InlineData(true, false, false, 12.0, 1.0, 0)]
    [InlineData(true, false, false, 0.0, 1.0, 1)]
    [InlineData(true, false, false, 0.0, 0.0, 0)]
    public void Second_Wind_lets_every_other_death_happen(bool isLeader, bool isAlreadyDead, bool isGameOver, double vitality, double usesPerRun, int usesSpent)
    {
        Assert.False(LoopPerks.TurnsDeathAway(isLeader, isAlreadyDead, isGameOver, vitality, usesPerRun, usesSpent));
    }

    [Theory]
    [InlineData(30, 30f)]
    [InlineData(0, 1f)]
    [InlineData(0.2, 1f)]
    public void Second_Wind_leaves_at_least_1_Vitality(double setting, float left)
    {
        Assert.Equal(left, LoopPerks.VitalityAfterSecondWind(setting));
    }

    [Theory]
    [InlineData(2, false, 0.3, 0.1, 3)]
    [InlineData(2, false, 0.3, 0.5, 2)]
    [InlineData(2, true, 1.0, 0.0, 2)]
    [InlineData(0, false, 1.0, 0.0, 0)]
    public void Lucky_Finds_adds_a_copy_to_searched_loot_when_the_roll_comes_under_the_chance(int copies, bool isGroundLoot, double chance, double roll, int result)
    {
        Assert.Equal(result, LoopPerks.LuckyCopies(copies, isGroundLoot, chance, roll));
    }

    [Theory]
    [InlineData(3, 0.2, 0.1, 0.0, 0)]
    [InlineData(3, 0.2, 0.1, 0.5, 1)]
    [InlineData(3, 0.2, 0.1, 0.999, 2)]
    [InlineData(3, 0.2, 0.1, 1.0, 2)]
    public void Tinkerer_gives_back_the_material_the_pick_lands_on_when_the_roll_comes_under_the_chance(int materials, double chance, double roll, double pickRoll, int picked)
    {
        Assert.Equal(picked, LoopPerks.TinkererPick(materials, chance, roll, pickRoll));
    }

    [Theory]
    [InlineData(3, 0.2, 0.2)]
    [InlineData(3, 0.2, 0.9)]
    [InlineData(3, 0.0, 0.0)]
    [InlineData(0, 1.0, 0.0)]
    public void Tinkerer_gives_nothing_back_when_the_roll_misses_or_there_is_nothing_to_give(int materials, double chance, double roll)
    {
        Assert.Null(LoopPerks.TinkererPick(materials, chance, roll, 0.5));
    }

    [Fact]
    public void Scout_s_Notes_reveals_the_first_trade_points_not_yet_found_in_the_game_s_order()
    {
        var points = new (int, string)[]
        {
            (30, "TradePointUnlock_3"), (10, "TradePointUnlock_1"), (20, "TradePointUnlock_2"), (5, "CrossTradeUnlock_1"), (40, "TradePointUnlock_4"),
        };

        var scouted = LoopPerks.ScoutedTags(points, tag => tag == "TradePointUnlock_1", 2);

        Assert.Equal(new[] { "TradePointUnlock_2", "TradePointUnlock_3" }, scouted);
    }

    [Fact]
    public void Scout_s_Notes_with_no_count_reveals_nothing()
    {
        Assert.Empty(LoopPerks.ScoutedTags(new[] { (1, "TradePointUnlock_1") }, _ => false, 0));
    }
}

/// <summary>Muscle Memory starts a run with a share of what the last run gained on each skill.</summary>
public class MuscleMemoryTests
{
    [Theory]
    [InlineData(1, 5, 0.5, 3)]
    [InlineData(2, 5, 0.5, 2)]
    [InlineData(1, 5, 1.0, 5)]
    [InlineData(1, 5, 3.0, 5)]
    [InlineData(1, 1, 0.5, 1)]
    [InlineData(3, 0, 0.5, 0)]
    [InlineData(4, 5, 0.0, 1)]
    public void A_skill_comes_back_to_its_start_plus_the_share_of_what_it_gained_rounded_down(int skill, int last, double share, int target)
    {
        Assert.Equal(target, MuscleMemory.Target(skill, last, share));
    }

    [Fact]
    public void The_levels_written_are_the_levels_read_back()
    {
        var levels = new Dictionary<int, int> { [1] = 4, [2] = 3, [6] = 5 };

        Assert.Equal(levels, MuscleMemory.Parse(MuscleMemory.Format(levels)));
    }

    [Fact]
    public void A_line_that_is_not_a_known_skill_and_level_is_skipped()
    {
        var levels = MuscleMemory.Parse("1=3\r\n9=5\ngarbage\n2=-1\n3=x\n4=2");

        Assert.Equal(new Dictionary<int, int> { [1] = 3, [4] = 2 }, levels);
    }
}

/// <summary>Interest and Safety Net pay where the starting bonus does, once as a new loop starts.</summary>
public class LoopStartTests
{
    [Fact]
    public void Only_the_screen_opening_after_a_run_starts_a_loop()
    {
        Assert.True(StartingBonus.IsLoopStart(StartingBonus.AfterRunMode, isAlreadyWaiting: false));
        Assert.False(StartingBonus.IsLoopStart(StartingBonus.AfterRunMode, isAlreadyWaiting: true));
        Assert.False(StartingBonus.IsLoopStart(1, isAlreadyWaiting: false));
    }
}

/// <summary>Crisis Veteran raises crisis and event rewards on top of the player's own multiplier.</summary>
public class CrisisVeteranTests
{
    [Fact]
    public void The_share_multiplies_the_crisis_setting()
    {
        var multipliers = new PointsMultipliers(Overall: 1, Survival: 1, Crisis: 2, Record: 1).WithCrisisBonus(0.25);

        Assert.Equal(250, multipliers.EventReward(100));
        Assert.Equal(10, multipliers.RecordPerDay(10));
    }

    [Fact]
    public void No_share_leaves_the_multipliers_as_they_are()
    {
        var multipliers = new PointsMultipliers(Overall: 1, Survival: 1, Crisis: 2, Record: 1);

        Assert.Same(multipliers, multipliers.WithCrisisBonus(0));
    }
}

/// <summary>Family Recipe and Drone Pilot change game settings, and with nothing owned leave the game's own.</summary>
public class PerkSettingsTests
{
    [Theory]
    [InlineData(3600, 600, 3000)]
    [InlineData(3600, 1800, 1800)]
    [InlineData(3600, 0, 3600)]
    [InlineData(600, 1800, 60)]
    [InlineData(45, 600, 45)]
    public void Drone_Pilot_shortens_trips_but_never_under_a_minute(int game, double cut, int seconds)
    {
        Assert.Equal(seconds, PerkSettings.TripSeconds(game, cut));
    }

    [Theory]
    [InlineData(3600, 0, 0.75, 900)]
    [InlineData(3600, 1800, 0.75, 450)]
    [InlineData(3600, 0, 0.15, 3060)]
    [InlineData(3600, 0, 5, 360)]
    [InlineData(3600, 0, -1, 3600)]
    [InlineData(100, 0, 0.75, 60)]
    [InlineData(45, 0, 0.75, 45)]
    public void Fast_Rotors_cuts_a_share_of_what_Drone_Pilot_leaves_at_most_90_percent_and_never_under_a_minute(int game, double cut, double share, int seconds)
    {
        Assert.Equal(seconds, PerkSettings.TripSeconds(game, cut, share));
    }

    [Theory]
    [InlineData(10, 3, 13)]
    [InlineData(10, 15, 25)]
    [InlineData(10, 0, 10)]
    [InlineData(10, -5, 10)]
    public void Family_Recipe_adds_its_points_to_a_cooking_bonus(int game, double added, int bonus)
    {
        Assert.Equal(bonus, PerkSettings.CookingBonus(game, added));
    }

    [Theory]
    [InlineData(3f, 2, 1f)]
    [InlineData(3f, 0, 3f)]
    [InlineData(3f, 5, 1f)]
    [InlineData(5f, 2, 3f)]
    [InlineData(1f, 2, 1f)]
    public void Family_Recipe_unlocks_multi_pot_cooking_sooner_but_never_below_level_1(float game, double sooner, float level)
    {
        Assert.Equal(level, PerkSettings.MultiPotLevel(game, sooner));
    }

    [Theory]
    [InlineData(1, 3, 4)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -2, 1)]
    public void Good_Neighbor_adds_its_affinity_to_each_basket(int game, double added, int affinity)
    {
        Assert.Equal(affinity, PerkSettings.AffinityPerBasket(game, added));
    }

    [Theory]
    [InlineData(150, 0.5, 75)]
    [InlineData(500, 0.25, 375)]
    [InlineData(900, 0.05, 855)]
    [InlineData(150, 0.05, 143)]
    [InlineData(900, 0, 900)]
    [InlineData(900, -0.5, 900)]
    [InlineData(500, 2.0, 50)]
    [InlineData(1, 0.5, 1)]
    public void A_vehicle_discount_lowers_the_price_but_never_takes_more_than_90_percent_off(int game, double discount, int price)
    {
        Assert.Equal(price, PerkSettings.VehiclePrice(game, discount));
    }

    [Theory]
    [InlineData(1f, 0.5, 0.5f)]
    [InlineData(0.55f, 0.5, 0.275f)]
    [InlineData(0.85f, 0, 0.85f)]
    [InlineData(1f, -1, 1f)]
    [InlineData(1f, 3, 0.1f)]
    public void Shortcuts_cut_travel_time_with_or_without_a_vehicle_but_never_more_than_90_percent(float game, double cut, float coeff)
    {
        Assert.Equal(coeff, PerkSettings.TravelCoeff(game, cut), 4);
    }

    [Theory]
    [InlineData(100, 250, 350)]
    [InlineData(0, 50, 50)]
    [InlineData(100, 0, 100)]
    public void Seasoned_Explorer_adds_to_the_max_exposure_bonus(int game, double added, int bonus)
    {
        Assert.Equal(bonus, PerkSettings.ExposureBonus(game, added));
    }

    [Theory]
    [InlineData(1, 8, 6)]
    [InlineData(2, 8, 7)]
    [InlineData(3, 8, 8)]
    [InlineData(1, 4, 2)]
    [InlineData(3, 4, 4)]
    [InlineData(1, 3, 1)]
    [InlineData(3, 3, 3)]
    [InlineData(2, 0, 2)]
    [InlineData(2, 1, 2)]
    [InlineData(3, 2, 3)]
    [InlineData(4, 8, 4)]
    public void Bigger_phone_loans_follow_the_Resourceful_Management_level_owned(int choice, int owned, int level)
    {
        Assert.Equal(level, PerkSettings.LoanLevel(choice, owned));
    }
}
