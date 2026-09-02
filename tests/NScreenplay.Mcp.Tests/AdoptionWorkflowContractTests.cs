using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Resources;
using NScreenplay.Mcp.Tools;
using System.Reflection;
using System.Text.Json;

namespace NScreenplay.Mcp.Tests;

public sealed class AdoptionWorkflowContractTests : IDisposable
{
    private readonly string _tempSkillsDir;
    private readonly string _tempProjectsDir;
    private readonly NScreenplayTools _tools;
    private readonly NScreenplayResources _resources;

    public AdoptionWorkflowContractTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-workflow-skills-{Guid.NewGuid():N}");
        _tempProjectsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-workflow-projects-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSkillsDir);
        Directory.CreateDirectory(_tempProjectsDir);

        CreateSkill("screenplay", "# Screenplay\n");
        CreateSkill("test-authoring", "# Test Authoring\n");

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
    public void WorkflowResource_ExposesCanonicalSequenceAndBoundaries()
    {
        var json = _resources.GetAdoptionWorkflowResource();
        var doc = JsonDocument.Parse(json);

        var workflow = doc.RootElement.GetProperty("workflow");
        Assert.True(workflow.GetArrayLength() >= 6);
        Assert.Equal("Analyze", workflow[0].GetProperty("step").GetString());
        Assert.Equal("Plan", workflow[1].GetProperty("step").GetString());
        Assert.Equal("HumanApproval", workflow[3].GetProperty("step").GetString());
        Assert.Equal("Apply", workflow[4].GetProperty("step").GetString());

        var boundaries = doc.RootElement.GetProperty("requiredBoundaries");
        Assert.True(boundaries.GetProperty("approvalRequiredBeforeApply").GetBoolean());
        Assert.True(boundaries.GetProperty("applyMustUseValidatedPlan").GetBoolean());
        Assert.True(boundaries.GetProperty("noAutonomousAdoptTool").GetBoolean());
    }

    [Fact]
    public void ApplyAdoptionPlan_WithoutValidAnalyzePlanContract_IsRejected()
    {
        var projectDir = CreateProject("workflow-contract");
        var projectPath = Path.Combine(projectDir, "workflow-contract.csproj");

        // Tampered plan: does not match planner output for this project.
        var tampered = new AdoptionPlan(
            ProjectPath: projectPath,
            CurrentState: new AdoptionPlanCurrentState("recommended", "xunit", null, null, false),
            RecommendedPackages: ["NScreenplay.Core", "NScreenplay.Unknown"],
            RecommendedSkills: [new SkillRecommendation("screenplay", "test")],
            Steps: [new AdoptionPlanStep("introduce-core", "Introduce core", "package", "required", "reason", [], ["test project"])],
            Risks: [],
            Warnings: [],
            PreservationRules: ["Preserve the existing test framework."],
            EstimatedComplexity: "medium");

        var tamperedJson = JsonSerializer.Serialize(tampered, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var resultJson = _tools.ApplyAdoptionPlan(projectPath, tamperedJson, dryRun: true);
        var resultDoc = JsonDocument.Parse(resultJson);

        Assert.True(resultDoc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("does not match the current Analyze -> Plan result", error.GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AlreadyAdoptedProject_PlanIndicatesNoPackageChanges()
    {
        var projectDir = CreateProject("already-adopted",
            packageRefs: [
                "<PackageReference Include=\"NScreenplay.Core\" Version=\"0.1.0\" />",
                "<PackageReference Include=\"NScreenplay.Playwright\" Version=\"0.1.0\" />"
            ]);

        var planJson = _tools.CreateAdoptionPlan(projectDir);
        var doc = JsonDocument.Parse(planJson);

        Assert.Equal("already-adopted", doc.RootElement.GetProperty("currentState").GetProperty("adoptionLevel").GetString());
        Assert.Equal(0, doc.RootElement.GetProperty("recommendedPackages").GetArrayLength());
    }

    private string CreateProject(string name, IReadOnlyList<string>? packageRefs = null)
    {
        var dir = Path.Combine(_tempProjectsDir, name);
        Directory.CreateDirectory(dir);

        var packageBlock = packageRefs is null || packageRefs.Count == 0
            ? string.Empty
            : $"<ItemGroup>{string.Join(Environment.NewLine, packageRefs)}</ItemGroup>";

        var csproj = $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework><IsTestProject>true</IsTestProject></PropertyGroup>{packageBlock}</Project>";
        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), csproj);
        File.WriteAllText(Path.Combine(dir, "SampleTests.cs"), "using Xunit; public class SampleTests { [Fact] public void T() { } }");
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
