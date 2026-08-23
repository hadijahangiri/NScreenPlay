using Microsoft.Playwright;
using NScreenplay.Core;
using NScreenplay.Playwright;
using NSubstitute;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>
/// Tests for ScenarioActor lifecycle: initialization, isolation, and disposal.
/// Uses mocked Playwright objects — no real browser required.
/// </summary>
public class ScenarioActorTests
{
    [Fact]
    public async Task InitializeAsync_CreatesContextPageAndActor()
    {
        var (browser, context, page) = PlaywrightFakes.CreateBrowser();
        await using var scenario = new ScenarioActor();

        await scenario.InitializeAsync(browser, "Test Scenario");

        Assert.Same(context, scenario.Context);
        Assert.Same(page, scenario.Page);
        Assert.Equal("Test Scenario", scenario.Actor.Name);
    }

    [Fact]
    public async Task InitializeAsync_GrantsBrowseTheWebAbility()
    {
        var (browser, _, _) = PlaywrightFakes.CreateBrowser();
        await using var scenario = new ScenarioActor();
        await scenario.InitializeAsync(browser, "Alice");

        Assert.True(scenario.Actor.HasAbility<BrowseTheWeb>());
        Assert.Same(scenario.Page, scenario.Actor.GetAbility<BrowseTheWeb>().Page);
    }

    [Fact]
    public async Task Actor_IsScenarioScoped_TwoScenariosGetDifferentActors()
    {
        var browser1 = PlaywrightFakes.CreateBrowser();
        var browser2 = PlaywrightFakes.CreateBrowser();

        await using var scenarioA = new ScenarioActor();
        await using var scenarioB = new ScenarioActor();
        await scenarioA.InitializeAsync(browser1.browser, "Scenario A");
        await scenarioB.InitializeAsync(browser2.browser, "Scenario B");

        Assert.NotSame(scenarioA.Actor, scenarioB.Actor);
        Assert.NotSame(scenarioA.Page, scenarioB.Page);
        Assert.NotSame(scenarioA.Context, scenarioB.Context);
    }

    [Fact]
    public async Task InitializeAsync_ThrowsIfAlreadyInitialized()
    {
        var (browser, _, _) = PlaywrightFakes.CreateBrowser();
        await using var scenario = new ScenarioActor();
        await scenario.InitializeAsync(browser, "First");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.InitializeAsync(browser, "Second"));
    }

    [Fact]
    public void Accessors_ThrowBeforeInitialization()
    {
        var scenario = new ScenarioActor();
        Assert.Throws<InvalidOperationException>(() => scenario.Actor);
        Assert.Throws<InvalidOperationException>(() => scenario.Context);
        Assert.Throws<InvalidOperationException>(() => scenario.Page);
    }

    [Fact]
    public async Task DisposeAsync_DisposesContextAndClosesPage()
    {
        var (browser, context, page) = PlaywrightFakes.CreateBrowser();
        var scenario = new ScenarioActor();
        await scenario.InitializeAsync(browser, "Alice");

        context.DisposeAsync().Returns(new ValueTask());
        page.CloseAsync(Arg.Any<PageCloseOptions?>()).Returns(Task.CompletedTask);

        await scenario.DisposeAsync();

        // BrowseTheWeb closes the page; ScenarioActor disposes the context
        await context.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        var (browser, context, _) = PlaywrightFakes.CreateBrowser();
        var scenario = new ScenarioActor();
        await scenario.InitializeAsync(browser, "Alice");
        context.DisposeAsync().Returns(new ValueTask());

        await scenario.DisposeAsync();
        await scenario.DisposeAsync(); // second call must not throw

        await context.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_BeforeInitialization_DoesNotThrow()
    {
        // covers scenarios that fail during setup before InitializeAsync ran
        await using var scenario = new ScenarioActor();
    }
}
