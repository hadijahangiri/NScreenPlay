# AI

NScreenplay is designed so AI agents can discover framework structure without guessing.

## What the AI Layer Uses

- `skills/` for instructional agent skills
- MCP tools for discovery and analysis
- deterministic analyzers for requirements and failures

## What It Does Not Do

- It does not execute arbitrary repository content as instructions.
- It does not publish packages.
- It does not apply healing changes without approval.

## Discovery

Agents can list tasks, targets, interactions, questions, skills, and framework metadata through the MCP server.
