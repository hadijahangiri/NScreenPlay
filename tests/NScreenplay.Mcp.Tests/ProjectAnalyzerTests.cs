using NScreenplay.Mcp.ProjectAnalysis;

namespace NScreenplay.Mcp.Tests;

public class ProjectAnalyzerTests : IDisposable
{
    private readonly string _root;
    private readonly string _skills;
    private readonly ProjectAnalyzer _analyzer;

    public ProjectAnalyzerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"nscreenplay-project-analyzer-{Guid.NewGuid():N}");
        _skills = Path.Combine(_root, "skills");
        Directory.CreateDirectory(_skills);
        CreateSkill("screenplay");
        CreateSkill("playwright");
        CreateSkill("reqnroll");
        CreateSkill("test-authoring");
        CreateSkill("test-review");
        _analyzer = new ProjectAnalyzer(_root, _skills);
    }

    [Fact]
    public void Analyze_XunitPlaywright_ProjectIsRecommended()
    {
        var project = CreateProject("XunitPlaywright", "xunit", "Microsoft.Playwright", "using Microsoft.Playwright;\nusing Xunit;\npublic class Sample { [Fact] public void Test() { var page = default(IPage); } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Equal("dotnet-test", result.ProjectType);
        Assert.Equal("C#", result.Language);
        Assert.Equal("xunit", result.TestFramework);
        Assert.Equal("playwright", result.BrowserAutomation);
        Assert.Equal("recommended", result.AdoptionLevel);
        Assert.Contains("NScreenplay.Playwright", result.RecommendedPackages);
        Assert.Contains("screenplay", result.RecommendedSkills);
        Assert.Contains("playwright", result.RecommendedSkills);
    }

    [Fact]
    public void Analyze_NunitSelenium_ProjectIsRecommended()
    {
        var project = CreateProject("NunitSelenium", "NUnit", "Selenium.WebDriver", "using NUnit.Framework;\nusing OpenQA.Selenium;\n[TestFixture] public class Sample { [Test] public void Test() { IWebDriver? driver = null; } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Equal("nunit", result.TestFramework);
        Assert.Equal("selenium", result.BrowserAutomation);
        Assert.Equal("recommended", result.AdoptionLevel);
        Assert.Contains("test-authoring", result.RecommendedSkills);
        Assert.DoesNotContain("NScreenplay.Playwright", result.RecommendedPackages);
    }

    [Fact]
    public void Analyze_ReqnrollPlaywright_RecognizesIntegration()
    {
        var project = CreateProject("ReqnrollPlaywright", "Reqnroll", "Microsoft.Playwright", "using Reqnroll;\nusing Microsoft.Playwright;\n[Binding] public class Steps { }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Equal("reqnroll", result.BddFramework);
        Assert.Equal("playwright", result.BrowserAutomation);
        Assert.Contains("reqnroll", result.RecommendedSkills);
        Assert.Contains("NScreenplay.Reqnroll", result.RecommendedPackages);
    }

    [Fact]
    public void Analyze_BddfyProject_RecognizesBddfy()
    {
        var project = CreateProject("Bddfy", "xunit", "BDDfy", "using Xunit;\nusing BDDfy;\npublic class Sample { [Fact] public void Test() { } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Equal("bddfy", result.BddFramework);
        Assert.Contains("test-authoring", result.RecommendedSkills);
    }

    [Fact]
    public void Analyze_ExistingNscreenplay_ProjectIsAlreadyAdopted()
    {
        var project = CreateProject("NScreenplayExisting", "xunit", "NScreenplay.Core;NScreenplay.Playwright;NScreenplay.Reqnroll", "using NScreenplay.Core; using NScreenplay.Playwright; using NScreenplay.Reqnroll; using Xunit; public class Sample { [Fact] public void Test() { var actor = Actor.Named(\"Alice\"); actor.Can(BrowseTheWeb.Using(default!)); actor.AttemptsTo(Navigate.To(\"https://example.com\")); } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.True(result.NScreenplay.Core);
        Assert.True(result.NScreenplay.Playwright);
        Assert.True(result.NScreenplay.Reqnroll);
        Assert.Equal("already-adopted", result.AdoptionLevel);
        Assert.Empty(result.MigrationPlan);
    }

    [Fact]
    public void Analyze_ManualScreenplayLike_ProjectIsPartiallyAdopted()
    {
        var project = CreateProject("ManualScreenplay", "xunit", null, "public sealed class Actor { public Task AttemptsTo(object p) => Task.CompletedTask; } public interface ITask { } public class LoginTask : ITask { }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.True(result.ScreenplayDetected);
        Assert.Equal("partially-adopted", result.AdoptionLevel);
        Assert.Contains(result.ScreenplayDetectionEvidence, e => e.Contains("AttemptsTo", StringComparison.OrdinalIgnoreCase) || e.Contains("ITask", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ApiOnly_ProjectDetectsApiTesting()
    {
        var project = CreateProject("ApiOnly", "xunit", null, "using System.Net.Http;\nusing Xunit;\npublic class Sample { [Fact] public void Test() { var client = new HttpClient(); } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.True(result.ApiTesting);
        Assert.Contains("test-review", result.RecommendedSkills);
        Assert.Equal("recommended", result.AdoptionLevel);
        Assert.DoesNotContain("NScreenplay.Mcp", result.RecommendedPackages);
    }

    [Fact]
    public void Analyze_UnknownProject_ReturnsPossible()
    {
        var project = CreateProject("UnknownProject", null, null, "public class PlainClass { }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Equal("possible", result.AdoptionLevel);
        Assert.Null(result.TestFramework);
        Assert.Null(result.BddFramework);
        Assert.Null(result.BrowserAutomation);
        Assert.Empty(result.RecommendedPackages);
    }

    [Fact]
    public void Analyze_DoesNotRecommendMcpPackageForAdoption()
    {
        var project = CreateProject("NoNscreenplayYet", "xunit", "Microsoft.Playwright", "using Xunit; using Microsoft.Playwright; public class Sample { [Fact] public void Test() { } }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.DoesNotContain("NScreenplay.Mcp", result.RecommendedPackages);
    }

    [Fact]
    public void Analyze_InvalidPath_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => _analyzer.Analyze(new ProjectAnalyzerOptions(Path.Combine(_root, "missing"))));
    }

    [Fact]
    public void Analyze_PathTraversal_RejectsOutsideWorkspace()
    {
        var outside = Path.GetFullPath(Path.Combine(_root, "..", "..", "windows"));
        Assert.Throws<UnauthorizedAccessException>(() => _analyzer.Analyze(new ProjectAnalyzerOptions(outside)));
    }

    [Fact]
    public void Analyze_MultiFramework_ProjectDetectsMultipleSignals()
    {
        var project = CreateProject("MultiFramework", "xunit;net10.0", "Microsoft.Playwright;Reqnroll", "using Xunit; using Microsoft.Playwright; using Reqnroll; [Fact] public void Test() { }");

        var result = _analyzer.Analyze(new ProjectAnalyzerOptions(project));

        Assert.Contains("xunit", result.TestFramework);
        Assert.Equal("reqnroll", result.BddFramework);
        Assert.Equal("playwright", result.BrowserAutomation);
        Assert.Contains("net10.0", result.TargetFrameworks);
    }

    private string CreateProject(string name, string? frameworks, string? packages, string content)
    {
        var dir = Path.Combine(_root, name);
        Directory.CreateDirectory(dir);
        var csproj = Path.Combine(dir, $"{name}.csproj");
        var tf = frameworks is null ? "<TargetFramework>net10.0</TargetFramework>" : $"<TargetFrameworks>{frameworks}</TargetFrameworks>";
        var pkgRefs = packages is null ? string.Empty : string.Join(Environment.NewLine, packages.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(p => $"    <PackageReference Include=\"{p}\" Version=\"1.0.0\" />"));
        File.WriteAllText(csproj, $"<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup>{tf}<IsTestProject>true</IsTestProject></PropertyGroup><ItemGroup>{pkgRefs}</ItemGroup></Project>");
        File.WriteAllText(Path.Combine(dir, "Sample.cs"), content);
        return dir;
    }

    private void CreateSkill(string name)
    {
        var dir = Path.Combine(_skills, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"# {name}\n");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}