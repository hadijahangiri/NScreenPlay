using NScreenplay.Core;
using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Tools;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace NScreenplay.Mcp.Tests;

/// <summary>
/// Contract tests: verify the AI-facing tool API returns consistent, structured data.
/// These tests do NOT require an actual MCP client or LLM.
/// </summary>
public class ToolContractTests : IDisposable
{
    private readonly NScreenplayTools _tools;
    private readonly string _tempSkillsDir;
    private readonly string _tempProjectsDir;
    private readonly AdoptionPlanner _planner = new();

    public ToolContractTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-contract-{Guid.NewGuid():N}");
        _tempProjectsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-project-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSkillsDir);
        Directory.CreateDirectory(_tempProjectsDir);
        CreateSkill("screenplay", "# Screenplay Skill\n\nLearn the Screenplay pattern.");
        CreateSkill("playwright", "# Playwright Skill\n\nLearn Playwright integration.");

        var discovery = new ComponentDiscovery([typeof(TestComponents).Assembly]);
        var skillLoader = new SkillLoader(_tempSkillsDir);
        var analyzer = new FailureAnalyzer();
        var projectAnalyzer = new ProjectAnalyzer(_tempProjectsDir, _tempSkillsDir);
        var applier = new AdoptionApplier(_tempProjectsDir);
        _tools = new NScreenplayTools(discovery, skillLoader, analyzer, projectAnalyzer, _planner, applier);
    }

    [Fact]
    public void GetFrameworkInfo_ReturnsValidJson()
    {
        var json = _tools.GetFrameworkInfo();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("version", out _));
        Assert.True(doc.RootElement.TryGetProperty("modules", out _));
        Assert.True(doc.RootElement.TryGetProperty("capabilities", out _));
    }

    [Fact]
    public void ListTasks_WhenTaskExists_ReturnsDiscoverableTask()
    {
        var json = _tools.ListTasks();
        var tasks = JsonSerializer.Deserialize<List<DiscoveredTask>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(tasks);
        Assert.Contains(tasks, t => t.Name == "ContractTestTask");
    }

    [Fact]
    public void ListTargets_WhenTargetExists_ReturnsDiscoverableTarget()
    {
        var json = _tools.ListTargets();
        var targets = JsonSerializer.Deserialize<List<DiscoveredTarget>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(targets);
        Assert.Contains(targets, t => t.Name == "ContractButton");
    }

    [Fact]
    public void ListSkills_ReturnsBothSkills()
    {
        var json = _tools.ListSkills();
        var skills = JsonSerializer.Deserialize<List<SkillInfo>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(skills);
        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "screenplay");
    }

    [Fact]
    public void GetSkill_ValidName_ReturnsContent()
    {
        var json = _tools.GetSkill("screenplay");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("content", out var content));
        Assert.Contains("Screenplay Skill", content.GetString());
    }

    [Fact]
    public void GetSkill_InvalidName_ReturnsErrorJson()
    {
        var json = _tools.GetSkill("../../../etc/passwd");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void GetSkill_UnknownName_ReturnsErrorJson()
    {
        var json = _tools.GetSkill("does-not-exist");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void AnalyzeFailure_SelectorFailure_ReturnsCategoryAndInvestigation()
    {
        var json = _tools.AnalyzeFailure(
            scenarioTitle: "Login scenario",
            stepText: "When the user clicks the login button",
            exceptionType: "PlaywrightException",
            exceptionMessage: "Timeout waiting for locator('data-testid=login-btn') to be visible.");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("category", out var cat));
        Assert.Equal("SelectorFailure", cat.GetString());
        Assert.True(doc.RootElement.TryGetProperty("recommendedInvestigation", out _));
    }

    [Fact]
    public void AnalyzeFailure_AlwaysReturnsDoNotDoList()
    {
        var json = _tools.AnalyzeFailure("s", "s", "SomeException", "some message");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("doNotDo", out var doNotDo));
        Assert.True(doNotDo.GetArrayLength() > 0);
    }

    [Fact]
    public void AnalyzeProject_RegisteredAndReturnsExpectedContract()
    {
        var projectPath = CreateProject("AnalyzeProject", "xunit", "Microsoft.Playwright", "using Xunit; using Microsoft.Playwright; [Fact] public void Test() { }");

        var json = _tools.AnalyzeProject(projectPath);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("projectPath", out _));
        Assert.True(doc.RootElement.TryGetProperty("testFramework", out var testFramework));
        Assert.Equal("xunit", testFramework.GetString());
        Assert.True(doc.RootElement.TryGetProperty("browserAutomation", out var browserAutomation));
        Assert.Equal("playwright", browserAutomation.GetString());
        Assert.True(doc.RootElement.TryGetProperty("recommendedPackages", out _));
        Assert.True(doc.RootElement.TryGetProperty("recommendedSkills", out _));
    }

    [Fact]
    public void AnalyzeProject_InvalidPath_ReturnsSafeError()
    {
        var json = _tools.AnalyzeProject("../../../etc/passwd");
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void AnalyzeProject_DoesNotMutateProjectDirectory()
    {
        var projectPath = CreateProject("NoMutation", "xunit", null, "using Xunit; [Fact] public void Test() { }");
        var before = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();

        _ = _tools.AnalyzeProject(projectPath);

        var after = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();
        Assert.Equal(before, after);
    }

    [Fact]
    public void CreateAdoptionPlan_ReturnsStructuredPlan()
    {
        var projectPath = CreateProject("PlanProject", "xunit", "Microsoft.Playwright", "using Xunit; using Microsoft.Playwright; [Fact] public void Test() { }");
        var analysis = JsonDocument.Parse(_tools.AnalyzeProject(projectPath)).RootElement;
        var plan = _planner.Create(new ProjectAnalysisResult(
            analysis.GetProperty("projectPath").GetString()!,
            analysis.GetProperty("projectType").GetString(),
            analysis.GetProperty("language").GetString(),
            ["net10.0"],
            analysis.GetProperty("testFramework").GetString(),
            analysis.GetProperty("bddFramework").ValueKind == JsonValueKind.Null ? null : analysis.GetProperty("bddFramework").GetString(),
            analysis.GetProperty("browserAutomation").GetString(),
            analysis.GetProperty("apiTesting").GetBoolean(),
            new NScreenplayPackagePresence(false, false, false, false),
            analysis.GetProperty("screenplayDetected").GetBoolean(),
            [],
            [],
            [],
            analysis.GetProperty("adoptionLevel").GetString()!,
            [],
            [],
            []));

        Assert.Equal(projectPath, plan.ProjectPath);
        Assert.NotEmpty(plan.RecommendedPackages);
        Assert.NotEmpty(plan.RecommendedSkills);
        Assert.NotEmpty(plan.Steps);
    }

    [Fact]
    public void ApplyAdoptionPlan_EmptyPayload_ReturnsErrorJson()
    {
        var projectPath = CreateProject("ApplyToolEmptyPayload", "xunit", null, "using Xunit; [Fact] public void Test() { }");

        var json = _tools.ApplyAdoptionPlan(projectPath, string.Empty, dryRun: true);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void ApplyAdoptionPlan_MalformedPayload_ReturnsErrorJson()
    {
        var projectPath = CreateProject("ApplyToolMalformedPayload", "xunit", null, "using Xunit; [Fact] public void Test() { }");

        var json = _tools.ApplyAdoptionPlan(projectPath, "{ not-valid-json", dryRun: true);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public void ApplyAdoptionPlan_TamperedPlan_IsRejectedByAnalyzePlanIntegrityGate()
    {
        var projectPath = CreateProject("ApplyToolTamperedPlan", "xunit", null, "using Xunit; [Fact] public void Test() { }");

        var analysisJson = _tools.AnalyzeProject(projectPath);
        var analysis = JsonSerializer.Deserialize<ProjectAnalysisResult>(analysisJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
        var plan = _planner.Create(analysis);
        var tampered = plan with { RecommendedPackages = ["NScreenplay.Core", "NScreenplay.Unknown"] };
        var tamperedJson = JsonSerializer.Serialize(tampered, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var json = _tools.ApplyAdoptionPlan(projectPath, tamperedJson, dryRun: true);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("does not match the current Analyze -> Plan result", error.GetString(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempSkillsDir, recursive: true); } catch { }
        try { Directory.Delete(_tempProjectsDir, recursive: true); } catch { }
    }

    private void CreateSkill(string name, string content)
    {
        var dir = Path.Combine(_tempSkillsDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);
    }

    private string CreateProject(string name, string? frameworks, string? packages, string content)
    {
        var dir = Path.Combine(_tempProjectsDir, name);
        Directory.CreateDirectory(dir);
        var tf = frameworks is null ? "<TargetFramework>net10.0</TargetFramework>" : $"<TargetFrameworks>{frameworks}</TargetFrameworks>";
        var pkgRefs = packages is null ? string.Empty : string.Join(Environment.NewLine, packages.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(p => $"    <PackageReference Include=\"{p}\" Version=\"1.0.0\" />"));
        File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{tf}<IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup>{pkgRefs}</ItemGroup></Project>");
        File.WriteAllText(Path.Combine(dir, "Sample.cs"), content);
        return dir;
    }

    // ── Test components discoverable by ComponentDiscovery ───────────────────

    public sealed class ContractTestTask : ITask
    {
        public Task PerformAs(Actor actor, CancellationToken ct = default) => Task.CompletedTask;
    }

    public static class TestPageObjects
    {
        public static Target ContractButton = Target.The("contract test button").ByTestId("contract-btn");
    }
}

internal static class TestComponents { }
