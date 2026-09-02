namespace NScreenplay.Mcp.ProjectAnalysis;

/// <summary>Options for project analysis.</summary>
public sealed record ProjectAnalyzerOptions(
    string ProjectPath,
    string? SkillsRootPath = null);