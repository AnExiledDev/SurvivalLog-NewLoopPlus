using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The reinforce rules of the talents file. While the survivor holds a talent buff named in a rule, every door and
/// window of the home carries that rule's buff; the game's own Reinforce buffs raise Max Durability the same way. A
/// door or window built later, or a new home after the disaster, is picked up at the next check. Each piece of
/// furniture gets a rule's buff once, so it stacks with the player's own reinforcing but never with itself.
/// </summary>
internal sealed class DoorWindowReinforcement
{
    private const float CheckSeconds = 2f;
    private const int DoorSlot = 8;
    private const int WindowSlot = 9;

    private readonly IReadOnlyList<IReadOnlyList<ReinforceRule>> _groups;
    private readonly Action<string> _log;
    private float _nextCheckAt;

    public DoorWindowReinforcement(IReadOnlyList<IReadOnlyList<ReinforceRule>> groups, Action<string> log)
    {
        _groups = groups;
        _log = log;
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var agents = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager;
        var role = agents?.GetLeadingRole();
        var roleBuffs = role == null ? null : AgentTools.GetAgentComponent<BuffComponent>(role);

        if (agents == null || role == null || roleBuffs == null) return;

        var reinforced = 0;

        foreach (var rules in _groups)
        {
            if (Reinforcement.Pick(rules, roleBuffs.IsBuffConfigExist) is not { } buff) continue;

            reinforced += Reinforce(agents, DoorSlot, buff, role.InstanceId) + Reinforce(agents, WindowSlot, buff, role.InstanceId);
        }

        if (reinforced > 0) _log($"reinforced {reinforced} doors and windows");
    }

    private static int Reinforce(AgentManager agents, int slot, int buff, long releaserId)
    {
        var furniture = agents.GetHomeFurnituresBySlotType(slot);
        var reinforced = 0;

        for (var i = 0; furniture != null && i < furniture.Count; i++)
        {
            var piece = furniture[i];
            var buffs = piece == null || piece.IsDead ? null : AgentTools.GetAgentComponent<BuffComponent>(piece);

            if (buffs == null || buffs.IsBuffConfigExist(buff)) continue;

            buffs.AddBuff(releaserId, buff);
            reinforced++;
        }

        return reinforced;
    }
}
