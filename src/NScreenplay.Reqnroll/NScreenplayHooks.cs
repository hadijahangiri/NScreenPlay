using Microsoft.Playwright;
using Reqnroll;
using Reqnroll.BoDi;

namespace NScreenplay.Reqnroll;

/// <summary>
/// Reqnroll lifecycle hooks for NScreenplay.
/// </summary>
/// <remarks>
/// <para>
/// <b>Feature scope</b>: one <see cref="BrowserManager"/> (and thus one <c>IBrowser</c>) per feature,
/// stored in <c>FeatureContext</c>.
/// </para>
/// <para>
/// <b>Scenario scope</b>: one <see cref="ScenarioActor"/> (context + page + actor) per scenario,
/// registered into the BoDi scenario container so step definitions receive it via constructor injection.
/// </para>
/// <para>
/// Hooks manage infrastructure only â€” no business logic lives here.
/// </para>
/// </remarks>
[Binding]
public sealed class NScreenplayHooks
{
    private readonly IObjectContainer _scenarioContainer;
    private readonly FeatureContext _featureContext;

    /// <summary>Creates the hooks with Reqnroll's scenario container and feature context.</summary>
    public NScreenplayHooks(IObjectContainer scenarioContainer, FeatureContext featureContext)
    {
        _scenarioContainer = scenarioContainer;
        _featureContext = featureContext;
    }

    /// <summary>Registers a fresh <see cref="ScenarioActor"/> before each scenario.</summary>
    [BeforeScenario(Order = 0)]
    public void RegisterScenarioActor()
    {
        _scenarioContainer.RegisterInstanceAs(new ScenarioActor());
    }

    /// <summary>
    /// Disposes the scenario's actor, page, and browser context after each scenario.
    /// Runs even when the scenario failed, so resources never leak.
    /// </summary>
    [AfterScenario]
    public async Task DisposeScenarioActorAsync()
    {
        var scenarioActor = _scenarioContainer.Resolve<ScenarioActor>();
        await scenarioActor.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Launches the feature-level browser before the first scenario of the feature.
    /// </summary>
    [BeforeFeature]
    public static async Task LaunchBrowserAsync(FeatureContext featureContext)
    {
        var manager = new BrowserManager();
        await manager.InitializeAsync().ConfigureAwait(false);
        featureContext.Set(manager);
    }

    /// <summary>
    /// Closes the feature-level browser after the last scenario of the feature.
    /// </summary>
    [AfterFeature]
    public static async Task CloseBrowserAsync(FeatureContext featureContext)
    {
        if (featureContext.ContainsKey(nameof(BrowserManager)))
        {
            var manager = featureContext.Get<BrowserManager>();
            await manager.DisposeAsync().ConfigureAwait(false);
        }
    }
}
