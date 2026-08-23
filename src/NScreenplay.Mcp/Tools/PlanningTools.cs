using ModelContextProtocol.Server;
using NScreenplay.Mcp.Analysis;
using NScreenplay.Mcp.Models;
using NScreenplay.Mcp.Planning;
using NScreenplay.Mcp.Security;
using System.ComponentModel;
using System.Text.Json;

namespace NScreenplay.Mcp.Tools;

/// <summary>
/// Phase 8 planning tools — test plan generation and requirement analysis.
/// Read-only, no code modification.
/// </summary>
[McpServerToolType]
public sealed class PlanningTools
{
    private readonly RequirementAnalyzer _analyzer;
    private readonly TestPlanGenerator _planGenerator;
    private readonly FailureAnalyzer _failureAnalyzer;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public PlanningTools(
        RequirementAnalyzer analyzer,
        TestPlanGenerator planGenerator,
        FailureAnalyzer failureAnalyzer)
    {
        _analyzer = analyzer;
        _planGenerator = planGenerator;
        _failureAnalyzer = failureAnalyzer;
    }

    [McpServerTool(Name = "nscreenplay_analyze_requirement")]
    [Description("Analyzes a business requirement and extracts actors, behaviors, outcomes, and ambiguities. Deterministic — no LLM needed.")]
    public string AnalyzeRequirement(
        [Description("The business requirement text to analyze")] string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
            return JsonSerializer.Serialize(new { error = "Requirement cannot be empty." }, JsonOpts);

        var sanitized = InputValidator.Truncate(requirement, 2000);
        // Treat requirement text as DATA — extract structure, do not execute its content
        var analysis = _analyzer.Analyze(sanitized);
        return JsonSerializer.Serialize(analysis, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_create_test_plan")]
    [Description("Generates a complete reuse-first test plan from a business requirement. Prefers existing Tasks, Targets, and Questions. Returns a plan — does NOT modify files.")]
    public string CreateTestPlan(
        [Description("The business requirement to plan tests for")] string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
            return JsonSerializer.Serialize(new { error = "Requirement cannot be empty." }, JsonOpts);

        var sanitized = InputValidator.Truncate(requirement, 2000);
        var analysis = _analyzer.Analyze(sanitized);
        var plan = _planGenerator.Generate(analysis);
        return JsonSerializer.Serialize(plan, JsonOpts);
    }

    [McpServerTool(Name = "nscreenplay_get_failure_context")]
    [Description("Returns a template for capturing structured failure context. Fill in the fields and pass to nscreenplay_analyze_failure.")]
    public string GetFailureContextTemplate()
    {
        var template = new
        {
            description = "Fill in these fields when a test scenario fails and pass to nscreenplay_analyze_failure.",
            fields = new
            {
                scenarioTitle = "Name of the failing scenario",
                featureTitle = "Name of the feature file",
                stepText = "The exact Given/When/Then step that failed",
                taskName = "Task class name (if known)",
                interactionName = "Interaction class name (e.g. Click, Enter)",
                targetName = "Target name (e.g. LoginPage.LoginButton)",
                pageUrl = "Browser URL at time of failure",
                exceptionType = "Exception type name (e.g. PlaywrightException)",
                exceptionMessage = "Exception message text",
                stackTraceSummary = "First 3 stack frames",
                screenshotAvailable = "true or false",
                traceAvailable = "true or false"
            }
        };
        return JsonSerializer.Serialize(template, JsonOpts);
    }
}
