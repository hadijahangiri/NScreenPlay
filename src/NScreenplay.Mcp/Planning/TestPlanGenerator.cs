using NScreenplay.Mcp.Discovery;
using NScreenplay.Mcp.Models;

namespace NScreenplay.Mcp.Planning;

/// <summary>
/// Generates test plans from requirement analysis using reuse-first principle.
/// Prefers existing Tasks, Targets, Questions, and Interactions over creating new ones.
/// Does NOT modify any code — produces a plan for human review.
/// </summary>
public sealed class TestPlanGenerator
{
    private readonly ComponentDiscovery _discovery;

    public TestPlanGenerator(ComponentDiscovery discovery)
    {
        _discovery = discovery;
    }

    /// <summary>
    /// Generates a complete test plan from a requirement analysis.
    /// Reuse-first: existing components are preferred; new ones are only proposed when needed.
    /// </summary>
    public TestPlan Generate(RequirementAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var existingTasks = _discovery.DiscoverTasks();
        var existingTargets = _discovery.DiscoverTargets();
        var existingQuestions = _discovery.DiscoverQuestions();
        var existingInteractions = _discovery.DiscoverInteractions();

        var scenarios = BuildScenarios(analysis, existingTasks, existingTargets, existingQuestions, existingInteractions);

        var reusedTasks = scenarios
            .SelectMany(s => s.Tasks)
            .Where(t => t.IsExisting)
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        var newTasks = scenarios
            .SelectMany(s => s.Tasks)
            .Where(t => !t.IsExisting)
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        var reusedTargets = scenarios
            .SelectMany(s => s.Targets)
            .Where(t => t.IsExisting)
            .Select(t => $"{t.DeclaringType}.{t.Name}")
            .Distinct()
            .ToList();

        var newTargets = scenarios
            .SelectMany(s => s.Targets)
            .Where(t => !t.IsExisting)
            .Select(t => t.Name)
            .Distinct()
            .ToList();

        var planConfidence = analysis.Confidence switch
        {
            AnalysisConfidence.High => "high",
            AnalysisConfidence.Medium => "medium",
            _ => "low"
        };

        return new TestPlan(
            Requirement: analysis.OriginalRequirement,
            Analysis: analysis,
            Scenarios: scenarios,
            ExistingTasksReused: reusedTasks,
            NewTasksNeeded: newTasks,
            ExistingTargetsReused: reusedTargets,
            NewTargetsNeeded: newTargets,
            Ambiguities: analysis.Ambiguities,
            PlanConfidence: planConfidence);
    }

    private IReadOnlyList<TestScenario> BuildScenarios(
        RequirementAnalysis analysis,
        IReadOnlyList<DiscoveredTask> existingTasks,
        IReadOnlyList<DiscoveredTarget> existingTargets,
        IReadOnlyList<DiscoveredQuestion> existingQuestions,
        IReadOnlyList<DiscoveredInteraction> existingInteractions)
    {
        var scenarios = new List<TestScenario>();
        var actor = analysis.DetectedActors.FirstOrDefault() ?? "user";
        var actorName = char.ToUpper(actor[0]) + actor[1..];

        // Build a happy-path scenario from detected behaviors
        if (analysis.DetectedBehaviors.Contains("login"))
        {
            scenarios.Add(BuildLoginScenario(actorName, existingTasks, existingTargets, existingQuestions, analysis));
        }

        // If no recognizable scenario was built, create a generic one
        if (scenarios.Count == 0)
        {
            scenarios.Add(BuildGenericScenario(actorName, analysis, existingTasks));
        }

        return scenarios;
    }

