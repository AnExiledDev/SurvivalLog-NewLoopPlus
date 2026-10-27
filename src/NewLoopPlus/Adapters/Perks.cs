using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// How much of each New Loop Plus effect the player has right now. The talents file says which talents give what
/// (<see cref="Grants"/>, set once the file is written into the tables); the save says which levels the player owns
/// (GameSaveData.TalentData.TalentLevelMap, the map the game copies into a run's buffs in TalentComponent.OnInitBuffs).
/// Static because the Harmony hooks that read it are.
/// </summary>
internal static class Perks
{
    private static IReadOnlyList<NlpGrant> _grants = Array.Empty<NlpGrant>();

    public static IReadOnlyList<NlpGrant> Grants
    {
        set => _grants = value;
    }

    /// <summary>True when some enabled talent gives the effect, owned or not.</summary>
    public static bool IsOffered(string effect) => _grants.Any(grant => grant.Effect == effect);

    /// <summary>The effect's field summed over the talents owned; 0 with no save loaded.</summary>
    public static double Value(string effect, string field)
    {
        if (!IsOffered(effect)) return 0;

        var owned = GameSingleton.Existing<GameSaveManager>()?.gameSave?.TalentData?.TalentLevelMap;

        if (owned == null) return 0;

        return NlpPerks.Value(_grants, talentId => owned.ContainsKey(talentId) ? owned[talentId] : 0, effect, field);
    }

    /// <summary>The level owned of a talent; 0 with no save loaded.</summary>
    public static int Level(int talentId)
    {
        var owned = GameSingleton.Existing<GameSaveManager>()?.gameSave?.TalentData?.TalentLevelMap;

        return owned != null && owned.ContainsKey(talentId) ? owned[talentId] : 0;
    }

    public static int Whole(string effect) => (int)Math.Round(Value(effect, NlpPerks.Whole));

    public static double Share(string effect) => Value(effect, NlpPerks.Share);
}
