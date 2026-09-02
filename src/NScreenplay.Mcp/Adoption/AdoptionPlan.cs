namespace NScreenplay.Mcp.Adoption;

/// <summary>Structured, deterministic migration plan for a discovered project.</summary>
public sealed record AdoptionPlan(
    string ProjectPath,
    AdoptionPlanCurrentState CurrentState,
    IReadOnlyList<string> RecommendedPackages,
    IReadOnlyList<SkillRecommendation> RecommendedSkills,
    IReadOnlyList<AdoptionPlanStep> Steps,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> PreservationRules,
    string EstimatedComplexity);

/// <summary>Current analyzed state used to drive the plan.</summary>
public sealed record AdoptionPlanCurrentState(
    string? AdoptionLevel,
    string? TestFramework,
    string? BddFramework,
    string? BrowserAutomation,
    bool ApiTesting);

/// <summary>Why a recommended skill is relevant.</summary>
public sealed record SkillRecommendation(string Name, string Reason);