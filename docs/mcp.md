# MCP

NScreenplay includes an MCP server in `src/NScreenplay.Mcp`.

## Run

```bash
dotnet run --project src/NScreenplay.Mcp
```

## Actual Tools

- `nscreenplay_get_framework_info`
- `nscreenplay_list_tasks`
- `nscreenplay_list_targets`
- `nscreenplay_list_interactions`
- `nscreenplay_list_questions`
- `nscreenplay_list_skills`
- `nscreenplay_get_skill`
- `nscreenplay_analyze_failure`
- `nscreenplay_analyze_requirement`
- `nscreenplay_create_test_plan`
- `nscreenplay_get_failure_context`
- `nscreenplay_get_fix_proposal`
- `nscreenplay_list_fix_proposals`
- `nscreenplay_reject_fix_proposal`
- `nscreenplay_approve_fix_proposal`
- `nscreenplay_apply_fix_proposal`
- `nscreenplay_get_audit_log`
- `nscreenplay_analyze_project`
- `nscreenplay_create_adoption_plan`
- `nscreenplay_apply_adoption_plan`

## Adoption Workflow For AI Agents

Canonical workflow:

Analyze
-> Plan
-> Human Approval
-> Apply
-> Validate
-> Final Report

Required MCP calls:

1. `nscreenplay_analyze_project`
2. `nscreenplay_create_adoption_plan`
3. `nscreenplay_apply_adoption_plan` (only after explicit human approval)

The MCP server does not orchestrate this workflow autonomously. The AI agent orchestrates it.

## Workflow Resource

- `nscreenplay://adoption-workflow`

This resource exposes machine-readable workflow order, safety boundaries, failure handling, and validation guidance for AI agents.

## Scope

The MCP server is for discovery, analysis, planning, approval-gated healing, and explicit plan-driven adoption apply.

The server does not execute shell commands, PowerShell, arbitrary scripts, or automatic migrations.

See [project-analysis.md](project-analysis.md) for the Phase A-D adoption contracts.
