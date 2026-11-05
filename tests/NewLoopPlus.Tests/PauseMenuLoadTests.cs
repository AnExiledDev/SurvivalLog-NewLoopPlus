using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// Looking for the pause menu reads its size, which makes the browser lay out the page, so it is done on Exiled's
/// Toolkit's page check and when End Run's dialog is about to show, never as a game page loads. The End Run button is
/// placed by Exiled's Toolkit, and its click reaches the page through the plugin, so the menu may have opened since the
/// last check: the dialog looks for it again rather than say there is no pause menu. The game can run either script
/// first, so each test runs in both orders.
/// </summary>
public class PauseMenuLoadTests
{
    private const string Menu = "frames[0].contentDocument";
    private const string Dialog = Menu + ".getElementById('slnlp-confirm')";
    private const string Confirm = "__slnlp.confirm('End Run?', 'Ends like a death.', 'Cancel', 'End Run')";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Game_pages_loading_do_not_look_for_the_pause_menu(bool isToolkitFirst)
    {
        var page = Started(isToolkitFirst);

        page.Run("addFrame('../UI/OutSetting.html?t=1').fire('load'); addFrame('../UI/Bag.html').fire('load')");

        Assert.Equal(0, page.Number("layoutReads"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void The_confirm_dialog_shows_in_a_pause_menu_opened_since_the_last_check(bool isToolkitFirst)
    {
        var page = Started(isToolkitFirst);

        page.Run("addFrame('../UI/OutSetting.html')");
        page.Run(Confirm);

        Assert.True(page.Bool(Dialog + ".classList.contains('active')"));
        Assert.Equal("End Run?", page.Text(Dialog + ".querySelector('.confirm-title').textContent"));
        Assert.DoesNotContain("slmod|newloopplus|diag|no pause menu to show the confirm dialog in", page.Text("sent.join('\\n')").Split('\n'));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Confirming_asks_the_plugin_to_end_the_run(bool isToolkitFirst)
    {
        var page = Started(isToolkitFirst);

        page.Run("addFrame('../UI/OutSetting.html')");
        page.Run(Confirm);
        page.Run(Dialog + ".querySelector('.c-btn-confirm').fire('click')");

        Assert.Contains("slmod|newloopplus|end", page.Text("sent.join('\\n')").Split('\n'));
        Assert.False(page.Bool(Dialog + ".classList.contains('active')"));
    }

    [Fact]
    public void A_dialog_left_open_when_the_menu_hides_is_closed()
    {
        var page = Started(isToolkitFirst: true);

        page.Run("addFrame('../UI/OutSetting.html')");
        page.Run(Confirm);
        page.Run("frames[0].hide(); poll()");

        Assert.False(page.Bool(Dialog + ".classList.contains('active')"));
    }

    [Fact]
    public void With_no_pause_menu_the_plugin_hears_why_nothing_showed()
    {
        var page = Started(isToolkitFirst: true);

        page.Run(Confirm);

        Assert.Contains("slmod|newloopplus|diag|no pause menu to show the confirm dialog in", page.Text("sent.join('\\n')").Split('\n'));
    }

    private static RootPage Started(bool isToolkitFirst)
    {
        var page = new RootPage(isToolkitFirst);

        if (!isToolkitFirst) page.LoadToolkit();

        return page;
    }
}
