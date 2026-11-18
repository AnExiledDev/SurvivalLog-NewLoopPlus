using System.Collections.Generic;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>Before the mod is removed, every level the game has no row for is taken off and its points paid back.</summary>
public class UninstallTests
{
    /// <summary>Talent 1001 has 5 levels in the game, 1002 has 3, and 5101 exists only with the mod. Level n costs n * 10.</summary>
    private static UninstallSteps Plan(params (int Talent, int Level)[] owned)
    {
        var map = new Dictionary<int, int>();

        foreach (var (talent, level) in owned) map[talent] = level;

        return Uninstall.Plan(map, talent => talent switch { 1001 => 5, 1002 => 3, _ => 0 }, (_, lv) => lv * 10);
    }

    [Fact]
    public void Levels_the_game_has_are_left_alone()
    {
        var steps = Plan((1001, 5), (1002, 2));

        Assert.Empty(steps.Reverts);
        Assert.Equal(0, steps.Refund);
    }

    [Fact]
    public void Extra_levels_drop_to_the_games_top_level_and_each_one_is_paid_back()
    {
        var steps = Plan((1001, 8));

        Assert.Equal(new TalentRevert(1001, 5), Assert.Single(steps.Reverts));
        Assert.Equal(60 + 70 + 80, steps.Refund);
    }

    [Fact]
    public void A_talent_only_the_mod_adds_is_removed_and_every_level_paid_back()
    {
        var steps = Plan((5101, 2), (1002, 3));

        Assert.Equal(new TalentRevert(5101, 0), Assert.Single(steps.Reverts));
        Assert.Equal(10 + 20, steps.Refund);
    }

    [Fact]
    public void Every_talent_that_needs_it_is_taken_down_in_one_pass()
    {
        var steps = Plan((5101, 1), (1002, 4), (1001, 6));

        Assert.Equal(new[] { new TalentRevert(1001, 5), new TalentRevert(1002, 3), new TalentRevert(5101, 0) }, steps.Reverts);
        Assert.Equal(60 + 40 + 10, steps.Refund);
    }
}
