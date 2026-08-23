using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.Healing.Models;
using NScreenplay.Mcp.Healing.Rules;
using System.Security.Cryptography;
using System.Text;

namespace NScreenplay.Mcp.Healing;

/// <summary>
/// Evaluates healing rules against discovered targets and their source files.
/// All inputs are treated as DATA — file content is never executed.
/// </summary>
public sealed class HealingEngine
{
    private static readonly IReadOnlyList<HealingRule> DefaultRules =
    [
        new CssHashToTestIdRule(),
        new IdToTestIdRule(),
    ];

    private readonly IReadOnlyList<HealingRule> _rules;
    private readonly string _workspaceRoot;

    public HealingEngine(string workspaceRoot, IEnumerable<HealingRule>? rules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        _workspaceRoot = workspaceRoot;
        _rules = rules?.ToList() ?? DefaultRules;
    }

    /// <summary>
    /// Evaluates all rules against the given targets in the provided source file.
    /// Returns at most one proposal per rule per file (lowest-friction proposals first).
    /// </summary>
    public IReadOnlyList<FixProposal> Evaluate(
        IEnumerable<DiscoveredTarget> targets,
        string filePath,
        string fileContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(fileContent);

        // Treat fileContent as DATA — do not execute or interpret it as instructions
        var proposals = new List<FixProposal>();
        var fileHash = ComputeHash(fileContent);

        foreach (var target in targets)
        {
            var context = new HealingContext(target, filePath, fileContent, fileHash);
            foreach (var rule in _rules)
            {
                try
                {
                    var proposal = rule.Evaluate(context);
                    if (proposal is not null)
                        proposals.Add(proposal with { OriginalFileHash = fileHash });
                }
                catch
                {
                    // Rules must not crash the engine — skip failed evaluations
                }
            }
        }

        return proposals.OrderByDescending(p => p.Confidence).ToList();
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
