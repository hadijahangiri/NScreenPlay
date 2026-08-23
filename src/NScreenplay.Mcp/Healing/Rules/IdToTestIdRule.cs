using NScreenplay.Mcp.Healing.Models;
using NScreenplay.Mcp.Models;
using System.Text.RegularExpressions;

namespace NScreenplay.Mcp.Healing.Rules;

/// <summary>
/// H-02: Target uses ById("id") when ByTestId("id") would be more robust.
/// ById resolves via CSS #id which fails if the element is inside a Shadow DOM or renamed.
/// ByTestId uses data-testid which is an explicit automation contract.
/// Only proposed when confidence is reasonable — does not blindly substitute.
/// </summary>
public sealed class IdToTestIdRule : HealingRule
{
    public override string Id => "H-02";
    public override string Description => "Replace ById(\"id\") with ByTestId(\"id\") for improved stability.";

    private static readonly Regex ByIdPattern =
        new(@"\.ById\(""([\w-]+)""\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public override FixProposal? Evaluate(HealingContext context)
    {
        var match = ByIdPattern.Match(context.FileContent);
        if (!match.Success) return null;

        if (!context.FileContent.Contains(context.Target.Name, StringComparison.Ordinal)) return null;

        var elementId = match.Groups[1].Value;
        var originalCode = match.Value;
        var proposedCode = $".ByTestId(\"{elementId}\")";

        if (originalCode == proposedCode) return null;

        return new FixProposal
        {
            Id = NewId(),
            RuleId = Id,
            Category = "SelectorSuboptimal",
            Confidence = 0.70,
            Summary = $"Replace ById(\"{elementId}\") with ByTestId(\"{elementId}\")",
            Reason = "ById resolves via the HTML id attribute which can change during refactoring. " +
                     "ByTestId is an explicit automation contract less likely to change.",
            Evidence = $"Target uses ById(\"{elementId}\"). If the element has a matching data-testid " +
                       $"attribute, ByTestId provides equivalent, more stable location.",
            FilePath = context.FilePath,
            OriginalCode = originalCode,
            ProposedCode = proposedCode,
            Diff = FixProposal.GenerateDiff(context.FilePath, originalCode, proposedCode),
            Risk = ProposalRisk.Low,
            ValidationPlan = "dotnet build, verify the element has data-testid attribute, run affected test.",
            OriginalFileHash = ComputeHash(context.FileContent),
            State = ProposalState.Proposed,
            CreatedAt = DateTimeOffset.UtcNow,
            ProposedAt = DateTimeOffset.UtcNow,
        };
    }
}
