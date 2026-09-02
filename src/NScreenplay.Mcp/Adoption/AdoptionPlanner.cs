using NScreenplay.Mcp.ProjectAnalysis;
using System.Linq;

namespace NScreenplay.Mcp.Adoption;

/// <summary>
/// Deterministic adoption planner.
/// Converts a project analysis result into a structured migration plan.
/// Read-only: no file writes, package installs, builds, tests, or shell execution.
/// </summary>
public sealed class AdoptionPlanner
{
    public AdoptionPlan Create(ProjectAnalysisResult analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var currentState = new AdoptionPlanCurrentState(
            AdoptionLevel: analysis.AdoptionLevel,
            TestFramework: analysis.TestFramework,
            BddFramework: analysis.BddFramework,
            BrowserAutomation: analysis.BrowserAutomation,
            ApiTesting: analysis.ApiTesting);

        var recommendedPackages = BuildRecommendedPackages(analysis);
        var recommendedSkills = BuildRecommendedSkills(analysis);
        var steps = BuildSteps(analysis);
        var risks = BuildRisks(analysis);
        var warnings = BuildWarnings(analysis);
        var preservationRules = BuildPreservationRules(analysis);
        var complexity = DetermineComplexity(analysis);

        return new AdoptionPlan(
            ProjectPath: analysis.ProjectPath,
            CurrentState: currentState,
            RecommendedPackages: recommendedPackages,
            RecommendedSkills: recommendedSkills,
            Steps: steps,
            Risks: risks,
            Warnings: warnings,
            PreservationRules: preservationRules,
            EstimatedComplexity: complexity);
    }

