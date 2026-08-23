# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 0.1.x   | ✅ Active  |
| < 0.1   | ❌ None    |

NScreenplay is in early development (v0.1.0). Only the current release receives security fixes.

## Reporting a Vulnerability

⚠️ **Do not open a public GitHub issue for security vulnerabilities.**

> **[DECISION REQUIRED]** A private reporting email or GitHub private security advisory must be configured before public release.
>
> Until then: open a GitHub private security advisory using the "Security" tab in this repository.

Please include:

- Description of the vulnerability
- Steps to reproduce
- Affected version(s)
- Potential impact
- Any suggested mitigation

## Response Timeline

We will acknowledge receipt within 7 days and provide an initial assessment within 14 days.

## Scope

### In scope

- Authentication bypass in MCP tools
- Path traversal in healing/file operations
- Arbitrary code execution via MCP inputs
- Secret exposure in logs or outputs
- Approval boundary bypass in healing workflow
- Prompt injection vulnerabilities

### Out of scope

- Vulnerabilities in the underlying frameworks (Playwright, Reqnroll, .NET) — report directly to those projects
- Issues requiring physical access to the machine
- Social engineering

## Disclosure Policy

We follow **responsible disclosure**. Please allow us reasonable time to address the issue before public disclosure.
