# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x | ✅ Active |
| Older versions | ❌ None |

## Security Model

NScreenplay treats MCP and healing inputs as untrusted data.

Documented protections in the current implementation:

- MCP input is truncated before logging or analysis.
- Skill names are validated before loading.
- Healing paths are checked against the configured workspace root.
- The MCP server does not execute shell commands.
- The MCP server does not automatically modify repository files.
- Healing requires explicit human approval before application.

These controls reduce risk, but they are not a guarantee that the system is secure in every deployment.

## Reporting a Vulnerability

Do not open a public issue for security reports.

Use GitHub private security advisories for:

- path traversal concerns
- approval-bypass concerns
- secret leakage concerns
- unintended code execution concerns

Include reproduction steps, affected version, and the expected impact.
