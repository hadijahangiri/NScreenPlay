# Architecture

## Overview

```mermaid
graph TD
    R[Reqnroll] --> NR[NScreenplay.Reqnroll]
    NR --> C[NScreenplay.Core]
    P[NScreenplay.Playwright] --> C
    M[NScreenplay.Mcp] --> C
    S[skills/\nSKILL.md files] --> M
    L[samples/Login] --> NR
    L --> P
```

## Project Roles

- `NScreenplay.Core` contains the Screenplay abstractions.
- `NScreenplay.Playwright` adapts the abstractions to Playwright.
- `NScreenplay.Reqnroll` manages feature and scenario lifecycles.
- `NScreenplay.Mcp` exposes discovery, analysis, and healing tools.
- `samples/Login` demonstrates the supported flow end to end.

## Limits

The repository is intentionally small and focused. It does not include extra product packages, extra adapter layers, or speculative public APIs beyond what is already in the source tree.
