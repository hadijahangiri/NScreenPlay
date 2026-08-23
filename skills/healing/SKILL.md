# NScreenplay — Healing Skill

## Status: FUTURE FUNCTIONALITY

**Healing is not yet implemented in NScreenplay.**

This skill defines the **rules and constraints** that a future healing agent must follow. It exists to prevent premature or unsafe healing implementations from being built.

---

## What Healing Is (and Is Not)

**Healing IS:**
- A guided, approval-gated process for proposing targeted fixes to failing tests
- A way to surface actionable repair candidates to a human reviewer
- A tool that suggests one minimal change with supporting evidence

**Healing IS NOT:**
- Autonomous code modification
- Silent self-repair
- A replacement for proper test maintenance
- A way to suppress failures without fixing root causes

---

## The Healing Workflow (Future)

```
Test failure detected
        ↓
[Failure Analysis]     ← Use failure-analysis skill first
        ↓
Evidence collected
  - Exception + stack trace
  - Screenshot (if available)
  - Playwright trace (if available)
  - Page URL at failure
  - Target information
  - Actor name, Task name, Interaction name
        ↓
AI generates candidate diagnosis
        ↓
AI proposes candidate fix
  (single, minimal, targeted change)
        ↓
Validation
  - Does the proposed change compile?
  - Does it make sense given the evidence?
  - Does it touch only the minimum necessary?
        ↓
Human review and approval
  - Human reads diagnosis
  - Human reads proposed fix
  - Human approves or rejects
        ↓
Change applied (only after explicit approval)
        ↓
Test re-run to verify fix
```

---

## Rules for Future Healing Agents

### Rule H-01: Never Modify Code Without Explicit Approval
A healing agent must **never** write to the filesystem without a documented, explicit human approval in the current session.

The approval must include:
- The file to be modified
- The specific change (old → new)
- The reason for the change

### Rule H-02: One Fix at a Time
A healing candidate must target exactly one issue. If multiple issues are found, present them separately. Do not bundle multiple fixes into one proposal.

### Rule H-03: Minimum Necessary Change
The proposed fix must be the smallest possible change that addresses the root cause.

```
GOOD: Change Target.The("btn").ByTestId("old-id") to ByTestId("new-id")
BAD:  Rewrite the entire LoginPage class
```

### Rule H-04: No Healing of Business Logic
Healing agents must not modify:
- Task implementations
- Consequence logic
- Gherkin feature files
- Step Definition step patterns

Healing may only modify:
- `Target` locator strategies (selector updates)
- Test data / configuration values
- Infrastructure settings (timeouts, URLs)

**When in doubt, do not heal. Report to a human.**

### Rule H-05: Preserve Failure Evidence
Before proposing a fix, record all available evidence:
- Exception type and message
- Stack trace
- Screenshot path (if captured)
- Playwright trace path (if captured)
- Page URL at failure time
- HTML snapshot or relevant element state

This evidence must accompany any healing proposal so the human reviewer can make an informed decision.

### Rule H-06: No Silent Retry Logic
Healing must not add retry loops, exception suppression, or `try/catch` blocks that hide failures. If a test is failing, the failure must be visible.

### Rule H-07: Validate Before Proposing
Before presenting a healing proposal, verify:
1. The proposed change compiles (conceptually).
2. The change addresses the specific root cause identified in failure analysis.
3. The change does not break other tests (check for other usages of the modified Target).

### Rule H-08: Distinguish Healing from Refactoring
Healing fixes a broken test. Refactoring improves working code. Never conflate the two in a healing proposal.

---

## Evidence Model (Future Implementation)

When NScreenplay implements failure capture, a `FailureContext` will contain:

```csharp
public record FailureContext
{
    public string ActorName { get; init; }
    public string ScenarioTitle { get; init; }
    public string StepText { get; init; }
    public string TaskName { get; init; }
    public string InteractionName { get; init; }
    public string TargetName { get; init; }
    public string PageUrl { get; init; }
    public Exception Exception { get; init; }
    public string? ScreenshotPath { get; init; }
    public string? TracePath { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
```

This model is intentionally minimal for v0.1. It will be extended in future milestones.

---

## What Is Safe to Implement Now (v0.1)
The following healing-adjacent capabilities are acceptable to build now:

1. **Failure capture**: Record `FailureContext` in `[AfterScenario]` when a scenario fails.
2. **Screenshot on failure**: Capture a screenshot and attach path to `FailureContext`.
3. **Report generation**: Produce a structured failure report consumable by AI tools.

None of these modify code. They only collect and surface information.

---

## What Must Not Be Built Until Approved
- Automatic test code modification
- Automatic selector replacement
- Any healing that runs without human review
- Any agent with write access to source files without approval
