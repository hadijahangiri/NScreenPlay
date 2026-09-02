using NScreenplay.Mcp.Adoption;

namespace NScreenplay.Mcp.Tests;

public sealed class AdoptionApplyTests : IDisposable
{
    private readonly string _root;
    private readonly AdoptionApplier _applier;

    public AdoptionApplyTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"nscreenplay-adoption-apply-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        _applier = new AdoptionApplier(_root);
    }

    [Fact]
    public void Apply_ValidPlan_AddsMissingPackageReference()
    {
        var project = CreateProject("ValidProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("Success", result.Status);
        Assert.Contains("NScreenplay.Core", File.ReadAllText(project));
        Assert.NotEmpty(result.AppliedOperations);
    }

    [Fact]
    public void Apply_DryRun_DoesNotMutateProject()
    {
        var project = CreateProject("DryRunProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);
        var before = File.ReadAllText(project);

        var result = _applier.Apply(plan, project, dryRun: true);

        Assert.Equal("DryRun", result.Status);
        Assert.Equal(before, File.ReadAllText(project));
        Assert.NotEmpty(result.AppliedOperations);
    }

    [Fact]
    public void Apply_InvalidPlan_IsRejected()
    {
        var project = CreateProject("InvalidPlanProject");
        var plan = CreatePlan(string.Empty, ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("ValidationFailed", result.Status);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Apply_PathTraversal_IsRejected()
    {
        var project = CreateProject("TraversalProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, "../../outside-project", dryRun: false);

        Assert.Equal("PreconditionFailed", result.Status);
    }

    [Fact]
    public void Apply_Idempotent_DoesNotDuplicatePackageReference()
    {
        var project = CreateProject("IdempotentProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);

        _ = _applier.Apply(plan, project, dryRun: false);
        var second = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("Success", second.Status);
        Assert.Equal(1, CountPackageOccurrences(project, "NScreenplay.Core"));
    }

    [Fact]
    public void Apply_MismatchedProjectPath_ReportsConflict()
    {
        var project = CreateProject("ConflictProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);
        var differentProject = CreateProject("OtherProject");

        var result = _applier.Apply(plan, differentProject, dryRun: false);

        Assert.Equal("Conflict", result.Status);
    }

    [Fact]
    public void Apply_AbsolutePathOutsideWorkspace_IsRejected()
    {
        var project = CreateProject("OutsideProject");
        var outside = Path.GetPathRoot(project)!;
        var plan = CreatePlan(Path.Combine(outside, "outside", "outside.csproj"), ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("PreconditionFailed", result.Status);
    }

    [Fact]
    public void Apply_PlanWithNonNscreenplayPackage_IsRejected()
    {
        var project = CreateProject("PackagePolicyProject");
        var plan = CreatePlan(project, ["Newtonsoft.Json"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("ValidationFailed", result.Status);
        Assert.Contains(result.Errors, e => e.Contains("Unsupported package", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_PlanWithUnknownNscreenplayPackage_IsRejected()
    {
        var project = CreateProject("UnknownNscreenplayPackageProject");
        var plan = CreatePlan(project, ["NScreenplay.Unknown"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("ValidationFailed", result.Status);
        Assert.Contains(result.Errors, e => e.Contains("Unsupported package", StringComparison.Ordinal));
    }

    [Fact]
    public void Apply_WithNoPackageSteps_IsRejected()
    {
        var project = CreateProject("NoPackageStepProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]) with
        {
            Steps = [new AdoptionPlanStep("actor-lifecycle", "Introduce actor lifecycle", "architecture", "required", "Architecture step only.", [], ["tests"])]
        };

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("ValidationFailed", result.Status);
    }

    [Fact]
    public void Apply_MsbuildXmlNamespace_Project_IsHandledCorrectly()
    {
        var project = CreateProject("NamespacedProject", withMsbuildNamespace: true);
        var plan = CreatePlan(project, ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("Success", result.Status);
        Assert.Equal(1, CountPackageOccurrences(project, "NScreenplay.Core"));
    }

    [Fact]
    public void Apply_DirectoryPathWithSingleCsproj_IsResolvedAndApplied()
    {
        var project = CreateProject("DirectoryPathProject");
        var projectDir = Path.GetDirectoryName(project)!;
        var plan = CreatePlan(projectDir, ["NScreenplay.Core"]);

        var result = _applier.Apply(plan, projectDir, dryRun: false);

        Assert.Equal("Success", result.Status);
        Assert.Equal(1, CountPackageOccurrences(project, "NScreenplay.Core"));
    }

    [Fact]
    public void Apply_InvalidCsprojXml_IsRejectedSafely()
    {
        var project = CreateProject("InvalidXmlProject");
        File.WriteAllText(project, "<Project><ItemGroup><PackageReference Include=\"NScreenplay.Core\"></Project>");
        var plan = CreatePlan(project, ["NScreenplay.Playwright"]);

        var result = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("ValidationFailed", result.Status);
    }

    [Fact]
    public void Apply_IdempotentSecondRun_ReportsNoChanges()
    {
        var project = CreateProject("NoOpSecondRunProject");
        var plan = CreatePlan(project, ["NScreenplay.Core"]);

        _ = _applier.Apply(plan, project, dryRun: false);
        var second = _applier.Apply(plan, project, dryRun: false);

        Assert.Equal("Success", second.Status);
        Assert.Empty(second.AppliedOperations);
        Assert.Contains(second.Warnings, w => w.Contains("already in the requested package state", StringComparison.Ordinal));
    }

    private static AdoptionPlan CreatePlan(string projectPath, IReadOnlyList<string> packages)
    {
        return new AdoptionPlan(
            ProjectPath: projectPath,
            CurrentState: new AdoptionPlanCurrentState(
                AdoptionLevel: "recommended",
                TestFramework: "xunit",
                BddFramework: null,
                BrowserAutomation: null,
                ApiTesting: false),
            RecommendedPackages: packages,
            RecommendedSkills: [new SkillRecommendation("screenplay", "Required for Actor/Task/Question modeling.")],
            Steps: [new AdoptionPlanStep("introduce-core", "Introduce NScreenplay.Core", "package", "required", "Add the core framework.", [], ["test project"])],
            Risks: ["Direct framework calls may be present."],
            Warnings: [],
            PreservationRules: ["Preserve the existing test framework."],
            EstimatedComplexity: "medium");
    }

    private static int CountPackageOccurrences(string projectPath, string package)
    {
        var text = File.ReadAllText(projectPath);
        return text.Split(package, StringSplitOptions.None).Length - 1;
    }

    private string CreateProject(string name, bool withMsbuildNamespace = false)
    {
        var dir = Path.Combine(_root, name);
        var filePath = Path.Combine(dir, $"{name}.csproj");
        Directory.CreateDirectory(dir);
        var projectXml = withMsbuildNamespace
            ? "<Project Sdk=\"Microsoft.NET.Sdk\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>"
            : "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>";
        File.WriteAllText(filePath, projectXml);
        return filePath;
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}
