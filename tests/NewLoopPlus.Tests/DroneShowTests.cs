using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>A drone back before its departure animation ended must not be hidden by that departure's stop.</summary>
public class DroneShowTests
{
    [Fact]
    public void A_return_during_the_departure_forgets_the_departure()
    {
        Assert.True(DroneShow.ForgetsDeparture("Return", "Dispatch"));
    }

    [Fact]
    public void A_return_after_the_departure_ended_changes_nothing()
    {
        Assert.False(DroneShow.ForgetsDeparture("Return", null));
    }

    [Fact]
    public void A_departure_or_an_instant_hide_never_forgets_anything()
    {
        Assert.False(DroneShow.ForgetsDeparture("Dispatch", "Dispatch"));
        Assert.False(DroneShow.ForgetsDeparture("HideInstant", "Dispatch"));
        Assert.False(DroneShow.ForgetsDeparture("Dispatch", "Return"));
    }
}
