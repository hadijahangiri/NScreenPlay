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

## Scope

The MCP server is for discovery, analysis, planning, and approval-gated healing. It does not execute shell commands or modify code automatically.
