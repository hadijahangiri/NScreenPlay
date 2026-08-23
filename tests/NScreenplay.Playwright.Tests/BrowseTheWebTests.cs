using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;

namespace NScreenplay.Playwright.Tests;

/// <summary>
/// Unit tests for BrowseTheWeb ability and Playwright interactions using mocked IPage/ILocator.
/// These tests validate framework wiring without launching a real browser.
/// </summary>
public class BrowseTheWebTests
{
    [Fact]
    public void Using_ThrowsForNullPage()
    {
        Assert.Throws<ArgumentNullException>(() => BrowseTheWeb.Using(null!));
    }

    [Fact]
    public void Using_StoresPage()
    {
        var page = Substitute.For<IPage>();
        var ability = BrowseTheWeb.Using(page);
        Assert.Same(page, ability.Page);
    }

    [Fact]
    public async Task DisposeAsync_ClosesPage()
    {
        var page = Substitute.For<IPage>();
        page.CloseAsync().Returns(Task.CompletedTask);
        var ability = BrowseTheWeb.Using(page);
        await ability.DisposeAsync();
        await page.Received(1).CloseAsync();
    }

    [Fact]
    public void Actor_CanBeGrantedBrowseTheWeb()
    {
        var page = Substitute.For<IPage>();
        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        Assert.True(actor.HasAbility<BrowseTheWeb>());
    }

    [Fact]
    public void Actor_CanRetrieveBrowseTheWeb()
    {
        var page = Substitute.For<IPage>();
        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        var ability = actor.GetAbility<BrowseTheWeb>();
        Assert.Same(page, ability.Page);
    }
}
