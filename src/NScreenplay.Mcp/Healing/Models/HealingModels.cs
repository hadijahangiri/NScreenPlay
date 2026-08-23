namespace NScreenplay.Mcp.Healing.Models;

/// <summary>State machine states for a healing fix proposal.</summary>
public enum ProposalState
{
    /// <summary>Being constructed — not yet visible to reviewers.</summary>
    Draft,
    /// <summary>Submitted for human review.</summary>
    Proposed,
    /// <summary>Explicitly approved by a human reviewer.</summary>
    Approved,
    /// <summary>Explicitly rejected by a human reviewer.</summary>
    Rejected,
    /// <summary>File change has been applied to disk.</summary>
    Applied,
    /// <summary>Post-apply validation passed.</summary>
    Validated,
    /// <summary>Post-apply validation failed; rollback required.</summary>
    ValidationFailed,
}

/// <summary>Risk level of applying a fix proposal.</summary>
public enum ProposalRisk { Low, Medium, High }

/// <summary>A code location within a source file.</summary>
public sealed record CodeLocation(int Line, int Column, string? ContainingType, string? ContainingMember);

/// <summary>
/// Result of validating a proposal's current state:
/// whether the target file has changed since the proposal was created.
/// </summary>
public enum StalenessStatus { Fresh, Stale }

/// <summary>Record of a significant event in a proposal's lifecycle.</summary>
public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    string ProposalId,
    string Action,
    string Actor,
    string? Detail);

/// <summary>Result captured when post-apply validation runs.</summary>
public sealed record ValidationResult(
    bool Passed,
    string Command,
    string Output,
    TimeSpan Duration);
