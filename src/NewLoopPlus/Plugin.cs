using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using NewLoopPlus.Adapters;
using NewLoopPlus.Core;

namespace NewLoopPlus;

[BepInPlugin(Guid, "New Loop Plus", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
[BepInDependency(WindLink.Guid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.newloopplus";
    private const string Version = "0.11.3";

    /// <summary>Exiled's Toolkit: its shared helpers, toasts, live settings and the End Run button's place in the pause menu all come from it.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.20.0";

    private const string LogFileName = "new-loop-plus.log";
    private const string TalentsFileName = "NewLoopPlus.Talents.json";
    private const string MuscleMemoryFileName = "NewLoopPlus.MuscleMemory.txt";
    private const float MaxMultiplier = 100f;
    private const string AppliesAtRestart = "ModManager.Restart";

    public override void Load()
    {
        var log = new ModLog(Log, LogFileName);
        var harmony = new Harmony(Guid);
        var hooks = new PatchKit(harmony, log.Write);
        var page = new WebPage(log.Write);
        var talentsPath = Path.Combine(Paths.ConfigPath, TalentsFileName);
        var tab = new TalentsTab(talentsPath, page, log.Write);

        log.Write($"===== New Loop Plus {Version} started");
        CompanionBuffs.Apply(hooks);
        DroneReturnShow.Apply(hooks);
        EndRunTicker.Messages = ExiledToolkit.Web.PageInbox.Listen(PageCommand.Prefix);
        EndRunTicker.Page = page;
        EndRunTicker.Talents = tab;
        var endRun = new EndRun(new GameRun(log.Write), page, log.Write);
        EndRunTicker.Flow = endRun;
        EndRunTicker.Log = log.Write;
        AddComponent<EndRunTicker>();
        AddEndRunButton(endRun);
        LoopStartBonus.Apply(hooks, log.Write);
        ExtraPick.Apply(hooks, log.Write);
        SecondWind.Apply(hooks, log.Write);
        LuckyFinds.Apply(hooks);
        Tinkerer.Apply(hooks, log.Write);
        MentalToughnessUses.Apply(hooks, log.Write);
        ExploreTravelHooks.Prepare(hooks);
        LoanTierHooks.Prepare(hooks);
        EndlessRewardsSetting.On = BindEndlessEverywhere().Value;
        if (EndlessRewardsSetting.On) EndlessRewardHooks.Apply(hooks, log.Write);
        LiveSettings.Watch(BindStartingBonus(), bonus => LoopStartBonus.Bonus = bonus);
        tab.HasWindPower = LinkWind(log.Write);
        var talents = TalentFileStore.Load(talentsPath, log.Write);
        StartScaling(BindCosts(log.Write), tab.HasWindPower ? talents : WindLink.WithoutWind(talents), tab, log.Write);
    }

    /// <summary>
    /// With Wind Power installed, the wind talents owned make its turbines stronger. Without it, or with a Wind Power too
    /// old to take them, the wind talents stay out of the talent tree and the tab does not offer them.
    /// </summary>
    private static bool LinkWind(System.Action<string> log)
    {
        if (!IL2CPPChainloader.Instance.Plugins.ContainsKey(WindLink.Guid)) return false;

        try
        {
            if (WindLink.Add(System.AppDomain.CurrentDomain.GetAssemblies(), "New Loop Plus", () => WindLink.Multiplier(Perks.Share(NlpEffect.WindPower))))
            {
                log("wind talents work with Wind Power's turbines");

                return true;
            }

            log("Wind Power is older than 0.9.3, so the wind talents are left out");
        }
        catch (System.Exception ex)
        {
            log("could not hand the wind talents to Wind Power, so they are left out: " + ex.Message);
        }

        return false;
    }

    /// <summary>
    /// The points tables are always checked, because a multiplier changed in game may need them put back. The talents
    /// file is written before the costs are scaled, so a cost it sets is scaled like the game's.
    /// </summary>
    private void StartScaling(CostMultipliers costs, TalentFile talents, TalentsTab tab, System.Action<string> log)
    {
        var points = new PointsTables(log);
        var reinforce = talents.ReinforceGroups();

        WatchPoints(points, log);
        ScaleTicker.Checks.Add(points.Tick);
        ScaleTicker.Checks.Add(new TalentTables(talents, tab, log, EndlessRewardsSetting.On).Tick);
        ScaleTicker.Checks.Add(new RunStartPerks(Path.Combine(Paths.ConfigPath, MuscleMemoryFileName), log).Tick);
        ScaleTicker.Checks.Add(new PerkTables(log).Tick);

        if (reinforce.Count > 0) ScaleTicker.Checks.Add(new DoorWindowReinforcement(reinforce, log).Tick);

        if (costs.ChangesAnything) ScaleTicker.Checks.Add(new TalentCosts(costs, log).Tick);
        else log("loop reward costs are the game's");

        ScaleTicker.Log = log;
        AddComponent<ScaleTicker>();
    }

    private void WatchPoints(PointsTables tables, System.Action<string> log)
    {
        var overall = Multiplier("Points", "OverallMultiplier",
            "Multiplies every Planning Point a run earns, on top of the three settings below. 1 changes nothing, 2 doubles it, 0.5 halves it.\n" +
            "一局获得的所有信念点都乘以这个倍数，并与下面三项叠加。1 表示不变，2 为两倍，0.5 为一半。");
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
            "Multiplies the Planning Points a new endless game starts with (normally 500). 0 starts with none.\n" +
            "新开无尽模式时初始信念点（原本为 500）乘以这个倍数。0 为没有初始信念点。");

        LiveSettings.Watch(() =>
        {
            var multipliers = new PointsMultipliers(Rounded(overall), Rounded(survival), Rounded(crisis), Rounded(record), Rounded(endless));

            tables.Use(multipliers);
            log(multipliers.ChangesAnything ? $"Planning Points multipliers: {multipliers}" : "Planning Points multipliers are all 1, points are the game's");
        }, overall, survival, crisis, record, endless);
    }

    private ConfigEntry<int> BindStartingBonus() =>
        Config.Bind("Loop", "StartingBonus", 0, new ConfigDescription(
            "Extra Planning Points paid at the start of each new loop, as the loop reward screen opens after a run, so they can be spent there. 0 pays none.\n" +
            "每次开始新轮回时额外获得的信念点，在一局结束后打开轮回奖励界面时发放，可以当场使用。0 为不发放。",
            new AcceptableValueRange<int>(0, StartingBonus.Max)));

    private ConfigEntry<bool> BindEndlessEverywhere() =>
        Config.Bind("Loop", "EndlessRewardsEverywhere", false, new ConfigDescription(
            "Offers the endless mode loop rewards in every mode, so they can be bought and work outside endless too. The camp rewards and Cartographer stay endless only. Applies at the next game start.\n" +
            "在所有模式中提供无尽模式的天赋，在无尽模式之外也能购买并生效。营地类天赋和「地图学家」仍只限无尽模式。下次启动游戏时生效。",
            null, AppliesAtRestart));

    private CostMultipliers BindCosts(System.Action<string> log)
    {
        var overall = Rounded(Multiplier("Costs", "CostMultiplier",
            "Multiplies what every loop reward upgrade costs in Planning Points. 1 keeps the normal price, 0.5 halves it, 0 makes upgrades free.\n" +
            "所有天赋升级消耗的信念点乘以这个倍数。1 为原价，0.5 为半价，0 为免费。",
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
    /// <summary>
    /// End Run at the bottom of the pause menu, below every other mod's buttons and right above the game's Restart and
    /// Save &amp; Exit. Exiled's Toolkit places it with the other mods' menu buttons, so the menu fits on screen.
    /// </summary>
    private static void AddEndRunButton(EndRun endRun)
    {
        ExiledToolkit.GameMenu.Add(new ExiledToolkit.GameMenuButton("newloopplus.endrun", Texts.Button.En, ExiledToolkit.GameMenuPlace.PauseBottom, () => EndRunTicker.IsAskPending = true)
        {
            ChineseLabel = Texts.Button.Zh,
            Order = 900,
            Accent = "#EBA308",
            IsShown = () => endRun.IsButtonShown,
        });
    }

    private static double Rounded(ConfigEntry<float> entry) => System.Math.Round((double)entry.Value, 4);
}
