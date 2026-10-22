using System;
using System.Reflection;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle;
using GameCore.HotUpdate.Battle.Logic;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Second Wind: when the player's character runs out of Vitality, the run goes on with a little Vitality left instead,
/// a set number of times per run. A death from Vitality goes through AttributeComponent.CostAttrValue, which writes the
/// new Vitality and then calls BaseAgent.TriggerDead and returns; the hook stands in front of TriggerDead, puts
/// Vitality back to at least 1 (SetBaseValue_Float, which also refreshes the bar) and skips the death. The uses spent
/// are kept in the run's own counters (GameCounter, saved with the run), so a reload does not hand them back. A save
/// loaded with its character already dead, and every other agent, are left to the game.
/// Static because a Harmony prefix has nowhere else to look.
/// </summary>
internal static class SecondWind
{
    private const string UsesSpentKey = "nlp.secondwind";

    private static Action<string> _log = _ => { };
    private static MethodInfo? _setVitality;
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;

        try
        {
            _setVitality = AccessTools.Method(typeof(AttributeComponent), "SetBaseValue_Float", new[] { typeof(AttrName), typeof(float), typeof(bool) })
                           ?? throw new MissingMethodException("AttributeComponent.SetBaseValue_Float(AttrName, float, bool)");
            harmony.Patch(AccessTools.Method(typeof(BaseAgent), nameof(BaseAgent.TriggerDead)), prefix: new HarmonyMethod(typeof(SecondWind), nameof(BeforeDeath)));
        }
        catch (Exception ex)
        {
            log("could not hook deaths, Second Wind will not save a run: " + ex.Message);
        }
    }

    /// <summary>False skips the death. Never throws into the game; on any failure the death goes ahead as the game's.</summary>
    private static bool BeforeDeath(BaseAgent __instance)
    {
        try
        {
            var usesPerRun = Perks.Whole(NlpEffect.SecondWind);

            if (usesPerRun <= 0 || __instance == null || _setVitality == null) return true;

            var main = GameSingleton.Existing<BattleMain>();
            var leader = GameSingleton.Existing<BattleLogicWorld>()?._AgentManager?.GetLeadingRole();
            var attributes = AgentTools.GetAgentComponent<AttributeComponent>(__instance);

            if (main == null || !main._IsLoadingFinished || leader == null || attributes == null) return true;

            var vitality = attributes.GetBaseValue_Float(AttrName.Vitality);
            var usesSpent = GameCounter.GetCounter(UsesSpentKey);
            var isLeader = __instance.Pointer == leader.Pointer;

            if (!LoopPerks.TurnsDeathAway(isLeader, __instance.IsDead, main.IsGameOver, vitality, usesPerRun, usesSpent)) return true;

            var left = LoopPerks.VitalityAfterSecondWind(Perks.Share(NlpEffect.SecondWind));

            _setVitality.Invoke(attributes, new object[] { AttrName.Vitality, left, true });
            GameCounter.SetCounter(UsesSpentKey, usesSpent + 1);
            _log($"Second Wind turned a death away: Vitality {vitality:0.##} set to {left:0.##}, {usesPerRun - usesSpent - 1} of {usesPerRun} uses left this run");
            EndRunTicker.Page?.Toast(Texts.SecondWindUsed.Format(GameLanguage.IsChinese(), usesPerRun - usesSpent - 1));

            return false;
        }
        catch (Exception ex)
        {
            if (!_hasReportedFailure)
            {
                _hasReportedFailure = true;
                _log("Second Wind failed, the death went ahead (reported once): " + ex);
            }

            return true;
        }
    }
}
