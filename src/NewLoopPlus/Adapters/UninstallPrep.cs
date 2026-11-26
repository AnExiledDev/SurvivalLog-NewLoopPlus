using System;
using System.Collections.Generic;
using System.Linq;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// While the UninstallPrep setting is on, takes every talent level the game has no row for off the player and pays its
/// Planning Points back (see <see cref="Uninstall"/>). Each talent goes down through LeadingRole.OnTalentLevelReverted,
/// the step the game's reset uses, so the level's buff comes off and the right one goes on. Nothing it changes is
/// undone when the setting goes off again.
/// </summary>
internal static class UninstallPrep
{
    private const float CheckSeconds = 3f;

    private static readonly WakeGate Gate = new(CheckSeconds);
    private static volatile bool _isOn;
    private static IReadOnlyDictionary<int, int>? _gameTopLevels;

    internal static Action<string> Log = _ => { };

    /// <summary>Turns it on or off. Safe from any thread.</summary>
    public static void Set(bool isOn)
    {
        _isOn = isOn;
        ScaleTicker.Wake();
    }

    /// <summary>The highest level the game itself has for each talent, read before the talents file changed the table.</summary>
    public static void UseGameLevels(IReadOnlyDictionary<int, int> topLevels) => _gameTopLevels = topLevels;

    public static void Tick(double now)
    {
        if (!_isOn || _gameTopLevels is not { } topLevels || !Gate.IsDue(now, ScaleTicker.Wakes)) return;

        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var leader = world?._AgentManager?.GetLeadingRole();
        var owned = leader == null ? null : AgentTools.GetAgentComponent<TalentComponent>(leader)?.talentLevelMap;

        if (world?._FaithManager is not { } faith || config == null || leader == null || owned == null) return;

        var levels = new List<KeyValuePair<int, int>>();

        foreach (var pair in owned) levels.Add(new KeyValuePair<int, int>(pair.Key, pair.Value));

        var steps = Uninstall.Plan(levels, talentId => topLevels.TryGetValue(talentId, out var top) ? top : 0, (talentId, lv) => config.GetTalentConfig(talentId, lv)?._LvUpCost_k__BackingField ?? 0);

        Gate.Done();

        if (steps.Reverts.Count == 0) return;

        foreach (var revert in steps.Reverts) leader.OnTalentLevelReverted(revert.TalentId, revert.ToLevel);

        var paid = steps.Refund == 0 || faith.AddFaithPoint(steps.Refund, "TalentReset");

        Log($"Uninstall prep: took {steps.Reverts.Count} loop rewards down to the levels the game has ({string.Join(", ", steps.Reverts.Select(revert => $"{revert.TalentId} to {revert.ToLevel}"))}) and "
            + (paid ? $"paid back {steps.Refund} Planning Points" : $"the game refused the {steps.Refund} Planning Points back (a casual save takes none)")
            + ". Save the game, then remove NewLoopPlus.dll.");
    }
}
