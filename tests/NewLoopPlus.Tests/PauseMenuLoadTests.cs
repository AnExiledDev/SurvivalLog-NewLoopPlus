using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// End Run goes into the pause menu the moment its page loads, but looking for the pause menu reads its size, which makes
/// the browser lay out the page. So only the pause menu's own page loading does that; the many other game pages that
/// load while playing leave it to the poll.
/// </summary>
public class PauseMenuLoadTests
{
    private const string CountLooks = "var looks = 0; document.querySelectorAll = function () { looks++; return []; }";

    [Fact]
    public void The_pause_menu_page_loading_is_patched_on_the_spot()
    {
        var page = new RootPage();
        page.Run(CountLooks);

        page.Run("addFrame('../UI/OutSetting.html?t=1').fire('load')");

        Assert.Equal(1, page.Number("looks"));
    }

    [Fact]
    public void Another_game_page_loading_does_not_look_for_the_pause_menu()
    {
        var page = new RootPage();
        page.Run(CountLooks);

        page.Run("addFrame('../UI/Bag.html').fire('load'); addFrame('../UI/ToolTable.html').fire('load')");

        Assert.Equal(0, page.Number("looks"));
    }
}
