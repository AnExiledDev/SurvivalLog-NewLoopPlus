using System.Collections.Generic;
using System.Globalization;

namespace NewLoopPlus.Core;

/// <summary>One piece of on-screen text in the two languages the game ships.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    /// <summary>The picked text with its {0}, {1}, ... filled in.</summary>
    public string Format(bool isChinese, params object[] values) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), values);
}

/// <summary>The words the mod puts on screen.</summary>
public static class Texts
{
    public static readonly Phrase Button = new("End Run", "结束本局");

    public static readonly Phrase ConfirmTitle = new("End this run?", "结束本局？");

    public static readonly Phrase Cancel = new("Cancel", "取消");

    public static readonly Phrase EndsLikeDeath = new(
        "The run ends now and is settled as if your character had died.",
        "本局将立即结束，按角色死亡结算。");

    public static readonly Phrase PointsLine = new(
        "You will get {0} Planning Points: {1} from survival points, {2} from crises and {3} from days survived.",
        "将获得信念点{0}：生存点{1}，危机{2}，生存天数{3}。");

    public static readonly Phrase PointsUnknown = new(
        "The Planning Points you will get could not be worked out; the death screen shows them.",
        "无法预估将获得的信念点，结算界面会显示实际数值。");

    public static readonly Phrase CasualLine = new("Casual mode gives no Planning Points.", "休闲模式不获得信念点。");

    public static readonly Phrase ModeSettlesLine = new(
        "This mode settles the run on its own results screen.",
        "本模式会在其结算界面结算本局。");

    public static readonly Phrase CannotEndNow = new("The run cannot be ended right now.", "当前无法结束本局。");

    public static readonly Phrase StillPaused = new(
        "End Run could not close the pause menu, so the run was not ended. See BepInEx/new-loop-plus.log.",
        "无法关闭暂停菜单，本局未结束。详见 BepInEx/new-loop-plus.log。");

    public static readonly Phrase Failed = new("End Run failed, see BepInEx/new-loop-plus.log.", "结束本局失败，详见 BepInEx/new-loop-plus.log。");

    public static readonly Phrase TalentsSaved = new(
        "Saved. Changes apply the next time you start the game.",
        "已保存。更改将在下次启动游戏时生效。");

    public static readonly Phrase TalentsNotSaved = new("Not saved:", "未保存：");

    public static readonly Phrase InterestPaid = new("Interest: +{0} Planning Points", "利息：+{0}信念点");

    public static readonly Phrase SafetyNetPaid = new("Safety Net: +{0} Planning Points", "保底：+{0}信念点");

    public static readonly Phrase SecondWindUsed = new(
        "Second Wind: you pulled through. Uses left this run: {0}",
        "绝处逢生：你挺了过来。本局剩余次数：{0}");

    public static readonly Phrase MuscleMemoryUsed = new("Muscle Memory: some of your skill levels came back.", "肌肉记忆：部分技能等级恢复了。");

    public static readonly Phrase ScoutsNotesUsed = new("Scout's Notes: {0} trading posts are marked on the map.", "侦察笔记：地图上标出了{0}个交易点。");

    public static IReadOnlyList<Phrase> All { get; } = new[]
    {
        Button, ConfirmTitle, Cancel, EndsLikeDeath, PointsLine, PointsUnknown, CasualLine, ModeSettlesLine, CannotEndNow, StillPaused, Failed,
        TalentsSaved, TalentsNotSaved, InterestPaid, SafetyNetPaid, SecondWindUsed, MuscleMemoryUsed, ScoutsNotesUsed,
    };
}
