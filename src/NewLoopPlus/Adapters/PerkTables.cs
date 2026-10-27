using System;
using GameCore.HotUpdate;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Family Recipe and Drone Pilot change the game's own settings, so cooking, the trade drone and their previews all
/// show the change with nothing patched. Family Recipe adds to GlobalSetting CookingQuality_FreshBonus and
/// CookingQuality_ExactMatchBonus and lowers CookingMultiPot_UnlockLevel; Drone Pilot shortens
/// StrangerTradeDroneRoundtripSec. The GameKey caches of the four are kept in step too. The amounts follow the levels
/// owned, so each check works them out again and, when they changed, every value is worked out again from the game's own.
/// </summary>
internal sealed class PerkTables
{
    private const float CheckSeconds = 1f;

    private readonly Action<string> _log;
    private readonly ScaledValues<string, int> _whole;
    private readonly ScaledValues<string, float> _multiPot;
    private Amounts _amounts = new(0, 0, 0);
    private float _nextCheckAt;

    public PerkTables(Action<string> log)
    {
        _log = log;
        _whole = new ScaledValues<string, int>((key, value) => key == GameKey.Key_GlobalSetting_StrangerTradeDroneRoundtripSec ? PerkSettings.TripSeconds(value, _amounts.TripCut) : PerkSettings.CookingBonus(value, _amounts.CookingBonus));
        _multiPot = new ScaledValues<string, float>((_, level) => PerkSettings.MultiPotLevel(level, _amounts.LevelsSooner));
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        // With neither effect offered and nothing written, the game's own values stand and there is nothing to check.
        if (!Perks.IsOffered(NlpEffect.FamilyRecipe) && !Perks.IsOffered(NlpEffect.DronePilot) && _amounts == new Amounts(0, 0, 0)) return;

        var amounts = new Amounts(Perks.Whole(NlpEffect.DronePilot), Perks.Whole(NlpEffect.FamilyRecipe), (int)Math.Round(Perks.Value(NlpEffect.FamilyRecipe, NlpPerks.SecondWhole)));

        if (amounts != _amounts)
        {
            _amounts = amounts;
            _whole.Rescale();
            _multiPot.Rescale();
        }

        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null) return;

        if (Whole(config, GameKey.Key_GlobalSetting_StrangerTradeDroneRoundtripSec, "a trade drone trip now takes {0} seconds (game: {1})") is { } trip && GameCache.Next(GameKey._GlobalSetting_StrangerTradeDroneRoundtripSec, trip) is { } cachedTrip) GameKey._GlobalSetting_StrangerTradeDroneRoundtripSec = cachedTrip;

        if (Whole(config, GameKey.Key_GlobalSetting_CookingQuality_FreshBonus, "fresh ingredients now add {0} cooking quality (game: {1})") is { } fresh && GameCache.Next(GameKey._GlobalSetting_CookingQuality_FreshBonus, fresh) is { } cachedFresh) GameKey._GlobalSetting_CookingQuality_FreshBonus = cachedFresh;

        if (Whole(config, GameKey.Key_GlobalSetting_CookingQuality_ExactMatchBonus, "an exact recipe now adds {0} cooking quality (game: {1})") is { } exact && GameCache.Next(GameKey._GlobalSetting_CookingQuality_ExactMatchBonus, exact) is { } cachedExact) GameKey._GlobalSetting_CookingQuality_ExactMatchBonus = cachedExact;

        if (MultiPot(config) is { } level && GameCache.Next(GameKey._GlobalSetting_CookingMultiPot_UnlockLevel, level) is { } cachedLevel) GameKey._GlobalSetting_CookingMultiPot_UnlockLevel = cachedLevel;
    }

    /// <summary>Writes a Params1 setting; returns the value it should hold, or null before the table is loaded.</summary>
    private int? Whole(ConfigManager config, string? key, string written)
    {
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return null;

        if (_whole.Next(key, entry._Params1_k__BackingField) is { } value)
        {
            entry._Params1_k__BackingField = value;
            _log(string.Format(written, value, _whole.Original(key)));
        }

        return _whole.Scaled(key);
    }

    private float? MultiPot(ConfigManager config)
    {
        var key = GameKey.Key_GlobalSetting_CookingMultiPot_UnlockLevel;
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return null;

        if (_multiPot.Next(key, entry._Params2_k__BackingField) is { } level)
        {
            entry._Params2_k__BackingField = level;
            _log($"multi-pot cooking now unlocks at Cooking {level:0} (game: {_multiPot.Original(key):0})");
        }

        return _multiPot.Scaled(key);
    }

    private readonly record struct Amounts(int TripCut, int CookingBonus, int LevelsSooner);
}
