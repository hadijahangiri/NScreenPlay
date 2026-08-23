using Login.Consequences;
using Login.Support;
using Login.Tasks;
using NScreenplay.Reqnroll;
using Reqnroll;

namespace Login.StepDefinitions;

/// <summary>
/// Step definitions for the Login feature.
///
/// These remain intentionally thin — they translate Gherkin into business-level
/// Screenplay operations. No Playwright locators or selectors appear here.
/// </summary>
[Binding]
public sealed class LoginSteps
{
    private readonly ScenarioActor _scenario;

    public LoginSteps(ScenarioActor scenario) => _scenario = scenario;

    [Given("the user is on the login page")]
    public Task NavigateToLoginPage() =>
        TestApp.NavigateToLoginPage(_scenario.Actor);

    [When("the user logs in with valid credentials")]
    public Task LoginWithValidCredentials() =>
        _scenario.Actor.AttemptsTo(LoginAs.ValidUser());

    [When("the user logs in with invalid credentials")]
    public Task LoginWithInvalidCredentials() =>
        _scenario.Actor.AttemptsTo(LoginAs.InvalidUser());

    [Then("the dashboard should be displayed")]
    public Task DashboardShouldBeDisplayed() =>
        _scenario.Actor.Should(DashboardIsDisplayed.Now());

    [Then("the login error should be displayed")]
    public Task LoginErrorShouldBeDisplayed() =>
        _scenario.Actor.Should(LoginErrorIsDisplayed.Now());
}
