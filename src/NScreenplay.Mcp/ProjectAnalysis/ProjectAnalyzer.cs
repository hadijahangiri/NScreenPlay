using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Security;
using System.Text.Json;
using System.Xml.Linq;

namespace NScreenplay.Mcp.ProjectAnalysis;

/// <summary>
/// Deterministic, read-only analysis of an existing .NET test project.
/// Does not restore, build, test, modify, or execute the project.
/// </summary>
public sealed class ProjectAnalyzer
{
    private readonly string _workspaceRoot;
    private readonly SkillLoader _skillLoader;
    private readonly string _skillsRootPath;

    public ProjectAnalyzer(string workspaceRoot, string skillsRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillsRootPath);
        _workspaceRoot = Path.GetFullPath(workspaceRoot);
        _skillsRootPath = Path.GetFullPath(skillsRootPath);
        _skillLoader = new SkillLoader(_skillsRootPath);
    }

    public ProjectAnalysisResult Analyze(ProjectAnalyzerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectPath = ValidateProjectPath(options.ProjectPath);
        var projectDirectory = File.Exists(projectPath) ? Path.GetDirectoryName(projectPath) ?? projectPath : projectPath;
        var evidence = new List<string>();

        var projectFiles = DiscoverProjectFiles(projectDirectory);
        var csprojPaths = projectFiles.Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
        var targetFrameworks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceFiles = projectFiles.Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToList();
        var textBlobs = new List<string>();

        foreach (var file in projectFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                textBlobs.Add(content);
                if (file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    ParseProjectFile(content, targetFrameworks, packageReferences, evidence, file);
            }
            catch (Exception ex)
            {
                evidence.Add($"Could not read {Path.GetFileName(file)}: {ex.GetType().Name}");
            }
        }

        var testFramework = DetectTestFramework(packageReferences, textBlobs, evidence);
        var bddFramework = DetectBddFramework(packageReferences, textBlobs, evidence);
        var browserAutomation = DetectBrowserAutomation(packageReferences, textBlobs, evidence);
        var apiTesting = DetectApiTesting(packageReferences, textBlobs, evidence);
        var screenplay = DetectScreenplay(packageReferences, textBlobs, evidence);
        var nscreenplay = DetectNscreenplayPackages(packageReferences);
        var availableSkills = _skillLoader.ListSkills().Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var skills = RecommendSkills(testFramework, bddFramework, browserAutomation, screenplay, nscreenplay, availableSkills);
        var packages = RecommendPackages(testFramework, bddFramework, browserAutomation, screenplay, nscreenplay);
        var adoption = DetermineAdoptionLevel(nscreenplay, screenplay, testFramework, bddFramework, browserAutomation);
        var migration = BuildMigrationPlan(testFramework, bddFramework, browserAutomation, screenplay, nscreenplay, adoption);
        var warnings = BuildWarnings(projectFiles, csprojPaths, packageReferences, bddFramework, browserAutomation);

        return new ProjectAnalysisResult(
            ProjectPath: projectPath,
            ProjectType: DetermineProjectType(projectFiles, packageReferences),
            Language: sourceFiles.Count > 0 ? "C#" : null,
            TargetFrameworks: targetFrameworks.OrderBy(x => x).ToList(),
            TestFramework: testFramework,
            BddFramework: bddFramework,
            BrowserAutomation: browserAutomation,
            ApiTesting: apiTesting,
            NScreenplay: nscreenplay,
            ScreenplayDetected: screenplay.Detected,
            ScreenplayDetectionEvidence: screenplay.Evidence,
            RecommendedPackages: packages,
            RecommendedSkills: skills,
            AdoptionLevel: adoption,
            MigrationPlan: migration,
            Warnings: warnings,
            Evidence: evidence);
    }

    private string ValidateProjectPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required.", nameof(projectPath));

        var fullPath = Path.GetFullPath(Path.IsPathRooted(projectPath)
            ? projectPath
            : Path.Combine(_workspaceRoot, projectPath));

        if (!InputValidator.IsPathWithinRoot(fullPath, _workspaceRoot))
            throw new UnauthorizedAccessException("Project path is outside the allowed workspace root.");

        if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");

        return fullPath;
    }

    private static IReadOnlyList<string> DiscoverProjectFiles(string root)
    {
        if (File.Exists(root))
            return [root];

        var results = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".props", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".targets", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".json", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".config", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(file);
            }
        }

        return results;
    }

    private static void ParseProjectFile(
        string content,
        ISet<string> targetFrameworks,
        ISet<string> packageReferences,
        ICollection<string> evidence,
        string filePath)
    {
        try
        {
            var doc = XDocument.Parse(content);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            foreach (var tf in doc.Descendants(ns + "TargetFramework"))
                AddFrameworks(tf.Value, targetFrameworks);
            foreach (var tfs in doc.Descendants(ns + "TargetFrameworks"))
                AddFrameworks(tfs.Value, targetFrameworks);
            foreach (var pkg in doc.Descendants(ns + "PackageReference"))
            {
                var include = pkg.Attribute("Include")?.Value;
                if (!string.IsNullOrWhiteSpace(include))
                    packageReferences.Add(include);
            }

            evidence.Add($"Parsed package metadata from {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            evidence.Add($"Could not parse {Path.GetFileName(filePath)}: {ex.GetType().Name}");
        }
    }

    private static void AddFrameworks(string value, ISet<string> targetFrameworks)
    {
        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            targetFrameworks.Add(part);
    }

    private static string? DetectTestFramework(ISet<string> packageReferences, IEnumerable<string> textBlobs, ICollection<string> evidence)
    {
        if (packageReferences.Contains("xunit") || textBlobs.Any(t => t.Contains("[Fact]", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected xUnit via package reference or [Fact].");
            return "xunit";
        }

        if (packageReferences.Contains("NUnit") || textBlobs.Any(t => t.Contains("[Test]", StringComparison.OrdinalIgnoreCase) || t.Contains("[TestFixture]", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected NUnit via package reference or NUnit attributes.");
            return "nunit";
        }

        if (packageReferences.Contains("MSTest.TestFramework") || textBlobs.Any(t => t.Contains("[TestMethod]", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected MSTest via package reference or [TestMethod].");
            return "mstest";
        }

        return null;
    }

    private static string? DetectBddFramework(ISet<string> packageReferences, IEnumerable<string> textBlobs, ICollection<string> evidence)
    {
        if (packageReferences.Contains("Reqnroll") || textBlobs.Any(t => t.Contains("[Binding]", StringComparison.OrdinalIgnoreCase) || t.Contains("Reqnroll", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected Reqnroll via package reference or binding attributes.");
            return "reqnroll";
        }

        if (packageReferences.Contains("SpecFlow") || textBlobs.Any(t => t.Contains("TechTalk.SpecFlow", StringComparison.OrdinalIgnoreCase) || t.Contains("[Binding]", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected SpecFlow via package reference or namespace usage.");
            return "specflow";
        }

        if (packageReferences.Any(p => p.Equals("BDDfy", StringComparison.OrdinalIgnoreCase) || p.Contains("BDDfy", StringComparison.OrdinalIgnoreCase)) ||
            textBlobs.Any(t => t.Contains("BDDfy", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected BDDfy via package or source reference.");
            return "bddfy";
        }

        return null;
    }

    private static string? DetectBrowserAutomation(ISet<string> packageReferences, IEnumerable<string> textBlobs, ICollection<string> evidence)
    {
        if (packageReferences.Contains("Microsoft.Playwright") || textBlobs.Any(t => t.Contains("Microsoft.Playwright", StringComparison.OrdinalIgnoreCase) || t.Contains("IPage", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected Playwright via package reference or IPage usage.");
            return "playwright";
        }

        if (packageReferences.Any(p => p.Equals("Selenium.WebDriver", StringComparison.OrdinalIgnoreCase) || p.Contains("Selenium", StringComparison.OrdinalIgnoreCase)) ||
            textBlobs.Any(t => t.Contains("OpenQA.Selenium", StringComparison.OrdinalIgnoreCase) || t.Contains("IWebDriver", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add("Detected Selenium via package reference or IWebDriver usage.");
            return "selenium";
        }

        return null;
    }

    private static bool DetectApiTesting(ISet<string> packageReferences, IEnumerable<string> textBlobs, ICollection<string> evidence)
    {
        var detected = packageReferences.Contains("RestSharp") ||
            textBlobs.Any(t => t.Contains("HttpClient", StringComparison.OrdinalIgnoreCase) ||
                               t.Contains("WebApplicationFactory", StringComparison.OrdinalIgnoreCase) ||
                               t.Contains("RestSharp", StringComparison.OrdinalIgnoreCase));

        if (detected)
            evidence.Add("Detected API testing evidence via HttpClient, WebApplicationFactory, or RestSharp.");

        return detected;
    }

    private static (bool Detected, IReadOnlyList<string> Evidence) DetectScreenplay(ISet<string> packageReferences, IEnumerable<string> textBlobs, ICollection<string> evidence)
    {
        var screenplaySignals = new[]
        {
            "Actor.Named(",
            "AttemptsTo(",
            "AsksFor(",
            "Should(",
            "Target.The(",
            "BrowseTheWeb.Using(",
            "ITask",
            "IInteraction",
            "IQuestion<",
            "IConsequence",
            "NScreenplay.Core",
            "NScreenplay.Playwright",
            "NScreenplay.Reqnroll",
            "NScreenplay.Mcp"
        };
        var findings = new List<string>();

        if (packageReferences.Contains("NScreenplay.Core") || packageReferences.Contains("NScreenplay.Playwright") || packageReferences.Contains("NScreenplay.Reqnroll") || packageReferences.Contains("NScreenplay.Mcp"))
        {
            findings.Add("NScreenplay package reference detected.");
        }

        foreach (var signal in screenplaySignals)
        {
            if (textBlobs.Any(t => t.Contains(signal, StringComparison.Ordinal)))
                findings.Add($"Found Screenplay signal: {signal}");
        }

        var detected = findings.Count > 0 && (findings.Any(f => f.Contains("NScreenplay package reference", StringComparison.Ordinal)) || findings.Count >= 2);
        if (detected)
            evidence.Add("Detected Screenplay-style code or NScreenplay package usage.");

        return (detected, findings.Distinct().ToList());
    }

    private static NScreenplayPackagePresence DetectNscreenplayPackages(ISet<string> packageReferences) =>
        new(
            Core: packageReferences.Contains("NScreenplay.Core"),
            Playwright: packageReferences.Contains("NScreenplay.Playwright"),
            Reqnroll: packageReferences.Contains("NScreenplay.Reqnroll"),
            Mcp: packageReferences.Contains("NScreenplay.Mcp"));

    private static string DetermineProjectType(IEnumerable<string> files, ISet<string> packageReferences)
    {
        if (files.Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) || packageReferences.Count > 0)
            return "dotnet-test";
        return "unknown";
    }

    private static IReadOnlyList<string> RecommendPackages(
        string? testFramework,
        string? bddFramework,
        string? browserAutomation,
        (bool Detected, IReadOnlyList<string> Evidence) screenplay,
        NScreenplayPackagePresence nscreenplay)
    {
        var packages = new List<string>();

        if (nscreenplay.Core || nscreenplay.Playwright || nscreenplay.Reqnroll || nscreenplay.Mcp)
        {
            if (!nscreenplay.Core && screenplay.Detected)
                packages.Add("NScreenplay.Core");
            if (!nscreenplay.Playwright && browserAutomation == "playwright")
                packages.Add("NScreenplay.Playwright");
            if (!nscreenplay.Reqnroll && bddFramework == "reqnroll")
                packages.Add("NScreenplay.Reqnroll");
            return packages;
        }

        if (screenplay.Detected)
            packages.Add("NScreenplay.Core");
        if (browserAutomation == "playwright")
            packages.Add("NScreenplay.Playwright");
        if (bddFramework == "reqnroll")
            packages.Add("NScreenplay.Reqnroll");

        return packages;
    }

    private static IReadOnlyList<string> RecommendSkills(
        string? testFramework,
        string? bddFramework,
        string? browserAutomation,
        (bool Detected, IReadOnlyList<string> Evidence) screenplay,
        NScreenplayPackagePresence nscreenplay,
        ISet<string> availableSkills)
    {
        var skills = new List<string>();
        if ((screenplay.Detected || browserAutomation is not null || bddFramework is not null) && availableSkills.Contains("screenplay"))
            skills.Add("screenplay");
        if (browserAutomation == "playwright" && availableSkills.Contains("playwright"))
            skills.Add("playwright");
        if (bddFramework == "reqnroll" && availableSkills.Contains("reqnroll"))
            skills.Add("reqnroll");
        if (!nscreenplay.Core && availableSkills.Contains("test-authoring"))
            skills.Add("test-authoring");
        if (availableSkills.Contains("test-review"))
            skills.Add("test-review");
        if (testFramework is not null && !skills.Contains("test-authoring") && availableSkills.Contains("test-authoring"))
            skills.Add("test-authoring");
        return skills.Distinct().ToList();
    }

    private static string DetermineAdoptionLevel(
        NScreenplayPackagePresence nscreenplay,
        (bool Detected, IReadOnlyList<string> Evidence) screenplay,
        string? testFramework,
        string? bddFramework,
        string? browserAutomation)
    {
        if (nscreenplay.Core && (nscreenplay.Playwright || browserAutomation == "playwright") && (nscreenplay.Reqnroll || bddFramework == "reqnroll" || bddFramework is null))
            return screenplay.Detected ? "already-adopted" : "partially-adopted";

        if (screenplay.Detected)
            return "partially-adopted";

        if (testFramework is not null || browserAutomation is not null || bddFramework is not null)
            return "recommended";

        return "possible";
    }

    private static IReadOnlyList<string> BuildMigrationPlan(
        string? testFramework,
        string? bddFramework,
        string? browserAutomation,
        (bool Detected, IReadOnlyList<string> Evidence) screenplay,
        NScreenplayPackagePresence nscreenplay,
        string adoptionLevel)
    {
        if (adoptionLevel == "already-adopted")
            return [];

        var steps = new List<string>();
        if (!nscreenplay.Core)
            steps.Add("Introduce NScreenplay.Core and an Actor lifecycle.");
        if (browserAutomation == "playwright" && !nscreenplay.Playwright)
            steps.Add("Move browser interactions behind NScreenplay.Playwright BrowseTheWeb and Screenplay interactions.");
        if (bddFramework == "reqnroll" && !nscreenplay.Reqnroll)
            steps.Add("Keep step definitions thin and route them through ScenarioActor or Actor.");
        if (screenplay.Detected && !nscreenplay.Core)
            steps.Add("Consolidate manual Screenplay-like classes into NScreenplay.Core concepts.");
        if (testFramework is not null)
            steps.Add("Keep test assertions in the test framework, but move business actions into Tasks and Questions.");
        steps.Add("Move selectors and locators into Targets.");
        steps.Add("Remove direct framework calls from step definitions or tests where Screenplay abstractions can be reused.");
        return steps.Distinct().ToList();
    }

    private static IReadOnlyList<string> BuildWarnings(
        IReadOnlyList<string> files,
        IReadOnlyList<string> csprojPaths,
        ISet<string> packageReferences,
        string? bddFramework,
        string? browserAutomation)
    {
        var warnings = new List<string>();
        if (csprojPaths.Count == 0)
            warnings.Add("No .csproj file found; analysis is based on source files only.");
        if (packageReferences.Contains("BDDfy") && bddFramework != "bddfy")
            warnings.Add("BDDfy package reference detected but BDD usage is not conclusive.");
        if (browserAutomation is null)
            warnings.Add("No browser automation package or API usage was detected.");
        if (files.Count == 0)
            warnings.Add("No inspectable files were found.");
        return warnings;
    }
}