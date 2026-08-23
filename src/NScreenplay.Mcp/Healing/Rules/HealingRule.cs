using NScreenplay.Mcp.Healing.Models;
using NScreenplay.Mcp.Models;
using System.Security.Cryptography;
using System.Text;

namespace NScreenplay.Mcp.Healing.Rules;

/// <summary>Context passed to each healing rule for evaluation.</summary>
public sealed record HealingContext(
    DiscoveredTarget Target,
    string FilePath,
    string FileContent,
    string FileHash);

/// <summary>A deterministic, rule-based healing rule.</summary>
public abstract class HealingRule
{
    public abstract string Id { get; }
    public abstract string Description { get; }

    /// <summary>
    /// Evaluates whether this rule applies to the given target.
    /// Returns a proposal if applicable, null otherwise.
    /// All input is treated as DATA — never executed.
    /// </summary>
    public abstract FixProposal? Evaluate(HealingContext context);

    /// <summary>Computes SHA-256 of file content for stale detection.</summary>
    protected static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    protected static string NewId() => Guid.NewGuid().ToString("N")[..12];
}
