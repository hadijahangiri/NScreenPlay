# Changelog

All notable changes to NScreenplay are documented here.

## [0.1.0] — 2026-08-23

### Added

**Core (NScreenplay.Core)**
- `Actor` — scenario-scoped, `IAsyncDisposable`, strongly typed ability retrieval
- `IAbility` — marker interface for actor capabilities
- `IPerformable`, `ITask`, `IInteraction` — composable execution model
- `IQuestion<T>` — read-only state queries with typed returns
- `IConsequence` — assertion/verification abstraction
- `Target` — adapter-neutral semantic UI locator (9 strategies)
- `LocatorStrategy`, `LocatorStrategyKind` — strategy value objects
- `MissingAbilityException`, `ScreenplayException` — framework exception hierarchy
- Full `CancellationToken` propagation throughout
- Disposal-safe `Actor.DisposeAsync` (continues disposing all abilities even if one fails)

**Playwright Integration (NScreenplay.Playwright)**
- `BrowseTheWeb` — `IAbility` wrapping `IPage`, `IAsyncDisposable`
- `TargetResolver` — maps all 9 `LocatorStrategyKind` values to Playwright locators
- Interactions: `Click`, `Enter`, `Navigate`, `Select`, `Check`
- Questions: `Text`, `Visibility`, `CurrentUrl`, `PageTitle`, `InputValue`

**Reqnroll Integration (NScreenplay.Reqnroll)**
- `NScreenplayHooks` — `[BeforeFeature/AfterFeature/BeforeScenario/AfterScenario]` hooks
- `BrowserManager` — feature-scoped `IBrowser` lifecycle
- `ScenarioActor` — scenario-scoped `IBrowserContext + IPage + Actor`
- `NScreenplayOptions` + `NScreenplayConfiguration` — clean configuration
- `reqnroll.json` pattern for assembly scanning
- Parallel scenario isolation via separate `IBrowserContext` per scenario

**AI Agent Skills (7 skills)**
- `screenplay`, `playwright`, `reqnroll`, `test-authoring`, `test-review`, `failure-analysis`, `healing`
- Instruction-first format with numbered rules and GOOD/BAD examples

**MCP Server (NScreenplay.Mcp)**
- 11 MCP tools: discovery, failure analysis, test planning, healing workflow
- 8 MCP resources: `nscreenplay://framework`, `//architecture`, `//skills`, `//tasks`, `//targets`, `//interactions`, `//questions`, `//context`
- 1 MCP prompt: `nscreenplay_create_test`
- Deterministic `RequirementAnalyzer` and `TestPlanGenerator` (no LLM required)
- Rule-based `FailureAnalyzer` classifying 6 failure categories with confidence levels
- `IAiProvider` — optional LLM abstraction

**Controlled Healing (Phase 9)**
- Rules H-01 (ByCss#id → ByTestId) and H-02 (ById → ByTestId)
- `FixProposal` — strongly typed, fully audited proposal model
- 7-state state machine with invalid transition enforcement
- `ProposalStore` — thread-safe, SHA-256 staleness detection
- `ProposalApplicator` — atomic file writes with rollback capture
- `FileSafetyValidator` — path traversal protection
- AI cannot approve its own proposals (identity check)

**Login Sample**
- Complete runnable example: Reqnroll + NScreenplay + Playwright
- Self-contained HTML test app (no server required)
- Demonstrates Actor, Ability, Task, Target, Question, Consequence

### Security

- MCP server is read-only by default
- All file operations validated against workspace boundary
- Inputs truncated and sanitised before processing
- Proposal application requires explicit human approval

### Known Limitations

- `BrowserManager` supports Chromium only (Firefox/WebKit in future)
- `NScreenplayConfiguration` is a global singleton (set-once pattern)
- Healing rules are regex-based (no AST analysis)
- `NSCREENPLAY_WORKSPACE_ROOT` must be set for healing file operations
- No NuGet publishing in v0.1 (packages built but not published)
- Tested on Windows only (Linux expected to work, unverified)

---

## [Unreleased]

- Firefox and WebKit support in `BrowserManager`
- Roslyn-based healing rules (AST-aware)
- Screenshot capture on scenario failure
- LLM-assisted failure analysis (with explicit provider)
- `NScreenplay.Api` package for HTTP/REST testing
- `NScreenplay.Cli` package
