using System;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using NewLoopPlus.Core;
using ToolkitPage = ExiledToolkit.Web.WebPage;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's pages sit in. It
/// shows End Run's confirm dialog in the pause menu; the End Run button itself is placed by Exiled's Toolkit, and the
/// messages are Exiled's Toolkit toasts. The web view can reload at any time and lose the script, so Exiled's Toolkit
/// checks for it every couple of seconds and it is put back when the page says it is missing.
/// </summary>
internal sealed class WebPage : IRunPage
{
    private const string PageResource = "NewLoopPlus.page.js";
    private const string TalentsTabResource = "NewLoopPlus.talents-tab.js";
    private const string Global = "window.__slnlp";

    private readonly ToolkitPage _page;
    private readonly Action<string> _log;

    public WebPage(Action<string> log)
    {
        _log = log;

        // The pause menu script and the Talents tab travel together, so a reloaded page gets both back at once. The
        // tab draws with the Toolkit's reward editor, which goes first.
        _page = new ToolkitPage(typeof(WebPage).Assembly, PageResource, Global, PageCommand.Prefix + "noscript", log)
        {
            ToolkitScripts = new[] { ToolkitPage.Editor },
            ExtraResources = new[] { TalentsTabResource },
        };
    }

    public void KeepInstalled(double now) => _page.KeepInstalled(now);

    public void Install() => _page.Install();

    public void ShowConfirm(ConfirmDialog dialog) =>
        _page.Run(Global + "&&" + Global + ".confirm(" + string.Join(",", JsText.Literal(dialog.Title), JsText.Literal(dialog.Body), JsText.Literal(dialog.Cancel), JsText.Literal(dialog.Confirm)) + ")");

    /// <summary>A message as an Exiled's Toolkit toast; a failure shows as an error.</summary>
    public void Toast(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var isChinese = GameLanguage.IsChinese();
        var kind = text == Texts.Failed.Pick(isChinese) ? ToastKind.Error : ToastKind.Info;

        if (Toasts.Show(new ToastRequest(Texts.Source.Pick(isChinese), text) { Kind = kind }).IsNone) _log("a toast was not shown: " + text);
    }

    /// <summary>Runs a call into the page's scripts, such as a reply to the Talents tab.</summary>
    public void Call(string javaScript) => _page.Run(javaScript);
}
