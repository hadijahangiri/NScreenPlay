using NScreenplay.Core;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Models;
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

    public ToolContractTests()
    {
        _tempSkillsDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSkillsDir);
        CreateSkill("screenplay", "# Screenplay Skill\n\nLearn the Screenplay pattern.");
        CreateSkill("playwright", "# Playwright Skill\n\nLearn Playwright integration.");

        var discovery = new ComponentDiscovery([typeof(TestComponents).Assembly]);
        var skillLoader = new SkillLoader(_tempSkillsDir);
        var analyzer = new FailureAnalyzer();
        _tools = new NScreenplayTools(discovery, skillLoader, analyzer);
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

    public void Dispose()
    {
        try { Directory.Delete(_tempSkillsDir, recursive: true); } catch { }
    }

    private void CreateSkill(string name, string content)
    {
        var dir = Path.Combine(_tempSkillsDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);
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
