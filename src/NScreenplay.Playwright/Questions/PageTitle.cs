using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Question: reads the current page title.
/// </summary>
/// <example>
/// <code>
/// var title = await actor.AsksFor(PageTitle.Value());
/// </code>
/// </example>
public sealed class PageTitle : IQuestion<string>
{
    private static readonly PageTitle Instance = new();

    private PageTitle() { }

    /// <summary>Returns the singleton question instance.</summary>
    public static PageTitle Value() => Instance;

    /// <inheritdoc/>
    public async Task<string> AnsweredBy(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        return await page.TitleAsync().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => "Page title";
}
