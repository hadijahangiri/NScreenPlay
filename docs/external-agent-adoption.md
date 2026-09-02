# External-Agent Adoption Playbook

Canonical end-to-end adoption workflow for external AI coding agents.

Scope:

- Existing .NET test project
- Analyze
- Plan
- Safe Apply
- Author test
- Run
- Diagnose failure

This playbook is deterministic and contract-first. It does not add new framework features.

## 1. What NScreenplay is

NScreenplay is an AI-native Screenplay Test Automation Framework for .NET.

Core flow:

Actor -> Ability -> Task -> Interaction -> Question -> Consequence

## 2. Supported packages

Official adoption NuGet packages:

- NScreenplay.Core
- NScreenplay.Playwright
- NScreenplay.Reqnroll

Tooling package role:

- NScreenplay.Mcp is an MCP server/tooling layer, not an adoption package.

## 3. Unsupported/nonexistent packages

No official package or adapter exists in this repository for:

- NScreenplay.Api
- NScreenplay.BDDfy

For API-only projects, use NScreenplay.Core with a custom HttpClient-backed Ability.
For BDDfy projects, preserve BDDfy and do not introduce a fake NScreenplay.BDDfy adapter.

## 4. Project detection

Goal: determine project shape before any mutation.

Input:

- Project directory path or .csproj path

Action:

- Use repository evidence and MCP detection flow.
- Detect test framework, BDD framework, browser automation, API signals, and current NScreenplay presence.

Expected output:

- Deterministic project metadata with adoptionLevel and recommended packages/skills.

Failure handling:

- If path is invalid or outside workspace root, stop and report error.

Safety boundary:

- No restore, build, test, install, or shell execution during detection.

## 5. Analyze

Use MCP tool:

- nscreenplay_analyze_project

Input:

- projectPath

Action:

- Analyze project read-only.

Expected output:

- testFramework, bddFramework, browserAutomation, apiTesting
- nscreenplay package presence
- recommendedPackages
- recommendedSkills
- adoptionLevel
- warnings/evidence

Failure handling:

- On analyzer error, stop and do not continue to plan/apply.

Safety boundary:

- Analyze is read-only by contract.

## 6. Plan

Use MCP tool:

- nscreenplay_create_adoption_plan

Input:

- same projectPath used in Analyze

Action:

- Produce deterministic, structured adoption plan.

Expected output:

- recommendedPackages (allow-list constrained)
- recommendedSkills
- steps, risks, warnings, preservationRules, estimatedComplexity

Allowed package outcomes:

- NScreenplay.Core
- NScreenplay.Playwright
- NScreenplay.Reqnroll

Framework preservation rules:

- Preserve existing test framework.
- Preserve existing BDD framework.
- Do not replace BDDfy.
- Do not introduce Playwright into API-only projects.

Failure handling:

- If planning fails, stop and report.

Safety boundary:

- Plan is read-only by contract.

Skills to load for plan/author/diagnose coverage:

- screenplay
- playwright
- reqnroll
- test-authoring
- test-review
- failure-analysis
- healing

## 7. Apply

Use MCP tool:

- nscreenplay_apply_adoption_plan

Reference MCP workflow resource:

- nscreenplay://adoption-workflow

Input:

- projectPath
- planJson from nscreenplay_create_adoption_plan
- dryRun flag

Action:

- Validate Analyze->Plan consistency.
- Validate project path and workspace boundaries.
- Apply only explicit plan-driven package actions.

Expected output:

- status (DryRun or Success)
- appliedOperations
- warnings/errors

Dry-run behavior:

- dryRun=true returns planned operations with zero file mutation.

Approval boundary:

- Apply requires explicit human approval before execution.
- Autonomous adoption is not allowed.

Never executed by MCP:

- shell commands
- PowerShell commands
- arbitrary scripts
- arbitrary code execution
- framework replacement

Failure handling:

- On ValidationFailed, Conflict, or PreconditionFailed, stop and report exact error.

Safety boundary:

- Workspace-root restriction
- Path traversal rejection
- Reparse-point rejection
- Package allow-list enforcement

## 8. Author

After successful adoption apply, author the first test with minimal surface.

### xUnit + Playwright (minimum path)

Input:

- NScreenplay.Core + NScreenplay.Playwright added

Action:

- Create Actor
- Grant BrowseTheWeb ability
- Reuse or define Target
- Use Navigate/Enter/Click interactions
- Use Question(s) for state reads

