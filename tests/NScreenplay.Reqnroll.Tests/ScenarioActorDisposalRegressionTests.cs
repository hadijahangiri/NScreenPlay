using Microsoft.Playwright;
using NScreenplay.Core;
using NScreenplay.Playwright;
using NSubstitute;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>
/// Regression tests ensuring that ScenarioActor disposes the browser context even
/// when the actor's ability (BrowseTheWeb) throws during page close.
/// </summary>
public class ScenarioActorDisposalRegressionTests
{
    [Fact]
    public async Task DisposeAsync_DisposesContext_WhenPageCloseThrows()
    {
        var (browser, context, page) = PlaywrightFakes.CreateBrowser();

        // Simulate BrowseTheWeb closing the page throwing
        page.CloseAsync(Arg.Any<PageCloseOptions?>()).Returns(_ => throw new InvalidOperationException("page-close-fail"));
        context.DisposeAsync().Returns(new ValueTask());

        var scenario = new ScenarioActor();
        await scenario.InitializeAsync(browser, "Regression");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => scenario.DisposeAsync().AsTask());
        Assert.Contains("page-close-fail", ex.Message);

        // Ensure context.DisposeAsync was still called despite the page close failure
        await context.Received(1).DisposeAsync();
    }
}
