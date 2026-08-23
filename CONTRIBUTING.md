# Contributing to NScreenplay

Thank you for your interest in contributing!

## Development Setup

**Prerequisites:**
- .NET 10 SDK
- Microsoft.Playwright (install browsers: `playwright install chromium`)
- A modern code editor (VS Code recommended)

**Clone and build:**
```bash
git clone https://github.com/nscreenplay/nscreenplay.git
cd nscreenplay
dotnet build
dotnet test
```

## Running Tests

```bash
dotnet test                    # all tests
dotnet test -c Release         # release mode
dotnet test tests/NScreenplay.Core.Tests     # specific project
```

## Coding Conventions

- **Nullable reference types** enabled — no `null!` suppression without justification
- **Async-first** — all public APIs that perform I/O are `async Task`
- **CancellationToken** — propagate through every async call
- **No static mutable state** — no `AsyncLocal<T>` without documented rationale
- **No `Thread.Sleep`** — use Playwright's built-in waiting
- **XML docs on all public APIs** — explain intent, not implementation
- **Records for immutable data** — prefer records over classes for value types
- **No premature abstractions** — every interface needs a concrete reason

## Adding a Task

Tasks live in `src/NScreenplay.Playwright` or your integration layer.

```csharp
public sealed class MyBusinessTask : ITask
{
    private readonly string _param;
    private MyBusinessTask(string param) => _param = param;
    public static MyBusinessTask With(string param) => new(param);

    public async Task PerformAs(Actor actor, CancellationToken ct = default)
    {
        await actor.AttemptsTo(Enter.TheValue(_param).Into(MyPage.InputField), ct);
        await actor.AttemptsTo(Click.On(MyPage.SubmitButton), ct);
    }
}
```

## Adding a Target

```csharp
public static class MyPage
{
    public static Target SubmitButton = Target.The("submit button").ByTestId("submit-btn");
    public static Target InputField  = Target.The("input field").ByLabel("My Field");
}
```

## Adding a Skill

1. Create `skills/your-skill/SKILL.md`
2. Follow the existing Skill format (instruction-first, GOOD/BAD examples, numbered rules)
3. Verify no obsolete API references

## Pull Request Expectations

- All tests must pass (`dotnet test`)
- Zero warnings (`dotnet build -warnaserror`)
- Public API changes require documentation update
- Breaking changes must be clearly labelled
- New dependencies require justification

## Architecture Rules

- **Core** must have zero external dependencies
- Integrations depend on Core, never the reverse
- MCP tools are read-only by default
- Healing requires explicit human approval

## Code of Conduct

Be respectful, constructive, and professional.
