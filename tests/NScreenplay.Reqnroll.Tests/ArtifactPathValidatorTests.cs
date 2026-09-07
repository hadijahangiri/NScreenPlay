using System;
using System.IO;

namespace NScreenplay.Reqnroll.Tests;

public class ArtifactPathValidatorTests
{
    [Fact]
    public void ValidRelativePath_IsAllowed()
    {
        var tmp = Path.Combine("artifacts", "valid");
        var resolved = ArtifactPathValidator.ResolveAndValidate(tmp);
        Assert.True(Path.IsPathRooted(resolved));
        Assert.Contains("artifacts", resolved, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TraversalPath_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ArtifactPathValidator.ResolveAndValidate("..\\outside"));
    }

    [Fact]
    public void SiblingPrefixEscape_IsRejected()
    {
        // Find repository root similarly to the production helper
        string FindRepo()
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

            return Directory.GetCurrentDirectory();
        }

        var repo = FindRepo();
        var evil = Path.GetFullPath(repo + "-evil\\artifacts");
        Assert.Throws<ArgumentException>(() => ArtifactPathValidator.ResolveAndValidate(evil));
    }
}
