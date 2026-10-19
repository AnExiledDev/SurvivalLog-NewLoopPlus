using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>Doors and windows get the reinforcement of the talent level the survivor holds, and only that one.</summary>
public class ReinforcementTests
{
    private static readonly ReinforceRule[] Levels = { new(101010, 101011), new(101020, 101021), new(101030, 101031) };

    [Fact]
    public void The_level_held_picks_its_own_door_and_window_buff()
    {
        Assert.Equal(101021, Reinforcement.Pick(Levels, buff => buff == 101020));
    }

    [Fact]
    public void Holding_two_levels_gives_only_the_higher_one()
    {
        Assert.Equal(101031, Reinforcement.Pick(Levels, buff => buff is 101010 or 101030));
    }

    [Fact]
    public void Without_the_talent_nothing_is_reinforced()
    {
        Assert.Null(Reinforcement.Pick(Levels, _ => false));
    }
}
