namespace NScreenplay.Mcp.Adoption;

/// <summary>One deterministic migration step in the adoption plan.</summary>
public sealed record AdoptionPlanStep(
    string Id,
    string Title,
    string Category,
    string Priority,
    string Reason,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<string> AffectedAreas);