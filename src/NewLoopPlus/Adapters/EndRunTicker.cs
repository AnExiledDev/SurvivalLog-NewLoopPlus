using System;
using System.Collections.Generic;
using NewLoopPlus.Core;
using UnityEngine;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The Unity object that hands frames to End Run. Page messages arrive on the web view's thread and are handled here,
/// on the game's. Each distinct failure is logged once and never rethrown into the game; a failure that repeats every
/// frame would otherwise flood the log.
/// </summary>
public sealed class EndRunTicker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    internal static EndRun? Flow;
    internal static WebPage? Page;
    internal static Action<string> Log = _ => { };
    private static readonly HashSet<string> ReportedFailures = new();

    public EndRunTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (Flow == null || Page == null) return;

        try
        {
            if (Page.KeepInstalled(Time.unscaledTime)) Flow.RefreshButton(isPageNew: false);

            TakePageCommands(Flow, Page);
            Flow.Tick(Time.unscaledTime);
        }
        catch (Exception ex)
        {
            if (ReportFailure(ex)) TellPlayer(Page);
        }
    }

    private static void TakePageCommands(EndRun flow, WebPage page)
    {
        while (PageMessages.TryTake(out var command))
        {
            switch (command)
            {
                case PageCommand.Ask:
                    flow.Ask();
                    break;
                case PageCommand.End:
                    flow.Confirm(Time.unscaledTime);
                    break;
                case PageCommand.Ready:
                    Log("page script is running");
                    flow.RefreshButton(isPageNew: true);
                    break;
                case PageCommand.NoScript:
                    page.Install();
                    break;
                case PageCommand.Unknown unknown:
                    Log("page: " + unknown.Text);
                    break;
            }
        }
    }

    private static void TellPlayer(WebPage page)
    {
        try
        {
            page.Toast(Texts.Failed.Pick(GameLanguage.IsChinese()));
        }
        catch (Exception)
        {
            // The failure is in the log already; a toast that cannot show adds nothing.
        }
    }

    /// <summary>True the first time a failure is seen, so the player hears about each one once rather than every frame.</summary>
    private static bool ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return false;

        Log("update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct update failures reported; further ones are not logged");

        return true;
    }
}
