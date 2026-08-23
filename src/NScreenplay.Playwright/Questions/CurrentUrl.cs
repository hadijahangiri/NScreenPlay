using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Question: reads the current page URL.
/// </summary>
/// <example>
/// <code>
/// var url = await actor.AsksFor(CurrentUrl.Value());
/// </code>
/// </example>
public sealed class CurrentUrl : IQuestion<string>
{
    private static readonly CurrentUrl Instance = new();

    private CurrentUrl() { }

    /// <summary>Returns the singleton question instance.</summary>
    public static CurrentUrl Value() => Instance;

    /// <inheritdoc/>
    public Task<string> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        return Task.FromResult(page.Url);
    }

    /// <inheritdoc/>
    public override string ToString() => "Current URL";
}
