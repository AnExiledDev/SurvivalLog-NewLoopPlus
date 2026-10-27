using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>A use of a multi-use buff fires it and keeps the uses that are left; the last use is left to the game.</summary>
public class MultiUseBuffsTests
{
    private static MultiUseBuffs Mental()
    {
        var buffs = new MultiUseBuffs();
        buffs.Add(201060);
        return buffs;
    }

    [Fact]
    public void A_use_with_uses_to_spare_keeps_the_rest()
    {
        Assert.Equal(2, Mental().UsesLeft(201060, 3, 1));
    }

    [Fact]
    public void The_last_use_is_left_to_the_game_which_ends_the_buff_itself()
    {
        Assert.Null(Mental().UsesLeft(201060, 1, 1));
        Assert.Null(Mental().UsesLeft(201060, 2, 3));
    }

    [Fact]
    public void Other_buffs_and_empty_uses_are_left_alone()
    {
        Assert.Null(Mental().UsesLeft(201050, 3, 1));
        Assert.Null(Mental().UsesLeft(201060, 3, 0));
    }

    [Fact]
    public void Clearing_forgets_every_buff()
    {
        var buffs = Mental();
        buffs.Clear();

        Assert.Equal(0, buffs.Count);
        Assert.Null(buffs.UsesLeft(201060, 3, 1));
    }
}
