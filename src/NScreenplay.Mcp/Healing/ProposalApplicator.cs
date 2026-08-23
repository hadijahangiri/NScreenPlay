using NScreenplay.Mcp.Healing.Models;
using System.Security.Cryptography;
using System.Text;

namespace NScreenplay.Mcp.Healing;

/// <summary>
/// Applies approved fix proposals to disk — with safety checks and rollback capture.
/// Only proposals in the <see cref="ProposalState.Approved"/> state may be applied.
/// </summary>
public sealed class ProposalApplicator
{
    private readonly FileSafetyValidator _safety;
    private readonly ProposalStore _store;

    public ProposalApplicator(FileSafetyValidator safety, ProposalStore store)
    {
        _safety = safety;
        _store = store;
    }

    /// <summary>
    /// Applies the approved proposal to disk.
    /// Pre-conditions: proposal must be Approved; file must not be stale.
    /// Post-conditions: proposal state transitions to Applied; rollback content is captured.
    /// </summary>
    public FixProposal Apply(string proposalId, string actor)
    {
        var proposal = _store.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal '{proposalId}' not found.");

        if (proposal.State != ProposalState.Approved)
            throw new InvalidOperationException(
                $"Cannot apply proposal '{proposalId}' in state {proposal.State}. Must be Approved.");

        // Path safety
        _safety.ValidateWritePath(proposal.FilePath);
        var fullPath = _safety.Resolve(proposal.FilePath);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Target file not found: {proposal.FilePath}");

        var currentContent = File.ReadAllText(fullPath);
        var currentHash = ComputeHash(currentContent);

        // Stale detection: file changed since proposal was created
        if (!string.Equals(currentHash, proposal.OriginalFileHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Proposal '{proposalId}' is stale: the target file has been modified since the proposal was created. " +
                "Reject this proposal and create a new one against the current file.");

        // Verify original code is still present exactly
        if (!currentContent.Contains(proposal.OriginalCode, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Proposal '{proposalId}' is stale: the original code pattern no longer exists in the file.");

        // Capture rollback content BEFORE writing
        var withRollback = proposal with { RollbackContent = currentContent };
        _store.Update(withRollback);

        // Apply: atomic write via temp file + move
        var newContent = currentContent.Replace(proposal.OriginalCode, proposal.ProposedCode, StringComparison.Ordinal);
        var tempPath = fullPath + ".nscreenplay.tmp";
        try
        {
            File.WriteAllText(tempPath, newContent, Encoding.UTF8);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }

        // Transition to Applied
        return _store.Transition(proposalId, ProposalState.Applied, actor,
            $"Applied to {proposal.FilePath}");
    }

    /// <summary>
    /// Rolls back an applied proposal by restoring the original file content.
    /// Only allowed when the proposal has RollbackContent.
    /// </summary>
    public void Rollback(string proposalId, string actor)
    {
        var proposal = _store.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal '{proposalId}' not found.");

        if (proposal.RollbackContent is null)
            throw new InvalidOperationException($"No rollback content available for proposal '{proposalId}'.");

        _safety.ValidateWritePath(proposal.FilePath);
        var fullPath = _safety.Resolve(proposal.FilePath);

        var tempPath = fullPath + ".nscreenplay.rollback.tmp";
        try
        {
            File.WriteAllText(tempPath, proposal.RollbackContent, Encoding.UTF8);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }

        // Transition to ValidationFailed so the proposal can be re-evaluated
        _store.Transition(proposalId, ProposalState.ValidationFailed, actor, "Rolled back");
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
