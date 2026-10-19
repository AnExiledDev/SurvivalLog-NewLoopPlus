using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>The preview has to match what the death screen then pays, so it follows the game's settle exactly.</summary>
public class SettlementTests
{
    private static SettleInputs Inputs(int points = 100, float rate = 0.5f, bool isSettled = false, int[]? stages = null, int crisis = 0, int day = 0, int paidDay = 0, int dayBelief = 0) =>
        new(points, rate, isSettled, stages, crisis, day, paidDay, dayBelief);

    [Fact]
    public void A_first_settle_pays_every_converted_survival_point_plus_crises_and_unpaid_days()
    {
        var preview = Settlement.Preview(Inputs(points: 100, rate: 0.5f, crisis: 7, day: 5, paidDay: 2, dayBelief: 4));

        Assert.Equal(new PointsPreview(50, 7, 12), preview);
        Assert.Equal(69, preview.Total);
    }

    [Fact]
    public void A_later_settle_pays_only_what_was_converted_since_the_last_one()
    {
        var preview = Settlement.Preview(Inputs(points: 100, rate: 0.5f, isSettled: true, stages: new[] { 30, 0, 0 }));

        Assert.Equal(20, preview.Survival);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(new[] { 30, 0 })]
    public void A_missing_or_short_settle_record_counts_everything_as_already_paid(int[]? stages)
    {
        var preview = Settlement.Preview(Inputs(points: 100, rate: 0.5f, isSettled: true, stages: stages));

        Assert.Equal(0, preview.Survival);
    }

    [Fact]
    public void Survival_never_goes_negative_when_more_was_paid_than_is_converted_now()
    {
        var preview = Settlement.Preview(Inputs(points: 10, rate: 0.5f, isSettled: true, stages: new[] { 30, 0, 0 }));

        Assert.Equal(0, preview.Survival);
    }

    [Fact]
    public void Days_already_paid_for_pay_nothing_again()
    {
        var preview = Settlement.Preview(Inputs(day: 4, paidDay: 6, dayBelief: 5));

        Assert.Equal(0, preview.Record);
    }

    [Theory]
    [InlineData(25, 12)]
    [InlineData(27, 14)]
    public void Half_points_round_to_even_like_the_game(int points, int expected)
    {
        Assert.Equal(expected, Settlement.Preview(Inputs(points: points, rate: 0.5f)).Survival);
    }

    [Fact]
    public void The_conversion_multiplies_in_single_precision_like_the_game()
    {
        // 125 * 0.1f is exactly 12.5 as a float (rounds to 12) but 12.5000002 as a double (would round to 13).
        Assert.Equal(12, Settlement.Preview(Inputs(points: 125, rate: 0.1f)).Survival);
    }
}
