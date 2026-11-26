using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NewLoopPlus.Core;

/// <summary>
/// Muscle Memory: a new run starts with a share of the skill levels the last run reached. The levels are kept in a
/// small text file between runs, because a new run's save starts them over. Each save has its own lines,
/// "save|skill=level", so a run in one save does not change what another save brings back. A new loop stays in the same
/// save file. Lines written before saves were told apart ("skill=level") are read for a save with no lines of its own.
/// </summary>
public static class MuscleMemory
{
    private const char SaveMark = '|';

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
        string.Join("\n", Lines(levels)) + "\n";

    /// <summary>The levels kept for <paramref name="save"/>, or the old lines with no save when it has none of its own.</summary>
    public static IReadOnlyDictionary<int, int> ParseFor(string text, string save)
    {
        var own = Parse(string.Join("\n", text.Split('\n').Select(line => line.Trim()).Where(line => SaveOf(line) == save).Select(line => line.Substring(line.LastIndexOf(SaveMark) + 1))));

        return own.Count > 0 ? own : Parse(text);
    }

    /// <summary>The file's text with <paramref name="save"/>'s lines set to <paramref name="levels"/>; every other line stays.</summary>
    public static string FormatFor(string text, string save, IReadOnlyDictionary<int, int> levels)
    {
        var kept = text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && SaveOf(line) != save);

        return string.Join("\n", kept.Concat(Lines(levels).Select(line => save + SaveMark + line))) + "\n";
    }

    private static IEnumerable<string> Lines(IReadOnlyDictionary<int, int> levels) =>
        Skills.Where(levels.ContainsKey).Select(skill => $"{skill}={levels[skill].ToString(CultureInfo.InvariantCulture)}");

    private static string? SaveOf(string line)
    {
        var at = line.LastIndexOf(SaveMark);

        return at < 0 ? null : line.Substring(0, at);
    }
}
