# NScreenplay — Failure Analysis Skill

## What This Skill Is For
Use this skill when a test has **failed** and you need to reason about why. Follow the analysis workflow precisely. **Do not modify any code** until you have determined the root cause.

---

## IMPORTANT RULE
**Never modify test code or application code during analysis.**

Analysis produces a diagnosis and a recommendation.
A human reviews the recommendation and approves changes.

---

## Failure Classification

Classify the failure into exactly one of these categories before proceeding:

| Category | Description | Example |
|---|---|---|
| **Application failure** | The app behaved incorrectly | Login accepted wrong password |
| **Test logic failure** | The test asserts the wrong thing | Consequence checks wrong element |
| **Selector failure** | The Target locator no longer matches | Element renamed, restructured |
| **Synchronization failure** | The test acted before the app was ready | Clicked button before page loaded |
| **Infrastructure failure** | Environment problem (browser, network, CI) | Browser crashed, timeout due to slow CI |
| **Framework failure** | Bug in NScreenplay itself | Ability not found, disposal error |

---

## Analysis Workflow

### Phase 1: Read the Failure
1. Read the full exception message and stack trace.
2. Identify the **scenario** name from the Reqnroll output.
3. Identify the **failing step** (Given/When/Then).
4. Identify which **Step Definition method** was executing.
5. Identify which **Task** or **Interaction** threw.

```
Example stack trace reading:
  at NScreenplay.Playwright.Click.PerformAs(...)      ← Interaction: Click
    at Login.Tasks.LoginWithCredentials.PerformAs(...)  ← Task: LoginWithCredentials
      at Login.StepDefinitions.LoginSteps.Login()       ← Step: "When the user logs in"
```

### Phase 2: Identify the Target
1. Find the Target being used by the failing Interaction.
2. Examine the Target's locator strategies.
3. Determine whether the element exists on the page.

**Questions to ask:**
- Was the Target recently changed?
- Does the element's `testid`, role, or label still match the Target definition?
- Is the page in the correct state before this step runs?

### Phase 3: Classify the Failure

#### If `PlaywrightException: Element not found` or `TimeoutException`
→ Likely **Selector failure** or **Synchronization failure**

Check:
- Open the page manually. Does the element exist?
- Was a `data-testid` attribute removed or renamed?
- Did a recent UI change restructure the page?
- Does the step require a previous action to have completed?

#### If the element is found but the assertion fails
→ Likely **Application failure** or **Test logic failure**

Check:
- Did the application return unexpected data?
- Does the Consequence check the right element and the right condition?
- Is the Consequence's failure message accurate?

#### If the test passed before but fails intermittently
→ Likely **Synchronization failure** or **Infrastructure failure**

Check:
- Is there any `Thread.Sleep` or `Task.Delay` that was masking a race condition?
- Is the test environment slow (CI, VPN, low memory)?
- Are tests being run in parallel in a way that causes interference?

#### If the error is in the hooks or lifecycle
→ Likely **Infrastructure failure** or **Framework failure**

Check:
- Is `BrowserManager.InitializeAsync` failing? Check browser installation.
- Is `ScenarioActor.InitializeAsync` failing? Check `reqnroll.json` configuration.
- Is an ability missing? Check that the hook correctly calls `actor.Can(...)`.

### Phase 4: Determine Root Cause

State the root cause precisely:
```
Root cause: The Target `LoginPage.LoginButton` uses `ByTestId("login-button")` but
the HTML element's attribute was changed from `data-testid="login-button"` to
`data-testid="signin-button"` in commit abc1234.
```

### Phase 5: Recommend a Fix

State a concrete, minimal recommendation:
```
Recommended fix: Update `LoginPage.LoginButton` to use `.ByTestId("signin-button")`.
Verify there are no other references to the old testid.
Do not change any other Targets or Tasks.
```

**Do not suggest changes beyond the minimum necessary.**

---

## Common Failure Patterns

### Pattern A: `Timeout waiting for element`
```
Failure: Microsoft.Playwright.TimeoutException: Timeout 30000ms exceeded while
waiting for locator('data-testid=login-button') to be visible.
```

Analysis:
1. Is the page loaded? Check `Navigate.To(url)` ran successfully.
2. Does the element exist at all? Check the testid in the HTML.
3. Is the element hidden by CSS? Check visibility conditions.
4. Did a prerequisite step fail silently?

### Pattern B: `MissingAbilityException`
```
Failure: Actor 'Alice' does not have the ability 'BrowseTheWeb'.
```

Analysis:
1. Is `actor.Can(BrowseTheWeb.Using(page))` called in the lifecycle hook?
2. Is `ScenarioActor.InitializeAsync` called before the first step?
3. Is the `[BeforeScenario(Order = 10)]` hook running? Check `reqnroll.json`.

### Pattern C: Consequence throws unexpected message
```
Failure: Expected dashboard heading to be visible, but it was not.
```

Analysis:
1. What page is the browser actually on? Check `CurrentUrl.Value()`.
2. Did the login Task execute successfully?
3. Does the Target `DashboardPage.Heading` resolve to the right element?

### Pattern D: `KeyNotFoundException: BrowserManager`
```
Failure: The given key 'NScreenplay.Reqnroll.BrowserManager' was not present.
```

Analysis:
1. Is `reqnroll.json` present in the test project?
2. Does `reqnroll.json` include `"assembly": "NScreenplay.Reqnroll"` in `stepAssemblies`?
3. Is `reqnroll.json` set to `CopyToOutputDirectory: PreserveNewest`?

---

## Failure Analysis Report Format

```
SCENARIO: [name of failed scenario]
STEP: [Given/When/Then step text]
EXCEPTION TYPE: [e.g., PlaywrightException, MissingAbilityException]
EXCEPTION MESSAGE: [full message]

CLASSIFICATION: [Application / Test Logic / Selector / Synchronization / Infrastructure / Framework]

EXECUTION PATH:
  Step Definition → Task → Interaction → Target/Ability

ROOT CAUSE:
  [Precise, specific statement of why the failure occurred]

EVIDENCE:
  [Stack trace location, element state, URL, etc.]

RECOMMENDED FIX:
  [Minimal, specific change. File and location.]

DO NOT CHANGE:
  [List of things that should NOT be touched]
```

---

## What Not To Do During Analysis
- Do not modify Targets "to see if it works"
- Do not add `Thread.Sleep` to "fix" timing issues
- Do not change Gherkin to avoid the failure
- Do not delete the scenario
- Do not change the Consequence to always pass
