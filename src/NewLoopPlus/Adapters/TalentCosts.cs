using System;
using GameCore.HotUpdate;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// Scales what loop reward upgrades cost in the game's talent table (Config_Talent rows of type 1; type 2 rows are
/// survival plans, bought with survival points, and stay as they are). The upgrade screen, the purchase and the
/// screen's reset all read LvUpCost from this table. A reset only undoes upgrades bought since the screen opened, at
/// the costs in force then, so it refunds what was paid.
/// </summary>
internal sealed class TalentCosts
{
    private const float CheckSeconds = 1f;
    private const int LoopRewardType = 1;

    private readonly CostMultipliers _multipliers;
    private readonly Action<string> _log;
    private readonly ScaledValues<(int RowId, int TalentId), int> _costs;
    private float _nextCheckAt;

    public TalentCosts(CostMultipliers multipliers, Action<string> log)
    {
        _multipliers = multipliers;
        _log = log;
        _costs = new ScaledValues<(int RowId, int TalentId), int>((row, cost) => multipliers.Cost(row.TalentId, cost));
    }

    public void Tick(float now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        var rows = GameSingleton.Existing<ConfigManager>()?._Config_Talent_Dict;

        if (rows == null || rows.Count == 0) return;

        var scaled = 0;

        foreach (var row in rows.Values)
        {
            if (row == null || row._Type_k__BackingField != LoopRewardType) continue;

            if (_costs.Next((row._ID_k__BackingField, row._TalentID_k__BackingField), row._LvUpCost_k__BackingField) is not { } cost) continue;

            row._LvUpCost_k__BackingField = cost;
            scaled++;
        }

        if (scaled > 0) _log($"{scaled} loop reward levels now cost {_multipliers.Overall:0.##}x the game's price, {_multipliers.PerTalent.Count} talents with their own multiplier");
    }
}
