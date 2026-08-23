# NScreenplay

**AI-native Screenplay Test Automation Framework for .NET**

[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/nscreenplay/nscreenplay)
[![Tests](https://img.shields.io/badge/tests-260%20passing-brightgreen)](https://github.com/nscreenplay/nscreenplay)
[![Version](https://img.shields.io/badge/version-0.1.0-blue)](https://github.com/nscreenplay/nscreenplay)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

## What is NScreenplay?

NScreenplay brings the **Screenplay pattern** to modern .NET with first-class support for Playwright, Reqnroll (BDD/Gherkin), and AI agent integration via the Model Context Protocol (MCP).

It lets you write tests that read like business requirements and are understandable by both human developers and AI coding agents.

## Why Screenplay?

The Screenplay pattern organises test automation around **what a user does**, not **how the UI works**:

| Concept | Role |
|---------|------|
| **Actor** | The user or system performing actions |
| **Ability** | What the Actor can do (browse, call APIs) |
| **Task** | A business-level operation (Login, Checkout) |
| **Interaction** | A single atomic action (Click, Enter, Navigate) |
| **Target** | A semantic UI element (LoginPage.Username) |
| **Question** | A state query (Text.Of, Visibility.Of) |
| **Consequence** | A verification (DashboardIsDisplayed) |

## Quick Start

```bash
dotnet add package NScreenplay.Core
dotnet add package NScreenplay.Playwright
dotnet add package NScreenplay.Reqnroll
```

### Minimal Example (without BDD)

```csharp
using NScreenplay.Core;
using NScreenplay.Playwright;

// 1. Create an Actor
var actor = Actor.Named("Alice");

// 2. Grant browser ability
actor.Can(BrowseTheWeb.Using(page));  // IPage from Playwright

// 3. Navigate and log in (Task)
await actor.AttemptsTo(Navigate.To("https://myapp.com/login"));
await actor.AttemptsTo(
    LoginWithCredentials.Using("alice@example.com", "secret"));

// 4. Verify (Question + Consequence)
var isVisible = await actor.AsksFor(Visibility.Of(DashboardPage.Heading));
```

### BDD Example (with Reqnroll)

```gherkin
Feature: Login

  Scenario: Successful login
    Given the user is on the login page
    When the user logs in with valid credentials
    Then the dashboard should be displayed
```

```csharp
[Binding]
public sealed class LoginSteps
{
    private readonly ScenarioActor _scenario;
    public LoginSteps(ScenarioActor scenario) => _scenario = scenario;

    [When("the user logs in with valid credentials")]
    public Task Login() =>
        _scenario.Actor.AttemptsTo(LoginAs.ValidUser());

    [Then("the dashboard should be displayed")]
    public Task Verify() =>
        _scenario.Actor.Should(DashboardIsDisplayed.Now());
}
```

## Architecture

```
Reqnroll → NScreenplay.Reqnroll → NScreenplay.Core ← NScreenplay.Playwright
                                           ↑
                                   NScreenplay.Mcp (AI/MCP — optional)
```

**Core is completely independent.** Zero external dependencies. Integrations depend on Core, never the reverse.

## Package Structure

| Package | Purpose |
|---------|---------|
| `NScreenplay.Core` | Core abstractions (Actor, Ability, Task, etc.) |
| `NScreenplay.Playwright` | Playwright browser automation integration |
| `NScreenplay.Reqnroll` | Reqnroll BDD/Gherkin integration |
| `NScreenplay.Mcp` | MCP server for AI agent integration (optional) |

## Supported Versions

- **.NET 10** (target framework)
- **Microsoft.Playwright 1.49.0**
- **Reqnroll 3.3.4**
- **ModelContextProtocol 2.2.0** (MCP server, optional)

## AI/MCP Integration

NScreenplay exposes its capabilities to AI coding agents via the Model Context Protocol:

```bash
dotnet run --project src/NScreenplay.Mcp
```

Available MCP tools: `nscreenplay_list_tasks`, `nscreenplay_list_targets`, `nscreenplay_analyze_failure`, `nscreenplay_create_test_plan`, and more.

The AI can **DISCOVER**, **ANALYZE**, **PLAN**, and **PROPOSE** — but cannot modify code without explicit human approval.

## Sample

See [`samples/Login/`](samples/Login/) for a complete working example demonstrating:
- Reqnroll feature file
- NScreenplay Actor + Tasks
- Playwright interactions
- Targets defined as Page classes
- Consequences for verification

## Status

⚠️ **v0.1.0 — Pre-release.** APIs may change before v1.0.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT License](LICENSE)


**Status**: Phase 0 - Architecture Proposal (Not yet implemented)

## Vision

NScreenplay brings the Screenplay pattern to modern .NET with first-class support for:

- **Playwright** for browser automation
- **Reqnroll** for BDD/Gherkin
- **HTTP/API testing**
- **AI Agents** that understand the framework structure
- **Model Context Protocol (MCP)** for agent integration
- **Agent Skills** that teach AI how to use the framework

The framework is designed for **human readability** and **AI interoperability** simultaneously.

## Why NScreenplay?

### The Screenplay Pattern
Screenplay provides a business-readable abstraction over test automation:
- **Tasks** represent business-level actions
- **Interactions** are atomic operations  
- **Questions** query system state
- **Consequences** verify expectations
- **Actors** execute workflows

### Why .NET?
Modern C#, type safety, nullable reference types, LINQ, async/await, analyzers.

### Why Playwright?
Native browser synchronization, first-class async, rich debugging support.

### Why Reqnroll?
Production-grade BDD/Gherkin for .NET, active maintenance.

### Why AI-Native?
The framework must make it possible for AI agents to:
- Understand the project structure
- Discover existing Tasks, Targets, Questions
- Analyze test failures
- Suggest fixes
- Learn project conventions

## Project Status

**Current Phase**: Architecture Proposal (Phase 0)

**Expected Deliverables**:
- [ ] Architecture diagram
- [ ] Dependency graph  
- [ ] Core API proposal
- [ ] Lifecycle documentation
- [ ] Async model explanation
- [ ] Error handling strategy
- [ ] Extensibility model
- [ ] Risks & trade-offs

**Timeline**: See [ARCHITECTURE.md](docs/architecture/ARCHITECTURE.md) for detailed phase plan.

## Installation (Pre-release)

NScreenplay is not yet released. Follow development on GitHub.

## Quick Example (Proposed)

```csharp
// Create an actor
var actor = Actor.Named("User");

// Grant ability to browse the web
actor.Can(
    BrowseTheWeb.Using(page)
);

// Perform a task
await actor.AttemptsTo(
    Login.WithCredentials(
        "user@example.com",
        "password"
    )
);

// Ask a question
var title = await actor.AsksFor(
    Text.Of(Dashboard.Title)
);

// Verify consequences
await actor.Should(
    See.That(Dashboard.IsDisplayed())
);
```

## Architecture Overview

See [ARCHITECTURE.md](docs/architecture/ARCHITECTURE.md) for:
- Detailed architecture diagram
- Dependency direction
- Actor lifecycle
- Async model
- Extension model

## Core Principles

The framework follows 29 core principles:

1. **Core Independence**: Core has ZERO dependencies on Playwright, Reqnroll, or test runners.
2. **Dependency Direction**: Integrations depend on Core, never the reverse.
3. **Minimal APIs**: 10 excellent abstractions over 50 mediocre ones.
4. **Async-First**: All public APIs support async/await and CancellationToken.
5. **No Global State**: No static Actor state, no service locators.
6. **AI-First**: Framework exposes structured metadata, Skills, and MCP for agent interoperability.
7. **Composability**: Actors, Abilities, Tasks, Interactions are composable.
8. **Type Safety**: Use C# features (records, readonly, nullable).
9. **Playwright-Native**: Use Playwright's built-in waiting, no polling.
10. **Test Runner Independence**: Works with any .NET test framework.

See [PRINCIPLES.md](docs/PRINCIPLES.md) for the complete list.

## Project Structure

```
NScreenplay/
├── .github/
│   ├── agents/
│   │   └── nscreenplay-architect.agent.md
│   └── skills/
├── src/
│   ├── NScreenplay.Core/           # Core abstractions (independent)
│   ├── NScreenplay.Playwright/     # Playwright integration
│   ├── NScreenplay.Reqnroll/       # Reqnroll/BDD integration
│   ├── NScreenplay.Api/            # API testing ability
│   ├── NScreenplay.Mcp/            # MCP tool provider
│   ├── NScreenplay.Ai/             # AI provider abstraction
│   └── NScreenplay.Cli/            # CLI tooling
├── tests/
│   ├── NScreenplay.Core.Tests/
│   ├── NScreenplay.Playwright.Tests/
│   ├── NScreenplay.Reqnroll.Tests/
│   ├── NScreenplay.Api.Tests/
│   ├── NScreenplay.Mcp.Tests/
│   └── NScreenplay.IntegrationTests/
├── samples/
│   └── Login/                      # Complete Login example
├── skills/
│   ├── screenplay/
│   ├── playwright/
│   ├── reqnroll/
│   ├── test-authoring/
│   ├── test-review/
│   ├── failure-analysis/
│   └── healing/
├── docs/
│   ├── architecture/
│   ├── getting-started/
│   ├── screenplay/
│   └── ai/
└── NScreenplay.sln
```

## Development Phases

| Phase | Focus | Status |
|-------|-------|--------|
| **0** | Architecture Proposal | 🔄 In Progress |
| **1** | Core API | ⏳ Pending |
| **2** | Playwright Integration | ⏳ Pending |
| **3** | Reqnroll Integration | ⏳ Pending |
| **4** | API Testing | ⏳ Pending |
| **5** | Skills & Documentation | ⏳ Pending |
| **6** | AI/MCP Foundation | ⏳ Pending |

## Technology Stack

- **.NET 10**
- **C# latest**
- **Playwright** (Microsoft.Playwright)
- **Reqnroll** (BDD/Gherkin)
- **Roslyn** (code analysis)
- **MCP** (Model Context Protocol)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development workflow.

## Code of Conduct

See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Security

See [SECURITY.md](SECURITY.md).

## License

[MIT License](LICENSE) (pending)

## Resources

- [Architecture Documentation](docs/architecture/ARCHITECTURE.md)
- [Core Principles](docs/PRINCIPLES.md)
- [Screenplay Pattern](https://cucumber.io/docs/bdd/who-does-what/)
- [Playwright Documentation](https://playwright.dev/)
- [Reqnroll Documentation](https://docs.reqnroll.net/)
- [Model Context Protocol](https://modelcontextprotocol.io/)

---

**Status**: ⚠️ Pre-release. Not recommended for production use.

**Questions?** [Create an issue](https://github.com/NScreenplay/NScreenplay/issues) on GitHub.
