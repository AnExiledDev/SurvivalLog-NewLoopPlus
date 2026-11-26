using System;
using System.Collections.Generic;
using System.IO;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle;
using GameCore.HotUpdate.Battle.Logic;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The talents that act once as a run starts: Muscle Memory brings the skills up towards the last run's levels, and
/// Scout's Notes marks trading posts as already found. Each is done once per run: the run's own counters (GameCounter,
/// saved with the run) say it was, so a reload does not do it again. A run counts as started a few seconds after it has
/// loaded and the clock runs, so the game's start-of-run levels and tags are in first. The tutorial is left alone.
/// While a run goes on, its skill levels are written to <see cref="MuscleMemory"/>'s file under the save's name
/// whenever they change, for the next run in that save to read.
/// </summary>
internal sealed class RunStartPerks
{
    private const float CheckSeconds = 1f;
    private const float SettleSeconds = 3f;
    private const string MuscleMemoryDoneKey = "nlp.musclememory";
    private const string ScoutsNotesDoneKey = "nlp.scoutsnotes";

    private readonly string _levelsPath;
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();
    private Dictionary<int, int> _recorded = new();
    private string? _recordedSave;
    private double _nextCheckAt;
    private double? _readySince;

    public RunStartPerks(string levelsPath, Action<string> log)
    {
        _levelsPath = levelsPath;
        _log = log;
    }

    public void Tick(double now)
    {
        if (now < _nextCheckAt) return;

        _nextCheckAt = now + CheckSeconds;

        if (ReadyWorld() is not { } world)
        {
            _readySince = null;

            return;
        }

        _readySince ??= now;

        if (now - _readySince < SettleSeconds) return;

        Once(MuscleMemoryDoneKey, () => BringBackSkills(world));
        Once(ScoutsNotesDoneKey, () => MarkTradingPosts(world));

        if (GameCounter.GetCounter(MuscleMemoryDoneKey) > 0) Record(world);
    }

    /// <summary>A run in play: loaded, its clock running, not over and not the tutorial.</summary>
    private static BattleLogicWorld? ReadyWorld()
    {
        var main = GameSingleton.Existing<BattleMain>();
        var world = GameSingleton.Existing<BattleLogicWorld>();

        if (main == null || !main._IsLoadingFinished || main.IsGameOver) return null;

        return world == null || !world.IsUpdate || world.GameModeKey == GameMode.Guide ? null : world;
    }

    /// <summary>Marked done whether or not the talent is owned, so a talent bought later in the loop waits for the next run.</summary>
    private void Once(string doneKey, Action apply)
    {
        if (GameCounter.GetCounter(doneKey) > 0) return;

        GameCounter.SetCounter(doneKey, 1);

        try
        {
            apply();
        }
        catch (Exception ex)
        {
            Report(doneKey, ex);
        }
    }

    private void BringBackSkills(BattleLogicWorld world)
    {
        var share = Perks.Share(NlpEffect.MuscleMemory);
        var skills = world._ProficiencyManager;

        if (share <= 0 || skills == null || SaveName() is not { } save || !File.Exists(_levelsPath)) return;

        var last = MuscleMemory.ParseFor(File.ReadAllText(_levelsPath), save);
        var raised = new List<string>();

        foreach (var skill in MuscleMemory.Skills)
        {
            if (!last.TryGetValue(skill, out var lastLevel)) continue;

            var type = (ProficiencyType)skill;
            var level = skills.GetLevel(type);
            var target = MuscleMemory.Target(skill, lastLevel, share);

            if (target <= level) continue;

            skills.AddLevel(type, target - level, false);
            raised.Add($"{type} {level} to {skills.GetLevel(type)}");
        }

        if (raised.Count == 0) return;

        _log($"Muscle Memory ({share:0.##} of the last run): " + string.Join(", ", raised));
        EndRunTicker.Page?.Toast(Texts.MuscleMemoryUsed.Pick(GameLanguage.IsChinese()));
    }

    private void MarkTradingPosts(BattleLogicWorld world)
    {
        var count = Perks.Whole(NlpEffect.ScoutsNotes);
        var tags = world._TagManager;
        var points = GameSingleton.Existing<ConfigManager>()?._Config_MapPoint_Dict;

        if (count <= 0 || tags == null || points == null) return;

        var known = new List<(int Id, string Tag)>();

        foreach (var point in points.Values)
        {
            if (point?._UnlockTag_k__BackingField is { } tag) known.Add((point._ID_k__BackingField, tag));
        }

        var scouted = LoopPerks.ScoutedTags(known, tags.HasTag, count);

        if (scouted.Count == 0) return;

        var list = new Il2CppSystem.Collections.Generic.List<string>();

        foreach (var tag in scouted) list.Add(tag);

        tags.AddTags(list);
        _log("Scout's Notes marked " + string.Join(", ", scouted));
        EndRunTicker.Page?.Toast(Texts.ScoutsNotesUsed.Say(GameLanguage.IsChinese(), scouted.Count));
    }

    /// <summary>Writes the run's skill levels when they change; a write that fails is tried again next check.</summary>
    private void Record(BattleLogicWorld world)
    {
        var skills = world._ProficiencyManager;

        if (skills == null || SaveName() is not { } save) return;

        if (save != _recordedSave)
        {
            _recordedSave = save;
            _recorded = new Dictionary<int, int>();
        }

        var levels = new Dictionary<int, int>();

        foreach (var skill in MuscleMemory.Skills) levels[skill] = skills.GetLevel((ProficiencyType)skill);

        if (IsSame(levels, _recorded)) return;

        try
        {
            var text = File.Exists(_levelsPath) ? File.ReadAllText(_levelsPath) : "";

            File.WriteAllText(_levelsPath, MuscleMemory.FormatFor(text, save, levels));
            _recorded = levels;
        }
        catch (Exception ex)
        {
            Report("recording skill levels", ex);
        }
    }

    /// <summary>The loaded save's file name, which a new loop keeps; null with no save loaded.</summary>
    private static string? SaveName() => GameSingleton.Existing<GameSaveManager>()?.GetGameSave()?.FileName is { Length: > 0 } name ? name : null;

    private static bool IsSame(Dictionary<int, int> a, Dictionary<int, int> b)
    {
        if (a.Count != b.Count) return false;

        foreach (var pair in a)
        {
            if (!b.TryGetValue(pair.Key, out var other) || other != pair.Value) return false;
        }

        return true;
    }

    private void Report(string doing, Exception ex)
    {
        if (_reportedFailures.Add(doing)) _log($"run start perks: {doing} failed (reported once): {ex}");
    }
}
