# NScreenplay — Test Review Skill

## What This Skill Is For
Use this skill when **reviewing existing NScreenplay tests**. Apply the checklist below. Report each finding with file, line, severity, and recommended fix.

---

## Review Checklist

### Category 1: Step Definitions

**R-01: Raw Playwright API in Step Definitions** `[CRITICAL]`

Detect: `IPage`, `ILocator`, `.ClickAsync(`, `.FillAsync(`, `.GetByRole(`, `.GetByLabel(`, `.Locator(` inside any `[Binding]` class.

```csharp
// BAD — CRITICAL violation
[When("the user logs in")]
public async Task Login()
{
    await _page.GetByLabel("Username").FillAsync("alice@example.com"); // VIOLATION
    await _page.GetByRole(AriaRole.Button).ClickAsync();               // VIOLATION
}

// GOOD
[When("the user logs in")]
public Task Login() => _scenario.Actor.AttemptsTo(LoginAs.ValidUser());
```

**R-02: Assertions in Step Definitions** `[HIGH]`

Detect: `Assert.`, `Should()` from xUnit/NUnit/FluentAssertions, `throw new` directly inside a step method.

```csharp
// BAD
[Then("the dashboard is shown")]
public async Task DashboardShown()
{
    var visible = await _scenario.Actor.AsksFor(Visibility.Of(DashboardPage.Heading));
    Assert.True(visible); // VIOLATION — use a Consequence
}

// GOOD
[Then("the dashboard is shown")]
public Task DashboardShown() =>
    _scenario.Actor.Should(DashboardIsDisplayed.Now());
```

**R-03: Business Logic in Step Definitions** `[HIGH]`

Detect: `if`, `for`, `while`, `switch`, multi-statement step methods (more than 2 lines).

```csharp
// BAD
[When("the user applies discount")]
public async Task ApplyDiscount()
{
    var code = _config.DiscountCode ?? "DEFAULT"; // VIOLATION — logic here
    if (code.Length > 10) code = code.Substring(0, 10);
    await _actor.AttemptsTo(Enter.TheValue(code).Into(CheckoutPage.PromoField));
    await _actor.AttemptsTo(Click.On(CheckoutPage.ApplyButton));
}

// GOOD — move logic to Task, keep step thin
[When("the user applies the default discount")]
public Task ApplyDiscount() =>
    _actor.AttemptsTo(ApplyPromoCode.WithDefaultCode());
```

**R-04: CSS/XPath Selectors in Step Definitions or Gherkin** `[CRITICAL]`

Detect: strings containing `#`, `.`, `[data-`, `/html/`, `//`, `xpath=` inside step definitions or `.feature` files.

---

### Category 2: Tasks

**R-05: Low-Level UI Steps Directly in a Task** `[HIGH]`

Tasks must use NScreenplay Interactions, not raw Playwright:

```csharp
// BAD
public async Task PerformAs(Actor actor, CancellationToken ct = default)
{
    var page = actor.GetAbility<BrowseTheWeb>().Page;
    await page.Locator("#username").FillAsync("alice"); // VIOLATION
}

// GOOD
public async Task PerformAs(Actor actor, CancellationToken ct = default)
{
    await actor.AttemptsTo(Enter.TheValue("alice").Into(LoginPage.Username), ct);
}
```

**R-06: Task Named After UI Action, Not Business Intent** `[MEDIUM]`

```csharp
// BAD names (UI-centric)
class FillUsernameField : ITask { }
class ClickLoginButton : ITask { }

// GOOD names (business-centric)
class LoginWithCredentials : ITask { }
class PlaceOrder : ITask { }
```

---

### Category 3: Targets

**R-07: Hardcoded Selectors Outside Page Classes** `[HIGH]`

```csharp
// BAD — selector inside a Task
var t = Target.The("btn").ByCss(".login-button"); // defined inline

// GOOD — defined in LoginPage.cs
await actor.AttemptsTo(Click.On(LoginPage.LoginButton));
```

**R-08: Duplicate Target Definitions** `[MEDIUM]`

Search all `Target.The(...)` calls. If the same UI element is described in multiple places, merge into one canonical location in the appropriate Page class.

**R-09: Unstable Selector Strategy** `[MEDIUM]`

Prefer (1→5): `ByTestId` > `ByRole` > `ByLabel` > `ByText` > `ByCss` > `ByXPath`.

Flag any `ByCss` or `ByXPath` where a more stable strategy is available.

---

### Category 4: Timing and Waits

**R-10: Thread.Sleep in Any Test Code** `[CRITICAL]`

```csharp
Thread.Sleep(2000); // VIOLATION — must be removed
```

**R-11: Task.Delay in Any Test Code** `[CRITICAL]`

```csharp
await Task.Delay(1000); // VIOLATION — use Playwright auto-wait
```

**R-12: Explicit Playwright Wait Methods Used Unnecessarily** `[MEDIUM]`

```csharp
await page.WaitForTimeoutAsync(2000); // usually a VIOLATION
await page.WaitForLoadStateAsync();   // sometimes acceptable — review context
```

---

### Category 5: State and Isolation

**R-13: Static Actor State** `[CRITICAL]`

```csharp
static Actor _sharedActor;          // VIOLATION
static IPage _page;                  // VIOLATION
[ThreadStatic] Actor _actor;        // VIOLATION
```

**R-14: Actor or Page Shared Across Scenarios** `[CRITICAL]`

Each scenario must receive its own `Actor` and `IPage` via `ScenarioActor`. Flag any `FeatureContext.Set<Actor>(...)` or shared state passed between scenarios.

---

### Category 6: Interactions and Questions

**R-15: State Mutation Inside a Question** `[HIGH]`

A `IQuestion<T>` must be read-only. It must not call `FillAsync`, `ClickAsync`, `GotoAsync`, or any other mutating Playwright method.

**R-16: Multi-Step Logic Inside a Single Interaction** `[MEDIUM]`

An Interaction must do one thing. If it calls `actor.AttemptsTo(...)` internally, it should probably be a Task.

---

### Category 7: Consequences

**R-17: Empty or Always-Passing Consequence** `[HIGH]`

```csharp
public Task EvaluateAs(Actor actor, CancellationToken ct = default)
{
    return Task.CompletedTask; // VIOLATION — never verifies anything
}
```

**R-18: Consequence Uses Assert.X() from Test Framework** `[MEDIUM]`

Consequences should throw `InvalidOperationException` with a business-readable message. Using `Assert.True(...)` tightly couples the consequence to a specific test framework.

---

## Review Report Format
For each finding, report:

```
[SEVERITY] File: path/to/File.cs Line: N
Rule: R-XX
Finding: <what was found>
Recommended fix: <concrete action>
```

Example:
```
[CRITICAL] File: StepDefinitions/LoginSteps.cs Line: 23
Rule: R-01
Finding: Raw Playwright `_page.GetByLabel("Username").FillAsync(...)` in step definition.
Recommended fix: Replace with `await _scenario.Actor.AttemptsTo(Enter.TheValue(username).Into(LoginPage.Username))`.
```

---

## Severity Definitions
| Severity | Meaning |
|---|---|
| CRITICAL | Violates a fundamental architectural principle; must be fixed before merging |
| HIGH | Significant quality issue; should be fixed soon |
| MEDIUM | Improvement recommended; fix in next iteration |
| LOW | Minor style or naming concern |
