using NScreenplay.Mcp.Security;

namespace NScreenplay.Mcp.Healing;

/// <summary>
/// Validates that file paths used in healing operations are safe.
/// Protects against path traversal, absolute path abuse, and out-of-workspace writes.
/// </summary>
public sealed class FileSafetyValidator
{
    private static readonly string[] AllowedExtensions = [".cs", ".feature", ".json"];
    private readonly string _workspaceRoot;

    public FileSafetyValidator(string workspaceRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
    }

    /// <summary>
    /// Validates that <paramref name="relativeOrAbsolutePath"/> is safe to write.
    /// Throws <see cref="UnauthorizedAccessException"/> if validation fails.
    /// </summary>
    public void ValidateWritePath(string relativeOrAbsolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeOrAbsolutePath);

        // Resolve to absolute
        var fullPath = Path.IsPathRooted(relativeOrAbsolutePath)
            ? Path.GetFullPath(relativeOrAbsolutePath)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, relativeOrAbsolutePath));

        // Must be within workspace
        if (!InputValidator.IsPathWithinRoot(fullPath, _workspaceRoot))
            throw new UnauthorizedAccessException(
                $"Path '{relativeOrAbsolutePath}' is outside the allowed workspace root.");

        // Must have an allowed extension
        var ext = Path.GetExtension(fullPath);
        if (!AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Extension '{ext}' is not allowed. Permitted: {string.Join(", ", AllowedExtensions)}.");

        // Must not contain suspicious segments
        if (fullPath.Contains("..", StringComparison.Ordinal))
            throw new UnauthorizedAccessException("Path contains '..' traversal segment.");
    }

    /// <summary>Resolves a workspace-relative path to an absolute path.</summary>
    public string Resolve(string relativeOrAbsolutePath) =>
        Path.IsPathRooted(relativeOrAbsolutePath)
            ? Path.GetFullPath(relativeOrAbsolutePath)
            : Path.GetFullPath(Path.Combine(_workspaceRoot, relativeOrAbsolutePath));
}
