using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// New Loop Plus's messages are Exiled's Toolkit toasts shown by the plugin, so page.js, run in Jint on a fake root page
/// (PageFake.js), draws no notice box of its own. End Run's dialog sits in the game's pause menu in the game's own
/// style, so it keeps the game's size and is not handed to the Toolkit's UI size.
/// </summary>
public class PageToastTests
{
    [Fact]
    public void The_page_has_no_toast_of_its_own()
    {
        var page = new RootPage();

        Assert.False(page.Bool("window.__slnlp.toast"));
        Assert.False(page.Bool("document.getElementById('slnlp-toast')"));
    }

    [Fact]
    public void Nothing_is_handed_to_the_toolkit_for_the_pause_menu()
    {
        var page = new RootPage();

        page.Run("addFrame('../UI/OutSetting.html')");
        page.Run("__slnlp.confirm('End Run?', 'Ends like a death.', 'Cancel', 'End Run')");

        Assert.True(page.Bool("frames[0].contentDocument.getElementById('slnlp-confirm')"));
        Assert.Equal(0, page.Number("handed.length"));
    }
}
