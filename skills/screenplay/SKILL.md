# NScreenplay — Screenplay Pattern Skill

## What This Skill Is For
Use this skill when working with NScreenplay code: reading, writing, reviewing, or modifying any `.cs` file in a project that uses NScreenplay.

## Framework Overview
NScreenplay is a Screenplay-pattern test automation framework for .NET 10. It maps business intent to browser automation through a set of composable abstractions.

**Dependency direction:**
```
Reqnroll → NScreenplay.Reqnroll → NScreenplay.Core ← NScreenplay.Playwright
```
Core has zero dependencies on Playwright, Reqnroll, or any test runner.

---

## The Seven Core Concepts

### 1. Actor
An Actor represents a user or system that performs actions and checks results.

```csharp
var actor = Actor.Named("Alice");
```

- Every test scenario has exactly **one Actor per scenario** (created fresh, never shared).
- An Actor owns **Abilities** and executes **Tasks**, **Interactions**, **Questions**, and **Consequences**.
- Actor is `IAsyncDisposable` — always dispose it at the end of the scenario.

### 2. Ability
An Ability represents something an Actor can do (browse the web, call an API, etc.).

```csharp
actor.Can(BrowseTheWeb.Using(page));      // NScreenplay.Playwright
actor.Can(CallAnApi.Using(httpClient));   // NScreenplay.Api
```

- Abilities are granted to the Actor, not embedded in Tasks.
- Tasks access Abilities via `actor.GetAbility<BrowseTheWeb>()`.
- Core defines only `IAbility`. Concrete abilities live in integration packages.

### 3. Task
A Task represents a **business-level operation** composed of one or more Interactions.

```csharp
// GOOD — business vocabulary, reusable
public sealed class LoginWithCredentials : ITask
{
    public async Task PerformAs(Actor actor, CancellationToken ct = default)
    {
        await actor.AttemptsTo(Enter.TheValue(_username).Into(LoginPage.Username), ct);
        await actor.AttemptsTo(Enter.TheValue(_password).Into(LoginPage.Password), ct);
        await actor.AttemptsTo(Click.On(LoginPage.LoginButton), ct);
    }
}
```

**BAD — too low-level for a Task:**
```csharp
// Wrong: this is interaction-level detail, not a Task
public sealed class FillUsername : ITask { ... }
```

**Rule**: Name Tasks after user intent (`Login.WithCredentials`, `Checkout.WithCard`), not after UI actions (`FillField`, `ClickButton`).

### 4. Interaction
An Interaction is an **atomic, single-purpose UI/API action**.

```csharp
// Provided by NScreenplay.Playwright:
Click.On(target)
Enter.TheValue(value).Into(target)
Navigate.To(url)
Select.TheOption(option).From(target)
Check.The(target) / Check.Not(target)
```

**Rule**: Interactions do one thing. Never put business logic or multi-step flows in an Interaction.

### 5. Target
A Target is a **semantic description** of something to interact with — no Playwright types, no hardcoded selectors.

```csharp
// In a Page class (e.g., Pages/LoginPage.cs):
public static class LoginPage
{
    public static Target Username    = Target.The("username field").ByLabel("Username");
    public static Target LoginButton = Target.The("login button").ByRole("button", "Sign in");
    public static Target ErrorMessage = Target.The("login error").ByTestId("login-error");
}
```

**Strategy preference (most to least stable):**
1. `ByTestId(...)` — most stable, explicitly for automation
2. `ByRole(...)` — semantic, ARIA-correct
3. `ByLabel(...)` — good for form fields
4. `ByText(...)` — readable but fragile to copy changes
5. `ByCss(...)` / `ByXPath(...)` — last resort

**Rule**: Never put selectors (CSS, XPath, testid strings) inside Gherkin, Step Definitions, or Tasks. Selectors belong only in `Target` definitions.

### 6. Question
A Question reads state without mutating it.

