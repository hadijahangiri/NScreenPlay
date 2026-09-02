using NScreenplay.Mcp.AI;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Resources;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Tools;
using System.Reflection;
using System.Text.Json;

namespace NScreenplay.Mcp.Tests;

public sealed class DiscoverabilityAuditTests : IDisposable
{
    private readonly string _tempSkillsDir;
    private readonly string _tempProjectsDir;
    private readonly NScreenplayTools _tools;
    private readonly NScreenplayResources _resources;

    public DiscoverabilityAuditTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-discovery-skills-{Guid.NewGuid():N}");
        _tempProjectsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-discovery-projects-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSkillsDir);
        Directory.CreateDirectory(_tempProjectsDir);

        CreateSkill("screenplay", "# Screenplay\n");
        CreateSkill("playwright", "# Playwright\n");
        CreateSkill("reqnroll", "# Reqnroll\n");
        CreateSkill("test-authoring", "# Test Authoring\n");

        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var skillLoader = new SkillLoader(_tempSkillsDir);
        var analyzer = new FailureAnalyzer();
        var projectAnalyzer = new ProjectAnalyzer(_tempProjectsDir, _tempSkillsDir);
        var planner = new AdoptionPlanner();
        var applier = new AdoptionApplier(_tempProjectsDir);

        _tools = new NScreenplayTools(discovery, skillLoader, analyzer, projectAnalyzer, planner, applier);
        _resources = new NScreenplayResources(discovery, skillLoader, new AiContextBuilder(discovery, skillLoader));
    }

    [Fact]
    public void FrameworkResource_ExposesAdoptionAndSafetyBoundaries()
    {
        var json = _resources.GetFrameworkResource();
        var doc = JsonDocument.Parse(json);

        var canDo = doc.RootElement.GetProperty("approvalBoundary").GetProperty("aiCanDo")
            .EnumerateArray().Select(x => x.GetString()).ToList();
        var cannotDo = doc.RootElement.GetProperty("approvalBoundary").GetProperty("aiCannotDo")
            .EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Contains("ANALYZE", canDo);
        Assert.Contains("PLAN", canDo);
        Assert.Contains("APPLY_APPROVED_PLAN", canDo);

        Assert.Contains("EXECUTE_SHELL", cannotDo);
        Assert.Contains("EXECUTE_POWERSHELL", cannotDo);
    }

    [Fact]
    public void AdoptionWorkflowResource_ExposesAnalyzePlanApplyOrder()
    {
        var json = _resources.GetAdoptionWorkflowResource();
        var doc = JsonDocument.Parse(json);

        var workflow = doc.RootElement.GetProperty("workflow");
        Assert.Equal("nscreenplay_analyze_project", workflow[0].GetProperty("requiredTool").GetString());
        Assert.Equal("nscreenplay_create_adoption_plan", workflow[1].GetProperty("requiredTool").GetString());
        Assert.Equal("nscreenplay_apply_adoption_plan", workflow[4].GetProperty("requiredTool").GetString());
    }

    [Fact]
    public void AnalyzeAndPlan_XunitPlaywright_RecommendCoreAndPlaywrightWithoutReqnroll()
    {
        var project = CreateProject("xunit-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;

        var recommended = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("NScreenplay.Core", recommended);
        Assert.Contains("NScreenplay.Playwright", recommended);
        Assert.DoesNotContain("NScreenplay.Reqnroll", recommended);
    }

    [Fact]
    public void AnalyzeAndPlan_ReqnrollPlaywright_PreservesReqnrollAndRecommendsReqnrollIntegration()
    {
        var project = CreateProject("reqnroll-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"Reqnroll\" Version=\"3.3.4\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var recommended = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Contains("NScreenplay.Reqnroll", recommended);
    }

    [Fact]
    public void AnalyzeAndPlan_ApiOnly_DoesNotIntroducePlaywright()
    {
        var project = CreateProject("api-only", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />"
        ],
        "using Xunit; using System.Net.Http; public class ApiTests { [Fact] public void T() { using var c = new HttpClient(); } }");

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var recommended = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var warnings = plan.GetProperty("warnings").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();

        Assert.DoesNotContain("NScreenplay.Playwright", recommended);
        Assert.DoesNotContain("NScreenplay.Mcp", recommended);
        Assert.Contains(warnings, w => w.Contains("No official NScreenplay API package exists", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeAndPlan_BddfyPlaywright_PreservesBddfyAndRejectsAdapterRecommendation()
    {
        var project = CreateProject("bddfy-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"BDDfy\" Version=\"1.0.0\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var recommended = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var warnings = plan.GetProperty("warnings").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var steps = plan.GetProperty("steps").EnumerateArray().Select(x => x.GetProperty("id").GetString() ?? string.Empty).ToList();

        Assert.Contains("NScreenplay.Core", recommended);
        Assert.Contains("NScreenplay.Playwright", recommended);
        Assert.DoesNotContain("NScreenplay.BDDfy", recommended);
        Assert.Contains(steps, s => s == "preserve-bddfy");
        Assert.Contains(warnings, w => w.Contains("No official NScreenplay BDDfy adapter exists", StringComparison.OrdinalIgnoreCase));
    }

    private string CreateProject(string name, IReadOnlyList<string> packageRefs, string? testCode = null)
    {
        var dir = Path.Combine(_tempProjectsDir, name);
        Directory.CreateDirectory(dir);
        var packageBlock = string.Join(Environment.NewLine, packageRefs);
        var csproj = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup>{packageBlock}</ItemGroup></Project>";

        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), csproj);
        File.WriteAllText(Path.Combine(dir, "SampleTests.cs"), testCode ?? "using Xunit; using Microsoft.Playwright; public class SampleTests { [Fact] public void T() { } }");
        return dir;
    }

    private void CreateSkill(string name, string content)
    {
        var dir = Path.Combine(_tempSkillsDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempSkillsDir, recursive: true); } catch { }
        try { Directory.Delete(_tempProjectsDir, recursive: true); } catch { }
    }
}