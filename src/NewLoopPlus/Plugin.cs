using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NewLoopPlus.Adapters;
using NewLoopPlus.Core;

namespace NewLoopPlus;

[BepInPlugin(Guid, "New Loop Plus", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.newloopplus";
    private const string Version = "0.7.0";
    private const string TalentsFileName = "NewLoopPlus.Talents.json";
    private const string MuscleMemoryFileName = "NewLoopPlus.MuscleMemory.txt";
    private const float MaxMultiplier = 100f;
    private const string AppliesAtRestart = "ModManager.Restart";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var log = new ModLog(Log, Path.Combine(Paths.BepInExRootPath, "new-loop-plus.log"));
        var harmony = new Harmony(Guid);
        var page = new WebPage(log.Write);
        var talentsPath = Path.Combine(Paths.ConfigPath, TalentsFileName);
        var tab = new TalentsTab(talentsPath, page, log.Write);

        log.Write($"===== New Loop Plus {Version} started");
        PageMessages.Apply(harmony, log.Write);
        CompanionBuffs.Apply(harmony, log.Write);
        EndRunTicker.Page = page;
        EndRunTicker.Talents = tab;
        EndRunTicker.Flow = new EndRun(new GameRun(log.Write), page, log.Write);
        EndRunTicker.Log = log.Write;
        AddComponent<EndRunTicker>();
        LoopStartBonus.Apply(harmony, log.Write);
        ExtraPick.Apply(harmony, log.Write);
        SecondWind.Apply(harmony, log.Write);
        LuckyFinds.Apply(harmony, log.Write);
        Tinkerer.Apply(harmony, log.Write);
        OptionalLive.Watch(BindStartingBonus(), bonus => LoopStartBonus.Bonus = bonus);
        StartScaling(BindCosts(log.Write), TalentFileStore.Load(talentsPath, log.Write), tab, log.Write);
    }

    /// <summary>
    /// The points tables are always checked, because a multiplier changed in game may need them put back. The talents
    /// file is written before the costs are scaled, so a cost it sets is scaled like the game's own.
    /// </summary>
    private void StartScaling(CostMultipliers costs, TalentFile talents, TalentsTab tab, System.Action<string> log)
    {
        var points = new PointsTables(log);
        var reinforce = talents.ReinforceGroups();

        WatchPoints(points, log);
        ScaleTicker.Checks.Add(points.Tick);
        ScaleTicker.Checks.Add(new TalentTables(talents, tab, log).Tick);
        ScaleTicker.Checks.Add(new RunStartPerks(Path.Combine(Paths.ConfigPath, MuscleMemoryFileName), log).Tick);
        ScaleTicker.Checks.Add(new PerkTables(log).Tick);

        if (reinforce.Count > 0) ScaleTicker.Checks.Add(new DoorWindowReinforcement(reinforce, log).Tick);

        if (costs.ChangesAnything) ScaleTicker.Checks.Add(new TalentCosts(costs, log).Tick);
        else log("loop reward costs are the game's own");

        ScaleTicker.Log = log;
        AddComponent<ScaleTicker>();
    }

    private void WatchPoints(PointsTables tables, System.Action<string> log)
    {
        var overall = Multiplier("Points", "OverallMultiplier",
            "Multiplies every Planning Point a run earns, on top of the three settings below. 1 is the game's amount, 2 doubles it, 0.5 halves it.\n" +
            "一局获得的所有信念点都乘以这个倍数，并与下面三项叠加。1 为游戏原本的数量，2 为两倍，0.5 为一半。");
        var survival = Multiplier("Points", "SurvivalMultiplier",
            "Multiplies the Planning Points that survival points turn into when a run ends.\n" +
            "结算时由生存点换算出的信念点乘以这个倍数。");
        var crisis = Multiplier("Points", "CrisisMultiplier",
            "Multiplies the Planning Points that crises and events reward, both during the run and at its end. Penalties stay as they are.\n" +
            "危机与事件奖励的信念点（局内获得的和结算时获得的）乘以这个倍数。扣除的信念点不变。");
        var record = Multiplier("Points", "RecordMultiplier",
            "Multiplies the Planning Points paid for each survived day.\n" +
            "每存活一天所得的信念点乘以这个倍数。");
        var endless = Multiplier("Endless", "StartingPointsMultiplier",
            "Multiplies the Planning Points a new endless game starts with (500 in the game). 0 starts with none.\n" +
            "新开无尽模式时初始信念点（游戏中为 500）乘以这个倍数。0 为没有初始信念点。");

        OptionalLive.Watch(() =>
        {
            var multipliers = new PointsMultipliers(Rounded(overall), Rounded(survival), Rounded(crisis), Rounded(record), Rounded(endless));

            tables.Use(multipliers);
            log(multipliers.ChangesAnything ? $"Planning Points multipliers: {multipliers}" : "Planning Points multipliers are all 1, points are the game's own");
        }, overall, survival, crisis, record, endless);
    }

    private ConfigEntry<int> BindStartingBonus() =>
        Config.Bind("Loop", "StartingBonus", 0, new ConfigDescription(
            "Extra Planning Points paid at the start of each new loop, as the loop reward screen opens after a run, so they can be spent there. 0 pays none.\n" +
            "每次开始新轮回时额外获得的信念点，在一局结束后打开轮回奖励界面时发放，可以当场使用。0 为不发放。",
            new AcceptableValueRange<int>(0, StartingBonus.Max)));

    private CostMultipliers BindCosts(System.Action<string> log)
    {
        var overall = Rounded(Multiplier("Costs", "CostMultiplier",
            "Multiplies what every loop reward upgrade costs in Planning Points. 1 is the game's price, 0.5 halves it, 0 makes upgrades free.\n" +
            "所有天赋升级消耗的信念点乘以这个倍数。1 为游戏原价，0.5 为半价，0 为免费。",
            AppliesAtRestart));
        var text = Config.Bind("Costs", "TalentCostOverrides", "", new ConfigDescription(
            "Gives single loop rewards their own cost multiplier in place of CostMultiplier, as talent ID:multiplier pairs separated by commas. Example: 1001:0.5, 2017:2\n" +
            "为单个天赋设置自己的消耗倍数（代替 CostMultiplier），格式为 天赋ID:倍数，用逗号分隔。例如：1001:0.5, 2017:2",
            null, AppliesAtRestart)).Value;
        var parsed = CostOverrides.Parse(text);

        foreach (var rejected in parsed.Rejected) log($"TalentCostOverrides: could not read \"{rejected}\", it was skipped (use talentId:multiplier, such as 1001:0.5)");

        return new CostMultipliers(overall, parsed.PerTalent);
    }

    /// <summary>Costs apply at the next start: a price changed while the loop reward screen is open would break its reset, which refunds what was paid.</summary>
    private ConfigEntry<float> Multiplier(string section, string key, string description, params object[] tags) =>
        Config.Bind(section, key, 1f, new ConfigDescription(description, new AcceptableValueRange<float>(0f, MaxMultiplier), tags));

    /// <summary>Rounded so a setting such as 1.1 multiplies by 1.1, not by the float nearest it.</summary>
    private static double Rounded(ConfigEntry<float> entry) => System.Math.Round((double)entry.Value, 4);
}
