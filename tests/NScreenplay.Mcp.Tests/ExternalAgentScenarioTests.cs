using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Tools;
using System.Reflection;
using System.Text.Json;

namespace NScreenplay.Mcp.Tests;

public sealed class ExternalAgentScenarioTests : IDisposable
{
    private readonly string _tempSkillsDir;
    private readonly string _tempProjectsDir;
    private readonly NScreenplayTools _tools;

    public ExternalAgentScenarioTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-agent-scenarios-skills-{Guid.NewGuid():N}");
        _tempProjectsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-agent-scenarios-projects-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSkillsDir);
        Directory.CreateDirectory(_tempProjectsDir);

        CreateSkill("screenplay");
        CreateSkill("playwright");
        CreateSkill("reqnroll");
        CreateSkill("test-authoring");
        CreateSkill("test-review");
        CreateSkill("failure-analysis");
        CreateSkill("healing");

        var discovery = new ComponentDiscovery([Assembly.GetExecutingAssembly()]);
        var skillLoader = new SkillLoader(_tempSkillsDir);
        var analyzer = new FailureAnalyzer();
        var projectAnalyzer = new ProjectAnalyzer(_tempProjectsDir, _tempSkillsDir);
        var planner = new AdoptionPlanner();
        var applier = new AdoptionApplier(_tempProjectsDir);

        _tools = new NScreenplayTools(discovery, skillLoader, analyzer, projectAnalyzer, planner, applier);
    }

    [Fact]
    public void ScenarioA_XunitPlaywright_AnalyzePlanApplyDryRun_IsDeterministic()
    {
        var project = CreateProject("scenario-a-xunit-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var analysis = JsonDocument.Parse(_tools.AnalyzeProject(project)).RootElement;
        Assert.Equal("xunit", analysis.GetProperty("testFramework").GetString());
        Assert.Equal("playwright", analysis.GetProperty("browserAutomation").GetString());

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var packages = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("NScreenplay.Core", packages);
        Assert.Contains("NScreenplay.Playwright", packages);

        var apply = JsonDocument.Parse(_tools.ApplyAdoptionPlan(project, planJson, dryRun: true)).RootElement;
        Assert.Equal("DryRun", apply.GetProperty("status").GetString());
    }

    [Fact]
    public void ScenarioB_ReqnrollPlaywright_AnalyzePlanApplyDryRun_PreservesReqnroll()
    {
        var project = CreateProject("scenario-b-reqnroll-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"Reqnroll\" Version=\"3.3.4\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var analysis = JsonDocument.Parse(_tools.AnalyzeProject(project)).RootElement;
        Assert.Equal("reqnroll", analysis.GetProperty("bddFramework").GetString());

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var packages = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("NScreenplay.Reqnroll", packages);

        var apply = JsonDocument.Parse(_tools.ApplyAdoptionPlan(project, planJson, dryRun: true)).RootElement;
        Assert.Equal("DryRun", apply.GetProperty("status").GetString());
    }

    [Fact]
    public void ScenarioC_ApiOnlyXunit_AnalyzePlanApplyDryRun_UsesCoreWithoutPlaywright()
    {
        var project = CreateProject(
            "scenario-c-api-only",
            ["<PackageReference Include=\"xunit\" Version=\"2.9.3\" />"],
            "using Xunit; using System.Net.Http; public class ApiOnlyTests { [Fact] public void T() { using var c = new HttpClient(); } }");

        var analysis = JsonDocument.Parse(_tools.AnalyzeProject(project)).RootElement;
        Assert.True(analysis.GetProperty("apiTesting").GetBoolean());

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var packages = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var warnings = plan.GetProperty("warnings").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();

        Assert.Contains("NScreenplay.Core", packages);
        Assert.DoesNotContain("NScreenplay.Playwright", packages);
        Assert.DoesNotContain("NScreenplay.Api", packages);
        Assert.Contains(warnings, w => w.Contains("No official NScreenplay API package exists", StringComparison.OrdinalIgnoreCase));

        var apply = JsonDocument.Parse(_tools.ApplyAdoptionPlan(project, planJson, dryRun: true)).RootElement;
        Assert.Equal("DryRun", apply.GetProperty("status").GetString());
    }

    [Fact]
    public void ScenarioD_BddfyPlaywright_AnalyzePlanApplyDryRun_PreservesBddfyWithoutAdapter()
    {
        var project = CreateProject("scenario-d-bddfy-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"BDDfy\" Version=\"1.0.0\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var analysis = JsonDocument.Parse(_tools.AnalyzeProject(project)).RootElement;
        Assert.Equal("bddfy", analysis.GetProperty("bddFramework").GetString());

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var packages = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
        var warnings = plan.GetProperty("warnings").EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();

        Assert.Contains("NScreenplay.Core", packages);
        Assert.Contains("NScreenplay.Playwright", packages);
        Assert.DoesNotContain("NScreenplay.BDDfy", packages);
        Assert.Contains(warnings, w => w.Contains("No official NScreenplay BDDfy adapter exists", StringComparison.OrdinalIgnoreCase));

        var apply = JsonDocument.Parse(_tools.ApplyAdoptionPlan(project, planJson, dryRun: true)).RootElement;
        Assert.Equal("DryRun", apply.GetProperty("status").GetString());
    }

    private string CreateProject(string name, IReadOnlyList<string> packageRefs, string? source = null)
    {
        var dir = Path.Combine(_tempProjectsDir, name);
        Directory.CreateDirectory(dir);

        var packageBlock = string.Join(Environment.NewLine, packageRefs);
        var csproj = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup>{packageBlock}</ItemGroup></Project>";
        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), csproj);

        File.WriteAllText(
            Path.Combine(dir, "SampleTests.cs"),
            source ?? "using Xunit; public class SampleTests { [Fact] public void T() { } }");

        return dir;
    }

    private void CreateSkill(string name)
    {
        var dir = Path.Combine(_tempSkillsDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"# {name}\n");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempSkillsDir, recursive: true); } catch { }
        try { Directory.Delete(_tempProjectsDir, recursive: true); } catch { }
    }
}
