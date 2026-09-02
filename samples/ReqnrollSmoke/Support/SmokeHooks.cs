using NScreenplay.Reqnroll;
using Reqnroll;

namespace ReqnrollSmoke.Support;

[Binding]
public sealed class SmokeHooks
{
    private readonly ScenarioActor _scenarioActor;
    private readonly FeatureContext _featureContext;

    public SmokeHooks(ScenarioActor scenarioActor, FeatureContext featureContext)
    {
        _scenarioActor = scenarioActor;
        _featureContext = featureContext;
    }

    [BeforeScenario(Order = 10)]
    public async Task InitializeActorAsync()
    {
        LifecycleProbe.Reset();
        await _scenarioActor.InitializeFromFeatureBrowserAsync(_featureContext);
        _ = _scenarioActor.Actor;
        _ = _scenarioActor.Page;
        LifecycleProbe.BeforeScenarioInitialized = true;
    }

    [AfterScenario(Order = 100)]
    public void MarkAfterScenario()
    {
        LifecycleProbe.AfterScenarioDisposalObserved = true;
    }
}
