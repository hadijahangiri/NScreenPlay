# Contributing to NScreenplay

Thank you for helping improve NScreenplay.

## Prerequisites

- .NET 10 SDK
- Playwright browsers for the Login sample and Playwright tests
- Git

## Clone

```bash
git clone https://github.com/hadijahangiri/NScreenPlay.git
cd NScreenPlay
```

## Restore, Build, Test, Pack

Use the repository root solution:

```bash
dotnet restore NScreenplay.sln
dotnet build NScreenplay.sln
dotnet test NScreenplay.sln
dotnet pack NScreenplay.sln -c Release -o artifacts
```

## Coding Expectations

- Keep the core package free of Playwright and Reqnroll dependencies.
- Prefer async APIs and forward `CancellationToken` through public methods.
- Keep documentation aligned with the actual source code.
- Avoid adding new public APIs unless they are needed for the current release.
- Keep AI/MCP behavior read-only unless the workflow explicitly requires approval.

## Pull Requests

- Open focused pull requests with a clear description of the change.
- Include build and test results when behavior changes.
- Update docs whenever public APIs or workflows change.

## Test Requirements

- `dotnet build` must succeed.
- `dotnet test` must succeed.
- `dotnet pack` must succeed for the published packages.
