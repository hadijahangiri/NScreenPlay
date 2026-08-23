using Microsoft.Playwright;
using NScreenplay.Core;
using NScreenplay.Playwright;

namespace NScreenplay.Reqnroll;

/// <summary>
/// Scenario-scoped holder for the Actor and its Playwright resources.
/// </summary>
/// <remarks>
/// <para>
/// Inject this class into step definition classes via Reqnroll's constructor injection.
/// Access the current actor through <see cref="Actor"/>.
/// </para>
/// <para>
/// <b>Ownership</b>: this class owns the <see cref="IPage"/> (via BrowseTheWeb) and the
/// <see cref="IBrowserContext"/>. Disposing it closes both, in that order.
/// The feature-level <c>IBrowser</c> is NOT owned here — it is managed by
/// <see cref="BrowserManager"/> at feature scope.
/// </para>
/// </remarks>
public sealed class ScenarioActor : IAsyncDisposable
{
    private Actor? _actor;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _disposed;

    /// <summary>The browser context for this scenario.</summary>
    public IBrowserContext Context => _context ?? throw new InvalidOperationException(
        "Scenario not initialized. Call InitializeAsync first (normally done by NScreenplay hooks).");

    /// <summary>The page for this scenario.</summary>
    public IPage Page => _page ?? throw new InvalidOperationException(
        "Scenario not initialized. Call InitializeAsync first (normally done by NScreenplay hooks).");

    /// <summary>The actor for this scenario.</summary>
    public Actor Actor => _actor ?? throw new InvalidOperationException(
        "Scenario not initialized. Call InitializeAsync first (normally done by NScreenplay hooks).");

    /// <summary>
    /// Creates the browser context, page, and actor. Called once per scenario.
    /// </summary>
    internal async Task InitializeAsync(IBrowser browser, string scenarioTitle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_actor is not null)
            throw new InvalidOperationException("Scenario already initialized.");

        _context = await browser.NewContextAsync().ConfigureAwait(false);
        _page = await _context.NewPageAsync().ConfigureAwait(false);

        _actor = Actor.Named(scenarioTitle);
        _actor.Can(BrowseTheWeb.Using(_page));
    }

    /// <summary>
    /// Disposes the actor (closing the page), then the browser context.
    /// Idempotent; safe to call even when initialization partially failed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_actor is not null)
            await _actor.DisposeAsync().ConfigureAwait(false); // closes the page via BrowseTheWeb

        if (_context is not null)
            await _context.DisposeAsync().ConfigureAwait(false);
    }
}
