using NScreenplay.Mcp.Healing.Models;

namespace NScreenplay.Mcp.Healing.Models;

/// <summary>
/// A fully specified healing fix proposal.
/// Immutable — transitions produce new instances via <c>With</c> mutations.
/// </summary>
public sealed record FixProposal
{
    /// <summary>Unique identifier (GUID).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The healing rule that generated this proposal (e.g. H-01).</summary>
    public string RuleId { get; init; } = string.Empty;

    /// <summary>Human-readable category (e.g. "SelectorObsolete").</summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>Confidence 0.0–1.0.</summary>
    public double Confidence { get; init; }

    /// <summary>One-line summary of what this proposal does.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Why this change is proposed.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Evidence supporting the proposal (selector analysis, etc.).</summary>
    public string Evidence { get; init; } = string.Empty;

    /// <summary>Workspace-relative path to the file to be modified.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Location within the file.</summary>
    public CodeLocation? Location { get; init; }

    /// <summary>The code before the change.</summary>
    public string OriginalCode { get; init; } = string.Empty;

    /// <summary>The code after the change.</summary>
    public string ProposedCode { get; init; } = string.Empty;

    /// <summary>Unified diff representation of the change.</summary>
    public string Diff { get; init; } = string.Empty;

    /// <summary>Risk of applying the change.</summary>
    public ProposalRisk Risk { get; init; }

    /// <summary>What to run after applying to validate the fix.</summary>
    public string ValidationPlan { get; init; } = string.Empty;

    /// <summary>Always true — every proposal requires explicit human approval.</summary>
    public bool RequiresApproval => true;

    /// <summary>SHA-256 hash of the file at proposal creation time, for stale detection.</summary>
    public string OriginalFileHash { get; init; } = string.Empty;

    // ── Lifecycle fields ──────────────────────────────────────────────────────

    public ProposalState State { get; init; } = ProposalState.Draft;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProposedAt { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTimeOffset? RejectedAt { get; init; }
    public string? RejectedBy { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset? AppliedAt { get; init; }
    public string? RollbackContent { get; init; }
    public ValidationResult? PostApplyValidation { get; init; }

    /// <summary>Generates a human-readable unified diff string.</summary>
    public static string GenerateDiff(string filePath, string originalCode, string proposedCode)
    {
        var origLines = originalCode.Split('\n');
        var propLines = proposedCode.Split('\n');
        return $"--- {filePath}\n+++ {filePath}\n"
             + string.Join("\n",
                 origLines.Select(l => "- " + l)
                 .Concat(propLines.Select(l => "+ " + l)));
    }
}
