using NScreenplay.Reqnroll;
using Reqnroll;

namespace Login.Support;

/// <summary>
/// Sample-specific Reqnroll hooks.
/// Relies on NScreenplayHooks (from NScreenplay.Reqnroll) for the browser and
/// ScenarioActor lifecycle, then initializes the actor within each scenario.
/// </summary>
[Binding]
public sealed class SampleHooks
{
    private readonly ScenarioActor _scenarioActor;
    private readonly FeatureContext _featureContext;
    private readonly ScenarioContext _scenarioContext;

    public SampleHooks(
        ScenarioActor scenarioActor,
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
    {
        _scenarioActor = scenarioActor;
        _featureContext = featureContext;
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Runs after NScreenplayHooks registers ScenarioActor.
    /// Initializes the actor with a fresh browser context, page, and BrowseTheWeb ability.
    /// </summary>
    [BeforeScenario(Order = 10)]
    public Task InitializeActorAsync() =>
        _scenarioActor.InitializeFromFeatureBrowserAsync(_featureContext);
}
