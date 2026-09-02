using System.Xml.Linq;

namespace NScreenplay.Reqnroll.Tests;

public sealed class ReqnrollSmokeHarnessTests
{
    [Fact]
    public void SmokeHarness_HasCanonicalReqnrollConfiguration()
    {
        var root = FindRepositoryRoot();
        var configPath = Path.Combine(root, "samples", "ReqnrollSmoke", "reqnroll.json");
        Assert.True(File.Exists(configPath));

        var text = File.ReadAllText(configPath);
        Assert.Contains("reqnroll-config-latest.json", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NScreenplay.Reqnroll", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SmokeHarness_HasFeatureBindingHooksAndDeterministicHtml()
    {
        var root = FindRepositoryRoot();

        var feature = Path.Combine(root, "samples", "ReqnrollSmoke", "Features", "Smoke.feature");
        var steps = Path.Combine(root, "samples", "ReqnrollSmoke", "StepDefinitions", "SmokeSteps.cs");
        var hooks = Path.Combine(root, "samples", "ReqnrollSmoke", "Support", "SmokeHooks.cs");
        var html = Path.Combine(root, "samples", "ReqnrollSmoke", "TestApplication", "smoke.html");

        Assert.True(File.Exists(feature));
        Assert.True(File.Exists(steps));
        Assert.True(File.Exists(hooks));
        Assert.True(File.Exists(html));

        Assert.Contains("Scenario: Deterministic local page flow", File.ReadAllText(feature), StringComparison.Ordinal);
        Assert.Contains("[Binding]", File.ReadAllText(steps), StringComparison.Ordinal);
        Assert.Contains("InitializeFromFeatureBrowserAsync", File.ReadAllText(hooks), StringComparison.Ordinal);
        Assert.Contains("data-testid=\"smoke-submit\"", File.ReadAllText(html), StringComparison.Ordinal);
    }

    [Fact]
    public void SmokeHarness_Project_ReferencesCanonicalPackagesAndProjects()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "samples", "ReqnrollSmoke", "ReqnrollSmoke.csproj");
        Assert.True(File.Exists(projectPath));

        var doc = XDocument.Load(projectPath);
        var text = doc.ToString();

        Assert.Contains("Reqnroll.xUnit", text, StringComparison.Ordinal);
        Assert.Contains("Microsoft.Playwright", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.Core", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.Playwright", text, StringComparison.Ordinal);
        Assert.Contains("NScreenplay.Reqnroll", text, StringComparison.Ordinal);
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
