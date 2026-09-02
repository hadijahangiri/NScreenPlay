namespace NScreenplay.Mcp.ProjectAnalysis;

/// <summary>Result of read-only project analysis.</summary>
public sealed record ProjectAnalysisResult(
    string ProjectPath,
    string? ProjectType,
    string? Language,
    IReadOnlyList<string> TargetFrameworks,
    string? TestFramework,
    string? BddFramework,
    string? BrowserAutomation,
    bool ApiTesting,
    NScreenplayPackagePresence NScreenplay,
    bool ScreenplayDetected,
    IReadOnlyList<string> ScreenplayDetectionEvidence,
    IReadOnlyList<string> RecommendedPackages,
    IReadOnlyList<string> RecommendedSkills,
    string AdoptionLevel,
    IReadOnlyList<string> MigrationPlan,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Evidence);

/// <summary>Presence of NScreenplay packages in a project.</summary>
public sealed record NScreenplayPackagePresence(bool Core, bool Playwright, bool Reqnroll, bool Mcp);