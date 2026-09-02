using Reqnroll;
using ReqnrollSmoke.Consequences;
using ReqnrollSmoke.Support;
using ReqnrollSmoke.Tasks;
using NScreenplay.Reqnroll;

namespace ReqnrollSmoke.StepDefinitions;

[Binding]
public sealed class SmokeSteps
{
    private readonly ScenarioActor _scenarioActor;

    public SmokeSteps(ScenarioActor scenarioActor)
    {
        _scenarioActor = scenarioActor;
    }

    [Given("the smoke page is open")]
    public async Task GivenTheSmokePageIsOpen()
    {
        if (!LifecycleProbe.BeforeScenarioInitialized)
            throw new InvalidOperationException("ScenarioActor was not initialized before steps executed.");

        await SmokeTestApp.OpenAsync(_scenarioActor.Actor);
    }

    [When("the user submits the smoke form")]
    public Task WhenTheUserSubmitsTheSmokeForm() =>
        _scenarioActor.Actor.AttemptsTo(SubmitSmokeForm.With("Deterministic"));

    [Then("the success message should be visible")]
    public Task ThenTheSuccessMessageShouldBeVisible() =>
        _scenarioActor.Actor.Should(SuccessMessageIsVisible.Now());
}
