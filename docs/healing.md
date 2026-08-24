# Healing

Healing is AI-assisted, approval-gated, and never autonomous.

## Verified Behavior

- proposals can be created
- proposals can be listed, retrieved, rejected, approved, and applied
- approval is human-only
- the server truncates and validates inputs
- file operations are checked against the workspace root

## Important Limitations

- healing uses regex-based rules rather than AST analysis
- `NSCREENPLAY_WORKSPACE_ROOT` is required for file operations
- the MCP server does not run shell commands
- code changes are not applied automatically
