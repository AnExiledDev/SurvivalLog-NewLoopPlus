using Jint;

namespace NewLoopPlus.Tests;

/// <summary>page.js run in Jint on the fake root page PageFake.js; <c>before</c> runs between the two.</summary>
internal sealed class RootPage
{
    private readonly Engine _engine = new();

    public RootPage(string? before = null)
    {
        _engine.Execute(Resource("NewLoopPlus.Tests.PageFake.js"));
        if (before != null) _engine.Execute(before);
        _engine.Execute(Resource("NewLoopPlus.page.js"));
    }

    public void Run(string statements) => _engine.Execute(statements);

    public int Number(string expression) => (int)_engine.Evaluate($"Number({expression})").AsNumber();

    public bool Bool(string expression) => _engine.Evaluate($"!!({expression})").AsBoolean();

    private static string Resource(string name)
    {
        using var stream = typeof(RootPage).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException(name + " is not embedded in the test assembly");
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
