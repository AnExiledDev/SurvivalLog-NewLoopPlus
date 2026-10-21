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

    [Fact]
    public void The_talents_tab_loads_and_saves_and_a_save_keeps_every_character_of_the_file()
    {
        Assert.Equal(new PageCommand.TalentsLoad(), PageCommand.Parse("slmod|newloopplus|talents|load"));
        Assert.Equal(new PageCommand.TalentsSave("{\"a\":\"x|y\"}"), PageCommand.Parse("slmod|newloopplus|talents|save|{\"a\":\"x|y\"}"));
        Assert.Equal(new PageCommand.TalentsSave(""), PageCommand.Parse("slmod|newloopplus|talents|save|"));
    }

    [Theory]
    [InlineData("slmod|newloopplus|talents|load|now")]
    [InlineData("slmod|newloopplus|talents|save")]
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
