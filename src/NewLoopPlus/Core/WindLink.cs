using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NewLoopPlus.Core;

/// <summary>
/// Wind Power's turbine multipliers, found by name so this mod loads and runs without Wind Power. The method is
/// WindPower.WindBoosts.Add(string source, Func&lt;float&gt; multiplier), in Wind Power 0.9.3 or newer; every
/// turbine's rating is multiplied by what the function returns, read once a second.
/// </summary>
public static class WindLink
{
    public const string Guid = "com.dev.survivallog.windpower";
    public const string TypeName = "WindPower.WindBoosts";

    /// <summary>Hands the multiplier to Wind Power. False when no loaded assembly has Wind Power's Add.</summary>
    public static bool Add(IEnumerable<Assembly> assemblies, string source, Func<float> multiplier)
    {
        var add = Find(assemblies);

        if (add == null) return false;

        add.Invoke(null, new object[] { source, multiplier });

        return true;
    }

    /// <summary>What the turbines make with the wind talents owned: 1 plus their share, never below the turbines' own.</summary>
    public static float Multiplier(double share) => double.IsNaN(share) ? 1f : 1f + (float)Math.Max(0.0, share);

    /// <summary>
    /// The file without its wind talents, for a game with no Wind Power to give them to: they are left out of the
    /// talent tree, as if switched off.
    /// </summary>
    public static TalentFile WithoutWind(TalentFile file) =>
        file with { Talents = file.Talents.Where(talent => !talent.Gives.Any(IsWind)).ToList() };

    private static bool IsWind(GiveEntry give) => give.Effect == TalentPlan.NlpPrefix + NlpEffect.WindPower;

    private static MethodInfo? Find(IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            Type? type;

            try
            {
                type = assembly.GetType(TypeName);
            }
            catch (Exception)
            {
                continue;
            }

            var add = type?.GetMethod("Add", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(Func<float>) }, null);

            if (add != null) return add;
        }

        return null;
    }
}