```csharp
// Provided by NScreenplay.Playwright:
await actor.AsksFor(Text.Of(DashboardPage.Heading))
await actor.AsksFor(Visibility.Of(LoginPage.ErrorMessage))
await actor.AsksFor(CurrentUrl.Value())
await actor.AsksFor(PageTitle.Value())
await actor.AsksFor(InputValue.Of(LoginPage.Username))
```

**Rule**: Questions must not change page state. If you need to interact *and* read, split into an Interaction + Question.

### 7. Consequence
A Consequence verifies an expectation. It throws a meaningful exception if the expectation is not met.

```csharp
await actor.Should(DashboardIsDisplayed.Now());
```

Implement a Consequence:
```csharp
public sealed class DashboardIsDisplayed : IConsequence
{
    public static DashboardIsDisplayed Now() => _instance;
    
    public async Task EvaluateAs(Actor actor, CancellationToken ct = default)
    {
        var visible = await actor.AsksFor(Visibility.Of(DashboardPage.Heading), ct);
        if (!visible)
            throw new InvalidOperationException("Expected dashboard heading to be visible.");
    }
}
```

---

## Execution Model

```
actor.AttemptsTo(task)
    └── task.PerformAs(actor, ct)
            └── actor.AttemptsTo(interaction)
                    └── interaction.PerformAs(actor, ct)
                            └── actor.GetAbility<BrowseTheWeb>().Page
                                    └── Playwright locator actions
```

## Async Model
- All execution APIs are `async Task` with `CancellationToken`.
- Always `await` — never `.Result`, `.Wait()`, or `Thread.Sleep`.
- Always pass `cancellationToken` through the chain.

---

## Actor Lifecycle (Reqnroll)
```
[BeforeScenario]  → Create Actor, grant abilities
[Steps]           → Actor performs tasks, asks questions, should consequences
[AfterScenario]   → await actor.DisposeAsync()
```

Never share an Actor between scenarios. Never use static Actor state.

---

## Anti-Patterns (Never Do These)

| Anti-Pattern | Why It's Wrong | Correct Approach |
|---|---|---|
| `page.Locator("#email").FillAsync(...)` in a Task | Leaks Playwright into business layer | Use `Enter.TheValue(...).Into(target)` |
| `static Actor Current` | Breaks parallel test isolation | Use DI-injected `ScenarioActor` |
| `Thread.Sleep(2000)` | Fragile timing | Use Playwright's built-in waiting |
| `Task.Delay(...)` as a wait | Same problem | Let Playwright auto-wait |
| Selector strings inside Step Definitions | Mixes UI details with business steps | Define `Target` in a Page class |
| Selector strings inside Gherkin | Non-readable, brittle | Use business language in Gherkin |
| Business logic inside Interactions | Wrong abstraction level | Move to Task |
| Multi-step logic inside Interactions | Wrong abstraction level | Create a Task |
| Duplicate Target definitions | Maintenance risk | Single source of truth in Page class |
| Duplicate Task definitions | Maintenance risk | Reuse existing Tasks |

---

## Composition Example

```csharp
// Step Definition (thin — only business intent)
[When("the user logs in with valid credentials")]
public Task Login() => _scenario.Actor.AttemptsTo(LoginAs.ValidUser());

// Task (business level)
public sealed class LoginAs
{
    public static LoginWithCredentials ValidUser() =>
        LoginWithCredentials.Using("alice@example.com", "secret123");
}

// Task implementation (composes interactions)
public async Task PerformAs(Actor actor, CancellationToken ct = default)
{
    await actor.AttemptsTo(Enter.TheValue(_username).Into(LoginPage.Username), ct);
    await actor.AttemptsTo(Enter.TheValue(_password).Into(LoginPage.Password), ct);
    await actor.AttemptsTo(Click.On(LoginPage.LoginButton), ct);
}

// Target definition (Page class)
public static class LoginPage
{
    public static Target Username = Target.The("username field").ByLabel("Username");
    public static Target Password = Target.The("password field").ByLabel("Password");
    public static Target LoginButton = Target.The("login button").ByRole("button", "Sign in");
}
```
