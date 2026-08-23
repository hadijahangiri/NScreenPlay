namespace NScreenplay.Mcp.Models;

/// <summary>A complete test plan generated from a business requirement.</summary>
public sealed record TestPlan(
    string Requirement,
    RequirementAnalysis Analysis,
    IReadOnlyList<TestScenario> Scenarios,
    IReadOnlyList<string> ExistingTasksReused,
    IReadOnlyList<string> NewTasksNeeded,
    IReadOnlyList<string> ExistingTargetsReused,
    IReadOnlyList<string> NewTargetsNeeded,
    IReadOnlyList<string> Ambiguities,
    string PlanConfidence);

/// <summary>One BDD scenario within a test plan.</summary>
public sealed record TestScenario(
    string Feature,
    string Title,
    string Actor,
    string Ability,
    IReadOnlyList<GherkinStep> GherkinSteps,
    IReadOnlyList<TaskReference> Tasks,
    IReadOnlyList<TargetReference> Targets,
    IReadOnlyList<QuestionReference> Questions,
    IReadOnlyList<ConsequenceSpec> Consequences,
    IReadOnlyList<ImplementationStep> ImplementationSteps);

/// <summary>A Gherkin step (Given/When/Then).</summary>
public sealed record GherkinStep(string Keyword, string Text);

/// <summary>Reference to a Task — existing (reuse) or new (create).</summary>
public sealed record TaskReference(string Name, bool IsExisting, string? FullTypeName, string Purpose);

/// <summary>Reference to a Target — existing (reuse) or new (define).</summary>
public sealed record TargetReference(
    string Name,
    bool IsExisting,
    string? DeclaringType,
    string Purpose,
    string? SuggestedStrategy);

/// <summary>Reference to a Question.</summary>
public sealed record QuestionReference(string Name, bool IsExisting, string? AnswerType, string Purpose);

/// <summary>A consequence (assertion) in the plan.</summary>
public sealed record ConsequenceSpec(string Name, bool IsExisting, string Description);

/// <summary>A concrete implementation step for the developer.</summary>
public sealed record ImplementationStep(int Order, string Description, ImplementationAction Action);

/// <summary>What kind of implementation action is required.</summary>
public enum ImplementationAction
{
    /// <summary>Create a new file.</summary>
    CreateFile,
    /// <summary>Add to an existing file.</summary>
    AddToExistingFile,
    /// <summary>Reuse existing — no action needed.</summary>
    ReuseExisting,
    /// <summary>Write a new Gherkin feature/scenario.</summary>
    WriteGherkin,
    /// <summary>Add a thin step definition.</summary>
    AddStepDefinition,
}

/// <summary>Result of deterministic requirement analysis.</summary>
public sealed record RequirementAnalysis(
    string OriginalRequirement,
    IReadOnlyList<string> DetectedActors,
    IReadOnlyList<string> DetectedBehaviors,
    IReadOnlyList<string> DetectedPreconditions,
    IReadOnlyList<string> DetectedOutcomes,
    IReadOnlyList<string> Ambiguities,
    IReadOnlyList<string> MissingInformation,
    AnalysisConfidence Confidence);

/// <summary>Confidence level for deterministic analyses.</summary>
public enum AnalysisConfidence { High, Medium, Low }
