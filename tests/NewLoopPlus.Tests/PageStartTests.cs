using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// The game can run New Loop Plus's page script before Exiled's Toolkit's. The script is there at once, so the
/// plugin's check for it does not send it again, and it starts its work, and says it is ready, once the Toolkit's page
/// API arrives. Extra Pick's extra end-of-day rewards are laid out from the Toolkit's page check.
/// </summary>
public class PageStartTests
{
    private const string Ready = "slmod|newloopplus|ready";
    private const string Rewards = "frames[0].contentDocument.documentElement";

    [Fact]
    public void Before_the_toolkit_the_script_is_there_and_says_nothing()
    {
        var page = new RootPage(isToolkitFirst: false);

        Assert.True(page.Bool("window.__slnlp && window.__slnlp.confirm"));
        Assert.Equal(0, page.Number("sent.length"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_script_says_it_is_ready_once_the_toolkit_is_there(bool isToolkitFirst)
    {
        var page = new RootPage(isToolkitFirst);

        if (!isToolkitFirst) page.LoadToolkit();

        Assert.Equal(new[] { Ready }, page.Text("sent.join('\\n')").Split('\n'));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void More_than_five_reward_cards_go_in_even_smaller_rows(bool isToolkitFirst)
    {
        var page = new RootPage(isToolkitFirst);

        page.Run("addRewardsPage(6)");

        if (!isToolkitFirst) page.LoadToolkit();

        page.Run("poll()");

        Assert.True(page.Bool(Rewards + ".classList.contains('slnlp-many')"));
        Assert.True(page.Bool(Rewards + ".classList.contains('slnlp-compact')"));
        Assert.Equal("3", page.Text(Rewards + ".style['--slnlp-columns']"));
        Assert.True(page.Bool("frames[0].contentDocument.getElementById('slnlp-grid-style')"));
    }

    [Fact]
    public void The_games_three_cards_keep_the_games_layout()
    {
        var page = new RootPage();

        page.Run("addRewardsPage(3); poll()");

        Assert.False(page.Bool(Rewards + ".classList.contains('slnlp-many')"));
    }

    [Fact]
    public void With_more_rewards_installed_its_layout_is_left_alone()
    {
        var page = new RootPage();

        page.Run("window.__slmorerewards = {}; addRewardsPage(6); poll()");

        Assert.False(page.Bool(Rewards + ".classList.contains('slnlp-many')"));
    }
}
