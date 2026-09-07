using System;
using System.IO;

namespace NScreenplay.Reqnroll;

internal static class ArtifactPathValidator
{
    // Resolves and validates the provided artifacts directory against the repository root.
    // Returns the canonical absolute path to use, or throws ArgumentException when invalid.
    public static string ResolveAndValidate(string? configuredPath)
    {
        // Determine repo root using the same convention tests use: search upward for NScreenplay.slnx
        var repoRoot = FindRepositoryRoot();

        string resolved;
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            resolved = Path.Combine(Path.GetTempPath(), "nscreenplay-artifacts");
        }
        else if (Path.IsPathRooted(configuredPath))
        {
            resolved = configuredPath;
        }
        else
        {
            // Resolve relative paths relative to the repository root
            resolved = Path.GetFullPath(Path.Combine(repoRoot, configuredPath));
        }

        // Canonicalize
        resolved = Path.GetFullPath(resolved);

        // Enforce containment: resolved must be inside repoRoot
        var repoRootCanonical = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = resolved.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        // Separator-aware prefix match
        if (!candidate.StartsWith(repoRootCanonical, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Artifacts directory '{configuredPath ?? "(temp)"}' is outside the repository root and is not allowed.");

        // Prevent sibling-prefix escape like C:\repo\artifacts-evil when repo root is C:\repo\artifacts
        // (handled by the StartsWith above because repoRootCanonical ends with a separator)

        // Disallow traversal segments
        if (resolved.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Artifacts directory must not contain traversal segments ('..').");

        return resolved;
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            var root = Path.GetFullPath(Path.Combine(current, "..", "..", "..", ".."));
            if (File.Exists(Path.Combine(root, "NScreenplay.slnx")))
                return root;

            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, current, StringComparison.Ordinal))
                break;
            current = parent;
        }

        // Fallback to current working directory if not found (defensive)
        return Directory.GetCurrentDirectory();
    }
}
