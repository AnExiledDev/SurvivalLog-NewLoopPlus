using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using NewLoopPlus.Core;
using UnityEngine;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The game side of End Run. The run ends through BattleMain.GameOver with the arguments LeadingRole.OnStateDead
/// passes on a real death, so the death screen, settlement and save are the game's own.
/// </summary>
internal sealed class GameRun : IRunGame
{
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedReadFailures = new();

    public GameRun(Action<string> log)
    {
        _log = log;
    }

    public bool IsChinese() => GameLanguage.IsChinese();

    public bool CanEndRun()
    {
        var main = GameSingleton.Existing<BattleMain>();
        var world = GameSingleton.Existing<BattleLogicWorld>();

        return main != null && world != null && !main.IsGameOver && world.GameModeKey != GameMode.Guide;
    }

    /// <summary>The same checks, in the same order, as the game's death screen picks its settlement with.</summary>
    public RunMode Mode()
    {
        var save = CurrentSave();

        if (save == null) return RunMode.Normal;
        if (save.ChallengeTargetId > 0) return RunMode.Challenge;
        if (GameKey.IsCasualSave(save)) return RunMode.Casual;

        return save.EndlessActive ? RunMode.Endless : RunMode.Normal;
    }

    public SettleInputs? ReadSettle()
    {
        try
        {
            var world = GameSingleton.Existing<BattleLogicWorld>() ?? throw new InvalidOperationException("no battle world");
            var round = CurrentSave()?.CurSave;
            var stages = round?.FaithSettledStages;

            return new SettleInputs(
                SurvivalPoints: world._SurvivalResultsManager.GetTotalPoints(),
                SurvivalToBelief: GameKey.GlobalSetting_SurvivalToBelief,
                IsFaithSettled: round?.IsFaithSettled == true,
                SettledStages: stages == null ? null : CopyOf(stages),
                CrisisPoints: world._DynamicEventManager.GetSettlementBeliefPoint(),
                Day: world._GameTimeManager.GetDay(),
                RecordRewardPaidDay: world._FaithManager.RecordRewardPaidDay,
                RecordRewardBelief: GameKey.GlobalSetting_RecordRewardBelief);
        }
        catch (Exception ex)
        {
            if (_reportedReadFailures.Add(ex.GetType().FullName + ": " + ex.Message)) _log("could not read the points preview (each distinct failure reported once): " + ex);

            return null;
        }
    }

    public void ClosePauseMenu() => Ac_OutSetting_Close.SendAction();

    public bool IsPaused()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();

        return world == null || world.IsPause || Time.timeScale <= 0f;
    }

    public void EndRun()
    {
        var main = GameSingleton.Existing<BattleMain>() ?? throw new InvalidOperationException("no BattleMain to end the run with");
        var configId = LeadingRoleConfigId();

        _log($"calling GameOver as a death, leading role config {configId}");
        main.GameOver(BattleResult.LeadingRoleDead, configId, null, new Il2CppSystem.Nullable<Vector3>(), 0, false);
    }

    /// <summary>A real death passes the leading role's config id. If it cannot be read the run still ends, with 0.</summary>
    private int LeadingRoleConfigId()
    {
        try
        {
            return GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole()?.AgentConfigId ?? 0;
        }
        catch (Exception ex)
        {
            _log("could not read the leading role, ending the run without its id: " + ex.Message);

            return 0;
        }
    }

    private static GameSaveData? CurrentSave() => GameSingleton.Existing<GameSaveManager>()?.gameSave;

    private static int[] CopyOf(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int> stages)
    {
        var copy = new int[stages.Length];

        for (var i = 0; i < copy.Length; i++) copy[i] = stages[i];

        return copy;
    }
}
