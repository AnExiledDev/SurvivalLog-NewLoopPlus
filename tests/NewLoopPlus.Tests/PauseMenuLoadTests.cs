using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// Looking for the pause menu reads its size, which makes the browser lay out the page, so it is done on the poll and
/// when End Run's dialog is about to show, never as a game page loads. The End Run button is placed by Exiled's Toolkit,
/// and its click reaches the page through the plugin, so the menu may have opened since the last poll: the dialog looks
/// for it again rather than say there is no pause menu.
/// </summary>
public class PauseMenuLoadTests
{
    private const string CountLooks = "var looks = 0; document.querySelectorAll = function () { looks++; return []; }";

    [Fact]
    public void Game_pages_loading_do_not_look_for_the_pause_menu()
    {
        var page = new RootPage();
        page.Run(CountLooks);

        page.Run("addFrame('../UI/OutSetting.html?t=1').fire('load'); addFrame('../UI/Bag.html').fire('load')");

        Assert.Equal(0, page.Number("looks"));
    }

    [Fact]
    public void The_confirm_dialog_looks_for_the_pause_menu_before_it_shows()
    {
        var page = new RootPage();
        page.Run(CountLooks);

        page.Run("__slnlp.confirm('End Run?', 'Ends like a death.', 'Cancel', 'End Run')");

        Assert.Equal(1, page.Number("looks"));
    }
}
