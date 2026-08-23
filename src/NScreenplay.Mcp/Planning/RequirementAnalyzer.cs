using NScreenplay.Mcp.Models;

namespace NScreenplay.Mcp.Planning;

/// <summary>
/// Deterministic requirement analyzer — no LLM needed.
/// Extracts actors, behaviors, outcomes, and flags ambiguities from free-text requirements.
/// </summary>
public sealed class RequirementAnalyzer
{
    private static readonly string[] KnownActors =
        ["user", "admin", "customer", "guest", "operator", "manager", "visitor", "member"];

    private static readonly string[] LoginKeywords =
        ["log in", "login", "sign in", "signin", "authenticate", "credentials"];

    private static readonly string[] LogoutKeywords =
        ["log out", "logout", "sign out", "signout"];

    private static readonly string[] NavigationKeywords =
        ["navigate", "go to", "open", "visit", "redirect", "land on"];

    private static readonly string[] VisibilityKeywords =
        ["see", "view", "display", "show", "visible", "appear", "presented", "shown"];

    private static readonly string[] SearchKeywords =
        ["search", "find", "filter", "query", "look up"];

    private static readonly string[] CheckoutKeywords =
        ["checkout", "purchase", "buy", "order", "cart", "payment"];

    private static readonly string[] ValidationKeywords =
        ["valid", "invalid", "error", "fail", "incorrect", "wrong", "reject"];

    /// <summary>Analyzes a business requirement and extracts structured information.</summary>
    public RequirementAnalysis Analyze(string requirement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requirement);

        var lower = requirement.ToLowerInvariant();
        var actors = ExtractActors(lower);
        var behaviors = ExtractBehaviors(lower);
        var preconditions = ExtractPreconditions(lower);
        var outcomes = ExtractOutcomes(lower);
        var ambiguities = DetectAmbiguities(lower, actors, behaviors, outcomes);
        var missing = DetectMissingInfo(lower, behaviors);

        var confidence = ambiguities.Count == 0 && missing.Count == 0
            ? AnalysisConfidence.High
            : ambiguities.Count + missing.Count <= 2
                ? AnalysisConfidence.Medium
                : AnalysisConfidence.Low;

        return new RequirementAnalysis(
            OriginalRequirement: requirement,
            DetectedActors: actors,
            DetectedBehaviors: behaviors,
            DetectedPreconditions: preconditions,
            DetectedOutcomes: outcomes,
            Ambiguities: ambiguities,
            MissingInformation: missing,
            Confidence: confidence);
    }

    private static IReadOnlyList<string> ExtractActors(string lower)
    {
        var found = KnownActors.Where(a => lower.Contains(a)).ToList();
        return found.Count > 0 ? found : ["user"];  // default to user
    }

    private static IReadOnlyList<string> ExtractBehaviors(string lower)
    {
        var behaviors = new List<string>();
        if (LoginKeywords.Any(k => lower.Contains(k))) behaviors.Add("login");
        if (LogoutKeywords.Any(k => lower.Contains(k))) behaviors.Add("logout");
        if (NavigationKeywords.Any(k => lower.Contains(k))) behaviors.Add("navigation");
        if (SearchKeywords.Any(k => lower.Contains(k))) behaviors.Add("search");
        if (CheckoutKeywords.Any(k => lower.Contains(k))) behaviors.Add("checkout");
        if (ValidationKeywords.Any(k => lower.Contains(k))) behaviors.Add("validation");
        return behaviors;
    }

    private static IReadOnlyList<string> ExtractPreconditions(string lower)
    {
        var preconditions = new List<string>();
        if (lower.Contains("valid") && LoginKeywords.Any(k => lower.Contains(k)))
            preconditions.Add("User has valid credentials");
        if (lower.Contains("logged in") || lower.Contains("authenticated"))
            preconditions.Add("User is already authenticated");
        if (lower.Contains("cart") && lower.Contains("item"))
            preconditions.Add("User has items in cart");
        return preconditions;
    }

    private static IReadOnlyList<string> ExtractOutcomes(string lower)
    {
        var outcomes = new List<string>();
        if (VisibilityKeywords.Any(k => lower.Contains(k)))
        {
            if (lower.Contains("dashboard")) outcomes.Add("Dashboard is displayed");
            else if (lower.Contains("error")) outcomes.Add("Error message is displayed");
            else if (lower.Contains("result")) outcomes.Add("Results are shown");
            else outcomes.Add("Target page/element is visible");
        }
        if (lower.Contains("redirect") || lower.Contains("navigate"))
            outcomes.Add("User is redirected to expected page");
        return outcomes;
    }

    private static IReadOnlyList<string> DetectAmbiguities(
        string lower, IReadOnlyList<string> actors, IReadOnlyList<string> behaviors,
        IReadOnlyList<string> outcomes)
    {
        var ambiguities = new List<string>();
        if (behaviors.Count == 0)
            ambiguities.Add("No recognizable behavior found in requirement. What action is the user performing?");
        if (outcomes.Count == 0)
            ambiguities.Add("No expected outcome detected. What should happen after the action?");
        if (lower.Contains("should") && lower.Contains("not") && lower.Contains("should not") == false)
            ambiguities.Add("Requirement uses 'should not' pattern — ensure negative test cases are covered.");
        return ambiguities;
    }

    private static IReadOnlyList<string> DetectMissingInfo(
        string lower, IReadOnlyList<string> behaviors)
    {
        var missing = new List<string>();
        if (behaviors.Contains("login") && !lower.Contains("credential") && !lower.Contains("password"))
            missing.Add("What credentials should be used? (valid/invalid/specific values)");
        if (behaviors.Contains("search") && !lower.Contains("query") && !lower.Contains("term") && !lower.Contains("keyword"))
            missing.Add("What search term or category should be used?");
        if (behaviors.Contains("checkout") && !lower.Contains("payment") && !lower.Contains("card"))
            missing.Add("What payment method is expected?");
        return missing;
    }
}
