using Microsoft.Playwright;
using NScreenplay.Core;

namespace NScreenplay.Playwright;

/// <summary>
/// Grants an <see cref="Actor"/> the ability to control a browser via Playwright.
/// </summary>
/// <remarks>
/// <para>
/// Obtain a Playwright <see cref="IPage"/> from your test setup and pass it here.
/// The actor will use this page for all browser interactions.
/// </para>
/// <para>
/// <b>Lifecycle</b>: BrowseTheWeb implements <see cref="IAsyncDisposable"/>.
/// When the actor is disposed at the end of a scenario, this ability closes the
/// underlying page automatically. The browser and browser context remain open
/// (they are typically managed at a higher scope).
/// </para>
/// <example>
/// <code>
/// var page = await browser.NewPageAsync();
/// var actor = Actor.Named("Alice");
/// actor.Can(BrowseTheWeb.Using(page));
/// </code>
/// </example>
/// </remarks>
public sealed class BrowseTheWeb : NScreenplay.Core.IAbility, IAsyncDisposable
{
    /// <summary>The Playwright page this ability drives.</summary>
    public IPage Page { get; }

    private BrowseTheWeb(IPage page) => Page = page;

    /// <summary>Creates a <see cref="BrowseTheWeb"/> ability backed by the given page.</summary>
    public static BrowseTheWeb Using(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        return new BrowseTheWeb(page);
    }

    /// <summary>Closes the page when the actor is disposed.</summary>
    public ValueTask DisposeAsync() => new(Page.CloseAsync());
}
