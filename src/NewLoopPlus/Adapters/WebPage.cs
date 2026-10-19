using System;
using System.IO;
using GameCore.HotUpdate.ReduxUI;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's own pages sit in. It
/// puts End Run in the pause menu, shows the confirm dialog there and shows a toast when something went wrong. The web
/// view can reload at any time and lose the script, so it is checked for every couple of seconds and put back when the
/// page says it is missing.
/// </summary>
internal sealed class WebPage : IRunPage
{
    private const float CheckSeconds = 2f;
    private const string ScriptResource = "NewLoopPlus.page.js";
    private const string NoScriptReply = "window.vuplex&&window.vuplex.postMessage('" + PageCommand.Prefix + "noscript')";

    private readonly Action<string> _log;
    private string? _script;
    private float _nextCheckAt;
    private bool _hasReportedRunFailure;

    public WebPage(Action<string> log)
    {
        _log = log;
    }

    /// <summary>True on the frames that checked for the script, so the caller can keep the button in step at the same pace.</summary>
    public bool KeepInstalled(float now)
    {
        if (now < _nextCheckAt) return false;

        _nextCheckAt = now + CheckSeconds;
        Run("window.__slnlp||" + NoScriptReply);

        return true;
    }

    public void Install()
    {
        if (Run(Script())) _log("page script sent to the web UI");
    }

    public void ShowButton(string label, bool isShown) =>
        Run("window.__slnlp&&window.__slnlp.setButton(" + JsText.Literal(label) + "," + (isShown ? "true" : "false") + ")");

    public void ShowConfirm(ConfirmDialog dialog) =>
        Run("window.__slnlp&&window.__slnlp.confirm(" + string.Join(",", JsText.Literal(dialog.Title), JsText.Literal(dialog.Body), JsText.Literal(dialog.Cancel), JsText.Literal(dialog.Confirm)) + ")");

    /// <summary>The script travels with the toast, so a page that reloaded since the last check still shows it.</summary>
    public void Toast(string text) => Run(Script() + "\n;window.__slnlp&&window.__slnlp.toast(" + JsText.Literal(text) + ");");

    private bool Run(string javaScript)
    {
        var webView = GameSingleton.Existing<ReduxUISystem>()?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        if (webView == null) return false;

        try
        {
            webView.ExecuteJavaScript(javaScript, null);

            return true;
        }
        catch (Exception ex)
        {
            if (!_hasReportedRunFailure) _log("could not run script in the web UI (reported once): " + ex.Message);

            _hasReportedRunFailure = true;

            return false;
        }
    }

    private string Script()
    {
        if (_script != null) return _script;

        using var stream = typeof(WebPage).Assembly.GetManifestResourceStream(ScriptResource) ?? throw new InvalidOperationException(ScriptResource + " is not embedded");
        using var reader = new StreamReader(stream);

        return _script = reader.ReadToEnd();
    }
}
