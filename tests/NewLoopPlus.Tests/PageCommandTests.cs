using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

public class PageCommandTests
{
    [Fact]
    public void The_button_dialog_and_script_messages_parse_to_their_commands()
    {
        Assert.Equal(new PageCommand.Ask(), PageCommand.Parse("slmod|newloopplus|ask"));
        Assert.Equal(new PageCommand.End(), PageCommand.Parse("slmod|newloopplus|end"));
        Assert.Equal(new PageCommand.Ready(), PageCommand.Parse("slmod|newloopplus|ready"));
        Assert.Equal(new PageCommand.NoScript(), PageCommand.Parse("slmod|newloopplus|noscript"));
    }

    [Theory]
    [InlineData("slmod|newloopplus|end|now")]
    [InlineData("slmod|newloopplus|End")]
    [InlineData("slmod|newloopplus|diag|cannot reach the pause menu document")]
    [InlineData("slmod|newloopplus|")]
    [InlineData("slmod|unpackall|end")]
    [InlineData("")]
    public void A_malformed_or_foreign_message_never_ends_a_run(string message)
    {
        Assert.Equal(new PageCommand.Unknown(message), PageCommand.Parse(message));
    }

    [Theory]
    [InlineData("slmod|unpackall|unpack", false)]
    [InlineData(null, false)]
    [InlineData("slmod|newloopplus|ask", true)]
    public void Only_this_mods_messages_are_taken(string? message, bool isOurs)
    {
        Assert.Equal(isOurs, PageCommand.IsOurs(message));
    }
}
