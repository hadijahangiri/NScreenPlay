using NScreenplay.Mcp.Discovery;

namespace NScreenplay.Mcp.Tests;

public sealed class DocumentationConsistencyTests
{
    [Fact]
    public void CriticalDocs_ReferenceCanonicalPlaybook()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));
        var gettingStarted = File.ReadAllText(Path.Combine(root, "docs", "getting-started.md"));
        var ai = File.ReadAllText(Path.Combine(root, "docs", "ai.md"));

        Assert.Contains("external-agent-adoption.md", readme, StringComparison.Ordinal);
        Assert.Contains("external-agent-adoption.md", gettingStarted, StringComparison.Ordinal);
        Assert.Contains("external-agent-adoption.md", ai, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalPlaybook_ContainsSevenSkills_AndRealMcpResources()
    {
        var root = FindRepositoryRoot();
        var canonical = File.ReadAllText(Path.Combine(root, "docs", "external-agent-adoption.md"));

        // Skills
        Assert.Contains("screenplay", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("playwright", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reqnroll", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-authoring", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("test-review", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("failure", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("healing", canonical, StringComparison.OrdinalIgnoreCase);

        // Core MCP resources/tools used by workflow
        Assert.Contains("nscreenplay://adoption-workflow", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("nscreenplay_analyze_project", canonical, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_create_adoption_plan", canonical, StringComparison.Ordinal);
        Assert.Contains("nscreenplay_apply_adoption_plan", canonical, StringComparison.Ordinal);
    }

    [Fact]
    public void NoCriticalContradiction_OnFakePackagesAcrossDocsAndSkills()
    {
        var root = FindRepositoryRoot();

        var docs = new[]
        {
            Path.Combine(root, "README.md"),
            Path.Combine(root, "docs", "getting-started.md"),
            Path.Combine(root, "docs", "ai.md"),
            Path.Combine(root, "docs", "external-agent-adoption.md"),
            Path.Combine(root, "docs", "project-analysis.md")
        };

        foreach (var path in docs)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("dotnet add package NScreenplay.Api", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("dotnet add package NScreenplay.BDDfy", text, StringComparison.OrdinalIgnoreCase);
        }

        var skillLoader = new SkillLoader(Path.Combine(root, "skills"));
        var skills = skillLoader.ListSkills();
        Assert.Equal(7, skills.Count);

        foreach (var skill in skills)
        {
            var content = File.ReadAllText(skill.FilePath);
            Assert.DoesNotContain("dotnet add package NScreenplay.Api", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("dotnet add package NScreenplay.BDDfy", content, StringComparison.OrdinalIgnoreCase);
        }
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
}
