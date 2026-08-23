using NScreenplay.Mcp.Models;

namespace NScreenplay.Mcp.Analysis;

/// <summary>
/// Rule-based failure classifier. No LLM required.
/// Matches exception patterns to failure categories with a confidence rating.
/// </summary>
public sealed class FailureAnalyzer
{
    /// <summary>
    /// Analyzes a structured failure input and returns a categorized result.
    /// This method does NOT modify any code. Analysis only.
    /// </summary>
    public FailureAnalysisResult Analyze(FailureInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (category, confidence, cause, investigation) = Classify(input);

        return new FailureAnalysisResult(
            Category: category,
            Confidence: confidence,
            Evidence: BuildEvidence(input),
            ProbableCause: cause,
            RecommendedInvestigation: investigation,
            DoNotDo: [
                "Do not add Thread.Sleep or Task.Delay as a fix.",
                "Do not modify the Consequence to always pass.",
                "Do not delete the scenario.",
                "Do not change the Gherkin to avoid the assertion.",
                "Do not automatically apply any code change without human review."
            ]);
    }

    private static (string category, ConfidenceLevel confidence, string cause, string investigation)
        Classify(FailureInput input)
    {
        var exType = input.ExceptionType;
        var exMsg = input.ExceptionMessage;

        // Playwright timeout / element not found → Selector or Sync failure
        if (ContainsAny(exType, "TimeoutException", "PlaywrightException") &&
            ContainsAny(exMsg, "not found", "not visible", "locator", "element", "timeout"))
        {
            if (ContainsAny(exMsg, "testid", "data-testid", "selector", "locator", "css", "xpath"))
                return ("SelectorFailure", ConfidenceLevel.High,
                    "The Target's locator strategy no longer matches an element on the page. " +
                    "A UI change likely renamed or restructured the element.",
                    "1. Open the page manually and inspect the element. " +
                    "2. Compare with the Target definition. " +
                    "3. Update the Target's locator strategy to match the current HTML.");

            return ("SynchronizationFailure", ConfidenceLevel.Medium,
                "The element was not ready when the interaction executed. " +
                "A navigation, animation, or loading state may not have completed.",
                "1. Check whether a previous step (Navigate.To) completed successfully. " +
                "2. Verify the page transitions as expected. " +
                "3. Check for loading indicators or overlays blocking the element.");
        }

        // Missing ability → Infrastructure/Configuration failure
        if (ContainsAny(exType, "MissingAbilityException"))
            return ("InfrastructureFailure", ConfidenceLevel.High,
                "The Actor does not have the required Ability. " +
                "The lifecycle hook may not have initialized the ScenarioActor correctly.",
                "1. Check that reqnroll.json includes NScreenplay.Reqnroll in stepAssemblies. " +
                "2. Verify your [BeforeScenario(Order=10)] hook calls InitializeFromFeatureBrowserAsync. " +
                "3. Check that reqnroll.json is copied to the output directory.");

        // BrowserManager key not found → Config failure
        if (ContainsAny(exMsg, "BrowserManager") && ContainsAny(exType, "KeyNotFoundException"))
            return ("InfrastructureFailure", ConfidenceLevel.High,
                "BrowserManager was not stored in FeatureContext. " +
                "The [BeforeFeature] hook from NScreenplay.Reqnroll did not run.",
                "1. Ensure reqnroll.json contains: { \"stepAssemblies\": [{\"assembly\": \"NScreenplay.Reqnroll\"}] }. " +
                "2. Ensure reqnroll.json is CopyToOutputDirectory=PreserveNewest.");

        // Playwright executable not found → Infrastructure
        if (ContainsAny(exMsg, "Executable doesn't exist", "playwright", "chromium", "headless_shell"))
            return ("InfrastructureFailure", ConfidenceLevel.High,
                "The Playwright browser executable is missing. " +
                "Browsers have not been installed or the wrong version is expected.",
                "Run: playwright install chromium (or the appropriate browser).");

        // InvalidOperationException from a Consequence → Test or Application failure
        if (ContainsAny(exType, "InvalidOperationException") && !string.IsNullOrWhiteSpace(input.TargetName))
            return ("TestLogicOrApplicationFailure", ConfidenceLevel.Medium,
                "A Consequence assertion failed. The application may not have reached the expected state, " +
                "or the Consequence is checking the wrong condition.",
                "1. Check the page URL at the time of failure to confirm the app navigated correctly. " +
                "2. Check the Consequence implementation to verify it checks the right element/condition. " +
                "3. Run the scenario headed (Headless=false) to observe the failure visually.");

        // ObjectDisposedException or NullReferenceException → Framework or lifecycle issue
        if (ContainsAny(exType, "ObjectDisposedException", "NullReferenceException"))
            return ("FrameworkFailure", ConfidenceLevel.Low,
                "A null or disposed object was accessed. This may indicate a lifecycle ordering problem.",
                "1. Check that ScenarioActor.InitializeAsync runs before the first step. " +
                "2. Check that disposals are not called before the test completes. " +
                "3. Review hook order attributes (Order=0 vs Order=10).");

        // Default: unknown
        return ("Unknown", ConfidenceLevel.Low,
            "The failure pattern does not match known NScreenplay failure signatures.",
            "1. Read the full stack trace and identify the throwing type. " +
            "2. Consult the failure-analysis skill for manual classification guidance. " +
            "3. Run the scenario with headed browser and verbose logging.");
    }

    private static string BuildEvidence(FailureInput input)
    {
        var parts = new List<string>
        {
            $"Scenario: {input.ScenarioTitle}",
            $"Step: {input.StepText}",
            $"Exception: {input.ExceptionType}: {input.ExceptionMessage[..Math.Min(200, input.ExceptionMessage.Length)]}"
        };
        if (input.TaskName is not null) parts.Add($"Task: {input.TaskName}");
        if (input.InteractionName is not null) parts.Add($"Interaction: {input.InteractionName}");
        if (input.TargetName is not null) parts.Add($"Target: {input.TargetName}");
        if (input.PageUrl is not null) parts.Add($"URL: {input.PageUrl}");
        if (input.ScreenshotAvailable) parts.Add("Screenshot: available");
        return string.Join(" | ", parts);
    }

    private static bool ContainsAny(string source, params string[] terms) =>
        terms.Any(t => source.Contains(t, StringComparison.OrdinalIgnoreCase));
}
