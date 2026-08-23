using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.Security;

namespace NScreenplay.Mcp.Discovery;

/// <summary>
/// Loads NScreenplay Agent Skills from SKILL.md files.
/// One source of truth: the skills/ directory in the repository.
/// </summary>
public sealed class SkillLoader
{
    private readonly string _skillsRootPath;

    public SkillLoader(string skillsRootPath)
    {
        _skillsRootPath = Path.GetFullPath(skillsRootPath);
    }

    /// <summary>
    /// Returns metadata about all available skills without loading their full content.
    /// </summary>
    public IReadOnlyList<SkillInfo> ListSkills()
    {
        if (!Directory.Exists(_skillsRootPath)) return [];

        return Directory
            .GetDirectories(_skillsRootPath)
            .Select(dir =>
            {
                var skillMd = Path.Combine(dir, "SKILL.md");
                if (!File.Exists(skillMd)) return null;
                var name = Path.GetFileName(dir);
                var heading = ReadFirstHeading(skillMd);
                return new SkillInfo(name, skillMd, heading);
            })
            .Where(s => s is not null)
            .Select(s => s!)
            .OrderBy(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// Loads the full content of a named skill.
    /// Validates the name to prevent path traversal.
    /// </summary>
    public SkillContent? LoadSkill(string skillName)
    {
        if (!InputValidator.IsValidSkillName(skillName))
            throw new ArgumentException($"Invalid skill name: '{skillName}'.");

        var skillDir = Path.Combine(_skillsRootPath, skillName);
        var skillMd = Path.Combine(skillDir, "SKILL.md");

        // Path traversal check
        if (!InputValidator.IsPathWithinRoot(skillMd, _skillsRootPath))
            throw new ArgumentException("Requested skill path is outside the skills directory.");

        if (!File.Exists(skillMd)) return null;

        var content = File.ReadAllText(skillMd);
        return new SkillContent(skillName, skillMd, content);
    }

    private static string? ReadFirstHeading(string path)
    {
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith('#'))
                    return trimmed.TrimStart('#').Trim();
            }
            return null;
        }
        catch { return null; }
    }
}
