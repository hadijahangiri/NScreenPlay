using Microsoft.Playwright;

namespace NScreenplay.Reqnroll;

/// <summary>
/// Feature-scoped manager for the shared Playwright <see cref="IBrowser"/>.
/// </summary>
/// <remarks>
/// One browser instance is launched per feature and shared by its scenarios.
/// Each scenario gets its own <c>IBrowserContext</c>, which provides full isolation
/// (cookies, storage, cache) while avoiding the cost of a browser launch per scenario.
/// </remarks>
public sealed class BrowserManager : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed;

    /// <summary>The feature-level browser. Throws if <see cref="InitializeAsync"/> has not run.</summary>
    public IBrowser Browser => _browser ?? throw new InvalidOperationException(
        "Browser not initialized. Call InitializeAsync first (normally done by NScreenplay hooks).");

    /// <summary>Launches Playwright and the configured browser. Idempotent per instance.</summary>
    public async Task InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_browser is not null) return;

        var options = NScreenplayConfiguration.Options;
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);

        // Use the standard Chromium (not chromium-headless-shell) for compatibility
        // when only the full browser package is installed.
        var chromiumExecutable = FindChromiumExecutable();
        var launchOptions = new BrowserTypeLaunchOptions { Headless = options.Headless };
        if (chromiumExecutable is not null)
            launchOptions.ExecutablePath = chromiumExecutable;

        _browser = await _playwright.Chromium.LaunchAsync(launchOptions).ConfigureAwait(false);
    }

    /// <summary>Closes the browser and disposes Playwright. Idempotent.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser is not null)
            await _browser.DisposeAsync().ConfigureAwait(false);

        _playwright?.Dispose();
    }

    // Looks for the full Chromium executable when chromium-headless-shell is absent.
    private static string? FindChromiumExecutable()
    {
        var playwrightHome = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright");

        if (!Directory.Exists(playwrightHome)) return null;

        // Prefer full chromium over headless-shell
        foreach (var dir in Directory.GetDirectories(playwrightHome, "chromium-*"))
        {
            var exe = Path.Combine(dir, "chrome-win", "chrome.exe");
            if (File.Exists(exe)) return exe;
        }
        return null;
    }
}
