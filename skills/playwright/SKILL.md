---
name: playwright
description: Use NScreenplay Playwright integration when creating or modifying browser automation with BrowseTheWeb, Targets, Interactions, Questions, and Playwright-based test components.
---

# NScreenplay — Playwright Integration Skill

## What This Skill Is For
Use this skill when working with `NScreenplay.Playwright`: creating Targets, using built-in Interactions and Questions, or extending the Playwright adapter.

---

## Package Role
`NScreenplay.Playwright` is the **only** package that knows about Playwright. It sits between Core abstractions and the Playwright `IPage`.

```
NScreenplay.Core (IAbility, Target, IInteraction, IQuestion)
        ↑
NScreenplay.Playwright (BrowseTheWeb, TargetResolver, Click, Enter, ...)
        ↑
Microsoft.Playwright (IPage, ILocator, IBrowser, ...)
```

**Rule**: Playwright types (`IPage`, `ILocator`, `IBrowser`) must NEVER appear in:
- Tasks
- Step Definitions
- Page classes (Target definitions)
- Business-level code

They belong only inside `NScreenplay.Playwright` implementation files.

---

## BrowseTheWeb Ability
Grants the Actor the ability to control a browser.

```csharp
var page = await browser.NewPageAsync();
actor.Can(BrowseTheWeb.Using(page));
```

Access from within an Interaction or Task:
```csharp
var page = actor.GetAbility<BrowseTheWeb>().Page;
```

`BrowseTheWeb` implements `IAsyncDisposable` and closes the page when the Actor is disposed.

---

## Target and TargetResolver

### Defining Targets (in Page classes)
```csharp
public static class LoginPage
{
    // PREFER: testid — most stable
    public static Target Username = Target.The("username field").ByTestId("username-input");
    
    // GOOD: role + accessible name
    public static Target LoginButton = Target.The("login button").ByRole("button", "Sign in");
    
    // GOOD: label text (for form fields)
    public static Target Password = Target.The("password field").ByLabel("Password");
    
    // ACCEPTABLE: CSS when no semantic option exists
    public static Target Spinner = Target.The("loading spinner").ByCss(".spinner");
    
    // LAST RESORT: XPath — fragile, hard to read
    public static Target LegacyField = Target.The("legacy field").ByXPath("//input[@name='legacy']");
}
```

### Multiple Strategies
Targets can carry multiple strategies; the **first** strategy is used by `TargetResolver`:
```csharp
// TargetResolver uses the first strategy (ByTestId here)
Target.The("submit button")
    .ByTestId("submit-btn")
    .ByRole("button", "Submit");
```

### TargetResolver
`TargetResolver.Resolve(page, target)` translates a `Target` to a Playwright `ILocator`.

**You rarely call this directly** — the built-in Interactions call it for you.
Only call it when writing a custom Interaction:
```csharp
public async Task PerformAs(Actor actor, CancellationToken ct = default)
{
    var page = actor.GetAbility<BrowseTheWeb>().Page;
    var locator = TargetResolver.Resolve(page, _target);
    await locator.HoverAsync();  // example: hover (not built in yet)
}
```

---

## Built-in Interactions

### Click
```csharp
await actor.AttemptsTo(Click.On(LoginPage.LoginButton));
```

### Enter (fill an input field)
```csharp
await actor.AttemptsTo(Enter.TheValue("alice@example.com").Into(LoginPage.Username));
```
**Rule**: Always call `.Into(target)` before executing. Forgetting it throws `InvalidOperationException`.

### Navigate
```csharp
await actor.AttemptsTo(Navigate.To("https://myapp.com/login"));
```

### Select (dropdown)
```csharp
await actor.AttemptsTo(Select.TheOption("Canada").From(CheckoutPage.CountryDropdown));
```

### Check / Uncheck
```csharp
await actor.AttemptsTo(Check.The(SignupPage.TermsCheckbox));
await actor.AttemptsTo(Check.Not(SettingsPage.EmailNotifications));
```

---

## Built-in Questions

### Text.Of — visible text content
```csharp
var heading = await actor.AsksFor(Text.Of(DashboardPage.Heading));
```

### Visibility.Of — is the element visible?
```csharp
bool isVisible = await actor.AsksFor(Visibility.Of(LoginPage.ErrorMessage));
```

### CurrentUrl.Value — the current page URL
```csharp
string url = await actor.AsksFor(CurrentUrl.Value());
```

### PageTitle.Value — the page `<title>`
```csharp
string title = await actor.AsksFor(PageTitle.Value());
```

### InputValue.Of — value of an `<input>` element
```csharp
string value = await actor.AsksFor(InputValue.Of(LoginPage.Username));
```

---

## Adding a Custom Interaction
When a built-in Interaction is insufficient:

```csharp
public sealed class Hover : IInteraction
{
    private readonly Target _target;
    
    private Hover(Target target) => _target = target;
    
    public static Hover Over(Target target) => new(target);
    
    public async Task PerformAs(Actor actor, CancellationToken ct = default)
    {
        var page = actor.GetAbility<BrowseTheWeb>().Page;
        var locator = TargetResolver.Resolve(page, _target);
        await locator.HoverAsync().ConfigureAwait(false);
    }
}
```

**Rules for custom Interactions:**
1. Implement `IInteraction` (not `ITask`) — it must be atomic.
2. Access the page only via `actor.GetAbility<BrowseTheWeb>().Page`.
3. Do not put multiple steps in one Interaction.
4. Use `ConfigureAwait(false)` on all `await` calls.

---

## Playwright's Auto-Waiting
Playwright automatically waits for elements to be actionable before clicking, filling, etc. **Do not fight this** with explicit waits.

**BAD:**
```csharp
await Task.Delay(2000); // never do this
await actor.AttemptsTo(Click.On(target));
```

**GOOD:**
```csharp
await actor.AttemptsTo(Click.On(target)); // Playwright waits automatically
```

If you need to wait for a specific condition, use a `IQuestion` + retry logic in the Consequence, or Playwright's built-in `WaitForAsync` — never `Thread.Sleep` or `Task.Delay`.

---

## Anti-Patterns

| Anti-Pattern | Why Wrong | Correct |
|---|---|---|
| `page.Locator(".btn").ClickAsync()` in a Task | Leaks Playwright into business layer | `actor.AttemptsTo(Click.On(target))` |
| `ILocator` in a Task or Step Definition | Breaks Core independence | Use Targets + Interactions |
| Raw selector strings in Tasks | Hard to maintain, duplicated | Define `Target` in Page class |
| `Thread.Sleep` / `Task.Delay` as waits | Flaky, slow | Playwright auto-waits |
| Calling `page.WaitForSelectorAsync(...)` in a Task | Implementation detail in wrong layer | Playwright handles this; if needed, wrap in a custom Interaction |
| Creating new `BrowseTheWeb` inside a Task | Bypasses lifecycle management | Always receive via `actor.GetAbility<BrowseTheWeb>()` |
