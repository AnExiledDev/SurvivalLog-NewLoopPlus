using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>End Run always asks first, and only ends the run once the pause menu is closed and the clock runs again.</summary>
public class EndRunTests
{
    private readonly FakeGame _game = new();
    private readonly FakePage _page = new();
    private readonly EndRun _flow;

    public EndRunTests()
    {
        _flow = new EndRun(_game, _page, _ => { });
    }

    [Fact]
    public void Clicking_the_button_only_asks_and_never_ends_the_run()
    {
        _flow.Ask();

        var dialog = Assert.Single(_page.Dialogs);

        Assert.Equal(Texts.ConfirmTitle.En, dialog.Title);
        Assert.True(_game.IsMenuOpen);
        Assert.Equal(0, _game.RunsEnded);
    }

    [Fact]
    public void A_normal_run_shows_the_points_the_death_screen_will_pay()
    {
        _game.Settle = new SettleInputs(100, 0.5f, false, null, 7, 5, 2, 4);

        _flow.Ask();

        Assert.Contains(Texts.PointsLine.Format(false, 69, 50, 7, 12), _page.Dialogs[0].Body);
    }

    [Fact]
    public void A_normal_run_whose_points_cannot_be_read_still_asks_and_says_so()
    {
        _game.Settle = null;

        _flow.Ask();

        Assert.Contains(Texts.PointsUnknown.En, Assert.Single(_page.Dialogs).Body);
    }

    [Theory]
    [InlineData(RunMode.Casual)]
    [InlineData(RunMode.Endless)]
    [InlineData(RunMode.Challenge)]
    public void Modes_that_pay_no_planning_points_on_death_show_no_points(RunMode mode)
    {
        _game.RunMode = mode;

        _flow.Ask();

        var body = Assert.Single(_page.Dialogs).Body;

        Assert.DoesNotContain("Planning Points:", body);
        Assert.Contains(mode == RunMode.Casual ? Texts.CasualLine.En : Texts.ModeSettlesLine.En, body);
    }

    [Fact]
    public void The_dialog_follows_the_games_language()
    {
        _game.Chinese = true;

        _flow.Ask();

        var dialog = _page.Dialogs[0];

        Assert.Equal(Texts.ConfirmTitle.Zh, dialog.Title);
        Assert.Equal(Texts.Cancel.Zh, dialog.Cancel);
        Assert.StartsWith(Texts.EndsLikeDeath.Zh, dialog.Body);
    }

    [Fact]
    public void A_run_that_cannot_end_says_so_instead_of_asking()
    {
        _game.CanEnd = false;

        _flow.Ask();

        Assert.Empty(_page.Dialogs);
        Assert.Equal(new[] { Texts.CannotEndNow.En }, _page.Toasts);
    }

    [Fact]
    public void Confirming_closes_the_pause_menu_and_waits_while_the_game_is_still_paused()
    {
        _flow.Confirm(now: 10f);
        _flow.Tick(now: 11f);

        Assert.False(_game.IsMenuOpen);
        Assert.Equal(0, _game.RunsEnded);
        Assert.True(_flow.IsEnding);
    }

    [Fact]
    public void The_run_ends_on_the_first_frame_the_clock_runs_again()
    {
        _flow.Confirm(now: 10f);
        _flow.Tick(now: 10.1f);
        _game.Paused = false;
        _flow.Tick(now: 10.2f);
        _flow.Tick(now: 10.3f);

        Assert.Equal(1, _game.RunsEnded);
        Assert.False(_flow.IsEnding);
    }

    [Fact]
    public void A_game_still_paused_after_the_timeout_is_left_running_and_the_player_is_told()
    {
        _flow.Confirm(now: 10f);
        _flow.Tick(now: 10f + EndRun.UnpauseTimeoutSeconds);
        _game.Paused = false;
        _flow.Tick(now: 20f);

        Assert.Equal(0, _game.RunsEnded);
        Assert.Equal(new[] { Texts.StillPaused.En }, _page.Toasts);
    }

    [Fact]
    public void A_run_that_ended_on_its_own_meanwhile_is_not_ended_again()
    {
        _flow.Confirm(now: 10f);
        _game.CanEnd = false;
        _game.Paused = false;
        _flow.Tick(now: 10.1f);

        Assert.Equal(0, _game.RunsEnded);
        Assert.False(_flow.IsEnding);
    }

    [Fact]
    public void Confirming_twice_ends_the_run_once()
    {
        _flow.Confirm(now: 10f);
        _flow.Confirm(now: 10.1f);
        _game.Paused = false;
        _flow.Tick(now: 10.2f);
        _flow.Confirm(now: 10.3f);
        _flow.Tick(now: 10.4f);

        Assert.Equal(1, _game.RunsEnded);
    }

    [Fact]
    public void The_button_hides_while_a_confirmed_end_is_pending_and_after_the_run_ends()
    {
        Assert.True(_flow.IsButtonShown);

        _flow.Confirm(now: 10f);

        Assert.False(_flow.IsButtonShown);

        _game.Paused = false;
        _flow.Tick(now: 10.1f);

        Assert.Equal(1, _game.RunsEnded);
        Assert.False(_flow.IsButtonShown);
    }

    [Fact]
    public void The_button_is_hidden_where_a_run_cannot_end()
    {
        _game.CanEnd = false;

        Assert.False(_flow.IsButtonShown);
    }
}
