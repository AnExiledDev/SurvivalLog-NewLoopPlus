using System;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Resourceful Management (talent 1011) past level 3. The phone offers three bigger loans, read from levels 1 to 3:
/// WebUI_PhoneSMS.GetLoanExtraBonusByLv sets the amounts shown, and LoanSharkManager.GetExtraTierBonus (tiers 4 to 6)
/// the money paid out and owed. Both move up to the level owned, so the last choice matches the credit limit. The hooks
/// go in only once a level past 3 is owned and leave the game alone otherwise. Static because a Harmony hook has
/// nowhere else to look.
/// </summary>
internal static class LoanTierHooks
{
    public const int TalentId = 1011;

    private static Harmony? _harmony;
    private static Action<string> _log = _ => { };
    private static bool _hooked;

    /// <summary>The level of Resourceful Management owned; set by PerkTables.</summary>
    public static int Owned { get; private set; }

    public static void Prepare(Harmony harmony, Action<string> log)
    {
        _harmony = harmony;
        _log = log;
    }

    public static void Use(int owned)
    {
        Owned = owned;

        if (_harmony == null || owned <= PerkSettings.PhoneLoanLevels || _hooked) return;

        _hooked = true;
        Hook(AccessTools.Method(typeof(WebUI_PhoneSMS), "GetLoanExtraBonusByLv"), new HarmonyMethod(typeof(LoanTierHooks), nameof(BeforeShownBonus)), null, "the phone's loan amounts");
        Hook(AccessTools.Method(typeof(LoanSharkManager), "GetExtraTierBonus"), null, new HarmonyMethod(typeof(LoanTierHooks), nameof(AfterTierBonus)), "the loan money paid out");
    }

    private static void Hook(System.Reflection.MethodBase? target, HarmonyMethod? prefix, HarmonyMethod? postfix, string what)
    {
        try
        {
            if (target == null) throw new MissingMethodException(what);

            _harmony!.Patch(target, prefix: prefix, postfix: postfix);
        }
        catch (Exception ex)
        {
            _log($"could not hook {what}, the bigger loans stay at level 3: {ex.Message}");
        }
    }

    private static void BeforeShownBonus(ref int __0)
    {
        __0 = PerkSettings.LoanLevel(__0, Owned);
    }

    private static void AfterTierBonus(int __0, ref int __result)
    {
        if (Owned <= PerkSettings.PhoneLoanLevels || __0 < 4 || __0 > 6) return;

        var config = GameSingleton.Existing<ConfigManager>();
        var talent = config?.GetTalentConfig(TalentId, PerkSettings.LoanLevel(__0 - 3, Owned));
        var buff = talent == null || talent._BuffID_k__BackingField <= 0 ? null : config!.Get_Config_Buff(talent._BuffID_k__BackingField);
        var effect = buff == null || buff._EffectConfigId_k__BackingField <= 0 ? null : config!.Get_Config_Effect(buff._EffectConfigId_k__BackingField);

        if (effect != null) __result = effect._IntValue0_k__BackingField;
    }
}
