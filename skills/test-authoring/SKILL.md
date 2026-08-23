# NScreenplay — Test Authoring Skill

## What This Skill Is For
Use this skill when **creating new tests** in NScreenplay. Follow this workflow precisely. Do not skip steps.

---

## Authoring Workflow

```
1. Understand the requirement
2. Identify the Actor
3. Identify Abilities needed
4. Define or reuse Targets
5. Define or reuse Tasks
6. Write the Gherkin scenario
7. Implement thin Step Definitions
8. Implement Tasks
9. Implement Consequences (if needed)
10. Build and run
```

---

## Step 1: Understand the Requirement
Before writing any code, answer these questions:
- What is the user trying to achieve? (business goal)
- What are the preconditions?
- What is the expected outcome?
- What are the failure scenarios?

If you cannot answer these without reading UI code, stop. The requirement is unclear.

---

## Step 2: Identify the Actor
An Actor represents who is performing the action.

```csharp
// In Reqnroll: provided via ScenarioActor
// Name usually matches scenario or persona
Actor.Named("Alice")        // a named user
Actor.Named("Admin User")   // a role
```

One Actor per scenario. Never two.

---

## Step 3: Identify Required Abilities
What does the Actor need to perform the test?

| Scenario Type | Ability |
|---|---|
| Browser test | `BrowseTheWeb` (via `NScreenplay.Playwright`) |
| API test | `CallAnApi` (via `NScreenplay.Api`) |
| Combined | Multiple abilities on same Actor |

Abilities are granted in the lifecycle hook, not in step definitions:
```csharp
// In your BeforeScenario hook:
actor.Can(BrowseTheWeb.Using(page));
```

---

## Step 4: Define or Reuse Targets

**First: search existing Page classes for the Target you need.**

If it exists → reuse it. **Do not create a duplicate.**

If it does not exist → add it to the appropriate Page class:

```csharp
public static class CheckoutPage
{
    // Add new targets here, in the relevant Page class
    public static Target PromoCodeField = Target.The("promo code field").ByTestId("promo-input");
    public static Target ApplyButton    = Target.The("apply button").ByRole("button", "Apply");
}
```

**Target naming rules:**
- Name describes the element's role: `LoginButton` not `BlueButtonTopRight`
- Use PascalCase
- Name must match what a business analyst would call it

**Strategy selection (prefer in this order):**
1. `ByTestId` — most stable
2. `ByRole` — accessible and semantic
3. `ByLabel` — good for form fields
4. `ByPlaceholder` / `ByText` — readable but may change
5. `ByCss` / `ByXPath` — last resort

---

## Step 5: Define or Reuse Tasks

**First: search existing Task classes for the Task you need.**

If it exists → reuse it. **Do not create a duplicate.**

If it does not exist → create a new Task:

```csharp
public sealed class ApplyPromoCode : ITask
{
    private readonly string _code;
    
    private ApplyPromoCode(string code) => _code = code;
    
    public static ApplyPromoCode With(string code) => new(code);
    
    public async Task PerformAs(Actor actor, CancellationToken ct = default)
    {
        await actor.AttemptsTo(Enter.TheValue(_code).Into(CheckoutPage.PromoCodeField), ct);
        await actor.AttemptsTo(Click.On(CheckoutPage.ApplyButton), ct);
    }
}
```

**Task naming rules:**
- Name is a verb phrase describing intent: `Login.With(...)`, `ApplyPromoCode.With(...)`
- Not: `FillPromoCodeField`, `ClickApplyButton`

---

## Step 6: Write the Gherkin Scenario

```gherkin
Scenario: Applying a valid promo code reduces the order total
  Given the user has items in the cart
  When the user applies the promo code "SAVE10"
  Then the discount should be applied to the order total
```

**Checklist before writing Gherkin:**
- [ ] Scenario title describes a business outcome, not a UI action
- [ ] Steps use business vocabulary, not technical terms
- [ ] No selectors or URLs in the Gherkin text
- [ ] Each step is independently understandable
- [ ] Scenario is independent (does not require another scenario to run first)

---

## Step 7: Implement Thin Step Definitions

```csharp
[Binding]
public sealed class CheckoutSteps
{
    private readonly ScenarioActor _scenario;
    
    public CheckoutSteps(ScenarioActor scenario) => _scenario = scenario;
    
    [Given("the user has items in the cart")]
    public Task CartHasItems() =>
        _scenario.Actor.AttemptsTo(AddItemToCart.Any());
    
    [When("the user applies the promo code {string}")]
    public Task ApplyPromoCode(string code) =>
        _scenario.Actor.AttemptsTo(ApplyPromoCode.With(code));
    
    [Then("the discount should be applied to the order total")]
    public Task DiscountApplied() =>
        _scenario.Actor.Should(DiscountIsApplied.Now());
}
```

**Step Definition Rules:**
- One line per step (or a simple `await` + method call)
- No `if`, `for`, or multi-line logic
- No Playwright types (`IPage`, `ILocator`)
- No raw selectors
- No `Assert.X()` — use Consequences

---

## Step 8: Implement the Task Body
(If you created a new Task in Step 5 — fill in `PerformAs`.)

---

## Step 9: Implement Consequences (if needed)

```csharp
public sealed class DiscountIsApplied : IConsequence
{
    private static readonly DiscountIsApplied _instance = new();
    private DiscountIsApplied() {}
    public static DiscountIsApplied Now() => _instance;
    
    public async Task EvaluateAs(Actor actor, CancellationToken ct = default)
    {
        var total = await actor.AsksFor(Text.Of(CheckoutPage.OrderTotal), ct);
        // Parse and validate; throw if expectation not met
        if (!total.Contains("-"))
            throw new InvalidOperationException($"Expected discount in total but found: {total}");
    }
}
```

---

## Step 10: Build and Run

```
dotnet build
dotnet test
```

All tests must pass. No new warnings.

---

## Common Mistakes to Avoid

| Mistake | Consequence | Fix |
|---|---|---|
| Creating duplicate Target | Two sources of truth, both may drift | Search before adding |
| Creating duplicate Task | Inconsistent behavior | Search before adding |
| Writing selectors in step definitions | Tight coupling to UI | Move to Target |
| `Thread.Sleep` or `Task.Delay` | Flaky tests | Remove; Playwright auto-waits |
| Asserting in step definitions directly | Bypasses Consequence model | Create a Consequence |
| Implementation-driven Gherkin | Tests become UI scripts | Use business vocabulary |
| Testing multiple behaviors in one scenario | Difficult to diagnose failures | One scenario per behavior |
| Hardcoded credentials in step definitions | Security risk, hard to maintain | Use Task factory or config |
