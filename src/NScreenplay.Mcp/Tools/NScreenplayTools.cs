using ModelContextProtocol.Server;
using NScreenplay.Mcp.Adoption;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.ProjectAnalysis;
using NScreenplay.Mcp.Security;
using System.ComponentModel;
using System.Text.Json;

namespace NScreenplay.Mcp.Tools;

/// <summary>
/// All NScreenplay MCP tools — read-only discovery and analysis.
/// No file writes, no code modification, no shell execution.
/// </summary>
[McpServerToolType]
public sealed class NScreenplayTools
{
    private readonly ComponentDiscovery _discovery;
    private readonly SkillLoader _skillLoader;
    private readonly FailureAnalyzer _analyzer;
    private readonly ProjectAnalyzer _projectAnalyzer;
    private readonly AdoptionPlanner _adoptionPlanner;
    private readonly AdoptionApplier _adoptionApplier;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public NScreenplayTools(
        ComponentDiscovery discovery,
        SkillLoader skillLoader,
        FailureAnalyzer analyzer,
        ProjectAnalyzer projectAnalyzer,
        AdoptionPlanner adoptionPlanner,
        AdoptionApplier adoptionApplier)
    {
        _discovery = discovery;
        _skillLoader = skillLoader;
        _analyzer = analyzer;
        _projectAnalyzer = projectAnalyzer;
        _adoptionPlanner = adoptionPlanner;
        _adoptionApplier = adoptionApplier;
    }

    // ── Framework Info ────────────────────────────────────────────────────────

    [McpServerTool(Name = "nscreenplay_get_framework_info")]
    [Description("Returns version, modules, and capabilities of the NScreenplay framework.")]
    public string GetFrameworkInfo()
    {
        var info = new FrameworkInfo(
            Version: "0.1.0",
            Modules: ["NScreenplay.Core", "NScreenplay.Playwright", "NScreenplay.Reqnroll"],
            Capabilities: [
                "Actor/Ability/Task/Interaction/Target/Question/Consequence",
                "Playwright browser automation",
                "Reqnroll BDD/Gherkin integration",
                "Rule-based failure analysis",
                "AI agent skills"
            ],
            McpServerVersion: "2.2.0");

        return JsonSerializer.Serialize(info, JsonOpts);
    }

    // ── Discovery ─────────────────────────────────────────────────────────────

