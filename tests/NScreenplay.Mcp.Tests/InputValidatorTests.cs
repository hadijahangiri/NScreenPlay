using NScreenplay.Mcp.Security;

namespace NScreenplay.Mcp.Tests;

public class InputValidatorTests
{
    // ── Skill name validation ─────────────────────────────────────────────────

    [Theory]
    [InlineData("screenplay")]
    [InlineData("test-review")]
    [InlineData("failure-analysis")]
    [InlineData("playwright")]
    public void IsValidSkillName_ValidNames_ReturnsTrue(string name)
    {
        Assert.True(InputValidator.IsValidSkillName(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../etc/passwd")]
    [InlineData("SkillName")]          // uppercase not allowed
    [InlineData("skill name")]         // space not allowed
    [InlineData("skill/name")]         // slash not allowed
    [InlineData("skill\\name")]        // backslash not allowed
    [InlineData("a;b")]                // semicolon
    [InlineData("a\0b")]               // null byte
    public void IsValidSkillName_InvalidNames_ReturnsFalse(string? name)
    {
        Assert.False(InputValidator.IsValidSkillName(name));
    }

    [Fact]
    public void IsValidSkillName_TooLongName_ReturnsFalse()
    {
        Assert.False(InputValidator.IsValidSkillName(new string('a', 101)));
    }

    // ── Path within root validation ───────────────────────────────────────────

    [Fact]
    public void IsPathWithinRoot_SafePath_ReturnsTrue()
    {
        var root = Path.GetTempPath();
        var safe = Path.Combine(root, "skills", "screenplay", "SKILL.md");
        Assert.True(InputValidator.IsPathWithinRoot(safe, Path.Combine(root, "skills")));
    }

    [Fact]
    public void IsPathWithinRoot_TraversalPath_ReturnsFalse()
    {
        var root = Path.Combine(Path.GetTempPath(), "skills");
        var traversal = Path.Combine(root, "..", "..", "etc", "passwd");
        Assert.False(InputValidator.IsPathWithinRoot(traversal, root));
    }

    // ── Truncation ────────────────────────────────────────────────────────────

    [Fact]
    public void Truncate_ShortString_ReturnsUnchanged()
    {
        Assert.Equal("hello", InputValidator.Truncate("hello", 100));
    }

    [Fact]
    public void Truncate_LongString_TruncatesWithMarker()
    {
        var result = InputValidator.Truncate(new string('x', 600), 500);
        Assert.StartsWith(new string('x', 500), result);
        Assert.Contains("truncated", result);
    }

    [Fact]
    public void Truncate_NullInput_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InputValidator.Truncate(null));
    }

    // ── Assembly path validation ──────────────────────────────────────────────

    [Fact]
    public void IsValidAssemblyPath_ValidPath_ReturnsTrue()
    {
        Assert.True(InputValidator.IsValidAssemblyPath(@"C:\projects\MyApp\bin\MyApp.dll"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("relative/path.dll")]  // not rooted
    [InlineData(@"C:\path\file.exe")]  // not .dll
    [InlineData(@"C:\path\file.DLL")]  // uppercase — should still work (case insensitive)
    public void IsValidAssemblyPath_InvalidOrEdgeCases_ReturnsFalseOrTrue(string? path)
    {
        // The uppercase .DLL case should pass (OrdinalIgnoreCase used internally)
        if (path == @"C:\path\file.DLL")
            Assert.True(InputValidator.IsValidAssemblyPath(path));
        else
            Assert.False(InputValidator.IsValidAssemblyPath(path));
    }
}
