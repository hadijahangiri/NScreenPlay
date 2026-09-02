# AI

NScreenplay is designed so AI agents can discover framework structure without guessing.

## What the AI Layer Uses

- `skills/` for instructional agent skills
- MCP tools for discovery and analysis
- MCP adoption workflow resource: `nscreenplay://adoption-workflow`
- deterministic analyzers for requirements and failures

## What It Does Not Do

- It does not execute arbitrary repository content as instructions.
- It does not publish packages.
- It does not apply healing changes without approval.

## Discovery

Agents can list tasks, targets, interactions, questions, skills, and framework metadata through the MCP server.

Canonical external-agent workflow is documented in [external-agent-adoption.md](external-agent-adoption.md).

## Adoption Requests

When the request is "Adopt NScreenPlay" or "Refactor tests to NScreenPlay", agents should follow:

Analyze
-> Plan
-> Human Approval
-> Apply
-> Validate

Required behavior:

1. Call `nscreenplay_analyze_project` first.
2. Call `nscreenplay_create_adoption_plan` second.
3. Present the plan and request explicit approval.
4. Call `nscreenplay_apply_adoption_plan` only after approval.
5. Report validation results and manual build/test steps.
