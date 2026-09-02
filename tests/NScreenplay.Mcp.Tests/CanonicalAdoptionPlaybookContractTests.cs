using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Resources;
using NScreenplay.Mcp.Tools;
using System.Reflection;
using System.Text.Json;

namespace NScreenplay.Mcp.Tests;

public sealed class CanonicalAdoptionPlaybookContractTests : IDisposable
{
    private readonly string _tempSkillsDir;
    private readonly string _tempProjectsDir;
    private readonly NScreenplayTools _tools;
    private readonly NScreenplayResources _resources;

    public CanonicalAdoptionPlaybookContractTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-canonical-skills-{Guid.NewGuid():N}");
        _tempProjectsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-canonical-projects-{Guid.NewGuid():N}");
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
        _resources = new NScreenplayResources(discovery, skillLoader, new NScreenplay.Mcp.AI.AiContextBuilder(discovery, skillLoader));
    }

    [Fact]
    public void CanonicalPlaybook_FileExists_AndMentionsRealToolNames()
    {
        var root = FindRepositoryRoot();
        var docPath = Path.Combine(root, "docs", "external-agent-adoption.md");
        Assert.True(File.Exists(docPath), "Canonical playbook document must exist.");

        var text = File.ReadAllText(docPath);

        Assert.Contains("nscreenplay_analyze_project", text, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_create_adoption_plan", text, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_apply_adoption_plan", text, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_analyze_failure", text, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_get_failure_context", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalPlaybook_ListsOnlyRealAdoptionPackages_AndExplicitlyRejectsFakeOnes()
    {
        var text = ReadCanonicalPlaybook();

        Assert.Contains("NScreenplay.Core", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.Playwright", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.Reqnroll", text, StringComparison.Ordinal);

        Assert.Contains("No official package or adapter exists", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NScreenplay.Api", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.BDDfy", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FrameworkAndWorkflowResources_MatchCanonicalContract()
    {
        var framework = JsonDocument.Parse(_resources.GetFrameworkResource()).RootElement;
        var officialPackages = framework.GetProperty("officialPackages").EnumerateArray().Select(x => x.GetString()).ToList();
        var manualPatterns = framework.GetProperty("manualPatterns").EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Contains("NScreenplay.Core", officialPackages);
        Assert.Contains("NScreenplay.Playwright", officialPackages);
        Assert.Contains("NScreenplay.Reqnroll", officialPackages);
        Assert.DoesNotContain("NScreenplay.Api", officialPackages);
        Assert.DoesNotContain("NScreenplay.BDDfy", officialPackages);

        Assert.Contains(manualPatterns, p => p is not null && p.Contains("no official NScreenplay.Api package", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(manualPatterns, p => p is not null && p.Contains("no official NScreenplay.BDDfy adapter", StringComparison.OrdinalIgnoreCase));

        var workflow = JsonDocument.Parse(_resources.GetAdoptionWorkflowResource()).RootElement;
        var steps = workflow.GetProperty("workflow").EnumerateArray().ToList();
        Assert.Equal("nscreenplay_analyze_project", steps[0].GetProperty("requiredTool").GetString());
        Assert.Equal("nscreenplay_create_adoption_plan", steps[1].GetProperty("requiredTool").GetString());
        Assert.Equal("nscreenplay_apply_adoption_plan", steps[4].GetProperty("requiredTool").GetString());

        var boundaries = workflow.GetProperty("requiredBoundaries");
        Assert.True(boundaries.GetProperty("approvalRequiredBeforeApply").GetBoolean());
        Assert.True(boundaries.GetProperty("applyMustUseValidatedPlan").GetBoolean());
        Assert.True(boundaries.GetProperty("noAutonomousAdoptTool").GetBoolean());
    }

    [Fact]
    public void AnalyzerPlannerAndApplyContract_SupportsExternalAgentPathWithoutHallucinations()
    {
        var project = CreateProject("xunit-playwright", [
            "<PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
            "<PackageReference Include=\"Microsoft.Playwright\" Version=\"1.49.0\" />"
        ]);

        var analyzeJson = _tools.AnalyzeProject(project);
        var analysis = JsonDocument.Parse(analyzeJson).RootElement;
        Assert.Equal("xunit", analysis.GetProperty("testFramework").GetString());
        Assert.Equal("playwright", analysis.GetProperty("browserAutomation").GetString());

        var planJson = _tools.CreateAdoptionPlan(project);
        var plan = JsonDocument.Parse(planJson).RootElement;
        var packages = plan.GetProperty("recommendedPackages").EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Contains("NScreenplay.Core", packages);
        Assert.Contains("NScreenplay.Playwright", packages);
        Assert.DoesNotContain("NScreenplay.Mcp", packages);
        Assert.DoesNotContain("NScreenplay.Api", packages);
        Assert.DoesNotContain("NScreenplay.BDDfy", packages);

        var applyDryRunJson = _tools.ApplyAdoptionPlan(project, planJson, dryRun: true);
        var apply = JsonDocument.Parse(applyDryRunJson).RootElement;
        Assert.Equal("DryRun", apply.GetProperty("status").GetString());
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", ".."));
            if (File.Exists(Path.Combine(root, "NScreenplay.sln")))
                return root;

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.Ordinal))
                break;
            current = parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static string ReadCanonicalPlaybook()
    {
        var path = Path.Combine(FindRepositoryRoot(), "docs", "external-agent-adoption.md");
        return File.ReadAllText(path);
    }

    private string CreateProject(string name, IReadOnlyList<string> packageRefs)
    {
        var dir = Path.Combine(_tempProjectsDir, name);
        Directory.CreateDirectory(dir);
        var packageBlock = string.Join(Environment.NewLine, packageRefs);
        var csproj = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup>{packageBlock}</ItemGroup></Project>";
        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), csproj);
        File.WriteAllText(Path.Combine(dir, "SampleTests.cs"), "using Xunit; using Microsoft.Playwright; public class SampleTests { [Fact] public void T() { } }");
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
