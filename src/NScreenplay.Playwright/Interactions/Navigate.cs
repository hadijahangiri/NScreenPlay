using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Interaction: navigates the browser to a URL.
/// </summary>
/// <example>
/// <code>
/// await actor.AttemptsTo(Navigate.To("https://example.com/login"));
/// </code>
/// </example>
public sealed class Navigate : IInteraction
{
    private readonly string _url;

    private Navigate(string url) => _url = url;

    /// <summary>Creates a navigation interaction to the given URL.</summary>
    public static Navigate To(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new Navigate(url);
    }

    /// <inheritdoc/>
    public async Task PerformAs(Actor actor, CancellationToken cancellationToken = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        await page.GotoAsync(_url).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override string ToString() => $"Navigate to {_url}";
}
