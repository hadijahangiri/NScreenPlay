# Screenplay Pattern

NScreenplay centers test automation on a small set of concepts:

Actor → Ability → Task → Interaction → Question → Consequence

## Core Types

- `Actor` creates and owns abilities
- `IAbility` represents what the actor can do
- `ITask` and `IInteraction` are both `IPerformable`
- `IQuestion<T>` reads state
- `IConsequence` verifies state
- `Target` describes what the adapter should find

## Verified API Shape

From `NScreenplay.Core`:

- `Actor.Named(string)`
- `Actor.Can(IAbility)`
- `Actor.AttemptsTo(IPerformable, CancellationToken)`
- `Actor.AttemptsTo(IEnumerable<IPerformable>, CancellationToken)`
- `Actor.AsksFor<T>(IQuestion<T>, CancellationToken)`
- `Actor.Should(IConsequence, CancellationToken)`
- `Actor.Should(IEnumerable<IConsequence>, CancellationToken)`
- `Target.The(string)`
- `Target.ByCss(string)`
- `Target.ByXPath(string)`
- `Target.ByRole(string, string?)`
- `Target.ByLabel(string)`
- `Target.ById(string)`
- `Target.ByTestId(string)`
- `Target.ByText(string)`
- `Target.ByPlaceholder(string)`
- `Target.ByAltText(string)`

## Why It Works

The core package has no dependency on Playwright or Reqnroll. That keeps the abstraction reusable across integrations and easy to discover by AI tooling.
