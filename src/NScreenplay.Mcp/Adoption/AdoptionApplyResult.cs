namespace NScreenplay.Mcp.Adoption;

/// <summary>Outcome of a plan-driven adoption apply operation.</summary>
public sealed record AdoptionApplyResult(
    string Status,
    IReadOnlyList<string> AppliedOperations,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    string? ProjectPath);