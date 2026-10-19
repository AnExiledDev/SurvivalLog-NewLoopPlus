using System;
using GameCore.HotUpdate;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Scales the Planning Points a run earns by editing the game's own numbers, so the death screen, its breakdown and
/// the End Run preview all show the scaled points with nothing patched:
/// survival points convert at GlobalSetting SurvivalToBelief, each survived day pays GlobalSetting RecordRewardBelief,
/// and every crisis and event reward pays its Config_RewardSet BeliefPoint (both the in-run grants and the crisis
/// total the death screen adds up). A new endless game starts with GlobalSetting Endless_FaithGrant. The GlobalSetting
/// values are also cached by GameKey once read, so the caches are kept in step. The multipliers can change while the
/// game runs; the next check then scales every value again from the game's own.
/// </summary>
internal sealed class PointsTables
{
    private const float CheckSeconds = 1f;

    private readonly Action<string> _log;
    private readonly ScaledValues<string, float> _rates;
    private readonly ScaledValues<string, int> _perDay;
    private readonly ScaledValues<int, int> _rewards;
    private readonly ScaledValues<string, int> _endlessGrant;
    private PointsMultipliers _multipliers = PointsMultipliers.None;
    private float _nextCheckAt;

    public PointsTables(Action<string> log)
    {
        _log = log;
        _rates = new ScaledValues<string, float>((_, rate) => _multipliers.SurvivalRate(rate));
        _perDay = new ScaledValues<string, int>((_, belief) => _multipliers.RecordPerDay(belief));
        _rewards = new ScaledValues<int, int>((_, belief) => _multipliers.EventReward(belief));
        _endlessGrant = new ScaledValues<string, int>((_, belief) => _multipliers.EndlessGrant(belief));
    }

    /// <summary>Takes new multipliers; the next check writes them, and checks again at once.</summary>
    public void Use(PointsMultipliers multipliers)
    {
        if (multipliers == _multipliers) return;

        _multipliers = multipliers;
        _rates.Rescale();
        _perDay.Rescale();
        _rewards.Rescale();
        _endlessGrant.Rescale();
        _nextCheckAt = 0;
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var config = GameSingleton.Existing<ConfigManager>();

        if (config == null) return;

        ScaleSurvivalRate(config);
        ScaleRecordReward(config);
        ScaleEventRewards(config);
        ScaleEndlessGrant(config);
    }

    private void ScaleSurvivalRate(ConfigManager config)
    {
        var key = GameKey.Key_GlobalSetting_SurvivalToBelief;
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return;

        if (_rates.Next(key, entry._Params2_k__BackingField) is { } rate)
        {
            entry._Params2_k__BackingField = rate;
            _log($"survival points now convert at {rate:0.####} per point (game: {_rates.Original(key):0.####})");
        }

        if (GameCache.Next(GameKey._GlobalSetting_SurvivalToBelief, _rates.Scaled(key)) is { } cached) GameKey._GlobalSetting_SurvivalToBelief = cached;
    }

    private void ScaleRecordReward(ConfigManager config)
    {
        var key = GameKey.Key_GlobalSetting_RecordRewardBelief;
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return;

        if (_perDay.Next(key, entry._Params1_k__BackingField) is { } perDay)
        {
            entry._Params1_k__BackingField = perDay;
            _log($"each survived day now pays {perDay} Planning Points (game: {_perDay.Original(key)})");
        }

        if (GameCache.Next(GameKey._GlobalSetting_RecordRewardBelief, _perDay.Scaled(key)) is { } cached) GameKey._GlobalSetting_RecordRewardBelief = cached;
    }

    private void ScaleEventRewards(ConfigManager config)
    {
        var rows = config._Config_RewardSet_Dict;

        if (rows == null || rows.Count == 0) return;

        var scaled = 0;

        foreach (var row in rows.Values)
        {
            if (row == null || _rewards.Next(row._ID_k__BackingField, row._BeliefPoint_k__BackingField) is not { } belief) continue;

            row._BeliefPoint_k__BackingField = belief;
            scaled++;
        }

        if (scaled > 0) _log($"{scaled} crisis and event rewards now pay {Math.Max(0, _multipliers.Crisis) * Math.Max(0, _multipliers.Overall):0.##}x the game's Planning Points");
    }

    private void ScaleEndlessGrant(ConfigManager config)
    {
        var key = GameKey.Key_GlobalSetting_Endless_FaithGrant;
        var table = config._Config_GlobalSetting_Dict;

        if (key == null || table == null || !table.TryGetValue(key, out var entry) || entry == null) return;

        if (_endlessGrant.Next(key, entry._Params1_k__BackingField) is { } grant)
        {
            entry._Params1_k__BackingField = grant;
            _log($"a new endless game now starts with {grant} Planning Points (game: {_endlessGrant.Original(key)})");
        }

        if (GameCache.Next(GameKey._GlobalSetting_Endless_FaithGrant, _endlessGrant.Scaled(key)) is { } cached) GameKey._GlobalSetting_Endless_FaithGrant = cached;
    }
}
