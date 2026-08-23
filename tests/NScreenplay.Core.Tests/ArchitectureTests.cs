using System.Reflection;

namespace NScreenplay.Core.Tests;

/// <summary>
/// Validates that NScreenplay.Core has no forbidden dependencies.
/// If any integration dependency leaks into Core, these tests will fail.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly CoreAssembly = typeof(NScreenplay.Core.Actor).Assembly;

    private static readonly string[] ForbiddenAssemblies =
    [
        "Microsoft.Playwright",
        "Reqnroll",
        "NUnit.Framework",
        "xunit",
        "Microsoft.VisualStudio.TestPlatform",
        "Selenium.WebDriver",
        "Appium",
        "Microsoft.Extensions.Http",
        "RestSharp",
        "System.Net.Http.Json", // acceptable at runtime but not as a dep in Core itself
    ];

    [Fact]
    public void Core_HasNoForbiddenReferences()
    {
        var referencedNames = CoreAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToList();

        foreach (var forbidden in ForbiddenAssemblies)
        {
            var found = referencedNames.Any(r =>
                r.Equals(forbidden, StringComparison.OrdinalIgnoreCase) ||
                r.StartsWith(forbidden + ".", StringComparison.OrdinalIgnoreCase));

            Assert.False(found,
                $"NScreenplay.Core must NOT reference '{forbidden}'. " +
                $"Found referenced assemblies: {string.Join(", ", referencedNames)}");
        }
    }

    [Fact]
    public void Core_HasNoThirdPartyDependencies()
    {
        // Core should only depend on .NET BCL assemblies (names start with "System" or "Microsoft.NETCore" etc.)
        var referenced = CoreAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(name =>
                !name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Microsoft.NETCore", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(referenced);
    }
}
