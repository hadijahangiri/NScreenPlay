---
name: reqnroll
description: Use NScreenplay Reqnroll integration when creating or modifying Gherkin Feature files, Step Definitions, scenario hooks, and BDD tests using Reqnroll.
---

# NScreenplay — Reqnroll Integration Skill

## What This Skill Is For
Use this skill when working with Reqnroll (BDD/Gherkin) integration in NScreenplay: writing Feature files, Step Definitions, or lifecycle hooks.

---

## Package Role
`NScreenplay.Reqnroll` bridges Reqnroll's lifecycle with NScreenplay's Actor model.

```
Gherkin Feature → Reqnroll → NScreenplay.Reqnroll → NScreenplay.Core
                                      ↓
                             NScreenplay.Playwright
```

---

## Feature File Principles

### Business Language Only
Gherkin must describe **what** the user does, not **how** the system does it.

**GOOD:**
```gherkin
Feature: Login

  Scenario: Successful login
    Given the user is on the login page
    When the user logs in with valid credentials
    Then the dashboard should be displayed
```

**BAD — implementation details in Gherkin:**
```gherkin
  Scenario: Successful login
    Given the user navigates to "/login"
    When the user fills "#username" with "alice@example.com"
    And the user fills "#password" with "secret123"
    And the user clicks ".login-btn"
    Then the element "[data-testid='dashboard']" should be visible
```

**Why it's wrong**: Gherkin becomes a maintenance nightmare when selectors change. Business intent doesn't change when the UI is refactored.

### One Business Action Per Step
Each step should represent one cohesive business action.

```gherkin
# GOOD: one action
When the user logs in with valid credentials

# BAD: multiple actions crammed into one step (or too granular)
When the user enters username and password and clicks login
```

---

## Step Definitions Must Be Thin

**THE most important rule**: Step Definitions translate Gherkin to Screenplay. Nothing else.

**GOOD — thin step definition:**
```csharp
[Binding]
public sealed class LoginSteps
{
    private readonly ScenarioActor _scenario;
    
    public LoginSteps(ScenarioActor scenario) => _scenario = scenario;
    
    [Given("the user is on the login page")]
    public Task NavigateToLoginPage() =>
        _scenario.Actor.AttemptsTo(Navigate.To("/login"));
    
    [When("the user logs in with valid credentials")]
    public Task Login() =>
        _scenario.Actor.AttemptsTo(LoginAs.ValidUser());
    
    [Then("the dashboard should be displayed")]
    public Task VerifyDashboard() =>
        _scenario.Actor.Should(DashboardIsDisplayed.Now());
}
```

**BAD — fat step definition:**
```csharp
[When("the user logs in with valid credentials")]
public async Task Login()
{
    // WRONG: raw Playwright in a step definition
    await _page.GetByLabel("Username").FillAsync("alice@example.com");
    await _page.GetByLabel("Password").FillAsync("secret123");
    await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
    await _page.WaitForURLAsync("**/dashboard");
}
```

**Why it's wrong**: The step definition now knows about the UI structure. When the UI changes, you have to update step definitions instead of just the Task/Target layer.

---

## Actor Access in Step Definitions
Actor is accessed via constructor-injected `ScenarioActor`:

```csharp
public sealed class MySteps
{
    private readonly ScenarioActor _scenario;
    
    public MySteps(ScenarioActor scenario) => _scenario = scenario;
    
    // Use: _scenario.Actor.AttemptsTo(...)
    //      _scenario.Actor.AsksFor(...)
    //      _scenario.Actor.Should(...)
    //      _scenario.Page   (if you absolutely need the raw page — avoid)
}
```

**Never use:**
```csharp
static Actor _actor;          // breaks parallel test isolation
Actor.Current                  // does not exist in NScreenplay
ServiceLocator.GetActor()      // anti-pattern
```

---

## Actor Lifecycle
```
[BeforeFeature]     → BrowserManager launches IBrowser
[BeforeScenario/0]  → NScreenplayHooks registers new ScenarioActor
[BeforeScenario/10] → SampleHooks (or your hooks) calls InitializeAsync
                      → IBrowserContext + IPage + Actor created
[Steps]             → Actor performs tasks, asks questions
[AfterScenario]     → NScreenplayHooks disposes ScenarioActor
                      → Actor.DisposeAsync → BrowseTheWeb → Page.CloseAsync
                      → IBrowserContext.DisposeAsync
[AfterFeature]      → BrowserManager closes IBrowser
```

### Scenario Isolation
Each scenario gets its own:
- `IBrowserContext` (isolated cookies, storage, session)
- `IPage`
- `Actor`

**Scenarios cannot share or contaminate each other's state**, even when run in parallel.

---

## Adding NScreenplay to a New Project

1. Reference `NScreenplay.Reqnroll` and `Reqnroll.xUnit` (or your test runner adapter).
2. Add `reqnroll.json` to register the `NScreenplay.Reqnroll` assembly:

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "stepAssemblies": [
    { "assembly": "NScreenplay.Reqnroll" }
  ]
}
```

3. Add your own `[BeforeScenario(Order = 10)]` hook to initialize the actor:

```csharp
[Binding]
public sealed class YourHooks
{
    private readonly ScenarioActor _scenarioActor;
    private readonly FeatureContext _featureContext;
    
    public YourHooks(ScenarioActor scenarioActor, FeatureContext featureContext)
    {
        _scenarioActor = scenarioActor;
        _featureContext = featureContext;
    }
    
    [BeforeScenario(Order = 10)]
    public Task InitializeAsync() =>
        _scenarioActor.InitializeFromFeatureBrowserAsync(_featureContext);
}
```

4. Inject `ScenarioActor` into your step definition classes.

---

## Anti-Patterns

| Anti-Pattern | Why Wrong | Correct |
|---|---|---|
| Raw `IPage` in step definitions | Bypasses Screenplay abstraction | Use `_scenario.Actor.AttemptsTo(...)` |
| `Thread.Sleep` in step definitions | Fragile timing dependency | Playwright auto-waits |
| `static ScenarioContext` | Breaks parallel isolation | Constructor injection |
| Business logic in step definitions | Hard to reuse, test, or understand | Move to a Task |
| Assertions using `Assert.X()` in step definitions | Bypasses Consequence model | Use `actor.Should(consequence)` |
| Gherkin steps that mirror CSS selectors | Makes Gherkin a UI script | Use business vocabulary |
| Multiple responsibilities per step | Ambiguous, hard to maintain | One business action per step |
