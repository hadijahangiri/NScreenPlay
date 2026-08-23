using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.Planning;

namespace NScreenplay.Mcp.Tests;

public class RequirementAnalyzerTests
{
    private readonly RequirementAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_LoginRequirement_DetectsLoginBehavior()
    {
        var result = _analyzer.Analyze("User logs in with valid credentials and sees dashboard");
        Assert.Contains("login", result.DetectedBehaviors);
    }

    [Fact]
    public void Analyze_LoginRequirement_DetectsUserActor()
    {
        var result = _analyzer.Analyze("A user logs in with valid credentials");
        Assert.Contains("user", result.DetectedActors);
    }

    [Fact]
    public void Analyze_LoginRequirement_DetectsValidPrecondition()
    {
        var result = _analyzer.Analyze("User logs in with valid credentials");
        Assert.Contains(result.DetectedPreconditions, p => p.Contains("valid credentials", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_LoginRequirement_DetectsDashboardOutcome()
    {
        var result = _analyzer.Analyze("User logs in and sees the dashboard");
        Assert.Contains(result.DetectedOutcomes, o => o.Contains("dashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_HighConfidenceForClearRequirement()
    {
        var result = _analyzer.Analyze("A user with valid credentials logs in and sees the dashboard");
        Assert.Equal(AnalysisConfidence.High, result.Confidence);
    }

    [Fact]
    public void Analyze_LowConfidenceForVagueRequirement()
    {
        var result = _analyzer.Analyze("something happens");
        Assert.NotEqual(AnalysisConfidence.High, result.Confidence);
    }

    [Fact]
    public void Analyze_DetectsAmbiguityWhenNoBehavior()
    {
        var result = _analyzer.Analyze("the sky is blue");
        Assert.NotEmpty(result.Ambiguities);
    }

    [Fact]
    public void Analyze_ThrowsForNullRequirement()
    {
        Assert.Throws<ArgumentNullException>(() => _analyzer.Analyze(null!));
    }

    [Fact]
    public void Analyze_ThrowsForEmptyRequirement()
    {
        Assert.Throws<ArgumentException>(() => _analyzer.Analyze(""));
    }

    [Fact]
    public void Analyze_PreservesOriginalRequirement()
    {
        const string req = "User logs in with valid credentials";
        var result = _analyzer.Analyze(req);
        Assert.Equal(req, result.OriginalRequirement);
    }

    [Fact]
    public void Analyze_SearchRequirement_DetectsSearchBehavior()
    {
        var result = _analyzer.Analyze("User searches for a product by keyword");
        Assert.Contains("search", result.DetectedBehaviors);
    }

    [Fact]
    public void Analyze_SearchWithoutTerm_FlagsMissingInfo()
    {
        var result = _analyzer.Analyze("User searches for something");
        Assert.NotEmpty(result.MissingInformation);
    }
}
