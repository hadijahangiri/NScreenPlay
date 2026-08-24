namespace NScreenplay.Mcp.Security;

/// <summary>
/// Validates and sanitizes inputs from AI clients before use.
/// All MCP inputs must be treated as untrusted.
/// </summary>
public static class InputValidator
{
    private static readonly char[] PathSeparators = ['/', '\\'];

    /// <summary>
    /// Validates that a skill name is a safe identifier.
    /// Rejects path traversal attempts, null bytes, and unexpected characters.
    /// </summary>
    public static bool IsValidSkillName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 100) return false;
        // Allow only lowercase letters, digits, and hyphens (matches directory names)
        return name.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');
    }

    /// <summary>
    /// Validates that a resolved file path is safely within the expected root directory.
    /// Prevents path traversal attacks.
    /// </summary>
    public static bool IsPathWithinRoot(string filePath, string rootDirectory)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fullRoot = Path.GetFullPath(rootDirectory)
            .TrimEnd(PathSeparators) + Path.DirectorySeparatorChar;
        // Use Ordinal on Linux (case-sensitive FS), OrdinalIgnoreCase on Windows
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return fullPath.StartsWith(fullRoot, comparison);
    }

    /// <summary>
    /// Truncates a string to a safe maximum length for logging/display.
    /// Prevents log injection through excessively long inputs.
    /// </summary>
    public static string Truncate(string? value, int maxLength = 500)
    {
        if (value is null) return string.Empty;
        if (value.Length <= maxLength) return value;
        return value[..maxLength] + $"... [truncated {value.Length - maxLength} chars]";
    }

    /// <summary>
    /// Validates that an assembly path is an absolute path to a .dll file.
    /// </summary>
    public static bool IsValidAssemblyPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!Path.IsPathRooted(path)) return false;
        if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
}
