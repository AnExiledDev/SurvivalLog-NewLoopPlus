using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NewLoopPlus.Adapters;
using NewLoopPlus.Core;

namespace NewLoopPlus;

[BepInPlugin(Guid, "New Loop Plus", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.newloopplus";
    private const string Version = "0.2.0";
    private const float MaxMultiplier = 100f;

    public override void Load()
    {
        var log = new ModLog(Log, Path.Combine(Paths.BepInExRootPath, "new-loop-plus.log"));
        var harmony = new Harmony(Guid);
        var page = new WebPage(log.Write);

        log.Write($"===== New Loop Plus {Version} started");
        PageMessages.Apply(harmony, log.Write);
        EndRunTicker.Page = page;
        EndRunTicker.Flow = new EndRun(new GameRun(log.Write), page, log.Write);
        EndRunTicker.Log = log.Write;
        AddComponent<EndRunTicker>();
        StartScaling(BindPoints(), BindCosts(log.Write), log.Write);
    }

    private void StartScaling(PointsMultipliers points, CostMultipliers costs, System.Action<string> log)
    {
        if (points.ChangesAnything) ScaleTicker.Checks.Add(new PointsTables(points, log).Tick);
        else log("Planning Points multipliers are all 1, points are the game's own");

        if (costs.ChangesAnything) ScaleTicker.Checks.Add(new TalentCosts(costs, log).Tick);
        else log("loop reward costs are the game's own");

        if (ScaleTicker.Checks.Count == 0) return;

        ScaleTicker.Log = log;
        AddComponent<ScaleTicker>();
    }

    private PointsMultipliers BindPoints() => new(
        Multiplier("Points", "OverallMultiplier",
            "Multiplies every Planning Point a run earns, on top of the three settings below. 1 is the game's amount, 2 doubles it, 0.5 halves it.\n" +
            "一局获得的所有信念点都乘以这个倍数，并与下面三项叠加。1 为游戏原本的数量，2 为两倍，0.5 为一半。"),
        Multiplier("Points", "SurvivalMultiplier",
            "Multiplies the Planning Points that survival points turn into when a run ends.\n" +
            "结算时由生存点换算出的信念点乘以这个倍数。"),
        Multiplier("Points", "CrisisMultiplier",
            "Multiplies the Planning Points that crises and events reward, both during the run and at its end. Penalties stay as they are.\n" +
            "危机与事件奖励的信念点（局内获得的和结算时获得的）乘以这个倍数。扣除的信念点不变。"),
        Multiplier("Points", "RecordMultiplier",
            "Multiplies the Planning Points paid for each survived day.\n" +
            "每存活一天所得的信念点乘以这个倍数。"));

    private CostMultipliers BindCosts(System.Action<string> log)
    {
        var overall = Multiplier("Costs", "CostMultiplier",
            "Multiplies what every loop reward upgrade costs in Planning Points. 1 is the game's price, 0.5 halves it, 0 makes upgrades free.\n" +
            "所有天赋升级消耗的信念点乘以这个倍数。1 为游戏原价，0.5 为半价，0 为免费。");
        var text = Config.Bind("Costs", "TalentCostOverrides", "",
            "Gives single loop rewards their own cost multiplier in place of CostMultiplier, as talent ID:multiplier pairs separated by commas. Example: 1001:0.5, 2017:2\n" +
            "为单个天赋设置自己的消耗倍数（代替 CostMultiplier），格式为 天赋ID:倍数，用逗号分隔。例如：1001:0.5, 2017:2").Value;
        var parsed = CostOverrides.Parse(text);

        foreach (var rejected in parsed.Rejected) log($"TalentCostOverrides: could not read \"{rejected}\", it was skipped (use talentId:multiplier, such as 1001:0.5)");

        return new CostMultipliers(overall, parsed.PerTalent);
    }

    /// <summary>Rounded so a setting such as 1.1 multiplies by 1.1, not by the float nearest it.</summary>
    private double Multiplier(string section, string key, string description) =>
        System.Math.Round((double)Config.Bind(section, key, 1f, new ConfigDescription(description, new AcceptableValueRange<float>(0f, MaxMultiplier))).Value, 4);
}
