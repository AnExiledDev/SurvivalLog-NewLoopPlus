using System.Collections.Generic;
using System.Globalization;

namespace NewLoopPlus.Core;

/// <summary>The per-talent cost setting read into multipliers, with every entry that could not be read.</summary>
public sealed record ParsedOverrides(IReadOnlyDictionary<int, double> PerTalent, IReadOnlyList<string> Rejected);

/// <summary>
/// Reads the per-talent cost setting: comma-separated <c>talentId:multiplier</c> pairs such as <c>1001:0.5, 2017:2</c>.
/// A bad entry is skipped and reported rather than failing the whole setting; a talent listed twice keeps its last value.
/// </summary>
public static class CostOverrides
{
    public static ParsedOverrides Parse(string? text)
    {
        var perTalent = new Dictionary<int, double>();
        var rejected = new List<string>();

        foreach (var raw in (text ?? "").Split(','))
        {
            var entry = raw.Trim();

            if (entry.Length == 0) continue;

            if (TryParseEntry(entry, out var talentId, out var multiplier)) perTalent[talentId] = multiplier;
            else rejected.Add(entry);
        }

        return new ParsedOverrides(perTalent, rejected);
    }

    private static bool TryParseEntry(string entry, out int talentId, out double multiplier)
    {
        multiplier = 0;

        var parts = entry.Split(':');

        if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out talentId) || talentId <= 0)
        {
            talentId = 0;

            return false;
        }

        return double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out multiplier) && multiplier >= 0 && !double.IsInfinity(multiplier);
    }
}
