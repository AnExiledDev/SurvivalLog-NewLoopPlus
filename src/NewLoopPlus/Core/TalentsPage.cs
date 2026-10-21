using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NewLoopPlus.Core;

/// <summary>One level of a game talent as the Talents tab shows it. <paramref name="Values"/> are the level effect's fields.</summary>
public sealed record GameLevel(int Lv, int Cost, int Buff, LocalText Description, string Script, IReadOnlyList<FieldSet> Values);

/// <summary>A game talent as the Talents tab shows it, read before the talents file changed anything.</summary>
public sealed record GameTalent(int Id, int Type, int Endless, int Available, int Show, string WebIcon, string Icon, LocalText Name, LocalText LevelText, IReadOnlyList<GameLevel> Levels);

/// <summary>What a save from the Talents tab comes to: the text to write, or why nothing is written.</summary>
public abstract record SaveCheck
{
    private SaveCheck()
    {
    }

    public sealed record Write(string Text) : SaveCheck;

    public sealed record Refused(IReadOnlyList<string> Problems) : SaveCheck;
}

/// <summary>What the plugin sends the Talents tab, and what it makes of a save.</summary>
public static class TalentsPage
{
    public const int FirstTalent = 1000;
    public const int LastTalent = 9999;

    private const string ScriptPrefix = "Buff/";

    private static readonly JsonDocumentOptions Forgiving = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /// <summary>
    /// The script that hands the tab its data. The talents file goes as plain JSON, comments dropped, or as null when it is
    /// not JSON at all, which the tab takes as "do not save over it". Every non-ASCII character is escaped, so the data can
    /// never end the script early. <paramref name="scripts"/> are the effect scripts any game talent runs, loop reward or
    /// not, since a give can use any of them.
    /// </summary>
    public static string DataCall(IReadOnlyList<GameTalent> game, IEnumerable<string> scripts, string? fileText, IReadOnlyList<string> startProblems)
    {
        var problems = (fileText == null ? new[] { "the talents file could not be read" } : TalentFile.Parse(fileText).Problems).Concat(startProblems).Distinct().ToList();

        return "window.__slnlpTalents&&window.__slnlpTalents.data(" + Json(writer => WriteData(writer, game, scripts, fileText, problems)) + ")";
    }

    /// <summary>A reply to a save, shown in the tab's status line.</summary>
    public static string SavedCall(bool isSaved, string message) =>
        $"window.__slnlpTalents&&window.__slnlpTalents.saved({(isSaved ? "true" : "false")},{JsText.Literal(message)})";

    /// <summary>A save is written only if every entry of it reads cleanly, laid out for reading by hand.</summary>
    public static SaveCheck CheckSave(string json)
    {
        var problems = TalentFile.Parse(json).Problems;

        if (problems.Count > 0) return new SaveCheck.Refused(problems);

        using var document = JsonDocument.Parse(json, Forgiving);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            document.RootElement.WriteTo(writer);
        }

        return new SaveCheck.Write(Encoding.UTF8.GetString(stream.ToArray()) + Environment.NewLine);
    }

    /// <summary>Game pages sit two folders below the web root and reach images with "../../"; the tab runs at the root.</summary>
    public static string FromRoot(string webIcon)
    {
        var path = webIcon;

        while (path.StartsWith("../", StringComparison.Ordinal)) path = path.Substring(3);

        return path;
    }

    private static void WriteData(Utf8JsonWriter writer, IReadOnlyList<GameTalent> game, IEnumerable<string> scripts, string? fileText, IReadOnlyList<string> problems)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("game");

        foreach (var talent in game.Where(talent => talent.Id is >= FirstTalent and <= LastTalent).OrderBy(talent => talent.Id)) WriteTalent(writer, talent);

        writer.WriteEndArray();
        writer.WriteStartArray("scripts");

        foreach (var script in scripts.Select(Unprefixed).Distinct().OrderBy(script => script, StringComparer.Ordinal)) writer.WriteStringValue(script);

        writer.WriteEndArray();
        writer.WritePropertyName("file");
        WriteFile(writer, fileText);
        writer.WriteStartArray("problems");

        foreach (var problem in problems) writer.WriteStringValue(problem);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteFile(Utf8JsonWriter writer, string? fileText)
    {
        try
        {
            using var document = JsonDocument.Parse(fileText ?? "", Forgiving);

            document.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteNullValue();
        }
    }

    private static void WriteTalent(Utf8JsonWriter writer, GameTalent talent)
    {
        writer.WriteStartObject();
        writer.WriteNumber("id", talent.Id);
        writer.WriteNumber("type", talent.Type);
        writer.WriteNumber("endless", talent.Endless);
        writer.WriteNumber("available", talent.Available);
        writer.WriteNumber("show", talent.Show);
        writer.WriteString("icon", FromRoot(talent.WebIcon));
        writer.WriteString("unityIcon", talent.Icon);
        WriteText(writer, "name", talent.Name);
        WriteText(writer, "levelText", talent.LevelText);
        writer.WriteStartArray("levels");

        foreach (var level in talent.Levels.OrderBy(level => level.Lv)) WriteLevel(writer, level);

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string Unprefixed(string script) =>
        script.StartsWith(ScriptPrefix, StringComparison.Ordinal) ? script.Substring(ScriptPrefix.Length) : script;

    private static void WriteLevel(Utf8JsonWriter writer, GameLevel level)
    {
        writer.WriteStartObject();
        writer.WriteNumber("cost", level.Cost);
        writer.WriteNumber("buff", level.Buff);
        WriteText(writer, "desc", level.Description);
        writer.WriteString("script", Unprefixed(level.Script));
        writer.WriteStartObject("values");

        foreach (var field in level.Values.Where(field => field.Field is not ("ID" or "ConfigName"))) WriteValue(writer, field);

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    /// <summary>Only the fields a level actually sets: zeros, empty text and false say nothing about the effect.</summary>
    private static void WriteValue(Utf8JsonWriter writer, FieldSet field)
    {
        switch (field.Value)
        {
            case FieldValue.Number { Value: not 0.0 } number:
                writer.WriteNumber(field.Field, number.Value);
                break;
            case FieldValue.Text { Value.Length: > 0 } text:
                writer.WriteString(field.Field, text.Value);
                break;
            case FieldValue.Flag { Value: true }:
                writer.WriteBoolean(field.Field, true);
                break;
        }
    }

    private static void WriteText(Utf8JsonWriter writer, string name, LocalText text)
    {
        writer.WriteStartObject(name);
        writer.WriteString("en", text.En);
        writer.WriteString("zh", text.Zh);
        writer.WriteEndObject();
    }

    private static string Json(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
