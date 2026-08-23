using NScreenplay.Mcp.Healing.Models;

namespace NScreenplay.Mcp.Healing;

/// <summary>
/// Enforces valid state transitions for fix proposals.
/// Invalid transitions throw <see cref="InvalidOperationException"/>.
/// </summary>
public static class ProposalStateMachine
{
    // Valid transitions: key = current state, value = allowed next states
    private static readonly IReadOnlyDictionary<ProposalState, ProposalState[]> AllowedTransitions =
        new Dictionary<ProposalState, ProposalState[]>
        {
            [ProposalState.Draft]            = [ProposalState.Proposed],
            [ProposalState.Proposed]         = [ProposalState.Approved, ProposalState.Rejected],
            [ProposalState.Approved]         = [ProposalState.Applied],
            [ProposalState.Rejected]         = [],  // terminal
            [ProposalState.Applied]          = [ProposalState.Validated, ProposalState.ValidationFailed],
            [ProposalState.Validated]        = [],  // terminal
            [ProposalState.ValidationFailed] = [ProposalState.Proposed], // can be re-proposed after rollback
        };

    /// <summary>
    /// Returns the proposal with its state transitioned.
    /// Throws <see cref="InvalidOperationException"/> for invalid transitions.
    /// </summary>
    public static FixProposal Transition(FixProposal proposal, ProposalState target)
    {
        if (!AllowedTransitions.TryGetValue(proposal.State, out var allowed) ||
            !allowed.Contains(target))
        {
            throw new InvalidOperationException(
                $"Cannot transition proposal '{proposal.Id}' from {proposal.State} to {target}. " +
                $"Allowed: [{string.Join(", ", AllowedTransitions.GetValueOrDefault(proposal.State, []))}].");
        }

        var now = DateTimeOffset.UtcNow;
        return proposal with
        {
            State = target,
            ProposedAt  = target == ProposalState.Proposed  ? now : proposal.ProposedAt,
            ApprovedAt  = target == ProposalState.Approved  ? now : proposal.ApprovedAt,
            RejectedAt  = target == ProposalState.Rejected  ? now : proposal.RejectedAt,
            AppliedAt   = target == ProposalState.Applied   ? now : proposal.AppliedAt,
        };
    }

    /// <summary>Returns whether a transition from <paramref name="from"/> to <paramref name="to"/> is valid.</summary>
    public static bool IsValidTransition(ProposalState from, ProposalState to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>Returns all allowed next states from the given state.</summary>
    public static IReadOnlyList<ProposalState> AllowedFrom(ProposalState state) =>
        AllowedTransitions.TryGetValue(state, out var allowed) ? allowed : [];
}
