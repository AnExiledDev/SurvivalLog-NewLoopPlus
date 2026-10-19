using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using NewLoopPlus.Adapters;
using NewLoopPlus.Core;

namespace NewLoopPlus;

[BepInPlugin(Guid, "New Loop Plus", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.newloopplus";
    private const string Version = "0.1.0";

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
    }
}
