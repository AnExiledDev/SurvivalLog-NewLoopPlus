using System.Linq;
using System.Text.Json;
using NewLoopPlus.Core;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>The Talents tab sees the game's own talents and the file, and a save never writes a file the mod cannot read.</summary>
public class TalentsPageTests
{
    private const string CallStart = "window.__slnlpTalents&&window.__slnlpTalents.data(";

    private static readonly GameTalent Awakening = new(1001, 1, 0, 1, 1, "../../Res/Material/UI_Item_Icon_Equip_S_1.png", "Assets/RuntimeAssets/Texture/Gather/UI_Item_Icon_Equip_S_1",
        new LocalText("Awakening", "觉醒"), new LocalText("+1h / +2h", "+1小时 / +2小时"), new[]
        {
            new GameLevel(2, 8, 100120, new LocalText("2 hours", "2小时"), "Buff/PreDisaster_AddCountDownTime", new FieldSet[] { new("IntValue0", new FieldValue.Number(7200)) }),
            new GameLevel(1, 5, 100110, new LocalText("1 hour </script>", "1小时"), "Buff/PreDisaster_AddCountDownTime", new FieldSet[]
            {
                new("ID", new FieldValue.Number(200100110)),
                new("IntValue0", new FieldValue.Number(3600)),
                new("FloatValue0", new FieldValue.Number(0)),
                new("StrValue0", new FieldValue.Text("")),
                new("IsLoop", new FieldValue.Flag(false)),
            }),
        });

    private static readonly GameTalent EndlessSolar = Awakening with { Id = 31271, WebIcon = "../../Res/icon/UI_Talent_Solar.png", Icon = "Assets/Solar" };

    private static readonly GameIcon[] Items =
    {
        new("../../Res/Food/UI_Item_Icon_Food_tang.png", "Assets/Food/tang"),
        new("../../Res/Material/UI_Item_Icon_Equip_S_1.png", "Assets/Gather/other"),
        new("", "Assets/NoWebIcon"),
    };

    private static JsonElement Data(string? fileText, params string[] startProblems)
    {
        var call = TalentsPage.DataCall(new[] { Awakening, EndlessSolar }, new[] { "PreDisaster_AddCountDownTime", "AE_PowerSolarEfficiency", "Buff/AE_PowerSolarEfficiency" }, Items, fileText, startProblems);

        Assert.StartsWith(CallStart, call);
        Assert.EndsWith(")", call);
        Assert.All(call, character => Assert.True(character < 128, $"non-ASCII {(int)character} in the script"));
        Assert.DoesNotContain("</", call);

        return JsonDocument.Parse(call.Substring(CallStart.Length, call.Length - CallStart.Length - 1)).RootElement;
    }

    [Fact]
    public void The_tab_gets_the_loop_reward_talents_with_their_levels_in_order_and_icons_it_can_reach()
    {
        var game = Data("{}").GetProperty("game");
        var talent = Assert.Single(game.EnumerateArray());
        var levels = talent.GetProperty("levels");

        Assert.Equal(1001, talent.GetProperty("id").GetInt32());
        Assert.Equal("Res/Material/UI_Item_Icon_Equip_S_1.png", talent.GetProperty("icon").GetString());
        Assert.Equal("觉醒", talent.GetProperty("name").GetProperty("zh").GetString());
        Assert.Equal(5, levels[0].GetProperty("cost").GetInt32());
        Assert.Equal(8, levels[1].GetProperty("cost").GetInt32());
        Assert.Equal("1 hour </script>", levels[0].GetProperty("desc").GetProperty("en").GetString());
        Assert.Equal("PreDisaster_AddCountDownTime", levels[0].GetProperty("script").GetString());
    }

    [Fact]
    public void The_tab_learns_every_script_a_give_can_use_even_one_only_an_endless_reward_runs()
    {
        var scripts = Data("{}").GetProperty("scripts").EnumerateArray().Select(script => script.GetString()).ToList();

        Assert.Equal(new[] { "AE_PowerSolarEfficiency", "PreDisaster_AddCountDownTime" }, scripts);
    }

    [Fact]
    public void A_reward_can_show_the_image_of_any_talent_or_item_each_offered_once_under_its_group()
    {
        var icons = Data("{}").GetProperty("icons").EnumerateArray()
            .Select(icon => (icon.GetProperty("group").GetString(), icon.GetProperty("icon").GetString(), icon.GetProperty("unityIcon").GetString())).ToList();

        Assert.Equal(new (string?, string?, string?)[]
        {
            ("rewards", "Res/Material/UI_Item_Icon_Equip_S_1.png", "Assets/RuntimeAssets/Texture/Gather/UI_Item_Icon_Equip_S_1"),
            ("rewards", "Res/icon/UI_Talent_Solar.png", "Assets/Solar"),
            ("Food", "Res/Food/UI_Item_Icon_Food_tang.png", "Assets/Food/tang"),
        }, icons);
    }

    [Fact]
    public void A_level_carries_only_the_effect_values_it_sets()
    {
        var values = Data("{}").GetProperty("game")[0].GetProperty("levels")[0].GetProperty("values");

        Assert.Equal("IntValue0", Assert.Single(values.EnumerateObject()).Name);
        Assert.Equal(3600, values.GetProperty("IntValue0").GetInt32());
    }

    [Fact]
    public void The_file_reaches_the_tab_without_its_comments_and_a_broken_file_reaches_it_as_null()
    {
        var file = Data("""
            // hand notes
            { "talents": [ { "id": 1006, }, ], }
            """).GetProperty("file");

        Assert.Equal(1006, file.GetProperty("talents")[0].GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, Data("{ talents: oops").GetProperty("file").ValueKind);
        Assert.Equal(JsonValueKind.Null, Data(null).GetProperty("file").ValueKind);
    }

    [Fact]
    public void Problems_in_the_file_and_from_the_last_start_both_reach_the_tab_once_each()
    {
        var problems = Data("""{ "talents": [ { "enabled": true } ] }""", "talent 5001 was skipped", "talent 5001 was skipped").GetProperty("problems");

        Assert.Equal(2, problems.GetArrayLength());
        Assert.Equal("talent 5001 was skipped", problems[1].GetString());
    }

    [Fact]
    public void A_save_with_any_unreadable_entry_is_refused_with_the_reason()
    {
        var check = TalentsPage.CheckSave("""{ "talents": [ { "id": 1002, "gives": [ { "choice": {} } ] } ] }""");

        Assert.Contains("give #1", Assert.Single(Assert.IsType<SaveCheck.Refused>(check).Problems));
        Assert.IsType<SaveCheck.Refused>(TalentsPage.CheckSave("not json"));
    }

    [Fact]
    public void A_good_save_is_written_laid_out_for_reading_with_its_chinese_kept_as_text()
    {
        var check = TalentsPage.CheckSave("""{"talents":[{"id":5001,"name":{"en":"Scout","zh":"侦察"}}]}""");
        var text = Assert.IsType<SaveCheck.Write>(check).Text;

        Assert.Contains("侦察", text);
        Assert.Contains("\n", text.TrimEnd());
        Assert.Equal(5001, Assert.Single(TalentFile.Parse(text).Talents).Id);
    }

    [Theory]
    [InlineData("../../Res/Material/a.png", "Res/Material/a.png")]
    [InlineData("Res/Material/a.png", "Res/Material/a.png")]
    [InlineData("", "")]
    public void Icons_are_reached_from_the_web_root(string webIcon, string fromRoot)
    {
        Assert.Equal(fromRoot, TalentsPage.FromRoot(webIcon));
    }
}
