using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.ProjectAnalysis;

namespace NScreenplay.Mcp.Tests;

public class AdoptionPlannerTests
{
    private readonly AdoptionPlanner _planner = new();

    [Fact]
    public void Plan_XunitPlaywright_NoNscreenplay_Recommended()
    {
        var analysis = Analysis("xunit", null, "playwright", false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Contains("NScreenplay.Core", plan.RecommendedPackages);
        Assert.Contains("NScreenplay.Playwright", plan.RecommendedPackages);
        Assert.Contains(plan.Steps, s => s.Id == "introduce-core");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-playwright");
        Assert.Equal("medium", plan.EstimatedComplexity);
    }

    [Fact]
    public void Plan_ReqnrollPlaywright_PreservesReqnroll()
    {
        var analysis = Analysis("xunit", "reqnroll", "playwright", false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Contains("NScreenplay.Reqnroll", plan.RecommendedPackages);
        Assert.DoesNotContain(plan.Steps, s => s.Title.Contains("BDDfy", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.Warnings, w => w.Contains("BDDfy", StringComparison.OrdinalIgnoreCase) && w.Contains("replace", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_Bddfy_DoesNotSubstituteReqnroll()
    {
        var analysis = Analysis("xunit", "bddfy", null, false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Equal("bddfy", plan.CurrentState.BddFramework);
        Assert.Contains(plan.Warnings, w => w.Contains("BDDfy", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(plan.Steps, s => s.Id == "preserve-bddfy");
        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Reqnroll");
        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.BDDfy");
        Assert.Contains(plan.Warnings, w => w.Contains("No official NScreenplay BDDfy adapter exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_AlreadyAdopted_DoesNotRecommendDuplicates()
    {
        var analysis = Analysis("xunit", "reqnroll", "playwright", true, true, true, true, false, true, false);
        var plan = _planner.Create(analysis);

        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Core");
        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Playwright");
        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Reqnroll");
        Assert.Contains(plan.Steps, s => s.Id == "architecture-review");
        Assert.Equal("low", plan.EstimatedComplexity);
    }

    [Fact]
    public void Plan_PartialAdoption_OnlyAddsMissingPackages()
    {
        var analysis = Analysis("xunit", null, "playwright", false, true, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Core");
        Assert.Contains(plan.RecommendedPackages, p => p == "NScreenplay.Playwright");
        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Mcp");
        Assert.DoesNotContain(plan.Steps, s => s.Id == "introduce-core");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-playwright");
    }

    [Fact]
    public void Plan_XunitPlaywright_NoNscreenplay_ContainsArchitectureAndMigrationSteps()
    {
        var analysis = Analysis("xunit", null, "playwright", false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Contains(plan.Steps, s => s.Id == "introduce-core");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-playwright");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-actor-lifecycle");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-ability");
        Assert.Contains(plan.Steps, s => s.Id == "organize-tasks-interactions");
        Assert.Contains(plan.Steps, s => s.Id == "separate-questions");
        Assert.Contains(plan.Steps, s => s.Id == "introduce-targets");
        Assert.Contains(plan.Steps, s => s.Id == "review-direct-playwright");
        Assert.Contains(plan.Steps, s => s.Id == "validate-migration");
    }

    [Fact]
    public void Plan_ManualScreenplay_UsesReviewSteps()
    {
        var analysis = Analysis("xunit", null, null, true, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Contains(plan.Steps, s => s.Id == "map-screenplay");
        Assert.Contains(plan.Risks, r => r.Contains("Manual Screenplay-like", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_ApiOnly_DoesNotRecommendPlaywright()
    {
        var analysis = Analysis("xunit", null, null, false, false, false, false, true, false, false);
        var plan = _planner.Create(analysis);

        Assert.DoesNotContain(plan.RecommendedPackages, p => p == "NScreenplay.Playwright");
        Assert.DoesNotContain(plan.Steps, s => s.Category == "playwright");
        Assert.Contains(plan.Steps, s => s.Category == "api");
        Assert.Contains(plan.Steps, s => s.Id == "manual-api-pattern");
        Assert.Contains(plan.Warnings, w => w.Contains("No official NScreenplay API package exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Plan_UnknownProject_IsLowComplexityOrPossible()
    {
        var analysis = Analysis(null, null, null, false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.Equal("low", plan.EstimatedComplexity);
        Assert.NotNull(plan.PreservationRules);
    }

    [Fact]
    public void Plan_MultipleFrameworks_HighComplexity()
    {
        var analysis = Analysis("xunit", "reqnroll", "playwright", false, false, false, false, true, false, false);
        var plan = _planner.Create(analysis);

        Assert.Equal("high", plan.EstimatedComplexity);
    }

    [Fact]
    public void Plan_ReturnsStableStepIds()
    {
        var analysis = Analysis("xunit", null, "playwright", false, false, false, false, false, false, false);
        var plan = _planner.Create(analysis);

        Assert.All(plan.Steps, step => Assert.False(string.IsNullOrWhiteSpace(step.Id)));
        Assert.Equal(plan.Steps.Select(s => s.Id).Distinct().Count(), plan.Steps.Count);
    }

    private static ProjectAnalysisResult Analysis(
        string? testFramework,
        string? bddFramework,
        string? browserAutomation,
        bool screenplayDetected,
        bool core,
        bool playwright,
        bool reqnroll,
        bool apiTesting,
        bool mcp,
        bool warnings)
    {
        return new ProjectAnalysisResult(
            ProjectPath: "C:/Projects/MyTests",
            ProjectType: "dotnet-test",
            Language: "C#",
            TargetFrameworks: ["net8.0"],
            TestFramework: testFramework,
            BddFramework: bddFramework,
            BrowserAutomation: browserAutomation,
            ApiTesting: apiTesting,
            NScreenplay: new NScreenplayPackagePresence(core, playwright, reqnroll, mcp),
            ScreenplayDetected: screenplayDetected,
            ScreenplayDetectionEvidence: screenplayDetected ? ["Found Screenplay signal: AttemptsTo("] : [],
            RecommendedPackages: [],
            RecommendedSkills: [],
            AdoptionLevel: DetermineAdoptionLevel(core, playwright, reqnroll, screenplayDetected, browserAutomation, bddFramework, apiTesting),
            MigrationPlan: [],
            Warnings: warnings ? ["warning"] : [],
            Evidence: []);
    }

    private static string DetermineAdoptionLevel(bool core, bool playwright, bool reqnroll, bool screenplayDetected, string? browserAutomation, string? bddFramework, bool apiTesting)
    {
        if (core && (playwright || browserAutomation == "playwright") && (reqnroll || bddFramework == "reqnroll" || bddFramework is null))
            return screenplayDetected ? "already-adopted" : "partially-adopted";
        if (screenplayDetected)
            return "partially-adopted";
        if (browserAutomation is not null || bddFramework is not null || apiTesting)
            return "recommended";
        return "possible";
    }
}