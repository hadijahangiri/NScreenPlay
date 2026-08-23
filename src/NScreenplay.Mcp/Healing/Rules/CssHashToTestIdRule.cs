using NScreenplay.Mcp.Healing.Models;
using NScreenplay.Mcp.Models;
using System.Text.RegularExpressions;

namespace NScreenplay.Mcp.Healing.Rules;

/// <summary>
/// H-01: Target uses ByCss("#id") — a fragile ID-based CSS selector.
/// Proposes replacing it with ByTestId("id") which is more stable.
/// Only applies when the selector is a simple #id with no spaces or combinators.
/// </summary>
public sealed class CssHashToTestIdRule : HealingRule
{
    public override string Id => "H-01";
    public override string Description => "Replace fragile ByCss(\"#id\") with stable ByTestId(\"id\").";

    // Matches: .ByCss("#someId") or .ByCss("#some-id") — no spaces, simple IDs only
    private static readonly Regex CssHashPattern =
        new(@"\.ByCss\(""#([\w-]+)""\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public override FixProposal? Evaluate(HealingContext context)
    {
        var match = CssHashPattern.Match(context.FileContent);
        if (!match.Success) return null;

        // Find all occurrences for this specific target
        var targetName = context.Target.Name;
        if (!context.FileContent.Contains(targetName, StringComparison.Ordinal)) return null;

        var originalCode = match.Value;
        var elementId = match.Groups[1].Value;
        var proposedCode = $".ByTestId(\"{elementId}\")";

        if (originalCode == proposedCode) return null;

        var diff = FixProposal.GenerateDiff(context.FilePath, originalCode, proposedCode);

        return new FixProposal
        {
            Id = NewId(),
            RuleId = Id,
            Category = "SelectorObsolete",
            Confidence = 0.85,
            Summary = $"Replace ByCss(\"#{elementId}\") with ByTestId(\"{elementId}\")",
            Reason = $"CSS ID selectors (#id) are fragile and break when HTML restructuring occurs. " +
                     $"ByTestId uses data-testid attributes which are stable automation contracts.",
            Evidence = $"Found ByCss(\"#{elementId}\") in {context.FilePath}. " +
                       $"ByTestId(\"{elementId}\") provides equivalent, more stable location.",
            FilePath = context.FilePath,
            Location = new CodeLocation(FindLineNumber(context.FileContent, match.Index), 0,
                context.Target.DeclaringType, context.Target.Name),
            OriginalCode = originalCode,
            ProposedCode = proposedCode,
            Diff = diff,
            Risk = ProposalRisk.Low,
            ValidationPlan = "dotnet build, then run the specific test using the modified Target.",
            OriginalFileHash = ComputeHash(context.FileContent),
            State = ProposalState.Proposed,
            CreatedAt = DateTimeOffset.UtcNow,
            ProposedAt = DateTimeOffset.UtcNow,
        };
    }

    private static int FindLineNumber(string content, int charIndex)
    {
        var lineNumber = 1;
        for (var i = 0; i < Math.Min(charIndex, content.Length); i++)
            if (content[i] == '\n') lineNumber++;
        return lineNumber;
    }
}
