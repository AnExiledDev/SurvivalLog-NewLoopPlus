using System;
using System.Collections.Generic;
using System.IO;
using ExiledToolkit.Game;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>
/// The plugin's side of the Talents tab in the Mod Manager: hands the tab the game's talents and the talents file, and
/// writes the file back when the tab saves. The game's talents are read once, before the file changed them, so the tab
/// always compares against the game's own rewards. A save takes effect at the next start, like any edit of the file.
/// </summary>
internal sealed class TalentsTab
{
    private readonly string _path;
    private readonly WebPage _page;
    private readonly Action<string> _log;
    private IReadOnlyList<GameTalent> _game = Array.Empty<GameTalent>();
    private IReadOnlyList<string> _scripts = Array.Empty<string>();
    private IReadOnlyList<GameIcon> _items = Array.Empty<GameIcon>();
    private IReadOnlyList<string> _startProblems = Array.Empty<string>();

    /// <summary>With Wind Power installed, the tab offers the wind effect for a new reward.</summary>
    public bool HasWindPower { get; set; }

    public TalentsTab(string path, WebPage page, Action<string> log)
    {
        _path = path;
        _page = page;
        _log = log;
    }

    /// <summary>What the game had, the effect scripts its talents run, its item images, and what went wrong writing the file into it, all as of this start.</summary>
    public void Started(IReadOnlyList<GameTalent> game, IReadOnlyList<string> scripts, IReadOnlyList<GameIcon> items, IReadOnlyList<string> problems)
    {
        _game = game;
        _scripts = scripts;
        _items = items;
        _startProblems = problems;
    }

    public void Load()
    {
        string? text = null;

        try
        {
            text = File.ReadAllText(_path);
        }
        catch (Exception ex)
        {
            _log("Talents tab: could not read the talents file: " + ex.Message);
        }

        _page.Call(TalentsPage.DataCall(_game, _scripts, _items, text, _startProblems, HasWindPower));
    }

    public void Save(string json)
    {
        _page.Call(TalentsPage.CheckSave(json) switch
        {
            SaveCheck.Write write => Write(write.Text),
            SaveCheck.Refused refused => Refuse(refused.Problems),
            _ => throw new InvalidOperationException("unknown save check"),
        });
    }

    private string Write(string text)
    {
        var chinese = GameLanguage.IsChinese();

        try
        {
            if (File.Exists(_path)) File.Copy(_path, _path + ".bak", overwrite: true);

            File.WriteAllText(_path, text);
            _log($"Talents tab saved the talents file ({text.Length} characters); the old one is {_path}.bak");

            return TalentsPage.SavedCall(true, Texts.TalentsSaved.Pick(chinese));
        }
        catch (Exception ex)
        {
            _log("Talents tab: could not save the talents file: " + ex.Message);

            return TalentsPage.SavedCall(false, Texts.TalentsNotSaved.Pick(chinese) + " " + ex.Message);
        }
    }

    private string Refuse(IReadOnlyList<string> problems)
    {
        foreach (var problem in problems) _log("Talents tab save refused: " + problem);

        return TalentsPage.SavedCall(false, Texts.TalentsNotSaved.Pick(GameLanguage.IsChinese()) + " " + string.Join("; ", problems));
    }
}
