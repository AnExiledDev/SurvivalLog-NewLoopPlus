using Jint;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// The toast New Loop Plus draws on the root page follows Exiled's Toolkit's UI size: page.js hands it to the Toolkit,
/// run in Jint on a fake root page (PageFake.js). A one-line message is never shrunk. The End Run button and its dialog
/// sit in the game's pause menu in the game's own style, so they keep the game's size and are not handed over.
/// </summary>
public class ToastUiSizeTests
{
    [Fact]
    public void The_toast_is_handed_over_once_and_never_shrunk()
    {
        var page = new Page();

        page.Run("__slnlp.toast('Run ended'); __slnlp.toast('Another message')");

        Assert.Equal(1, page.Number("handed.length"));
        Assert.True(page.Bool("handed[0].el === toastNode()"));
        Assert.True(page.Bool("handed[0].options.fit === false"));
    }

    /// <summary>The Toolkit lets go of a panel taken out of its page, so a toast made again must be handed again.</summary>
    [Fact]
    public void A_toast_made_again_after_the_page_lost_it_is_handed_over_again()
    {
        var page = new Page();
        page.Run("__slnlp.toast('Run ended'); var first = toastNode(); first.remove()");

        page.Run("__slnlp.toast('Another message')");

        Assert.Equal(2, page.Number("handed.length"));
        Assert.True(page.Bool("handed[1].el === toastNode() && handed[1].el !== first"));
    }

    [Fact]
    public void Before_the_Toolkit_loads_the_toast_waits_for_it()
    {
        var page = new Page("delete window.ExiledUi");

        page.Run("__slnlp.toast('Run ended')");

        Assert.Equal(1, page.Number("window.__exiledUiWait.length"));
        Assert.True(page.Bool("window.__exiledUiWait[0][0] === toastNode() && window.__exiledUiWait[0][1].fit === false"));
    }

    [Fact]
    public void Nothing_is_handed_over_before_there_is_a_toast_to_show()
    {
        var page = new Page();

        page.Run("__slnlp.setButton('End Run', true)");

        Assert.Equal(0, page.Number("handed.length"));
    }

    /// <summary>page.js on PageFake.js; <paramref name="before"/> runs between the two.</summary>
    private sealed class Page
    {
        private readonly Engine _engine = new();

        public Page(string? before = null)
        {
            _engine.Execute(Resource("NewLoopPlus.Tests.PageFake.js"));
            if (before != null) _engine.Execute(before);
            _engine.Execute(Resource("NewLoopPlus.page.js"));
        }

        public void Run(string statements) => _engine.Execute(statements);

        public int Number(string expression) => (int)_engine.Evaluate($"Number({expression})").AsNumber();

        public bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

        private static string Resource(string name)
        {
            using var stream = typeof(ToastUiSizeTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
