# Reqnroll Smoke Harness

Deterministic smoke harness for validating this path:

Reqnroll feature -> generated tests -> NScreenplay hooks -> ScenarioActor -> BrowseTheWeb -> Playwright -> scenario execution -> disposal.

## Why this exists

This sample is intentionally minimal and local-only so failures can be categorized clearly without external network noise.

## Required packages

- Reqnroll.xUnit
- Microsoft.Playwright
- NScreenplay.Core
- NScreenplay.Playwright
- NScreenplay.Reqnroll

## Determinism

- Uses local HTML at TestApplication/smoke.html via page.SetContentAsync
- No dependency on external websites
- Headless browser by default through NScreenplay.Reqnroll options

## Canonical files

- reqnroll.json
- Features/Smoke.feature
- StepDefinitions/SmokeSteps.cs
- Support/SmokeHooks.cs
- Support/SmokeTestApp.cs

## Run

```bash
dotnet restore samples/ReqnrollSmoke/ReqnrollSmoke.csproj
dotnet build samples/ReqnrollSmoke/ReqnrollSmoke.csproj -c Release --no-restore
dotnet test samples/ReqnrollSmoke/ReqnrollSmoke.csproj -c Release --no-build
```

## Common failure categories

- Feature not discovered: .feature missing or generation problem
- Generated test missing: Reqnroll generator/toolchain issue
- Binding not discovered: [Binding] not found or namespace/class mismatch
- ScenarioActor not initialized: missing hook order or missing init hook
- Browser init failure: Playwright launch failure
- Browser binary missing: Playwright browsers not installed
- Target/action failure: locator or interaction mismatch
- Assertion/question failure: expected state not reached
- Disposal failure: resource cleanup exceptions