    private TestScenario BuildLoginScenario(
        string actorName,
        IReadOnlyList<DiscoveredTask> existingTasks,
        IReadOnlyList<DiscoveredTarget> existingTargets,
        IReadOnlyList<DiscoveredQuestion> existingQuestions,
        RequirementAnalysis analysis)
    {
        var isValidLogin = !analysis.DetectedBehaviors.Contains("validation") ||
                           analysis.OriginalRequirement.Contains("valid", StringComparison.OrdinalIgnoreCase);
        var title = isValidLogin ? "Successful login" : "Invalid login is rejected";

        // REUSE-FIRST: find existing login tasks
        var loginTask = existingTasks
            .FirstOrDefault(t => t.Name.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
                                 t.Name.Contains("Credentials", StringComparison.OrdinalIgnoreCase));

        var taskRefs = new List<TaskReference>();
        if (loginTask is not null)
        {
            taskRefs.Add(new TaskReference(loginTask.Name, IsExisting: true, loginTask.FullTypeName, "Perform login with credentials"));
        }
        else
        {
            taskRefs.Add(new TaskReference("LoginWithCredentials", IsExisting: false, null,
                "Create a Task that enters username, password, and clicks login button"));
        }

        // REUSE-FIRST: find existing targets for login page
        var usernameTarget = existingTargets.FirstOrDefault(t =>
            t.Name.Contains("Username", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("Email", StringComparison.OrdinalIgnoreCase));
        var passwordTarget = existingTargets.FirstOrDefault(t =>
            t.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
        var loginBtnTarget = existingTargets.FirstOrDefault(t =>
            t.Name.Contains("Login", StringComparison.OrdinalIgnoreCase) &&
            t.Name.Contains("Button", StringComparison.OrdinalIgnoreCase));
        var dashboardTarget = existingTargets.FirstOrDefault(t =>
            t.Name.Contains("Dashboard", StringComparison.OrdinalIgnoreCase) ||
            t.Name.Contains("Heading", StringComparison.OrdinalIgnoreCase));

        var targetRefs = new List<TargetReference>
        {
            usernameTarget is not null
                ? new TargetReference(usernameTarget.Name, true, usernameTarget.DeclaringType, "Username input field", null)
                : new TargetReference("Username", false, "LoginPage", "Username input field", "ByLabel(\"Username\") or ByTestId(\"username-input\")"),

            passwordTarget is not null
                ? new TargetReference(passwordTarget.Name, true, passwordTarget.DeclaringType, "Password input field", null)
                : new TargetReference("Password", false, "LoginPage", "Password input field", "ByLabel(\"Password\")"),

            loginBtnTarget is not null
                ? new TargetReference(loginBtnTarget.Name, true, loginBtnTarget.DeclaringType, "Login submit button", null)
                : new TargetReference("LoginButton", false, "LoginPage", "Login submit button", "ByRole(\"button\", \"Sign in\")"),
        };

        if (isValidLogin)
        {
            targetRefs.Add(dashboardTarget is not null
                ? new TargetReference(dashboardTarget.Name, true, dashboardTarget.DeclaringType, "Dashboard heading", null)
                : new TargetReference("Heading", false, "DashboardPage", "Dashboard heading", "ByTestId(\"dashboard-heading\")"));
        }

        // REUSE-FIRST: find existing visibility question
        var visibilityQuestion = existingQuestions.FirstOrDefault(q =>
            q.Name.Contains("Visibility", StringComparison.OrdinalIgnoreCase));

        var questionRefs = new List<QuestionReference>
        {
            visibilityQuestion is not null
                ? new QuestionReference(visibilityQuestion.Name, true, visibilityQuestion.AnswerType, "Check dashboard visibility")
                : new QuestionReference("Visibility", false, "bool", "Check element visibility")
        };

        var consequenceName = isValidLogin ? "DashboardIsDisplayed" : "LoginErrorIsDisplayed";
        var consequences = new List<ConsequenceSpec>
        {
            new(consequenceName, false,
                isValidLogin
                    ? "Verify the dashboard heading is visible after successful login"
                    : "Verify the error message is visible after failed login")
        };

        var gherkinSteps = new List<GherkinStep>
        {
            new("Given", "the user is on the login page"),
            new("When", isValidLogin
                ? "the user logs in with valid credentials"
                : "the user logs in with invalid credentials"),
            new("Then", isValidLogin
                ? "the dashboard should be displayed"
                : "the login error should be displayed")
        };

        var implSteps = BuildImplementationSteps(taskRefs, targetRefs, consequences, loginTask is not null);

        return new TestScenario(
            Feature: "Login",
            Title: title,
            Actor: actorName,
            Ability: "BrowseTheWeb",
            GherkinSteps: gherkinSteps,
            Tasks: taskRefs,
            Targets: targetRefs,
            Questions: questionRefs,
            Consequences: consequences,
            ImplementationSteps: implSteps);
    }

    private static TestScenario BuildGenericScenario(
        string actorName,
        RequirementAnalysis analysis,
        IReadOnlyList<DiscoveredTask> existingTasks)
    {
        var behavior = analysis.DetectedBehaviors.FirstOrDefault() ?? "perform action";
        var outcome = analysis.DetectedOutcomes.FirstOrDefault() ?? "verify result";

        var gherkinSteps = new List<GherkinStep>
        {
            new("Given", $"the {actorName.ToLower()} is on the relevant page"),
            new("When", $"the {actorName.ToLower()} {behavior}s"),
            new("Then", outcome.ToLower())
        };

        return new TestScenario(
            Feature: "Feature name here",
            Title: $"{behavior} - {outcome}",
            Actor: actorName,
            Ability: "BrowseTheWeb",
            GherkinSteps: gherkinSteps,
            Tasks: [new TaskReference($"{char.ToUpper(behavior[0])}{behavior[1..]}Task",
                false, null, $"Implement task for: {behavior}")],
            Targets: [],
            Questions: [],
            Consequences: [new ConsequenceSpec("ExpectedOutcome", false, outcome)],
            ImplementationSteps: [
                new(1, "Write the Gherkin scenario", ImplementationAction.WriteGherkin),
                new(2, "Identify UI targets and add to a Page class", ImplementationAction.CreateFile),
                new(3, "Implement the Task class", ImplementationAction.CreateFile),
                new(4, "Implement a Consequence class", ImplementationAction.CreateFile),
                new(5, "Add thin step definitions", ImplementationAction.AddStepDefinition),
            ]);
    }

    private static IReadOnlyList<ImplementationStep> BuildImplementationSteps(
        IReadOnlyList<TaskReference> tasks,
        IReadOnlyList<TargetReference> targets,
        IReadOnlyList<ConsequenceSpec> consequences,
        bool hasExistingTask)
    {
        var steps = new List<ImplementationStep>();
        int order = 1;

        // Write Gherkin first
        steps.Add(new(order++, "Write the Gherkin feature file with the scenario", ImplementationAction.WriteGherkin));

        // New targets need to be defined
        var newTargets = targets.Where(t => !t.IsExisting).ToList();
        if (newTargets.Count > 0)
            steps.Add(new(order++,
                $"Define {newTargets.Count} new Target(s) in the appropriate Page class: {string.Join(", ", newTargets.Select(t => t.Name))}",
                ImplementationAction.AddToExistingFile));

        // Task
        if (hasExistingTask)
            steps.Add(new(order++, $"Reuse existing task: {tasks.First(t => t.IsExisting).Name}", ImplementationAction.ReuseExisting));
        else
            steps.Add(new(order++, $"Implement Task class: {tasks.First(t => !t.IsExisting).Name}", ImplementationAction.CreateFile));

        // New consequences
        var newConsequences = consequences.Where(c => !c.IsExisting).ToList();
        if (newConsequences.Count > 0)
            steps.Add(new(order++,
                $"Implement Consequence class(es): {string.Join(", ", newConsequences.Select(c => c.Name))}",
                ImplementationAction.CreateFile));

        // Step definitions
        steps.Add(new(order++, "Add thin step definitions (one line each, calling Tasks and Consequences)",
            ImplementationAction.AddStepDefinition));

        return steps;
    }
}