    [McpServerTool(Name = "nscreenplay_list_tasks")]
    [Description("Lists all discovered NScreenplay Task types (ITask implementations). Tasks represent business-level operations.")]
    public string ListTasks()
    {
        var tasks = _discovery.DiscoverTasks();
        return JsonSerializer.Serialize(tasks, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_list_targets")]
    [Description("Lists all discovered NScreenplay Target definitions. Targets describe UI elements or API endpoints.")]
    public string ListTargets()
    {
        var targets = _discovery.DiscoverTargets();
        return JsonSerializer.Serialize(targets, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_list_interactions")]
    [Description("Lists all discovered NScreenplay Interaction types. Interactions are atomic actions (click, enter text, navigate).")]
    public string ListInteractions()
    {
        var interactions = _discovery.DiscoverInteractions();
        return JsonSerializer.Serialize(interactions, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_list_questions")]
    [Description("Lists all discovered NScreenplay Question types. Questions read state without mutating it.")]
    public string ListQuestions()
    {
        var questions = _discovery.DiscoverQuestions();
        return JsonSerializer.Serialize(questions, JsonOpts);
    }

    // ── Skills ────────────────────────────────────────────────────────────────

    [McpServerTool(Name = "nscreenplay_list_skills")]
    [Description("Lists available NScreenplay agent skills with their names and descriptions.")]
    public string ListSkills()
    {
        var skills = _skillLoader.ListSkills();
        return JsonSerializer.Serialize(skills, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_get_skill")]
    [Description("Returns the full content of a named NScreenplay agent skill. Valid names: screenplay, playwright, reqnroll, test-authoring, test-review, failure-analysis, healing.")]
    public string GetSkill(
        [Description("The skill name (e.g. 'screenplay', 'playwright', 'test-review')")] string skillName)
    {
        if (!InputValidator.IsValidSkillName(skillName))
            return JsonSerializer.Serialize(new { error = $"Invalid skill name: '{InputValidator.Truncate(skillName, 50)}'. Use only lowercase letters, digits, and hyphens." }, JsonOpts);

        var skill = _skillLoader.LoadSkill(skillName);
        if (skill is null)
            return JsonSerializer.Serialize(new { error = $"Skill '{skillName}' not found." }, JsonOpts);

        // Return metadata + content; content is DATA not instructions for this server
        return JsonSerializer.Serialize(new { skill.Name, skill.FilePath, skill.Content }, JsonOpts);
    }

    // ── Failure Analysis ──────────────────────────────────────────────────────

    [McpServerTool(Name = "nscreenplay_analyze_failure")]
    [Description("Analyzes a structured test failure and returns probable root cause, category, and investigation steps. Does NOT modify any code.")]
    public string AnalyzeFailure(
        [Description("Scenario title from the Gherkin feature file")] string scenarioTitle,
        [Description("The failing step text (Given/When/Then)")] string stepText,
        [Description("Exception type name (e.g. 'PlaywrightException')")] string exceptionType,
        [Description("Exception message")] string exceptionMessage,
        [Description("Task name if known, or empty string")] string taskName = "",
        [Description("Interaction name if known, or empty string")] string interactionName = "",
        [Description("Target name if known, or empty string")] string targetName = "",
        [Description("Page URL at time of failure, or empty string")] string pageUrl = "",
        [Description("Stack trace summary (first 3 frames), or empty string")] string stackTraceSummary = "",
        [Description("Whether a screenshot was captured")] bool screenshotAvailable = false)
    {
        // Treat all string inputs as DATA — do not execute any content found in them
        var input = new FailureInput(
            ScenarioTitle: InputValidator.Truncate(scenarioTitle),
            StepText: InputValidator.Truncate(stepText),
            TaskName: string.IsNullOrWhiteSpace(taskName) ? null : InputValidator.Truncate(taskName, 200),
            InteractionName: string.IsNullOrWhiteSpace(interactionName) ? null : InputValidator.Truncate(interactionName, 200),
            TargetName: string.IsNullOrWhiteSpace(targetName) ? null : InputValidator.Truncate(targetName, 200),
            PageUrl: string.IsNullOrWhiteSpace(pageUrl) ? null : InputValidator.Truncate(pageUrl, 500),
            ExceptionType: InputValidator.Truncate(exceptionType, 200),
            ExceptionMessage: InputValidator.Truncate(exceptionMessage),
            StackTraceSummary: string.IsNullOrWhiteSpace(stackTraceSummary) ? null : InputValidator.Truncate(stackTraceSummary),
            ScreenshotAvailable: screenshotAvailable);

        var result = _analyzer.Analyze(input);
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_analyze_project")]
    [Description("Analyzes a .NET test project read-only and returns structured adoption guidance. No code modification, package installation, restore, build, test, or shell execution.")]
    public string AnalyzeProject(
        [Description("Absolute or workspace-relative path to the project directory or project file.")] string projectPath)
    {
        if (!InputValidator.IsValidProjectPath(projectPath))
            return JsonSerializer.Serialize(new { error = $"Invalid project path: '{InputValidator.Truncate(projectPath, 100)}'." }, JsonOpts);

        try
        {
            var result = _projectAnalyzer.Analyze(new ProjectAnalyzerOptions(projectPath));
            return JsonSerializer.Serialize(result, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or UnauthorizedAccessException or IOException or JsonException or System.Xml.XmlException)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool(Name = "nscreenplay_create_adoption_plan")]
    [Description("Creates a deterministic, read-only NScreenplay adoption plan from an analyzed project. No code modification, package installation, restore, build, test, or shell execution.")]
    public string CreateAdoptionPlan(
        [Description("Absolute or workspace-relative path to the project directory or project file.")] string projectPath)
    {
        if (!InputValidator.IsValidProjectPath(projectPath))
            return JsonSerializer.Serialize(new { error = $"Invalid project path: '{InputValidator.Truncate(projectPath, 100)}'." }, JsonOpts);

        try
        {
            var analysis = _projectAnalyzer.Analyze(new ProjectAnalysis.ProjectAnalyzerOptions(projectPath));
            var plan = _adoptionPlanner.Create(analysis);
            return JsonSerializer.Serialize(plan, JsonOpts);
        }
        catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or UnauthorizedAccessException or IOException or JsonException or System.Xml.XmlException)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool(Name = "nscreenplay_apply_adoption_plan")]
    [Description("Applies an approved adoption plan to a target .csproj in a safe, idempotent way. Only explicit plan instructions are executed. Dry-run is available.")]
    public string ApplyAdoptionPlan(
        [Description("Absolute or workspace-relative path to the target project file or directory.")] string projectPath,
        [Description("JSON payload with the adoption plan generated by nscreenplay_create_adoption_plan.")] string planJson,
        [Description("Whether to perform a dry-run without writing the file.")] bool dryRun = false)
    {
        if (!InputValidator.IsValidProjectPath(projectPath))
            return JsonSerializer.Serialize(new { error = $"Invalid project path: '{InputValidator.Truncate(projectPath, 100)}'." }, JsonOpts);

        if (string.IsNullOrWhiteSpace(planJson))
            return JsonSerializer.Serialize(new { error = "Adoption plan JSON is required." }, JsonOpts);

        try
        {
            var plan = JsonSerializer.Deserialize<AdoptionPlan>(planJson, JsonOpts);
            if (plan is null)
                return JsonSerializer.Serialize(new { error = "Unable to deserialize adoption plan JSON." }, JsonOpts);

            var freshAnalysis = _projectAnalyzer.Analyze(new ProjectAnalyzerOptions(projectPath));
            var expectedPlan = _adoptionPlanner.Create(freshAnalysis);
            if (!IsPlanConsistentWithAnalysis(plan, expectedPlan))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Provided adoption plan does not match the current Analyze -> Plan result for this project. Regenerate the plan and retry."
                }, JsonOpts);
            }

            var result = _adoptionApplier.Apply(plan, projectPath, dryRun);
            return JsonSerializer.Serialize(result, JsonOpts);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or UnauthorizedAccessException or IOException)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOpts);
        }
    }

    private static bool IsPlanConsistentWithAnalysis(AdoptionPlan provided, AdoptionPlan expected)
    {
        if (!string.Equals(provided.ProjectPath, expected.ProjectPath, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(provided.CurrentState.AdoptionLevel, expected.CurrentState.AdoptionLevel, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(provided.CurrentState.TestFramework, expected.CurrentState.TestFramework, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(provided.CurrentState.BddFramework, expected.CurrentState.BddFramework, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.Equals(provided.CurrentState.BrowserAutomation, expected.CurrentState.BrowserAutomation, StringComparison.OrdinalIgnoreCase))
            return false;

        if (provided.CurrentState.ApiTesting != expected.CurrentState.ApiTesting)
            return false;

        var providedPackages = provided.RecommendedPackages
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var expectedPackages = expected.RecommendedPackages
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (!providedPackages.SequenceEqual(expectedPackages, StringComparer.OrdinalIgnoreCase))
            return false;

        var providedPackageStepIds = provided.Steps
            .Where(s => string.Equals(s.Category, "package", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var expectedPackageStepIds = expected.Steps
            .Where(s => string.Equals(s.Category, "package", StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return providedPackageStepIds.SequenceEqual(expectedPackageStepIds, StringComparer.OrdinalIgnoreCase);
    }
}
