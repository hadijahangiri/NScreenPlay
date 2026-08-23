using Microsoft.Playwright;
using NScreenplay.Reqnroll;
using NSubstitute;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>Shared helpers for mocking the Playwright object graph.</summary>
internal static class PlaywrightFakes
{
    /// <summary>Creates a mocked IBrowser whose NewContextAsync returns a mocked context/page.</summary>
    public static (IBrowser browser, IBrowserContext context, IPage page) CreateBrowser()
    {
        var page = Substitute.For<IPage>();
        var context = Substitute.For<IBrowserContext>();
        context.NewPageAsync().Returns(Task.FromResult(page));

        var browser = Substitute.For<IBrowser>();
        browser.NewContextAsync(Arg.Any<BrowserNewContextOptions?>())
               .Returns(Task.FromResult(context));

        return (browser, context, page);
    }
}
