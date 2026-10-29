using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// New Loop Plus's messages are Exiled's Toolkit toasts shown by the plugin, so page.js, run in Jint on a fake root page
/// (PageFake.js), draws no notice box of its own. The End Run button and its dialog sit in the game's pause menu in the
/// game's own style, so they keep the game's size and are not handed to the Toolkit.
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

        page.Run("__slnlp.setButton('End Run', true)");

        Assert.Equal(0, page.Number("handed.length"));
    }
}
