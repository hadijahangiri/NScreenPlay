using NScreenplay.Mcp.Discovery;
using System.IO;

namespace NScreenplay.Mcp.Tests;

public class SkillLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SkillLoader _loader;

    public SkillLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"nscreenplay-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        CreateSkill("screenplay", "# NScreenplay Screenplay Skill\n\nTest content.");
        CreateSkill("playwright", "# Playwright Skill\n\nPlaywright content.");
        _loader = new SkillLoader(_tempDir);
    }

    [Fact]
    public void ListSkills_ReturnsAllSkillDirectories()
    {
        var skills = _loader.ListSkills();
        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "screenplay");
        Assert.Contains(skills, s => s.Name == "playwright");
    }

    [Fact]
    public void ListSkills_ExtractsFirstHeading()
    {
        var skills = _loader.ListSkills();
        var screenplay = skills.First(s => s.Name == "screenplay");
        Assert.Equal("NScreenplay Screenplay Skill", screenplay.FirstHeading);
    }

    [Fact]
    public void LoadSkill_ReturnsContentForExistingSkill()
    {
        var content = _loader.LoadSkill("screenplay");
        Assert.NotNull(content);
        Assert.Equal("screenplay", content.Name);
        Assert.Contains("Test content", content.Content);
    }

    [Fact]
    public void LoadSkill_ReturnsNullForMissingSkill()
    {
        var content = _loader.LoadSkill("nonexistent");
        Assert.Null(content);
    }

    [Fact]
    public void LoadSkill_ThrowsForInvalidName()
    {
        Assert.Throws<ArgumentException>(() => _loader.LoadSkill("../../../etc/passwd"));
    }

    [Fact]
    public void LoadSkill_ThrowsForNameWithSpecialChars()
    {
        Assert.Throws<ArgumentException>(() => _loader.LoadSkill("skill; rm -rf /"));
    }

    [Fact]
    public void LoadSkill_ThrowsForEmptyName()
    {
        Assert.Throws<ArgumentException>(() => _loader.LoadSkill(""));
    }

    [Fact]
    public void LoadSkill_ThrowsForNullName()
    {
        Assert.Throws<ArgumentException>(() => _loader.LoadSkill(null!));
    }

    [Fact]
    public void ListSkills_EmptyDirectoryReturnsEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty-root");
        Directory.CreateDirectory(emptyDir);
        var loader = new SkillLoader(emptyDir);
        Assert.Empty(loader.ListSkills());
    }

    [Fact]
    public void ListSkills_NonExistentRootReturnsEmpty()
    {
        var loader = new SkillLoader(Path.Combine(_tempDir, "does-not-exist"));
        Assert.Empty(loader.ListSkills());
    }

    private void CreateSkill(string name, string content)
    {
        var dir = Path.Combine(_tempDir, name);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), content);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
