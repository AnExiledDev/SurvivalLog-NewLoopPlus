using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>Endless rewards bought outside endless must be as strong as the game makes them in endless, no stronger.</summary>
public class EndlessRewardsTests
{
    [Theory]
    [InlineData(4001)]
    [InlineData(4013)]
    [InlineData(4019)]
    [InlineData(4026)]
    public void The_rewards_that_work_anywhere_are_unlocked(int talentId) => Assert.True(EndlessRewards.Unlocks(talentId));

    [Theory]
    [InlineData(4005)]
    [InlineData(4007)]
    [InlineData(4012)]
    [InlineData(4018)]
    [InlineData(1001)]
    public void Camp_rewards_Cartographer_and_ordinary_rewards_are_left_alone(int talentId) => Assert.False(EndlessRewards.Unlocks(talentId));

    [Fact]
    public void Buffer_Zone_halves_wave_health_at_half_ratio() => Assert.Equal(0.75f, EndlessRewards.WaveHp(1.5f, 0.5f), 4);

    [Fact]
    public void Wave_health_never_drops_below_one_percent() => Assert.Equal(0.02f, EndlessRewards.WaveHp(2f, 3f), 4);

    [Fact]
    public void Trapcraft_cuts_the_durability_cost() => Assert.Equal(7, EndlessRewards.TrapCost(10, 0.3f));

    [Fact]
    public void Trapcraft_rounds_halves_to_even_as_the_game_does() => Assert.Equal(2, EndlessRewards.TrapCost(5, 0.5f));

    [Fact]
    public void Trapcraft_never_cuts_more_than_ninety_percent() => Assert.Equal(1, EndlessRewards.TrapCost(10, 5f));

    [Fact]
    public void No_ratio_leaves_the_cost_alone() => Assert.Equal(10, EndlessRewards.TrapCost(10, 0f));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Only_the_first_ten_days_count(int day, bool early) => Assert.Equal(early, EndlessRewards.IsEarlyDay(day));

    [Fact]
    public void The_settlement_pays_for_the_day_just_ended() => Assert.Equal(10, EndlessRewards.SettledDay(11));

    [Fact]
    public void Seasoned_pays_per_crisis_survived() => Assert.Equal(10, EndlessRewards.CrisisPoints(3, 5, 5f));

    [Fact]
    public void A_crisis_not_survived_pays_nothing() => Assert.Equal(0, EndlessRewards.CrisisPoints(3, 3, 5f));

    [Fact]
    public void Points_round_as_the_game_rounds()
    {
        Assert.Equal(8, EndlessRewards.Points(8f));
        Assert.Equal(2, EndlessRewards.Points(2.5f));
        Assert.Equal(0, EndlessRewards.Points(0f));
    }
}
