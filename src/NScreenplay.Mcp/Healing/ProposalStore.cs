using NScreenplay.Mcp.Healing.Models;
using System.Collections.Concurrent;

namespace NScreenplay.Mcp.Healing;

/// <summary>
/// Thread-safe in-memory store for fix proposals and audit entries.
/// Proposals are immutable records; transitions produce new versions.
/// </summary>
public sealed class ProposalStore
{
    private readonly ConcurrentDictionary<string, FixProposal> _proposals = new();
    private readonly ConcurrentBag<AuditEntry> _auditLog = [];

    /// <summary>Stores a new proposal. Throws if a proposal with the same ID already exists.</summary>
    public void Add(FixProposal proposal)
    {
        if (!_proposals.TryAdd(proposal.Id, proposal))
            throw new InvalidOperationException($"A proposal with ID '{proposal.Id}' already exists.");
        Audit(proposal.Id, "Created", "system", $"Rule {proposal.RuleId}, State {proposal.State}");
    }

    /// <summary>Retrieves a proposal by ID. Returns null if not found.</summary>
    public FixProposal? Get(string id) =>
        _proposals.TryGetValue(id, out var p) ? p : null;

    /// <summary>Returns all proposals ordered by creation time.</summary>
    public IReadOnlyList<FixProposal> GetAll() =>
        [.. _proposals.Values.OrderByDescending(p => p.CreatedAt)];

    /// <summary>Returns proposals in the given state.</summary>
    public IReadOnlyList<FixProposal> GetByState(ProposalState state) =>
        [.. _proposals.Values.Where(p => p.State == state).OrderByDescending(p => p.CreatedAt)];

    /// <summary>
    /// Updates a proposal to its new transitioned state.
    /// Throws if the proposal does not exist or transition is invalid.
    /// </summary>
    public FixProposal Transition(string id, ProposalState targetState, string actor, string? detail = null)
    {
        if (!_proposals.TryGetValue(id, out var current))
            throw new KeyNotFoundException($"Proposal '{id}' not found.");

        var updated = ProposalStateMachine.Transition(current, targetState);
        _proposals[id] = updated;
        Audit(id, $"StateChange:{current.State}→{targetState}", actor, detail);
        return updated;
    }

    /// <summary>Replaces a proposal with a new version (e.g. after setting rollback content).</summary>
    internal void Update(FixProposal updated)
    {
        if (!_proposals.ContainsKey(updated.Id))
            throw new KeyNotFoundException($"Proposal '{updated.Id}' not found.");
        _proposals[updated.Id] = updated;
    }

    /// <summary>Returns the full audit log ordered by timestamp.</summary>
    public IReadOnlyList<AuditEntry> GetAuditLog(string? proposalId = null)
    {
        var entries = _auditLog.OrderBy(e => e.Timestamp);
        return proposalId is null
            ? [.. entries]
            : [.. entries.Where(e => e.ProposalId == proposalId)];
    }

    private void Audit(string proposalId, string action, string actor, string? detail)
    {
        // Never log secrets — caller is responsible for sanitizing detail
        _auditLog.Add(new AuditEntry(DateTimeOffset.UtcNow, proposalId, action, actor, detail));
    }
}