Expected output:

- First passing deterministic test using Screenplay boundaries

Failure handling:

- Keep steps minimal and isolate selector issues in Targets.

Safety boundary:

- No direct low-level Playwright calls in business Tasks when reusable interactions exist.

### Reqnroll + Playwright (minimum path)

Input:

- Existing Reqnroll project
- NScreenplay.Core + NScreenplay.Playwright + NScreenplay.Reqnroll

Action:

- Keep Reqnroll
- Use ScenarioActor in step definitions
- Keep step definitions thin
- Delegate actions to Tasks/Interactions

Expected output:

- Passing scenario with Actor lifecycle and preserved BDD framework

Deterministic harness reference:

- samples/ReqnrollSmoke/README.md

Failure handling:

- Validate reqnroll.json, hooks order, and feature discovery if tests are not discovered.

Safety boundary:

- Do not replace Reqnroll.

### API-only (minimum path)

Input:

- Existing xUnit API tests with HttpClient

Action:

- Use NScreenplay.Core
- Implement custom HttpClient-backed Ability
- Model operations as Tasks
- Model reads as Questions

Expected output:

- Passing API-only test with Screenplay structure and no browser dependency

Failure handling:

- Separate HTTP/environment issues from NScreenplay structure issues.

Safety boundary:

- Do not introduce NScreenplay.Api (nonexistent).
- Do not introduce Playwright if not needed.

## 9. Run

Input:

- Adopted project with authored first test(s)

Action:

- dotnet restore
- dotnet build
- dotnet test

Expected output:

- Restore/build succeed
- Test discovery succeeds
- Test execution result is explicit

Failure handling:

- If environment error occurs (for example Playwright browser installation/network), classify as environment/tooling unless evidence points to framework bug.

Safety boundary:

- Execution occurs in user/CI environment, not through MCP mutation tools.

## 10. Diagnose

Use MCP tools:

- nscreenplay_get_failure_context
- nscreenplay_analyze_failure

Input:

- scenarioTitle
- stepText
- exceptionType
- exceptionMessage
- task/interaction/target context when available

Action:

- Classify failure
- Collect evidence
- Propose minimal remediation

Expected output:

- category
- probable root cause
- investigation steps
- do-not-do list

Failure handling:

- If evidence is incomplete, report missing data and do not invent root cause.

Safety boundary:

- Diagnosis does not mutate code.

## 11. Healing boundaries

Healing is approval-gated and non-autonomous.

- Human approval is required.
- AI must not approve its own proposal.
- No automatic healing or approval.

## 12. Framework preservation

- Preserve xUnit/NUnit/MSTest.
- Preserve Reqnroll.
- Preserve BDDfy where present.
- Migrate incrementally; avoid broad replacement rewrites.

## 13. API-only pattern

Canonical pattern:

- xUnit + HttpClient + NScreenplay.Core + custom HttpClient Ability

No official NScreenplay.Api package exists.

## 14. BDDfy handling

- Detect BDDfy.
- Preserve BDDfy.
- Do not recommend NScreenplay.BDDfy.

## 15. Troubleshooting

- Analyze failed: verify projectPath and workspace root.
- Plan failed: stop and report planning error.
- Apply rejected: regenerate Analyze->Plan and re-run with explicit approval.
- Reqnroll discovery issues: verify reqnroll.json and hook ordering.
- Playwright environment issues: verify browser installation and network access separately from framework logic.

## 16. Minimal examples

### A. Analyze -> Plan -> Dry-run Apply

1. nscreenplay_analyze_project(projectPath)
2. nscreenplay_create_adoption_plan(projectPath)
3. present plan for explicit approval
4. nscreenplay_apply_adoption_plan(projectPath, planJson, dryRun: true)

### B. First diagnose call

1. nscreenplay_get_failure_context()
2. nscreenplay_analyze_failure(...)

## 17. Definition of success

The workflow is successful when all items below are true:

- Agent detects project shape without guessing package names.
- Analyze output is valid and deterministic.
- Plan output recommends only real, allowed packages.
- Apply succeeds as dry-run, then safe apply after explicit approval.
- First test is authored for target path (xUnit+Playwright, Reqnroll+Playwright, or API-only).
- restore/build/test execution is explicit and reproducible.
- Failure diagnosis is evidence-first and classification-based.
- No hallucinated package/API appears.
- Safety boundaries remain intact.
