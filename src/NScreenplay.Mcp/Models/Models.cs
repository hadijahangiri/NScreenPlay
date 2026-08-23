namespace NScreenplay.Mcp.Models;

/// <summary>A discovered Task type.</summary>
public sealed record DiscoveredTask(
    string Name,
    string FullTypeName,
    string Assembly,
    string? Description);

/// <summary>A discovered Target field.</summary>
public sealed record DiscoveredTarget(
    string Name,
    string HumanReadableName,
    string DeclaringType,
    IReadOnlyList<DiscoveredStrategy> Strategies);

/// <summary>One locator strategy on a Target.</summary>
public sealed record DiscoveredStrategy(string Kind, string Value, string? Qualifier);

/// <summary>A discovered Interaction type.</summary>
public sealed record DiscoveredInteraction(
    string Name,
    string FullTypeName,
    string Assembly);

/// <summary>A discovered Question type (IQuestion&lt;T&gt;).</summary>
public sealed record DiscoveredQuestion(
    string Name,
    string FullTypeName,
    string AnswerType,
    string Assembly);

/// <summary>Metadata about a skill SKILL.md file.</summary>
public sealed record SkillInfo(
    string Name,
    string FilePath,
    string? FirstHeading);

/// <summary>Full content of a loaded skill.</summary>
public sealed record SkillContent(
    string Name,
    string FilePath,
    string Content);

/// <summary>Input to the failure analysis tool.</summary>
public sealed record FailureInput(
    string ScenarioTitle,
    string StepText,
    string? TaskName,
    string? InteractionName,
    string? TargetName,
    string? PageUrl,
    string ExceptionType,
    string ExceptionMessage,
    string? StackTraceSummary,
    bool ScreenshotAvailable);

/// <summary>Result of the failure analysis tool.</summary>
public sealed record FailureAnalysisResult(
    string Category,
    ConfidenceLevel Confidence,
    string Evidence,
    string ProbableCause,
    string RecommendedInvestigation,
    IReadOnlyList<string> DoNotDo);

/// <summary>Confidence level for AI analysis results.</summary>
public enum ConfidenceLevel { High, Medium, Low }

/// <summary>Top-level framework info returned by the info tool.</summary>
public sealed record FrameworkInfo(
    string Version,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Capabilities,
    string McpServerVersion);
