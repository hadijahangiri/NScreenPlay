# Reqnroll Integration

`NScreenplay.Reqnroll` provides a scenario-scoped actor and feature-scoped browser lifecycle.

## Verified Types

- `BrowserManager`
- `ScenarioActor`
- `NScreenplayHooks`
- `ScenarioActorExtensions.InitializeFromFeatureBrowserAsync(...)`
- `NScreenplayConfiguration`
- `NScreenplayOptions`

## Sample Flow

The Login sample uses constructor injection:

```csharp
public LoginSteps(ScenarioActor scenario) => _scenario = scenario;
```

The hooks create the browser and scenario actor, then each step definition uses `scenario.Actor` to perform business-level actions.

Deterministic smoke harness:

- [samples/ReqnrollSmoke/README.md](../samples/ReqnrollSmoke/README.md)
- [samples/ReqnrollSmoke/reqnroll.json](../samples/ReqnrollSmoke/reqnroll.json)
- [samples/ReqnrollSmoke/Features/Smoke.feature](../samples/ReqnrollSmoke/Features/Smoke.feature)

## Parallel Isolation

Each scenario gets its own `IBrowserContext` and `IPage`, so cookies, storage, and cache are isolated across parallel tests.
