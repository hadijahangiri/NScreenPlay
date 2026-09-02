# Project Analysis

`nscreenplay_analyze_project` is the Phase A MCP tool for read-only assessment of an existing .NET test project.

## What It Does

- inspects a project directory or `.csproj` file
- detects project shape, test framework, BDD framework, browser automation, and API testing signals
- detects Screenplay-style code and NScreenplay package usage
- recommends NScreenplay packages and Skills
- returns a deterministic adoption level and migration plan

## What It Does Not Do

- it does not restore packages
- it does not build the project
- it does not run tests
- it does not install packages
- it does not modify files
- it does not execute shell commands
- it does not migrate code automatically

## Explicit Package Reality and Manual Patterns

The only official NuGet adoption packages in this repository are:

- `NScreenplay.Core`
- `NScreenplay.Playwright`
- `NScreenplay.Reqnroll`

`NScreenplay.Mcp` is tooling, not an adoption package. There is no official `NScreenplay.Api` package and no official `NScreenplay.BDDfy` adapter in this repository.

For API-only projects, the guidance is to keep xUnit + `HttpClient` and use `NScreenplay.Core` with a manually implemented `IAbility`/`ITask` pattern, not a fake `NScreenplay.Api` package. For BDDfy-based projects, preserve the existing BDDfy framework and avoid any recommendation for `NScreenplay.BDDfy`.

## Adoption Levels

- `not-applicable` - the project does not look like a target for NScreenplay adoption
- `possible` - not enough evidence for a strong recommendation
- `recommended` - the project appears suited for NScreenplay adoption
- `partially-adopted` - some Screenplay or NScreenplay usage already exists
- `already-adopted` - the project already appears to use NScreenplay well

## AI Workflow

Analyze
↓
Understand
↓
Plan
↓
Human/Agent approval
↓
Apply

Phase A ends at Analyze.

## Phase B

`nscreenplay_create_adoption_plan` consumes the analysis result and produces a structured adoption plan. It is still read-only and does not modify the project.

Phase B creates a migration/adoption plan, but it does not mutate code, install packages, restore the project, build the project, execute tests, or run shell commands. The plan is a human/agent review artifact for approval before any later execution phase.

## Phase C

Phase C applies an approved plan through `nscreenplay_apply_adoption_plan`.

Phase C is execution-only and must remain plan-driven:

- apply only explicit plan package actions
- validate project identity and workspace root boundaries
- support `dryRun` with zero mutation
- reject malformed or unsupported plan actions
- do not execute shell commands, PowerShell, scripts, or arbitrary code

Do not add `nscreenplay_apply_adoption` or any file/modification capability during Phase B.

## Agent Workflow

1. Discover NScreenPlay.
2. Load the relevant Skills.
3. Call `nscreenplay_analyze_project`.
4. Call `nscreenplay_create_adoption_plan`.
5. Review the plan.
6. Get human approval.
7. Apply the approved plan with `nscreenplay_apply_adoption_plan`.

## Phase D

Phase D defines the canonical AI adoption workflow contract:

Analyze
-> Plan
-> Human Approval
-> Apply
-> Validate
-> Final Report

Rules:

- Agents must not skip Analyze.
- Agents must not construct a replacement plan when `nscreenplay_create_adoption_plan` is available.
- Agents must not call Apply before explicit human approval.
- Agents must not invoke shell, PowerShell, scripts, or arbitrary commands through MCP.
- Validation should prefer deterministic checks from Apply output and project state.
- Build/test execution is a manual validation step outside MCP mutation boundaries.

Failure handling:

- Analysis failure: stop.
- Planning failure: stop.
- Plan rejected: stop with no mutation.
- Apply failure: report structured failure and stop.
- Validation failure: report `ADOPTION APPLIED - VALIDATION INCOMPLETE`.

Skills are knowledge and instructions. Analyze is project understanding. Plan is migration strategy. Apply is execution.

## Example Output

### Phase A: analyze result

```json
{
  "projectPath": "C:/work/MyTests",
  "projectType": "dotnet-test",
  "language": "C#",
  "targetFrameworks": ["net8.0"],
  "testFramework": "xunit",
  "bddFramework": null,
  "browserAutomation": "playwright",
  "apiTesting": false,
  "nscreenplay": {
    "core": false,
    "playwright": false,
    "reqnroll": false,
    "mcp": false
  },
  "screenplayDetected": false,
  "screenplayDetectionEvidence": [],
  "recommendedPackages": ["NScreenplay.Core", "NScreenplay.Playwright"],
  "recommendedSkills": ["screenplay", "playwright", "test-authoring", "test-review"],
  "adoptionLevel": "recommended",
  "migrationPlan": ["Introduce NScreenplay.Core and an Actor lifecycle."],
  "warnings": [],
  "evidence": ["Parsed package metadata from MyTests.csproj"]
}
```

### Phase B: adoption plan example

```json
{
  "projectPath": "C:/work/MyTests",
  "currentState": {
    "adoptionLevel": "recommended",
    "testFramework": "xunit",
    "bddFramework": null,
    "browserAutomation": "playwright",
    "apiTesting": false
  },
  "recommendedPackages": ["NScreenplay.Core", "NScreenplay.Playwright"],
  "recommendedSkills": [
    { "name": "screenplay", "reason": "Required to structure Actors, Abilities, Tasks, Questions, and Interactions." },
    { "name": "playwright", "reason": "Relevant because the project uses Playwright browser automation and should keep browser actions behind abilities." },
    { "name": "test-authoring", "reason": "Useful for migration planning and incremental conversion of tests into Screenplay patterns." },
    { "name": "test-review", "reason": "Useful to validate migration quality, preserve boundaries, and avoid over-aggressive refactors." }
  ],
  "steps": [
    {
      "id": "introduce-core",
      "title": "Introduce NScreenplay.Core",
      "category": "package",
      "priority": "required",
      "reason": "The project currently does not reference NScreenplay.Core and needs the core Actor/Task/Question model.",
      "dependsOn": [],
      "affectedAreas": ["test project"]
    },
    {
      "id": "introduce-playwright",
      "title": "Add NScreenplay.Playwright",
      "category": "package",
      "priority": "required",
      "reason": "The project uses Playwright and can move browser actions behind BrowseTheWeb and Screenplay interactions.",
      "dependsOn": ["introduce-core"],
      "affectedAreas": ["browser tests", "page objects"]
    }
  ],
  "risks": ["Existing tests may contain direct Playwright calls that need incremental migration."],
  "warnings": [],
  "preservationRules": [
    "Preserve the existing test framework.",
    "Do not introduce Playwright into API-only projects."
  ],
  "estimatedComplexity": "medium"
}
```

## How Agents Should Use It

Use the analyzer result as evidence for planning, not as an instruction to mutate code. The result should inform package recommendations, skill selection, and migration sequencing only.

Skills are knowledge and instructions. Analyze is project understanding. Plan is migration strategy. Apply is execution.