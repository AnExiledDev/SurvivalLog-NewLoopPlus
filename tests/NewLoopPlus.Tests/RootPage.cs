using Jint;

namespace NewLoopPlus.Tests;

/// <summary>
/// page.js exactly as the plugin embeds it, run in Jint with Exiled's Toolkit's page API (api.js, from the Toolkit's
/// source) on the fake root page PageFake.js.
/// </summary>
internal sealed class RootPage
{
    private readonly Engine _engine = new();

    /// <param name="isToolkitFirst">
    /// The game runs the Toolkit's script first, or with false New Loop Plus's first; then the Toolkit's runs only at
    /// <see cref="LoadToolkit"/>.
    /// </param>
    public RootPage(bool isToolkitFirst = true)
    {
        _engine.Execute(Resource("NewLoopPlus.Tests.PageFake.js"));

        if (isToolkitFirst) LoadToolkit();

        _engine.Execute(Resource("NewLoopPlus.page.js"));
    }

    /// <summary>The Toolkit's page API arriving, after New Loop Plus's script when the page was built with isToolkitFirst false.</summary>
    public void LoadToolkit() => _engine.Execute(Resource("ExiledToolkit.api.js"));

    public void Run(string statements) => _engine.Execute(statements);

    public int Number(string expression) => (int)_engine.Evaluate($"Number({expression})").AsNumber();

    public bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    public string Text(string expression) => _engine.Evaluate($"String({expression})").AsString();

    private static string Resource(string name)
    {
        using var stream = typeof(RootPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
