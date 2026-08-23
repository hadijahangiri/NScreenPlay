using Reqnroll;

namespace NScreenplay.Reqnroll;

/// <summary>
/// Extension methods bridging the scenario <see cref="ScenarioActor"/> with the
/// feature-level browser managed by NScreenplay hooks.
/// </summary>
public static class ScenarioActorExtensions
{
    /// <summary>
    /// Initializes the scenario actor with a fresh browser context and page from the
    /// feature-level browser. Call from a <c>[BeforeScenario]</c> hook in the test project
    /// (after the NScreenplay registration hook) or as the first step of scenario setup.
    /// </summary>
    public static async Task InitializeFromFeatureBrowserAsync(
        this ScenarioActor scenarioActor, FeatureContext featureContext)
    {
        ArgumentNullException.ThrowIfNull(scenarioActor);
        ArgumentNullException.ThrowIfNull(featureContext);

        var manager = featureContext.Get<BrowserManager>();
        await scenarioActor.InitializeAsync(manager.Browser, featureContext.FeatureInfo.Title)
            .ConfigureAwait(false);
    }
}
