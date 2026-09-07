using Microsoft.Playwright;
using NSubstitute;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>
/// Regression test ensuring that BrowserManager disposes Playwright even when
/// IBrowser.DisposeAsync throws.
/// </summary>
public class BrowserManagerDisposalRegressionTests
{
    [Fact]
    public async Task DisposeAsync_DisposesPlaywright_WhenBrowserDisposeThrows()
    {
        var playwright = Substitute.For<IPlaywright>();
        var browser = Substitute.For<IBrowser>();

        // Simulate browser.DisposeAsync throwing
        browser.DisposeAsync().Returns(_ => throw new InvalidOperationException("browser-dispose-fail"));
        playwright.Chromium.Returns(Substitute.For<IBrowserType>());

        var manager = new BrowserManager();

        // Inject internal fields via reflection to avoid launching a real browser
        var playwrightField = typeof(BrowserManager).GetField("_playwright", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var browserField = typeof(BrowserManager).GetField("_browser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        playwrightField.SetValue(manager, playwright);
        browserField.SetValue(manager, browser);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.DisposeAsync().AsTask());
        Assert.Contains("browser-dispose-fail", ex.Message);

        // Ensure Playwright.Dispose was still attempted despite the browser dispose failure
        playwright.Received(1).Dispose();
    }
}
