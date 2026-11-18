using Jint;
using Xunit;

namespace NewLoopPlus.Tests;

/// <summary>
/// talents-tab.js as the plugin ships it, on the reward editor from Exiled's Toolkit's source (editor.js), run in
/// Jint. The tab is fed data the way the plugin sends it, but never drawn: drawing it needs a page.
/// </summary>
public class TalentsTabTests
{
    private readonly Engine _engine = new();

    public TalentsTabTests()
    {
        _engine.Execute("var window = this;");
        _engine.Execute(Resource("ExiledToolkit.editor.js"));
        // Keep the tab's side (T) and its editor (ed) so the tests can reach them.
        _engine.Execute("var make = window.ExiledEditor.make; window.ExiledEditor.make = function (t) { window.T = t; window.ed = make(t); return window.ed; };");
        _engine.Execute(Resource("NewLoopPlus.talents-tab.js"));
    }

    // An effect's page quotes the cards of the game rewards that use it, with the card's line for each level, so a
    // player can tell what the numbers do. An effect with no name says where to look instead of "raw fields".
    [Fact]
    public void An_effect_page_shows_the_card_text_of_the_rewards_that_use_it()
    {
        Load(scripts: "['AE_HomePackageMax', 'AE_NoNameYet']", game: """
            [
              { id: 3101, type: 1, name: { en: 'Big Entryway', zh: '大玄关' }, levelText: { en: '1 / 2', zh: '1 / 2' }, levels: [
                { cost: 10, buff: 0, desc: { en: 'The Entryway holds more packages.', zh: '玄关能放更多包裹。' }, script: 'AE_HomePackageMax', values: { FloatValue0: 1 } },
                { cost: 14, buff: 0, desc: { en: 'The Entryway holds even more packages.', zh: '玄关能放更多的包裹。' }, script: 'AE_HomePackageMax', values: { FloatValue0: 2 } } ] },
              { id: 3102, type: 1, name: { en: 'Odd Luck', zh: '怪运' }, levelText: { en: '', zh: '' }, levels: [
                { cost: 10, buff: 0, desc: { en: 'Something rare happens.', zh: '会发生罕见的事。' }, script: 'AE_NoNameYet', values: { IntValue0: 3 } } ] }
            ]
            """);
        _engine.Execute("T.st.picker = { selId: 'game:AE_HomePackageMax' };");

        var named = Text("T.detailHtml(T.st.entries[0], ed.cat.list)");

        Assert.Contains("WHAT THE GAME SAYS", named);
        Assert.Contains("Big Entryway", named);
        Assert.Contains("The Entryway holds more packages.", named);
        Assert.Contains("By level: 1 / 2", named);
        Assert.DoesNotContain("has no name yet", named);

        _engine.Execute("T.st.picker = { selId: 'raw:AE_NoNameYet' };");

        var raw = Text("T.detailHtml(T.st.entries[0], ed.cat.list)");

        Assert.Contains("WHAT THE GAME SAYS", raw);
        Assert.Contains("Something rare happens.", raw);
        Assert.Contains(Text("ed.RAW_NOTE[0]"), raw);
        Assert.DoesNotContain("no friendly names", raw);
    }

    // The mod gives Resourceful a new effect, so its page must not quote the game's old card for that reward.
    [Fact]
    public void A_mod_effect_does_not_quote_the_game_card_it_replaces()
    {
        Load(scripts: "['PreDisaster_AddRandomItem']", game: """
            [
              { id: 2004, type: 1, name: { en: 'Resourceful', zh: '物尽其用' }, levelText: { en: '', zh: '' }, levels: [
                { cost: 10, buff: 0, desc: { en: 'Turn junk items into raw materials.', zh: '把废品变成原材料。' }, script: 'PreDisaster_AddRandomItem', values: { IntValue0: 1 } } ] }
            ]
            """);
        _engine.Execute("T.st.picker = { selId: 'nlp:junk' };");

        var html = Text("T.detailHtml(T.st.entries[0], ed.cat.list)");

        Assert.DoesNotContain("WHAT THE GAME SAYS", html);
        Assert.DoesNotContain("Turn junk items into raw materials.", html);
    }

    // The tab used to add its own drone and generator effects next to the editor's, so both showed up in the effect
    // list under different names and numbers.
    [Fact]
    public void Each_game_script_shows_once_in_the_effect_list()
    {
        Load(scripts: "['AE_DroneForagingExtraTrip', 'AE_PowerGeneratorEfficiency']", game: "[]");

        foreach (var script in new[] { "AE_DroneForagingExtraTrip", "AE_PowerGeneratorEfficiency" })
        {
            Assert.Equal(1.0, _engine.Evaluate($"ed.cat.list.filter(function (f) {{ return f.scripts.indexOf('{script}') >= 0; }}).length").AsNumber());
            Assert.False(Bool($"ed.cat.list.find(function (f) {{ return f.scripts.indexOf('{script}') >= 0; }}).raw"));
        }
    }

    private void Load(string scripts, string game)
    {
        _engine.Execute($$"""
            window.document = window.document || { addEventListener: function () {}, removeEventListener: function () {} };
            window.__slnlpTalents.data({ scripts: {{scripts}}, game: {{game}}, icons: [], file: { talents: [ { id: 3101 } ] }, problems: [] });
            """);
    }

    private bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    private string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private static string Resource(string name)
    {
        using var stream = typeof(TalentsTabTests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
