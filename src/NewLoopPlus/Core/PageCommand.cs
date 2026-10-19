using System;

namespace NewLoopPlus.Core;

/// <summary>
/// What the pause menu's script asks for. It sends "slmod|newloopplus|" followed by a verb; anything else parses to
/// Unknown so it can be logged rather than acted on (the script's "diag|..." notes land there on purpose).
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|newloopplus|";

    private PageCommand()
    {
    }

    /// <summary>End Run was clicked: the player wants the confirm dialog.</summary>
    public sealed record Ask : PageCommand;

    /// <summary>The player confirmed ending the run.</summary>
    public sealed record End : PageCommand;

    /// <summary>The page script just installed itself.</summary>
    public sealed record Ready : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        return message.Substring(Prefix.Length) switch
        {
            "ask" => new Ask(),
            "end" => new End(),
            "ready" => new Ready(),
            "noscript" => new NoScript(),
            _ => new Unknown(message),
        };
    }
}
