using System;

namespace NewLoopPlus.Core;

/// <summary>What End Run needs from the game. Reads that fail come back as null or false; the adapter logs why.</summary>
public interface IRunGame
{
    bool IsChinese();

    /// <summary>True in a loaded run past the tutorial that has not ended yet.</summary>
    bool CanEndRun();

    RunMode Mode();

    /// <summary>Null when a value the settlement needs could not be read.</summary>
    SettleInputs? ReadSettle();

    void ClosePauseMenu();

    /// <summary>True while the game's clock stands still, which would hold the death screen back.</summary>
    bool IsPaused();

    /// <summary>Ends the run the way a death does.</summary>
    void EndRun();
}

/// <summary>What End Run needs from the pause menu's page.</summary>
public interface IRunPage
{
    void ShowConfirm(ConfirmDialog dialog);

    void Toast(string text);
}

/// <summary>The confirm dialog's words; <see cref="Body"/> may hold line breaks.</summary>
public sealed record ConfirmDialog(string Title, string Body, string Cancel, string Confirm);

/// <summary>
/// End Run from click to death screen. The player is always asked first. On yes the pause menu closes, and the run
/// only ends once the game's clock runs again: the death screen waits on game time, so ending it paused would leave
/// the player on a frozen screen. If the clock is still stopped after <see cref="UnpauseTimeoutSeconds"/>, nothing
/// ends and the player is told.
/// </summary>
public sealed class EndRun
{
    public const float UnpauseTimeoutSeconds = 5f;

    private readonly IRunGame _game;
    private readonly IRunPage _page;
    private readonly Action<string> _log;
    private float? _giveUpAt;

    public EndRun(IRunGame game, IRunPage page, Action<string> log)
    {
        _game = game;
        _page = page;
        _log = log;
    }

    public bool IsEnding => _giveUpAt.HasValue;

    /// <summary>Whether the pause menu shows End Run: in a run that can end, and not once an end is under way.</summary>
    public bool IsButtonShown => !IsEnding && _game.CanEndRun();

    public void Ask()
    {
        var isChinese = _game.IsChinese();

        if (IsEnding) return;

        if (!_game.CanEndRun())
        {
            _page.Toast(Texts.CannotEndNow.Pick(isChinese));

            return;
        }

        _page.ShowConfirm(Dialog(isChinese));
    }

    public void Confirm(float now)
    {
        if (IsEnding) return;

        if (!_game.CanEndRun())
        {
            _page.Toast(Texts.CannotEndNow.Pick(_game.IsChinese()));

            return;
        }

        _log("end run confirmed, closing the pause menu");
        _game.ClosePauseMenu();
        _giveUpAt = now + UnpauseTimeoutSeconds;
    }

    /// <summary>Runs once a frame; ends the run on the first frame the clock runs after a confirm.</summary>
    public void Tick(float now)
    {
        if (_giveUpAt is not { } giveUpAt) return;

        if (!_game.CanEndRun())
        {
            Finish("the run ended on its own while the pause menu closed");

            return;
        }

        if (!_game.IsPaused())
        {
            Finish("the game runs again, ending the run");
            _game.EndRun();

            return;
        }

        if (now < giveUpAt) return;

        Finish($"the game was still paused {UnpauseTimeoutSeconds} s after closing the pause menu, the run was not ended");
        _page.Toast(Texts.StillPaused.Pick(_game.IsChinese()));
    }

    /// <summary>Clears the pending end before anything that can throw, so a failure never leaves End Run stuck.</summary>
    private void Finish(string note)
    {
        _giveUpAt = null;
        _log(note);
    }

    private ConfirmDialog Dialog(bool isChinese)
    {
        var body = Texts.EndsLikeDeath.Pick(isChinese) + "\n" + ModeLine(isChinese);

        return new ConfirmDialog(Texts.ConfirmTitle.Pick(isChinese), body, Texts.Cancel.Pick(isChinese), Texts.Button.Pick(isChinese));
    }

    private string ModeLine(bool isChinese)
    {
        switch (_game.Mode())
        {
            case RunMode.Normal:
                return PointsLine(isChinese);
            case RunMode.Casual:
                return Texts.CasualLine.Pick(isChinese);
            case RunMode.Endless:
            case RunMode.Challenge:
                return Texts.ModeSettlesLine.Pick(isChinese);
            default:
                throw new ArgumentOutOfRangeException(nameof(RunMode));
        }
    }

    private string PointsLine(bool isChinese)
    {
        var inputs = _game.ReadSettle();

        if (inputs is null) return Texts.PointsUnknown.Pick(isChinese);

        var preview = Settlement.Preview(inputs);

        _log($"points preview: survival {preview.Survival}, crisis {preview.Crisis}, record {preview.Record}, total {preview.Total} ({inputs})");

        return Texts.PointsLine.Format(isChinese, preview.Total, preview.Survival, preview.Crisis, preview.Record);
    }
}
