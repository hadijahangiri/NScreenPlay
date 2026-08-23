using NScreenplay.Core;

namespace NScreenplay.Reqnroll.Tests;

/// <summary>
/// Architecture test: verifies NScreenplay.Core does not reference Reqnroll.
/// Reqnroll must only flow in one direction: Reqnroll → NScreenplay.Reqnroll → Core.
/// </summary>
public class ReqnrollArchitectureTests
{
    private static readonly System.Reflection.Assembly CoreAssembly =
        typeof(Actor).Assembly;

    [Fact]
    public void Core_DoesNotReferenceReqnroll()
    {
        var refs = CoreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .ToList();

        Assert.DoesNotContain(refs, r =>
            r.Equals("Reqnroll", StringComparison.OrdinalIgnoreCase) ||
            r.StartsWith("Reqnroll.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Core_DoesNotReferenceBoDi()
    {
        var refs = CoreAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .ToList();

        Assert.DoesNotContain(refs, r =>
            r.Equals("BoDi", StringComparison.OrdinalIgnoreCase));
    }
}
