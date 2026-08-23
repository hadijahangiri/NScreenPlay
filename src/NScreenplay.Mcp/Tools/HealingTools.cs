using ModelContextProtocol.Server;
using NScreenplay.Mcp.Healing;
using NScreenplay.Mcp.Healing.Models;
using NScreenplay.Mcp.Security;
using System.ComponentModel;
using System.Text.Json;

namespace NScreenplay.Mcp.Tools;

/// <summary>
/// Controlled healing MCP tools.
///
/// SECURITY MODEL:
/// - Default mode: READ-ONLY
/// - Proposal creation: ALLOWED (by AI or human)
/// - Proposal approval: HUMAN ONLY — document this constraint to operators
/// - Proposal application: ONLY after explicit approval
/// - Automatic healing: DISABLED — always requires human approval
///
/// APPROVAL BOUNDARY:
/// The AI must NOT approve its own proposals.
/// In production, configure the server so that nscreenplay_approve_fix_proposal
/// is only accessible through a human-facing channel (not the AI agent's MCP session).
/// </summary>
[McpServerToolType]
public sealed class HealingTools
{
    private readonly ProposalStore _store;
    private readonly ProposalApplicator _applicator;
    private readonly FileSafetyValidator _safety;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public HealingTools(ProposalStore store, ProposalApplicator applicator, FileSafetyValidator safety)
    {
        _store = store;
        _applicator = applicator;
        _safety = safety;
    }

    [McpServerTool(Name = "nscreenplay_get_fix_proposal")]
    [Description("Retrieves a fix proposal by ID. Returns its current state, diff, and evidence.")]
    public string GetFixProposal(
        [Description("The proposal ID")] string proposalId)
    {
        var proposal = _store.Get(InputValidator.Truncate(proposalId, 50));
        if (proposal is null)
            return JsonSerializer.Serialize(new { error = $"Proposal '{proposalId}' not found." }, JsonOpts);
        return JsonSerializer.Serialize(proposal, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_list_fix_proposals")]
    [Description("Lists all fix proposals, optionally filtered by state.")]
    public string ListFixProposals(
        [Description("Filter by state: Draft, Proposed, Approved, Rejected, Applied, Validated, ValidationFailed. Empty = all.")] string state = "")
    {
        IReadOnlyList<FixProposal> proposals = string.IsNullOrWhiteSpace(state)
            ? _store.GetAll()
            : Enum.TryParse<ProposalState>(state, ignoreCase: true, out var parsed)
                ? _store.GetByState(parsed)
                : [];

        return JsonSerializer.Serialize(proposals.Select(p => new
        {
            p.Id,
            p.RuleId,
            p.State,
            p.Summary,
            p.Confidence,
            p.Risk,
            p.FilePath,
            p.CreatedAt
        }), JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_reject_fix_proposal")]
    [Description("Rejects a fix proposal. Rejected proposals cannot be applied. Provide a reason.")]
    public string RejectFixProposal(
        [Description("The proposal ID to reject")] string proposalId,
        [Description("Who is rejecting (e.g. developer name)")] string rejectedBy,
        [Description("Reason for rejection")] string reason)
    {
        var safeId = InputValidator.Truncate(proposalId, 50);
        var safeBy = InputValidator.Truncate(rejectedBy, 100);
        var safeReason = InputValidator.Truncate(reason, 500);

        try
        {
            var proposal = _store.Get(safeId)
                ?? throw new KeyNotFoundException($"Proposal '{safeId}' not found.");

            var rejected = ProposalStateMachine.Transition(proposal, ProposalState.Rejected);
            var withReason = rejected with { RejectedBy = safeBy, RejectionReason = safeReason, RejectedAt = DateTimeOffset.UtcNow };
            _store.Update(withReason);
            _store.GetAuditLog(); // ensure audit flush

            return JsonSerializer.Serialize(new { success = true, proposalId = safeId, state = ProposalState.Rejected }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool(Name = "nscreenplay_approve_fix_proposal")]
    [Description(
        "HUMAN-ONLY TOOL: Approves a fix proposal for application. " +
        "AI agents must NOT call this tool. " +
        "In production, configure this tool to be accessible only through a human-facing interface. " +
        "Requires explicit approver identification.")]
    public string ApproveFixProposal(
        [Description("The proposal ID to approve")] string proposalId,
        [Description("The human approver's name or identifier (must not be 'ai', 'agent', or 'system')")] string approvedBy)
    {
        var safeId = InputValidator.Truncate(proposalId, 50);
        var safeBy = InputValidator.Truncate(approvedBy, 100);

        // Enforce human-only: reject auto-approval attempts
        if (IsAiIdentity(safeBy))
            return JsonSerializer.Serialize(new
            {
                error = "Approval must be performed by a human. AI agents cannot approve their own proposals. " +
                        "Provide the human reviewer's name."
            }, JsonOpts);

        try
        {
            var updated = _store.Transition(safeId, ProposalState.Approved, safeBy);
            var withApprover = updated with { ApprovedBy = safeBy, ApprovedAt = DateTimeOffset.UtcNow };
            _store.Update(withApprover);

            return JsonSerializer.Serialize(new
            {
                success = true,
                proposalId = safeId,
                state = ProposalState.Approved,
                approvedBy = safeBy,
                message = "Proposal approved. Call nscreenplay_apply_fix_proposal to apply the change."
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool(Name = "nscreenplay_apply_fix_proposal")]
    [Description("Applies an approved fix proposal to the target file. Only works on Approved proposals. Captures rollback content before writing.")]
    public string ApplyFixProposal(
        [Description("The proposal ID to apply")] string proposalId,
        [Description("Who is applying (for audit trail)")] string appliedBy)
    {
        var safeId = InputValidator.Truncate(proposalId, 50);
        var safeBy = InputValidator.Truncate(appliedBy, 100);

        try
        {
            var applied = _applicator.Apply(safeId, safeBy);
            return JsonSerializer.Serialize(new
            {
                success = true,
                proposalId = safeId,
                state = applied.State,
                message = "Fix applied. Run dotnet build and the relevant tests to validate.",
                validationPlan = applied.ValidationPlan
            }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool(Name = "nscreenplay_get_audit_log")]
    [Description("Returns the audit trail for all or a specific proposal.")]
    public string GetAuditLog(
        [Description("Proposal ID to filter by, or empty for all entries")] string proposalId = "")
    {
        var log = string.IsNullOrWhiteSpace(proposalId)
            ? _store.GetAuditLog()
            : _store.GetAuditLog(InputValidator.Truncate(proposalId, 50));

        return JsonSerializer.Serialize(log, JsonOpts);
    }

    /// <summary>Checks if an identity string looks like an AI/automated identity.</summary>
    private static bool IsAiIdentity(string identity)
    {
        var lower = identity.ToLowerInvariant();
        return lower is "ai" or "agent" or "system" or "bot" or "automation" or "gpt" or "claude" or "copilot"
            || lower.Contains("ai", StringComparison.Ordinal)
            || lower.Contains("agent", StringComparison.Ordinal)
            || lower.Contains("bot", StringComparison.Ordinal);
    }
}
