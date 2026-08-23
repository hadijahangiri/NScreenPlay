namespace NScreenplay.Mcp.Models;

/// <summary>
/// Rich context captured when a test scenario fails.
/// Consumed by the AI failure analysis layer — no modification capability.
/// </summary>
public sealed record FailureContext(
    string ScenarioTitle,
    string FeatureTitle,
    string StepText,
    string? TaskName,
    string? InteractionName,
    string? TargetName,
    string? PageUrl,
    string ExceptionType,
    string ExceptionMessage,
    string? StackTraceSummary,
    FailureEvidence? Evidence,
    DateTimeOffset Timestamp);

/// <summary>
/// Evidence collected at failure time. Carries only metadata/paths, never raw browser objects.
/// </summary>
public sealed record FailureEvidence(
    string? ScreenshotPath,
    string? TraceArchivePath,
    bool ScreenshotAvailable,
    bool TraceAvailable);

/// <summary>Enhanced failure analysis result with remediation candidates.</summary>
public sealed record EnhancedFailureAnalysis(
    string Category,
    ConfidenceLevel Confidence,
    string Evidence,
    string ProbableCause,
    string RecommendedInvestigation,
    IReadOnlyList<RemediationCandidate> RemediationCandidates,
    IReadOnlyList<string> DoNotDo);

/// <summary>A concrete, human-reviewable remediation candidate.</summary>
public sealed record RemediationCandidate(
    string Description,
    string TargetFile,
    string ProposedChange,
    bool RequiresHumanApproval);
