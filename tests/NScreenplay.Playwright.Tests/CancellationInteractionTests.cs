using Microsoft.Playwright;
using NScreenplay.Core;
using NSubstitute;

namespace NScreenplay.Playwright.Tests;

public class CancellationInteractionTests
{
    [Fact]
    public async Task Click_RespectsPreCancelledToken()
    {
        var (page, locator, actor) = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => actor.AttemptsTo(Click.On(Target.The("btn").ByCss(".btn")), cts.Token));
        await locator.DidNotReceiveWithAnyArgs().ClickAsync();
    }

    [Fact]
    public async Task Enter_RespectsPreCancelledToken()
    {
        var (page, locator, actor) = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var interaction = Enter.TheValue("x").Into(Target.The("f").ByLabel("F"));
        await Assert.ThrowsAsync<OperationCanceledException>(() => actor.AttemptsTo(interaction, cts.Token));
        await locator.DidNotReceiveWithAnyArgs().FillAsync(Arg.Any<string>(), Arg.Any<LocatorFillOptions?>());
    }

    private static (IPage page, ILocator locator, Actor actor) Setup()
    {
        var locator = Substitute.For<ILocator>();
        var page = Substitute.For<IPage>();
        // ensure locator calls return the fake locator
        page.Locator(Arg.Any<string>()).Returns(locator);
        page.GetByLabel(Arg.Any<string>(), Arg.Any<PageGetByLabelOptions?>()).Returns(locator);

        var actor = Actor.Named("Alice");
        actor.Can(BrowseTheWeb.Using(page));
        return (page, locator, actor);
    }
}