    private static IReadOnlyList<string> BuildRecommendedPackages(ProjectAnalysisResult analysis)
    {
        if (analysis.AdoptionLevel == "already-adopted")
            return [];

        var packages = new List<string>();

        if (!analysis.NScreenplay.Core && (analysis.ScreenplayDetected || analysis.TestFramework is not null || analysis.BddFramework is not null || analysis.BrowserAutomation is not null || analysis.ApiTesting))
            packages.Add("NScreenplay.Core");

        if (!analysis.NScreenplay.Playwright && analysis.BrowserAutomation == "playwright")
            packages.Add("NScreenplay.Playwright");

        if (!analysis.NScreenplay.Reqnroll && analysis.BddFramework == "reqnroll")
            packages.Add("NScreenplay.Reqnroll");

        return packages.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<SkillRecommendation> BuildRecommendedSkills(ProjectAnalysisResult analysis)
    {
        var skills = new List<SkillRecommendation>();

        if (analysis.ScreenplayDetected || analysis.TestFramework is not null || analysis.BrowserAutomation is not null || analysis.BddFramework is not null || analysis.ApiTesting)
            skills.Add(new SkillRecommendation("screenplay", "Required to structure Actors, Abilities, Tasks, Questions, and Interactions."));

        if (analysis.BrowserAutomation == "playwright")
            skills.Add(new SkillRecommendation("playwright", "Relevant because the project uses Playwright browser automation and should keep browser actions behind abilities."));

        if (analysis.BddFramework == "reqnroll")
            skills.Add(new SkillRecommendation("reqnroll", "Relevant because the project uses Reqnroll and should preserve its BDD lifecycle while adopting Screenplay abstractions."));

        if (analysis.TestFramework is not null || analysis.BrowserAutomation is not null || analysis.ApiTesting)
            skills.Add(new SkillRecommendation("test-authoring", "Useful for migration planning and incremental conversion of tests into Screenplay patterns."));

        if (analysis.TestFramework is not null || analysis.BrowserAutomation is not null || analysis.BddFramework is not null || analysis.ApiTesting || analysis.ScreenplayDetected)
            skills.Add(new SkillRecommendation("test-review", "Useful to validate migration quality, preserve boundaries, and avoid over-aggressive refactors."));

        return skills
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static IReadOnlyList<AdoptionPlanStep> BuildSteps(ProjectAnalysisResult analysis)
    {
        var steps = new List<AdoptionPlanStep>();

        if (!analysis.NScreenplay.Core && (analysis.ScreenplayDetected || analysis.TestFramework is not null || analysis.BddFramework is not null || analysis.BrowserAutomation is not null || analysis.ApiTesting))
            steps.Add(Step("introduce-core", "Introduce NScreenplay.Core", "package", "required",
                "The project currently does not reference NScreenplay.Core and needs the core Actor/Task/Question model.", [], ["test project"]));

        if (!analysis.NScreenplay.Playwright && analysis.BrowserAutomation == "playwright")
            steps.Add(Step("introduce-playwright", "Add NScreenplay.Playwright", "package", "required",
                "The project uses Playwright and can move browser actions behind BrowseTheWeb and Screenplay interactions.", ["introduce-core"], ["browser tests", "page objects"]));

        if (!analysis.NScreenplay.Reqnroll && analysis.BddFramework == "reqnroll")
            steps.Add(Step("introduce-reqnroll", "Add NScreenplay.Reqnroll", "package", "required",
                "The project uses Reqnroll and should keep its BDD framework while adopting NScreenplay abstractions.", ["introduce-core"], ["step definitions", "feature hooks"]));

        if (!analysis.NScreenplay.Core && analysis.TestFramework is not null)
            steps.Add(Step("introduce-actor-lifecycle", "Introduce Actor lifecycle", "architecture", "required",
                "The project uses a test framework but has no Screenplay actor lifecycle yet; define Actor creation and disposal rules first.", ["introduce-core"], ["test project", "actor setup"]));

        if (!analysis.NScreenplay.Core && (analysis.BrowserAutomation is not null || analysis.ApiTesting || analysis.TestFramework is not null))
            steps.Add(Step("introduce-ability", "Introduce project-specific Ability", "architecture", "required",
                "Browser or API capabilities should be exposed through abilities rather than direct framework calls in the test code.", ["introduce-core"], ["browser automation", "API testing"]));

        if (analysis.BrowserAutomation is "playwright" || analysis.ApiTesting || analysis.TestFramework is not null)
            steps.Add(Step("organize-tasks-interactions", "Move UI actions into Tasks and Interactions", "refactor", "required",
                "Testing code should move business operations and atomic UI actions behind Tasks and Interactions instead of direct framework calls.", ["introduce-core"], ["tests", "step definitions", "page actions"]));

        if (analysis.BrowserAutomation is "playwright" || analysis.ApiTesting)
            steps.Add(Step("separate-questions", "Move read operations into Questions", "refactor", "required",
                "The project should keep read-only assertions in Questions and isolate orchestration from verification.", ["introduce-core"], ["assertions", "UI reads", "API response checks"]));

        if (analysis.BrowserAutomation is not null || analysis.ApiTesting)
            steps.Add(Step("introduce-targets", "Introduce Targets", "architecture", "required",
                "Locators and endpoint descriptions should be centralized as Targets to avoid duplication and brittle selectors.", ["introduce-core"], ["selectors", "targets", "API endpoints"]));

        if (analysis.BrowserAutomation == "playwright" && !analysis.NScreenplay.Playwright)
            steps.Add(Step("review-direct-playwright", "Review tests for direct Playwright usage", "playwright", "required",
                "Existing Playwright-specific calls should be audited before migration to avoid bypassing Screenplay abstractions.", ["introduce-playwright"], ["browser tests", "locators", "page objects"]));

        if (analysis.BddFramework == "reqnroll")
            steps.Add(Step("refactor-step-definitions", "Refactor Step Definitions", "bdd", "required",
                "Keep Reqnroll step definitions thin by delegating to Actor and Screenplay Tasks instead of embedding browser or business logic.", ["introduce-reqnroll"], ["step definitions", "feature files"]));

        if (analysis.BddFramework == "bddfy")
            steps.Add(Step("preserve-bddfy", "Preserve the existing BDD framework", "bdd", "required",
                "BDDfy was detected; no dedicated NScreenplay BDDfy integration currently exists in this repository. Keep BDDfy and adopt Core/Playwright only where appropriate.", [], ["BDD framework", "step definitions"]));

        if (analysis.ScreenplayDetected && !analysis.NScreenplay.Core)
            steps.Add(Step("map-screenplay", "Review existing Screenplay-like abstractions", "architecture", "recommended",
                "The project already appears to use Screenplay-like concepts and should be mapped carefully to the NScreenplay model without replacing working business logic.", [], ["existing Screenplay-like classes"]));

        if (analysis.ApiTesting && analysis.BrowserAutomation is null)
            steps.Add(Step("manual-api-pattern", "Use NScreenplay.Core with a manual HttpClient-backed Ability", "api", "required",
                "No official NScreenplay API package exists in this repository. Keep xUnit + HttpClient tests and model the API capability with a custom Ability built on NScreenplay.Core.", ["introduce-core"], ["API test helpers", "HTTP assertions", "HttpClient"]));

        if (analysis.ApiTesting && analysis.BrowserAutomation is null)
            steps.Add(Step("api-ability", "Assess an API Ability", "api", "recommended",
                "The project shows API testing evidence and should keep API concerns separate from browser automation and UI concerns.", ["introduce-core"], ["API test helpers", "HTTP assertions"]));

        if (analysis.AdoptionLevel == "already-adopted")
            steps.Add(Step("architecture-review", "Review Screenplay conformance", "architecture", "recommended",
                "The project already appears to use NScreenplay; focus on conformance, boundaries, and test quality.", [], ["existing NScreenplay usage"]));

        if (analysis.AdoptionLevel == "partially-adopted" || analysis.ScreenplayDetected)
            steps.Add(Step("evaluate-migration", "Evaluate migration of existing abstractions", "architecture", "recommended",
                "The project has existing test abstractions that may overlap with NScreenplay; assess whether they should be retained or mapped.", [], ["test utilities", "custom abstractions"]));

        steps.Add(Step("validate-migration", "Validate the migration plan with tests", "validation", "recommended",
            "The plan should be reviewed before any code changes are applied in a later phase.", [], ["tests", "review gate"]));

        return steps
            .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static IReadOnlyList<string> BuildRisks(ProjectAnalysisResult analysis)
    {
        var risks = new List<string>();

        if (analysis.ScreenplayDetected && !analysis.NScreenplay.Core)
            risks.Add("Manual Screenplay-like abstractions may overlap with NScreenplay concepts.");

        if (analysis.BddFramework == "reqnroll")
            risks.Add("BDD framework integration may require project-specific lifecycle handling.");

        if (analysis.BddFramework == "bddfy")
            risks.Add("BDDfy support is not a dedicated NScreenplay integration in this repository.");

        if (analysis.BrowserAutomation == "playwright" && !analysis.NScreenplay.Playwright)
            risks.Add("Existing tests may contain direct Playwright calls that need incremental migration.");

        if (analysis.ApiTesting)
            risks.Add("Existing tests may mix API orchestration with assertion logic.");

        if (analysis.AdoptionLevel is "recommended" or "partially-adopted")
            risks.Add("Migration may require incremental conversion rather than a big-bang rewrite.");

        if (analysis.ScreenplayDetected && analysis.NScreenplay.Core)
            risks.Add("Existing custom abstractions may still need review to ensure they align with NScreenplay conventions.");

        return risks.Distinct().ToList();
    }

    private static IReadOnlyList<string> BuildWarnings(ProjectAnalysisResult analysis)
    {
        var warnings = new List<string>(analysis.Warnings);

        if (analysis.BddFramework == "bddfy")
            warnings.Add("No official NScreenplay BDDfy adapter exists in this repository; preserve BDDfy and use Core/Playwright adoption where appropriate.");

        if (analysis.ApiTesting && analysis.BrowserAutomation is null)
        {
            warnings.Add("API testing was detected without browser automation; do not recommend Playwright unnecessarily.");
            warnings.Add("No official NScreenplay API package exists. Use NScreenplay.Core with a manual HttpClient-backed Ability for API-only tests.");
        }

        if (analysis.BddFramework is not null && analysis.BrowserAutomation is not null)
            warnings.Add("The project combines BDD and browser automation; prefer incremental migration over a broad rewrite.");

        return warnings.Distinct().ToList();
    }

    private static IReadOnlyList<string> BuildPreservationRules(ProjectAnalysisResult analysis)
    {
        var rules = new List<string>
        {
            "Preserve the existing test framework.",
            "Preserve the existing BDD framework unless there is explicit evidence that migration is desired.",
            "Do not modify production code unless the migration plan explicitly requires it.",
            "Do not duplicate existing abstractions.",
            "Do not replace existing business logic with framework-specific code.",
            "Do not bypass Screenplay through direct HTTP or browser calls where existing abstractions can be reused.",
            "Do not move business logic into Step Definitions."
        };

        if (analysis.BddFramework == "reqnroll")
            rules.Add("Preserve the existing Reqnroll framework.");

        if (analysis.BddFramework == "bddfy")
            rules.Add("Preserve the existing BDDfy framework; no replacement recommendation should be inferred and no NScreenplay.BDDfy package should be recommended.");

        if (analysis.BrowserAutomation is null)
            rules.Add("Do not introduce Playwright into API-only projects.");

        if (analysis.ApiTesting && analysis.BrowserAutomation is null)
            rules.Add("No official NScreenplay API package exists; use a manual HttpClient-backed Ability with NScreenplay.Core for API-only tests.");

        if (analysis.ScreenplayDetected)
            rules.Add("Do not overwrite manual Screenplay-like architecture without careful mapping.");

        return rules.Distinct().ToList();
    }

    private static string DetermineComplexity(ProjectAnalysisResult analysis)
    {
        if (analysis.AdoptionLevel == "already-adopted")
            return "low";

        if (analysis.ScreenplayDetected && !analysis.NScreenplay.Core)
            return "high";

        if (analysis.BddFramework is not null && analysis.BrowserAutomation is not null)
            return analysis.ApiTesting ? "high" : "medium";

        if (analysis.ApiTesting && analysis.BrowserAutomation is not null)
            return "medium";

        if (analysis.BddFramework is not null || analysis.BrowserAutomation is not null || analysis.ApiTesting || analysis.ScreenplayDetected)
            return "medium";

        if (analysis.TestFramework is not null)
            return "low";

        return "low";
    }

    private static AdoptionPlanStep Step(string id, string title, string category, string priority, string reason, IReadOnlyList<string> dependsOn, IReadOnlyList<string> affectedAreas) =>
        new(id, title, category, priority, reason, dependsOn, affectedAreas);
}