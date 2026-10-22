using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>
/// Muscle Memory: a new run starts with a share of the skill levels the last run reached. The levels are kept in a
/// small text file between runs, one "skill=level" line per skill, because a new run's save starts them over.
/// </summary>
public static class MuscleMemory
{
    /// <summary>The game's skills (ProficiencyType): Production, Planting, Cooking, Trapping, Exploring, Defense.</summary>
    public static readonly IReadOnlyList<int> Skills = new[] { 1, 2, 3, 4, 5, 6 };

    /// <summary>Planting and Cooking start a run at level 0, the others at level 1.</summary>
    public static int StartLevel(int skill) => skill is 2 or 3 ? 0 : 1;

    /// <summary>The level a new run brings the skill up to: the start level plus the share of what the last run gained on it, rounded down.</summary>
    public static int Target(int skill, int lastLevel, double share)
    {
        var start = StartLevel(skill);

        if (lastLevel <= start || share <= 0) return start;

        return start + (int)Math.Floor((lastLevel - start) * Math.Min(1, share));
    }

    /// <summary>The levels in a file's text; a line that is not "skill=level" with a known skill is skipped.</summary>
    public static IReadOnlyDictionary<int, int> Parse(string text)
    {
        var levels = new Dictionary<int, int>();

        foreach (var line in text.Split('\n'))
        {
            var parts = line.Trim().Split('=');

            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var skill) || !Skills.Contains(skill)) continue;

            if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) && level >= 0) levels[skill] = level;
        }

        return levels;
    }

    public static string Format(IReadOnlyDictionary<int, int> levels) =>
        string.Join("\n", Skills.Where(levels.ContainsKey).Select(skill => $"{skill}={levels[skill].ToString(CultureInfo.InvariantCulture)}")) + "\n";
}
