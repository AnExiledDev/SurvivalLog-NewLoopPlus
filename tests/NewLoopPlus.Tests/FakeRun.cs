using System.Collections.Generic;
using NewLoopPlus.Core;

namespace NewLoopPlus.Tests;

/// <summary>A run the tests can steer: whether it can end, its mode, the settle values and whether the clock runs.</summary>
internal sealed class FakeGame : IRunGame
{
    public bool Chinese { get; set; }
    public bool CanEnd { get; set; } = true;
    public RunMode RunMode { get; set; } = RunMode.Normal;
    public SettleInputs? Settle { get; set; } = new(100, 0.5f, false, null, 7, 3, 0, 2);
    public bool Paused { get; set; } = true;
    public bool IsMenuOpen { get; private set; } = true;
    public int RunsEnded { get; private set; }

    public bool IsChinese() => Chinese;

    public bool CanEndRun() => CanEnd && RunsEnded == 0;

    public RunMode Mode() => RunMode;

    public SettleInputs? ReadSettle() => Settle;

    public void ClosePauseMenu() => IsMenuOpen = false;

    public bool IsPaused() => Paused;

    public void EndRun() => RunsEnded++;
}

/// <summary>Records what the player would see on the pause menu's page.</summary>
internal sealed class FakePage : IRunPage
{
    public List<ConfirmDialog> Dialogs { get; } = new();
    public List<string> Toasts { get; } = new();
    public (string Label, bool IsShown)? Button { get; private set; }

    public void ShowButton(string label, bool isShown) => Button = (label, isShown);

    public void ShowConfirm(ConfirmDialog dialog) => Dialogs.Add(dialog);

    public void Toast(string text) => Toasts.Add(text);
}
