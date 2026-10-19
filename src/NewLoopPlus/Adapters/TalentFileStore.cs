using System;
using System.IO;
using System.Reflection;
using NewLoopPlus.Core;

namespace NewLoopPlus.Adapters;

/// <summary>Reads the talents file from the config folder, first writing the default one there if it is missing.</summary>
internal static class TalentFileStore
{
    private const string DefaultResource = "NewLoopPlus.talents.default.json";

    public static TalentFile Load(string path, Action<string> log)
    {
        try
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, DefaultText());
                log($"wrote the default talents file to {path}");
            }

            var file = TalentFile.Parse(File.ReadAllText(path));

            foreach (var problem in file.Problems) log("talents file: " + problem);

            return file;
        }
        catch (Exception ex)
        {
            log($"talents file could not be read, so no talent changes apply: {ex.Message}");

            return TalentFile.Empty;
        }
    }

    private static string DefaultText()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(DefaultResource) ?? throw new InvalidOperationException("the default talents file is missing from the mod");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
